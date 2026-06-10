using System.Reflection;
using ObdFree.Cli;
using ObdFree.Core;
using ObdFree.Core.Diagnostics;
using ObdFree.Core.Transport;

string version = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    PrintUsage(version);
    return 0;
}

string command = args[0].ToLowerInvariant();
string[] commandArgs = args[1..];

// "dtc read" / "dtc clear" are two-word commands.
string subcommand = string.Empty;
if (command == "dtc" && commandArgs.Length > 0)
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

Console.WriteLine($"obd-free-tool {version}");
Console.WriteLine($"Vehicle    : {options.Profile.DisplayName}");
Console.WriteLine($"Connecting via {options.Connection}...");

try
{
    await using var session = new ObdSession(options.Connection.CreateTransport(), options.Profile);

    switch (command)
    {
        case "status":
            await RunStatusAsync(session);
            return 0;

        case "dtc" when subcommand == "read":
            await RunDtcReadAsync(session);
            return 0;

        case "dtc" when subcommand == "clear":
            return await RunDtcClearAsync(session);

        default:
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

    Console.WriteLine();
    PrintCodes("Stored codes (Mode 03)", stored);
    PrintCodes("Pending codes (Mode 07)", pending);
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

static void PrintUsage(string version)
{
    Console.WriteLine($"obd-free-tool {version} — free & open-source OBD-II tool");
    Console.WriteLine();
    Console.WriteLine("USAGE:");
    Console.WriteLine("  obd <command> <connection>");
    Console.WriteLine();
    Console.WriteLine("COMMANDS:");
    Console.WriteLine("  status        Show adapter status and a live-data snapshot");
    Console.WriteLine("  dtc read      Read stored and pending trouble codes");
    Console.WriteLine("  dtc clear     Clear trouble codes from memory (asks for confirmation)");
    Console.WriteLine();
    Console.WriteLine("CONNECTION (choose one):");
    Console.WriteLine("  --usb <port>        USB/serial adapter, e.g. /dev/ttyUSB0 or COM3");
    Console.WriteLine("  --wifi [host:port]  Wi-Fi adapter (default 192.168.0.10:35000)");
    Console.WriteLine("  --bluetooth <port>  Bluetooth (SPP) adapter serial device");
    Console.WriteLine("  --baud <rate>       Serial baud rate (default 38400)");
    Console.WriteLine();
    Console.WriteLine("VEHICLE (optional — improves protocol selection):");
    Console.WriteLine("  --make <make>       Car make: generic, toyota, lexus (prompts if omitted)");
    Console.WriteLine("  --protocol <proto>  Force a protocol: auto, can, can29, iso9141, kwp");
    Console.WriteLine();
    Console.WriteLine("EXAMPLES:");
    Console.WriteLine("  obd status --usb /dev/ttyUSB0 --make toyota");
    Console.WriteLine("  obd dtc read --wifi --make lexus");
    Console.WriteLine("  obd dtc clear --bluetooth /dev/rfcomm0 --make toyota");
}
