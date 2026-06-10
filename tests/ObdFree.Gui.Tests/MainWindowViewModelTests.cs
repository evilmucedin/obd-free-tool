using System.ComponentModel;
using ObdFree.Core.Transport;
using ObdFree.Core.Vehicles;
using ObdFree.Gui.ViewModels;

namespace ObdFree.Gui.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void Defaults_AreUsbGenericAndReady()
    {
        var vm = new MainWindowViewModel();

        Assert.Equal(ConnectionKind.Usb, vm.SelectedConnection);
        Assert.Equal("/dev/ttyUSB0", vm.Target);
        Assert.Equal(VehicleProfiles.Generic, vm.SelectedProfile);
        Assert.True(vm.CanRun);
    }

    [Fact]
    public void Lists_ExposeConnectionKindsProfilesAndAdapters()
    {
        var vm = new MainWindowViewModel();

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
        var vm = new MainWindowViewModel
        {
            SelectedConnection = kind,
        };

        Assert.Equal(expectedTarget, vm.Target);
    }

    [Fact]
    public void IsBusy_RaisesCanRunChange()
    {
        var vm = new MainWindowViewModel();
        var changed = new List<string?>();
        ((INotifyPropertyChanged)vm).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.IsBusy = true;

        Assert.False(vm.CanRun);
        Assert.Contains(nameof(MainWindowViewModel.CanRun), changed);
    }

    [Fact]
    public void Commands_AreWiredUp()
    {
        var vm = new MainWindowViewModel();

        Assert.NotNull(vm.StatusCommand);
        Assert.NotNull(vm.ReadCodesCommand);
        Assert.NotNull(vm.ClearCodesCommand);
        Assert.NotNull(vm.ReadSrsCommand);
        Assert.NotNull(vm.ClearSrsCommand);
        Assert.NotNull(vm.ReadinessCommand);
        Assert.NotNull(vm.ReadVinCommand);
    }

    [Fact]
    public void ClearSrs_IsGatedBehindConfirmation()
    {
        var vm = new MainWindowViewModel();

        Assert.False(vm.CanClearSrs);                 // not confirmed yet
        Assert.False(vm.ClearSrsCommand.CanExecute(null));

        vm.SrsClearConfirmed = true;

        Assert.True(vm.CanClearSrs);
        Assert.True(vm.ClearSrsCommand.CanExecute(null));
    }
}
