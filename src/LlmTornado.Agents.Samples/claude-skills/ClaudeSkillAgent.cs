using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Code.MimeTypeMap;
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


    public async Task<Skill> UploadSkillFile(TornadoApi api, string skillName, string fileName, string filePath)
    {
        string fileExt = Path.GetExtension(filePath).ToLower();
        string mimeType = MimeTypeMap.GetMimeType(fileExt);
        var file = new CreateSkillRequest(skillName, [new FileUploadRequest() {
                Bytes = File.ReadAllBytes(filePath),
                Name = $"{skillName}/{fileName}",
                MimeType = mimeType
            }]);

        Skill skill = await api.Skills.CreateSkillAsync(
          file
       );
        return skill;
    }

    public async Task<Skill> UploadSkillFolder(TornadoApi api, string skillName, string folderPath)
    {
        
        var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
            .Select(filePath => {
                string fileExt = Path.GetExtension(filePath).ToLower();
                string mimeType = MimeTypeMap.GetMimeType(fileExt);
                return new FileUploadRequest()
                {
                    Bytes = File.ReadAllBytes(filePath),
                    Name = $"{skillName}/{Path.GetRelativePath(folderPath, filePath).Replace("\\", "/")}",
                    MimeType = mimeType
                };
            }).ToList();
        var folder = new CreateSkillRequest(skillName, files.ToArray());
        Skill skill = await api.Skills.CreateSkillAsync(
          folder
       );
        return skill;
    }

    public async Task<Conversation> Invoke(TornadoApi api, ChatMessage message,  List<AnthropicSkill> skills)
    {
        var githubToolkit = MCPToolkits.GithubToolkit(Environment.GetEnvironmentVariable("GITHUB_API_KEY"), 
            ["search_issues",
            "search_users",
            "update_issues",
            "request_copilot_review",
            "reprioritize_sub_issue",
            "remove_sub_issue",
            "merge_pull_request",
            "list_label",
            "list_tags",
            "list_sub_issues",
            "list_issue_types",
            "list_issues",
            "get_issue_comments",
            "get_teams",
            "get_team_members",
            "get_me",
            "get_label",
            "get_release_by_tag",
            "get_tag",
            "get_issue",
            "add_comment_to_pending_review",
            "add_issue_comment",
            "add_sub_issue",
            "assign_copilot_to_issue",
            "create_issue",
            "create_repository",
            "create_pending_pull_request_review",
            "create_and_submit_pull_request_review",
            "delete_pending_pull_request_review",
            "submit_pending_pull_request_review",
            "update_issue",
            ]);
        await githubToolkit.InitializeAsync();

        TornadoAgent agent = new TornadoAgent(api, ChatModel.Anthropic.Claude45.Sonnet250929, mcpServers: [githubToolkit]);

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
                   ],
            }
        };

        agent.Options.MaxTokens = 10024;
        agent.Options.ReasoningBudget = 8000;
        
        return await agent.RunAsync(appendMessages: [message] );
    }
}
