# Skill Creator

A comprehensive Anthropic skill for generating new Skills with complete GitHub workflow integration.

## Overview

This skill automates the entire process of creating new Anthropic Skills, including:
- Structured SKILL.md generation
- GitHub repository integration
- Automated pull request creation
- Local download verification

## Features

- ✨ **Template-based generation**: Creates properly formatted SKILL.md files
- 🔄 **GitHub workflow**: Automatic branch creation and PR submission
- 📦 **Local download**: Downloads skill to your PC with verification
- ✅ **Progress tracking**: Built-in checklist for workflow steps
- 🛠️ **Flexible authentication**: Supports PAT, SSH, and GitHub CLI

## Usage

### Quick Start

```
Create a skill called "data-analyzer" that processes CSV files and generates reports
```

### With Specifications

```
Create a skill with the following:
- Name: api-integrator
- Description: Integrates with external APIs
- Steps: Setup, authentication, request handling, response parsing
- Outputs: Structured JSON data
```

## Prerequisites

### For GitHub Integration

Choose one authentication method:

1. **Personal Access Token (PAT)**:
   ```bash
   export GITHUB_TOKEN="ghp_yourTokenHere"
   ```

2. **GitHub CLI** (Recommended):
   ```bash
   gh auth login
   ```

3. **SSH Key**:
   - Add your SSH public key to GitHub
   - Configure git to use SSH URLs

## Skill Structure

The generated skill follows this structure:

```
skill-name/
├── SKILL.md          # Main skill workflow
├── README.md         # Documentation (optional)
└── scripts/          # Supporting scripts (if needed)
    ├── helper.py
    └── setup.sh
```

## Workflow Steps

1. **Requirements Gathering**: Collect specifications
2. **Generation**: Create SKILL.md and supporting files
3. **Validation**: Check completeness and formatting
4. **Git Operations**: Branch creation and commit
5. **PR Creation**: Submit pull request to repository
6. **Download**: Copy to local disk
7. **Verification**: Confirm successful download

## Configuration

### Repository Settings

Default repository: `Agent-Skills`
Default branch: `main`

To use a different repository, specify in your request:
```
Create a skill for my repository "my-org/my-skills-repo"
```

### Branch Naming

Format: `add-[skill-name]-skill`

Example: `add-data-analyzer-skill`

## Examples

### Basic Skill

```
Create a skill that validates JSON files
```

### Advanced Skill

```
Create a skill called "ml-pipeline" that:
1. Loads data from multiple sources
2. Performs data cleaning and validation
3. Trains machine learning models
4. Generates performance reports
5. Deploys models to production

Include Python scripts for each step.
```

## Output

After completion, you'll receive:

1. **Skill files** in `/tmp/skill-name/`
2. **GitHub branch**: `add-skill-name-skill`
3. **Pull request** URL
4. **Downloaded files** on your local PC
5. **Verification report**

## Troubleshooting

### Authentication Issues

**Problem**: `Permission denied (publickey)` or `Authentication failed`

**Solution**: 
- Verify your GitHub token has `repo` scope
- Check SSH key is added to GitHub account
- Try GitHub CLI: `gh auth status`

### Branch Already Exists

**Problem**: `Branch 'add-skill-name-skill' already exists`

**Solution**:
- Use a different skill name
- Delete the existing branch on GitHub
- Specify a custom branch name

### Push Rejected

**Problem**: `Updates were rejected`

**Solution**:
- Ensure you have write access to repository
- Check repository isn't archived
- Verify branch protection rules

## Best Practices

1. **Clear naming**: Use descriptive, kebab-case names
2. **Detailed steps**: Each step should be actionable
3. **Progress tracking**: Include checklist for users
4. **Documentation**: Add README for complex skills
5. **Testing**: Validate workflow before submitting PR

## Contributing

This skill itself can be updated via pull request to improve:
- Template structure
- GitHub integration
- Verification process
- Documentation

## License

MIT License - Free to use and modify

## Support

For issues or questions:
- Open an issue on GitHub
- Check existing skills for examples
- Review the SKILL.md for detailed workflow

---

**Version**: 1.0.0  
**Last Updated**: 2024  
**Author**: Anthropic Skill Creator Team
