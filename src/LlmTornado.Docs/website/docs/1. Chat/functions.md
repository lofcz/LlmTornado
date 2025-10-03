# Function Calling

## Overview

Function calling (also known as tool use) allows AI models to execute functions and use tools to perform tasks beyond their knowledge cutoff. This powerful feature enables LlmTornado to interact with external systems, process data, and perform complex operations by leveraging the AI's ability to understand when and how to call specific functions.

## Quick Start

Here's a basic example of how to use function calling with LlmTornado:

```csharp
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.ChatFunctions;

var api = new TornadoApi("your-api-key");

// Create a conversation with function calling capabilities
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo,
    Tools =
    [
        new Tool((string city, ToolArguments args) =>
        {
            // Simulate getting weather data
            var weather = GetWeatherForCity(city);
            return $"The weather in {city} is {weather}";
        }, "get_weather", "Gets the current weather for a city")
    ],
    ToolChoice = OutboundToolChoice.Required
});

conversation.AddUserMessage("What's the weather like in Prague?");
ChatRichResponse response = await conversation.GetResponseRich();

Console.WriteLine(response.Content); // The AI will call the get_weather function
```

## Prerequisites

- Understanding of C# delegates and lambda expressions
- Knowledge of JSON serialization concepts
- Familiarity with the chat basics from previous documentation
- API access to a model that supports function calling (most modern models do)

## Detailed Explanation

### How Function Calling Works

Function calling follows this flow:

1. **Define Tools**: Create Tool objects with functions and descriptions
2. **Add to Conversation**: Include tools in the ChatRequest
3. **User Request**: User asks a question that might require a function
4. **AI Decision**: Model determines if/which function to call
5. **Function Execution**: LlmTornado executes the function
6. **Result Processing**: Function result is returned to the AI
7. **Final Response**: AI generates response based on function results

### Key Components

#### Tool Class
Represents a function the AI can call:

```csharp
public class Tool
{
    public Tool(Delegate function, string? name = null, string? description = null)
    public Tool(string type) // For advanced usage
    
    public string Type { get; set; } // Usually "function"
    public ToolFunction? Function { get; set; }
    public bool? Strict { get; set; }
}
```

#### ToolFunction
Contains the function schema:

```csharp
public class ToolFunction
{
    public string Name { get; set; }
    public string Description { get; set; }
    public object? Parameters { get; set; } // JSON schema
}
```

#### ToolArguments
Provides access to function arguments:

```csharp
public class ToolArguments
{
    public bool TryGetArgument<T>(string name, out T? value)
    public object? this[string name] { get; }
}
```

## Basic Usage

### Simple Function

```csharp
// Define a simple calculator function
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo,
    Tools =
    [
        new Tool((double num1, double num2, string operation, ToolArguments args) =>
        {
            double result = operation switch
            {
                "add" => num1 + num2,
                "subtract" => num1 - num2,
                "multiply" => num1 * num2,
                "divide" => num2 != 0 ? num1 / num2 : 0,
                _ => 0
            };
            
            return new { operation, num1, num2, result };
        }, "calculate", "Performs basic arithmetic operations")
    ],
    ToolChoice = OutboundToolChoice.Required
});

conversation.AddUserMessage("What is 15 multiplied by 4?");
var response = await conversation.GetResponseRich();
Console.WriteLine(response.Content);
```

### Multiple Functions

```csharp
// Define multiple related functions
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo,
    Tools =
    [
        new Tool((string city, ToolArguments args) =>
        {
            return $"Weather in {city}: Sunny, 22°C";
        }, "get_weather", "Gets current weather for a city"),
        
        new Tool((string city, ToolArguments args) =>
        {
            return $"Temperature in {city}: 22°C";
        }, "get_temperature", "Gets current temperature for a city"),
        
        new Tool((string city, ToolArguments args) =>
        {
            return $"Humidity in {city}: 65%";
        }, "get_humidity", "Gets current humidity for a city")
    ],
    ToolChoice = OutboundToolChoice.Auto
});

conversation.AddUserMessage("Tell me about the weather conditions in London.");
var response = await conversation.GetResponseRich();
```

### Function with Complex Parameters

