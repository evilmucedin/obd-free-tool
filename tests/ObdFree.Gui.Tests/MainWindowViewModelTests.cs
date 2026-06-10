using System.ComponentModel;
using ObdFree.Core.Adapters;
using ObdFree.Core.Config;
using ObdFree.Core.Modes;
using ObdFree.Core.Transport;
using ObdFree.Core.Vehicles;
using ObdFree.Gui.ViewModels;

namespace ObdFree.Gui.Tests;

public class MainWindowViewModelTests
{
    // A view model backed by a throwaway config file so tests never touch the
    // real per-user config.
    private static MainWindowViewModel CreateVm(OperatingMode mode = OperatingMode.Safe)
    {
        string path = Path.Combine(Path.GetTempPath(), $"obd-gui-cfg-{Guid.NewGuid():N}.json");
        var store = new ConfigStore(path);
        store.Save(new AppConfig { Mode = mode });
        return new MainWindowViewModel(store);
    }

    [Fact]
    public void SafeMode_GatesDangerousCommands()
    {
        var vm = CreateVm(OperatingMode.Safe);

        Assert.False(vm.IsProfessional);
        Assert.True(vm.StatusCommand.CanExecute(null));        // safe op allowed
        Assert.False(vm.ClearCodesCommand.CanExecute(null));   // write blocked
        Assert.False(vm.ReadSrsCommand.CanExecute(null));      // SRS blocked
    }

    [Fact]
    public void ProfessionalMode_UnlocksDangerousCommands()
    {
        var vm = CreateVm(OperatingMode.Professional);

        Assert.True(vm.IsProfessional);
        Assert.True(vm.ClearCodesCommand.CanExecute(null));
        Assert.True(vm.ReadSrsCommand.CanExecute(null));
    }

    [Fact]
    public void SwitchingToProfessional_PersistsToConfig()
    {
        string path = Path.Combine(Path.GetTempPath(), $"obd-gui-cfg-{Guid.NewGuid():N}.json");
        var vm = new MainWindowViewModel(new ConfigStore(path));

        vm.SelectedMode = OperatingMode.Professional;

        Assert.Equal(OperatingMode.Professional, new ConfigStore(path).Load().Mode);
        File.Delete(path);
    }

    [Fact]
    public void ChangingSettings_PersistsThem()
    {
        string path = Path.Combine(Path.GetTempPath(), $"obd-gui-cfg-{Guid.NewGuid():N}.json");
        var vm = new MainWindowViewModel(new ConfigStore(path));

        vm.SelectedConnection = ConnectionKind.WiFi;
        vm.Target = "10.0.0.5:35000";
        vm.BaudRate = 115200;
        vm.SelectedProfile = VehicleProfiles.Toyota;
        vm.SelectedAdapter = AdapterProfiles.Launch;

        AppConfig saved = new ConfigStore(path).Load();
        Assert.Equal(ConnectionKind.WiFi, saved.ConnectionKind);
        Assert.Equal("10.0.0.5:35000", saved.Target);
        Assert.Equal(115200, saved.BaudRate);
        Assert.Equal("toyota", saved.VehicleProfileKey);
        Assert.Equal("launch", saved.AdapterProfileKey);
        File.Delete(path);
    }

    [Fact]
    public void Restart_RestoresSavedSettings()
    {
        string path = Path.Combine(Path.GetTempPath(), $"obd-gui-cfg-{Guid.NewGuid():N}.json");
        new ConfigStore(path).Save(new AppConfig
        {
            Mode = OperatingMode.Professional,
            ConnectionKind = ConnectionKind.Bluetooth,
            Target = "/dev/rfcomm9",
            BaudRate = 115200,
            VehicleProfileKey = "lexus",
            AdapterProfileKey = "launch",
        });

        // Simulate a fresh launch with the same config file.
        var vm = new MainWindowViewModel(new ConfigStore(path));

        Assert.Equal(OperatingMode.Professional, vm.SelectedMode);
        Assert.Equal(ConnectionKind.Bluetooth, vm.SelectedConnection);
        Assert.Equal("/dev/rfcomm9", vm.Target);
        Assert.Equal(115200, vm.BaudRate);
        Assert.Equal("lexus", vm.SelectedProfile.Key);
        Assert.Equal("launch", vm.SelectedAdapter.Key);
        File.Delete(path);
    }

