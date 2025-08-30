using LlmTornado.Agents.ChatRuntime.Orchestration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Utility;

/// <summary>
/// Extension methods and utilities for visualizing Runner Records from orchestration execution
/// </summary>
public static class RunnerRecordVisualizationUtility
{
    /// <summary>
    /// Generates a Graphviz DOT format representation of the Runner Records
    /// </summary>
    /// <param name="runSteps">The orchestration run steps containing runner records</param>
    /// <param name="graphName">Optional name for the graph (default: "RunnerRecords")</param>
    /// <returns>DOT format string suitable for Graphviz rendering</returns>
    public static string ToRunnerRecordDotGraph(this ConcurrentDictionary<int, List<RunnerRecord>> runSteps, string graphName = "RunnerRecords")
    {
        var dotBuilder = new StringBuilder();
        var visitedStates = new HashSet<string>();
        var stateMetrics = new Dictionary<string, (int totalTokens, TimeSpan totalTime, int stepCount)>();

        dotBuilder.AppendLine($"digraph {SanitizeDotName(graphName)} {{");
        dotBuilder.AppendLine("    rankdir=LR;");
        dotBuilder.AppendLine("    node [shape=rectangle, style=filled];");
        dotBuilder.AppendLine("    edge [fontsize=10];");
        dotBuilder.AppendLine();

        // First pass: collect all states and their metrics
        foreach (var step in runSteps.OrderBy(kvp => kvp.Key))
        {
            foreach (var record in step.Value)
            {
                var stateName = record.RunnerName;
                if (stateMetrics.ContainsKey(stateName))
                {
                    var current = stateMetrics[stateName];
                    stateMetrics[stateName] = (
                        current.totalTokens + record.UsageTokens,
                        current.totalTime.Add(record.ExecutionTime),
                        current.stepCount + 1
                    );
                }
                else
                {
                    stateMetrics[stateName] = (record.UsageTokens, record.ExecutionTime, 1);
                }
            }
        }

        // Add nodes with metrics
        foreach (var stateMetric in stateMetrics)
        {
            var stateName = stateMetric.Key;
            var metrics = stateMetric.Value;
            var nodeId = SanitizeDotName(stateName);

            var label = $"{stateName}\\n" +
                       $"Tokens: {metrics.totalTokens}\\n" +
                       $"Time: {metrics.totalTime.TotalMilliseconds:F1}ms\\n" +
                       $"Executions: {metrics.stepCount}";

            var fillColor = GetNodeColor(metrics.totalTokens, metrics.totalTime);
            dotBuilder.AppendLine($"    {nodeId} [label=\"{label}\", fillcolor=\"{fillColor}\"];");
        }

        dotBuilder.AppendLine();

        // Add transitions
        var addedTransitions = new HashSet<string>();
        foreach (var step in runSteps.OrderBy(kvp => kvp.Key))
        {
            foreach (var record in step.Value)
            {
                foreach (var transition in record.TransitionRecords)
                {
                    var fromId = SanitizeDotName(transition.AdvancedFrom);
                    var toId = SanitizeDotName(transition.AdvancedTo);
                    var transitionKey = $"{fromId}->{toId}";

                    if (!addedTransitions.Contains(transitionKey))
                    {
                        addedTransitions.Add(transitionKey);
                        var label = $"Step {step.Key}";
                        dotBuilder.AppendLine($"    {fromId} -> {toId} [label=\"{label}\"];");
                    }
                }
            }
        }

        dotBuilder.AppendLine("}");
        return dotBuilder.ToString();
    }

