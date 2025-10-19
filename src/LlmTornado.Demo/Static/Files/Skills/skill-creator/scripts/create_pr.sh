#!/bin/bash
# GitHub Pull Request Creation Script
# Usage: ./create_pr.sh <skill-name> <repo-owner> <repo-name>

set -e

SKILL_NAME="$1"
REPO_OWNER="$2"
REPO_NAME="${3:-Agent-Skills}"
BRANCH_NAME="add-${SKILL_NAME}-skill"
BASE_BRANCH="${4:-main}"

if [ -z "$SKILL_NAME" ] || [ -z "$REPO_OWNER" ]; then
    echo "Usage: $0 <skill-name> <repo-owner> [repo-name] [base-branch]"
    echo "Example: $0 data-analyzer myusername Agent-Skills main"
    exit 1
fi

echo "========================================="
echo "GitHub PR Creation for Skill: $SKILL_NAME"
echo "========================================="
echo "Repository: $REPO_OWNER/$REPO_NAME"
echo "Branch: $BRANCH_NAME"
echo "Base: $BASE_BRANCH"
echo ""

# Check if GitHub CLI is available
if command -v gh &> /dev/null; then
    echo "✓ GitHub CLI detected"
    echo "Creating pull request..."
    
    gh pr create \
        --repo "$REPO_OWNER/$REPO_NAME" \
        --title "Add $SKILL_NAME skill" \
        --body "## Description

This PR adds a new skill: **$SKILL_NAME**

## What's Included
- SKILL.md with complete workflow and progress tracking
- README.md with usage instructions and examples
- Supporting scripts and utilities (if applicable)

## Validation
- [x] Proper markdown formatting
- [x] Clear, sequential workflow steps
- [x] Progress tracking checklist
- [x] Documentation complete
- [x] All required files included

## Testing
- [x] Structure validated
- [x] Workflow tested
- [x] Ready for review

## Type of Change
- [x] New skill addition
- [ ] Bug fix
- [ ] Enhancement
- [ ] Documentation update

Please review and merge when ready!" \
        --base "$BASE_BRANCH" \
        --head "$BRANCH_NAME"
    
    echo "✓ Pull request created successfully!"
    gh pr view --repo "$REPO_OWNER/$REPO_NAME"
    
elif [ -n "$GITHUB_TOKEN" ]; then
    echo "✓ GitHub token detected"
    echo "Creating pull request via API..."
    
    PR_BODY="## Description\n\nThis PR adds a new skill: **$SKILL_NAME**\n\n## What's Included\n- SKILL.md with complete workflow\n- README.md with documentation\n- Supporting scripts (if applicable)\n\n## Validation\n- [x] Structure validated\n- [x] Workflow tested\n- [x] Documentation complete"
    
    RESPONSE=$(curl -s -X POST \
        -H "Authorization: token $GITHUB_TOKEN" \
        -H "Accept: application/vnd.github.v3+json" \
        "https://api.github.com/repos/$REPO_OWNER/$REPO_NAME/pulls" \
        -d "{
            \"title\": \"Add $SKILL_NAME skill\",
            \"body\": \"$PR_BODY\",
            \"head\": \"$BRANCH_NAME\",
            \"base\": \"$BASE_BRANCH\"
        }")
    
    PR_URL=$(echo "$RESPONSE" | grep -o '"html_url": "[^"]*' | head -1 | cut -d'"' -f4)
    
    if [ -n "$PR_URL" ]; then
        echo "✓ Pull request created successfully!"
        echo "PR URL: $PR_URL"
    else
        echo "✗ Failed to create pull request"
        echo "Response: $RESPONSE"
        exit 1
    fi
    
else
    echo "⚠ No GitHub authentication found"
    echo ""
    echo "Please create the pull request manually:"
    echo "URL: https://github.com/$REPO_OWNER/$REPO_NAME/compare/$BASE_BRANCH...$BRANCH_NAME"
    echo ""
    echo "Or set up authentication:"
    echo "  1. GitHub CLI: gh auth login"
    echo "  2. Token: export GITHUB_TOKEN='your_token'"
    exit 1
fi

echo ""
echo "========================================="
echo "✓ PR Creation Complete"
echo "========================================="
