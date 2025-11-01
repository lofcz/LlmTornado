#!/bin/bash
# Skill Repository Setup Script
# Usage: ./setup_skill_repo.sh <skill-name> <skill-source-dir> <repo-owner> <repo-name>

set -e

SKILL_NAME="$1"
SKILL_SOURCE="$2"
REPO_OWNER="$3"
REPO_NAME="${4:-Agent-Skills}"
BRANCH_NAME="add-${SKILL_NAME}-skill"
WORK_DIR="/tmp/repo-work"

if [ -z "$SKILL_NAME" ] || [ -z "$SKILL_SOURCE" ] || [ -z "$REPO_OWNER" ]; then
    echo "Usage: $0 <skill-name> <skill-source-dir> <repo-owner> [repo-name]"
    echo "Example: $0 data-analyzer /tmp/data-analyzer myusername Agent-Skills"
    exit 1
fi

echo "========================================="
echo "Setting Up Skill Repository"
echo "========================================="
echo "Skill: $SKILL_NAME"
echo "Source: $SKILL_SOURCE"
echo "Repository: $REPO_OWNER/$REPO_NAME"
echo "Branch: $BRANCH_NAME"
echo ""

# Clean up any existing work directory
rm -rf "$WORK_DIR"
mkdir -p "$WORK_DIR"

# Determine repository URL based on available authentication
REPO_URL=""
if command -v gh &> /dev/null && gh auth status &> /dev/null; then
    echo "✓ Using GitHub CLI authentication"
    REPO_URL="https://github.com/$REPO_OWNER/$REPO_NAME.git"
elif [ -n "$GITHUB_TOKEN" ]; then
    echo "✓ Using GitHub Token authentication"
    REPO_URL="https://${GITHUB_TOKEN}@github.com/$REPO_OWNER/$REPO_NAME.git"
elif [ -f "$HOME/.ssh/id_rsa" ] || [ -f "$HOME/.ssh/id_ed25519" ]; then
    echo "✓ Using SSH authentication"
    REPO_URL="git@github.com:$REPO_OWNER/$REPO_NAME.git"
else
    echo "✗ No authentication method found"
    echo "Please set up one of:"
    echo "  - GitHub CLI: gh auth login"
    echo "  - Token: export GITHUB_TOKEN='your_token'"
    echo "  - SSH key: Add to ~/.ssh/"
    exit 1
fi

# Clone the repository
echo "Cloning repository..."
cd "$WORK_DIR"
git clone "$REPO_URL" repo
cd repo

# Configure git if needed
git config user.email "skill-creator@anthropic.com" || true
git config user.name "Skill Creator" || true

# Get default branch
DEFAULT_BRANCH=$(git symbolic-ref refs/remotes/origin/HEAD 2>/dev/null | sed 's@^refs/remotes/origin/@@' || echo "main")
echo "Default branch: $DEFAULT_BRANCH"

# Create and checkout new branch
echo "Creating branch: $BRANCH_NAME"
git checkout -b "$BRANCH_NAME"

# Create skills directory if it doesn't exist
mkdir -p "skills/$SKILL_NAME"

# Copy skill files
echo "Copying skill files..."
cp -r "$SKILL_SOURCE"/* "skills/$SKILL_NAME/"

# List what was copied
echo ""
echo "Files added:"
ls -la "skills/$SKILL_NAME/"
echo ""

# Stage all changes
git add "skills/$SKILL_NAME/"

# Check if there are changes to commit
if git diff --cached --quiet; then
    echo "⚠ No changes to commit"
    exit 0
fi

# Commit changes
echo "Committing changes..."
git commit -m "Add $SKILL_NAME skill

- Complete SKILL.md with workflow steps
- Documentation and usage examples
- Supporting scripts and utilities"

# Push to GitHub
echo "Pushing to GitHub..."
git push -u origin "$BRANCH_NAME"

echo ""
echo "========================================="
echo "✓ Repository Setup Complete"
echo "========================================="
echo "Branch: $BRANCH_NAME"
echo "Ready to create pull request!"
echo ""
