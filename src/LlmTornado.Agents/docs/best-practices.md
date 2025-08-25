# Best Practices

This guide outlines recommended patterns, approaches, and practices for building robust applications with LlmTornado.Agents.

## Agent Design

### Single Responsibility Principle

Design agents with clear, focused purposes:

```csharp
// Good: Focused on specific task
var translatorAgent = new TornadoAgent(
    client,
    model,
    name,
    "You translate English text to Spanish. Return only the translation."
);

// Avoid: Too many responsibilities
var everythingAgent = new TornadoAgent(
    client,
    model,
    "You translate, do math, write code, answer questions, and manage files."
);
```

### Clear and Specific Instructions

Write detailed, unambiguous instructions:

```csharp
// Good: Specific and detailed
var codeReviewAgent = new TornadoAgent(
    client,
    model,
    name,
    """
    You are a senior software engineer performing code reviews.
    
    For each code submission:
    1. Check for syntax errors and bugs
    2. Evaluate code quality and maintainability
    3. Identify security vulnerabilities
    4. Suggest performance improvements
    5. Verify adherence to coding standards
    
    Format your response as:
    - Issues Found: [list of problems]
    - Suggestions: [specific improvements]
    - Security Concerns: [any security issues]
    - Overall Assessment: [summary and recommendation]
    """
);

// Avoid: Vague instructions
var badAgent = new TornadoAgent(
    client,
    model,
    "Review code and make it better."
);
```

### Appropriate Model Selection

Choose models based on task complexity and requirements:

```csharp
// For simple tasks: use faster, cheaper models
var simpleAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,  // Fast and cost-effective
    "SimpleQAAgent",
    "You answer basic questions about our product."
);

// For complex reasoning: use more capable models
var complexAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41,  // More capable but slower/expensive
    "BusinessStrategist",
    "You analyze complex business scenarios and provide strategic recommendations."
);

// For specialized tasks: consider domain-specific models
var codeAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41,  // Good for code tasks
    "CodeAssistant",
    "You write and review code in multiple programming languages."
);
```

## Tool Design

### Atomic and Focused Tools

Create tools that do one thing well:

```csharp
// Good: Focused, single-purpose tools
[Description("Get the current weather for a specific location")]
public static string GetWeather(
    [Description("City name")] string city,
    [Description("Country code (optional)")] string? country = null)
{
    // Weather API implementation
}

[Description("Convert temperature between units")]
public static double ConvertTemperature(
    [Description("Temperature value")] double temperature,
    [Description("Source unit (C, F, K)")] string fromUnit,
    [Description("Target unit (C, F, K)")] string toUnit)
{
    // Conversion logic
}

// Avoid: Tools that do too much
[Description("Weather tool that gets weather, forecasts, converts units, and manages locations")]
public static string WeatherEverything(string input) // Too broad
{
    // Complex, hard-to-maintain implementation
}
```

### Comprehensive Tool Documentation

Use detailed descriptions for better AI understanding:

```csharp
[Description("Calculate the monthly payment for a loan")]
public static double CalculateLoanPayment(
    [Description("Principal loan amount in dollars")] double principal,
    [Description("Annual interest rate as a decimal (e.g., 0.05 for 5%)")] double annualRate,
    [Description("Loan term in years")] int termYears)
{
    double monthlyRate = annualRate / 12;
    int totalPayments = termYears * 12;
    
    return principal * (monthlyRate * Math.Pow(1 + monthlyRate, totalPayments)) 
           / (Math.Pow(1 + monthlyRate, totalPayments) - 1);
}
```

### Error Handling in Tools

Implement robust error handling:

```csharp
[Description("Divide two numbers")]
public static string SafeDivision(
    [Description("Dividend")] double dividend,
    [Description("Divisor")] double divisor)
{
    try
    {
        if (divisor == 0)
        {
            return "Error: Division by zero is not allowed";
        }
        
        double result = dividend / divisor;
        return $"Result: {result}";
    }
    catch (Exception ex)
    {
        return $"Error performing division: {ex.Message}";
    }
}
```

