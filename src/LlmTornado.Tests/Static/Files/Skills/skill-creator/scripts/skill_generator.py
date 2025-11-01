#!/usr/bin/env python3
"""
Skill Generator Script
Generates a complete skill structure from specifications
"""

import os
import sys
import json
from pathlib import Path
from typing import List, Dict, Optional

class SkillGenerator:
    def __init__(self, output_dir: str = "/tmp"):
        self.output_dir = Path(output_dir)
        
    def generate_skill(self, 
                      skill_name: str,
                      description: str,
                      steps: List[Dict[str, str]],
                      include_readme: bool = True,
                      include_scripts: bool = False) -> Path:
        """
        Generate a complete skill structure
        
        Args:
            skill_name: Kebab-case name (e.g., "data-analyzer")
            description: Brief description of the skill
            steps: List of dicts with 'name' and 'description' keys
            include_readme: Whether to generate README.md
            include_scripts: Whether to create scripts directory
            
        Returns:
            Path to the generated skill directory
        """
        # Create skill directory
        skill_dir = self.output_dir / skill_name
        skill_dir.mkdir(parents=True, exist_ok=True)
        
        # Generate SKILL.md
        self._generate_skill_md(skill_dir, skill_name, description, steps)
        
        # Generate README.md if requested
        if include_readme:
            self._generate_readme(skill_dir, skill_name, description, steps)
        
        # Create scripts directory if requested
        if include_scripts:
            scripts_dir = skill_dir / "scripts"
            scripts_dir.mkdir(exist_ok=True)
            self._generate_example_script(scripts_dir)
        
        return skill_dir
    
    def _generate_skill_md(self, skill_dir: Path, name: str, description: str, steps: List[Dict]):
        """Generate the SKILL.md file"""
        
        # Convert steps to checklist items
        checklist_items = [f"- [ ] Step {i+1}: {step['name']}" 
                          for i, step in enumerate(steps)]
        checklist = "\n".join(checklist_items)
        
        # Generate step sections
        step_sections = []
        for i, step in enumerate(steps):
            section = f"""## **Step {i+1}: {step['name']}**

{step['description']}"""
            step_sections.append(section)
        
        steps_content = "\n\n".join(step_sections)
        
        # Create title from name
        title = name.replace('-', ' ').title()
        
        # Generate complete SKILL.md content
        content = f"""---
name: {name}
description: {description}
---

## {title}

Copy this checklist and track your progress:

```
{title} Progress:
{checklist}
```

{steps_content}

## Additional Notes

### Tips for Success

- Follow each step in order
- Check off items as you complete them
- Review output at each stage
- Refer to documentation when needed

### Common Issues

- **Issue**: [Common problem]
  - **Solution**: [How to fix it]

### Best Practices

- Keep steps organized and documented
- Validate outputs before proceeding
- Save intermediate results
- Test thoroughly before finalizing

## Resources

- Related skills: [list any related skills]
- Documentation: [relevant docs]
- Examples: [example use cases]
"""
        
        skill_md = skill_dir / "SKILL.md"
        with open(skill_md, 'w', encoding='utf-8') as f:
            f.write(content)
        
        print(f"✓ Generated {skill_md}")
    
    def _generate_readme(self, skill_dir: Path, name: str, description: str, steps: List[Dict]):
        """Generate README.md file"""
        
        title = name.replace('-', ' ').title()
        
        # Create step list for README
        step_list = "\n".join([f"{i+1}. **{step['name']}**: {step['description'][:100]}..." 
                               for i, step in enumerate(steps)])
        
        content = f"""# {title}

{description}

## Overview

This skill provides a structured workflow for {description.lower()}.

## Features

- ✅ Step-by-step workflow
- 📋 Progress tracking checklist
- 📚 Comprehensive documentation
- 🔧 Automated processes

## Quick Start

To use this skill, simply trigger it with relevant keywords related to {name.replace('-', ' ')}.

### Example Usage

```
[Example usage command or trigger phrase]
```

## Workflow

{step_list}

## Requirements

- [List any prerequisites]
- [Required tools or dependencies]
- [Access credentials if needed]

## Output

This skill produces:
- [Output type 1]
- [Output type 2]
- [Output type 3]

## Configuration

No configuration required by default. Advanced users can customize:
- [Configuration option 1]
- [Configuration option 2]

## Troubleshooting

### Common Issues

**Problem**: [Issue description]
- **Solution**: [How to resolve]

**Problem**: [Another issue]
- **Solution**: [How to resolve]

## Examples

### Example 1: [Use Case]

```
[Example input]
```

Expected output:
```
[Example output]
```

### Example 2: [Another Use Case]

```
[Example input]
```

Expected output:
```
[Example output]
```

## Best Practices

1. [Best practice 1]
2. [Best practice 2]
3. [Best practice 3]

## Contributing

Improvements and suggestions are welcome! Please submit issues or pull requests.

## License

MIT License

## Version History

- **1.0.0** (Current): Initial release
  - Complete workflow implementation
  - Documentation and examples

---

**Last Updated**: 2024  
**Skill Type**: Automation  
**Complexity**: Medium
"""
        
        readme = skill_dir / "README.md"
        with open(readme, 'w', encoding='utf-8') as f:
            f.write(content)
        
        print(f"✓ Generated {readme}")
    
    def _generate_example_script(self, scripts_dir: Path):
        """Generate an example helper script"""
        
        content = """#!/usr/bin/env python3
\"\"\"
Example helper script for this skill
Customize as needed for your specific use case
\"\"\"

import sys
import os

def main():
    print("Helper script executed")
    print(f"Arguments: {sys.argv[1:]}")
    
    # Add your custom logic here
    
    print("✓ Script completed successfully")

if __name__ == "__main__":
    main()
"""
        
        script = scripts_dir / "helper.py"
        with open(script, 'w', encoding='utf-8') as f:
            f.write(content)
        
        # Make executable
        os.chmod(script, 0o755)
        
        print(f"✓ Generated {script}")

def main():
    """Example usage"""
    if len(sys.argv) < 2:
        print("Usage: python skill_generator.py <config.json>")
        print()
        print("Example config.json:")
        print(json.dumps({
            "name": "example-skill",
            "description": "An example skill that demonstrates the structure",
            "steps": [
                {
                    "name": "Setup",
                    "description": "Initialize the environment and prepare resources"
                },
                {
                    "name": "Process",
                    "description": "Execute the main workflow logic"
                },
                {
                    "name": "Finalize",
                    "description": "Clean up and produce final output"
                }
            ],
            "include_readme": True,
            "include_scripts": True
        }, indent=2))
        sys.exit(1)
    
    config_file = sys.argv[1]
    
    with open(config_file, 'r') as f:
        config = json.load(f)
    
    generator = SkillGenerator()
    skill_dir = generator.generate_skill(**config)
    
    print()
    print("=" * 50)
    print(f"✓ Skill generated successfully!")
    print(f"Location: {skill_dir}")
    print("=" * 50)

if __name__ == "__main__":
    main()
