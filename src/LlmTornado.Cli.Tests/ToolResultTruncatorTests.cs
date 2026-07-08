using LlmTornado.ChatFunctions;
using LlmTornado.Cli.Core.Tools;
using LlmTornado.Common;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class ToolResultTruncatorTests
{
    private static FunctionResult Result(string content) =>
        new("tool", content, FunctionResultSetContentModes.Passthrough);

    [Test]
    public void UnderLimit_IsUntouched()
    {
        string content = new('x', 100);
        Assert.That(ToolResultTruncator.Truncate(content, maxTokens: 100), Is.SameAs(content));
    }

    [Test]
    public void OverLimit_KeepsHeadAndTail_WithMarker()
    {
        // 4000 chars, cap at 100 tokens = 400 chars.
        string content = string.Concat(Enumerable.Range(0, 400).Select(i => $"L{i:D3}______")); // 10 chars each
        string truncated = ToolResultTruncator.Truncate(content, maxTokens: 100);

        Assert.That(truncated.Length, Is.LessThan(content.Length));
        Assert.That(truncated, Does.StartWith("L000______"));       // head kept
        Assert.That(truncated, Does.EndWith("L399______"));         // tail kept
        Assert.That(truncated, Does.Contain("characters truncated"));
        Assert.That(truncated, Does.Contain("narrower scope"));
    }

    [Test]
    public void HeadIsLargerThanTail()
    {
        string content = new('a', 10_000);
        string truncated = ToolResultTruncator.Truncate(content, maxTokens: 250); // 1000 chars kept

        int markerStart = truncated.IndexOf("\n[...", StringComparison.Ordinal);
        int markerEnd = truncated.IndexOf("...]\n", StringComparison.Ordinal) + 5;
        int head = markerStart;
        int tail = truncated.Length - markerEnd;

        Assert.That(head, Is.EqualTo(700));
        Assert.That(tail, Is.EqualTo(300));
    }

    [Test]
    public void SurrogatePairs_AreNeverSplit()
    {
        // Build content out of 4-byte emoji so any misaligned cut would split a pair.
        string content = string.Concat(Enumerable.Repeat("😀", 4000));
        string truncated = ToolResultTruncator.Truncate(content, maxTokens: 100);

        for (int i = 0; i < truncated.Length; i++)
        {
            if (char.IsHighSurrogate(truncated[i]))
            {
                Assert.That(i + 1, Is.LessThan(truncated.Length), "dangling high surrogate at end");
                Assert.That(char.IsLowSurrogate(truncated[i + 1]), Is.True, $"broken pair at {i}");
                i++;
            }
            else
            {
                Assert.That(char.IsLowSurrogate(truncated[i]), Is.False, $"orphan low surrogate at {i}");
            }
        }
    }

    [Test]
    public async Task Process_MutatesOversizedResult()
    {
        ToolResultTruncator truncator = new(() => 10);
        FunctionResult result = Result(new string('x', 1000));

        await truncator.Process("read_file", result, new FunctionCall { Name = "read_file" });

        Assert.That(result.Content.Length, Is.LessThan(1000));
        Assert.That(result.Content, Does.Contain("characters truncated"));
    }

    [Test]
    public async Task Process_ExemptTool_IsUntouched()
    {
        ToolResultTruncator truncator = new(() => 10, exemptTools: ["list_all_tools"]);
        string content = new('x', 1000);
        FunctionResult result = Result(content);

        await truncator.Process("list_all_tools", result, new FunctionCall { Name = "list_all_tools" });

        Assert.That(result.Content, Is.EqualTo(content));
    }

    [Test]
    public async Task Process_ReReadsCapEachCall()
    {
        int cap = 10;
        ToolResultTruncator truncator = new(() => cap);

        FunctionResult first = Result(new string('x', 1000));
        await truncator.Process("t", first, new FunctionCall { Name = "t" });
        Assert.That(first.Content, Does.Contain("truncated"));

        cap = 10_000; // window grew (e.g. model switch) — same instance must honor it
        FunctionResult second = Result(new string('x', 1000));
        await truncator.Process("t", second, new FunctionCall { Name = "t" });
        Assert.That(second.Content, Does.Not.Contain("truncated"));
    }
}
