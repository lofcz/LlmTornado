---
name: skill-generator
description: Generates complete Anthropic SKILL packages with proper structure, documentation, and automated download verification.
---

# Skill Generator - Create Anthropic Skills with Download Verification

## Trigger Words
- "create a skill"
- "make a skill"
- "generate a skill"
- "build a skill"
- "new skill"
- "skill generator"

## Description
This skill generates complete Anthropic SKILL packages with proper structure, documentation, and automated download verification. It ensures all files are successfully transferred to the user's local machine.

## Workflow

### Phase 1: Requirements Gathering
1. Ask the user for skill specifications:
   - **Skill name**: What should the skill be called?
   - **Description**: What does the skill do?
   - **Trigger words**: What words/phrases activate this skill?
   - **Functionality**: What are the main features?
   - **Required files**: Does it need Python scripts, bash scripts, config files?

### Phase 2: Skill Creation
1. Create a directory structure: `/tmp/skill-{skillname}/`
2. Generate the following files:
   - **SKILL.md**: Main skill documentation with:
     - Trigger words section
     - Description
     - Workflow/instructions
     - Usage examples
     - Technical details
   - **README.md**: User-facing documentation
   - **Any additional files** needed (Python scripts, bash scripts, configs, etc.)

### Phase 3: File Verification (Pre-Download)
1. List all created files in `/tmp/skill-{skillname}/`
2. Display file count and names to user
3. Ask user to confirm files look correct before download

### Phase 4: Download to User Machine
1. Copy all files from `/tmp/skill-{skillname}/` to `$OUTPUT_DIR/skill-{skillname}/`
2. Maintain directory structure during copy
3. Display success message with file list

### Phase 5: Post-Download Verification
1. Ask user to verify files are on their local machine:
   - "Please check your downloads folder for the 'skill-{skillname}' directory"
   - "Can you confirm you see the following files: [list files]"
2. Wait for user confirmation
3. If user confirms success:
   - Provide installation instructions
   - Explain how to use the skill
4. If user reports missing files:
   - Troubleshoot the issue
   - Offer to regenerate and re-download specific files

### Phase 6: Installation Guidance
1. Provide clear instructions:
   ```
   To install this skill:
   1. Move the 'skill-{skillname}' folder to your skills directory
   2. Ensure the folder structure is: skills/skill-{skillname}/SKILL.md
   3. Test the skill by using one of these trigger phrases: {list triggers}
   ```

## File Structure Template

Every skill should follow this structure:
```
skill-{skillname}/
├── SKILL.md           # Main skill documentation (REQUIRED)
├── README.md          # User-facing documentation (RECOMMENDED)
├── examples/          # Example files (OPTIONAL)
├── scripts/           # Python/Bash scripts (OPTIONAL)
└── configs/           # Configuration files (OPTIONAL)
```

## SKILL.md Template

```markdown
# {Skill Name} - {Brief Description}

## Trigger Words
- "trigger phrase 1"
- "trigger phrase 2"
- "trigger phrase 3"

## Description
{Detailed description of what the skill does}

## Workflow
1. Step 1
2. Step 2
3. Step 3

## Usage Examples
**Example 1:**
User: "{example user message}"
Assistant: {what the assistant should do}

## Technical Details
- {Any technical requirements}
- {Dependencies or prerequisites}
- {Special considerations}

## Files Included
- **SKILL.md**: This file
- **{other files}**: {description}

## Notes
{Any additional notes or warnings}
```

## Implementation Notes

### Creating Directory Structure
```bash
mkdir -p /tmp/skill-{skillname}/scripts
mkdir -p /tmp/skill-{skillname}/examples
```

### Copying to Output
```bash
# Copy entire directory structure to OUTPUT_DIR
cp -r /tmp/skill-{skillname} $OUTPUT_DIR/
```

### File Verification
```bash
# Before download - show what will be downloaded
echo "Files ready for download:"
find /tmp/skill-{skillname} -type f -exec basename {} \;

# Count files
find /tmp/skill-{skillname} -type f | wc -l
```

## Best Practices

1. **Always ask for requirements first** - Don't assume what the user needs
2. **Keep SKILL.md clear and concise** - This is what the AI reads
3. **Make README.md user-friendly** - This is for humans
4. **Include examples** - Show how the skill should be used
5. **Verify before download** - Prevent unnecessary re-downloads
6. **Confirm after download** - Ensure files reached the user
7. **Provide clear installation steps** - Don't leave users guessing

## Error Handling

### If files don't download:
1. Check OUTPUT_DIR is accessible
2. Verify files exist in /tmp
3. Try copying individual files
4. Offer to create a zip archive as backup

### If user can't find files:
1. Ask where they're checking
2. Explain typical download locations
3. Provide file names to search for
4. Offer to regenerate if needed

## Example Interaction

**User**: "Create a skill for processing CSV files"

**Assistant**: 
"I'll help you create a CSV processing skill! Let me gather some details:

1. What should this skill be called? (e.g., 'csv-processor')
2. What specific CSV operations should it perform? (e.g., filtering, merging, transforming)
3. Should it include any Python scripts for data manipulation?
4. Any specific trigger phrases you'd like? (I'll suggest: 'process csv', 'csv tools', 'analyze csv')

Please provide these details so I can create the perfect skill for you!"

## Security Considerations

1. Validate all user inputs for file names (no special characters that could break paths)
2. Keep all operations within allowed directories
3. Don't include sensitive information in skill files
4. Sanitize any code examples to prevent injection

## Maintenance

When updating this skill:
1. Test the complete workflow end-to-end
2. Verify download mechanism works
3. Ensure verification steps are clear
4. Update examples if workflow changes
