using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.DataModels;
using LlmTornado.Internal.Press.Services;
using LlmTornado.Mcp;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Images;
using System.Collections.Generic;

namespace LlmTornado.Internal.Press.Agents;

/// <summary>
/// Generates memes using MCP toolkit and validates them with vision model
/// </summary>
public class MemeGeneratorRunnable : OrchestrationRunnable<MemeDecision, MemeCollectionOutput>
{
    private readonly TornadoApi _client;
    private readonly AppConfiguration _config;
    private MCPServer? _mcpServer;
    private bool _mcpInitialized = false;
    private readonly HashSet<string> _usedTemplates = new HashSet<string>(); // Track used templates

    public MemeGeneratorRunnable(
        TornadoApi client,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _client = client;
        _config = config;
    }

    public override async ValueTask InitializeRunnable()
    {
        if (_mcpInitialized)
        {
            Console.WriteLine("  [MemeGeneratorAgent] Already initialized, skipping MCP setup");
            return;
        }

        _mcpInitialized = true;

        Console.WriteLine("  [MemeGeneratorAgent] Initializing MCP Meme Toolkit...");

        try
        {
            _mcpServer = MCPToolkits.MemeToolkit();
            await _mcpServer.InitializeAsync();
            Console.WriteLine($"  [MemeGeneratorAgent] ✓ MCP server initialized with {_mcpServer.Tools.Count} tools");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [MemeGeneratorAgent] ✗ Failed to initialize MCP: {ex.Message}");
            _mcpServer = null;
        }
    }

