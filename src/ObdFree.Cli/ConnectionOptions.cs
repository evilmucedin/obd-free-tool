using ObdFree.Core.Transport;

namespace ObdFree.Cli;

/// <summary>
/// Parses the connection flags shared by every command and builds an
/// <see cref="ObdConnection"/>.
/// </summary>
internal static class ConnectionOptions
{
    /// <summary>
    /// Extracts connection flags from <paramref name="args"/>, returning the
    /// remaining (non-connection) arguments via <paramref name="rest"/>.
    /// </summary>
    public static ObdConnection? Parse(IReadOnlyList<string> args, out List<string> rest, out string? error)
    {
        rest = [];
        error = null;

        ConnectionKind? kind = null;
        string? target = null;
        int baud = 38400;

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            switch (arg)
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

                    break;
                default:
                    rest.Add(arg);
                    break;
            }
        }

        if (kind is null)
        {
            error = "No connection specified. Use --usb <port>, --wifi [host:port], or --bluetooth <port>.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            error = $"Connection '{kind}' requires a target (port or endpoint).";
            return null;
        }

        return kind switch
        {
            ConnectionKind.Usb => ObdConnection.Usb(target, baud),
            ConnectionKind.Bluetooth => ObdConnection.Bluetooth(target, baud),
            ConnectionKind.WiFi => ObdConnection.WiFi(target),
            _ => null,
        };
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
