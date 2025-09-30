namespace LlmTornado.Agents.Samples.DataModels;

public struct AgentAction
{
    public string AgentType { get; set; }
    public string Action { get; set; }
    public string Reasoning { get; set; }
    public string Parameters { get; set; }
    public AgentAction(string agentType, string action, string reasoning, string parameters = "")
    {
        AgentType = agentType;
        Action = action;
        Reasoning = reasoning;
        Parameters = parameters;
    }
}

