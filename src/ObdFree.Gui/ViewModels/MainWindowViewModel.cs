using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObdFree.Core;
using ObdFree.Core.Adapters;
using ObdFree.Core.Diagnostics;
using ObdFree.Core.Transport;
using ObdFree.Core.Uds;
using ObdFree.Core.Vehicles;

namespace ObdFree.Gui.ViewModels;

/// <summary>
/// Drives the main window: choose a connection (USB / Wi-Fi / Bluetooth) and a
/// car make, then read status, read trouble codes, or clear them — the same
/// workflow as the CLI, backed by the shared <see cref="ObdSession"/>.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ConnectionKind _selectedConnection = ConnectionKind.Usb;

    [ObservableProperty]
    private string _target = "/dev/ttyUSB0";

    [ObservableProperty]
    private int _baudRate = 38400;

    [ObservableProperty]
    private VehicleProfile _selectedProfile = VehicleProfiles.Generic;

    [ObservableProperty]
    private AdapterProfile _selectedAdapter = AdapterProfiles.Standard;

    [ObservableProperty]
    private string _output = "Ready. Pick a connection and a car make, then choose an action.";

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>The connection kinds offered in the UI.</summary>
    public IReadOnlyList<ConnectionKind> ConnectionKinds { get; } =
        [ConnectionKind.Usb, ConnectionKind.WiFi, ConnectionKind.Bluetooth];

    /// <summary>The vehicle profiles offered in the UI.</summary>
    public IReadOnlyList<VehicleProfile> Profiles { get; } = [.. VehicleProfiles.All];

    /// <summary>The adapter profiles offered in the UI.</summary>
    public IReadOnlyList<AdapterProfile> Adapters { get; } = [.. AdapterProfiles.All];

    /// <summary>True when an action can run (not already busy).</summary>
    public bool CanRun => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRun));
        OnPropertyChanged(nameof(CanClearSrs));
    }

    partial void OnSelectedConnectionChanged(ConnectionKind value) =>
        Target = value switch
        {
            ConnectionKind.WiFi => $"{TcpObdTransport.DefaultHost}:{TcpObdTransport.DefaultPort}",
            ConnectionKind.Bluetooth => "/dev/rfcomm0",
            _ => "/dev/ttyUSB0",
        };

    /// <summary>Reads adapter status and a live-data snapshot.</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task StatusAsync() => RunAsync(async session =>
    {
        ObdStatus status = await session.GetStatusAsync();
        var sb = new StringBuilder();
        sb.AppendLine($"Adapter : {status.AdapterIdentity}");
        sb.AppendLine($"Battery : {status.BatteryVoltage ?? "n/a"}");
        sb.AppendLine();
        if (status.Readings.Count == 0)
        {
            sb.AppendLine("No live parameters responded (engine off, or unsupported).");
        }
        else
        {
            sb.AppendLine("Live data:");
            foreach (LiveReading reading in status.Readings)
            {
                sb.AppendLine($"  {reading.Definition.Name,-24} {reading.Value}");
            }
        }

        return sb.ToString();
    });

    /// <summary>Reads stored and pending trouble codes.</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task ReadCodesAsync() => RunAsync(async session =>
    {
        await session.ConnectAsync();
        IReadOnlyList<DiagnosticTroubleCode> stored = await session.ReadStoredCodesAsync();
        IReadOnlyList<DiagnosticTroubleCode> pending = await session.ReadPendingCodesAsync();

        var sb = new StringBuilder();
        AppendCodes(sb, "Stored codes (Mode 03)", stored);
        AppendCodes(sb, "Pending codes (Mode 07)", pending);
        return sb.ToString();
    });

    /// <summary>Clears stored trouble codes from memory (Mode 04).</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task ClearCodesAsync() => RunAsync(async session =>
    {
        await session.ConnectAsync();
        bool ok = await session.ClearCodesAsync();
        return ok
            ? "Trouble codes cleared. The MIL ('check engine' light) should turn off."
            : "Clear was not acknowledged by the ECU.";
    });

    /// <summary>Reads SRS/airbag status and codes (Toyota/Lexus, UDS over CAN).</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task ReadSrsAsync() => RunAsync(async session =>
    {
        ModuleStatus status = await session.ReadModuleStatusAsync(ToyotaModules.Srs);
        var sb = new StringBuilder();
        sb.AppendLine($"SRS / Airbag status: {status.Summary}");
        sb.AppendLine($"(module {status.Module.RequestHeader}/{status.Module.ResponseHeader})");
        sb.AppendLine();
        if (status.HasFaults)
        {
            foreach (UdsDtc code in status.Codes)
            {
                sb.AppendLine($"  {code}{(code.IsActive ? "  (active)" : string.Empty)}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Note: SRS addressing is make-specific and experimental.");
        return sb.ToString();
    });

    /// <summary>Clears SRS/airbag codes after the user confirms the safety warning.</summary>
    [RelayCommand(CanExecute = nameof(CanClearSrs))]
    private Task ClearSrsAsync() => RunAsync(async session =>
    {
        bool ok = await session.ClearModuleCodesAsync(ToyotaModules.Srs);
        SrsClearConfirmed = false;
        return ok
            ? "SRS codes cleared. If the airbag warning light stays on, the fault is still present."
            : "Clear was not acknowledged by the SRS module.";
    });

    /// <summary>
    /// Gets or sets a value indicating whether the user has acknowledged the SRS
    /// safety warning. The Clear SRS button stays disabled until this is set.
    /// </summary>
    [ObservableProperty]
    private bool _srsClearConfirmed;

    /// <summary>True when SRS clearing is allowed (not busy and the warning is acknowledged).</summary>
    public bool CanClearSrs => !IsBusy && SrsClearConfirmed;

    partial void OnSrsClearConfirmedChanged(bool value) => OnPropertyChanged(nameof(CanClearSrs));

    private async Task RunAsync(System.Func<ObdSession, Task<string>> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Output = $"Connecting via {DescribeConnection()}...";
        try
        {
            ObdConnection connection = BuildConnection();
            await using var session = new ObdSession(connection.CreateTransport(), SelectedProfile, SelectedAdapter);
            string result = await action(session);

            // Flag proprietary (non-ELM327) dongles such as Launch DBSCAR.
            if (session.AdapterIdentity is not null && !session.AdapterLooksElmCompatible)
            {
                result += "\n\nWARNING: " + AdapterCompatibility.ProprietaryAdapterHint;
            }

            Output = result;
        }
        catch (System.Exception ex)
        {
            Output = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ObdConnection BuildConnection() => SelectedConnection switch
    {
        ConnectionKind.WiFi => ObdConnection.WiFi(Target),
        ConnectionKind.Bluetooth => ObdConnection.Bluetooth(Target, BaudRate),
        _ => ObdConnection.Usb(Target, BaudRate),
    };

    private string DescribeConnection() => SelectedConnection == ConnectionKind.WiFi
        ? $"Wi-Fi ({Target})"
        : $"{SelectedConnection} ({Target} @ {BaudRate} baud)";

    private static void AppendCodes(StringBuilder sb, string title, IReadOnlyList<DiagnosticTroubleCode> codes)
    {
        sb.AppendLine($"{title}:");
        if (codes.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (DiagnosticTroubleCode code in codes)
            {
                sb.AppendLine($"  {code.Code}");
            }
        }

        sb.AppendLine();
    }
}
