using LlmTornado.Chat.Models;
using LlmTornado.Code;
namespace LlmTornado.Agents
{
    internal class LTBasicHelloWorldStreaming
    {
        public async Task RunHelloWorldStreaming()
        {
            LLMTornadoModelProvider client =
              new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], true);

            Agent agent = new Agent(client, "Have fun");

            // Enhanced streaming callback to handle the new ModelStreamingEvents system
            void StreamingHandler(ModelStreamingEvents streamingEvent)
            {
                switch (streamingEvent.EventType)
                {
                    case ModelStreamingEventType.Created:
                        Console.WriteLine($"[STREAMING] Stream created (Sequence: {streamingEvent.SequenceId})");
                        break;
                    
                    case ModelStreamingEventType.OutputTextDelta:
                        if (streamingEvent is ModelStreamingOutputTextDeltaEvent deltaEvent)
                        {
                            Console.Write(deltaEvent.DeltaText); // Write the text delta directly
                        }
                        break;
                    
                    case ModelStreamingEventType.Completed:
                        Console.WriteLine($"\n[STREAMING] Stream completed (Sequence: {streamingEvent.SequenceId})");
                        break;
                    
                    case ModelStreamingEventType.Error:
                        if (streamingEvent is ModelStreamingErrorEvent errorEvent)
                        {
                            Console.WriteLine($"\n[STREAMING] Error: {errorEvent.ErrorMessage}");
                        }
                        break;
                    
                    case ModelStreamingEventType.ReasoningPartAdded:
                        if (streamingEvent is ModelStreamingReasoningPartAddedEvent reasoningEvent)
                        {
                            Console.WriteLine($"\n[REASONING] {reasoningEvent.DeltaText}");
                        }
                        break;
                    
                    default:
                        Console.WriteLine($"[STREAMING] Event: {streamingEvent.EventType} (Status: {streamingEvent.Status})");
                        break;
                }
            }

            RunResult result = await Runner.RunAsync(agent, "Hello Streaming World!", streaming: true, streamingCallback: StreamingHandler);
        }
    }
}
