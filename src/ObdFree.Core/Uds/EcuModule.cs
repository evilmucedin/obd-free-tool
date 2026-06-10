namespace ObdFree.Core.Uds;

/// <summary>
/// Identifies a non-OBD ECU reached over UDS (ISO 14229) on CAN, such as the
/// SRS/airbag or ABS controller. Addressing is done with 11-bit CAN headers:
/// requests are sent to <see cref="RequestHeader"/> and the ECU replies on
/// <see cref="ResponseHeader"/>.
/// </summary>
/// <param name="Key">Stable lowercase key, e.g. <c>srs</c>.</param>
/// <param name="Name">Human-readable name, e.g. <c>SRS / Airbag</c>.</param>
/// <param name="RequestHeader">11-bit CAN request ID in hex, e.g. <c>7B0</c>.</param>
/// <param name="ResponseHeader">11-bit CAN response ID in hex, e.g. <c>7B8</c>.</param>
public sealed record EcuModule(string Key, string Name, string RequestHeader, string ResponseHeader)
{
    /// <summary>
    /// Returns a copy of this module with overridden headers, ignoring blank
    /// overrides. Used to apply CLI <c>--srs-tx</c>/<c>--srs-rx</c> flags.
    /// </summary>
    /// <param name="requestHeader">New request header, or null/blank to keep.</param>
    /// <param name="responseHeader">New response header, or null/blank to keep.</param>
    /// <returns>The (possibly) updated module.</returns>
    public EcuModule WithHeaders(string? requestHeader, string? responseHeader) => this with
    {
        RequestHeader = string.IsNullOrWhiteSpace(requestHeader) ? RequestHeader : requestHeader.Trim(),
        ResponseHeader = string.IsNullOrWhiteSpace(responseHeader) ? ResponseHeader : responseHeader.Trim(),
    };
}
