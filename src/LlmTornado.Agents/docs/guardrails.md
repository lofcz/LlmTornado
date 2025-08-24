# Guardrails

Guardrails provide input validation and safety mechanisms to control agent behavior and ensure appropriate responses.

## Overview

Guardrails act as safety mechanisms that:
- Validate input before processing
- Block inappropriate or harmful content
- Ensure agents stay within defined boundaries
- Provide clear feedback when rules are violated

Guardrails can trigger "tripwires" that stop processing when certain conditions are met.

## Basic Guardrail Implementation

### Simple Input Validation

```csharp
using LlmTornado.Agents.DataModels;

// Basic guardrail function
async ValueTask<GuardRailFunctionOutput> BasicContentFilter(string input = "")
{
    var inappropriateWords = new[] { "spam", "harmful", "inappropriate" };
    
    if (inappropriateWords.Any(word => input.Contains(word, StringComparison.OrdinalIgnoreCase)))
    {
        return new GuardRailFunctionOutput(
            outputInfo: "Content contains inappropriate language",
            tripwireTriggered: true // This will stop processing
        );
    }
    
    return new GuardRailFunctionOutput(
        outputInfo: "Content is appropriate",
        tripwireTriggered: false // Processing continues
    );
}

// Use guardrail with agent
TornadoAgent agent = new TornadoAgent(client, model, "You are a helpful assistant");

try
{
    var result = await agent.RunAsync(
        "Hello, how are you?", 
        inputGuardRailFunction: BasicContentFilter
    );
    Console.WriteLine(result.Messages.Last().Content);
}
catch (GuardRailTriggerException ex)
{
    Console.WriteLine($"Guardrail triggered: {ex.Message}");
}
```

### Advanced Content Filtering

Use AI-powered content analysis for sophisticated filtering:

```csharp
[Description("Content safety analysis")]
public struct ContentSafety
{
    [Description("Is the content safe and appropriate?")]
    public bool IsSafe { get; set; }
    
    [Description("Safety score from 0.0 (unsafe) to 1.0 (safe)")]
    public double SafetyScore { get; set; }
    
    [Description("Reasons for safety concerns")]
    public List<string> SafetyConcerns { get; set; }
    
    [Description("Content category")]
    public string Category { get; set; }
}

async ValueTask<GuardRailFunctionOutput> AISafetyGuardRail(string input = "")
{
    // Create a specialized safety analysis agent
    TornadoAgent safetyAgent = new TornadoAgent(
        client,
        ChatModel.OpenAi.Gpt41.V41Mini,
        instructions: """
            You are a content safety analyzer. Evaluate content for:
            - Harmful or dangerous information
            - Inappropriate content
            - Spam or low-quality content
            - Privacy violations
            - Legal concerns
            
            Provide a detailed safety analysis.
            """,
        outputSchema: typeof(ContentSafety)
    );
    
    var result = await safetyAgent.RunAsync($"Analyze this content for safety: {input}");
    var safety = result.Messages.Last().Content.ParseJson<ContentSafety>();
    
    // Trigger guardrail if content is unsafe
    bool shouldBlock = !safety.IsSafe || safety.SafetyScore < 0.7;
    
    return new GuardRailFunctionOutput(
        outputInfo: shouldBlock 
            ? $"Content blocked: {string.Join(", ", safety.SafetyConcerns)}"
            : "Content approved",
        tripwireTriggered: shouldBlock
    );
}
```

## Specialized Guardrail Patterns

### Topic-Specific Guardrails

Ensure agents only respond to specific topics:

