namespace ObdFree.Core.Adapters;

/// <summary>
/// Tunes how the ELM327 initialization sequence is sent, to cope with cheap
/// clones and finicky bridges (including ELM327-compatible Launch Wi-Fi/BT
/// units) that drop fast commands or need extra time after a reset.
/// </summary>
/// <param name="Key">Stable lowercase key used on the CLI, e.g. <c>launch</c>.</param>
/// <param name="DisplayName">Human-readable name.</param>
/// <param name="ResetCommand">Reset command: <c>ATZ</c> (full) or <c>ATWS</c> (warm start).</param>
/// <param name="ResetDelayMs">Delay after the reset command before the next command.</param>
/// <param name="InterCommandDelayMs">Delay inserted between subsequent commands.</param>
/// <param name="Notes">Short guidance shown to the user.</param>
public sealed record AdapterProfile(
    string Key,
    string DisplayName,
    string ResetCommand,
    int ResetDelayMs,
    int InterCommandDelayMs,
    string Notes);