```csharp
// Define a function with complex parameter types
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}

var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo,
    Tools =
    [
        new Tool((Person person, ToolArguments args) =>
        {
            // Validate the person data
            if (string.IsNullOrWhiteSpace(person.Name))
                return new { error = "Name is required" };
                
            if (person.Age < 0 || person.Age > 150)
                return new { error = "Invalid age" };
                
            if (!person.Email.Contains("@"))
                return new { error = "Invalid email format" };
                
            return new 
            { 
                success = true, 
                message = $"Successfully processed {person.Name}'s information",
                person = person 
            };
        }, "validate_person", "Validates person information")
    ]
});

conversation.AddUserMessage("Validate this person: Name: John Doe, Age: 30, Email: john@example.com");
var response = await conversation.GetResponseRich();
```

## Advanced Usage

### Schema Generation

LlmTornado automatically generates JSON schemas for your functions:

```csharp
// Complex function with automatic schema generation
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo,
    Tools =
    [
        new Tool((string location, DateTime date, int numberOfPeople, 
                 List<string> dietaryRestrictions, ToolArguments args) =>
        {
            // Restaurant search logic
            var restaurants = SearchRestaurants(location, date, numberOfPeople, dietaryRestrictions);
            return new 
            { 
                location, 
                date, 
                numberOfPeople, 
                dietaryRestrictions,
                recommendations = restaurants
            };
        }, "find_restaurant", "Finds restaurants based on criteria")
    ]
});
```

### Manual Schema Definition

For more control, you can define schemas manually:

```csharp
using LlmTornado.Code;

var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo,
    Tools =
    [
        new Tool(new ToolFunction("get_user", "Retrieves user information by ID", new
        {
            type = "object",
            properties = new
            {
                userId = new
                {
                    type = "string",
                    description = "The unique identifier of the user"
                }
            },
            required = new[] { "userId" }
        }))
    ]
});
```

### Function Metadata

Add additional metadata to control function behavior:

```csharp
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo,
    Tools =
    [
        new Tool((string query, ToolArguments args) =>
        {
            return SearchDatabase(query);
        }, 
        new ToolMetadata
        {
            Name = "advanced_search",
            Description = "Performs advanced search with filtering options",
            Params =
            [
                new ToolParamDefinition("limit", new ToolParamInteger("Maximum results to return", 10, 1, 100))
            ],
            Ignore = ["internal_only_param"] // Parameters to exclude from schema
        })
    ]
});
```

### Handling Function Results

```csharp
// Process function results with error handling
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo,
    Tools =
    [
        new Tool((string url, ToolArguments args) =>
        {
            try
            {
                var content = DownloadWebContent(url);
                return new { success = true, content = content.Substring(0, 500) };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }, "fetch_web_content", "Fetches content from a URL")
    ]
});

conversation.AddUserMessage("Get the content from https://example.com and summarize it.");
var response = await conversation.GetResponseRich();
```

### Conditional Function Calling

```csharp
// Only call functions under certain conditions
var conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt35Turbo,
    Tools =
    [
        new Tool((string location, ToolArguments args) =>
        {
            // Only process if location is valid
            if (!IsValidLocation(location))
                throw new ArgumentException($"Invalid location: {location}");
                
            return GetWeatherData(location);
        }, "get_weather", "Gets weather data for valid locations")
    ]
});

// Helper function
bool IsValidLocation(string location)
{
    var validLocations = new[] { "New York", "London", "Tokyo", "Paris" };
    return validLocations.Contains(location, StringComparer.OrdinalIgnoreCase);
}
```

## Best Practices

### 1. Clear Function Descriptions
- Write descriptive, action-oriented function descriptions
- Include examples of expected inputs and outputs
- Specify units and formats for parameters
- Document any constraints or limitations

```csharp
// Good description
new Tool((string city, ToolArguments args) =>
{
    return GetWeather(city);
}, "get_weather", "Gets current weather conditions for a specified city. City name should be in English and include country if ambiguous (e.g., 'London, UK' vs 'London, Ontario'). Returns temperature in Celsius and weather description.")

// Poor description
new Tool((string city, ToolArguments args) =>
{
    return GetWeather(city);
}, "get_weather", "Weather function")
```

