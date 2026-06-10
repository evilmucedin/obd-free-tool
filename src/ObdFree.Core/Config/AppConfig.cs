using ObdFree.Core.Modes;

namespace ObdFree.Core.Config;

/// <summary>User-configurable, persisted application settings.</summary>
public sealed class AppConfig
{
    /// <summary>Gets or sets the default operating mode (defaults to Safe).</summary>
    public OperatingMode Mode { get; set; } = OperatingMode.Safe;
}