    public override async ValueTask<MemeCollectionOutput> Invoke(RunnableProcess<MemeDecision, MemeCollectionOutput> process)
    {
        var decision = process.Input;

        // If memes not needed, return empty result
        if (!decision.ShouldGenerateMemes || decision.MemeCount == 0)
        {
            Console.WriteLine("  [MemeGeneratorAgent] No memes requested");
            return new MemeCollectionOutput
            {
                Memes = Array.Empty<MemeGenerationOutput>(),
                Success = true,
                ErrorMessage = string.Empty
            };
        }

        // Ensure MCP is initialized
        if (_mcpServer == null)
        {
            await InitializeRunnable();
            if (_mcpServer == null)
            {
                Console.WriteLine("  [MemeGeneratorAgent] ✗ MCP server not available");
                return new MemeCollectionOutput
                {
                    Memes = Array.Empty<MemeGenerationOutput>(),
                    Success = false,
                    ErrorMessage = "MCP meme server failed to initialize"
                };
            }
        }

        Console.WriteLine($"  [MemeGeneratorAgent] Generating {decision.MemeCount} meme(s)");
        Console.WriteLine($"  [MemeGeneratorAgent] Topics: {string.Join(", ", decision.Topics ?? Array.Empty<string>())}");

        var memes = new List<MemeGenerationOutput>();

        // Generate each requested meme
        for (int i = 0; i < decision.MemeCount; i++)
        {
            try
            {
                var topic = decision.Topics != null && i < decision.Topics.Length
                    ? decision.Topics[i]
                    : decision.Topics?.FirstOrDefault() ?? "programming";

                Console.WriteLine($"  [MemeGeneratorAgent] [{i + 1}/{decision.MemeCount}] Generating meme about: {topic}");

                var meme = await GenerateSingleMemeAsync(topic, i + 1);

                if (meme != null && meme.Approved)
                {
                    memes.Add(meme);
                    Console.WriteLine($"  [MemeGeneratorAgent] ✓ Meme {i + 1} generated and validated");
                }
                else
                {
                    Console.WriteLine($"  [MemeGeneratorAgent] ✗ Meme {i + 1} failed validation after max iterations");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [MemeGeneratorAgent] ✗ Error generating meme {i + 1}: {ex.Message}");
            }
        }

        Console.WriteLine($"  [MemeGeneratorAgent] ✓ Complete: {memes.Count}/{decision.MemeCount} memes generated successfully");

        return new MemeCollectionOutput
        {
            Memes = memes.ToArray(),
            Success = memes.Count > 0,
            ErrorMessage = memes.Count == 0 ? "No memes were successfully generated" : string.Empty
        };
    }

    /// <summary>
    /// Generate a single meme with validation loop
    /// </summary>
    private async Task<MemeGenerationOutput?> GenerateSingleMemeAsync(string topic, int memeNumber)
    {
        string? memeUrl = null;
        string? publicUrl = null;
        string? localPath = null;
        int iteration = 0;
        string feedback = "";

        for (iteration = 0; iteration < _config.MemeGeneration.MaxIterations; iteration++)
        {
            try
            {
                // Phase 1: Generate meme using MCP toolkit
                Console.WriteLine($"  [MemeGeneratorAgent]   Iteration {iteration + 1}/{_config.MemeGeneration.MaxIterations}");
                
                memeUrl = await GenerateMemeWithMcpAsync(topic, feedback, memeNumber);

                if (string.IsNullOrEmpty(memeUrl))
                {
                    Console.WriteLine($"  [MemeGeneratorAgent]   No URL returned from meme generation");
                    continue;
                }

                Console.WriteLine($"  [MemeGeneratorAgent]   Generated URL: {memeUrl}");

                // Phase 2: Process through upload service if enabled
                publicUrl = await ImageUploadService.ProcessImageUrlAsync(
                    memeUrl,
                    _config.ImageUpload,
                    "MemeGenerator");

                // Phase 3: Download meme (use publicUrl for permanent storage)
                MemeService.EnsureOutputDirectory(_config.MemeGeneration.OutputDirectory);
                localPath = await MemeService.DownloadImageFromUrlAsync(
                    publicUrl,
                    _config.MemeGeneration.OutputDirectory,
                    $"meme_{memeNumber}_{iteration}.jpg"
                );

                // Phase 4: Validate with vision model
                var validation = await ValidateMemeWithVisionAsync(localPath, topic);

                Console.WriteLine($"  [MemeGeneratorAgent]   Validation score: {validation.Score:F2}, Approved: {validation.Approved}");

                if (validation.Approved && validation.Score >= _config.MemeGeneration.MinValidationScore)
                {
                    // Success!
                    return new MemeGenerationOutput
                    {
                        Url = publicUrl,
                        LocalPath = localPath,
                        Caption = topic,
                        ValidationScore = validation.Score,
                        Feedback = validation.Issues,
                        IterationCount = iteration + 1,
                        Approved = true,
                        Topic = topic
                    };
                }

                // Not approved, prepare feedback for next iteration
                feedback = BuildFeedbackMessage(validation);
                Console.WriteLine($"  [MemeGeneratorAgent]   Feedback: {Snippet(feedback, 100)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [MemeGeneratorAgent]   Error in iteration {iteration + 1}: {ex.Message}");
            }
        }

        // Max iterations reached without approval
        // Use the last publicUrl we generated
        return localPath != null ? new MemeGenerationOutput
        {
            Url = publicUrl ?? memeUrl ?? string.Empty,
            LocalPath = localPath,
            Caption = topic,
            ValidationScore = 0,
            Feedback = new[] { "Max iterations reached" },
            IterationCount = iteration,
            Approved = false,
            Topic = topic
        } : null;
    }

    /// <summary>
    /// Generate meme using MCP toolkit with 3-step workflow:
    /// 1. Model selects template
    /// 2. We generate empty template preview with placeholders
    /// 3. Model generates actual meme seeing the preview
    /// </summary>
    private async Task<string?> GenerateMemeWithMcpAsync(string topic, string feedback, int memeNumber)
    {
        // STEP 1: Model selects the template
        Console.WriteLine($"  [MemeGeneratorAgent]   Step 1: Selecting template...");
        var (selectedTemplateId, textLineCount) = await SelectTemplateAsync(topic, memeNumber);
        
        if (string.IsNullOrEmpty(selectedTemplateId))
        {
            Console.WriteLine($"  [MemeGeneratorAgent]   ✗ Failed to select template");
            return null;
        }

        Console.WriteLine($"  [MemeGeneratorAgent]   ✓ Selected template: {selectedTemplateId} (expects {textLineCount} lines)");

        // STEP 2: Generate empty template preview with placeholder text
        Console.WriteLine($"  [MemeGeneratorAgent]   Step 2: Generating template preview...");
        string? templatePreviewUrl = await GenerateTemplatePreviewAsync(selectedTemplateId, textLineCount);
        
        if (string.IsNullOrEmpty(templatePreviewUrl))
        {
            Console.WriteLine($"  [MemeGeneratorAgent]   ⚠ Failed to generate template preview, continuing without it");
        }
        else
        {
            Console.WriteLine($"  [MemeGeneratorAgent]   ✓ Template preview: {templatePreviewUrl}");
        }

        // STEP 3: Model generates actual meme text seeing the preview
        Console.WriteLine($"  [MemeGeneratorAgent]   Step 3: Generating meme with model...");
        
        string? capturedUrl = null;
        
        var instructions = $"""
            You are an EDGY meme text generator for developers. You have already selected the template '{selectedTemplateId}'.
            
            {(!string.IsNullOrEmpty(templatePreviewUrl) ? "You will be shown an EMPTY template with placeholder text to understand the layout." : "")}
            
            Your task: Generate EXACTLY {textLineCount} text lines for the meme about: {topic}
            
            IMPORTANT RULES:
            1. Generate EXACTLY {textLineCount} lines of text (no more, no less)
            2. Text must be SHORT and PUNCHY (max 5-8 words per line)
            3. Keep it relevant to: {topic}
            4. Make it FUNNY, EDGY, and RELATABLE for developers
            5. Be SARCASTIC, use IRONY, or be CONTROVERSIAL
            6. Reference real developer pain points, frustrations, or memes
            7. Don't be corporate or safe - developers appreciate dark humor
            {(!string.IsNullOrEmpty(templatePreviewUrl) ? "8. Study the PREVIEW IMAGE to see where text will be placed" : "")}
            
            {(string.IsNullOrEmpty(feedback) ? "" : $"Previous feedback to improve: {feedback}\n")}
            
            Examples of ACTUALLY FUNNY memes:
            - "Works on my machine" / "Guess we're shipping your machine" (classic, still hits)
            - "Git pull" / "847 merge conflicts" (pain everyone knows)
            - "Just gonna fix one small bug" / "3 AM rewriting entire codebase" (relatable escalation)
            - "Documentation?" / "We don't do that here" (Wakanda meme reference)
            - "Production server on fire" / "This is fine" (classic "this is fine" dog)
            
            WHAT ACTUALLY MAKES IT FUNNY:
            - SHORT: Max 5 words per line
            - OBVIOUS: The joke is immediately clear
            - REAL: Everyone has lived this exact moment
            - SIMPLE: No trying to be clever, just true
            
            Call 'generate_meme' with:
            - template_id: '{selectedTemplateId}'
            - text_lines: ["line 1 with normal spaces", "line 2 with normal spaces", ...]
            
            Use NORMAL text with spaces in the array. The tool will handle URL encoding.
            
            After you get the meme URL, call 'handoff_result' with it.
            """;

        // Use vision-enabled model
        var model = new ChatModel(_config.MemeGeneration.VisionModel);

        var memeAgent = new TornadoAgent(
            client: _client,
            model: model,
            name: $"MemeTextGenerator{memeNumber}",
            instructions: instructions,
            temperature: 1);

        // Add MCP tools
        if (_mcpServer != null)
        {
            memeAgent.AddMcpTools(_mcpServer.AllowedTornadoTools.ToArray());
        }

        // Add handoff tool to capture the meme URL
        memeAgent.AddTornadoTool(new Tool(
            ([Description("URL to the generated meme")] string url) =>
            {
                capturedUrl = url;
                memeAgent.Cancel();
                return "Meme URL captured successfully";
            },
            "handoff_result"
        ));

        try
        {
            Conversation conversation;
            
            if (!string.IsNullOrEmpty(templatePreviewUrl))
            {
                // Download the preview image and convert to base64 data URI
                Console.WriteLine($"  [MemeGeneratorAgent]   Downloading preview image...");
                
                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(templatePreviewUrl);
                var base64Image = Convert.ToBase64String(imageBytes);
                
                // Determine mime type from URL
                string mimeType = templatePreviewUrl.EndsWith(".png", StringComparison.OrdinalIgnoreCase) 
                    ? "image/png" 
                    : "image/jpeg";
                
                // Create data URI with proper format: data:image/png;base64,<base64data>
                string dataUri = $"data:{mimeType};base64,{base64Image}";
                
                Console.WriteLine($"  [MemeGeneratorAgent]   Preview downloaded ({imageBytes.Length} bytes, {mimeType})");
                
                // Show the preview to help with text placement
                conversation = await memeAgent.Run([
                    new ChatMessagePart($"Here is the EMPTY template '{selectedTemplateId}' with placeholder text showing where your {textLineCount} lines will go:"),
                    new ChatMessagePart(dataUri, ImageDetail.Auto),
                    new ChatMessagePart($"Now generate the actual meme text about: {topic}")
                ], maxTurns: 10);
            }
            else
            {
                // No preview available
                conversation = await memeAgent.Run($"Generate the meme now with {textLineCount} lines of text", maxTurns: 10);
            }

            // Try to extract URL from conversation if handoff wasn't called
            if (string.IsNullOrEmpty(capturedUrl))
            {
                var lastMessage = conversation.Messages.LastOrDefault()?.Content;
                if (!string.IsNullOrEmpty(lastMessage))
                {
                    capturedUrl = MemeService.ExtractMemeUrlFromContent(lastMessage);
                }
            }

            return capturedUrl;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [MemeGeneratorAgent]   MCP generation error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Validate meme quality using vision model
    /// </summary>
    private async Task<MemeValidationResult> ValidateMemeWithVisionAsync(string imagePath, string topic)
    {
        try
        {
            var instructions = """
                You are a meme quality validator. Analyze the meme image and determine if it meets quality standards.
                
                Check for:
                1. **Text Placement**: Is the text readable and properly positioned?
                2. **Text Quality**: Is the text clear, not cut off, and properly formatted?
                3. **Visual Quality**: Is the image clear and not distorted?
                4. **Humor**: Is the meme funny or at least mildly amusing?
                5. **Relevance**: Does the meme relate to the intended topic?
                6. **Professionalism**: Is it appropriate for a technical/developer audience?
                
                Score the meme from 0.0 to 1.0, where:
                - 0.0-0.4: Poor quality, reject
                - 0.5-0.6: Acceptable but needs improvement
                - 0.7-0.8: Good quality
                - 0.9-1.0: Excellent quality
                
                Approve the meme if score >= 0.7 and there are no critical issues.
                Provide specific feedback on any issues found.
                """;

            var visionModel = new ChatModel(_config.MemeGeneration.VisionModel);

            var visionAgent = new TornadoAgent(
                client: _client,
                model: visionModel,
                name: "MemeValidator",
                instructions: instructions,
                outputSchema: typeof(MemeValidationResult),
                temperature: 0.3);

            // Convert local file to base64 data URI for vision model
            string imageDataUri;
            if (File.Exists(imagePath))
            {
                byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
                string base64Image = Convert.ToBase64String(imageBytes);
                string mimeType = imagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
                imageDataUri = $"data:{mimeType};base64,{base64Image}";
            }
            else
            {
                // If it's already a URL, use it directly
                imageDataUri = imagePath;
            }
            
            var conversation = await visionAgent.Run([
                new ChatMessagePart($"Validate this meme about '{topic}'. Is the text placement correct? Is it funny? Does it make sense?"),
                new ChatMessagePart(imageDataUri, ImageDetail.Auto)
            ]);
            
            var lastMessage = conversation.Messages.Last();

            var validation = await lastMessage.Content?.SmartParseJsonAsync<MemeValidationResult>(visionAgent);

            if (validation == null)
            {
                return new MemeValidationResult
                {
                    Approved = false,
                    Score = 0,
                    Issues = new[] { "Failed to parse validation result" },
                    Suggestions = Array.Empty<string>(),
                    Summary = "Validation failed"
                };
            }

            return validation;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [MemeGeneratorAgent]   Vision validation error: {ex.Message}");
            return new MemeValidationResult
            {
                Approved = false,
                Score = 0,
                Issues = new[] { $"Validation error: {ex.Message}" },
                Suggestions = Array.Empty<string>(),
                Summary = "Validation exception"
            };
        }
    }

    /// <summary>
    /// Build feedback message from validation result
    /// </summary>
    private string BuildFeedbackMessage(MemeValidationResult validation)
    {
        var feedback = $"Score: {validation.Score:F2}\n";

        if (validation.Issues != null && validation.Issues.Length > 0)
        {
            feedback += "\nIssues:\n";
            foreach (var issue in validation.Issues)
            {
                feedback += $"- {issue}\n";
            }
        }

        if (validation.Suggestions != null && validation.Suggestions.Length > 0)
        {
            feedback += "\nSuggestions:\n";
            foreach (var suggestion in validation.Suggestions)
            {
                feedback += $"- {suggestion}\n";
            }
        }

        return feedback;
    }

    private string Snippet(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text))
            return "[empty]";

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// Step 1: Model selects appropriate template for the topic
    /// </summary>
    private async Task<(string? templateId, int lineCount)> SelectTemplateAsync(string topic, int memeNumber)
    {
        try
        {
            var usedTemplatesList = _usedTemplates.Count > 0 
                ? string.Join(", ", _usedTemplates) 
                : "none";

            var instructions = $"""
                You are a meme template selector. Given a topic, select the BEST meme template.
                
                Topic: {topic}
                
                **IMPORTANT**: These templates have ALREADY been used: {usedTemplatesList}
                You MUST select a DIFFERENT template. Do NOT reuse any of these.
                
                Your task:
                1. Use 'list_templates' to see available templates
                2. Choose ONE template that best fits the topic and humor style
                3. ENSURE the template is NOT in the already-used list
                4. Use 'get_template' to get details about your chosen template
                5. Call 'handoff_template' with the template_id and line_count
                
                Consider:
                - Does the template's format match the joke structure?
                - Is it popular and recognizable among developers?
                - Will it be FUNNY and maybe a bit edgy?
                - Is it DIFFERENT from already-used templates?
                """;

            var model = new ChatModel(_config.MemeGeneration.MemeGenerationModel);
            
            string? selectedTemplate = null;
            int lineCount = 2; // Default

            var selectorAgent = new TornadoAgent(
                client: _client,
                model: model,
                name: $"TemplateSelector{memeNumber}",
                instructions: instructions,
                temperature: 0.7);

            // Add MCP tools
            if (_mcpServer != null)
            {
                selectorAgent.AddMcpTools(_mcpServer.AllowedTornadoTools.ToArray());
            }

            // Add handoff tool to capture the selection
            selectorAgent.AddTornadoTool(new Tool(
                ([Description("ID of the selected template")] string template_id,
                 [Description("Number of text lines the template expects")] int lines) =>
                {
                    selectedTemplate = template_id;
                    lineCount = lines;
                    selectorAgent.Cancel();
                    return $"Template '{template_id}' with {lines} lines selected";
                },
                "handoff_template"
            ));

            await selectorAgent.Run("Select the best template for this topic", maxTurns: 10);

            // Track the selected template to avoid reuse
            if (!string.IsNullOrEmpty(selectedTemplate))
            {
                _usedTemplates.Add(selectedTemplate);
                Console.WriteLine($"  [MemeGeneratorAgent]   Template '{selectedTemplate}' added to used list (total: {_usedTemplates.Count})");
            }

            return (selectedTemplate, lineCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [MemeGeneratorAgent]   Template selection error: {ex.Message}");
            return (null, 0);
        }
    }

    /// <summary>
    /// Step 2: Generate empty template preview with placeholder text
    /// </summary>
    private async Task<string?> GenerateTemplatePreviewAsync(string templateId, int lineCount)
    {
        try
        {
            if (_mcpServer == null)
                return null;

            // Generate placeholder text based on line count
            var placeholderLines = new List<string>();
            for (int i = 1; i <= lineCount; i++)
            {
                placeholderLines.Add($"LINE_{i}_TEXT_HERE");
            }

            Console.WriteLine($"  [MemeGeneratorAgent]     Generating preview with {lineCount} placeholder lines");

            // Create a simple agent to call the MCP tool
            string? capturedPreviewUrl = null;
            
            var previewAgent = new TornadoAgent(
                client: _client,
                model: new ChatModel(_config.MemeGeneration.MemeGenerationModel),
                name: "PreviewGenerator",
                instructions: $"Generate a preview meme using template '{templateId}' with placeholder text",
                temperature: 0);

            // Add MCP tools
            previewAgent.AddMcpTools(_mcpServer.AllowedTornadoTools.ToArray());

            // Add handoff tool to capture the URL
            previewAgent.AddTornadoTool(new Tool(
                ([Description("URL to the generated preview meme")] string url) =>
                {
                    capturedPreviewUrl = url;
                    previewAgent.Cancel();
                    return "Preview captured";
                },
                "handoff_preview"
            ));

            // Build the text_lines as JSON array string
            var textLinesJson = System.Text.Json.JsonSerializer.Serialize(placeholderLines);

            // Run the agent with explicit instructions
            await previewAgent.Run($"Call generate_meme with template_id='{templateId}' and text_lines={textLinesJson}, then call handoff_preview with the URL", maxTurns: 5);

            return capturedPreviewUrl;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [MemeGeneratorAgent]     Preview generation error: {ex.Message}");
            return null;
        }
    }
}

