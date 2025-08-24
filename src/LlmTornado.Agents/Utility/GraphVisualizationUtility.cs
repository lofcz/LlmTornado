using LlmTornado.Agents.ChatRuntime.Orchestration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Utility;

/// <summary>
/// Extension methods and utilities for visualizing state machines
/// </summary>
public static class OrchestrationVisualization
{
    /// <summary>
    /// Generates a Graphviz DOT format representation of the state machine
    /// </summary>
    /// <param name="stateMachine">The state machine to visualize</param>
    /// <param name="graphName">Optional name for the graph (default: "Orchestration")</param>
    /// <returns>DOT format string suitable for Graphviz rendering</returns>
    public static string ToDotGraph(this Orchestration stateMachine, string graphName = "Orchestration")
    {
        var dotBuilder = new StringBuilder();
        var visitedStates = new HashSet<string>();

        dotBuilder.AppendLine($"digraph {SanitizeDotName(graphName)} {{");
        dotBuilder.AppendLine("    rankdir=LR;");
        dotBuilder.AppendLine("    node [shape=rectangle, style=filled, fillcolor=lightblue];");
        dotBuilder.AppendLine();

        // Add states and transitions
        foreach (var state in stateMachine.Runnables)
        {
            AddStateToDotGraph(dotBuilder, state.Value, visitedStates);
        }

        dotBuilder.AppendLine("}");
        return dotBuilder.ToString();
    }

    /// <summary>
    /// Generates a Graphviz DOT format representation of the generic state machine
    /// </summary>
    /// <typeparam name="TInput">Input type</typeparam>
    /// <typeparam name="TOutput">Output type</typeparam>
    /// <param name="stateMachine">The state machine to visualize</param>
    /// <param name="graphName">Optional name for the graph (default: "Orchestration")</param>
    /// <returns>DOT format string suitable for Graphviz rendering</returns>
    public static string ToDotGraph<TInput, TOutput>(this Orchestration<TInput, TOutput> stateMachine, string graphName = "Orchestration")
    {
        var dotBuilder = new StringBuilder();
        var visitedStates = new HashSet<string>();

        dotBuilder.AppendLine($"digraph {SanitizeDotName(graphName)} {{");
        dotBuilder.AppendLine("    rankdir=LR;");
        dotBuilder.AppendLine("    node [shape=rectangle, style=filled, fillcolor=lightblue];");
        dotBuilder.AppendLine();

        // Mark start and result states with special styling
        if (stateMachine.InitialRunnable != null)
        {
            var startStateId = GetStateId(stateMachine.InitialRunnable);
            dotBuilder.AppendLine($"    {startStateId} [fillcolor=lightgreen, label=\"{GetStateLabel(stateMachine.InitialRunnable)}\\n(Start)\"];");
            AddStateToDotGraph(dotBuilder, stateMachine.InitialRunnable, visitedStates);
        }

        if (stateMachine.RunnableWithResult != null && stateMachine.RunnableWithResult != stateMachine.InitialRunnable)
        {
            var resultStateId = GetStateId(stateMachine.RunnableWithResult);
            dotBuilder.AppendLine($"    {resultStateId} [fillcolor=lightcoral, label=\"{GetStateLabel(stateMachine.RunnableWithResult)}\\n(Result)\"];");
            AddStateToDotGraph(dotBuilder, stateMachine.RunnableWithResult, visitedStates);
        }

        // Add other states
        foreach (var state in stateMachine.Runnables)
        {
            AddStateToDotGraph(dotBuilder, state.Value, visitedStates);
        }

        dotBuilder.AppendLine("}");
        return dotBuilder.ToString();
    }

