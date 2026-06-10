using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObdFree.Core;
using ObdFree.Core.Adapters;
using ObdFree.Core.Config;
using ObdFree.Core.Diagnostics;
using ObdFree.Core.Modes;
using ObdFree.Core.Readiness;
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
    private readonly ConfigStore _configStore;
    private readonly bool _loaded;

    /// <summary>Creates the view model using the default per-user config store.</summary>
    public MainWindowViewModel()
        : this(new ConfigStore())
    {
    }

    /// <summary>Creates the view model with a specific config store (used by tests).</summary>
    /// <param name="configStore">The config store that persists all settings.</param>
    public MainWindowViewModel(ConfigStore configStore)
    {
        _configStore = configStore;

        // Restore previously saved settings. Assign backing fields directly so we
        // don't trigger the change handlers (which would re-save and reset Target).
        AppConfig config = configStore.Load();
        _selectedMode = config.Mode;
        _selectedConnection = config.ConnectionKind;
        _target = config.Target;
        _baudRate = config.BaudRate;
        _selectedProfile = VehicleProfiles.TryGet(config.VehicleProfileKey, out VehicleProfile? v) && v is not null
            ? v
            : VehicleProfiles.Generic;
        _selectedAdapter = AdapterProfiles.TryGet(config.AdapterProfileKey, out AdapterProfile? a) && a is not null
            ? a
            : AdapterProfiles.Standard;

        _loaded = true;
    }

    /// <summary>Persists the current settings to disk (no-op until initial load completes).</summary>
    private void SaveSettings()
    {
        if (!_loaded)
        {
            return;
        }

        _configStore.Save(new AppConfig
        {
            Mode = SelectedMode,
            ConnectionKind = SelectedConnection,
            Target = Target,
            BaudRate = BaudRate,
            VehicleProfileKey = SelectedProfile.Key,
            AdapterProfileKey = SelectedAdapter.Key,
        });
    }

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
    [NotifyCanExecuteChangedFor(nameof(StatusCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReadinessCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReadVinCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReadCodesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearCodesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReadSrsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearSrsCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsProfessional))]
    [NotifyCanExecuteChangedFor(nameof(ClearCodesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReadSrsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearSrsCommand))]
    private OperatingMode _selectedMode;

    [ObservableProperty]
    private KnownAdapter? _selectedDongle;

    /// <summary>The operating modes offered in the UI.</summary>
    public IReadOnlyList<OperatingMode> Modes { get; } = [OperatingMode.Safe, OperatingMode.Professional];

    /// <summary>True when professional mode is selected (dangerous features unlocked).</summary>
    public bool IsProfessional => SelectedMode == OperatingMode.Professional;

    /// <summary>Persists the chosen mode so it sticks across launches.</summary>
    partial void OnSelectedModeChanged(OperatingMode value)
    {
        SaveSettings();

        OnPropertyChanged(nameof(CanClearDtc));
        OnPropertyChanged(nameof(CanUseSrs));
        OnPropertyChanged(nameof(CanClearSrs));
    }

    partial void OnTargetChanged(string value) => SaveSettings();

    partial void OnBaudRateChanged(int value) => SaveSettings();

    partial void OnSelectedProfileChanged(VehicleProfile value) => SaveSettings();

    partial void OnSelectedAdapterChanged(AdapterProfile value) => SaveSettings();

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRun));
        OnPropertyChanged(nameof(CanClearDtc));
        OnPropertyChanged(nameof(CanUseSrs));
        OnPropertyChanged(nameof(CanClearSrs));
    }

    /// <summary>The connection kinds offered in the UI.</summary>
    public IReadOnlyList<ConnectionKind> ConnectionKinds { get; } =
        [ConnectionKind.Usb, ConnectionKind.WiFi, ConnectionKind.Bluetooth];

    /// <summary>The vehicle profiles offered in the UI.</summary>
    public IReadOnlyList<VehicleProfile> Profiles { get; } = [.. VehicleProfiles.All];

    /// <summary>The adapter profiles offered in the UI.</summary>
    public IReadOnlyList<AdapterProfile> Adapters { get; } = [.. AdapterProfiles.All];

    /// <summary>Known dongles the user can pick to auto-fill settings (supported ones only).</summary>
    public IReadOnlyList<KnownAdapter> KnownDongles { get; } = [.. KnownAdapters.Supported];

    /// <summary>Applies a picked dongle's connection kind, target, baud, and adapter profile.</summary>
    partial void OnSelectedDongleChanged(KnownAdapter? value)
    {
        if (value is null)
        {
            return;
        }

        SelectedAdapter = value.AdapterProfile;
        BaudRate = value.DefaultBaud > 0 ? value.DefaultBaud : BaudRate;
        SelectedConnection = value.Link switch
        {
            DongleLink.WiFi => ConnectionKind.WiFi,
            DongleLink.BluetoothClassic => ConnectionKind.Bluetooth,
            _ => ConnectionKind.Usb,
        };

        // OnSelectedConnectionChanged set a sensible default target; for Wi-Fi
        // dongles use their known endpoint.
        if (value.Link == DongleLink.WiFi && value.DefaultEndpoint is { } endpoint)
        {
            Target = endpoint;
        }
    }

    /// <summary>True when a safe action can run (not already busy).</summary>
    public bool CanRun => !IsBusy;

    /// <summary>True when clearing DTCs is allowed (professional mode, not busy).</summary>
    public bool CanClearDtc => !IsBusy && IsProfessional;

    /// <summary>True when SRS access is allowed (professional mode, not busy).</summary>
    public bool CanUseSrs => !IsBusy && IsProfessional;

    partial void OnSelectedConnectionChanged(ConnectionKind value)
    {
        Target = value switch
        {
            ConnectionKind.WiFi => $"{TcpObdTransport.DefaultHost}:{TcpObdTransport.DefaultPort}",
            ConnectionKind.Bluetooth => "/dev/rfcomm0",
            _ => "/dev/ttyUSB0",
        };

        SaveSettings();
    }

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

    /// <summary>Reads stored, pending, and permanent trouble codes.</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task ReadCodesAsync() => RunAsync(async session =>
    {
        await session.ConnectAsync();
        IReadOnlyList<DiagnosticTroubleCode> stored = await session.ReadStoredCodesAsync();
        IReadOnlyList<DiagnosticTroubleCode> pending = await session.ReadPendingCodesAsync();
        IReadOnlyList<DiagnosticTroubleCode> permanent = await session.ReadPermanentCodesAsync();

        var sb = new StringBuilder();
        AppendCodes(sb, "Stored codes (Mode 03)", stored);
        AppendCodes(sb, "Pending codes (Mode 07)", pending);
        AppendCodes(sb, "Permanent codes (Mode 0A, cannot be cleared)", permanent);
        return sb.ToString();
    });

    /// <summary>Reads emissions readiness / MIL (US smog check).</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task ReadinessAsync() => RunAsync(async session =>
    {
        MonitorStatus? status = await session.ReadReadinessAsync();
        if (status is null)
        {
            return "Readiness unavailable (no response to Mode 01 PID 01).";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"MIL (check engine light): {(status.MilOn ? "ON" : "off")}");
        sb.AppendLine($"Confirmed DTCs          : {status.DtcCount}");
        sb.AppendLine();
        sb.AppendLine("Emissions monitors (I/M readiness):");
        foreach (MonitorReadiness monitor in status.Monitors)
        {
            sb.AppendLine($"  {monitor.Name,-26} {monitor.StatusText}");
        }

        sb.AppendLine();
        sb.AppendLine(status.LikelyReadyForInspection
            ? $"=> Likely READY for a US emissions/smog check ({status.NotReadyCount} not ready, MIL off)."
            : $"=> Likely NOT ready: MIL {(status.MilOn ? "ON" : "off")}, {status.NotReadyCount} monitor(s) not ready.");
        sb.AppendLine("   (Guidance only — pass rules vary by state.)");
        return sb.ToString();
    });

    /// <summary>Reads the Vehicle Identification Number (VIN).</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task ReadVinAsync() => RunAsync(async session =>
    {
        string? vin = await session.ReadVinAsync();
        return vin is null ? "VIN unavailable (no response to Mode 09 PID 02)." : $"VIN: {vin}";
    });

    /// <summary>Clears stored trouble codes from memory (Mode 04) — professional only.</summary>
    [RelayCommand(CanExecute = nameof(CanClearDtc))]
    private Task ClearCodesAsync() => RunAsync(async session =>
    {
        await session.ConnectAsync();
        bool ok = await session.ClearCodesAsync();
        return ok
            ? "Trouble codes cleared. The MIL ('check engine' light) should turn off."
            : "Clear was not acknowledged by the ECU.";
    });

    /// <summary>Reads SRS/airbag status and codes (Toyota/Lexus, UDS over CAN) — professional only.</summary>
    [RelayCommand(CanExecute = nameof(CanUseSrs))]
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
    [NotifyCanExecuteChangedFor(nameof(ClearSrsCommand))]
    private bool _srsClearConfirmed;

    /// <summary>True when SRS clearing is allowed (professional, acknowledged, not busy).</summary>
    public bool CanClearSrs => !IsBusy && IsProfessional && SrsClearConfirmed;

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
            await using var session = new ObdSession(
                connection.CreateTransport(), SelectedProfile, SelectedAdapter, SelectedMode);
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
