using System.Reflection;
using ObdFree.Cli;
using ObdFree.Core;
using ObdFree.Core.Adapters;
using ObdFree.Core.Config;
using ObdFree.Core.Diagnostics;
using ObdFree.Core.Modes;
using ObdFree.Core.Readiness;
using ObdFree.Core.Transport;
using ObdFree.Core.Uds;

string version = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    PrintUsage(version);
    return 0;
}

if (args[0] is "dongles")
{
    PrintDongles();
    return 0;
}

if (args[0] is "config")
{
    return RunConfig(args[1..]);
}

string command = args[0].ToLowerInvariant();
string[] commandArgs = args[1..];

// "dtc read|clear" and "srs status|read|clear" are two-word commands.
string subcommand = string.Empty;
if (command is "dtc" or "srs" && commandArgs.Length > 0 && !commandArgs[0].StartsWith("--", StringComparison.Ordinal))
{
    subcommand = commandArgs[0].ToLowerInvariant();
    commandArgs = commandArgs[1..];
}

CliOptions? options = CliOptionsParser.Parse(commandArgs, out string? error);
if (options is null)
{
    Console.Error.WriteLine($"Error: {error}");
    Console.Error.WriteLine("Run 'obd --help' for usage.");
    return 2;
}

// Gate dangerous commands behind professional mode before touching the adapter.
AppFeature? requiredFeature = command switch
{
    "dtc" when subcommand == "clear" => AppFeature.ClearDtc,
    "srs" when subcommand == "clear" => AppFeature.SrsClear,
    "srs" => AppFeature.SrsRead,
    _ => null,
};
if (requiredFeature is { } feature && !ModePolicy.IsAllowed(options.Mode, feature))
{
    string label = $"{command} {subcommand}".Trim();
    Console.Error.WriteLine($"'{label}' requires PROFESSIONAL mode (current: {options.Mode}).");
    Console.Error.WriteLine("Re-run with '--mode professional', or set it permanently:");
    Console.Error.WriteLine("  obd config set mode professional");
    return 3;
}

Console.WriteLine($"obd-free-tool {version}");
Console.WriteLine($"Mode       : {options.Mode}");
Console.WriteLine($"Vehicle    : {options.Profile.DisplayName}");
Console.WriteLine($"Adapter    : {options.Adapter.DisplayName}");
Console.WriteLine($"Connecting via {options.Connection}...");

try
{
    await using var session = new ObdSession(
        options.Connection.CreateTransport(), options.Profile, options.Adapter, options.Mode);

    int code = command switch
    {
        "status" => await Run(() => RunStatusAsync(session)),
        "readiness" or "monitors" => await Run(() => RunReadinessAsync(session)),
        "vin" => await Run(() => RunVinAsync(session)),
        "dtc" when subcommand == "read" => await Run(() => RunDtcReadAsync(session)),
        "dtc" when subcommand == "clear" => await RunDtcClearAsync(session),
        "srs" when subcommand is "status" or "read" or "" => await Run(() => RunSrsReadAsync(session, options.SrsModule)),
        "srs" when subcommand == "clear" => await RunSrsClearAsync(session, options.SrsModule),
        _ => Unknown(command, subcommand, version),
    };

    // If the device never answered ATI like an ELM327, it is most likely a
    // proprietary dongle (e.g. Launch DBSCAR). Make that explicit.
    if (session.AdapterIdentity is not null && !session.AdapterLooksElmCompatible)
    {
        Console.WriteLine();
        Console.WriteLine("WARNING: " + AdapterCompatibility.ProprietaryAdapterHint);
    }

    return code;

    static async Task<int> Run(Func<Task> action)
    {
        await action();
        return 0;
    }

    static int Unknown(string command, string subcommand, string version)
    {
        Console.Error.WriteLine($"Unknown command: '{command} {subcommand}'.");
        PrintUsage(version);
        return 2;
    }
}
catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or System.Net.Sockets.SocketException)
{
    Console.Error.WriteLine($"Connection error: {ex.Message}");
    return 1;
}