    /// <summary>
    /// Generates a PlantUML state diagram representation of the state machine
    /// </summary>
    /// <param name="stateMachine">The state machine to visualize</param>
    /// <param name="title">Optional title for the diagram</param>
    /// <returns>PlantUML state diagram string</returns>
    public static string ToPlantUML(this Orchestration stateMachine, string title = "State Machine")
    {
        var plantUmlBuilder = new StringBuilder();
        var visitedStates = new HashSet<string>();

        plantUmlBuilder.AppendLine("@startuml");
        if (!string.IsNullOrEmpty(title))
        {
            plantUmlBuilder.AppendLine($"title {title}");
        }
        plantUmlBuilder.AppendLine();

        // Add states and transitions
        foreach (var state in stateMachine.Runnables)
        {
            AddStateToPlantUML(plantUmlBuilder, state.Value, visitedStates);
        }

        plantUmlBuilder.AppendLine("@enduml");
        return plantUmlBuilder.ToString();
    }

    /// <summary>
    /// Generates a PlantUML state diagram representation of the generic state machine
    /// </summary>
    /// <typeparam name="TInput">Input type</typeparam>
    /// <typeparam name="TOutput">Output type</typeparam>
    /// <param name="stateMachine">The state machine to visualize</param>
    /// <param name="title">Optional title for the diagram</param>
    /// <returns>PlantUML state diagram string</returns>
    public static string ToPlantUML<TInput, TOutput>(this Orchestration<TInput, TOutput> stateMachine, string title = "State Machine")
    {
        var plantUmlBuilder = new StringBuilder();
        var visitedStates = new HashSet<string>();

        plantUmlBuilder.AppendLine("@startuml");
        if (!string.IsNullOrEmpty(title))
        {
            plantUmlBuilder.AppendLine($"title {title}");
        }
        plantUmlBuilder.AppendLine();

        // Mark start state
        if (stateMachine.InitialRunnable != null)
        {
            plantUmlBuilder.AppendLine($"[*] --> {GetPlantUMLStateId(stateMachine.InitialRunnable)}");
            AddStateToPlantUML(plantUmlBuilder, stateMachine.InitialRunnable, visitedStates);
        }

        // Add other states
        foreach (var state in stateMachine.Runnables)
        {
            AddStateToPlantUML(plantUmlBuilder, state.Value, visitedStates);
        }

        // Mark result state
        if (stateMachine.RunnableWithResult != null)
        {
            plantUmlBuilder.AppendLine($"{GetPlantUMLStateId(stateMachine.RunnableWithResult)} --> [*]");
        }

        plantUmlBuilder.AppendLine("@enduml");
        return plantUmlBuilder.ToString();
    }

    private static void AddStateToDotGraph(StringBuilder dotBuilder, OrchestrationRunnableBase state, HashSet<string> visitedStates)
    {
        var stateId = GetStateId(state);

        if (visitedStates.Contains(stateId))
            return;

        visitedStates.Add(stateId);

        // Add state declaration if not already added
        if (!IsSpecialState(state))
        {
            var fillColor = GetDotStateColor(state);
            dotBuilder.AppendLine($"    {stateId} [fillcolor={fillColor}, label=\"{GetStateLabel(state)}\"];");
        }

        // Get transitions - try both the base class and derived class transition properties
        IEnumerable<OrchestrationAdvancer> transitions = null;

        // First try the base class property
        if (state.BaseAdvancers != null && state.BaseAdvancers.Any())
        {
            transitions = state.BaseAdvancers;
        }
        else
        {
            // Try to get transitions from the generic derived class using reflection
            var transitionsProperty = state.GetType().GetProperty("Transitions");
            if (transitionsProperty != null)
            {
                var genericTransitions = transitionsProperty.GetValue(state);
                if (genericTransitions is IEnumerable<OrchestrationAdvancer> enumerable)
                {
                    transitions = enumerable;
                }
            }
        }

        if (transitions != null)
        {
            foreach (var transition in transitions)
            {
                var NextRunnableId = GetStateId(transition.NextRunnable);
                var transitionLabel = GetTransitionLabel(transition);

                dotBuilder.AppendLine($"    {stateId} -> {NextRunnableId} [label=\"{transitionLabel}\"];");

                // Recursively add the next state
                AddStateToDotGraph(dotBuilder, transition.NextRunnable, visitedStates);
            }
        }
    }