```csharp
[Description("Topic classification")]
public struct TopicAnalysis
{
    [Description("Is this topic relevant to math?")]
    public bool IsMathTopic { get; set; }
    
    [Description("Confidence level 0.0 to 1.0")]
    public double Confidence { get; set; }
    
    [Description("Explanation of the analysis")]
    public string Reasoning { get; set; }
}

async ValueTask<GuardRailFunctionOutput> MathOnlyGuardRail(string input = "")
{
    TornadoAgent mathChecker = new TornadoAgent(
        client,
        ChatModel.OpenAi.Gpt41.V41Mini,
        instructions: "Determine if the input is related to mathematics, calculations, or math problems.",
        outputSchema: typeof(TopicAnalysis)
    );
    
    var result = await mathChecker.RunAsync(input);
    var analysis = result.Messages.Last().Content.ParseJson<TopicAnalysis>();
    
    // Block non-math topics
    bool shouldBlock = !analysis.IsMathTopic || analysis.Confidence < 0.8;
    
    return new GuardRailFunctionOutput(
        outputInfo: analysis.Reasoning,
        tripwireTriggered: shouldBlock
    );
}

// Math-only agent
TornadoAgent mathAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a math tutor. Help with mathematical problems and calculations."
);

// This will work
var mathResult = await mathAgent.RunAsync(
    "What is 2 + 2?", 
    inputGuardRailFunction: MathOnlyGuardRail
);

// This will trigger the guardrail
try
{
    var nonMathResult = await mathAgent.RunAsync(
        "What's the weather like?", 
        inputGuardRailFunction: MathOnlyGuardRail
    );
}
catch (GuardRailTriggerException ex)
{
    Console.WriteLine($"Non-math question blocked: {ex.Message}");
}
```

### Multi-Stage Guardrails

Implement multiple validation stages:

```csharp
async ValueTask<GuardRailFunctionOutput> MultiStageGuardRail(string input = "")
{
    // Stage 1: Basic content filter
    var basicResult = await BasicContentFilter(input);
    if (basicResult.TripwireTriggered)
    {
        return basicResult;
    }
    
    // Stage 2: Topic validation
    var topicResult = await MathOnlyGuardRail(input);
    if (topicResult.TripwireTriggered)
    {
        return topicResult;
    }
    
    // Stage 3: Complexity check
    if (input.Length > 1000)
    {
        return new GuardRailFunctionOutput(
            "Input too long - please keep questions under 1000 characters",
            tripwireTriggered: true
        );
    }
    
    // All stages passed
    return new GuardRailFunctionOutput(
        "Input passed all validation stages",
        tripwireTriggered: false
    );
}
```

### Role-Based Guardrails

Different guardrails based on user roles:

```csharp
public class RoleBasedGuardRails
{
    private readonly string _userRole;
    
    public RoleBasedGuardRails(string userRole)
    {
        _userRole = userRole;
    }
    
    public async ValueTask<GuardRailFunctionOutput> ValidateAccess(string input = "")
    {
        return _userRole switch
        {
            "admin" => await AdminGuardRail(input),
            "user" => await UserGuardRail(input),
            "guest" => await GuestGuardRail(input),
            _ => new GuardRailFunctionOutput("Unknown role", tripwireTriggered: true)
        };
    }
    
    private async ValueTask<GuardRailFunctionOutput> AdminGuardRail(string input)
    {
        // Admins have full access
        return new GuardRailFunctionOutput("Admin access granted", tripwireTriggered: false);
    }
    
    private async ValueTask<GuardRailFunctionOutput> UserGuardRail(string input)
    {
        // Regular users have limited access
        var restrictedKeywords = new[] { "admin", "system", "configure" };
        
        if (restrictedKeywords.Any(keyword => input.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            return new GuardRailFunctionOutput(
                "Access denied: insufficient privileges",
                tripwireTriggered: true
            );
        }
        
        return new GuardRailFunctionOutput("User access granted", tripwireTriggered: false);
    }
    
    private async ValueTask<GuardRailFunctionOutput> GuestGuardRail(string input)
    {
        // Guests have very limited access
        if (input.Length > 100)
        {
            return new GuardRailFunctionOutput(
                "Guest queries limited to 100 characters",
                tripwireTriggered: true
            );
        }
        
        return new GuardRailFunctionOutput("Guest access granted", tripwireTriggered: false);
    }
}

// Usage
var guestGuardRails = new RoleBasedGuardRails("guest");
var result = await agent.RunAsync(
    "Quick question",
    inputGuardRailFunction: guestGuardRails.ValidateAccess
);
```

## Output Guardrails

Validate agent responses before returning them:

