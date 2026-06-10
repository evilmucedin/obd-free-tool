namespace ObdFree.Core.Vehicles;

/// <summary>
/// A vehicle profile tunes how the tool talks to a particular make. The most
/// important knob is the preferred <see cref="ObdProtocol"/>: picking the right
/// protocol up front connects faster and more reliably than auto-detection.
/// </summary>
/// <param name="Key">Stable lowercase key used on the CLI, e.g. <c>toyota</c>.</param>
/// <param name="DisplayName">Human-readable name, e.g. <c>Toyota / Lexus</c>.</param>
/// <param name="PreferredProtocol">Protocol to request via <c>ATSP</c> on connect.</param>
/// <param name="Notes">Short guidance shown to the user.</param>
public sealed record VehicleProfile(
    string Key,
    string DisplayName,
    ObdProtocol PreferredProtocol,
    string Notes);
