using LlmTornado.Cli;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class ConsoleRendererTests
{
    #region Static Methods — Smoke Tests

    [Test]
    public void WriteBanner_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => ConsoleRenderer.WriteBanner());
    }

    [Test]
    public void WritePrompt_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => ConsoleRenderer.WritePrompt("gpt-4.1-nano"));
    }

    [Test]
    public void WriteStreamingToken_Null_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => ConsoleRenderer.WriteStreamingToken(null));
    }

    [Test]
    public void WriteStreamingToken_Text_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            ConsoleRenderer.WriteStreamingToken("hello ");
            ConsoleRenderer.WriteStreamingToken("world");
            ConsoleRenderer.EndStreamingResponse();
        });
    }

    [Test]
    public void WriteReasoningToken_Text_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            ConsoleRenderer.WriteReasoningToken("thinking...");
            ConsoleRenderer.WriteStreamingToken("answer");
            ConsoleRenderer.EndStreamingResponse();
        });
    }

    [Test]
    public void WriteToolCallArgumentDelta_Text_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            ConsoleRenderer.WriteToolCallArgumentDelta("write_file", "{\"content\":\"hello");
            ConsoleRenderer.WriteToolCallArgumentDelta("write_file", " world\"}");
            ConsoleRenderer.EndStreamingResponse();
        });
    }

    [Test]
    public void EndStreamingResponse_NoStream_DoesNotThrow()
    {
        // Should be safe to call even when not streaming
        Assert.DoesNotThrow(() => ConsoleRenderer.EndStreamingResponse());
    }

    [Test]
    public void WriteInfo_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => ConsoleRenderer.WriteInfo("Info message"));
    }

    [Test]
    public void WriteError_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => ConsoleRenderer.WriteError("Error message"));
    }

    [Test]
    public void WriteSuccess_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => ConsoleRenderer.WriteSuccess("Success!"));
    }

    #endregion

    #region Instance Methods — Smoke Tests

    [Test]
    public void WriteToolAutoApproved_DoesNotThrow()
    {
        ConsoleRenderer renderer = new();
        Assert.DoesNotThrow(() => renderer.WriteToolAutoApproved("my-tool"));
    }

    [Test]
    public void WriteToolAutoDenied_DoesNotThrow()
    {
        ConsoleRenderer renderer = new();
        Assert.DoesNotThrow(() => renderer.WriteToolAutoDenied("my-tool"));
    }

    [Test]
    public void WriteToolApprovalPrompt_DoesNotThrow()
    {
        ConsoleRenderer renderer = new();
        Assert.DoesNotThrow(() => renderer.WriteToolApprovalPrompt("Tool: test\nArguments: {}"));
    }

    [Test]
    public void WriteToolApprovalPrompt_Wraps_Long_Argument_Line()
    {
        TextWriter originalOut = Console.Out;
        using StringWriter writer = new();
        try
        {
            Console.SetOut(writer);
            ConsoleRenderer renderer = new();
            string argumentTail = "TAIL_SHOULD_STILL_BE_VISIBLE";
            renderer.WriteToolApprovalPrompt("Tool: write_file\nArguments: " + new string('a', 180) + argumentTail);

            Assert.That(writer.ToString(), Does.Contain(argumentTail));
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public void ToolApprovalContentWidth_Leaves_Spare_Console_Column()
    {
        foreach (int consoleWidth in new[] { 12, 20, 80, 160 })
        {
            int contentWidth = ConsoleRenderer.ToolApprovalContentWidth(consoleWidth);

            Assert.That(contentWidth + 4, Is.LessThan(consoleWidth));
        }
    }

    #endregion

    #region Thread Safety

    [Test]
    public void ConcurrentWrites_DoNotThrow()
    {
        // Multiple threads writing simultaneously should not deadlock or throw
        Task[] tasks = new Task[10];
        for (int i = 0; i < tasks.Length; i++)
        {
            int idx = i;
            tasks[i] = Task.Run(() =>
            {
                ConsoleRenderer.WriteInfo($"Thread {idx}");
                ConsoleRenderer.WriteStreamingToken($"token-{idx}");
            });
        }

        Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
        ConsoleRenderer.EndStreamingResponse();
    }

    #endregion
}
