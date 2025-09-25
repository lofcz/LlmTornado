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
    public static Artifact ToArtifact(this ChatRuntimeOrchestrationEvent evt)
    {
        return evt.OrchestrationEventData switch
        {
            OnVerboseOrchestrationEvent e => e.ToArtifact(),
            OnErrorOrchestrationEvent e => e.ToArtifact(),
            OnCancelledOrchestrationEvent e => e.ToArtifact(),
            OnBeginOrchestrationEvent e => e.ToArtifact(),
            OnFinishedOrchestrationEvent e => e.ToArtifact(),
            OnInitializedOrchestrationEvent e => e.ToArtifact(),
            OnStartedRunnableEvent e => e.ToArtifact(),
            OnFinishedRunnableEvent e => e.ToArtifact(),
            OnStartedRunnableProcessEvent e => e.ToArtifact(),
            OnFinishedRunnableProcessEvent e => e.ToArtifact(),
            _ => evt.OrchestrationEventData.DefaultOrchestrationEventArtifact()
        };
    }

    public static Artifact DefaultOrchestrationEventArtifact(this OrchestrationEvent e)
    {
        Artifact artifact = new Artifact();
        artifact.Description = "Agent Orchestration Event";
        artifact.Parts.Add(new TextPart()
        {
            Text = $"An orchestration event of type {e.Type} occurred."
        });
        return artifact;
    }

    public static Artifact ToArtifact(this OnVerboseOrchestrationEvent e)
    {
        Artifact artifact = new Artifact();
        artifact.Description = "Agent Verbose Orchestration Event";
        artifact.Parts.Add(new TextPart()
        {
            Text = e.Message ?? ""
        });
        return artifact;
    }


    public static Artifact ToArtifact(this OnErrorOrchestrationEvent e)
    {
        Artifact artifact = new Artifact();
        artifact.Description = "Agent Orchestration Error";
        artifact.Parts.Add(new TextPart()
        {
            Text = e.Exception?.ToString() ?? "Unknown error"
        });
        return artifact;
    }


    public static Artifact ToArtifact(this OnCancelledOrchestrationEvent e)
    {
        Artifact artifact = new Artifact();
        artifact.Description = "Agent Orchestration Cancelled";
        artifact.Parts.Add(new TextPart()
        {
            Text = "The orchestration was cancelled."
        });
        return artifact;
    }

    public static Artifact ToArtifact(this OnBeginOrchestrationEvent e)
    {
        Artifact artifact = new Artifact();
        artifact.Description = "Agent Orchestration Started";
        artifact.Parts.Add(new TextPart()
        {
            Text = "The orchestration has started."
        });
        return artifact;
    }

    public static Artifact ToArtifact(this OnFinishedOrchestrationEvent e)
    {
        Artifact artifact = new Artifact();
        artifact.Description = "Agent Orchestration Completed";
        artifact.Parts.Add(new TextPart()
        {
            Text = "The orchestration has completed."
        });
        return artifact;
    }


    public static Artifact ToArtifact(this OnInitializedOrchestrationEvent e)
    {
        Artifact artifact = new Artifact();
        artifact.Description = "Agent Orchestration Initialized";
        artifact.Parts.Add(new TextPart()
        {
            Text = "The orchestration has been initialized."
        });
        return artifact;
    }

    public static Artifact ToArtifact(this OnStartedRunnableEvent e)
    {
        Artifact artifact = new Artifact();
        artifact.Description = "Agent Orchestration Started Runnable";
        artifact.Parts.Add(new TextPart()
        {
            Text = $"The orchestration has started runnable: {e.RunnableBase.RunnableName}"
        });
        return artifact;
    }

    public static Artifact ToArtifact(this OnFinishedRunnableEvent e)
    {
        Artifact artifact = new Artifact();
        artifact.Description = "Agent Orchestration Completed Runnable";
        artifact.Parts.Add(new TextPart()
        {
            Text = $"The orchestration has completed runnable: {e.Runnable.RunnableName}"
        });
        return artifact;
    }

    public static Artifact ToArtifact(this OnStartedRunnableProcessEvent e)
    {
        Artifact artifact = new Artifact();
        artifact.Description = "Agent Started Runnable with process";
        artifact.Parts.Add(new TextPart()
        {
            Text = $@"
Agent Has Started Runnable {e.RunnableProcess.Runner.RunnableName} with process ID: {e.RunnableProcess.Id}  
Input Variables: {JsonSerializer.Serialize(e.RunnableProcess.BaseInput)}
"
        });
        return artifact;
    }

    public static Artifact ToArtifact(this OnFinishedRunnableProcessEvent e)
    {
        Artifact artifact = new Artifact();
        artifact.Description = "Agent Finished Runnable with process";
        artifact.Parts.Add(new TextPart()
        {
            Text = $@"
Agent Has Finished Runnable {e.RunnableProcess.Runner.RunnableName} with process ID: {e.RunnableProcess.Id}  

Process Duration: {e.RunnableProcess.RunnableExecutionTime.TotalSeconds} seconds

Token Usage: {e.RunnableProcess.TokenUsage}

Result Variables: {JsonSerializer.Serialize(e.RunnableProcess.BaseResult)}
"
        });
        return artifact;
    }



}
