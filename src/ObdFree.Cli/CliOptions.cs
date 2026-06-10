using ObdFree.Core.Adapters;
using ObdFree.Core.Config;
using ObdFree.Core.Modes;
using ObdFree.Core.Transport;
using ObdFree.Core.Uds;
using ObdFree.Core.Vehicles;

namespace ObdFree.Cli;

/// <summary>The fully-resolved options for a command.</summary>
/// <param name="Connection">The adapter connection.</param>
/// <param name="Profile">The vehicle profile (protocol tuning).</param>
/// <param name="Adapter">The adapter profile (reset/timing tuning).</param>
/// <param name="SrsModule">The SRS module addressing (with any header overrides applied).</param>
/// <param name="Mode">The effective operating mode (safe/professional).</param>
internal sealed record CliOptions(
    ObdConnection Connection,
    VehicleProfile Profile,
    AdapterProfile Adapter,
    EcuModule SrsModule,
    OperatingMode Mode);

/// <summary>
/// Parses the flags shared by every command: connection (<c>--usb</c>,
/// <c>--wifi</c>, <c>--bluetooth</c>, <c>--baud</c>) and vehicle selection
/// (<c>--make</c>, <c>--protocol</c>).
/// </summary>
internal static class CliOptionsParser
{
    public static CliOptions? Parse(IReadOnlyList<string> args, out string? error)
    {
        error = null;

        ConnectionKind? kind = null;
        string? target = null;
        int baud = 38400;
        bool baudExplicit = false;
        string? makeKey = null;
        string? protocolText = null;
        string? adapterKey = null;
        string? dongleKey = null;
        string? modeText = null;
        string? srsTx = null;
        string? srsRx = null;

        for (int i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--usb":
                    kind = ConnectionKind.Usb;
                    target = NextValue(args, ref i);
                    break;
                case "--bluetooth":
                case "--bt":
                    kind = ConnectionKind.Bluetooth;
                    target = NextValue(args, ref i);
                    break;
                case "--wifi":
                    kind = ConnectionKind.WiFi;
                    target = NextValue(args, ref i) ?? $"{TcpObdTransport.DefaultHost}:{TcpObdTransport.DefaultPort}";
                    break;
                case "--baud":
                    string? baudText = NextValue(args, ref i);
                    if (!int.TryParse(baudText, out baud))
                    {
                        error = $"Invalid --baud value: '{baudText}'.";
                        return null;
                    }

                    baudExplicit = true;
                    break;
                case "--dongle":
                    dongleKey = NextValue(args, ref i);
                    break;
                case "--mode":
                    modeText = NextValue(args, ref i);
                    break;
                case "--make":
                    makeKey = NextValue(args, ref i);
                    break;
                case "--protocol":
                    protocolText = NextValue(args, ref i);
                    break;
                case "--adapter":
                    adapterKey = NextValue(args, ref i);
                    break;
                case "--srs-tx":
                    srsTx = NextValue(args, ref i);
                    break;
                case "--srs-rx":
                    srsRx = NextValue(args, ref i);
                    break;
                default:
                    error = $"Unknown argument: '{args[i]}'.";
                    return null;
            }
        }

        // A known dongle fills in connection / baud / adapter defaults.
        if (!string.IsNullOrWhiteSpace(dongleKey))
        {
            if (!KnownAdapters.TryGet(dongleKey, out KnownAdapter? dongle) || dongle is null)
            {
                error = $"Unknown --dongle '{dongleKey}'. Run 'obd dongles' to list known dongles.";
                return null;
            }

            if (!dongle.IsSupported)
            {
                error = $"{dongle.DisplayName} uses Bluetooth LE, which isn't supported yet. "
                    + "Use a USB, Wi-Fi, or classic-Bluetooth adapter (run 'obd dongles').";
                return null;
            }

            adapterKey ??= dongle.AdapterProfileKey;
            if (!baudExplicit && dongle.DefaultBaud > 0)
            {
                baud = dongle.DefaultBaud;
            }

            if (kind is null)
            {
                if (dongle.Link == DongleLink.WiFi)
                {
                    kind = ConnectionKind.WiFi;
                    target = dongle.DefaultEndpoint;
                }
                else
                {
                    error = $"{dongle.DisplayName} needs a serial port. Add --usb <port> or --bluetooth <port>.";
                    return null;
                }
            }
        }