    private static void AddStateToPlantUML(StringBuilder plantUmlBuilder, OrchestrationRunnableBase state, HashSet<string> visitedStates)
    {
        var stateId = GetPlantUMLStateId(state);

        if (visitedStates.Contains(stateId))
            return;

        visitedStates.Add(stateId);

        // Add state note if it has special properties
        if (state.AllowDeadEnd)
        {
            plantUmlBuilder.AppendLine($"{stateId} : Dead End State");
        }

        // Get transitions - try both the base class and derived class transition properties
        IEnumerable<OrchestrationAdvancer> transitions = null;

        // First try the base class property
        if (state.BaseAdvancers != null && state.BaseAdvancers.Any())
        {
            transitions = state.BaseAdvancers;
        }
        else
        {
            // Try to get transitions from the generic derived class using reflection
            var transitionsProperty = state.GetType().GetProperty("Transitions");
            if (transitionsProperty != null)
            {
                var genericTransitions = transitionsProperty.GetValue(state);
                if (genericTransitions is IEnumerable<OrchestrationAdvancer> enumerable)
                {
                    transitions = enumerable;
                }
            }
        }

        if (transitions != null)
        {
            foreach (var transition in transitions)
            {
                var NextRunnableId = GetPlantUMLStateId(transition.NextRunnable);
                var transitionLabel = GetTransitionLabel(transition);

                if (!string.IsNullOrEmpty(transitionLabel))
                {
                    plantUmlBuilder.AppendLine($"{stateId} --> {NextRunnableId} : {transitionLabel}");
                }
                else
                {
                    plantUmlBuilder.AppendLine($"{stateId} --> {NextRunnableId}");
                }

                // Recursively add the next state
                AddStateToPlantUML(plantUmlBuilder, transition.NextRunnable, visitedStates);
            }
        }
    }

    private static string GetStateId(OrchestrationRunnableBase state)
    {
        return SanitizeDotName(state.GetType().Name);
    }

    private static string GetPlantUMLStateId(OrchestrationRunnableBase state)
    {
        return SanitizePlantUMLName(state.GetType().Name);
    }

    private static string GetStateLabel(OrchestrationRunnableBase state)
    {
        var typeName = state.GetType().Name;
        var inputType = GetSimpleTypeName(state.GetInputType());
        var outputType = GetSimpleTypeName(state.GetOutputType());

        return $"{typeName}\\n({SanitizeDotName(inputType)} → {SanitizeDotName(outputType)})";
    }

    private static string GetTransitionLabel(OrchestrationAdvancer transition)
    {
        // Try to determine transition type and provide meaningful label
        switch (transition.type)
        {
            case "out":
                return "condition";
            case "in_out":
                return "convert";
            default:
                return "";
        }
    }

    private static string GetDotStateColor(OrchestrationRunnableBase state)
    {
        if (state.AllowDeadEnd)
            return "lightgray";

        return "lightblue";
    }

    private static bool IsSpecialState(OrchestrationRunnableBase state)
    {
        // Check if this state has already been styled as start or result state
        return false; // This is handled in the main method
    }

    private static string GetSimpleTypeName(Type type)
    {
        if (type == typeof(object))
            return "object";
        if (type == typeof(string))
            return "string";
        if (type == typeof(int))
            return "int";
        if (type.IsGenericType)
        {
            var genericTypeName = type.Name.Split('`')[0];
            var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetSimpleTypeName));
            return $"{genericTypeName}<{genericArgs}>";
        }

        return type.Name;
    }

    private static string SanitizeDotName(string name)
    {
        return name.Replace("<", "_").Replace(">", "_").Replace(",", "_").Replace(" ", "_").Replace("-", "_");
    }

    private static string SanitizePlantUMLName(string name)
    {
        return name.Replace("<", "_").Replace(">", "_").Replace(",", "_").Replace(" ", "_").Replace("-", "_");
    }
}
