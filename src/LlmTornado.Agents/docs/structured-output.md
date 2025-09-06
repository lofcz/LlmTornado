# Structured Output

Define and use structured output schemas to ensure consistent, predictable responses from your agents.

## Overview

Structured output allows you to define the exact format you want your agent to respond in, using C# types as schemas. This ensures consistency, enables type safety, and makes it easier to parse and use agent responses in your applications.

## Basic Structured Output

### Defining Output Schemas

Use C# structs or classes to define your expected output format:

```csharp
using System.ComponentModel;

[Description("Weather information for a location")]
public struct WeatherInfo
{
    [Description("The location name")]
    public string Location { get; set; }
    
    [Description("Temperature in Fahrenheit")]
    public int Temperature { get; set; }
    
    [Description("Weather condition (sunny, cloudy, rainy, etc.)")]
    public string Condition { get; set; }
    
    [Description("Humidity percentage")]
    public int Humidity { get; set; }
}
```

### Creating Agents with Structured Output

```csharp
TornadoAgent weatherAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "Provide weather information in the specified format.",
    outputSchema: typeof(WeatherInfo)
);

Conversation result = await weatherAgent.RunAsync("What's the weather like in Miami?");

// Parse the structured response
WeatherInfo weather = result.Messages.Last().Content.ParseJson<WeatherInfo>();

Console.WriteLine($"Location: {weather.Location}");
Console.WriteLine($"Temperature: {weather.Temperature}°F");
Console.WriteLine($"Condition: {weather.Condition}");
Console.WriteLine($"Humidity: {weather.Humidity}%");
```

## Advanced Schema Patterns

### Complex Data Structures

Create schemas with nested objects and collections:

```csharp
[Description("A complete research report")]
public struct ResearchReport
{
    [Description("Report title")]
    public string Title { get; set; }
    
    [Description("Executive summary")]
    public string Summary { get; set; }
    
    [Description("List of key findings")]
    public List<Finding> Findings { get; set; }
    
    [Description("Recommended actions")]
    public List<string> Recommendations { get; set; }
    
    [Description("Confidence level from 1-10")]
    public int ConfidenceLevel { get; set; }
}

[Description("A research finding")]
public struct Finding
{
    [Description("Finding title")]
    public string Title { get; set; }
    
    [Description("Detailed description")]
    public string Description { get; set; }
    
    [Description("Supporting evidence")]
    public string Evidence { get; set; }
    
    [Description("Importance level: High, Medium, Low")]
    public string Importance { get; set; }
}
```

### Enumeration Support

Use enums for controlled vocabularies:

```csharp
public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}

public enum TaskStatus
{
    NotStarted,
    InProgress,
    Completed,
    Blocked
}

[Description("A task in a project management system")]
public struct Task
{
    [Description("Task title")]
    public string Title { get; set; }
    
    [Description("Task description")]
    public string Description { get; set; }
    
    [Description("Task priority level")]
    public Priority Priority { get; set; }
    
    [Description("Current status of the task")]
    public TaskStatus Status { get; set; }
    
    [Description("Estimated hours to complete")]
    public int EstimatedHours { get; set; }
}
```

### Optional and Nullable Fields

Handle optional data with nullable types:

```csharp
[Description("Contact information")]
public struct ContactInfo
{
    [Description("First name")]
    public string FirstName { get; set; }
    
    [Description("Last name")]
    public string LastName { get; set; }
    
    [Description("Email address")]
    public string Email { get; set; }
    
    [Description("Phone number (optional)")]
    public string? PhoneNumber { get; set; }
    
    [Description("Company name (optional)")]
    public string? Company { get; set; }
    
    [Description("Age (optional)")]
    public int? Age { get; set; }
}
```

## Practical Use Cases

### Data Extraction

Extract structured information from unstructured text:

