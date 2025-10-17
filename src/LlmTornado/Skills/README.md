# Anthropic Skills API

This directory contains the implementation of the Anthropic Skills API for LlmTornado.

## Overview

The Skills API allows you to create specialized prompts and configurations that Claude can automatically select and use for specific tasks. Skills provide a way to customize Claude's behavior for different use cases.

## Features

### CRUD Operations for Skills

- **List Skills**: Get all skills for your authenticated account
- **Create Skill**: Create a new skill with a name and description
- **Get Skill**: Retrieve a specific skill by ID
- **Update Skill**: Update a skill's name, description, or active version
- **Delete Skill**: Remove a skill

### Skill Version Management

- **List Versions**: Get all versions of a skill
- **Create Version**: Create a new version with a system prompt
- **Get Version**: Retrieve a specific version
- **Delete Version**: Remove a version

## Usage Example

```csharp
using LlmTornado;
using LlmTornado.Skills;

// Initialize the API with Anthropic authentication
TornadoApi api = new TornadoApi("your-anthropic-api-key", LLmProviders.Anthropic);

// Create a new skill
Skill skill = await api.Skills.CreateSkillAsync(
    "Code Review Assistant",
    "Specialized skill for reviewing code and providing constructive feedback"
);

// Create a version with a system prompt
SkillVersion version = await api.Skills.CreateSkillVersionAsync(
    skill.Id,
    "You are an expert code reviewer. Analyze code for bugs, " +
    "performance issues, and best practices. Provide constructive feedback."
);

// Set the active version
await api.Skills.UpdateSkillAsync(skill.Id, new UpdateSkillRequest
{
    ActiveVersionId = version.Id
});

// List all skills
SkillListResponse skills = await api.Skills.ListSkillsAsync();

// Get a specific skill
Skill retrievedSkill = await api.Skills.GetSkillAsync(skill.Id);

// List versions of a skill
SkillVersionListResponse versions = await api.Skills.ListSkillVersionsAsync(skill.Id);

// Clean up
await api.Skills.DeleteSkillVersionAsync(skill.Id, version.Id);
await api.Skills.DeleteSkillAsync(skill.Id);
```

## Demo

A comprehensive demo is available in `src/LlmTornado.Demo/SkillsDemo.cs` that showcases all CRUD operations and version management features.

To run the demo:

1. Set up your API key in `apiKey.json`
2. Run the LlmTornado.Demo project
3. Select the Skills demo from the menu

## API Reference

### SkillsEndpoint Methods

#### ListSkillsAsync
```csharp
Task<SkillListResponse> ListSkillsAsync(int? limit = null, string? before = null, string? after = null)
```
Lists all skills with optional pagination.

#### CreateSkillAsync
```csharp
Task<Skill> CreateSkillAsync(CreateSkillRequest request)
Task<Skill> CreateSkillAsync(string name, string? description = null)
```
Creates a new skill.

#### GetSkillAsync
```csharp
Task<Skill> GetSkillAsync(string skillId)
```
Retrieves a specific skill by ID.

#### UpdateSkillAsync
```csharp
Task<Skill> UpdateSkillAsync(string skillId, UpdateSkillRequest request)
```
Updates a skill's properties.

#### DeleteSkillAsync
```csharp
Task<bool> DeleteSkillAsync(string skillId)
```
Deletes a skill.

#### ListSkillVersionsAsync
```csharp
Task<SkillVersionListResponse> ListSkillVersionsAsync(string skillId, int? limit = null, string? before = null, string? after = null)
```
Lists all versions of a skill.

#### CreateSkillVersionAsync
```csharp
Task<SkillVersion> CreateSkillVersionAsync(string skillId, CreateSkillVersionRequest request)
Task<SkillVersion> CreateSkillVersionAsync(string skillId, string systemPrompt)
```
Creates a new version of a skill.

#### GetSkillVersionAsync
```csharp
Task<SkillVersion> GetSkillVersionAsync(string skillId, string versionId)
```
Retrieves a specific version.

#### DeleteSkillVersionAsync
```csharp
Task<bool> DeleteSkillVersionAsync(string skillId, string versionId)
```
Deletes a version.

## Models

### Skill
- `Id`: Unique identifier
- `Type`: Always "skill"
- `Name`: Skill name
- `Description`: Optional description
- `ActiveVersionId`: ID of the currently active version
- `CreatedAt`: Creation timestamp
- `UpdatedAt`: Last update timestamp

### SkillVersion
- `Id`: Unique identifier
- `Type`: Always "skill_version"
- `SkillId`: ID of the parent skill
- `SystemPrompt`: The system prompt for this version
- `Metadata`: Optional metadata dictionary
- `CreatedAt`: Creation timestamp

### CreateSkillRequest
- `Name`: Required skill name
- `Description`: Optional description

### UpdateSkillRequest
- `Name`: Optional new name
- `Description`: Optional new description
- `ActiveVersionId`: Optional ID of version to activate

### CreateSkillVersionRequest
- `SystemPrompt`: Optional system prompt
- `Metadata`: Optional metadata dictionary

## Notes

- The Skills API is only available with the Anthropic provider
- Skills are scoped to your authenticated account
- System prompts in skill versions can be used to customize Claude's behavior
- Active versions can be set to control which version is used by default

## References

- [Anthropic Skills Guide](https://docs.claude.com/en/api/skills-guide)
- [Skills API Reference](https://docs.claude.com/en/api/skills/list-skills)
- [Skill Versions API](https://docs.claude.com/en/api/skills/create-skill-version)
