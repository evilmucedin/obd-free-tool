using System.Text;
using ObdFree.Core.Protocol;

namespace ObdFree.Core.Tests.Protocol;

public class ElmIoTests
{
    [Fact]
    public async Task ReadUntilPrompt_StopsAtPromptAndExcludesIt()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("410C0CE4\r\r>extra"));

        string result = await ElmIo.ReadUntilPromptAsync(stream);

        Assert.Equal("410C0CE4\r\r", result);
    }

    [Fact]
    public async Task WriteCommand_AppendsCarriageReturn()
    {
        using var stream = new MemoryStream();

        await ElmIo.WriteCommandAsync(stream, "010C");

        Assert.Equal("010C\r", Encoding.ASCII.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task ReadUntilPrompt_NoPromptBeforeEof_Throws()
    {
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<EndOfStreamException>(() => ElmIo.ReadUntilPromptAsync(stream));
    }
}
