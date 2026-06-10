namespace ObdFree.Core.Uds;

/// <summary>
/// Default UDS-on-CAN module addresses for Toyota / Lexus.
///
/// <para><b>Important:</b> these defaults are a documented starting point, not a
/// guarantee. Toyota/Lexus airbag (SRS) CAN addresses vary by model year and
/// platform and are not publicly standardized. If the SRS module does not
/// respond, override the request/response headers (CLI <c>--srs-tx</c> /
/// <c>--srs-rx</c>) with the values for your specific vehicle. These should be
/// validated on the actual car.</para>
/// </summary>
public static class ToyotaModules
{
    /// <summary>
    /// SRS / airbag controller. Default headers (<c>7B0</c> request, <c>7B8</c>
    /// response) are a common Toyota body/SRS pairing; verify per vehicle.
    /// </summary>
    public static EcuModule Srs { get; } = new("srs", "SRS / Airbag", "7B0", "7B8");
}
