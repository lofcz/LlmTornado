using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using LlmTornado.Files;
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
            Console.WriteLine($"  - {skill.DisplayTitle} (ID: {skill.Id})");
        }
    }
    
    [TornadoTest("Create a new skill")]
    public static async Task CreateSkill()
    {
        TornadoApi api = Program.Connect();

        Console.WriteLine("Creating a new skill...");
        Skill skill = await api.Skills.CreateSkillAsync(
            "pdf-processor",
            [new FileUploadRequest() {
                Bytes = File.ReadAllBytes("Static/Files/pdf-processor/SKILL.md"),
                Name = "pdf-processor/SKILL.md",
                MimeType = "text/markdown"
            }]
        );
        
        Console.WriteLine($"Created skill: {skill.DisplayTitle} (ID: {skill.Id})");
        Console.WriteLine($"Created at: {skill.CreatedAt}");

        // Clean up
        bool latestVersionDeleted = await api.Skills.DeleteSkillVersionAsync(skill.Id, skill.LatestVersion);
        Console.WriteLine($"Latest version deleted: {latestVersionDeleted}");
        Console.WriteLine("\nCleaning up - deleting created skill...");
        bool deleted = await api.Skills.DeleteSkillAsync(skill.Id);
        Console.WriteLine($"Skill deleted: {deleted}");
    }

    [TornadoTest("Delete skill")]
    public static async Task DeleteSkill()
    {
        TornadoApi api = Program.Connect();
        Console.WriteLine("Listing all skills...");
        SkillListResponse skills = await api.Skills.ListSkillsAsync();

        Console.WriteLine($"Found {skills.Data.Count} skill(s):");
        int cindex = 0;
        foreach (Skill skill in skills.Data)
        {
            if(skill.Source != "custom")
            {
                continue;
            }
            Console.WriteLine($" {cindex} - {skill.DisplayTitle} (ID: {skill.Id})");
            cindex++;
        }
        if(cindex == 0)
        {
            Console.WriteLine("No custom skills found to delete.");
            return;
        }
        Console.WriteLine($"Enter the # to delete: 0 - {cindex - 1} ");
        string skillIndex = Console.ReadLine();
        if (!int.TryParse(skillIndex, out int index) || index < 0 || index >= skills.Data.Count)
        {
            Console.WriteLine("Invalid Skill ID.");
            return;
        }
        string skillId = skills.Data[index].Id;
        Console.WriteLine($"\nDeleting skill ID: {skillId} ...");
        SkillVersionListResponse versions = await api.Skills.ListSkillVersionsAsync(skillId);
        foreach (SkillVersion version in versions.Data)
        {
            Console.WriteLine($" - {version.Id} (Created at: {version.CreatedAt})");
            await api.Skills.DeleteSkillVersionAsync(version.SkillId, version.Version);
        }
        // Clean up
        Console.WriteLine("\nCleaning up - deleting created skill...");
        bool deleted = await api.Skills.DeleteSkillAsync(skillId);
        Console.WriteLine($"Skill deleted: {deleted}");
    }

    [TornadoTest("Create skill with version")]
    public static async Task CreateSkillWithVersion()
    {
        TornadoApi api = Program.Connect();

        Console.WriteLine("Creating a new skill...");
        Skill skill = await api.Skills.CreateSkillAsync(
            new CreateSkillRequest("pdf-processor", [new FileUploadRequest() {
                Bytes = File.ReadAllBytes("Static/Files/pdf-processor/SKILL.md"),
                Name = "pdf-processor/SKILL.md",
                MimeType = "text/markdown"
            }])
        );
        
        Console.WriteLine($"Created skill: {skill.DisplayTitle} (ID: {skill.Id})");
        
        Console.WriteLine("\nCreating a version for the skill...");
        SkillVersion version = await api.Skills.CreateSkillVersionAsync(
            skill.Id,
            new CreateSkillVersionRequest([new FileUploadRequest() {
                Bytes = File.ReadAllBytes("Static/Files/pdf-processor/SKILL.md"),
                Name = "pdf-processor/SKILL.md",
                MimeType = "text/markdown"
            }])
        );
        
        Console.WriteLine($"Created version: {version.Id}");
        Console.WriteLine($"Created at: {version.CreatedAt}");
        
        // Clean up
        Console.WriteLine("\nCleaning up...");
        Console.WriteLine("Deleting version...");
        bool latestVersionDeleted = await api.Skills.DeleteSkillVersionAsync(skill.Id, skill.LatestVersion);
        Console.WriteLine($"Latest version deleted: {latestVersionDeleted}");

        bool versionDeleted = await api.Skills.DeleteSkillVersionAsync(version.SkillId, version.Version);
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
            new CreateSkillRequest("pdf-processor", [new FileUploadRequest() {
                Bytes = File.ReadAllBytes("Static/Files/pdf-processor/SKILL.md"),
                Name = "pdf-processor/SKILL.md",
                MimeType = "text/markdown"
            }])
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
            new CreateSkillVersionRequest([new FileUploadRequest() {
                Bytes = File.ReadAllBytes("Static/Files/pdf-processor/SKILL.md"),
                Name = "pdf-processor/SKILL.md",
                MimeType = "text/markdown"
            }])
        );
        Console.WriteLine($"Created version: {version1.Id}");
        
        SkillVersion version2 = await api.Skills.CreateSkillVersionAsync(
            skill.Id,
            new CreateSkillVersionRequest([new FileUploadRequest() {
                Bytes = File.ReadAllBytes("Static/Files/pdf-processor/SKILL.md"),
                Name = "pdf-processor/SKILL.md",
                MimeType = "text/markdown"
            }])
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
        SkillVersion retrievedVersion = await api.Skills.GetSkillVersionAsync(skill.Id, version1.Version);
        Console.WriteLine($"Retrieved version: {retrievedVersion.Id}");
        Console.WriteLine($"System Prompt: {retrievedVersion.Description}");
        

        // DELETE (cleanup)
        Console.WriteLine("\n=== DELETE ===");
        Console.WriteLine("Deleting versions...");
        bool v1Deleted = await api.Skills.DeleteSkillVersionAsync(skill.Id, version1.Version);
        Console.WriteLine($"Version 1 deleted: {v1Deleted}");

        bool v2Deleted = await api.Skills.DeleteSkillVersionAsync(skill.Id, version2.Version);
        Console.WriteLine($"Version 2 deleted: {v2Deleted}");

        bool latestVersionDeleted = await api.Skills.DeleteSkillVersionAsync(skill.Id, skill.LatestVersion);
        Console.WriteLine($"Latest version deleted: {latestVersionDeleted}");

        Task.Delay(3000).Wait(); // Wait a moment to ensure versions are deleted before deleting skill

        Console.WriteLine("Deleting skill...");
        bool skillDeleted = await api.Skills.DeleteSkillAsync(skill.Id);
        Console.WriteLine($"Skill deleted: {skillDeleted}");
        
        Console.WriteLine("\n=== DEMO COMPLETE ===");
    }
    
    [TornadoTest("PowerPoint Skill Demo - Create presentation with container (beware cost)")]
    public static async Task PowerPointSkillDemo()
    {
        const int ArgumentPreviewLength = 200;

        Console.WriteLine("Beware: This demo may incur costs as it uses Anthropic's Claude model with the PowerPoint skill.");
        Console.WriteLine("Do you want to proceed? (y/n)");
        string? input = Console.ReadLine();
        if (input?.ToLower() != "y")
        {
            Console.WriteLine("Demo cancelled.");
            return;
        }

        Console.WriteLine("=== CREATING POWERPOINT PRESENTATION WITH SKILLS (this could take 2-3 mins) ===\n");

        ChatRequest chatRequest = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            MaxTokens = 1024,
            Messages = new List<ChatMessage>
            {
                new ChatMessage(ChatMessageRoles.User, 
                    "Create a professional PowerPoint presentation about renewable energy. " +
                    "Include 5 slides:\n" +
                    "1. Title slide: 'The Future of Renewable Energy'\n" +
                    "2. Types of Renewable Energy (solar, wind, hydro, geothermal)\n" +
                    "3. Benefits and Challenges\n" +
                    "4. Market Growth Statistics\n" +
                    "5. Call to Action\n\n" +
                    "Make it visually appealing with a professional design.")
            },
            VendorExtensions = new ChatRequestVendorExtensions
            {
                
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    // Configure container with PowerPoint skill
                    Container = new AnthropicContainer
                    {
                        Skills = new List<AnthropicSkill>
                        {
                            new AnthropicSkill("pptx", "latest")
                        }
                    },
                    BuiltInTools =
                    [
                       new VendorAnthropicChatRequestBuiltInToolCodeExecution20250825()
                    ]
                }
            }
        };

        Console.WriteLine("Sending request to Claude with PowerPoint skill...\n");

        ChatResult response = await Program.Connect().Chat.CreateChatCompletion(chatRequest);

        Console.WriteLine("=== RESPONSE ===\n");
        
        if (response.Choices?.Count > 0)
        {
            foreach (ChatChoice choice in response.Choices)
            {
                if (choice.Message?.Content is not null)
                {
                    Console.WriteLine($"Message: {choice.Message.Content}\n");
                }
                
                if (choice.Message?.ToolCalls?.Count > 0)
                {
                    Console.WriteLine("Tool Calls:");
                    foreach (ToolCall toolCall in choice.Message.ToolCalls)
                    {
                        Console.WriteLine($"  - {toolCall.FunctionCall.Name}");
                        if (!string.IsNullOrEmpty(toolCall.FunctionCall.Arguments))
                        {
                            string preview = toolCall.FunctionCall.Arguments.Length > ArgumentPreviewLength 
                                ? toolCall.FunctionCall.Arguments.Substring(0, ArgumentPreviewLength) + "..." 
                                : toolCall.FunctionCall.Arguments;
                            Console.WriteLine($"    Arguments: {preview}");
                        }
                    }
                    Console.WriteLine();
                }
            }
        }

        if (response.Usage is not null)
        {
            Console.WriteLine("=== TOKEN USAGE ===");
            Console.WriteLine($"Input tokens: {response.Usage.PromptTokens}");
            Console.WriteLine($"Output tokens: {response.Usage.CompletionTokens}");
            Console.WriteLine($"Total tokens: {response.Usage.TotalTokens}");
        }

        Console.WriteLine("\n=== DEMO COMPLETE ===");
        Console.WriteLine("Note: To download the created file, you would need to:");
        Console.WriteLine("1. Extract the file_id from the response");
        Console.WriteLine("2. Use the Files API to download the file");
        Console.WriteLine("3. Save it to disk as a .pptx file");
    }
}
