using A2A;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
