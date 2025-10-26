using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Images;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.DataModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Agents;

public class ImageGenerationRunnable : OrchestrationRunnable<ArticleOutput, ImageOutput>
{
    private readonly TornadoAgent _promptAgent;
    private readonly TornadoApi _client;
    private readonly AppConfiguration _config;

    public ImageGenerationRunnable(
        TornadoApi client,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _client = client;
        _config = config;

        // Agent to generate image prompts
        var instructions = $"""
            You are an expert at creating DALL-E image generation prompts for technical blog articles.
            Your role is to generate descriptive, specific prompts that will create professional hero images.
            
            Guidelines for image prompts:
            1. Be specific and descriptive
            2. Include style guidance (e.g., "modern", "professional", "technical illustration")
            3. Specify composition and perspective
            4. Avoid text in images (DALL-E struggles with text)
            5. Focus on visual metaphors for technical concepts
            6. Keep prompts under 400 characters
            
            Style preferences:
            - Modern, clean aesthetic
            - Professional business/tech style
            - Suitable for blog hero images
            - Avoid cluttered or overly complex compositions
            
            Given an article title and description, generate a compelling DALL-E prompt
            that creates an appropriate hero image.
            """;

        var model = new ChatModel(config.Models.ImagePrompt);

        _promptAgent = new TornadoAgent(
            client: client,
            model: model,
            name: "Image Prompt Generator",
            instructions: instructions);
    }

    public override async ValueTask<ImageOutput> Invoke(RunnableProcess<ArticleOutput, ImageOutput> process)
    {
        if (!_config.ImageGeneration.Enabled)
        {
            return new ImageOutput
            {
                Url = string.Empty,
                AltText = process.Input.Title,
                PromptUsed = "Image generation disabled",
                Provider = "none"
            };
        }

        process.RegisterAgent(_promptAgent);

        var article = process.Input;

        try
        {
            // Step 1: Generate image prompt
            var promptRequest = $"""
                Generate a DALL-E image prompt for an article with:
                Title: {article.Title}
                Description: {article.Description}
                
                Create a professional, modern hero image prompt that captures the essence of this article.
                Return only the prompt text, nothing else.
                """;

            var promptConversation = await _promptAgent.RunAsync(promptRequest, singleTurn: true);
            var imagePrompt = promptConversation.Messages.Last().Content?.Trim() ?? 
                            $"Modern technical illustration representing {article.Title}";

            // Step 2: Generate image using configured model
            // LlmTornado will automatically route to the appropriate provider
            var imageUrl = await GenerateImage(imagePrompt);
            
            return new ImageOutput
            {
                Url = imageUrl,
                AltText = article.Title,
                PromptUsed = imagePrompt,
                Provider = _config.ImageGeneration.Model
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Image generation failed: {ex.Message}");
            
            return new ImageOutput
            {
                Url = string.Empty,
                AltText = article.Title,
                PromptUsed = $"Error: {ex.Message}",
                Provider = "error"
            };
        }
    }

    private async Task<string> GenerateImage(string prompt)
    {
        try
        {
            // LlmTornado handles multi-provider routing automatically
            var imageRequest = new ImageGenerationRequest(prompt)
            {
                Model = _config.ImageGeneration.Model,
                NumOfImages = 1
            };

            var result = await _client.ImageGenerations.CreateImage(imageRequest);
            
            if (result?.Data != null && result.Data.Count > 0)
            {
                return result.Data[0].Url ?? string.Empty;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Image generation API error: {ex.Message}");
            return string.Empty;
        }
    }
}

