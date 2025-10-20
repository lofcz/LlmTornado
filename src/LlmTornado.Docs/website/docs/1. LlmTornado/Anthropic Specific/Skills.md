# Anthropic Skills
See [https://docs.claude.com/en/api/skills-guide](https://docs.claude.com/en/api/skills-guide) for more information.

# Quick Start
Beware the token usage when using skills, as they can increase the token count significantly.

Some built in tools include:
 - PowerPoint (pptx): Create and edit presentations
 - Excel (xlsx): Create and analyze spreadsheets
 - Word (docx): Create and edit documents
 - PDF (pdf): Generate PDF documents

```csharp
ChatRequest chatRequest = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            MaxTokens = 2048,
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

        ChatResult response = await Program.Connect().Chat.CreateChatCompletion(chatRequest);
        
        if (response.Choices?.Count > 0)
        {
            foreach (ChatChoice choice in response.Choices)
            {
                if (choice.Message?.Content is not null)
                {
                    Console.WriteLine($"Message: {choice.Message.Content}\n");
                }
            }
        }
```

# Custom Skills

## Create A Skill
```csharp

    TornadoApi api = Program.Connect();

    Console.WriteLine("Creating a new skill...");
    Skill skill = await api.Skills.CreateSkillAsync(
        "pdf-processor",
        [new FileUploadRequest() {
            Bytes = File.ReadAllBytes("Static/Files/Skills/pdf-processor/SKILL.md"),
            Name = "pdf-processor/SKILL.md",
            MimeType = "text/markdown"
        }]
    );
        
    Console.WriteLine($"Created skill: {skill.DisplayTitle} (ID: {skill.Id})");
    Console.WriteLine($"Created at: {skill.CreatedAt}");
```

## Creating A Custom Skill
To create a custom skill, you need to prepare the skill definition files and then use the `CreateSkillAsync` method.

```csharp
Skill skill = await api.Skills.CreateSkillAsync(
        new CreateSkillRequest("pdf-processor", [new FileUploadRequest() {
            Bytes = File.ReadAllBytes("Static/Files/pdf-processor/SKILL.md"),
            Name = "pdf-processor/SKILL.md",
            MimeType = "text/markdown"
        }])
    );
```

For Uploading a Folder of Files, you can use the following helper method:
```csharp
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

```

# Using A Custom Skill
```csharp
TornadoApi api = Program.Connect();

//Max 8 Skills
var skills = new List<AnthropicSkill>
                {
                    new AnthropicSkill("skill_id_1", "latest"), 
                    new AnthropicSkill("skill_id_8","latest") 
                };

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
            new VendorAnthropicChatRequestBuiltInToolCodeExecution20250825() //Required
        ],
    }
};

agent.Options.MaxTokens = 10024;
agent.Options.ReasoningBudget = 8000;
        
Conversation conv = await agent.RunAsync(appendMessages: [new ChatMessage(ChatMessageRoles.User,
    "Can you please make me an anthropic SKILL that can compile a Company Product Context based off Company PDF file extraction, web search, and related industry knowledge?")] );

Console.WriteLine(conv.Messages.Last().Content ?? "n/a");
```