using System;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Skills;

namespace LlmTornado.Demo;

/// <summary>
/// Demonstrates the Anthropic Skills API functionality.
/// Skills allow you to create specialized prompts and configurations that Claude can automatically select and use.
/// </summary>
public class SkillsDemo : DemoBase
{
    [TornadoTest("List all skills")]
    public static async Task ListSkills()
    {
        TornadoApi api = new TornadoApi(new AnthropicEndpointProvider
        {
            Auth = new ProviderAuthentication(Program.ApiKeys.Anthropic),
            UrlResolver = (endpoint, url, ctx) => "https://api.anthropic.com/v1/skills",
            RequestResolver = (request, data, streaming) =>
            {
                // by default, providing a custom request resolver omits beta headers
                // to include beta headers for features like interleaved thinking, files API, code execution, and search results:
                request.Headers.Add("anthropic-beta", ["skills-2025-10-02", "files-api-2025-04-14", "code-execution-2025-08-25", "search-results-2025-06-09"]);
            }
        });


        Console.WriteLine("Listing all skills...");
        SkillListResponse skills = await api.Skills.ListSkillsAsync();
        
        Console.WriteLine($"Found {skills.Data.Count} skill(s):");
        foreach (Skill skill in skills.Data)
        {
            Console.WriteLine($"  - {skill.DisplayTitle} (ID: {skill.Id})");
        }
    }
    
    [TornadoTest("Create a new skill")]
    public static async Task CreateSkill()
    {
        TornadoApi api = new TornadoApi(new AnthropicEndpointProvider
        {
            Auth = new ProviderAuthentication(Program.ApiKeys.Anthropic),
            UrlResolver = (endpoint, url, ctx) => "https://api.anthropic.com/v1/skills",
            RequestResolver = (request, data, streaming) =>
            {
                // by default, providing a custom request resolver omits beta headers
                // to include beta headers for features like interleaved thinking, files API, code execution, and search results:
                request.Headers.Add("anthropic-beta", ["skills-2025-10-02", "files-api-2025-04-14", "code-execution-2025-08-25", "search-results-2025-06-09"]);
            }
        });

        Console.WriteLine("Creating a new skill...");
        Skill skill = await api.Skills.CreateSkillAsync(
            "Code Review Assistant"
        );
        
        Console.WriteLine($"Created skill: {skill.DisplayTitle} (ID: {skill.Id})");
        Console.WriteLine($"Created at: {skill.CreatedAt}");
        
        // Clean up
        Console.WriteLine("\nCleaning up - deleting created skill...");
        bool deleted = await api.Skills.DeleteSkillAsync(skill.Id);
        Console.WriteLine($"Skill deleted: {deleted}");
    }
    
    [TornadoTest("Create skill with version")]
    public static async Task CreateSkillWithVersion()
    {
        TornadoApi api = new TornadoApi(new AnthropicEndpointProvider
        {
            Auth = new ProviderAuthentication(Program.ApiKeys.Anthropic),
            UrlResolver = (endpoint, url, ctx) => "https://api.anthropic.com/v1/skills",
            RequestResolver = (request, data, streaming) =>
            {
                // by default, providing a custom request resolver omits beta headers
                // to include beta headers for features like interleaved thinking, files API, code execution, and search results:
                request.Headers.Add("anthropic-beta", ["skills-2025-10-02", "files-api-2025-04-14", "code-execution-2025-08-25", "search-results-2025-06-09"]);
            }
        });

        Console.WriteLine("Creating a new skill...");
        Skill skill = await api.Skills.CreateSkillAsync(
            "Technical Writer"
        );
        
        Console.WriteLine($"Created skill: {skill.DisplayTitle} (ID: {skill.Id})");
        
        Console.WriteLine("\nCreating a version for the skill...");
        SkillVersion version = await api.Skills.CreateSkillVersionAsync(
            skill.Id,
            new CreateSkillVersionRequest()
        );
        
        Console.WriteLine($"Created version: {version.Id}");
        Console.WriteLine($"Created at: {version.CreatedAt}");
        
        // Clean up
        Console.WriteLine("\nCleaning up...");
        Console.WriteLine("Deleting version...");
        bool versionDeleted = await api.Skills.DeleteSkillVersionAsync(skill.Id, version.Id);
        Console.WriteLine($"Version deleted: {versionDeleted}");
        
        Console.WriteLine("Deleting skill...");
        bool skillDeleted = await api.Skills.DeleteSkillAsync(skill.Id);
        Console.WriteLine($"Skill deleted: {skillDeleted}");
    }
    
