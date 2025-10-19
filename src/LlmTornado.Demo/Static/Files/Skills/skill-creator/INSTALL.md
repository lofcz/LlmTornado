# Installation Guide for Skill Creator

This guide will help you set up and use the Skill Creator skill.

## Quick Install

1. **Download the skill** (already downloaded to your PC)
2. **Extract to your skills directory**
3. **Configure GitHub authentication**
4. **Start creating skills!**

## Detailed Setup

### Step 1: Extract the Skill

```bash
# If you downloaded the tar.gz
tar -xzf skill-creator.tar.gz

# Move to your skills directory
mv skill-creator /path/to/your/skills/
```

### Step 2: GitHub Authentication Setup

Choose ONE of the following methods:

#### Option A: GitHub CLI (Recommended)

```bash
# Install GitHub CLI if not already installed
# On macOS:
brew install gh

# On Linux:
curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
sudo apt update
sudo apt install gh

# Authenticate
gh auth login
```

#### Option B: Personal Access Token

1. Go to https://github.com/settings/tokens
2. Click "Generate new token" → "Generate new token (classic)"
3. Give it a name like "Skill Creator"
4. Select scopes:
   - ✅ `repo` (Full control of private repositories)
   - ✅ `workflow` (Update GitHub Action workflows)
5. Click "Generate token"
6. Copy the token (starts with `ghp_`)

Set the token as an environment variable:

```bash
# Add to your ~/.bashrc or ~/.zshrc
export GITHUB_TOKEN="ghp_your_token_here"

# Or set it temporarily
export GITHUB_TOKEN="ghp_your_token_here"
```

#### Option C: SSH Key

```bash
# Generate SSH key if you don't have one
ssh-keygen -t ed25519 -C "your_email@example.com"

# Copy public key
cat ~/.ssh/id_ed25519.pub

# Add to GitHub:
# 1. Go to https://github.com/settings/keys
# 2. Click "New SSH key"
# 3. Paste your public key
# 4. Click "Add SSH key"

# Test connection
ssh -T git@github.com
```

### Step 3: Verify Installation

```bash
# Check scripts are executable
ls -la skill-creator/scripts/

# All scripts should have 'x' permission
# -rwxr-xr-x means executable

# Test validation script
python3 skill-creator/scripts/validate_skill.py skill-creator/
```

## Usage Examples

### Example 1: Create a Simple Skill

Just ask:
```
Create a skill called "json-validator" that validates JSON files
```

### Example 2: Create a Complex Skill

Provide details:
```
Create a skill called "api-integrator" with these steps:
1. Setup: Configure API credentials
2. Connect: Establish API connection
3. Request: Send requests with parameters
4. Parse: Process and format responses
5. Export: Save results to files

Include Python scripts for API handling.
```

### Example 3: Custom Repository

Specify repository:
```
Create a skill for my repository "myusername/custom-skills-repo"
```

## Troubleshooting

### Authentication Issues

**Problem**: `Permission denied` or `Authentication failed`

**Solutions**:
```bash
# Check GitHub CLI authentication
gh auth status

# Re-authenticate if needed
gh auth login

# Verify token has correct permissions
gh auth refresh -s repo,workflow

# Test with a simple command
gh repo view
```

### Script Permission Issues

**Problem**: `Permission denied` when running scripts

**Solution**:
```bash
# Make scripts executable
chmod +x skill-creator/scripts/*.sh
chmod +x skill-creator/scripts/*.py
```

### Python Issues

**Problem**: `ModuleNotFoundError` or Python errors

**Solution**:
```bash
# Ensure Python 3 is installed
python3 --version

# Should be Python 3.7 or higher
```

### Repository Not Found

**Problem**: `Repository not found: 404`

**Solutions**:
- Verify repository name is correct
- Ensure you have access to the repository
- Check if repository is private (need appropriate token permissions)
- Confirm repository exists: `gh repo view owner/repo`

## Advanced Configuration

### Custom Repository Settings

Create a config file: `~/.skill-creator-config`

```bash
REPO_OWNER="your-username"
REPO_NAME="Agent-Skills"
BASE_BRANCH="main"
```

Source it before using:
```bash
source ~/.skill-creator-config
```

### Automated Workflow

Create a script for repetitive tasks:

```bash
#!/bin/bash
# create-and-deploy-skill.sh

SKILL_NAME="$1"
SKILL_DESC="$2"

# Generate skill
python3 skill-creator/scripts/skill_generator.py config.json

# Validate
python3 skill-creator/scripts/validate_skill.py "/tmp/$SKILL_NAME"

# Setup repo
./skill-creator/scripts/setup_skill_repo.sh "$SKILL_NAME" "/tmp/$SKILL_NAME" "$REPO_OWNER"

# Create PR
./skill-creator/scripts/create_pr.sh "$SKILL_NAME" "$REPO_OWNER"

echo "✓ Skill created and deployed!"
```

## Integration with Anthropic Claude

This skill is designed to work seamlessly with Claude. Just describe what skill you want to create, and Claude will:

1. ✅ Generate the skill structure
2. ✅ Create all necessary files
3. ✅ Validate the skill
4. ✅ Push to GitHub
5. ✅ Create a pull request
6. ✅ Download to your PC

## File Structure

```
skill-creator/
├── SKILL.md              # Main skill workflow
├── README.md             # Documentation
├── INSTALL.md            # This file
└── scripts/
    ├── skill_generator.py    # Generates skills from specs
    ├── validate_skill.py     # Validates skill structure
    ├── setup_skill_repo.sh   # Sets up Git repository
    └── create_pr.sh          # Creates pull request
```

## Support

### Getting Help

1. **Check the README**: Most questions are answered there
2. **Review SKILL.md**: See the complete workflow
3. **GitHub Issues**: Open an issue on the repository
4. **Examples**: Look at existing skills for patterns

### Reporting Bugs

When reporting issues, include:
- Exact command or request used
- Full error message
- Operating system
- Python version (`python3 --version`)
- Git version (`git --version`)
- Authentication method used

## Updates

To update the skill:

```bash
cd /path/to/Agent-Skills
git pull origin main

# Your skill-creator will be updated
```

## Uninstallation

To remove the skill:

```bash
# Remove from skills directory
rm -rf /path/to/skills/skill-creator

# Remove from repository
cd /path/to/Agent-Skills
git rm -r skills/skill-creator
git commit -m "Remove skill-creator"
git push
```

---

**Version**: 1.0.0  
**Last Updated**: 2024  
**Compatibility**: Claude 3.x with MCP tools
