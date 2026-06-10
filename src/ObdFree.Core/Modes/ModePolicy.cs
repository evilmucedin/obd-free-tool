namespace ObdFree.Core.Modes;

/// <summary>
/// The single source of truth for what each <see cref="OperatingMode"/> permits.
/// Both the CLI and GUI consult this, and <c>ObdSession</c> enforces it, so a
/// feature can never run in a mode that disallows it.
/// </summary>
public static class ModePolicy
{
    /// <summary>Features allowed in <see cref="OperatingMode.Safe"/>. Everything is allowed in Professional.</summary>
    private static readonly HashSet<AppFeature> SafeFeatures =
    [
        AppFeature.ReadStatus,
        AppFeature.ReadReadiness,
        AppFeature.ReadVin,
        AppFeature.ReadDtc,
    ];

    /// <summary>Returns whether a feature is allowed in the given mode.</summary>
    /// <param name="mode">The current operating mode.</param>
    /// <param name="feature">The feature being attempted.</param>
    /// <returns><see langword="true"/> if allowed.</returns>
    public static bool IsAllowed(OperatingMode mode, AppFeature feature)
        => mode == OperatingMode.Professional || SafeFeatures.Contains(feature);

    /// <summary>Returns the minimum mode required to use a feature.</summary>
    /// <param name="feature">The feature.</param>
    /// <returns>The minimum <see cref="OperatingMode"/>.</returns>
    public static OperatingMode RequiredMode(AppFeature feature)
        => SafeFeatures.Contains(feature) ? OperatingMode.Safe : OperatingMode.Professional;

    /// <summary>Returns whether a feature needs professional mode (i.e. is gated/dangerous).</summary>
    /// <param name="feature">The feature.</param>
    /// <returns><see langword="true"/> if professional mode is required.</returns>
    public static bool RequiresProfessional(AppFeature feature) => !SafeFeatures.Contains(feature);
}

/// <summary>Thrown when a feature is attempted in a mode that does not allow it.</summary>
public sealed class FeatureNotAllowedInModeException : InvalidOperationException
{
    /// <summary>Initializes the exception for a feature/mode combination.</summary>
    /// <param name="feature">The disallowed feature.</param>
    /// <param name="currentMode">The current mode.</param>
    public FeatureNotAllowedInModeException(AppFeature feature, OperatingMode currentMode)
        : base($"'{feature}' requires {ModePolicy.RequiredMode(feature)} mode (current mode: {currentMode}).")
    {
        Feature = feature;
        CurrentMode = currentMode;
    }

    /// <summary>Gets the feature that was disallowed.</summary>
    public AppFeature Feature { get; }

    /// <summary>Gets the mode that was active when the feature was attempted.</summary>
    public OperatingMode CurrentMode { get; }
}