```csharp
[Description("Response quality analysis")]
public struct ResponseQuality
{
    [Description("Is the response helpful and relevant?")]
    public bool IsHelpful { get; set; }
    
    [Description("Does the response stay on topic?")]
    public bool IsOnTopic { get; set; }
    
    [Description("Quality score from 0.0 to 1.0")]
    public double QualityScore { get; set; }
    
    [Description("Issues with the response")]
    public List<string> Issues { get; set; }
}

async ValueTask<GuardRailFunctionOutput> ResponseQualityGuardRail(string response = "")
{
    TornadoAgent qualityChecker = new TornadoAgent(
        client,
        ChatModel.OpenAi.Gpt41.V41Mini,
        instructions: """
            Analyze the quality of AI assistant responses. Check for:
            - Helpfulness and relevance
            - Accuracy of information
            - Appropriate tone and language
            - Completeness of the answer
            """,
        outputSchema: typeof(ResponseQuality)
    );
    
    var result = await qualityChecker.RunAsync($"Evaluate this response: {response}");
    var quality = result.Messages.Last().Content.ParseJson<ResponseQuality>();
    
    bool shouldBlock = !quality.IsHelpful || !quality.IsOnTopic || quality.QualityScore < 0.6;
    
    return new GuardRailFunctionOutput(
        outputInfo: shouldBlock 
            ? $"Response quality issues: {string.Join(", ", quality.Issues)}"
            : "Response meets quality standards",
        tripwireTriggered: shouldBlock
    );
}

// Note: Output guardrails would be implemented in a custom runner
// This is conceptual - the current API focuses on input guardrails
```

## Guardrails with Structured Output

Combine guardrails with structured output validation:

```csharp
[Description("Math problem validation")]
public struct MathProblemAnalysis
{
    [Description("Is this a valid math problem?")]
    public bool IsValidMathProblem { get; set; }
    
    [Description("Type of math problem")]
    public string ProblemType { get; set; }
    
    [Description("Difficulty level: Elementary, Middle, High, College")]
    public string DifficultyLevel { get; set; }
    
    [Description("Explanation of the validation")]
    public string ValidationReasoning { get; set; }
}

async ValueTask<GuardRailFunctionOutput> MathProblemValidator(string input = "")
{
    TornadoAgent mathValidator = new TornadoAgent(
        client,
        ChatModel.OpenAi.Gpt41.V41Mini,
        instructions: "Validate if the input contains a legitimate mathematical problem or question.",
        outputSchema: typeof(MathProblemAnalysis)
    );
    
    var result = await mathValidator.RunAsync(input);
    var analysis = result.Messages.Last().Content.ParseJson<MathProblemAnalysis>();
    
    return new GuardRailFunctionOutput(
        outputInfo: analysis.ValidationReasoning,
        tripwireTriggered: !analysis.IsValidMathProblem
    );
}
```

## Custom Guardrail Exceptions

Handle different types of guardrail violations:

```csharp
public class ContentViolationException : GuardRailTriggerException
{
    public string ViolationType { get; }
    
    public ContentViolationException(string message, string violationType) 
        : base(message)
    {
        ViolationType = violationType;
    }
}

public class AccessDeniedException : GuardRailTriggerException
{
    public string RequiredRole { get; }
    public string UserRole { get; }
    
    public AccessDeniedException(string message, string requiredRole, string userRole) 
        : base(message)
    {
        RequiredRole = requiredRole;
        UserRole = userRole;
    }
}

// Enhanced guardrail with specific exceptions
async ValueTask<GuardRailFunctionOutput> EnhancedGuardRail(string input = "")
{
    // Check for content violations
    if (input.Contains("spam"))
    {
        throw new ContentViolationException("Spam content detected", "SPAM");
    }
    
    // Check for access violations
    if (input.Contains("admin commands"))
    {
        throw new AccessDeniedException("Admin access required", "admin", "user");
    }
    
    return new GuardRailFunctionOutput("Input validated", tripwireTriggered: false);
}

// Handle specific exceptions
try
{
    var result = await agent.RunAsync(input, inputGuardRailFunction: EnhancedGuardRail);
}
catch (ContentViolationException ex)
{
    Console.WriteLine($"Content violation: {ex.ViolationType} - {ex.Message}");
}
catch (AccessDeniedException ex)
{
    Console.WriteLine($"Access denied: User role '{ex.UserRole}' requires '{ex.RequiredRole}'");
}
catch (GuardRailTriggerException ex)
{
    Console.WriteLine($"Guardrail triggered: {ex.Message}");
}
```

## Performance Optimization

### Caching Guardrail Results

Cache validation results for repeated inputs:

