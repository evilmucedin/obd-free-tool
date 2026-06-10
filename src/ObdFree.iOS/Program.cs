using Avalonia;
using Avalonia.iOS;
using Foundation;
using UIKit;

namespace ObdFree.iOS;

/// <summary>
/// iOS application delegate. Hosts the shared Avalonia <c>App</c> from
/// <c>ObdFree.App</c>, so the iOS app uses the same UI and the same
/// <c>ObdFree.Core</c> engine as the desktop build.
/// </summary>
[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<global::ObdFree.App.App>
{
    /// <inheritdoc />
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder).WithInterFont();
}

/// <summary>iOS entry point.</summary>
public static class Application
{
    private static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}
