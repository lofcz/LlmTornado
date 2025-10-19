#!/usr/bin/env python3
"""
Skill Validation Script
Validates that a skill meets all requirements before submission
"""

import os
import sys
import re
from pathlib import Path
from typing import List, Tuple, Dict

class SkillValidator:
    def __init__(self, skill_path: str):
        self.skill_path = Path(skill_path)
        self.errors: List[str] = []
        self.warnings: List[str] = []
        self.checks_passed = 0
        self.checks_total = 0
        
    def validate(self) -> bool:
        """Run all validation checks"""
        print("=" * 50)
        print(f"Validating Skill: {self.skill_path.name}")
        print("=" * 50)
        print()
        
        self.check_directory_exists()
        self.check_skill_md_exists()
        self.check_skill_md_structure()
        self.check_front_matter()
        self.check_progress_checklist()
        self.check_workflow_steps()
        self.check_markdown_formatting()
        
        self.print_results()
        return len(self.errors) == 0
    
    def check_directory_exists(self):
        """Check if skill directory exists"""
        self.checks_total += 1
        if self.skill_path.exists() and self.skill_path.is_dir():
            self.checks_passed += 1
            print("✓ Skill directory exists")
        else:
            self.errors.append(f"Skill directory not found: {self.skill_path}")
            print("✗ Skill directory not found")
    
    def check_skill_md_exists(self):
        """Check if SKILL.md file exists"""
        self.checks_total += 1
        skill_md = self.skill_path / "SKILL.md"
        if skill_md.exists():
            self.checks_passed += 1
            print("✓ SKILL.md file exists")
        else:
            self.errors.append("SKILL.md file not found")
            print("✗ SKILL.md file not found")
    
    def check_skill_md_structure(self):
        """Check SKILL.md has proper structure"""
        skill_md = self.skill_path / "SKILL.md"
        if not skill_md.exists():
            return
        
        with open(skill_md, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Check minimum length
        self.checks_total += 1
        if len(content) > 100:
            self.checks_passed += 1
            print("✓ SKILL.md has content")
        else:
            self.errors.append("SKILL.md is too short")
            print("✗ SKILL.md is too short")
        
        # Check for headers
        self.checks_total += 1
        if re.search(r'^##\s+', content, re.MULTILINE):
            self.checks_passed += 1
            print("✓ SKILL.md has section headers")
        else:
            self.errors.append("SKILL.md missing section headers")
            print("✗ SKILL.md missing section headers")
    
    def check_front_matter(self):
        """Check for valid front matter"""
        skill_md = self.skill_path / "SKILL.md"
        if not skill_md.exists():
            return
        
        with open(skill_md, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Check for front matter
        self.checks_total += 1
        front_matter_pattern = r'^---\s*\n.*?^---\s*\n'
        match = re.search(front_matter_pattern, content, re.MULTILINE | re.DOTALL)
        
        if match:
            self.checks_passed += 1
            print("✓ Front matter present")
            
            front_matter = match.group(0)
            
            # Check for name
            self.checks_total += 1
            if re.search(r'^name:\s*\S+', front_matter, re.MULTILINE):
                self.checks_passed += 1
                print("✓ Front matter has 'name' field")
            else:
                self.errors.append("Front matter missing 'name' field")
                print("✗ Front matter missing 'name' field")
            
            # Check for description
            self.checks_total += 1
            if re.search(r'^description:\s*.+', front_matter, re.MULTILINE):
                self.checks_passed += 1
                print("✓ Front matter has 'description' field")
            else:
                self.errors.append("Front matter missing 'description' field")
                print("✗ Front matter missing 'description' field")
        else:
            self.errors.append("SKILL.md missing front matter")
            print("✗ Front matter missing")
    
    def check_progress_checklist(self):
        """Check for progress tracking checklist"""
        skill_md = self.skill_path / "SKILL.md"
        if not skill_md.exists():
            return
        
        with open(skill_md, 'r', encoding='utf-8') as f:
            content = f.read()
        
        self.checks_total += 1
        # Look for checklist items
        if re.search(r'- \[ \]', content):
            self.checks_passed += 1
            print("✓ Progress checklist found")
        else:
            self.warnings.append("No progress checklist found")
            print("⚠ No progress checklist found")
    
    def check_workflow_steps(self):
        """Check for numbered workflow steps"""
        skill_md = self.skill_path / "SKILL.md"
        if not skill_md.exists():
            return
        
        with open(skill_md, 'r', encoding='utf-8') as f:
            content = f.read()
        
        self.checks_total += 1
        # Look for step headers
        steps = re.findall(r'^##\s+\*\*Step \d+:', content, re.MULTILINE)
        
        if len(steps) >= 3:
            self.checks_passed += 1
            print(f"✓ Found {len(steps)} workflow steps")
        elif len(steps) > 0:
            self.warnings.append(f"Only {len(steps)} workflow steps found (recommend 3+)")
            print(f"⚠ Only {len(steps)} workflow steps found")
        else:
            self.warnings.append("No numbered workflow steps found")
            print("⚠ No numbered workflow steps found")
    
    def check_markdown_formatting(self):
        """Check for proper markdown formatting"""
        skill_md = self.skill_path / "SKILL.md"
        if not skill_md.exists():
            return
        
        with open(skill_md, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        # Check for very long lines
        self.checks_total += 1
        long_lines = [i+1 for i, line in enumerate(lines) if len(line) > 500 and not line.strip().startswith('http')]
        
        if not long_lines:
            self.checks_passed += 1
            print("✓ No excessively long lines")
        else:
            self.warnings.append(f"Very long lines found: {long_lines[:3]}")
            print(f"⚠ Some lines are very long: {long_lines[:3]}")
    
    def print_results(self):
        """Print validation results"""
        print()
        print("=" * 50)
        print("Validation Results")
        print("=" * 50)
        print(f"Checks passed: {self.checks_passed}/{self.checks_total}")
        print()
        
        if self.errors:
            print(f"❌ Errors ({len(self.errors)}):")
            for error in self.errors:
                print(f"  - {error}")
            print()
        
        if self.warnings:
            print(f"⚠️  Warnings ({len(self.warnings)}):")
            for warning in self.warnings:
                print(f"  - {warning}")
            print()
        
        if not self.errors and not self.warnings:
            print("✅ All checks passed! Skill is ready.")
        elif not self.errors:
            print("✅ No errors found. Warnings should be reviewed.")
        else:
            print("❌ Validation failed. Please fix errors before proceeding.")
        
        print("=" * 50)

def main():
    if len(sys.argv) < 2:
        print("Usage: python validate_skill.py <skill-directory>")
        print("Example: python validate_skill.py /tmp/my-skill")
        sys.exit(1)
    
    skill_path = sys.argv[1]
    validator = SkillValidator(skill_path)
    
    success = validator.validate()
    sys.exit(0 if success else 1)

if __name__ == "__main__":
    main()