### 2. Parameter Validation
- Validate all function parameters
- Provide clear error messages for invalid inputs
- Handle edge cases gracefully
- Use appropriate data types

```csharp
new Tool((int userId, ToolArguments args) =>
{
    if (userId <= 0)
        throw new ArgumentException("User ID must be a positive integer");
        
    if (!UserExists(userId))
        throw new KeyNotFoundException($"User with ID {userId} not found");
        
    return GetUser(userId);
}, "get_user", "Retrieves user information by ID")
```

### 3. Error Handling
- Implement comprehensive error handling
- Return structured error information
- Log errors for debugging
- Provide fallback responses when appropriate

```csharp
new Tool((string url, ToolArguments args) =>
{
    try
    {
        var content = DownloadContent(url);
        return new { success = true, content };
    }
    catch (Exception ex)
    {
        LogError($"Failed to download {url}: {ex.Message}");
        return new 
        { 
            success = false, 
            error = "Failed to retrieve content",
            details = ex.Message
        };
    }
}, "download_content", "Downloads content from a URL")
```

### 4. Performance Considerations
- Keep functions focused and efficient
- Avoid expensive operations in function calls
- Implement caching for repeated requests
- Consider rate limiting for external API calls

```csharp
// Use caching for expensive operations
private static readonly ConcurrentDictionary<string, WeatherData> _weatherCache = new();

new Tool((string city, ToolArguments args) =>
{
    var cacheKey = $"weather_{city}";
    
    if (_weatherCache.TryGetValue(cacheKey, out var cachedData))
        return cachedData;
        
    var freshData = GetWeatherFromApi(city);
    _weatherCache.TryAdd(cacheKey, freshData);
    
    return freshData;
}, "get_weather", "Gets weather data with caching")
```

### 5. Security
- Never expose sensitive information in function descriptions
- Validate and sanitize all inputs
- Implement proper authentication for external calls
- Consider rate limiting to prevent abuse

```csharp
// Sanitize inputs
new Tool((string userInput, ToolArguments args) =>
{
    var sanitized = SanitizeInput(userInput);
    if (!IsValidInput(sanitized))
        throw new SecurityException("Invalid input detected");
        
    return ProcessInput(sanitized);
}, "process_input", "Processes user input with security validation")
```

## Common Issues

### Schema Generation Problems
- **Issue**: Function parameters not recognized by AI
- **Solution**: Ensure parameter names are descriptive and types are clear
- **Prevention**: Test schema generation with simple examples first

### Function Execution Errors
- **Issue**: Functions throw exceptions during execution
- **Solution**: Implement proper error handling and validation
- **Prevention**: Add comprehensive input validation and try-catch blocks

### Infinite Loops
- **Issue**: AI keeps calling the same function repeatedly
- **Solution**: Add constraints to prevent recursive calls
- **Prevention**: Implement call limits and state tracking

### Performance Issues
- **Issue**: Slow response times due to expensive function calls
- **Solution**: Optimize functions and implement caching
- **Prevention**: Profile functions and identify bottlenecks

## API Reference

### Tool Class
- `Tool(Delegate function, string? name, string? description)` - Create tool from delegate
- `Tool(ToolFunction function)` - Create tool from ToolFunction
- `Tool(string type)` - Create tool with manual configuration

### ToolChoice Enum
- `OutboundToolChoice.Auto` - Let AI decide when to call functions
- `OutboundToolChoice.Required` - AI must call a function
- `OutboundToolChoice.None` - AI cannot call functions

### ToolMetadata
- `string Name` - Custom function name
- `string Description` - Function description
- `List<ToolParamDefinition> Params` - Parameter definitions
- `List<string> Ignore` - Parameters to exclude from schema

### ToolArguments
- `bool TryGetArgument<T>(string name, out T? value)` - Safely get argument
- `object? this[string name]` - Get argument by name
- `bool HasArgument(string name)` - Check if argument exists

## Related Topics

- [Advanced Function Calling](/chat/functions-advanced) - Complex patterns and best practices
- [Function Serialization](/chat/functions-serialization) - Schema generation and customization
- [Response Handling](/chat/responses) - Processing function call results
- [Error Handling](/chat/error-handling) - Handling function call errors
- [Streaming](/chat/streaming) - Streaming responses with function calls
