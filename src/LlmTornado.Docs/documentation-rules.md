# LlmTornado Documentation Rules

This file establishes guidelines and standards for creating and maintaining LlmTornado documentation. These rules should be followed by all AI and human contributors to ensure consistency, quality, and maintainability.

## Table of Contents
- [Content Standards](#content-standards)
- [Code Examples](#code-examples)
- [Documentation Structure](#documentation-structure)
- [Technical Guidelines](#technical-guidelines)
- [Visual Standards](#visual-standards)
- [Maintenance Guidelines](#maintenance-guidelines)
- [Special Considerations for LlmTornado](#special-considerations-for-llmtornado)

## Content Standards

### Clarity First
- Use simple, clear language that's accessible to developers of varying skill levels
- Avoid unnecessary jargon. When technical terms are used, provide clear explanations
- Write in active voice and use direct, concise language
- Break complex concepts into digestible sections

### Example-Driven
- Every concept should be accompanied by practical, runnable code examples
- Examples should demonstrate real-world use cases, not just toy scenarios
- Include both basic and advanced examples for each major feature
- Show before/after patterns where applicable

### Progressive Complexity
- Arrange content from basic to advanced concepts
- Each page should build upon previous concepts
- Include clear prerequisites for advanced topics
- Provide learning paths for different user types (beginner, intermediate, advanced)

### Real-World Context
- Examples should reflect actual production scenarios
- Include considerations for error handling and edge cases
- Discuss performance implications and best practices
- Highlight common pitfalls and how to avoid them

## Code Examples

### Complete & Runnable
- Examples should be complete and runnable with minimal setup
- Include all necessary using statements and imports
- Provide context about where code fits into larger applications
- Ensure examples work across different supported platforms

### Latest API
- Use the latest API patterns and avoid deprecated approaches
- Clearly mark any deprecated methods with alternatives
- Update examples when APIs change
- Follow the library's coding conventions and style

### Coding Style
- Do not use `var` - use explicit types
- Use collection expressions instead of traditional collection initialization
- Use full type names in expressions right-hand side, e.g. `new Conversion()` instead of `new()`
- Follow C# best practices and modern language features

### Error Handling
- Include proper error handling in production examples
- Show both success and error scenarios
- Explain when and how to handle different exception types
- Provide guidance on debugging common issues

### Comments
- Add meaningful comments explaining key decisions and logic
- Use XML documentation comments for public APIs
- Explain the "why" behind implementation choices, not just the "what"
- Keep comments concise and focused on adding value

## Documentation Structure

### Consistent Layout
- Follow the standardized page template (see below)
- Use consistent heading hierarchy and formatting
- Include table of contents for longer documents
- Maintain consistent naming conventions for files and sections

### Standard Page Template
```markdown
# Page Title

## Overview
Brief description of the topic and its importance

## Quick Start
Basic example with minimal setup to get started immediately

## Prerequisites
Any requirements or knowledge needed before proceeding

## Detailed Explanation
In-depth explanation of concepts and features

## Basic Usage
Simple examples with clear explanations

## Advanced Usage
Complex examples and edge cases

## Best Practices
Recommendations for production use

## Common Issues
Troubleshooting guide and common problems

## API Reference
Relevant classes, methods, and properties (if applicable)

## Related Topics
Links to related documentation and further reading
```

### Logical Flow
- Arrange content from basic to advanced concepts
- Ensure each section builds upon previous knowledge
- Include clear transitions between topics
- Provide summaries for complex sections

### Cross-References
- Link to related topics and prerequisites
- Use relative links for internal documentation
- Ensure all links are valid and up-to-date
- Link to relevant API documentation when appropriate

### Navigation
- Ensure pages are properly integrated into the sidebar
- Use clear, descriptive link text
- Organize content in logical groups
- Include breadcrumbs for complex navigation paths

## Technical Guidelines

### API Accuracy
- Ensure all API references are current and accurate
- Test all code examples with the latest version
- Verify method signatures and parameter types
- Check for any breaking changes between versions

### Version Compatibility
- Note any version-specific behaviors or requirements
- Clearly indicate which versions are supported
- Document deprecated features and migration paths
- Provide compatibility matrices for different platforms

### Platform Notes
- Include considerations for different platforms (Blazor, ASP.NET, console apps)
- Highlight platform-specific features and limitations
- Provide platform-specific examples where relevant
- Note any dependencies or requirements for each platform

### Performance
- Include performance considerations where relevant
- Provide benchmarks for critical operations
- Discuss memory usage and optimization strategies
- Note any performance implications of different approaches

## Visual Standards

### Code Formatting
- Use consistent syntax highlighting and formatting
- Follow the library's coding style conventions
- Ensure code is properly indented and readable
- Use language-appropriate syntax highlighting

### Diagram Usage
- Include architecture diagrams for complex concepts
- Use consistent styling for all diagrams
- Ensure diagrams are accessible (include alt text)
- Keep diagrams focused and uncluttered

### Screenshot Guidelines
- Use consistent styling for screenshots if included
- Ensure screenshots are clear and readable
- Include captions and explanations for each screenshot
- Update screenshots when UI changes occur

### Dark Theme Compliance
- Ensure all content works well with the dark theme
- Test contrast ratios for text and backgrounds
- Use appropriate colors for syntax highlighting
- Ensure images and diagrams are visible in dark mode

## Maintenance Guidelines

### Update Triggers
- Update documentation when:
  - New versions are released
  - APIs change or are deprecated
  - New features are added
  - Bug fixes affect documented behavior
  - User feedback indicates issues or confusion

### Review Process
- Establish a review workflow for documentation changes
- Include both technical and editorial reviews
- Ensure examples are tested and working
- Verify links and references are current

### Feedback Loop
- Create mechanisms for user feedback on documentation
- Monitor GitHub issues and discussions for documentation problems
- Track documentation usage and identify gaps
- Regularly review and update content based on feedback

### Analytics
- Track usage to identify which documentation is most helpful
- Monitor search queries to find unmet needs
- Analyze user behavior to improve navigation
- Use data to prioritize documentation updates

## Special Considerations for LlmTornado

### Provider Differences
- Clearly document differences between AI providers (OpenAI, Gemini, Claude, etc.)
- Note provider-specific features and limitations
- Include provider-specific examples where relevant
- Document any provider-specific configuration or authentication

### Feature Availability
- Note which features are available with which providers
- Include feature compatibility matrices
- Document beta features and their limitations
- Highlight experimental or unstable features

### Best Practices
- Include LlmTornado-specific best practices
- Discuss optimal configuration for different use cases
- Provide guidance on error handling and retries
- Discuss rate limiting and quota management

### Common Pitfalls
- Document common mistakes and how to avoid them
- Provide troubleshooting guides for typical issues
- Include debugging tips for hard-to-diagnose problems
- Discuss performance optimization strategies

## Quality Checklist

Before submitting documentation, ensure:

- [ ] Content is accurate and up-to-date
- [ ] All code examples are tested and working
- [ ] Examples follow the latest API patterns
- [ ] Error handling is included where appropriate
- [ ] Links are valid and functional
- [ ] Content is accessible and follows visual standards
- [ ] Page follows the standard template structure
- [ ] Cross-references are included and accurate
- [ ] Content is reviewed for clarity and completeness
- [ ] Documentation builds successfully in the VitePress environment

## Contributing

To contribute to LlmTornado documentation:

1. Familiarize yourself with these rules and existing documentation
2. Create or update documentation following the guidelines above
3. Test all code examples to ensure they work correctly
4. Submit your changes through the normal pull request process
5. Be prepared to respond to feedback and make revisions

Remember: Good documentation is as important as good code. It enables developers to use the library effectively and reduces support burden.
