# Anthropic Skills
See [https://docs.claude.com/en/api/skills-guide](https://docs.claude.com/en/api/skills-guide) for more information.

# Full Crud Demo Review
```csharp
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

# Read a Skill
All you need is the skill ID to get name / description and other metadata.
```csharp
Skill retrievedSkill = await api.Skills.GetSkillAsync(skill.Id);
```