static async Task RunStatusAsync(ObdSession session)
{
    ObdStatus status = await session.GetStatusAsync();

    Console.WriteLine();
    Console.WriteLine($"Adapter : {status.AdapterIdentity}");
    Console.WriteLine($"Battery : {status.BatteryVoltage ?? "n/a"}");
    Console.WriteLine();

    if (status.Readings.Count == 0)
    {
        Console.WriteLine("No live parameters responded (engine off, or unsupported).");
        return;
    }

    Console.WriteLine("Live data:");
    foreach (LiveReading reading in status.Readings)
    {
        Console.WriteLine($"  {reading.Definition.Name,-24} {reading.Value}");
    }
}

static async Task RunDtcReadAsync(ObdSession session)
{
    await session.ConnectAsync();
    IReadOnlyList<DiagnosticTroubleCode> stored = await session.ReadStoredCodesAsync();
    IReadOnlyList<DiagnosticTroubleCode> pending = await session.ReadPendingCodesAsync();
    IReadOnlyList<DiagnosticTroubleCode> permanent = await session.ReadPermanentCodesAsync();

    Console.WriteLine();
    PrintCodes("Stored codes (Mode 03)", stored);
    PrintCodes("Pending codes (Mode 07)", pending);
    PrintCodes("Permanent codes (Mode 0A, cannot be cleared)", permanent);
}

static async Task RunReadinessAsync(ObdSession session)
{
    await session.ConnectAsync();
    MonitorStatus? status = await session.ReadReadinessAsync();

    Console.WriteLine();
    if (status is null)
    {
        Console.WriteLine("Readiness unavailable (no response to Mode 01 PID 01).");
        return;
    }

    Console.WriteLine($"MIL (check engine light): {(status.MilOn ? "ON" : "off")}");
    Console.WriteLine($"Confirmed DTCs          : {status.DtcCount}");
    Console.WriteLine($"Engine type             : {(status.IsCompressionIgnition ? "compression (diesel)" : "spark (gasoline)")}");
    Console.WriteLine();
    Console.WriteLine("Emissions monitors (I/M readiness):");
    foreach (MonitorReadiness monitor in status.Monitors)
    {
        Console.WriteLine($"  {monitor.Name,-26} {monitor.StatusText}");
    }

    Console.WriteLine();
    Console.WriteLine(status.LikelyReadyForInspection
        ? $"=> Likely READY for a US emissions/smog check ({status.NotReadyCount} monitor(s) not ready, MIL off)."
        : $"=> Likely NOT ready: MIL {(status.MilOn ? "is ON" : "off")}, {status.NotReadyCount} monitor(s) not ready.");
    Console.WriteLine("   (Guidance only — exact pass rules vary by state.)");
}

static async Task RunVinAsync(ObdSession session)
{
    await session.ConnectAsync();
    string? vin = await session.ReadVinAsync();

    Console.WriteLine();
    Console.WriteLine(vin is null
        ? "VIN unavailable (no response to Mode 09 PID 02)."
        : $"VIN: {vin}");
}

static void PrintCodes(string title, IReadOnlyList<DiagnosticTroubleCode> codes)
{
    Console.WriteLine($"{title}:");
    if (codes.Count == 0)
    {
        Console.WriteLine("  (none)");
    }
    else
    {
        foreach (DiagnosticTroubleCode code in codes)
        {
            Console.WriteLine($"  {code.Code}");
        }
    }

    Console.WriteLine();
}

static async Task<int> RunDtcClearAsync(ObdSession session)
{
    await session.ConnectAsync();

    Console.Write("This will CLEAR all stored trouble codes and turn off the MIL. Continue? [y/N] ");
    string? answer = Console.ReadLine();
    if (answer?.Trim().ToLowerInvariant() is not ("y" or "yes"))
    {
        Console.WriteLine("Aborted.");
        return 0;
    }

    bool ok = await session.ClearCodesAsync();
    Console.WriteLine(ok ? "Trouble codes cleared." : "Clear was not acknowledged by the ECU.");
    return ok ? 0 : 1;
}

