using A2A;
using LlmTornado.Agents.DataModels;

namespace LlmTornado.A2A;
/// <summary>
/// ToDo:
/// - Handle more file types (e.g. video, other documents)
/// </summary>
public static partial class A2ATornadoExtension
{
    public static Artifact ToArtifact(this ChatRuntimeEvents evt)
    {
        return evt switch
        {
            ChatRuntimeAgentRunnerEvents e => e.ToArtifact(),
            ChatRuntimeOrchestrationEvent e => e.ToArtifact(),
            _ => throw new NotSupportedException($"Event type {evt.GetType().Name} is not supported"),
        };
    }
}
