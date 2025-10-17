using System;
using System.Threading.Tasks;
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
        TornadoApi api = Program.Connect();
        
        Console.WriteLine("Listing all skills...");
        SkillListResponse skills = await api.Skills.ListSkillsAsync();
        
        Console.WriteLine($"Found {skills.Data.Count} skill(s):");
        foreach (Skill skill in skills.Data)
        {
            Console.WriteLine($"  - {skill.Name} (ID: {skill.Id})");
            if (!string.IsNullOrEmpty(skill.Description))
            {
                Console.WriteLine($"    Description: {skill.Description}");
            }
            if (!string.IsNullOrEmpty(skill.ActiveVersionId))
            {
                Console.WriteLine($"    Active Version: {skill.ActiveVersionId}");
            }
        }
    }
    
    [TornadoTest("Create a new skill")]
    public static async Task CreateSkill()
    {
        TornadoApi api = Program.Connect();
        
        Console.WriteLine("Creating a new skill...");
        Skill skill = await api.Skills.CreateSkillAsync(
            "Code Review Assistant",
            "Specialized skill for reviewing code and providing constructive feedback"
        );
        
        Console.WriteLine($"Created skill: {skill.Name} (ID: {skill.Id})");
        Console.WriteLine($"Description: {skill.Description}");
        Console.WriteLine($"Created at: {skill.CreatedAt}");
        
        // Clean up
        Console.WriteLine("\nCleaning up - deleting created skill...");
        bool deleted = await api.Skills.DeleteSkillAsync(skill.Id);
        Console.WriteLine($"Skill deleted: {deleted}");
    }
    
    [TornadoTest("Create skill with version")]
    public static async Task CreateSkillWithVersion()
    {
        TornadoApi api = Program.Connect();
        
        Console.WriteLine("Creating a new skill...");
        Skill skill = await api.Skills.CreateSkillAsync(
            "Technical Writer",
            "Specialized skill for writing technical documentation"
        );
        
        Console.WriteLine($"Created skill: {skill.Name} (ID: {skill.Id})");
        
        Console.WriteLine("\nCreating a version for the skill...");
        SkillVersion version = await api.Skills.CreateSkillVersionAsync(
            skill.Id,
            "You are a technical writer specializing in API documentation. " +
            "Write clear, concise documentation with examples. " +
            "Always include code samples and usage instructions."
        );
        
        Console.WriteLine($"Created version: {version.Id}");
        Console.WriteLine($"System Prompt: {version.SystemPrompt}");
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
        TornadoApi api = Program.Connect();
        
        // CREATE
        Console.WriteLine("=== CREATE ===");
        Skill skill = await api.Skills.CreateSkillAsync(
            "Data Analyst",
            "Analyzes data and provides insights"
        );
        Console.WriteLine($"Created: {skill.Name} (ID: {skill.Id})");
        
        // READ
        Console.WriteLine("\n=== READ ===");
        Skill retrievedSkill = await api.Skills.GetSkillAsync(skill.Id);
        Console.WriteLine($"Retrieved: {retrievedSkill.Name}");
        Console.WriteLine($"Description: {retrievedSkill.Description}");
        
        // UPDATE
        Console.WriteLine("\n=== UPDATE ===");
        UpdateSkillRequest updateRequest = new UpdateSkillRequest
        {
            Name = "Advanced Data Analyst",
            Description = "Advanced skill for analyzing complex datasets and providing detailed insights"
        };
        Skill updatedSkill = await api.Skills.UpdateSkillAsync(skill.Id, updateRequest);
        Console.WriteLine($"Updated: {updatedSkill.Name}");
        Console.WriteLine($"New Description: {updatedSkill.Description}");
        
        // CREATE VERSION
        Console.WriteLine("\n=== CREATE VERSION ===");
        SkillVersion version1 = await api.Skills.CreateSkillVersionAsync(
            skill.Id,
            "You are an expert data analyst. Analyze the provided data and give actionable insights."
        );
        Console.WriteLine($"Created version: {version1.Id}");
        
        SkillVersion version2 = await api.Skills.CreateSkillVersionAsync(
            skill.Id,
            "You are a senior data analyst with 10 years of experience. " +
            "Provide comprehensive analysis with statistical rigor."
        );
        Console.WriteLine($"Created version: {version2.Id}");
        
        // LIST VERSIONS
        Console.WriteLine("\n=== LIST VERSIONS ===");
        SkillVersionListResponse versions = await api.Skills.ListSkillVersionsAsync(skill.Id);
        Console.WriteLine($"Found {versions.Data.Count} version(s):");
        foreach (SkillVersion v in versions.Data)
        {
            Console.WriteLine($"  - Version {v.Id}");
            Console.WriteLine($"    Prompt preview: {v.SystemPrompt?.Substring(0, Math.Min(50, v.SystemPrompt.Length ?? 0))}...");
        }
        
        // GET SPECIFIC VERSION
        Console.WriteLine("\n=== GET VERSION ===");
        SkillVersion retrievedVersion = await api.Skills.GetSkillVersionAsync(skill.Id, version1.Id);
        Console.WriteLine($"Retrieved version: {retrievedVersion.Id}");
        Console.WriteLine($"System Prompt: {retrievedVersion.SystemPrompt}");
        
        // UPDATE SKILL WITH ACTIVE VERSION
        Console.WriteLine("\n=== SET ACTIVE VERSION ===");
        UpdateSkillRequest activateRequest = new UpdateSkillRequest
        {
            ActiveVersionId = version2.Id
        };
        Skill skillWithActiveVersion = await api.Skills.UpdateSkillAsync(skill.Id, activateRequest);
        Console.WriteLine($"Active version set to: {skillWithActiveVersion.ActiveVersionId}");
        
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
