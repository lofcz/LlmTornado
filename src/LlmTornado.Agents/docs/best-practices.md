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
    "You answer basic questions about our product."
);

// For complex reasoning: use more capable models
var complexAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41,  // More capable but slower/expensive
    "You analyze complex business scenarios and provide strategic recommendations."
);

// For specialized tasks: consider domain-specific models
var codeAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41,  // Good for code tasks
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

## Performance Optimization

### Connection and Resource Management

Reuse agents and manage resources efficiently:

```csharp
public class AgentPool : IDisposable
{
    private readonly ConcurrentQueue<TornadoAgent> _agents = new();
    private readonly Func<TornadoAgent> _agentFactory;
    private readonly int _maxSize;
    
    public AgentPool(Func<TornadoAgent> agentFactory, int maxSize = 10)
    {
        _agentFactory = agentFactory;
        _maxSize = maxSize;
        
        // Pre-populate pool
        for (int i = 0; i < maxSize; i++)
        {
            _agents.Enqueue(_agentFactory());
        }
    }
    
    public async Task<T> ExecuteAsync<T>(Func<TornadoAgent, Task<T>> operation)
    {
        if (!_agents.TryDequeue(out var agent))
        {
            agent = _agentFactory();
        }
        
        try
        {
            return await operation(agent);
        }
        finally
        {
            // Return to pool if not full
            if (_agents.Count < _maxSize)
            {
                _agents.Enqueue(agent);
            }
        }
    }
    
    public void Dispose()
    {
        while (_agents.TryDequeue(out var agent))
        {
            // Dispose agents if they implement IDisposable
            if (agent is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
```

### Caching Strategies

Implement appropriate caching for repeated operations:

```csharp
public class CachedAgentService
{
    private readonly TornadoAgent _agent;
    private readonly MemoryCache _cache;
    private readonly TimeSpan _defaultExpiry;
    
    public CachedAgentService(TornadoAgent agent, TimeSpan defaultExpiry = default)
    {
        _agent = agent;
        _defaultExpiry = defaultExpiry == default ? TimeSpan.FromMinutes(10) : defaultExpiry;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1000  // Limit cache size
        });
    }
    
    public async Task<string> RunAsync(string input, TimeSpan? customExpiry = null)
    {
        var key = ComputeHash(input);
        
        if (_cache.TryGetValue(key, out string cachedResult))
        {
            return cachedResult;
        }
        
        var result = await _agent.RunAsync(input);
        var response = result.Messages.Last().Content;
        
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = customExpiry ?? _defaultExpiry,
            Size = 1  // Each entry counts as 1 toward the size limit
        };
        
        _cache.Set(key, response, options);
        return response;
    }
    
    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }
}
```

### Parallel Processing

Use parallel processing for independent operations:

```csharp
public class ParallelAgentProcessor
{
    private readonly TornadoAgent[] _agents;
    
    public ParallelAgentProcessor(int agentCount, Func<TornadoAgent> agentFactory)
    {
        _agents = Enumerable.Range(0, agentCount)
            .Select(_ => agentFactory())
            .ToArray();
    }
    
    public async Task<string[]> ProcessBatchAsync(string[] inputs)
    {
        var semaphore = new SemaphoreSlim(_agents.Length);
        
        var tasks = inputs.Select(async (input, index) =>
        {
            await semaphore.WaitAsync();
            try
            {
                var agent = _agents[index % _agents.Length];
                var result = await agent.RunAsync(input);
                return result.Messages.Last().Content;
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        return await Task.WhenAll(tasks);
    }
}
```

## Security Best Practices

### Input Validation and Sanitization

Always validate and sanitize inputs:

```csharp
public class SecureAgentWrapper
{
    private readonly TornadoAgent _agent;
    private readonly HashSet<string> _blockedPatterns;
    
    public SecureAgentWrapper(TornadoAgent agent)
    {
        _agent = agent;
        _blockedPatterns = new HashSet<string>
        {
            "eval(",
            "exec(",
            "system(",
            "<script",
            "javascript:",
            "data:text/html"
        };
    }
    
    public async Task<Conversation> RunAsync(string input)
    {
        // Validate input
        ValidateInput(input);
        
        // Sanitize input
        var sanitizedInput = SanitizeInput(input);
        
        return await _agent.RunAsync(sanitizedInput);
    }
    
    private void ValidateInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input cannot be empty");
        }
        
        if (input.Length > 10000)
        {
            throw new ArgumentException("Input too long");
        }
        
        foreach (var pattern in _blockedPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Input contains blocked pattern: {pattern}");
            }
        }
    }
    
    private string SanitizeInput(string input)
    {
        // Remove potentially harmful characters
        return Regex.Replace(input, @"[<>""']", "");
    }
}
```