    [TornadoTest("Full CRUD operations")]
    public static async Task FullCrudOperations()
    {
        TornadoApi api = new TornadoApi(new AnthropicEndpointProvider
        {
            Auth = new ProviderAuthentication(Program.ApiKeys.Anthropic),
            UrlResolver = (endpoint, url, ctx) => "https://api.anthropic.com/v1/skills",
            RequestResolver = (request, data, streaming) =>
            {
                // by default, providing a custom request resolver omits beta headers
                // to include beta headers for features like interleaved thinking, files API, code execution, and search results:
                request.Headers.Add("anthropic-beta", ["skills-2025-10-02", "files-api-2025-04-14", "code-execution-2025-08-25", "search-results-2025-06-09"]);
                request.Headers.Add("anthropic-version", "2023-06-01");
            }
        });

        // CREATE
        Console.WriteLine("=== CREATE ===");
        Skill skill = await api.Skills.CreateSkillAsync(
            "Data Analyst"
        );
        Console.WriteLine($"Created: {skill.DisplayTitle} (ID: {skill.Id})");
        
        // READ
        Console.WriteLine("\n=== READ ===");
        Skill retrievedSkill = await api.Skills.GetSkillAsync(skill.Id);
        Console.WriteLine($"Retrieved: {retrievedSkill.DisplayTitle}");
        
        
        // CREATE VERSION
        Console.WriteLine("\n=== CREATE VERSION ===");
        SkillVersion version1 = await api.Skills.CreateSkillVersionAsync(
            skill.Id,
            new CreateSkillVersionRequest()
        );
        Console.WriteLine($"Created version: {version1.Id}");
        
        SkillVersion version2 = await api.Skills.CreateSkillVersionAsync(
            skill.Id,
            new CreateSkillVersionRequest()
        );
        Console.WriteLine($"Created version: {version2.Id}");
        
        // LIST VERSIONS
        Console.WriteLine("\n=== LIST VERSIONS ===");
        SkillVersionListResponse versions = await api.Skills.ListSkillVersionsAsync(skill.Id);
        Console.WriteLine($"Found {versions.Data.Count} version(s):");
        foreach (SkillVersion v in versions.Data)
        {
            Console.WriteLine($"  - Version {v.Id}");
            Console.WriteLine($"    Prompt preview: {v.Description}...");
        }
        
        // GET SPECIFIC VERSION
        Console.WriteLine("\n=== GET VERSION ===");
        SkillVersion retrievedVersion = await api.Skills.GetSkillVersionAsync(skill.Id, version1.Id);
        Console.WriteLine($"Retrieved version: {retrievedVersion.Id}");
        Console.WriteLine($"System Prompt: {retrievedVersion.Description}");
        

        // DELETE (cleanup)
        Console.WriteLine("\n=== DELETE ===");
        Console.WriteLine("Deleting versions...");
        bool v1Deleted = await api.Skills.DeleteSkillVersionAsync(skill.Id, version1.Id);
        Console.WriteLine($"Version 1 deleted: {v1Deleted}");
        
        bool v2Deleted = await api.Skills.DeleteSkillVersionAsync(skill.Id, version2.Id);
        Console.WriteLine($"Version 2 deleted: {v2Deleted}");
        
        Console.WriteLine("Deleting skill...");
        bool skillDeleted = await api.Skills.DeleteSkillAsync(skill.Id);
        Console.WriteLine($"Skill deleted: {skillDeleted}");
        
        Console.WriteLine("\n=== DEMO COMPLETE ===");
    }
}
