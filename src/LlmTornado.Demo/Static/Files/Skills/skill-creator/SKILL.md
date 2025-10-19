---
name: skill-creator
description: Generates Anthropic Skills with complete workflow including GitHub PR creation and local download verification.
---

## Skill Creation and GitHub Workflow

Copy this checklist and track your progress:

```
Skill Creation Progress:
- [ ] Step 1: Gather skill requirements and specifications
- [ ] Step 2: Generate skill structure and SKILL.md
- [ ] Step 3: Create supporting scripts and files
- [ ] Step 4: Validate skill completeness
- [ ] Step 5: Prepare GitHub repository structure
- [ ] Step 6: Create and push Git branch
- [ ] Step 7: Generate pull request
- [ ] Step 8: Download to local disk
- [ ] Step 9: Verify local download
```

## **Step 1: Gather skill requirements and specifications**

Collect the following information from the user:
- **Skill name**: Kebab-case name (e.g., "data-analyzer")
- **Description**: Brief one-line description
- **Primary functionality**: What the skill does
- **Workflow steps**: Sequential steps for execution
- **Required tools**: Any external tools or dependencies
- **Trigger words**: Keywords that activate this skill
- **Expected inputs**: What the user provides
- **Expected outputs**: What the skill produces

## **Step 2: Generate skill structure and SKILL.md**

Create the main SKILL.md file with this structure:

```markdown
---
name: [skill-name]
description: [Brief description of what this skill does]
---

## [Skill Title]

Copy this checklist and track your progress:

```
[Skill Name] Progress:
- [ ] Step 1: [First step]
- [ ] Step 2: [Second step]
- [ ] Step 3: [Third step]
...
```

## **Step 1: [First Step Name]**
[Detailed instructions for first step]

## **Step 2: [Second Step Name]**
[Detailed instructions for second step]

[Continue for all steps...]
```

## **Step 3: Create supporting scripts and files**

If the skill requires automation, create:
- **Python scripts**: For complex logic or API interactions
- **Bash scripts**: For system operations and Git workflows
- **Config files**: For settings and parameters
- **README.md**: Usage instructions and examples

## **Step 4: Validate skill completeness**

Check that the skill includes:
- [ ] Clear, sequential workflow steps
- [ ] Specific, actionable instructions
- [ ] Progress tracking checklist
- [ ] All necessary files
- [ ] Proper markdown formatting
- [ ] Front matter with name and description

## **Step 5: Prepare GitHub repository structure**

Create the skill in this structure:
```
/tmp/[skill-name]/
├── SKILL.md
├── README.md (optional)
└── [other supporting files]
```

## **Step 6: Create and push Git branch**

Execute the following Git workflow:

```bash
# Clone the repository (if not already present)
cd /tmp
git clone https://github.com/[username]/Agent-Skills.git
cd Agent-Skills

# Create a new branch for the skill
git checkout -b add-[skill-name]-skill

# Copy skill files to the repository
mkdir -p skills/[skill-name]
cp -r /tmp/[skill-name]/* skills/[skill-name]/

# Stage and commit changes
git add skills/[skill-name]/
git commit -m "Add [skill-name] skill: [brief description]"

# Push to GitHub
git push origin add-[skill-name]-skill
```

**Note**: This requires GitHub authentication. User must provide:
- GitHub Personal Access Token (PAT), OR
- SSH key configuration, OR
- GitHub CLI authentication

## **Step 7: Generate pull request**

Create a PR using one of these methods:

### Method A: GitHub CLI (Recommended)
```bash
gh pr create \
  --title "Add [skill-name] skill" \
  --body "This PR adds a new skill for [description].\n\n## What's included:\n- SKILL.md with complete workflow\n- [other files]\n\n## Testing:\n- [ ] Validated structure\n- [ ] Tested workflow\n- [ ] Documentation complete" \
  --base main \
  --head add-[skill-name]-skill
```

### Method B: GitHub API
```bash
curl -X POST \
  -H "Authorization: token ${GITHUB_TOKEN}" \
  -H "Accept: application/vnd.github.v3+json" \
  https://api.github.com/repos/[username]/Agent-Skills/pulls \
  -d '{
    "title": "Add [skill-name] skill",
    "body": "Description of changes",
    "head": "add-[skill-name]-skill",
    "base": "main"
  }'
```

### Method C: Manual (if API unavailable)
Provide the user with:
- Branch name: `add-[skill-name]-skill`
- Direct URL to create PR: `https://github.com/[username]/Agent-Skills/compare/main...add-[skill-name]-skill`

## **Step 8: Download to local disk**

Copy the skill to OUTPUT_DIR for local download:

```bash
# Copy all skill files to output directory
cp -r /tmp/[skill-name] $OUTPUT_DIR/

# Create a packaged version
cd /tmp
tar -czf [skill-name].tar.gz [skill-name]/
cp [skill-name].tar.gz $OUTPUT_DIR/
```

## **Step 9: Verify local download**

Perform verification:

```bash
# Verify files exist before copying to OUTPUT_DIR
ls -la /tmp/[skill-name]/
cat /tmp/[skill-name]/SKILL.md | head -20

# After copying, confirm in persistent storage
ls -la /tmp/[skill-name]/
echo "✓ Skill files ready for download"
echo "✓ Location: /tmp/[skill-name]/"
echo "✓ Files will be automatically downloaded"
```

Provide user confirmation:
- ✓ Skill created: `/tmp/[skill-name]/`
- ✓ GitHub branch pushed: `add-[skill-name]-skill`
- ✓ Pull request created: [PR URL]
- ✓ Files downloaded to your PC
- ✓ Verification complete

## Additional Notes

### GitHub Authentication Options

1. **Personal Access Token (PAT)**:
   - Create at: https://github.com/settings/tokens
   - Required scopes: `repo`, `workflow`
   - Use: `export GITHUB_TOKEN="your_token_here"`

2. **GitHub CLI**:
   - Pre-authenticate with: `gh auth login`
   - Handles PR creation seamlessly

3. **SSH Keys**:
   - Add SSH key to GitHub account
   - Use SSH URLs for git operations

### Troubleshooting

- **Authentication failed**: Check token permissions
- **Branch already exists**: Use unique branch name
- **Push rejected**: Ensure repository access
- **PR creation failed**: Create manually using provided URL

### Best Practices

- Use descriptive skill names
- Include comprehensive documentation
- Test skill workflow before PR
- Keep steps clear and actionable
- Provide examples when helpful
- Update progress checklist as you go
