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
    public static string ToRunnerRecordDotGraph(this Dictionary<int, List<RunnerRecord>> runSteps, string graphName = "RunnerRecords")
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

        // Calculate totals for percentage calculations
        var totalTokensAll = stateMetrics.Values.Sum(m => m.totalTokens);
        var totalTimeAll = TimeSpan.FromMilliseconds(stateMetrics.Values.Sum(m => m.totalTime.TotalMilliseconds));

        // Add nodes with metrics including percentages
        foreach (var stateMetric in stateMetrics)
        {
            var stateName = stateMetric.Key;
            var metrics = stateMetric.Value;
            var nodeId = SanitizeDotName(stateName);

            var tokenPercentage = totalTokensAll > 0 ? (double)metrics.totalTokens / totalTokensAll * 100 : 0;
            var timePercentage = totalTimeAll.TotalMilliseconds > 0 ? metrics.totalTime.TotalMilliseconds / totalTimeAll.TotalMilliseconds * 100 : 0;

            var label = $"{stateName}\\n" +
                       $"Tokens: {metrics.totalTokens} ({tokenPercentage:F1}%)\\n" +
                       $"Time: {metrics.totalTime.TotalMilliseconds:F1}ms ({timePercentage:F1}%)\\n" +
                       $"Executions: {metrics.stepCount}";


            var fillColor = GetNodeColor((timePercentage+tokenPercentage)/200.0);
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
    public static string ToRunnerRecordPlantUML(this Dictionary<int, List<RunnerRecord>> runSteps, string title = "Runner Records Flow")
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

        // Calculate totals for percentage calculations
        var totalTokensAll = stateMetrics.Values.Sum(m => m.totalTokens);
        var totalTimeAll = TimeSpan.FromMilliseconds(stateMetrics.Values.Sum(m => m.totalTime.TotalMilliseconds));

        // Define states with their metrics including percentages
        foreach (var stateMetric in stateMetrics)
        {
            var stateName = SanitizePlantUMLName(stateMetric.Key);
            var metrics = stateMetric.Value;

            var tokenPercentage = totalTokensAll > 0 ? (double)metrics.totalTokens / totalTokensAll * 100 : 0;
            var timePercentage = totalTimeAll.TotalMilliseconds > 0 ? metrics.totalTime.TotalMilliseconds / totalTimeAll.TotalMilliseconds * 100 : 0;

            plantUmlBuilder.AppendLine($"state {stateName} {{");
            plantUmlBuilder.AppendLine($"  {stateName} : Tokens: {metrics.totalTokens} ({tokenPercentage:F1}%)");
            plantUmlBuilder.AppendLine($"  {stateName} : Time: {metrics.totalTime.TotalMilliseconds:F1}ms ({timePercentage:F1}%)");
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
    public static void SaveRunnerRecordDotGraphToFileAsync(this Dictionary<int, List<RunnerRecord>> runSteps, string filePath, string graphName = "RunnerRecords")
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
    public static void SaveRunnerRecordPlantUMLToFileAsync(this Dictionary<int, List<RunnerRecord>> runSteps, string filePath, string title = "Runner Records Flow")
    {
        var plantUMLContent = runSteps.ToRunnerRecordPlantUML(title);
        File.WriteAllText(filePath, plantUMLContent);
    }

    /// <summary>
    /// Generates a summary report of the Runner Records
    /// </summary>
    /// <param name="runSteps">The orchestration run steps containing runner records</param>
    /// <returns>String containing summary information</returns>
    public static string GetRunnerRecordSummary(this Dictionary<int, List<RunnerRecord>> runSteps)
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

        // Calculate totals for percentage calculations
        var totalTokensAll = stateMetrics.Values.Sum(m => m.totalTokens);
        var totalTimeAll = TimeSpan.FromMilliseconds(stateMetrics.Values.Sum(m => m.totalTime.TotalMilliseconds));

        summary.AppendLine("Runner Record Execution Summary");
        summary.AppendLine("==============================");
        summary.AppendLine($"Total Steps: {totalSteps}");
        summary.AppendLine($"Unique States: {stateMetrics.Count}");
        summary.AppendLine($"Total Tokens Used: {totalTokensAll}");
        summary.AppendLine($"Total Execution Time: {totalTimeAll:g}");
        summary.AppendLine();

        summary.AppendLine("State Details:");
        summary.AppendLine("--------------");
        foreach (var stateMetric in stateMetrics.OrderByDescending(kvp => kvp.Value.totalTokens))
        {
            var stateName = stateMetric.Key;
            var metrics = stateMetric.Value;

            var tokenPercentage = totalTokensAll > 0 ? (double)metrics.totalTokens / totalTokensAll * 100 : 0;
            var timePercentage = totalTimeAll.TotalMilliseconds > 0 ? metrics.totalTime.TotalMilliseconds / totalTimeAll.TotalMilliseconds * 100 : 0;

            summary.AppendLine($"{stateName}:");
            summary.AppendLine($"  Executions: {metrics.stepCount}");
            summary.AppendLine($"  Total Tokens: {metrics.totalTokens} ({tokenPercentage:F1}% of total)");
            summary.AppendLine($"  Total Time: {metrics.totalTime.TotalMilliseconds:F1}ms ({timePercentage:F1}% of total)");
            summary.AppendLine($"  Avg Tokens/Execution: {(double)metrics.totalTokens / metrics.stepCount:F1}");
            summary.AppendLine($"  Avg Time/Execution: {metrics.totalTime.TotalMilliseconds / metrics.stepCount:F1}ms");
            summary.AppendLine();
        }

        return summary.ToString();
    }

    private static string GetNodeColor(double overheadPercent)
    {
        if(overheadPercent > 1)
        {
            overheadPercent = 1.0;
        }
        if(overheadPercent < 0)
        {
            overheadPercent = 0.0;
        }
        return $"lightcoral;{overheadPercent}:lightgreen";
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