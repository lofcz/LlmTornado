using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
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

    public async Task<Conversation> Invoke(TornadoApi api, ChatMessage message)
    {
        TornadoAgent agent = new TornadoAgent(api, ChatModel.Anthropic.Claude45.Sonnet250929);

        SkillListResponse skills = await api.Skills.ListSkillsAsync();
        Skill mySkill = skills.Data.FirstOrDefault(s => s.Source == "custom");


        agent.Options.VendorExtensions = new ChatRequestVendorExtensions
        {
            Anthropic = new ChatRequestVendorAnthropicExtensions
            {
                // Configure container with PowerPoint skill
                Container = new AnthropicContainer
                {
                    Skills = new List<AnthropicSkill>
                        {
                            new AnthropicSkill(mySkill.Id, "latest")
                        }
                },
                BuiltInTools =
                   [
                      new VendorAnthropicChatRequestBuiltInToolCodeExecution20250825()
                   ]
            }
        };

        agent.Options.MaxTokens = 1024;

        return await agent.RunAsync(appendMessages: [message] );
    }
}