    /// <summary>
    /// Generates a PlantUML sequence diagram representation of the Runner Records
    /// </summary>
    /// <param name="runSteps">The orchestration run steps containing runner records</param>
    /// <param name="title">Optional title for the diagram</param>
    /// <returns>PlantUML sequence diagram string</returns>
    public static string ToRunnerRecordPlantUML(this ConcurrentDictionary<int, List<RunnerRecord>> runSteps, string title = "Runner Records Flow")
    {
        var plantUmlBuilder = new StringBuilder();
        var stateMetrics = new Dictionary<string, (int totalTokens, TimeSpan totalTime, int stepCount)>();

        plantUmlBuilder.AppendLine("@startuml");
        if (!string.IsNullOrEmpty(title))
        {
            plantUmlBuilder.AppendLine($"title {title}");
        }
        plantUmlBuilder.AppendLine();

        // Collect metrics first
        foreach (var step in runSteps.OrderBy(kvp => kvp.Key))
        {
            foreach (var record in step.Value)
            {
                var stateName = record.RunnerName;
                if (stateMetrics.ContainsKey(stateName))
                {
                    var current = stateMetrics[stateName];
                    stateMetrics[stateName] = (
                        current.totalTokens + record.UsageTokens,
                        current.totalTime.Add(record.ExecutionTime),
                        current.stepCount + 1
                    );
                }
                else
                {
                    stateMetrics[stateName] = (record.UsageTokens, record.ExecutionTime, 1);
                }
            }
        }

        // Define states with their metrics
        foreach (var stateMetric in stateMetrics)
        {
            var stateName = SanitizePlantUMLName(stateMetric.Key);
            var metrics = stateMetric.Value;

            plantUmlBuilder.AppendLine($"state {stateName} {{");
            plantUmlBuilder.AppendLine($"  {stateName} : Tokens: {metrics.totalTokens}");
            plantUmlBuilder.AppendLine($"  {stateName} : Time: {metrics.totalTime.TotalMilliseconds:F1}ms");
            plantUmlBuilder.AppendLine($"  {stateName} : Executions: {metrics.stepCount}");
            plantUmlBuilder.AppendLine("}");
        }

        plantUmlBuilder.AppendLine();

        // Add start state
        plantUmlBuilder.AppendLine("[*] --> FirstState");

        // Add transitions
        var addedTransitions = new HashSet<string>();
        string? firstStateName = null;

        foreach (var step in runSteps.OrderBy(kvp => kvp.Key))
        {
            foreach (var record in step.Value)
            {
                if (firstStateName == null)
                {
                    firstStateName = SanitizePlantUMLName(record.RunnerName);
                    plantUmlBuilder.AppendLine($"FirstState --> {firstStateName}");
                }

                foreach (var transition in record.TransitionRecords)
                {
                    var fromState = SanitizePlantUMLName(transition.AdvancedFrom);
                    var toState = SanitizePlantUMLName(transition.AdvancedTo);
                    var transitionKey = $"{fromState}->{toState}";

                    if (!addedTransitions.Contains(transitionKey))
                    {
                        addedTransitions.Add(transitionKey);
                        plantUmlBuilder.AppendLine($"{fromState} --> {toState} : Step {step.Key}");
                    }
                }
            }
        }

        // Find potential end states (states that don't transition to others)
        Dictionary<string, object> allStates = stateMetrics.Keys.Select(SanitizePlantUMLName).ToHashSet();
        var sourceStates = new HashSet<string>();
        var targetStates = new HashSet<string>();

        foreach (var step in runSteps.OrderBy(kvp => kvp.Key))
        {
            foreach (var record in step.Value)
            {
                foreach (var transition in record.TransitionRecords)
                {
                    sourceStates.Add(SanitizePlantUMLName(transition.AdvancedFrom));
                    targetStates.Add(SanitizePlantUMLName(transition.AdvancedTo));
                }
            }
        }

        foreach (var endState in allStates)
        {
            if (!sourceStates.Contains(endState.Key) || !targetStates.Contains(endState.Key))
                plantUmlBuilder.AppendLine($"{endState} --> [*]");
        }

        plantUmlBuilder.AppendLine("@enduml");
        return plantUmlBuilder.ToString();
    }

    /// <summary>
    /// Saves the DOT graph to a file
    /// </summary>
    /// <param name="runSteps">The orchestration run steps containing runner records</param>
    /// <param name="filePath">Path where to save the .dot file</param>
    /// <param name="graphName">Optional name for the graph</param>
    /// <returns>Task representing the async file write operation</returns>
    public static void SaveRunnerRecordDotGraphToFileAsync(this ConcurrentDictionary<int, List<RunnerRecord>> runSteps, string filePath, string graphName = "RunnerRecords")
    {
        var dotContent = runSteps.ToRunnerRecordDotGraph(graphName);
        File.WriteAllText(filePath, dotContent);
    }

    /// <summary>
    /// Saves the PlantUML diagram to a file
    /// </summary>
    /// <param name="runSteps">The orchestration run steps containing runner records</param>
    /// <param name="filePath">Path where to save the .puml file</param>
    /// <param name="title">Optional title for the diagram</param>
    /// <returns>Task representing the async file write operation</returns>
    public static void SaveRunnerRecordPlantUMLToFileAsync(this ConcurrentDictionary<int, List<RunnerRecord>> runSteps, string filePath, string title = "Runner Records Flow")
    {
        var plantUMLContent = runSteps.ToRunnerRecordPlantUML(title);
        File.WriteAllText(filePath, plantUMLContent);
    }

