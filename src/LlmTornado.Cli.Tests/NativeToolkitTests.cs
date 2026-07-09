using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Tools.Native;
using LlmTornado.Common;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class NativeToolkitTests
{
    private string _tempDir = null!;
    private NativeToolContext _ctx = null!;
    private Dictionary<string, Tool> _tools = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = TestHelpers.CreateTempDir();
        _ctx = new NativeToolContext
        {
            GetWorkingDirectory = () => _tempDir,
            Settings = new AgentSettings(),
        };
        _tools = NativeToolkit.Build(_ctx).ToDictionary(t => t.ResolvedName, t => t);
    }

    [TearDown]
    public void TearDown() => TestHelpers.CleanupTempDir(_tempDir);

    private string Invoke(string toolName, object request)
    {
        // Native tools are plain delegates; invoke them directly through the typed methods
        // by dispatching on the request type (mirrors what the runtime's JSON layer does).
        Delegate del = _tools[toolName].Delegate!;
        object? result = del.DynamicInvoke(request);
        if (result is Task<string> task)
            return task.GetAwaiter().GetResult();
        return (string)result!;
    }

    // ─────────────── read/write/edit round-trip ───────────────

    [Test]
    public void WriteReadEdit_RoundTrip()
    {
        string write = Invoke("write_file", new WriteFileRequest { Path = "notes/a.txt", Content = "hello\nworld\n" });
        Assert.That(write, Does.StartWith("Wrote"));

        string read = Invoke("read_file", new ReadFileRequest { Path = "notes/a.txt" });
        Assert.That(read, Does.Contain("1: hello"));
        Assert.That(read, Does.Contain("2: world"));

        string edit = Invoke("edit_file", new EditFileRequest { Path = "notes/a.txt", OldString = "world", NewString = "there" });
        Assert.That(edit, Does.Contain("Replaced 1 occurrence"));
        Assert.That(File.ReadAllText(Path.Combine(_tempDir, "notes", "a.txt")), Does.Contain("there"));
    }

    [Test]
    public void ReadFile_OffsetAndLimit_Windowing()
    {
        File.WriteAllLines(Path.Combine(_tempDir, "many.txt"), Enumerable.Range(1, 50).Select(i => $"line{i}"));

        string read = Invoke("read_file", new ReadFileRequest { Path = "many.txt", Offset = 10, Limit = 3 });
        Assert.That(read, Does.Contain("10: line10"));
        Assert.That(read, Does.Contain("12: line12"));
        Assert.That(read, Does.Not.Contain("13: line13"));
        Assert.That(read, Does.Contain("offset: 13"));
    }

    [Test]
    public void EditFile_NotFound_And_NotUnique_Errors()
    {
        File.WriteAllText(Path.Combine(_tempDir, "dup.txt"), "aaa bbb aaa");

        string missing = Invoke("edit_file", new EditFileRequest { Path = "dup.txt", OldString = "zzz", NewString = "x" });
        Assert.That(missing, Does.Contain("not found"));

        string ambiguous = Invoke("edit_file", new EditFileRequest { Path = "dup.txt", OldString = "aaa", NewString = "x" });
        Assert.That(ambiguous, Does.Contain("not unique"));
        Assert.That(ambiguous, Does.Contain("2 matches"));

        string replaceAll = Invoke("edit_file", new EditFileRequest { Path = "dup.txt", OldString = "aaa", NewString = "x", ReplaceAll = true });
        Assert.That(replaceAll, Does.Contain("Replaced 2 occurrence(s)"));
        Assert.That(File.ReadAllText(Path.Combine(_tempDir, "dup.txt")), Is.EqualTo("x bbb x"));
    }

    // ─────────────── glob / grep / list_dir ───────────────

    [Test]
    public void Glob_MatchesRecursively_AndSkipsIgnoredDirs()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "src", "deep"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "node_modules", "pkg"));
        File.WriteAllText(Path.Combine(_tempDir, "src", "a.cs"), "x");
        File.WriteAllText(Path.Combine(_tempDir, "src", "deep", "b.cs"), "x");
        File.WriteAllText(Path.Combine(_tempDir, "src", "c.txt"), "x");
        File.WriteAllText(Path.Combine(_tempDir, "node_modules", "pkg", "d.cs"), "x");

        string result = Invoke("glob", new GlobRequest { Pattern = "**/*.cs" });
        Assert.That(result, Does.Contain("src/a.cs"));
        Assert.That(result, Does.Contain("src/deep/b.cs"));
        Assert.That(result, Does.Not.Contain("c.txt"));
        Assert.That(result, Does.Not.Contain("node_modules"));
    }

    [Test]
    public void Grep_FindsMatches_WithGlobFilter_AndSkipsBinary()
    {
        File.WriteAllText(Path.Combine(_tempDir, "code.cs"), "int answer = 42;\nstring q = \"...\";");
        File.WriteAllText(Path.Combine(_tempDir, "notes.txt"), "answer is 42");
        File.WriteAllBytes(Path.Combine(_tempDir, "blob.bin"), [0x00, 0x42, 0x00, 0x61]);

        string all = Invoke("grep", new GrepRequest { Pattern = "answer" });
        Assert.That(all, Does.Contain("code.cs:1:"));
        Assert.That(all, Does.Contain("notes.txt:1:"));
        Assert.That(all, Does.Not.Contain("blob.bin"));

        string filtered = Invoke("grep", new GrepRequest { Pattern = "answer", Glob = "*.cs" });
        Assert.That(filtered, Does.Contain("code.cs"));
        Assert.That(filtered, Does.Not.Contain("notes.txt"));
    }

    [Test]
    public void Grep_InvalidRegex_ReturnsError()
    {
        string result = Invoke("grep", new GrepRequest { Pattern = "([unclosed" });
        Assert.That(result, Does.StartWith("ERROR: invalid regex"));
    }

    [Test]
    public void Grep_MaxResults_Caps()
    {
        File.WriteAllLines(Path.Combine(_tempDir, "many.txt"), Enumerable.Repeat("hit", 20));
        string result = Invoke("grep", new GrepRequest { Pattern = "hit", MaxResults = 5 });
        Assert.That(result, Does.Contain("stopped at 5 matches"));
    }

    [Test]
    public void ListDir_ShowsDirsWithSlash()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "sub"));
        File.WriteAllText(Path.Combine(_tempDir, "f.txt"), "x");

        string result = Invoke("list_dir", new ListDirRequest());
        Assert.That(result, Does.Contain("sub/"));
        Assert.That(result, Does.Contain("f.txt"));
    }

    // ─────────────── policy enforcement ───────────────

    [Test]
    public void PathsOutsideWorkingDirectory_AreDenied()
    {
        string outside = Path.Combine(Path.GetTempPath(), $"outside-{Guid.NewGuid():N}.txt");

        Assert.That(Invoke("read_file", new ReadFileRequest { Path = outside }), Does.Contain("access denied"));
        Assert.That(Invoke("write_file", new WriteFileRequest { Path = outside, Content = "x" }), Does.Contain("access denied"));
        Assert.That(Invoke("edit_file", new EditFileRequest { Path = outside, OldString = "a", NewString = "b" }), Does.Contain("access denied"));
        Assert.That(Invoke("glob", new GlobRequest { Pattern = "*", Path = Path.GetTempPath() }), Does.Contain("access denied"));
        Assert.That(File.Exists(outside), Is.False);
    }

    [Test]
    public void WhitelistedPath_IsAllowed()
    {
        string extra = TestHelpers.CreateTempDir();
        try
        {
            _ctx = new NativeToolContext
            {
                GetWorkingDirectory = () => _tempDir,
                Settings = new AgentSettings { FilesystemWhitelist = [extra] },
            };
            _tools = NativeToolkit.Build(_ctx).ToDictionary(t => t.ResolvedName, t => t);

            string file = Path.Combine(extra, "ok.txt");
            Assert.That(Invoke("write_file", new WriteFileRequest { Path = file, Content = "fine" }), Does.StartWith("Wrote"));
            Assert.That(Invoke("read_file", new ReadFileRequest { Path = file }), Does.Contain("fine"));
        }
        finally
        {
            TestHelpers.CleanupTempDir(extra);
        }
    }

    [Test]
    public void Shell_BlockedCommand_IsDenied()
    {
        _ctx = new NativeToolContext
        {
            GetWorkingDirectory = () => _tempDir,
            Settings = new AgentSettings { BlockedCommands = ["evilcmd"] },
        };
        _tools = NativeToolkit.Build(_ctx).ToDictionary(t => t.ResolvedName, t => t);

        string result = Invoke("shell", new ShellRequest { Command = "evilcmd --do-bad-things" });
        Assert.That(result, Does.Contain("blocked by session policy"));
    }

    [Test]
    public void Shell_AllowlistRestrictsOtherCommands()
    {
        _ctx = new NativeToolContext
        {
            GetWorkingDirectory = () => _tempDir,
            Settings = new AgentSettings { AllowedCommands = ["echo"] },
        };
        _tools = NativeToolkit.Build(_ctx).ToDictionary(t => t.ResolvedName, t => t);

        Assert.That(Invoke("shell", new ShellRequest { Command = "git status" }), Does.Contain("blocked by session policy"));
    }

    // ─────────────── shell execution ───────────────

    [Test]
    public void Shell_RunsCommand_AndReportsExitCode()
    {
        string result = Invoke("shell", new ShellRequest { Command = "echo native-tools-ok" });
        Assert.That(result, Does.Contain("native-tools-ok"));
        Assert.That(result, Does.Contain("(exit code 0)"));
    }

    [Test]
    public void Shell_NonZeroExit_IsReported()
    {
        string command = OperatingSystem.IsWindows() ? "exit /b 3" : "exit 3";
        string result = Invoke("shell", new ShellRequest { Command = command });
        Assert.That(result, Does.Contain("(exit code 3)"));
    }

    [Test]
    public void Shell_Timeout_KillsProcess()
    {
        string command = OperatingSystem.IsWindows() ? "ping -n 30 127.0.0.1 > nul" : "sleep 30";
        string result = Invoke("shell", new ShellRequest { Command = command, TimeoutSeconds = 1 });
        Assert.That(result, Does.Contain("timed out"));
    }

    // ─────────────── glob-to-regex unit coverage ───────────────

    [TestCase("**/*.cs", "src/deep/a.cs", true)]
    [TestCase("**/*.cs", "a.cs", true)]
    [TestCase("*.cs", "a.cs", true)]
    [TestCase("*.cs", "src/a.cs", false)]
    [TestCase("src/*.cs", "src/a.cs", true)]
    [TestCase("src/*.cs", "src/deep/a.cs", false)]
    [TestCase("src/**", "src/deep/a.cs", true)]
    [TestCase("a?.txt", "ab.txt", true)]
    [TestCase("a?.txt", "a/b.txt", false)]
    public void GlobToRegex_Matches(string pattern, string path, bool expected)
    {
        Assert.That(NativeToolkit.GlobToRegex(pattern).IsMatch(path), Is.EqualTo(expected));
    }
}
