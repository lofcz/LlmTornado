using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Code.MimeTypeMap;
using LlmTornado.Files;
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

        TornadoAgent agent = new TornadoAgent(api, ChatModel.Anthropic.Claude45.Sonnet250929);

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