```csharp
[Description("Extracted information from a business card")]
public struct BusinessCardInfo
{
    [Description("Person's full name")]
    public string FullName { get; set; }
    
    [Description("Job title")]
    public string JobTitle { get; set; }
    
    [Description("Company name")]
    public string Company { get; set; }
    
    [Description("Email address")]
    public string Email { get; set; }
    
    [Description("Phone number")]
    public string Phone { get; set; }
    
    [Description("Physical address")]
    public string Address { get; set; }
}

TornadoAgent extractorAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "Extract contact information from the provided business card text.",
    outputSchema: typeof(BusinessCardInfo)
);

string businessCardText = """
    John Smith
    Senior Software Engineer
    TechCorp Inc.
    john.smith@techcorp.com
    (555) 123-4567
    123 Tech Street, Silicon Valley, CA 94000
    """;

var result = await extractorAgent.RunAsync($"Extract information from this business card: {businessCardText}");
var contactInfo = result.Messages.Last().Content.ParseJson<BusinessCardInfo>();
```

### Decision Making

Structure decision-making outputs:

```csharp
[Description("Analysis and recommendation for a business decision")]
public struct BusinessDecision
{
    [Description("Decision title")]
    public string DecisionTitle { get; set; }
    
    [Description("List of available options")]
    public List<string> Options { get; set; }
    
    [Description("Pros and cons analysis")]
    public List<ProCon> Analysis { get; set; }
    
    [Description("Recommended option")]
    public string Recommendation { get; set; }
    
    [Description("Reasoning for the recommendation")]
    public string Reasoning { get; set; }
    
    [Description("Risk level: Low, Medium, High")]
    public string RiskLevel { get; set; }
    
    [Description("Confidence in recommendation (1-10)")]
    public int Confidence { get; set; }
}

[Description("Pros and cons for an option")]
public struct ProCon
{
    [Description("Option name")]
    public string Option { get; set; }
    
    [Description("List of advantages")]
    public List<string> Pros { get; set; }
    
    [Description("List of disadvantages")]
    public List<string> Cons { get; set; }
}
```

### Content Classification

Classify and categorize content:

```csharp
[Description("Content classification result")]
public struct ContentClassification
{
    [Description("Primary category")]
    public string PrimaryCategory { get; set; }
    
    [Description("Secondary categories")]
    public List<string> SecondaryCategories { get; set; }
    
    [Description("Content sentiment: Positive, Negative, Neutral")]
    public string Sentiment { get; set; }
    
    [Description("Key topics mentioned")]
    public List<string> Topics { get; set; }
    
    [Description("Confidence score (0.0 to 1.0)")]
    public double Confidence { get; set; }
    
    [Description("Content language")]
    public string Language { get; set; }
    
    [Description("Appropriate for all audiences")]
    public bool IsAppropriate { get; set; }
}
```

## Guardrails with Structured Output

Combine structured output with guardrails for validation:

```csharp
[Description("Math problem analysis")]
public struct MathAnalysis
{
    [Description("Is this a math problem?")]
    public bool IsMathProblem { get; set; }
    
    [Description("Type of math problem")]
    public string MathType { get; set; }
    
    [Description("Difficulty level: Elementary, Middle, High, College")]
    public string DifficultyLevel { get; set; }
    
    [Description("Explanation of reasoning")]
    public string Reasoning { get; set; }
}

// Guardrail function using structured output
async ValueTask<GuardRailFunctionOutput> MathGuardRail(string input = "")
{
    TornadoAgent mathChecker = new TornadoAgent(
        client,
        ChatModel.OpenAi.Gpt41.V41Mini,
        instructions: "Analyze if the input is a math-related question.",
        outputSchema: typeof(MathAnalysis)
    );
    
    var result = await mathChecker.RunAsync(input);
    var analysis = result.Messages.Last().Content.ParseJson<MathAnalysis>();
    
    // Block non-math questions
    return new GuardRailFunctionOutput(
        analysis.Reasoning,
        !analysis.IsMathProblem
    );
}
```

## Best Practices

### Schema Design Guidelines

1. **Clear Descriptions**: Always use `[Description]` attributes for types and properties
2. **Appropriate Types**: Use the most specific type possible (enums vs strings, int vs double)
3. **Validation**: Consider what validation the LLM needs to perform
4. **Simplicity**: Avoid deeply nested structures when possible