    [Fact]
    public void Defaults_AreUsbGenericAndReady()
    {
        // Fresh temp store with no file -> loads built-in defaults.
        string path = Path.Combine(Path.GetTempPath(), $"obd-gui-cfg-{Guid.NewGuid():N}.json");
        var vm = new MainWindowViewModel(new ConfigStore(path));

        Assert.Equal(ConnectionKind.Usb, vm.SelectedConnection);
        Assert.Equal("/dev/ttyUSB0", vm.Target);
        Assert.Equal(VehicleProfiles.Generic, vm.SelectedProfile);
        Assert.True(vm.CanRun);
    }

    [Fact]
    public void Lists_ExposeConnectionKindsProfilesAndAdapters()
    {
        var vm = CreateVm();

        Assert.Contains(ConnectionKind.WiFi, vm.ConnectionKinds);
        Assert.Contains(vm.Profiles, p => p.Key == "toyota");
        Assert.Contains(vm.Profiles, p => p.Key == "lexus");
        Assert.Contains(vm.Adapters, a => a.Key == "standard");
        Assert.Contains(vm.Adapters, a => a.Key == "launch");
        Assert.Equal("standard", vm.SelectedAdapter.Key);
    }

    [Theory]
    [InlineData(ConnectionKind.WiFi, "192.168.0.10:35000")]
    [InlineData(ConnectionKind.Bluetooth, "/dev/rfcomm0")]
    [InlineData(ConnectionKind.Usb, "/dev/ttyUSB0")]
    public void ChangingConnection_UpdatesDefaultTarget(ConnectionKind kind, string expectedTarget)
    {
        var vm = CreateVm();
        vm.SelectedConnection = kind;

        Assert.Equal(expectedTarget, vm.Target);
    }

    [Fact]
    public void IsBusy_RaisesCanRunChange()
    {
        var vm = CreateVm();
        var changed = new List<string?>();
        ((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.IsBusy = true;

        Assert.False(vm.CanRun);
        Assert.Contains(nameof(MainWindowViewModel.CanRun), changed);
    }

    [Fact]
    public void Commands_AreWiredUp()
    {
        var vm = CreateVm();

        Assert.NotNull(vm.StatusCommand);
        Assert.NotNull(vm.ReadCodesCommand);
        Assert.NotNull(vm.ClearCodesCommand);
        Assert.NotNull(vm.ReadSrsCommand);
        Assert.NotNull(vm.ClearSrsCommand);
        Assert.NotNull(vm.ReadinessCommand);
        Assert.NotNull(vm.ReadVinCommand);
    }

    [Fact]
    public void KnownDongles_ExcludeBleAndArePickable()
    {
        var vm = CreateVm();

        Assert.NotEmpty(vm.KnownDongles);
        Assert.All(vm.KnownDongles, d => Assert.NotEqual(DongleLink.BluetoothLe, d.Link));
    }

    [Fact]
    public void SelectingWiFiDongle_FillsConnectionSettings()
    {
        var vm = CreateVm();
        KnownAdapter veepeakWifi = vm.KnownDongles.First(d => d.Key == "veepeak-wifi");

        vm.SelectedDongle = veepeakWifi;

        Assert.Equal(ConnectionKind.WiFi, vm.SelectedConnection);
        Assert.Equal("192.168.0.10:35000", vm.Target);
    }

    [Fact]
    public void SelectingSerialDongle_SetsBaudAndAdapterProfile()
    {
        var vm = CreateVm();
        KnownAdapter obdlinkLx = vm.KnownDongles.First(d => d.Key == "obdlink-lx");

        vm.SelectedDongle = obdlinkLx;

        Assert.Equal(ConnectionKind.Bluetooth, vm.SelectedConnection);
        Assert.Equal(115200, vm.BaudRate);
        Assert.Equal(AdapterProfiles.Standard, vm.SelectedAdapter);
    }

    [Fact]
    public void ClearSrs_RequiresProfessionalAndConfirmation()
    {
        var vm = CreateVm(OperatingMode.Professional);

        Assert.False(vm.CanClearSrs);                 // not confirmed yet
        Assert.False(vm.ClearSrsCommand.CanExecute(null));

        vm.SrsClearConfirmed = true;

        Assert.True(vm.CanClearSrs);
        Assert.True(vm.ClearSrsCommand.CanExecute(null));
    }

    [Fact]
    public void ClearSrs_BlockedInSafeModeEvenIfConfirmed()
    {
        var vm = CreateVm(OperatingMode.Safe);
        vm.SrsClearConfirmed = true;

        Assert.False(vm.CanClearSrs);
        Assert.False(vm.ClearSrsCommand.CanExecute(null));
    }
}
