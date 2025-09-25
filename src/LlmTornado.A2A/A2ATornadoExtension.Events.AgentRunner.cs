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
    public static Artifact ToArtifact(this ChatRuntimeAgentRunnerEvents evt)
    {
        return evt.AgentRunnerEvent switch
        {
            AgentRunnerStreamingEvent e => e.ToArtifact(),
            _ => throw new NotSupportedException($"Event type {evt.GetType().Name} is not supported"),
        };
    }


    public static Artifact ToArtifact(this AgentRunnerToolInvokedEvent evt)
    {
        Artifact artifact = new Artifact()
        {
            Name = evt.EventType.ToString(),
        };
        artifact.Description = "Tool Invoked";
        artifact.Parts.Add(new TextPart()
        {
            Text = $@"
{evt.ToolCalled.Name} was invoked.

with Arguments = {evt.ToolCalled.Arguments}"
        });
        return artifact;
    }

    public static Artifact ToArtifact(this AgentRunnerToolCompletedEvent evt)
    {
        Artifact artifact = new Artifact()
        {
            Name = evt.EventType.ToString(),
        };
        artifact.Description = "Tool Completed";
        artifact.Parts.Add(new TextPart()
        {
            Text = $@"{evt.ToolCall.Name} has completed.
With Results: {evt.ToolCall.Result.RemoteContent?.ToString() ?? evt.ToolCall.Result.Content}"
        });
        return artifact;
    }

    public static Artifact ToArtifact(this AgentRunnerGuardrailTriggeredEvent evt)
    {
        Artifact artifact = new Artifact()
        {
            Name = evt.EventType.ToString(),
        };

        artifact.Description = "Guardrail Triggered";
        artifact.Parts.Add(new TextPart()
        {
            Text = $@"Guardrail was triggered. Reason: {evt.Reason}"
        });

        return artifact;
    }
}
