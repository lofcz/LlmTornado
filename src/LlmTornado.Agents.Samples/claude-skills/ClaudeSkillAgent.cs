using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Files;
using LlmTornado.Mcp;
using LlmTornado.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.claude_skills;

public class ClaudeSkillAgent
{
    public async Task<Skill> UploadSkillFile(TornadoApi api, string skillName, string fileName, string skillPath)
    {
        var file = new CreateSkillRequest(skillName, [new FileUploadRequest() {
                Bytes = File.ReadAllBytes(skillPath),
                Name = $"{skillName}/{fileName}",
                MimeType = "text/markdown"
            }]);

        Skill skill = await api.Skills.CreateSkillAsync(
          file
       );
        return skill;
    }

    public async Task<Skill> UploadSkillFolder(TornadoApi api, string skillName, string folderPath)
    {
        var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
            .Select(filePath => new FileUploadRequest()
            {
                Bytes = File.ReadAllBytes(filePath),
                Name = $"{skillName}/{Path.GetRelativePath(folderPath, filePath).Replace("\\", "/")}",
                MimeType = "text/markdown"
            }).ToList();
        var folder = new CreateSkillRequest(skillName, files.ToArray());
        Skill skill = await api.Skills.CreateSkillAsync(
          folder
       );
        return skill;
    }

    public async Task<Conversation> Invoke(TornadoApi api, ChatMessage message, string githubApiKey, List<AnthropicSkill> skills)
    {
        var localFileToolkit = MCPToolkits.FileSystemToolkit(Directory.GetCurrentDirectory());
        await localFileToolkit.InitializeAsync();

        var githubToolkit = MCPToolkits.GithubToolkit(githubApiKey);
        await githubToolkit.InitializeAsync();

        TornadoAgent agent = new TornadoAgent(api, ChatModel.Anthropic.Claude45.Sonnet250929, mcpServers: [localFileToolkit, githubToolkit]);

        agent.Options.VendorExtensions = new ChatRequestVendorExtensions
        {
            Anthropic = new ChatRequestVendorAnthropicExtensions
            {
                // Configure container with PowerPoint skill
                Container = new AnthropicContainer
                {
                    Skills = skills
                },
                BuiltInTools =
                   [
                      new VendorAnthropicChatRequestBuiltInToolCodeExecution20250825()
                   ]
            }
        };

        agent.Options.MaxTokens = 10024;

        return await agent.RunAsync(appendMessages: [message] );
    }
}
