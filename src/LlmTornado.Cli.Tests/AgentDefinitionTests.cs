using System.Text.Json;
using LlmTornado.Cli;
using LlmTornado.Cli.Agents;
using LlmTornado.Cli.Skills;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class AgentDefinitionTests
{
    #region PersonaParsing

    [TestFixture]
    public class PersonaParsing
    {
        private string _tempDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = TestHelpers.CreateTempDir();
        }

        [TearDown]
        public void TearDown()
        {
            TestHelpers.CleanupTempDir(_tempDir);
        }

        [Test]
        public void FullFrontmatter_AllFieldsParsed()
        {
            string content = """
                ---
                name: test-agent
                description: A test agent for unit testing
                enabled-skills: file-analyzer web-search
                disabled-skills: note-taker
                disabled-tools: web-search:ddg-search web-search:fetch-url
                auto-approve-tools: file-analyzer:line-count
                ---

                # Test Agent Instructions

                You are a test agent.
                """;
            string path = Path.Combine(_tempDir, "test-agent.md");
            File.WriteAllText(path, content);

            CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
                path, AgentSource.Custom);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("test-agent"));
            Assert.That(result.Description, Is.EqualTo("A test agent for unit testing"));
            Assert.That(result.Source, Is.EqualTo(AgentSource.Custom));
            Assert.That(result.EnabledSkills, Is.EqualTo(new[] { "file-analyzer", "web-search" }));
            Assert.That(result.DisabledSkills, Is.EqualTo(new[] { "note-taker" }));
            Assert.That(result.DisabledTools,
                Is.EqualTo(new[] { "web-search:ddg-search", "web-search:fetch-url" }));
            Assert.That(result.AutoApproveTools,
                Is.EqualTo(new[] { "file-analyzer:line-count" }));
            Assert.That(result.Instructions, Does.Contain("# Test Agent Instructions"));
            Assert.That(result.Instructions, Does.Contain("You are a test agent."));
            Assert.That(result.HasCapabilityCuration, Is.True);
        }

        [Test]
        public void NoFrontmatter_NameFromFilename_DescriptionFromBody()
        {
            string content = """
                # Quick Helper

                A fast assistant that gives concise answers.

                ## Style
                - Keep it short
                """;
            string path = Path.Combine(_tempDir, "quick-helper.md");
            File.WriteAllText(path, content);

            CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
                path, AgentSource.BuiltIn);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("quick-helper"));
            Assert.That(result.Description, Is.EqualTo("A fast assistant that gives concise answers."));
            Assert.That(result.Source, Is.EqualTo(AgentSource.BuiltIn));
            Assert.That(result.EnabledSkills, Is.Empty);
            Assert.That(result.DisabledSkills, Is.Empty);
            Assert.That(result.HasCapabilityCuration, Is.False);
        }

        [Test]
        public void FrontmatterName_TakesPrecedence_OverFilename()
        {
            string content = """
                ---
                name: custom-name
                description: Uses frontmatter name
                ---

                Instructions here.
                """;
            string path = Path.Combine(_tempDir, "different-filename.md");
            File.WriteAllText(path, content);

            CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
                path, AgentSource.Custom);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("custom-name"));
        }

        [Test]
        public void EmptyFile_ReturnsNull()
        {
            string path = Path.Combine(_tempDir, "empty.md");
            File.WriteAllText(path, "");

            CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
                path, AgentSource.Custom);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void WhitespaceOnly_ReturnsNull()
        {
            string path = Path.Combine(_tempDir, "whitespace.md");
            File.WriteAllText(path, "   \n  \n  ");

            CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
                path, AgentSource.Custom);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void UppercaseFilename_NormalizedToLowercase()
        {
            string path = Path.Combine(_tempDir, "InvalidName.md");
            File.WriteAllText(path, "# Instructions");

            CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
                path, AgentSource.Custom);

            // FileNameToSlug lowercases: "InvalidName" -> "invalidname"
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("invalidname"));
        }

        [Test]
        public void InvalidFilename_Underscores_ReturnsNull()
        {
            string path = Path.Combine(_tempDir, "bad_name.md");
            File.WriteAllText(path, "# Instructions");

            CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
                path, AgentSource.Custom);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ConsecutiveHyphens_ReturnsNull()
        {
            string path = Path.Combine(_tempDir, "bad--name.md");
            File.WriteAllText(path, "# Instructions");

            CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
                path, AgentSource.Custom);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void PartialFrontmatter_MissingFieldsDefaultToEmpty()
        {
            string content = """
                ---
                name: partial
                enabled-skills: file-analyzer
                ---

                Some instructions.
                """;
            string path = Path.Combine(_tempDir, "partial.md");
            File.WriteAllText(path, content);

            CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
                path, AgentSource.Custom);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("partial"));
            Assert.That(result.Description, Is.EqualTo("Some instructions."));
            Assert.That(result.EnabledSkills, Is.EqualTo(new[] { "file-analyzer" }));
            Assert.That(result.DisabledSkills, Is.Empty);
            Assert.That(result.DisabledTools, Is.Empty);
            Assert.That(result.AutoApproveTools, Is.Empty);
            Assert.That(result.HasCapabilityCuration, Is.True);
        }

        [Test]
        public void SingleValueInList_ParsesCorrectly()
        {
            string content = """
                ---
                enabled-skills: file-analyzer
                disabled-tools: web-search:ddg-search
                ---

                Instructions.
                """;
            string path = Path.Combine(_tempDir, "single-value.md");
            File.WriteAllText(path, content);

            CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
                path, AgentSource.Custom);

            Assert.That(result!.EnabledSkills, Has.Count.EqualTo(1));
            Assert.That(result.DisabledTools, Has.Count.EqualTo(1));
        }

        [Test]
        public void UnknownFrontmatterFields_AreIgnored()
        {
            string content = """
                ---
                name: tolerant
                unknown-field: some value
                another-thing: whatever
                enabled-skills: file-analyzer
                ---

                Instructions.
                """;
            string path = Path.Combine(_tempDir, "tolerant.md");
            File.WriteAllText(path, content);

            CliAgentDefinition? result = AgentDefinitionLoader.ParsePersonaFile(
                path, AgentSource.Custom);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("tolerant"));
            Assert.That(result.EnabledSkills, Is.EqualTo(new[] { "file-analyzer" }));
        }
    }

    #endregion

    #region ProjectDiscovery

    [TestFixture]
    public class ProjectDiscovery
    {
        private string _tempDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = TestHelpers.CreateTempDir();
        }

        [TearDown]
        public void TearDown()
        {
            TestHelpers.CleanupTempDir(_tempDir);
        }

        [Test]
        public void SingleAgentsMd_Discovered()
        {
            File.WriteAllText(
                Path.Combine(_tempDir, "AGENTS.md"),
                "# Project\n\n## Build\ndotnet build");

            CliAgentDefinition? result = AgentDefinitionLoader.DiscoverProjectAgents(_tempDir);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Source, Is.EqualTo(AgentSource.Project));
            Assert.That(result.Name, Is.EqualTo("project-context"));
            Assert.That(result.Instructions, Does.Contain("dotnet build"));
        }

        [Test]
        public void MultipleAgentsMd_NearestFirst()
        {
            string subDir = Path.Combine(_tempDir, "sub");
            Directory.CreateDirectory(subDir);

            File.WriteAllText(Path.Combine(_tempDir, "AGENTS.md"), "# Root instructions");
            File.WriteAllText(Path.Combine(subDir, "AGENTS.md"), "# Sub instructions");

            CliAgentDefinition? result = AgentDefinitionLoader.DiscoverProjectAgents(subDir);

            Assert.That(result, Is.Not.Null);
            int subIndex = result!.Instructions.IndexOf("Sub instructions");
            int rootIndex = result.Instructions.IndexOf("Root instructions");
            Assert.That(subIndex, Is.LessThan(rootIndex),
                "Nearest AGENTS.md content should appear first");
            Assert.That(result.FilePath, Does.Contain("sub"),
                "FilePath should be the nearest file");
        }

        [Test]
        public void MergedContent_IncludesPathComments()
        {
            string subDir = Path.Combine(_tempDir, "sub");
            Directory.CreateDirectory(subDir);

            File.WriteAllText(Path.Combine(_tempDir, "AGENTS.md"), "Root content");
            File.WriteAllText(Path.Combine(subDir, "AGENTS.md"), "Sub content");

            CliAgentDefinition? result = AgentDefinitionLoader.DiscoverProjectAgents(subDir);

            Assert.That(result!.Instructions, Does.Contain("<!-- AGENTS.md from:"));
        }

        [Test]
        public void EmptyAgentsMd_Skipped()
        {
            File.WriteAllText(Path.Combine(_tempDir, "AGENTS.md"), "   ");

            CliAgentDefinition? result = AgentDefinitionLoader.DiscoverProjectAgents(_tempDir);

            // The file itself is whitespace-only, so it's skipped.
            // Result may still be non-null if a parent directory has a valid AGENTS.md
            if (result is not null)
                Assert.That(result.Instructions, Is.Not.Empty.And.Not.All.WhiteSpace);
        }

        [Test]
        public void ProjectContext_NoCuration()
        {
            File.WriteAllText(Path.Combine(_tempDir, "AGENTS.md"), "# Instructions\nBuild stuff.");

            CliAgentDefinition? result = AgentDefinitionLoader.DiscoverProjectAgents(_tempDir);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.HasCapabilityCuration, Is.False);
            Assert.That(result.EnabledSkills, Is.Empty);
            Assert.That(result.DisabledSkills, Is.Empty);
            Assert.That(result.EnabledTools, Is.Empty);
            Assert.That(result.DisabledTools, Is.Empty);
        }
    }

    #endregion

    #region PersonaDiscovery

    [TestFixture]
    public class PersonaDiscovery
    {
        private string _tempDir = null!;
        private string _builtInDir = null!;
        private string _customDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = TestHelpers.CreateTempDir();
            _builtInDir = Path.Combine(_tempDir, "built-in");
            _customDir = Path.Combine(_tempDir, "custom");
            Directory.CreateDirectory(_builtInDir);
            Directory.CreateDirectory(_customDir);
        }

        [TearDown]
        public void TearDown()
        {
            TestHelpers.CleanupTempDir(_tempDir);
        }

        [Test]
        public void BuiltInAgents_DiscoveredWithCorrectSource()
        {
            File.WriteAllText(Path.Combine(_builtInDir, "reviewer.md"),
                "---\nname: reviewer\ndescription: Reviews code\n---\n# Review");

            List<CliAgentDefinition> result =
                AgentDefinitionLoader.DiscoverPersonaAgents(_builtInDir, _customDir);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("reviewer"));
            Assert.That(result[0].Source, Is.EqualTo(AgentSource.BuiltIn));
        }

        [Test]
        public void CustomAgent_ShadowsBuiltIn_BySameName()
        {
            File.WriteAllText(Path.Combine(_builtInDir, "reviewer.md"),
                "---\nname: reviewer\ndescription: Built-in reviewer\n---\n# Built-in");
            File.WriteAllText(Path.Combine(_customDir, "reviewer.md"),
                "---\nname: reviewer\ndescription: Custom reviewer\n---\n# Custom");

            List<CliAgentDefinition> result =
                AgentDefinitionLoader.DiscoverPersonaAgents(_builtInDir, _customDir);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Source, Is.EqualTo(AgentSource.Custom));
            Assert.That(result[0].Description, Is.EqualTo("Custom reviewer"));
        }

        [Test]
        public void MixedSources_Coexist()
        {
            File.WriteAllText(Path.Combine(_builtInDir, "reviewer.md"),
                "---\nname: reviewer\n---\n# Review");
            File.WriteAllText(Path.Combine(_customDir, "my-agent.md"),
                "---\nname: my-agent\n---\n# Custom");

            List<CliAgentDefinition> result =
                AgentDefinitionLoader.DiscoverPersonaAgents(_builtInDir, _customDir);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Select(a => a.Name),
                Is.EquivalentTo(new[] { "reviewer", "my-agent" }));
        }

        [Test]
        public void NonMdFiles_Ignored()
        {
            File.WriteAllText(Path.Combine(_builtInDir, "config.json"), "{}");
            File.WriteAllText(Path.Combine(_builtInDir, "readme.txt"), "hello");
            File.WriteAllText(Path.Combine(_builtInDir, "valid.md"),
                "---\nname: valid\n---\n# OK");

            List<CliAgentDefinition> result =
                AgentDefinitionLoader.DiscoverPersonaAgents(_builtInDir, _customDir);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("valid"));
        }

        [Test]
        public void EmptyDirectories_NoAgents()
        {
            List<CliAgentDefinition> result =
                AgentDefinitionLoader.DiscoverPersonaAgents(_builtInDir, _customDir);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void NonExistentDirectories_HandledGracefully()
        {
            string fakePath = Path.Combine(_tempDir, "nonexistent");

            List<CliAgentDefinition> result =
                AgentDefinitionLoader.DiscoverPersonaAgents(fakePath, fakePath);

            Assert.That(result, Is.Empty);
        }
    }

    #endregion

    #region ManagerLifecycle

    [TestFixture]
    public class ManagerLifecycle
    {
        private string _tempDir = null!;
        private string _builtInDir = null!;
        private string _customDir = null!;
        private CliSettings _settings = null!;
        private AgentDefinitionManager _manager = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = TestHelpers.CreateTempDir();
            _builtInDir = Path.Combine(_tempDir, "built-in");
            _customDir = Path.Combine(_tempDir, "custom");
            Directory.CreateDirectory(_builtInDir);
            Directory.CreateDirectory(_customDir);

            File.WriteAllText(Path.Combine(_builtInDir, "agent-a.md"),
                "---\nname: agent-a\ndescription: Agent A\n---\n# A");
            File.WriteAllText(Path.Combine(_builtInDir, "agent-b.md"),
                "---\nname: agent-b\ndescription: Agent B\n---\n# B");

            _settings = new CliSettings();
            _manager = new AgentDefinitionManager(_settings);
            _manager.LoadAll(_builtInDir, _customDir, _tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            TestHelpers.CleanupTempDir(_tempDir);
        }

        [Test]
        public void LoadAll_DiscoversPersonas()
        {
            Assert.That(_manager.GetAllPersonas(), Has.Count.EqualTo(2));
        }

        [Test]
        public void SetActivePersona_ValidName_Succeeds()
        {
            CliAgentDefinition? result = _manager.SetActivePersona("agent-a");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("agent-a"));
            Assert.That(_manager.ActivePersonaName, Is.EqualTo("agent-a"));
        }

        [Test]
        public void SetActivePersona_InvalidName_ReturnsNull()
        {
            CliAgentDefinition? result = _manager.SetActivePersona("nonexistent");

            Assert.That(result, Is.Null);
            Assert.That(_manager.ActivePersonaName, Is.Null);
        }

        [Test]
        public void ClearActivePersona_ResetsState()
        {
            _manager.SetActivePersona("agent-a");
            _manager.ClearActivePersona();

            Assert.That(_manager.ActivePersonaName, Is.Null);
            Assert.That(_manager.GetActivePersona(), Is.Null);
        }

        [Test]
        public void GetActivePersona_ReturnsSelectedAgent()
        {
            _manager.SetActivePersona("agent-b");

            CliAgentDefinition? active = _manager.GetActivePersona();

            Assert.That(active, Is.Not.Null);
            Assert.That(active!.Name, Is.EqualTo("agent-b"));
        }

        [Test]
        public void SetActivePersona_IsCaseInsensitive()
        {
            CliAgentDefinition? result = _manager.SetActivePersona("Agent-A");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("agent-a"));
        }

        [Test]
        public void LoadAll_RestoresSavedAgent()
        {
            _settings.ActiveAgent = "agent-a";
            AgentDefinitionManager fresh = new(_settings);
            fresh.LoadAll(_builtInDir, _customDir, _tempDir);

            Assert.That(fresh.ActivePersonaName, Is.EqualTo("agent-a"));
        }

        [Test]
        public void LoadAll_ClearsMissingSavedAgent()
        {
            _settings.ActiveAgent = "deleted-agent";
            AgentDefinitionManager fresh = new(_settings);
            fresh.LoadAll(_builtInDir, _customDir, _tempDir);

            Assert.That(fresh.ActivePersonaName, Is.Null);
            Assert.That(_settings.ActiveAgent, Is.Null);
        }

        [Test]
        public void GetPersona_ReturnsByName()
        {
            CliAgentDefinition? persona = _manager.GetPersona("agent-a");

            Assert.That(persona, Is.Not.Null);
            Assert.That(persona!.Name, Is.EqualTo("agent-a"));
        }

        [Test]
        public void GetPersona_UnknownReturnsNull()
        {
            Assert.That(_manager.GetPersona("unknown"), Is.Null);
        }
    }

    #endregion

    #region ToolFiltering

    [TestFixture]
    public class ToolFiltering
    {
        private string _tempDir = null!;
        private string _builtInDir = null!;
        private string _customDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = TestHelpers.CreateTempDir();
            _builtInDir = Path.Combine(_tempDir, "built-in");
            _customDir = Path.Combine(_tempDir, "custom");
            Directory.CreateDirectory(_builtInDir);
            Directory.CreateDirectory(_customDir);
        }

        [TearDown]
        public void TearDown()
        {
            TestHelpers.CleanupTempDir(_tempDir);
        }

        [Test]
        public void NoActivePersona_AllToolsAllowed()
        {
            CliSettings settings = new();
            AgentDefinitionManager manager = new(settings);

            Assert.That(manager.IsToolAllowed("any-tool"), Is.True);
            Assert.That(manager.IsToolAllowed("web-search:ddg-search"), Is.True);
        }

        [Test]
        public void BuiltInTools_AlwaysAllowed()
        {
            File.WriteAllText(Path.Combine(_builtInDir, "strict.md"), """
                ---
                name: strict
                enabled-tools: file-analyzer:line-count
                disabled-tools: everything:else
                ---
                # Strict
                """);

            CliSettings settings = new();
            AgentDefinitionManager manager = new(settings);
            manager.LoadAll(_builtInDir, _customDir, _tempDir);
            manager.SetActivePersona("strict");

            // Apply baseline to compute tool filters
            CliSkillManager skillManager = new(settings);
            ConsoleRenderer renderer = new();
            ToolApprovalManager toolApproval = new(renderer);
            manager.ApplyCapabilityBaseline(skillManager, toolApproval);

            Assert.That(manager.IsToolAllowed("load_skill"), Is.True);
            Assert.That(manager.IsToolAllowed("list_skills"), Is.True);
            Assert.That(manager.IsToolAllowed("read_reference"), Is.True);
        }

        [Test]
        public void BlockedTool_Rejected()
        {
            File.WriteAllText(Path.Combine(_builtInDir, "blocker.md"), """
                ---
                name: blocker
                disabled-tools: web-search:ddg-search
                ---
                # Blocker
                """);

            CliSettings settings = new();
            AgentDefinitionManager manager = new(settings);
            manager.LoadAll(_builtInDir, _customDir, _tempDir);
            manager.SetActivePersona("blocker");

            CliSkillManager skillManager = new(settings);
            ConsoleRenderer renderer = new();
            ToolApprovalManager toolApproval = new(renderer);
            manager.ApplyCapabilityBaseline(skillManager, toolApproval);

            Assert.That(manager.IsToolAllowed("web-search:ddg-search"), Is.False);
            Assert.That(manager.IsToolAllowed("file-analyzer:line-count"), Is.True);
        }

        [Test]
        public void ToolWhitelist_OnlyWhitelistedAllowed()
        {
            File.WriteAllText(Path.Combine(_builtInDir, "whitelister.md"), """
                ---
                name: whitelister
                enabled-tools: file-analyzer:line-count
                ---
                # Whitelister
                """);

            CliSettings settings = new();
            AgentDefinitionManager manager = new(settings);
            manager.LoadAll(_builtInDir, _customDir, _tempDir);
            manager.SetActivePersona("whitelister");

            CliSkillManager skillManager = new(settings);
            ConsoleRenderer renderer = new();
            ToolApprovalManager toolApproval = new(renderer);
            manager.ApplyCapabilityBaseline(skillManager, toolApproval);

            Assert.That(manager.IsToolAllowed("file-analyzer:line-count"), Is.True);
            Assert.That(manager.IsToolAllowed("file-analyzer:find-todos"), Is.False);
            Assert.That(manager.IsToolAllowed("web-search:ddg-search"), Is.False);
        }

        [Test]
        public void ClearPersona_AllToolsAllowed()
        {
            File.WriteAllText(Path.Combine(_builtInDir, "blocker2.md"), """
                ---
                name: blocker2
                disabled-tools: web-search:ddg-search
                ---
                # Blocker
                """);

            CliSettings settings = new();
            AgentDefinitionManager manager = new(settings);
            manager.LoadAll(_builtInDir, _customDir, _tempDir);
            manager.SetActivePersona("blocker2");

            CliSkillManager skillManager = new(settings);
            ConsoleRenderer renderer = new();
            ToolApprovalManager toolApproval = new(renderer);
            manager.ApplyCapabilityBaseline(skillManager, toolApproval);

            Assert.That(manager.IsToolAllowed("web-search:ddg-search"), Is.False);

            manager.ClearActivePersona();

            Assert.That(manager.IsToolAllowed("web-search:ddg-search"), Is.True);
        }
    }

    #endregion

    #region CapabilityBaseline

    [TestFixture]
    public class CapabilityBaseline
    {
        private string _tempDir = null!;
        private string _builtInDir = null!;
        private string _customDir = null!;
        private string _skillsDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = TestHelpers.CreateTempDir();
            _builtInDir = Path.Combine(_tempDir, "agents-built-in");
            _customDir = Path.Combine(_tempDir, "agents-custom");
            _skillsDir = Path.Combine(_tempDir, "skills");
            Directory.CreateDirectory(_builtInDir);
            Directory.CreateDirectory(_customDir);

            // Create agent with curation
            File.WriteAllText(Path.Combine(_builtInDir, "curated.md"), """
                ---
                name: curated
                enabled-skills: skill-a
                disabled-skills: skill-b
                ---
                # Curated agent
                """);

            // Create agent without curation
            File.WriteAllText(Path.Combine(_builtInDir, "uncurated.md"), """
                ---
                name: uncurated
                description: No curation
                ---
                # Uncurated
                """);

            // Create mock skills
            CreateMockSkill("skill-a", "Skill A");
            CreateMockSkill("skill-b", "Skill B");
            CreateMockSkill("skill-c", "Skill C");
        }

        private void CreateMockSkill(string name, string desc)
        {
            string dir = Path.Combine(_skillsDir, name);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "SKILL.md"),
                $"---\nname: {name}\ndescription: {desc}\n---\n# {desc}");
        }

        [TearDown]
        public void TearDown()
        {
            TestHelpers.CleanupTempDir(_tempDir);
        }

        [Test]
        public void Whitelist_OnlyEnabledSkillsActive()
        {
            CliSettings settings = new();
            CliSkillManager skillManager = new(settings);
            skillManager.LoadSkills(_skillsDir);

            AgentDefinitionManager manager = new(settings);
            manager.LoadAll(_builtInDir, _customDir, _tempDir);
            manager.SetActivePersona("curated");

            ConsoleRenderer renderer = new();
            ToolApprovalManager toolApproval = new(renderer);
            manager.ApplyCapabilityBaseline(skillManager, toolApproval);

            // enabled-skills: [skill-a], disabled-skills: [skill-b]
            // skill-a: enabled (whitelisted)
            // skill-b: disabled (blacklisted, was also in whitelist but blacklist overrides)
            // skill-c: disabled (not in whitelist)
            Assert.That(skillManager.GetSkill("skill-a")!.Enabled, Is.True);
            Assert.That(skillManager.GetSkill("skill-b")!.Enabled, Is.False);
            Assert.That(skillManager.GetSkill("skill-c")!.Enabled, Is.False);
        }

        [Test]
        public void NoCuration_AllSkillsEnabled()
        {
            CliSettings settings = new();
            CliSkillManager skillManager = new(settings);
            skillManager.LoadSkills(_skillsDir);

            AgentDefinitionManager manager = new(settings);
            manager.LoadAll(_builtInDir, _customDir, _tempDir);
            manager.SetActivePersona("uncurated");

            ConsoleRenderer renderer = new();
            ToolApprovalManager toolApproval = new(renderer);
            manager.ApplyCapabilityBaseline(skillManager, toolApproval);

            Assert.That(skillManager.GetSkill("skill-a")!.Enabled, Is.True);
            Assert.That(skillManager.GetSkill("skill-b")!.Enabled, Is.True);
            Assert.That(skillManager.GetSkill("skill-c")!.Enabled, Is.True);
        }

        [Test]
        public void ClearPersona_AllSkillsRestored()
        {
            CliSettings settings = new();
            CliSkillManager skillManager = new(settings);
            skillManager.LoadSkills(_skillsDir);

            AgentDefinitionManager manager = new(settings);
            manager.LoadAll(_builtInDir, _customDir, _tempDir);
            manager.SetActivePersona("curated");

            ConsoleRenderer renderer = new();
            ToolApprovalManager toolApproval = new(renderer);
            manager.ApplyCapabilityBaseline(skillManager, toolApproval);

            // Verify curated state
            Assert.That(skillManager.GetSkill("skill-c")!.Enabled, Is.False);

            // Clear and reapply
            manager.ClearActivePersona();
            manager.ApplyCapabilityBaseline(skillManager, toolApproval);

            Assert.That(skillManager.GetSkill("skill-a")!.Enabled, Is.True);
            Assert.That(skillManager.GetSkill("skill-b")!.Enabled, Is.True);
            Assert.That(skillManager.GetSkill("skill-c")!.Enabled, Is.True);
        }

        [Test]
        public void Baseline_IsIdempotent()
        {
            CliSettings settings = new();
            CliSkillManager skillManager = new(settings);
            skillManager.LoadSkills(_skillsDir);

            AgentDefinitionManager manager = new(settings);
            manager.LoadAll(_builtInDir, _customDir, _tempDir);
            manager.SetActivePersona("curated");

            ConsoleRenderer renderer = new();
            ToolApprovalManager toolApproval = new(renderer);

            manager.ApplyCapabilityBaseline(skillManager, toolApproval);
            manager.ApplyCapabilityBaseline(skillManager, toolApproval); // second call

            Assert.That(skillManager.GetSkill("skill-a")!.Enabled, Is.True);
            Assert.That(skillManager.GetSkill("skill-b")!.Enabled, Is.False);
            Assert.That(skillManager.GetSkill("skill-c")!.Enabled, Is.False);
        }
    }

    #endregion

    #region SettingsPersistence

    [TestFixture]
    public class SettingsPersistence
    {
        [Test]
        public void NewFields_SerializeAndDeserialize()
        {
            CliSettings original = new()
            {
                ActiveModel = "gpt-4o",
                ActiveAgent = "code-reviewer",
                AgentsDirectory = "/custom/agents",
                ProjectAgentsEnabled = false,
                DisabledSkills = ["note-taker"]
            };

            string json = JsonSerializer.Serialize(original);
            CliSettings? restored = JsonSerializer.Deserialize<CliSettings>(json);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.ActiveAgent, Is.EqualTo("code-reviewer"));
            Assert.That(restored.AgentsDirectory, Is.EqualTo("/custom/agents"));
            Assert.That(restored.ProjectAgentsEnabled, Is.False);
        }

        [Test]
        public void OldSettingsJson_DeserializesWithDefaults()
        {
            string oldJson = """{"active_model":"gpt-4o","disabled_skills":[]}""";

            CliSettings? settings = JsonSerializer.Deserialize<CliSettings>(oldJson);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings!.ActiveAgent, Is.Null);
            Assert.That(settings.AgentsDirectory, Is.Null);
            Assert.That(settings.ProjectAgentsEnabled, Is.True);
        }
    }

    #endregion

    #region InstructionsBlock

    [TestFixture]
    public class InstructionsBlock
    {
        private string _tempDir = null!;
        private string _builtInDir = null!;
        private string _customDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = TestHelpers.CreateTempDir();
            _builtInDir = Path.Combine(_tempDir, "built-in");
            _customDir = Path.Combine(_tempDir, "custom");
            Directory.CreateDirectory(_builtInDir);
            Directory.CreateDirectory(_customDir);
        }

        [TearDown]
        public void TearDown()
        {
            TestHelpers.CleanupTempDir(_tempDir);
        }

        [Test]
        public void WithPersonaAndProject_BothIncluded()
        {
            File.WriteAllText(Path.Combine(_builtInDir, "test-agent.md"),
                "---\nname: test-agent\n---\n# Test Agent\nBe helpful.");
            File.WriteAllText(Path.Combine(_tempDir, "AGENTS.md"),
                "# Project\nBuild with dotnet.");

            CliSettings settings = new();
            AgentDefinitionManager manager = new(settings);
            manager.LoadAll(_builtInDir, _customDir, _tempDir);
            manager.SetActivePersona("test-agent");

            string block = manager.BuildInstructionsBlock();

            Assert.That(block, Does.Contain("<agent_persona>"));
            Assert.That(block, Does.Contain("</agent_persona>"));
            Assert.That(block, Does.Contain("<project_context>"));
            Assert.That(block, Does.Contain("</project_context>"));
            Assert.That(block, Does.Contain("Be helpful."));
            Assert.That(block, Does.Contain("Build with dotnet."));

            // Persona should come before project context
            int personaIdx = block.IndexOf("<agent_persona>");
            int projectIdx = block.IndexOf("<project_context>");
            Assert.That(personaIdx, Is.LessThan(projectIdx));
        }

        [Test]
        public void PersonaOnly_NoProjectTags()
        {
            File.WriteAllText(Path.Combine(_builtInDir, "test-agent.md"),
                "---\nname: test-agent\n---\n# Test\nBe helpful.");

            // No AGENTS.md in tempDir
            CliSettings settings = new();
            AgentDefinitionManager manager = new(settings);
            manager.LoadAll(_builtInDir, _customDir, _tempDir);
            manager.SetActivePersona("test-agent");

            string block = manager.BuildInstructionsBlock();

            Assert.That(block, Does.Contain("<agent_persona>"));
            // Project context may or may not appear depending on parent AGENTS.md
            // so we just verify persona is there
        }

        [Test]
        public void NoPersonaNoProject_ReturnsEmpty()
        {
            // No agents, isolated temp directory with ProjectAgentsEnabled=false
            CliSettings settings = new() { ProjectAgentsEnabled = false };
            AgentDefinitionManager manager = new(settings);
            manager.LoadAll(_builtInDir, _customDir, _tempDir);

            string block = manager.BuildInstructionsBlock();

            Assert.That(block, Is.Empty);
        }

        [Test]
        public void ProjectDisabled_NotIncluded()
        {
            File.WriteAllText(Path.Combine(_tempDir, "AGENTS.md"), "# Project\nImportant.");

            CliSettings settings = new() { ProjectAgentsEnabled = false };
            AgentDefinitionManager manager = new(settings);
            manager.LoadAll(_builtInDir, _customDir, _tempDir);

            string block = manager.BuildInstructionsBlock();

            Assert.That(block, Does.Not.Contain("<project_context>"));
        }
    }

    #endregion

    #region BuiltInAgentsIntegrity

    [TestFixture]
    public class BuiltInAgentsIntegrity
    {
        [Test]
        public void AllBuiltInAgents_ParseCorrectly()
        {
            string builtInDir = AgentDefinitionLoader.ResolveBuiltInDirectory();

            // Skip if built-in directory doesn't exist (e.g., running tests outside build output)
            if (!Directory.Exists(builtInDir))
            {
                Assert.Ignore($"Built-in agents directory not found: {builtInDir}");
                return;
            }

            string[] files = Directory.GetFiles(builtInDir, "*.md");
            Assert.That(files, Is.Not.Empty, "Expected built-in agent files to be present");

            foreach (string file in files)
            {
                CliAgentDefinition? agent = AgentDefinitionLoader.ParsePersonaFile(
                    file, AgentSource.BuiltIn);

                Assert.That(agent, Is.Not.Null,
                    $"Failed to parse built-in agent: {Path.GetFileName(file)}");
                Assert.That(agent!.Name, Is.Not.Empty);
                Assert.That(agent.Instructions, Is.Not.Empty);
            }
        }

        [Test]
        public void DefaultAgent_HasNoCuration()
        {
            string builtInDir = AgentDefinitionLoader.ResolveBuiltInDirectory();
            if (!Directory.Exists(builtInDir))
            {
                Assert.Ignore("Built-in agents directory not found");
                return;
            }

            string defaultPath = Path.Combine(builtInDir, "default.md");
            if (!File.Exists(defaultPath))
            {
                Assert.Ignore("default.md not found in built-in agents");
                return;
            }

            CliAgentDefinition? agent = AgentDefinitionLoader.ParsePersonaFile(
                defaultPath, AgentSource.BuiltIn);

            Assert.That(agent, Is.Not.Null);
            Assert.That(agent!.HasCapabilityCuration, Is.False);
        }

        [Test]
        public void CodeReviewerAgent_HasExpectedCuration()
        {
            string builtInDir = AgentDefinitionLoader.ResolveBuiltInDirectory();
            if (!Directory.Exists(builtInDir))
            {
                Assert.Ignore("Built-in agents directory not found");
                return;
            }

            string path = Path.Combine(builtInDir, "code-reviewer.md");
            if (!File.Exists(path))
            {
                Assert.Ignore("code-reviewer.md not found");
                return;
            }

            CliAgentDefinition? agent = AgentDefinitionLoader.ParsePersonaFile(
                path, AgentSource.BuiltIn);

            Assert.That(agent, Is.Not.Null);
            Assert.That(agent!.HasCapabilityCuration, Is.True);
            Assert.That(agent.EnabledSkills, Does.Contain("file-analyzer"));
            Assert.That(agent.AutoApproveTools, Is.Not.Empty);
        }
    }

    #endregion
}