```csharp
public class CachedGuardRail
{
    private readonly Dictionary<string, GuardRailFunctionOutput> _cache = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);
    private readonly Dictionary<string, DateTime> _cacheTimestamps = new();
    
    public async ValueTask<GuardRailFunctionOutput> ValidateWithCache(string input = "")
    {
        var key = input.GetHashCode().ToString();
        
        // Check cache
        if (_cache.TryGetValue(key, out var cachedResult) &&
            _cacheTimestamps.TryGetValue(key, out var timestamp) &&
            DateTime.UtcNow - timestamp < _cacheExpiry)
        {
            return cachedResult;
        }
        
        // Perform validation
        var result = await PerformValidation(input);
        
        // Cache result
        _cache[key] = result;
        _cacheTimestamps[key] = DateTime.UtcNow;
        
        return result;
    }
    
    private async ValueTask<GuardRailFunctionOutput> PerformValidation(string input)
    {
        // Your validation logic here
        await Task.Delay(100); // Simulate validation work
        return new GuardRailFunctionOutput("Validated", tripwireTriggered: false);
    }
}
```

### Parallel Guardrail Validation

Run multiple guardrails in parallel:

```csharp
async ValueTask<GuardRailFunctionOutput> ParallelGuardRails(string input = "")
{
    // Define multiple guardrail tasks
    var validationTasks = new[]
    {
        BasicContentFilter(input),
        MathOnlyGuardRail(input),
        AISafetyGuardRail(input)
    };
    
    // Wait for all validations to complete
    var results = await Task.WhenAll(validationTasks);
    
    // Check if any guardrail was triggered
    var triggeredGuardrail = results.FirstOrDefault(r => r.TripwireTriggered);
    if (triggeredGuardrail != null)
    {
        return triggeredGuardrail;
    }
    
    // All guardrails passed
    return new GuardRailFunctionOutput(
        "All parallel validations passed",
        tripwireTriggered: false
    );
}
```

## Best Practices

### Guardrail Design

1. **Clear Feedback**: Provide clear explanations when guardrails trigger
2. **Appropriate Sensitivity**: Balance security with usability
3. **Fast Execution**: Keep guardrails lightweight for good performance
4. **Comprehensive Coverage**: Consider multiple types of violations

### Error Handling

1. **Graceful Degradation**: Handle guardrail failures gracefully
2. **Specific Exceptions**: Use specific exception types for different violations
3. **Logging**: Log guardrail triggers for monitoring and improvement
4. **User Experience**: Provide helpful feedback to users

### Performance

1. **Caching**: Cache validation results for repeated patterns
2. **Parallel Execution**: Run independent validations in parallel
3. **Early Exit**: Stop validation on first triggered guardrail
4. **Resource Management**: Monitor computational cost of AI-powered guardrails

## Testing Guardrails

```csharp
[Test]
public async Task TestMathGuardRail()
{
    // Test valid math input
    var validResult = await MathOnlyGuardRail("What is 2 + 2?");
    Assert.IsFalse(validResult.TripwireTriggered);
    
    // Test invalid input
    var invalidResult = await MathOnlyGuardRail("What's the weather?");
    Assert.IsTrue(invalidResult.TripwireTriggered);
}

[Test]
public async Task TestGuardRailException()
{
    var agent = new TornadoAgent(client, model, "Math assistant");
    
    Assert.ThrowsAsync<GuardRailTriggerException>(async () =>
    {
        await agent.RunAsync("Tell me a joke", inputGuardRailFunction: MathOnlyGuardRail);
    });
}
```

## Common Use Cases

### Content Moderation
- Filter inappropriate language
- Block harmful instructions
- Validate content quality

### Access Control
- Role-based permissions
- Feature gating
- Resource limits

### Topic Boundaries
- Domain-specific agents
- Specialized assistants
- Knowledge boundaries

### Quality Assurance
- Response validation
- Accuracy checking
- Completeness verification

## Troubleshooting

### Common Issues

**Guardrail Too Strict**
```
Error: Valid inputs being blocked
```
Solution: Adjust threshold values or improve validation logic.

**Performance Issues**
```
Error: Guardrails causing delays
```
Solution: Implement caching, optimize validation logic, or use parallel execution.

**False Positives**
```
Error: Legitimate content flagged as problematic
```
Solution: Improve training data for AI-powered guardrails or refine rules.

## Next Steps

- Learn about [API Reference](api/tornado-agent.md) for implementation details
- Explore [Examples](examples/) for complete guardrail implementations
- Review [Best Practices](best-practices.md) for production deployments
- Check [Tool Integration](tool-integration.md) for advanced validation patterns