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
    public static Artifact ToArtifact(this AgentRunnerStreamingEvent evt)
    {
        return evt.ModelStreamingEvent switch
        {
            ModelStreamingOutputTextDeltaEvent e => e.ToArtifact(),
            _ => throw new NotSupportedException($"Event type {evt.GetType().Name} is not supported"),
        };
    }

    public static Artifact ToArtifact(this ModelStreamingOutputTextDeltaEvent e)
    {
        Artifact artifact = new Artifact()
        {
            Description = e.EventType.ToString(),
            Parts = new List<Part>(),
            Name = e.EventType.ToString(),
        };
        artifact.Description = e.GetType().Name;
        artifact.Parts.Add(new TextPart()
        {
            Text = e.DeltaText ?? ""
        });
        return artifact;
    }
}
