using ObdFree.Core.Transport;

namespace ObdFree.Core.Tests.Transport;

/// <summary>
/// In-memory <see cref="IObdTransport"/> used by tests and session replay.
/// Maps commands to canned responses so protocol logic can be exercised
/// without any physical adapter.
/// </summary>
public sealed class FakeObdTransport(IReadOnlyDictionary<string, string> responses) : IObdTransport
{
    private readonly IReadOnlyDictionary<string, string> _responses = responses;

    public bool IsOpen { get; private set; }

    /// <summary>Commands received, in order, for assertion in tests.</summary>
    public List<string> SentCommands { get; } = [];

    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        IsOpen = true;
        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        IsOpen = false;
        return Task.CompletedTask;
    }

    public Task<string> SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("Transport is not open.");
        }

        SentCommands.Add(command);
        return Task.FromResult(_responses.TryGetValue(command, out string? response) ? response : "NO DATA");
    }

    public ValueTask DisposeAsync()
    {
        IsOpen = false;
        return ValueTask.CompletedTask;
    }
}