### Secure Configuration Management

Store sensitive configuration securely:

```csharp
public class SecureAgentFactory
{
    private readonly IConfiguration _configuration;
    private readonly ISecretManager _secretManager;
    
    public SecureAgentFactory(IConfiguration configuration, ISecretManager secretManager)
    {
        _configuration = configuration;
        _secretManager = secretManager;
    }
    
    public async Task<TornadoAgent> CreateAgentAsync(string agentType)
    {
        // Get API key from secure storage
        var apiKey = await _secretManager.GetSecretAsync("OPENAI_API_KEY");
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("API key not found in secure storage");
        }
        
        var client = new TornadoApi(apiKey);
        
        // Get agent configuration
        var agentConfig = _configuration.GetSection($"Agents:{agentType}");
        var instructions = agentConfig["Instructions"] ?? "You are a helpful assistant";
        var modelName = agentConfig["Model"] ?? "gpt-4o-mini";
        
        return new TornadoAgent(client, GetModel(modelName), instructions);
    }
    
    private ChatModel GetModel(string modelName)
    {
        return modelName.ToLower() switch
        {
            "gpt-4o-mini" => ChatModel.OpenAi.Gpt41.V41Mini,
            "gpt-4o" => ChatModel.OpenAi.Gpt41.V41,
            _ => throw new ArgumentException($"Unknown model: {modelName}")
        };
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

### Health Checks

Implement health checks for your agents:

```csharp
public class AgentHealthCheck : IHealthCheck
{
    private readonly TornadoAgent _agent;
    
    public AgentHealthCheck(TornadoAgent agent)
    {
        _agent = agent;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);
            
            // Simple health check query
            var result = await _agent.RunAsync("Hello");
            
            if (result?.Messages?.LastOrDefault()?.Content != null)
            {
                return HealthCheckResult.Healthy("Agent is responding normally");
            }
            
            return HealthCheckResult.Degraded("Agent response was empty");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Agent health check timed out");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Agent health check failed: {ex.Message}");
        }
    }
}
```

## Testing Strategies

### Unit Testing with Mocks

Create testable agent wrappers:

```csharp
public interface IAgentService
{
    Task<string> ProcessAsync(string input);
}

public class AgentService : IAgentService
{
    private readonly TornadoAgent _agent;
    
    public AgentService(TornadoAgent agent)
    {
        _agent = agent;
    }
    
    public async Task<string> ProcessAsync(string input)
    {
        var result = await _agent.RunAsync(input);
        return result.Messages.Last().Content;
    }
}

// Test with mock
[Test]
public async Task TestAgentService()
{
    var mockService = new Mock<IAgentService>();
    mockService.Setup(s => s.ProcessAsync("test input"))
           .ReturnsAsync("test response");
    
    var result = await mockService.Object.ProcessAsync("test input");
    
    Assert.AreEqual("test response", result);
}
```

### Integration Testing

Test complete workflows:

```csharp
[TestClass]
public class AgentIntegrationTests
{
    private TornadoAgent _agent;
    
    [TestInitialize]
    public void Setup()
    {
        var apiKey = Environment.GetEnvironmentVariable("TEST_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Inconclusive("TEST_API_KEY environment variable not set");
        }
        
        var client = new TornadoApi(apiKey);
        _agent = new TornadoAgent(client, ChatModel.OpenAi.Gpt41.V41Mini, "You are a test assistant");
    }
    
    [TestMethod]
    public async Task TestBasicFunctionality()
    {
        var result = await _agent.RunAsync("What is 2+2?");
        
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Messages);
        Assert.IsTrue(result.Messages.Count > 0);
        
        var response = result.Messages.Last().Content;
        Assert.IsTrue(response.Contains("4"));
    }
    
    [TestMethod]
    public async Task TestWithTools()
    {
        [Description("Add two numbers")]
        static int Add(int a, int b) => a + b;
        
        var agentWithTools = new TornadoAgent(
            _agent.Client,
            _agent.Model,
            "You can perform mathematical operations",
            tools: [Add]
        );
        
        var result = await agentWithTools.RunAsync("What is 5 plus 7?");
        var response = result.Messages.Last().Content;
        
        Assert.IsTrue(response.Contains("12"));
    }
}
```

By following these best practices, you'll build more reliable, maintainable, and performant applications with LlmTornado.Agents. Remember to adapt these patterns to your specific use case and requirements.