namespace ObdFree.Core.Protocol;

/// <summary>
/// Extracts the parameter data bytes from a positive Mode 01 / Mode 09 response.
/// </summary>
public static class ObdPayload
{
    /// <summary>
    /// Finds the data bytes that follow the positive response for a given mode
    /// and PID. A positive response echoes <c>mode + 0x40</c> then the PID.
    /// </summary>
    /// <param name="data">The combined response bytes.</param>
    /// <param name="mode">The request mode (e.g. <c>0x01</c>).</param>
    /// <param name="pid">The requested PID.</param>
    /// <param name="payload">The data bytes following the echoed PID, if found.</param>
    /// <returns><see langword="true"/> if a matching positive response was located.</returns>
    public static bool TryGetParameter(ReadOnlySpan<byte> data, byte mode, byte pid, out byte[] payload)
    {
        byte responseByte = (byte)(mode + 0x40);

        for (int i = 0; i + 1 < data.Length; i++)
        {
            if (data[i] == responseByte && data[i + 1] == pid)
            {
                payload = data[(i + 2)..].ToArray();
                return true;
            }
        }

        payload = [];
        return false;
    }
}
