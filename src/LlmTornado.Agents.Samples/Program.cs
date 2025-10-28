// See https://aka.ms/new-console-template for more information
using LlmTornado.Agents.Samples.ContextController;
using LlmTornado.Code;
using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.Chat;
using System.ComponentModel;
using LlmTornado.Agents.DataModels;
using LlmTornado.Mcp;
using LlmTornado.Agents.Samples.ResearchAgent;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Responses;

DeepResearchAgentConfiguration config = new DeepResearchAgentConfiguration()
{
    MaxQueries = 8,
    MinWordCount = 1000
};

var runtime = new ChatRuntime(config);

var researchGoal = "Please write me a report on how to wire up a agent orchestration in C#'s opensource github project LLMTornado (https://github.com/lofcz/LlmTornado). " +
    "Please include a working example of how a I could create a Magnetic One implementation using their library.";

var research = await runtime.InvokeAsync(new ChatMessage(ChatMessageRoles.User, researchGoal));

Console.WriteLine("Final Research Report:");
Console.WriteLine(research.GetMessageContent());
