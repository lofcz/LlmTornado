using LlmTornado.Internal.Press.Configuration;

namespace LlmTornado.Internal.Press.Agents.Writer.Personas;

/// <summary>
/// Base class for writer personas that define different writing styles, tones, and promotion strategies
/// </summary>
public abstract class WriterPersona
{
    /// <summary>
    /// Name of the persona (e.g., "Casual Blogger", "Data-Driven Analyst")
    /// </summary>
    public abstract string Name { get; }
    
    /// <summary>
    /// Brief description of the persona's characteristics
    /// </summary>
    public abstract string Description { get; }
    
    /// <summary>
    /// Promotion level: "Minimal", "Subtle", "Moderate", or "Strategic"
    /// </summary>
    public abstract string PromotionLevel { get; }
    
    /// <summary>
    /// Generate the full instruction prompt for this persona
    /// </summary>
    public abstract string GetInstructions(
        AppConfiguration config, 
        string articleTitle, 
        string articleIdea, 
        string codebaseInstructions);
    
    /// <summary>
    /// Helper method to build common technical requirements that all personas share
    /// </summary>
    protected string GetCommonTechnicalRequirements(AppConfiguration config)
    {
        return $"""
                **CRITICAL CODE SNIPPET REQUIREMENTS:**

                1. **ALWAYS Include `using` Statements**
                   - Every code example MUST start with necessary using statements
                   - Example: `using LlmTornado.Chat;`, `using LlmTornado.Agents;`

                2. **ALWAYS Include Installation Instructions BEFORE First Code**
                   - Place installation section immediately before the first code snippet
                   - Use `dotnet add package` format:
                   ```bash
                   dotnet add package LlmTornado
                   dotnet add package LlmTornado.Agents
                   ```

                3. **Terminology for LlmTornado**
                   - Call it an "SDK", "library", or "framework"
                   - Example: "LlmTornado", "this .NET SDK"

                4. **ALWAYS Link once to Repository**
                   - Include link to GitHub: https://github.com/lofcz/LlmTornado
                   - Place naturally in context, e.g., "For more examples, check the [LlmTornado repository](https://github.com/lofcz/LlmTornado)"

                5. **Citations with HYPERLINKS**
                   - ALWAYS hyperlink citations and sources
                   - Example: "[According to recent studies](https://example.com/study)..."
                   - Example: "As noted in the [official documentation](https://link.to/docs)..."
                   - Increases SEO value and builds credibility

                6. **Code Example Quality**
                   - Include 4-6 substantial code examples (15-40 lines each)
                   - Show initialization, configuration, error handling
                   - Demonstrate real-world scenarios
                   - Include code examples with C# syntax where relevant

                7. **Word Count Target**
                   - Target: {config.ReviewLoop.QualityThresholds.MinWordCount}+ words of VALUABLE content
                """;
    }
}

