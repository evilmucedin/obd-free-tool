using System.Text;

namespace ObdFree.Core.Protocol;

/// <summary>
/// Low-level helpers for the ELM327 line protocol over an arbitrary
/// <see cref="Stream"/>: commands terminate with a carriage return and the
/// adapter signals readiness with a <c>'&gt;'</c> prompt character.
/// </summary>
public static class ElmIo
{
    /// <summary>The character the adapter emits when it is ready for input.</summary>
    public const char Prompt = '>';

    /// <summary>Writes a command followed by a carriage return.</summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="command">The command text (without the carriage return).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async Task WriteCommandAsync(Stream stream, string command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(command);

        byte[] bytes = Encoding.ASCII.GetBytes(command + "\r");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads from the stream until the <c>'&gt;'</c> prompt is seen, returning the
    /// text received before it. Throws on end-of-stream before a prompt.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response text with the trailing prompt removed.</returns>
    public static async Task<string> ReadUntilPromptAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var builder = new StringBuilder();
        byte[] buffer = new byte[256];

        while (true)
        {
            int read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                if (builder.Length == 0)
                {
                    throw new EndOfStreamException("Adapter closed the connection before responding.");
                }

                break;
            }

            for (int i = 0; i < read; i++)
            {
                char c = (char)buffer[i];
                if (c == Prompt)
                {
                    return builder.ToString();
                }

                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