### Async Tools for I/O Operations

Use async methods for I/O-bound operations:

```csharp
[Description("Fetch data from a web API")]
public static async Task<string> FetchWebData(
    [Description("URL to fetch data from")] string url)
{
    using var httpClient = new HttpClient();
    
    try
    {
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }
    catch (HttpRequestException ex)
    {
        return $"Error fetching data: {ex.Message}";
    }
    catch (TaskCanceledException)
    {
        return "Error: Request timed out";
    }
}
```

## Structured Output Design

### Clear and Specific Schemas

Design schemas that are easy for LLMs to understand and fill:

```csharp
// Good: Clear, specific schema
[Description("Analysis of a business proposal")]
public struct ProposalAnalysis
{
    [Description("Overall viability score from 1-10")]
    public int ViabilityScore { get; set; }
    
    [Description("Primary strengths of the proposal")]
    public List<string> Strengths { get; set; }
    
    [Description("Major concerns or weaknesses")]
    public List<string> Concerns { get; set; }
    
    [Description("Recommended next steps")]
    public List<string> NextSteps { get; set; }
    
    [Description("Investment recommendation: Approve, Reject, or Request More Info")]
    public string Recommendation { get; set; }
}

// Avoid: Ambiguous or overly complex schemas
public struct BadSchema
{
    public object Data { get; set; }  // Too vague
    public Dictionary<string, List<Dictionary<string, object>>> ComplexNested { get; set; }  // Too complex
}
```

### Use Enums for Controlled Vocabularies

Define enums for fields with limited valid values:

```csharp
public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}

public enum ProjectStatus
{
    Planning,
    InProgress,
    OnHold,
    Completed,
    Cancelled
}

[Description("Project information")]
public struct Project
{
    [Description("Project name")]
    public string Name { get; set; }
    
    [Description("Project priority level")]
    public Priority Priority { get; set; }
    
    [Description("Current project status")]
    public ProjectStatus Status { get; set; }
}
```

### Validation and Default Values

Include validation and sensible defaults:

```csharp
[Description("User rating and feedback")]
public struct UserRating
{
    private int _rating;
    
    [Description("Rating from 1 to 5 stars")]
    public int Rating 
    { 
        get => _rating;
        set => _rating = Math.Clamp(value, 1, 5);  // Automatically clamp to valid range
    }
    
    [Description("Optional feedback comment")]
    public string? Comment { get; set; }
    
    [Description("Would recommend to others")]
    public bool WouldRecommend { get; set; }
    
    [Description("Category of feedback")]
    public string Category { get; set; } = "General";  // Default value
}
```

## Error Handling and Resilience

### Comprehensive Exception Handling

Handle different types of errors appropriately:

```csharp
public async Task<string> ResilientAgentExecution(TornadoAgent agent, string input)
{
    try
    {
        var result = await agent.RunAsync(input);
        return result.Messages.Last().Content;
    }
    catch (GuardRailTriggerException ex)
    {
        // Input was blocked by guardrails
        _logger.LogWarning("Input blocked by guardrails: {Message}", ex.Message);
        return "I cannot process that request due to content policy restrictions.";
    }
    catch (HttpRequestException ex)
    {
        // Network or API error
        _logger.LogError(ex, "API request failed");
        return "I'm experiencing technical difficulties. Please try again later.";
    }
    catch (TaskCanceledException ex)
    {
        // Timeout
        _logger.LogError(ex, "Request timed out");
        return "The request took too long to process. Please try again.";
    }
    catch (JsonException ex)
    {
        // Structured output parsing failed
        _logger.LogError(ex, "Failed to parse structured output");
        return "I encountered an error processing the response format.";
    }
    catch (Exception ex)
    {
        // Unexpected error
        _logger.LogError(ex, "Unexpected error during agent execution");
        return "An unexpected error occurred. Please contact support if this persists.";
    }
}
```

### Retry Logic with Exponential Backoff

Implement smart retry strategies:

