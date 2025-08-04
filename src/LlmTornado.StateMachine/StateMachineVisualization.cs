using System.Reflection;
using System.Text;

namespace LlmTornado.StateMachines
{
    /// <summary>
    /// Extension methods and utilities for visualizing state machines
    /// </summary>
    public static class StateMachineVisualization
    {
        /// <summary>
        /// Generates a Graphviz DOT format representation of the state machine
        /// </summary>
        /// <param name="stateMachine">The state machine to visualize</param>
        /// <param name="graphName">Optional name for the graph (default: "StateMachine")</param>
        /// <returns>DOT format string suitable for Graphviz rendering</returns>
        public static string ToDotGraph(this StateMachine stateMachine, string graphName = "StateMachine")
        {
            StringBuilder dotBuilder = new StringBuilder();
            HashSet<string> visitedStates = [];
            
            dotBuilder.AppendLine($"digraph {SanitizeDotName(graphName)} {{");
            dotBuilder.AppendLine("    rankdir=LR;");
            dotBuilder.AppendLine("    node [shape=rectangle, style=filled, fillcolor=lightblue];");
            dotBuilder.AppendLine();
            
            // Add states and transitions
            foreach (BaseState? state in stateMachine.States)
            {
                AddStateToDotGraph(dotBuilder, state, visitedStates);
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
        /// <param name="graphName">Optional name for the graph (default: "StateMachine")</param>
        /// <returns>DOT format string suitable for Graphviz rendering</returns>
        public static string ToDotGraph<TInput, TOutput>(this StateMachine<TInput, TOutput> stateMachine, string graphName = "StateMachine")
        {
            StringBuilder dotBuilder = new StringBuilder();
            HashSet<string> visitedStates = [];
            
            dotBuilder.AppendLine($"digraph {SanitizeDotName(graphName)} {{");
            dotBuilder.AppendLine("    rankdir=LR;");
            dotBuilder.AppendLine("    node [shape=rectangle, style=filled, fillcolor=lightblue];");
            dotBuilder.AppendLine();
            
            // Mark start and result states with special styling
            if (stateMachine.StartState != null)
            {
                string startStateId = GetStateId(stateMachine.StartState);
                dotBuilder.AppendLine($"    {startStateId} [fillcolor=lightgreen, label=\"{GetStateLabel(stateMachine.StartState)}\\n(Start)\"];");
                AddStateToDotGraph(dotBuilder, stateMachine.StartState, visitedStates);
            }
            
            if (stateMachine.ResultState != null && stateMachine.ResultState != stateMachine.StartState)
            {
                string resultStateId = GetStateId(stateMachine.ResultState);
                dotBuilder.AppendLine($"    {resultStateId} [fillcolor=lightcoral, label=\"{GetStateLabel(stateMachine.ResultState)}\\n(Result)\"];");
                AddStateToDotGraph(dotBuilder, stateMachine.ResultState, visitedStates);
            }
            
            // Add other states
            foreach (BaseState? state in stateMachine.States)
            {
                AddStateToDotGraph(dotBuilder, state, visitedStates);
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
        public static string ToPlantUml(this StateMachine stateMachine, string title = "State Machine")
        {
            StringBuilder plantUmlBuilder = new StringBuilder();
            HashSet<string> visitedStates = [];
            
            plantUmlBuilder.AppendLine("@startuml");
            if (!string.IsNullOrEmpty(title))
            {
                plantUmlBuilder.AppendLine($"title {title}");
            }
            plantUmlBuilder.AppendLine();
            
            // Add states and transitions
            foreach (BaseState state in stateMachine.States)
            {
                AddStateToPlantUml(plantUmlBuilder, state, visitedStates);
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
        public static string ToPlantUml<TInput, TOutput>(this StateMachine<TInput, TOutput> stateMachine, string title = "State Machine")
        {
            StringBuilder plantUmlBuilder = new StringBuilder();
            HashSet<string> visitedStates = [];
            
            plantUmlBuilder.AppendLine("@startuml");
            if (!string.IsNullOrEmpty(title))
            {
                plantUmlBuilder.AppendLine($"title {title}");
            }
            plantUmlBuilder.AppendLine();
            
            // Mark start state
            if (stateMachine.StartState != null)
            {
                plantUmlBuilder.AppendLine($"[*] --> {GetPlantUmlStateId(stateMachine.StartState)}");
                AddStateToPlantUml(plantUmlBuilder, stateMachine.StartState, visitedStates);
            }
            
            // Add other states
            foreach (BaseState? state in stateMachine.States)
            {
                AddStateToPlantUml(plantUmlBuilder, state, visitedStates);
            }
            
            // Mark result state
            if (stateMachine.ResultState != null)
            {
                plantUmlBuilder.AppendLine($"{GetPlantUmlStateId(stateMachine.ResultState)} --> [*]");
            }
            
            plantUmlBuilder.AppendLine("@enduml");
            return plantUmlBuilder.ToString();
        }

        private static void AddStateToDotGraph(StringBuilder dotBuilder, BaseState state, HashSet<string> visitedStates)
        {
            string stateId = GetStateId(state);
            
            if (!visitedStates.Add(stateId))
                return;

            // Add state declaration if not already added
            if (!IsSpecialState(state))
            {
                string fillColor = GetDotStateColor(state);
                dotBuilder.AppendLine($"    {stateId} [fillcolor={fillColor}, label=\"{GetStateLabel(state)}\"];");
            }
            
            // Get transitions - try both the base class and derived class transition properties
            IEnumerable<StateTransition>? transitions = null;
            
            // First try the base class property
            if (state.BaseTransitions != null && state.BaseTransitions.Any())
            {
                transitions = state.BaseTransitions;
            }
            else
            {
                // Try to get transitions from the generic derived class using reflection
                PropertyInfo? transitionsProperty = state.GetType().GetProperty("Transitions");
                if (transitionsProperty != null)
                {
                    object? genericTransitions = transitionsProperty.GetValue(state);
                    if (genericTransitions is IEnumerable<StateTransition> enumerable)
                    {
                        transitions = enumerable;
                    }
                }
            }
            
            if (transitions != null)
            {
                foreach (StateTransition? transition in transitions)
                {
                    string nextStateId = GetStateId(transition.NextState);
                    string transitionLabel = GetTransitionLabel(transition);
                    
                    dotBuilder.AppendLine($"    {stateId} -> {nextStateId} [label=\"{transitionLabel}\"];");
                    
                    // Recursively add the next state
                    AddStateToDotGraph(dotBuilder, transition.NextState, visitedStates);
                }
            }
        }

        private static void AddStateToPlantUml(StringBuilder plantUmlBuilder, BaseState state, HashSet<string> visitedStates)
        {
            string stateId = GetPlantUmlStateId(state);
            
            if (!visitedStates.Add(stateId))
                return;

            // Add state note if it has special properties
            if (state.IsDeadEnd)
            {
                plantUmlBuilder.AppendLine($"{stateId} : Dead End State");
            }
            
            // Get transitions - try both the base class and derived class transition properties
            IEnumerable<StateTransition>? transitions = null;
            
            // First try the base class property
            if (state.BaseTransitions != null && state.BaseTransitions.Any())
            {
                transitions = state.BaseTransitions;
            }
            else
            {
                // Try to get transitions from the generic derived class using reflection
                PropertyInfo? transitionsProperty = state.GetType().GetProperty("Transitions");
                
                if (transitionsProperty != null)
                {
                    object? genericTransitions = transitionsProperty.GetValue(state);
                    if (genericTransitions is IEnumerable<StateTransition> enumerable)
                    {
                        transitions = enumerable;
                    }
                }
            }
            
            if (transitions != null)
            {
                foreach (StateTransition transition in transitions)
                {
                    string nextStateId = GetPlantUmlStateId(transition.NextState);
                    string transitionLabel = GetTransitionLabel(transition);
                    
                    if (!string.IsNullOrEmpty(transitionLabel))
                    {
                        plantUmlBuilder.AppendLine($"{stateId} --> {nextStateId} : {transitionLabel}");
                    }
                    else
                    {
                        plantUmlBuilder.AppendLine($"{stateId} --> {nextStateId}");
                    }
                    
                    // Recursively add the next state
                    AddStateToPlantUml(plantUmlBuilder, transition.NextState, visitedStates);
                }
            }
        }

        private static string GetStateId(BaseState state)
        {
            return SanitizeDotName(state.GetType().Name);
        }

        private static string GetPlantUmlStateId(BaseState state)
        {
            return SanitizePlantUmlName(state.GetType().Name);
        }

        private static string GetStateLabel(BaseState state)
        {
            string typeName = state.GetType().Name;
            string inputType = GetSimpleTypeName(state.GetInputType());
            string outputType = GetSimpleTypeName(state.GetOutputType());
            
            return $"{typeName}\\n({SanitizeDotName(inputType)} → {SanitizeDotName(outputType)})";
        }

        private static string GetTransitionLabel(StateTransition transition)
        {
            // Try to determine transition type and provide meaningful label
            return transition.type switch
            {
                "out" => "condition",
                "in_out" => "convert",
                _ => ""
            };
        }

        private static string GetDotStateColor(BaseState state)
        {
            if (state.IsDeadEnd)
                return "lightgray";
            
            return state switch
            {
                ExitState => "lightcoral",
                DeadEnd => "lightgray",
                _ => "lightblue"
            };
        }

        private static bool IsSpecialState(BaseState state)
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
                string genericTypeName = type.Name.Split('`')[0];
                string genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetSimpleTypeName));
                return $"{genericTypeName}<{genericArgs}>";
            }
            
            return type.Name;
        }

        private static string SanitizeDotName(string name)
        {
            return name.Replace("<", "_").Replace(">", "_").Replace(",", "_").Replace(" ", "_").Replace("-", "_");
        }

        private static string SanitizePlantUmlName(string name)
        {
            return name.Replace("<", "_").Replace(">", "_").Replace(",", "_").Replace(" ", "_").Replace("-", "_");
        }
    }
}