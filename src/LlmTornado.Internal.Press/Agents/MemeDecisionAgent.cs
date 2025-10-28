using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.DataModels;
using LlmTornado.Internal.Press.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Chat;

namespace LlmTornado.Internal.Press.Agents;

/// <summary>
/// Decides whether an article should have memes and how many
/// </summary>
public class MemeDecisionRunnable : OrchestrationRunnable<ArticleOutput, MemeDecision>
{
    private readonly TornadoAgent _agent;
    private readonly AppConfiguration _config;

    public MemeDecisionRunnable(
        TornadoApi client,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _config = config;

        string instructions = """
                              You are a content strategist deciding whether articles should include memes.

                              Your role is to analyze articles and determine:
                              1. Should this article have memes? (considering tone, topic, and audience)
                              2. How many memes would be appropriate? (1-3 max)
                              3. What topics should the memes focus on?

                              Consider these factors:
                              - Article length (longer articles can support more memes)
                              - Topic type (technical tutorials vs. opinion pieces vs. news)
                              - Humor potential (some topics are naturally meme-friendly)
                              - Educational value (memes can help explain concepts)
                              - Professional tone (maintain credibility)

                              Guidelines:
                              - Highly technical/formal articles: 0-1 memes
                              - Tutorial/how-to articles: 1-2 memes (to break up content)
                              - Opinion/editorial pieces: 2-3 memes (more casual)
                              - News/announcements: 1 meme (if appropriate)
                              - Developer culture/career topics: 2-3 memes (highly meme-friendly)

                              IMPORTANT: Memes should enhance, not distract. When in doubt, suggest fewer memes.

                              Output your decision as structured JSON matching the MemeDecision schema.
                              """;

        ChatModel model = new ChatModel(config.MemeGeneration.MemeGenerationModel);

        _agent = new TornadoAgent(
            client: client,
            model: model,
            name: "Meme Decision Agent",
            instructions: instructions,
            outputSchema: typeof(MemeDecision),
            options: new ChatRequest() { Temperature = 0.7 });
    }

    public override async ValueTask<MemeDecision> Invoke(RunnableProcess<ArticleOutput, MemeDecision> process)
    {
        if (!_config.MemeGeneration.Enabled)
        {
            Console.WriteLine("  [MemeDecisionAgent] Meme generation is disabled");
            return new MemeDecision
            {
                ShouldGenerateMemes = false,
                MemeCount = 0,
                Topics = [],
                Reasoning = "Meme generation disabled in configuration"
            };
        }

        ArticleOutput article = process.Input;

        // If force is enabled, skip decision and always generate memes
        if (_config.MemeGeneration.Force)
        {
            Console.WriteLine($"  [MemeDecisionAgent] FORCED mode - always generating memes");
            string[] topics = MemeService.ExtractTopics(article);
            
            return new MemeDecision
            {
                ShouldGenerateMemes = true,
                MemeCount = _config.MemeGeneration.MaxMemesPerArticle,
                Topics = topics.Take(_config.MemeGeneration.MaxMemesPerArticle).ToArray(),
                Reasoning = "Forced meme generation (config.force = true)"
            };
        }

        try
        {
            process.RegisterAgent(_agent);

            Console.WriteLine($"  [MemeDecisionAgent] Analyzing article: {article.Title}");
            Console.WriteLine($"  [MemeDecisionAgent] Word count: {article.WordCount}, Tags: {string.Join(", ", article.Tags ?? [])}");

            // Extract article metadata for decision
            string lengthCategory = MemeService.GetArticleLengthCategory(article.WordCount);
            string[] topics = MemeService.ExtractTopics(article);

            string prompt = $"""
                             Analyze this article and decide if it should include memes:

                             **Title:** {article.Title}
                             **Description:** {article.Description}
                             **Word Count:** {article.WordCount} ({lengthCategory})
                             **Tags:** {string.Join(", ", article.Tags ?? [])}
                             **Key Topics:** {string.Join(", ", topics)}

                             **Article Preview (first 500 chars):**
                             {Snippet(article.Body, 500)}

                             Based on this information, decide:
                             1. Should this article have memes? (true/false)
                             2. How many memes? (0-{_config.MemeGeneration.MaxMemesPerArticle})
                             3. What topics should the memes cover? (be specific)

                             Consider the article's tone, technical depth, and target audience.
                             Memes should add value, not detract from professionalism.

                             Output your decision with clear reasoning.
                             """;

            try
            {
                Conversation conversation = await _agent.Run(prompt, maxTurns: 1);
                ChatMessage lastMessage = conversation.Messages.Last();

                MemeDecision? decision = await lastMessage.Content?.SmartParseJsonAsync<MemeDecision>(_agent);

                if (decision == null)
                {
                    Console.WriteLine($"  [MemeDecisionAgent] Failed to parse decision, defaulting to no memes");
                    return new MemeDecision
                    {
                        ShouldGenerateMemes = false,
                        MemeCount = 0,
                        Topics = [],
                        Reasoning = "Failed to parse decision"
                    };
                }

                // Enforce max memes limit
                if (decision.MemeCount > _config.MemeGeneration.MaxMemesPerArticle)
                {
                    decision.MemeCount = _config.MemeGeneration.MaxMemesPerArticle;
                }

                Console.WriteLine($"  [MemeDecisionAgent] Decision: {(decision.ShouldGenerateMemes ? "YES" : "NO")}");
                if (decision.ShouldGenerateMemes)
                {
                    Console.WriteLine($"  [MemeDecisionAgent] Meme count: {decision.MemeCount}");
                    Console.WriteLine($"  [MemeDecisionAgent] Topics: {string.Join(", ", decision.Topics ?? [])}");
                    Console.WriteLine($"  [MemeDecisionAgent] Reasoning: {Snippet(decision.Reasoning, 150)}");
                }

                return decision;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [MemeDecisionAgent] ✗ Error: {ex.Message}");
                Console.WriteLine($"  [MemeDecisionAgent] Gracefully skipping memes");
                return new MemeDecision
                {
                    ShouldGenerateMemes = false,
                    MemeCount = 0,
                    Topics = [],
                    Reasoning = $"Error during decision: {ex.Message}"
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [MemeDecisionAgent] ✗ Critical error: {ex.Message}");
            Console.WriteLine($"  [MemeDecisionAgent] Gracefully skipping memes");
            return new MemeDecision
            {
                ShouldGenerateMemes = false,
                MemeCount = 0,
                Topics = [],
                Reasoning = $"Critical error: {ex.Message}"
            };
        }
    }

    private string Snippet(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text))
            return "[empty]";

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }
}