        if (kind is null)
        {
            error = "No connection specified. Use --usb <port>, --wifi [host:port], --bluetooth <port>, or --dongle <key>.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            error = $"Connection '{kind}' requires a target (port or endpoint).";
            return null;
        }

        VehicleProfile? profile = ResolveProfile(makeKey, protocolText, out error);
        if (profile is null)
        {
            return null;
        }

        AdapterProfile adapter = AdapterProfiles.Standard;
        if (!string.IsNullOrWhiteSpace(adapterKey))
        {
            if (!AdapterProfiles.TryGet(adapterKey, out AdapterProfile? found) || found is null)
            {
                error = $"Unknown --adapter '{adapterKey}'. Known: {string.Join(", ", AdapterProfiles.All.Select(a => a.Key))}.";
                return null;
            }

            adapter = found;
        }

        // Effective mode: explicit --mode overrides the persisted config; else config (default Safe).
        OperatingMode mode = new ConfigStore().Load().Mode;
        if (!string.IsNullOrWhiteSpace(modeText))
        {
            if (!TryParseMode(modeText, out mode))
            {
                error = $"Unknown --mode '{modeText}'. Use 'safe' or 'professional'.";
                return null;
            }
        }

        ObdConnection connection = kind switch
        {
            ConnectionKind.Usb => ObdConnection.Usb(target, baud),
            ConnectionKind.Bluetooth => ObdConnection.Bluetooth(target, baud),
            _ => ObdConnection.WiFi(target),
        };

        EcuModule srsModule = ToyotaModules.Srs.WithHeaders(srsTx, srsRx);

        return new CliOptions(connection, profile, adapter, srsModule, mode);
    }

    /// <summary>Parses a mode name (<c>safe</c> / <c>professional</c>, or <c>pro</c>).</summary>
    public static bool TryParseMode(string text, out OperatingMode mode)
    {
        switch (text.Trim().ToLowerInvariant())
        {
            case "safe":
                mode = OperatingMode.Safe;
                return true;
            case "professional" or "pro":
                mode = OperatingMode.Professional;
                return true;
            default:
                mode = OperatingMode.Safe;
                return false;
        }
    }

    /// <summary>
    /// Resolves a vehicle profile from <c>--make</c>, applying an optional
    /// <c>--protocol</c> override. If no make is given, prompts interactively
    /// (when a console is attached) or falls back to the generic profile.
    /// </summary>
    private static VehicleProfile? ResolveProfile(string? makeKey, string? protocolText, out string? error)
    {
        error = null;

        VehicleProfile profile;
        if (string.IsNullOrWhiteSpace(makeKey))
        {
            profile = PromptForProfile();
        }
        else if (!VehicleProfiles.TryGet(makeKey, out VehicleProfile? found) || found is null)
        {
            error = $"Unknown --make '{makeKey}'. Known: {string.Join(", ", VehicleProfiles.All.Select(p => p.Key))}.";
            return null;
        }
        else
        {
            profile = found;
        }

        if (!string.IsNullOrWhiteSpace(protocolText))
        {
            if (!ObdProtocolExtensions.TryParse(protocolText, out ObdProtocol overridden))
            {
                error = $"Unknown --protocol '{protocolText}'. Try: auto, can, can29, iso9141, kwp.";
                return null;
            }

            profile = profile with { PreferredProtocol = overridden };
        }

        return profile;
    }

    /// <summary>
    /// Asks the user to pick a car make when one wasn't supplied. Non-interactive
    /// sessions (piped/redirected input) silently use the generic profile.
    /// </summary>
    private static VehicleProfile PromptForProfile()
    {
        if (Console.IsInputRedirected)
        {
            return VehicleProfiles.Generic;
        }

        VehicleProfile[] choices = [.. VehicleProfiles.All];

        Console.WriteLine("Select your car make (helps pick the right OBD protocol):");
        for (int i = 0; i < choices.Length; i++)
        {
            Console.WriteLine($"  [{i + 1}] {choices[i].DisplayName}");
        }

        Console.Write($"Choice [1-{choices.Length}, default 1]: ");
        string? answer = Console.ReadLine();
        if (int.TryParse(answer, out int pick) && pick >= 1 && pick <= choices.Length)
        {
            return choices[pick - 1];
        }

        return VehicleProfiles.Generic;
    }

    private static string? NextValue(IReadOnlyList<string> args, ref int i)
    {
        if (i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            return args[++i];
        }

        return null;
    }
}