```csharp
public async Task<T> ExecuteWithRetry<T>(
    Func<Task<T>> operation,
    int maxRetries = 3,
    TimeSpan baseDelay = default)
{
    if (baseDelay == default)
        baseDelay = TimeSpan.FromSeconds(1);
    
    Exception lastException = null;
    
    for (int attempt = 0; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await operation();
        }
        catch (HttpRequestException ex) when (attempt < maxRetries)
        {
            lastException = ex;
            var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
            
            _logger.LogWarning("Attempt {Attempt} failed, retrying in {Delay}ms: {Message}", 
                attempt + 1, delay.TotalMilliseconds, ex.Message);
            
            await Task.Delay(delay);
        }
        catch (TaskCanceledException ex) when (attempt < maxRetries)
        {
            lastException = ex;
            var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
            
            await Task.Delay(delay);
        }
    }
    
    throw new Exception($"Operation failed after {maxRetries + 1} attempts", lastException);
}
```

### Circuit Breaker Pattern

Implement circuit breakers for external dependencies:

```csharp
public class CircuitBreakerAgent
{
    private readonly TornadoAgent _agent;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    
    public CircuitBreakerAgent(TornadoAgent agent, int failureThreshold = 5, TimeSpan timeout = default)
    {
        _agent = agent;
        _failureThreshold = failureThreshold;
        _timeout = timeout == default ? TimeSpan.FromMinutes(1) : timeout;
    }
    
    public async Task<Conversation> RunAsync(string input)
    {
        // Check if circuit is open
        if (_failureCount >= _failureThreshold && 
            DateTime.UtcNow - _lastFailureTime < _timeout)
        {
            throw new InvalidOperationException("Circuit breaker is open. Service temporarily unavailable.");
        }
        
        try
        {
            var result = await _agent.RunAsync(input);
            
            // Reset on success
            _failureCount = 0;
            return result;
        }
        catch (Exception)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            throw;
        }
    }
}
```


## Monitoring and Observability

### Comprehensive Logging

Implement detailed logging for debugging and monitoring:

```csharp
public class ObservableAgent
{
    private readonly TornadoAgent _agent;
    private readonly ILogger<ObservableAgent> _logger;
    private readonly IMetrics _metrics;
    
    public ObservableAgent(TornadoAgent agent, ILogger<ObservableAgent> logger, IMetrics metrics)
    {
        _agent = agent;
        _logger = logger;
        _metrics = metrics;
    }
    
    public async Task<Conversation> RunAsync(string input)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();
        
        _logger.LogInformation("Agent execution started. RequestId: {RequestId}, InputLength: {Length}",
            requestId, input.Length);
        
        try
        {
            var result = await _agent.RunAsync(input, onAgentRunnerEvent: LogAgentEvents);
            
            stopwatch.Stop();
            
            _logger.LogInformation("Agent execution completed. RequestId: {RequestId}, Duration: {Duration}ms",
                requestId, stopwatch.ElapsedMilliseconds);
            
            _metrics.RecordExecutionTime(stopwatch.ElapsedMilliseconds);
            _metrics.IncrementCounter("agent_requests_success");
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "Agent execution failed. RequestId: {RequestId}, Duration: {Duration}ms",
                requestId, stopwatch.ElapsedMilliseconds);
            
            _metrics.IncrementCounter("agent_requests_error");
            throw;
        }
    }
    
    private ValueTask LogAgentEvents(AgentRunnerEvents runEvent)
    {
        switch (runEvent.EventType)
        {
            case AgentRunnerEventTypes.ToolCall:
                var toolEvent = (AgentRunnerToolCallEvent)runEvent;
                _logger.LogDebug("Tool called: {ToolName}", toolEvent.ToolCall.Name);
                _metrics.IncrementCounter("tool_calls", new[] { ("tool", toolEvent.ToolCall.Name) });
                break;
                
            case AgentRunnerEventTypes.ToolCallResult:
                _logger.LogDebug("Tool completed");
                break;
        }
        
        return ValueTask.CompletedTask;
    }
}
```