static async Task RunSrsReadAsync(ObdSession session, EcuModule module)
{
    Console.WriteLine($"Querying {module.Name} module (tx {module.RequestHeader} / rx {module.ResponseHeader})...");
    ModuleStatus status = await session.ReadModuleStatusAsync(module);

    Console.WriteLine();
    Console.WriteLine($"SRS / Airbag status: {status.Summary}");
    if (status.HasFaults)
    {
        foreach (UdsDtc code in status.Codes)
        {
            Console.WriteLine($"  {code}{(code.IsActive ? "  (active)" : string.Empty)}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Note: SRS addressing is make-specific and experimental. If you see no");
    Console.WriteLine("response, override headers with --srs-tx/--srs-rx for your vehicle.");
}

static async Task<int> RunSrsClearAsync(ObdSession session, EcuModule module)
{
    Console.WriteLine("!! SAFETY WARNING !!");
    Console.WriteLine("Clearing SRS/airbag codes does NOT repair the fault. Only clear codes");
    Console.WriteLine("AFTER the underlying airbag/seat-belt issue has been physically fixed.");
    Console.WriteLine("An improperly working SRS may not deploy in a crash.");
    Console.WriteLine();
    Console.Write($"Clear ALL codes from the {module.Name} module? Type 'yes' to confirm: ");
    string? answer = Console.ReadLine();
    if (answer?.Trim().ToLowerInvariant() is not "yes")
    {
        Console.WriteLine("Aborted.");
        return 0;
    }

    bool ok = await session.ClearModuleCodesAsync(module);
    Console.WriteLine(ok ? "SRS codes cleared." : "Clear was not acknowledged by the SRS module.");
    return ok ? 0 : 1;
}

static int RunConfig(string[] args)
{
    var store = new ConfigStore();
    AppConfig config = store.Load();

    if (args.Length == 0 || args[0] is "get" or "show")
    {
        Console.WriteLine($"Mode       : {config.Mode}");
        Console.WriteLine($"Connection : {config.ConnectionKind}");
        Console.WriteLine($"Target     : {config.Target}");
        Console.WriteLine($"Baud       : {config.BaudRate}");
        Console.WriteLine($"Vehicle    : {config.VehicleProfileKey}");
        Console.WriteLine($"Adapter    : {config.AdapterProfileKey}");
        Console.WriteLine($"File       : {store.Path}");
        return 0;
    }

    if (args[0] is "path")
    {
        Console.WriteLine(store.Path);
        return 0;
    }

    if (args[0] is "set" && args.Length >= 3 && args[1] is "mode")
    {
        if (!CliOptionsParser.TryParseMode(args[2], out OperatingMode mode))
        {
            Console.Error.WriteLine($"Unknown mode '{args[2]}'. Use 'safe' or 'professional'.");
            return 2;
        }

        config.Mode = mode;
        store.Save(config);
        Console.WriteLine($"Default mode set to {mode}. ({store.Path})");
        return 0;
    }

    Console.Error.WriteLine("Usage: obd config [get | path | set mode <safe|professional>]");
    return 2;
}

static void PrintDongles()
{
    Console.WriteLine("Known OBD-II dongles (use with --dongle <key>):");
    Console.WriteLine();
    foreach (KnownAdapter d in KnownAdapters.All)
    {
        string link = d.Link switch
        {
            DongleLink.Usb => "USB",
            DongleLink.WiFi => "Wi-Fi",
            DongleLink.BluetoothClassic => "Bluetooth (classic)",
            _ => "Bluetooth LE",
        };
        string status = d.IsSupported ? "supported" : "NOT YET (BLE)";
        Console.WriteLine($"  {d.Key,-20} {d.DisplayName,-34} {link,-20} {status}");
    }

    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  obd status --dongle veepeak-wifi               # Wi-Fi: endpoint auto-set");
    Console.WriteLine("  obd readiness --dongle bafx-bt --bluetooth /dev/rfcomm0");
    Console.WriteLine("  obd status --dongle generic-usb --usb /dev/ttyUSB0");
    Console.WriteLine();
    Console.WriteLine("BLE-only dongles (Veepeak BLE, OBDLink CX, Vgate iCar Pro BLE) aren't");
    Console.WriteLine("supported yet — Bluetooth LE needs a transport we haven't built.");
}

static void PrintUsage(string version)
{
    Console.WriteLine($"obd-free-tool {version} — free & open-source OBD-II tool");
    Console.WriteLine();
    Console.WriteLine("USAGE:");
    Console.WriteLine("  obd <command> <connection>");
    Console.WriteLine();
    Console.WriteLine("COMMANDS:");
    Console.WriteLine("  status        Show adapter status and a live-data snapshot");
    Console.WriteLine("  readiness     Show MIL + emissions monitors (US smog/I-M readiness)");
    Console.WriteLine("  vin           Read the Vehicle Identification Number (Mode 09)");
    Console.WriteLine("  dtc read      Read stored, pending, and permanent trouble codes");
    Console.WriteLine("  dtc clear     Clear trouble codes from memory (asks for confirmation)");
    Console.WriteLine("  srs status    Show SRS/airbag status & codes (Toyota/Lexus, UDS on CAN)");
    Console.WriteLine("  srs clear     Clear SRS/airbag codes (safety warning + confirmation)");
    Console.WriteLine("  dongles       List known Amazon OBD-II dongles and how to connect");
    Console.WriteLine("  config        get | path | set mode <safe|professional>");
    Console.WriteLine();
    Console.WriteLine("MODE (safe is the default; professional unlocks writes & risky features):");
    Console.WriteLine("  --mode <m>          safe or professional (overrides saved config for this run)");
    Console.WriteLine("  Professional-only: 'dtc clear', 'srs status', 'srs clear'.");
    Console.WriteLine();
    Console.WriteLine("CONNECTION (choose one):");
    Console.WriteLine("  --usb <port>        USB/serial adapter, e.g. /dev/ttyUSB0 or COM3");
    Console.WriteLine("  --wifi [host:port]  Wi-Fi adapter (default 192.168.0.10:35000)");
    Console.WriteLine("  --bluetooth <port>  Bluetooth (SPP) adapter serial device");
    Console.WriteLine("  --baud <rate>       Serial baud rate (default 38400)");
    Console.WriteLine("  --dongle <key>      Auto-configure for a known dongle (see 'obd dongles')");
    Console.WriteLine();
    Console.WriteLine("VEHICLE (optional — improves protocol selection):");
    Console.WriteLine("  --make <make>       Car make: generic, toyota, lexus (prompts if omitted)");
    Console.WriteLine("  --protocol <proto>  Force a protocol: auto, can, can29, iso9141, kwp");
    Console.WriteLine();
    Console.WriteLine("ADAPTER (optional):");
    Console.WriteLine("  --adapter <kind>    standard (default) or launch (tolerant clone/Launch timing)");
    Console.WriteLine("                      Note: proprietary Launch DBSCAR dongles are NOT supported.");
    Console.WriteLine();
    Console.WriteLine("SRS (Toyota/Lexus, experimental — addresses vary by model):");
    Console.WriteLine("  --srs-tx <hex>      Override SRS request CAN header (default 7B0)");
    Console.WriteLine("  --srs-rx <hex>      Override SRS response CAN header (default 7B8)");
    Console.WriteLine();
    Console.WriteLine("EXAMPLES:");
    Console.WriteLine("  obd status --usb /dev/ttyUSB0 --make toyota");
    Console.WriteLine("  obd readiness --usb /dev/ttyUSB0      # will it pass smog?");
    Console.WriteLine("  obd vin --wifi");
    Console.WriteLine("  obd dtc read --wifi --make lexus");
    Console.WriteLine("  obd srs status --usb /dev/ttyUSB0 --make toyota");
    Console.WriteLine("  obd srs clear --bluetooth /dev/rfcomm0 --make toyota --srs-tx 7B0 --srs-rx 7B8");
}
