using LlmTornado.Cli;
using LlmTornado.Cli.Core.Interactions;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class ToolApprovalManagerTests
{
    private TextReader? _originalIn;

    [SetUp]
    public void SetUp()
    {
        _originalIn = Console.In;

        // Delete persisted approvals to ensure test isolation
        string path = CliStorage.ToolApprovalsPath;
        if (File.Exists(path))
            File.Delete(path);
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalIn is not null)
            Console.SetIn(_originalIn);
    }

    #region ParseToolName (tested via HandleToolPermissionRequest behavior)

    [Test]
    public void GetAllApprovals_Empty_Initially()
    {
        ConsoleRenderer renderer = new();
        ToolApprovalManager manager = new(renderer);
        Assert.That(manager.GetAllApprovals(), Is.Empty);
    }

    #endregion

    #region PreApproveSkillTools

    [Test]
    public void PreApproveSkillTools_Adds_Approvals()
    {
        ConsoleRenderer renderer = new();
        ToolApprovalManager manager = new(renderer);

        manager.PreApproveSkillTools(["tool-a", "tool-b"]);

        var approvals = manager.GetAllApprovals();
        Assert.That(approvals, Has.Count.EqualTo(2));
        Assert.That(approvals["tool-a"], Is.EqualTo(ToolApprovalState.AlwaysAllow));
        Assert.That(approvals["tool-b"], Is.EqualTo(ToolApprovalState.AlwaysAllow));
    }

    [Test]
    public void PreApproveSkillTools_Does_Not_Override_Existing()
    {
        ConsoleRenderer renderer = new();
        ToolApprovalManager manager = new(renderer);

        // Pre-approve once
        manager.PreApproveSkillTools(["tool-x"]);
        Assert.That(manager.GetAllApprovals()["tool-x"], Is.EqualTo(ToolApprovalState.AlwaysAllow));

        // Pre-approve again shouldn't change existing
        manager.PreApproveSkillTools(["tool-x"]);
        Assert.That(manager.GetAllApprovals()["tool-x"], Is.EqualTo(ToolApprovalState.AlwaysAllow));
    }

    #endregion

    #region ApproveTools

    [Test]
    public void ApproveTools_Adds_AlwaysAllow_Approvals()
    {
        ConsoleRenderer renderer = new();
        ToolApprovalManager manager = new(renderer);

        int count = manager.ApproveTools(["tool-a", "tool-b", "tool-a"]);

        var approvals = manager.GetAllApprovals();
        Assert.That(count, Is.EqualTo(2));
        Assert.That(approvals, Has.Count.EqualTo(2));
        Assert.That(approvals["tool-a"], Is.EqualTo(ToolApprovalState.AlwaysAllow));
        Assert.That(approvals["tool-b"], Is.EqualTo(ToolApprovalState.AlwaysAllow));
    }

    [Test]
    public void ApproveTools_Overrides_Existing_Deny_By_Default()
    {
        ConsoleRenderer renderer = new();
        ToolApprovalManager manager = new(renderer);

        Console.SetIn(new StringReader("4" + Environment.NewLine));
        bool allowed = manager.HandleToolPermissionRequest("Tool: tool-a\nArguments: {}").GetAwaiter().GetResult();
        Assert.That(allowed, Is.False);
        Assert.That(manager.GetAllApprovals()["tool-a"], Is.EqualTo(ToolApprovalState.AlwaysDeny));

        manager.ApproveTools(["tool-a"]);

        Assert.That(manager.GetAllApprovals()["tool-a"], Is.EqualTo(ToolApprovalState.AlwaysAllow));
    }

    #endregion

    #region ResetAll / ResetTool

    [Test]
    public void ResetAll_Clears_Approvals()
    {
        ConsoleRenderer renderer = new();
        ToolApprovalManager manager = new(renderer);

        manager.PreApproveSkillTools(["a", "b", "c"]);
        Assert.That(manager.GetAllApprovals(), Has.Count.EqualTo(3));

        manager.ResetAll();
        Assert.That(manager.GetAllApprovals(), Is.Empty);
    }

    [Test]
    public void ResetTool_Removes_Single_Tool()
    {
        ConsoleRenderer renderer = new();
        ToolApprovalManager manager = new(renderer);

        manager.PreApproveSkillTools(["tool-1", "tool-2"]);
        bool removed = manager.ResetTool("tool-1");

        Assert.That(removed, Is.True);
        Assert.That(manager.GetAllApprovals(), Has.Count.EqualTo(1));
        Assert.That(manager.GetAllApprovals(), Does.ContainKey("tool-2"));
    }

    [Test]
    public void ResetTool_Returns_False_For_Unknown()
    {
        ConsoleRenderer renderer = new();
        ToolApprovalManager manager = new(renderer);

        Assert.That(manager.ResetTool("nonexistent"), Is.False);
    }

    #endregion

    #region HandleToolPermissionRequest — Auto-approve

    [Test]
    public async Task HandleToolPermission_AutoApproves_PreApproved()
    {
        ConsoleRenderer renderer = new();
        ToolApprovalManager manager = new(renderer);

        manager.PreApproveSkillTools(["my-tool"]);

        string requestMessage = "Tool: my-tool\nArguments: {\"arg\": \"value\"}";
        bool allowed = await manager.HandleToolPermissionRequest(requestMessage);
        Assert.That(allowed, Is.True);
    }

    #endregion

    #region AskQuestionsAsync

    [Test]
    public async Task AskQuestionsAsync_Collects_SingleChoice_And_Text_Answers()
    {
        Console.SetIn(new StringReader("2" + Environment.NewLine + "Needs approval" + Environment.NewLine));

        ConsoleRenderer renderer = new();
        ToolApprovalManager manager = new(renderer);

        AskQuestionsInteractionResponse response = await manager.AskQuestionsAsync(new AskQuestionsInteractionRequest
        {
            Title = "Follow up",
            Questions =
            [
                new InteractiveQuestionDefinition
                {
                    Key = "priority",
                    Prompt = "Choose priority",
                    Type = InteractiveQuestionInputType.SingleChoice,
                    Options =
                    [
                        new InteractiveQuestionOption { Value = "low", Label = "Low" },
                        new InteractiveQuestionOption { Value = "high", Label = "High" },
                    ],
                },
                new InteractiveQuestionDefinition
                {
                    Key = "note",
                    Prompt = "Add a note",
                    Type = InteractiveQuestionInputType.Text,
                },
            ],
        });

        Assert.That(response.Answers, Has.Count.EqualTo(2));
        Assert.That(response.Answers[0].TextValue, Is.EqualTo("high"));
        Assert.That(response.Answers[1].TextValue, Is.EqualTo("Needs approval"));
    }

    [Test]
    public async Task AskQuestionsAsync_Allows_Custom_And_MultiSelect_Answers()
    {
        Console.SetIn(new StringReader("0" + Environment.NewLine + "orange" + Environment.NewLine + "1,0" + Environment.NewLine + "other" + Environment.NewLine));

        ConsoleRenderer renderer = new();
        ToolApprovalManager manager = new(renderer);

        AskQuestionsInteractionResponse response = await manager.AskQuestionsAsync(new AskQuestionsInteractionRequest
        {
            Title = "Follow up",
            Questions =
            [
                new InteractiveQuestionDefinition
                {
                    Key = "color",
                    Prompt = "Pick one color",
                    Type = InteractiveQuestionInputType.SingleChoice,
                    AllowCustomAnswer = true,
                    Options =
                    [
                        new InteractiveQuestionOption { Value = "blue", Label = "Blue" },
                        new InteractiveQuestionOption { Value = "green", Label = "Green" },
                    ],
                },
                new InteractiveQuestionDefinition
                {
                    Key = "labels",
                    Prompt = "Pick labels",
                    Type = InteractiveQuestionInputType.MultiSelect,
                    AllowCustomAnswer = true,
                    Options =
                    [
                        new InteractiveQuestionOption { Value = "alpha", Label = "Alpha" },
                        new InteractiveQuestionOption { Value = "beta", Label = "Beta" },
                    ],
                },
            ],
        });

        Assert.That(response.Answers[0].TextValue, Is.EqualTo("orange"));
        Assert.That(response.Answers[0].UsedCustomAnswer, Is.True);
        Assert.That(response.Answers[1].SelectedValues, Is.EquivalentTo(new[] { "alpha", "other" }));
        Assert.That(response.Answers[1].UsedCustomAnswer, Is.True);
    }

    [Test]
    public async Task AskQuestionsAsync_Retries_Invalid_Number_Input()
    {
        Console.SetIn(new StringReader("abc" + Environment.NewLine + "42" + Environment.NewLine));

        ConsoleRenderer renderer = new();
        ToolApprovalManager manager = new(renderer);

        AskQuestionsInteractionResponse response = await manager.AskQuestionsAsync(new AskQuestionsInteractionRequest
        {
            Title = "Numbers",
            Questions =
            [
                new InteractiveQuestionDefinition
                {
                    Key = "count",
                    Prompt = "Enter a number",
                    Type = InteractiveQuestionInputType.Number,
                },
            ],
        });

        Assert.That(response.Answers[0].NumberValue, Is.EqualTo(42));
    }

    #endregion
}