```csharp
// Good: Clear, specific, well-documented
[Description("Product review analysis")]
public struct ProductReview
{
    [Description("Overall rating from 1 to 5 stars")]
    public int Rating { get; set; }
    
    [Description("Review sentiment: Positive, Negative, or Neutral")]
    public ReviewSentiment Sentiment { get; set; }
    
    [Description("Key positive aspects mentioned")]
    public List<string> Positives { get; set; }
    
    [Description("Key negative aspects or concerns")]
    public List<string> Negatives { get; set; }
}

public enum ReviewSentiment
{
    Positive,
    Negative,
    Neutral
}
```

### Error Handling

Handle parsing errors gracefully:

```csharp
try
{
    var result = await agent.RunAsync("Analyze this product review...");
    var review = result.Messages.Last().Content.ParseJson<ProductReview>();
    
    // Use the structured data
    Console.WriteLine($"Rating: {review.Rating}/5");
    Console.WriteLine($"Sentiment: {review.Sentiment}");
}
catch (JsonException ex)
{
    Console.WriteLine($"Failed to parse structured output: {ex.Message}");
    // Fallback to raw text if needed
    var rawResponse = result.Messages.Last().Content;
    Console.WriteLine($"Raw response: {rawResponse}");
}
```

### Validation

Add custom validation to your schemas:

```csharp
[Description("Validated user input")]
public struct ValidatedInput
{
    private int _score;
    
    [Description("Score from 1 to 10")]
    public int Score 
    { 
        get => _score;
        set => _score = Math.Clamp(value, 1, 10);
    }
    
    [Description("Email address")]
    public string Email { get; set; }
    
    public bool IsValidEmail()
    {
        return Email.Contains("@") && Email.Contains(".");
    }
}
```

## Advanced Features

### Schema Composition

Compose complex schemas from simpler ones:

```csharp
[Description("Basic address information")]
public struct Address
{
    [Description("Street address")]
    public string Street { get; set; }
    
    [Description("City")]
    public string City { get; set; }
    
    [Description("State or province")]
    public string State { get; set; }
    
    [Description("Postal code")]
    public string PostalCode { get; set; }
}

[Description("Complete person profile")]
public struct PersonProfile
{
    [Description("Personal information")]
    public ContactInfo Contact { get; set; }
    
    [Description("Home address")]
    public Address HomeAddress { get; set; }
    
    [Description("Work address")]
    public Address? WorkAddress { get; set; }
}
```

### Dynamic Schema Selection

Choose schemas based on input type:

```csharp
public async Task<object> ProcessInput(string input)
{
    // First, classify the input type
    var classifier = new TornadoAgent(
        client,
        model,
        "Classify the input type",
        outputSchema: typeof(InputClassification)
    );
    
    var classification = await classifier.RunAsync(input);
    var inputType = classification.Messages.Last().Content.ParseJson<InputClassification>();
    
    // Then process with appropriate schema
    TornadoAgent processor = inputType.Type switch
    {
        "weather" => new TornadoAgent(client, model, "Process weather query", outputSchema: typeof(WeatherInfo)),
        "contact" => new TornadoAgent(client, model, "Extract contact info", outputSchema: typeof(ContactInfo)),
        "task" => new TornadoAgent(client, model, "Process task request", outputSchema: typeof(Task)),
        _ => new TornadoAgent(client, model, "General processing")
    };
    
    return await processor.RunAsync(input);
}
```

## Troubleshooting

### Common Issues

**JSON Parsing Errors**
```
Error: Unexpected character in JSON
```
Solution: Ensure your schema descriptions are clear and the model understands the expected format.

**Missing Required Properties**
```
Error: Required property 'PropertyName' not found
```
Solution: Make properties nullable if they're optional, or improve instructions to ensure all required fields are provided.

**Type Conversion Errors**
```
Error: Cannot convert string to int
```
Solution: Be explicit about expected data types in descriptions, consider using string types for ambiguous values.

## Next Steps

- Explore [Guardrails](guardrails.md) for input validation
- Learn about [MCP Integration](mcp-integration.md) for external tools
- Discover [Chat Runtime](chat-runtime.md) for complex workflows
- Check [Examples](examples/) for complete implementations