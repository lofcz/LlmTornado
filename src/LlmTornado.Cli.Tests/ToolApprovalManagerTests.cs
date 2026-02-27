using LlmTornado.Cli;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class ToolApprovalManagerTests
{
    [SetUp]
    public void SetUp()
    {
        // Delete persisted approvals to ensure test isolation
        string path = CliStorage.ToolApprovalsPath;
        if (File.Exists(path))
            File.Delete(path);
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
}