    /// <summary>
    /// Generates a summary report of the Runner Records
    /// </summary>
    /// <param name="runSteps">The orchestration run steps containing runner records</param>
    /// <returns>String containing summary information</returns>
    public static string GetRunnerRecordSummary(this ConcurrentDictionary<int, List<RunnerRecord>> runSteps)
    {
        var summary = new StringBuilder();
        var stateMetrics = new Dictionary<string, (int totalTokens, TimeSpan totalTime, int stepCount)>();
        var totalSteps = 0;

        // Collect metrics
        foreach (var step in runSteps.OrderBy(kvp => kvp.Key))
        {
            totalSteps++;
            foreach (var record in step.Value)
            {
                var stateName = record.RunnerName;
                if (stateMetrics.ContainsKey(stateName))
                {
                    var current = stateMetrics[stateName];
                    stateMetrics[stateName] = (
                        current.totalTokens + record.UsageTokens,
                        current.totalTime.Add(record.ExecutionTime),
                        current.stepCount + 1
                    );
                }
                else
                {
                    stateMetrics[stateName] = (record.UsageTokens, record.ExecutionTime, 1);
                }
            }
        }

        summary.AppendLine("Runner Record Execution Summary");
        summary.AppendLine("==============================");
        summary.AppendLine($"Total Steps: {totalSteps}");
        summary.AppendLine($"Unique States: {stateMetrics.Count}");
        summary.AppendLine($"Total Tokens Used: {stateMetrics.Values.Sum(m => m.totalTokens)}");
        summary.AppendLine($"Total Execution Time: {TimeSpan.FromMilliseconds(stateMetrics.Values.Sum(m => m.totalTime.TotalMilliseconds)):g}");
        summary.AppendLine();

        summary.AppendLine("State Details:");
        summary.AppendLine("--------------");
        foreach (var stateMetric in stateMetrics.OrderByDescending(kvp => kvp.Value.totalTokens))
        {
            var stateName = stateMetric.Key;
            var metrics = stateMetric.Value;
            summary.AppendLine($"{stateName}:");
            summary.AppendLine($"  Executions: {metrics.stepCount}");
            summary.AppendLine($"  Total Tokens: {metrics.totalTokens}");
            summary.AppendLine($"  Total Time: {metrics.totalTime.TotalMilliseconds:F1}ms");
            summary.AppendLine($"  Avg Tokens/Execution: {(double)metrics.totalTokens / metrics.stepCount:F1}");
            summary.AppendLine($"  Avg Time/Execution: {metrics.totalTime.TotalMilliseconds / metrics.stepCount:F1}ms");
            summary.AppendLine();
        }

        return summary.ToString();
    }

    private static string GetNodeColor(int tokens, TimeSpan executionTime)
    {
        // Color coding based on resource usage
        if (tokens > 1000 || executionTime.TotalMilliseconds > 5000)
            return "lightcoral"; // High usage - red
        else if (tokens > 500 || executionTime.TotalMilliseconds > 2000)
            return "lightyellow"; // Medium usage - yellow
        else
            return "lightgreen"; // Low usage - green
    }

    private static string SanitizeDotName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "UnknownState";

        return name.Replace("<", "_").Replace(">", "_").Replace(",", "_")
                  .Replace(" ", "_").Replace("-", "_").Replace(".", "_")
                  .Replace("(", "_").Replace(")", "_").Replace("[", "_")
                  .Replace("]", "_").Replace("{", "_").Replace("}", "_");
    }

    private static string SanitizePlantUMLName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "UnknownState";

        return name.Replace("<", "_").Replace(">", "_").Replace(",", "_")
                  .Replace(" ", "_").Replace("-", "_").Replace(".", "_")
                  .Replace("(", "_").Replace(")", "_").Replace("[", "_")
                  .Replace("]", "_").Replace("{", "_").Replace("}", "_");
    }

    public static Dictionary<T, object> ToHashSet<T>(this IEnumerable<T> source)
    {
        Dictionary<T, object> dict = new Dictionary<T, object>();
        foreach (T item in source)
        {
            if (!dict.ContainsKey(item))
                dict[item] = null;
        }
        return dict;
    }
}