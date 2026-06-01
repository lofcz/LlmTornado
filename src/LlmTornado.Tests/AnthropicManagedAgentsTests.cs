using System.Linq;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.ManagedAgents.Anthropic;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Unit and integration tests for Claude Managed Agents multiagent sessions and outcomes.
/// </summary>
[TestFixture]
public class AnthropicManagedAgentsTests
{
    private TornadoApi? _api;

    [SetUp]
    public void SetUp()
    {
        string? apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            return;
        }

        _api = new TornadoApi(LLmProviders.Anthropic, apiKey);
    }

    [Test]
    public void MultiagentConfig_SerializesCoordinatorRoster()
    {
        AnthropicManagedAgentCreateRequest request = new AnthropicManagedAgentCreateRequest
        {
            Name = "coordinator-test",
            Model = AnthropicManagedAgentModels.ClaudeHaiku45,
            System = "Coordinate work.",
            Tools = [new AnthropicManagedAgentToolset()],
            Multiagent = new AnthropicManagedAgentMultiagentConfig
            {
                Type = "coordinator",
                Agents =
                [
                    AnthropicManagedAgentRosterEntry.FromAgentId("agent_sub_1"),
                    AnthropicManagedAgentRosterEntry.FromAgent("agent_sub_2", version: 2),
                    AnthropicManagedAgentRosterEntry.Self
                ]
            }
        };

        string json = request.Serialize();
        JObject body = JObject.Parse(json);

        Assert.That(body["multiagent"]?["type"]?.ToString(), Is.EqualTo("coordinator"));
        JArray agents = (JArray)body["multiagent"]!["agents"]!;
        Assert.That(agents.Count, Is.EqualTo(3));
        Assert.That(agents[0].Type, Is.EqualTo(JTokenType.String));
        Assert.That(agents[0].ToString(), Is.EqualTo("agent_sub_1"));
        Assert.That(agents[1]["type"]?.ToString(), Is.EqualTo("agent"));
        Assert.That(agents[1]["version"]?.Value<int>(), Is.EqualTo(2));
        Assert.That(agents[2]["type"]?.ToString(), Is.EqualTo("self"));
    }

    [Test]
    public void DefineOutcomeEvent_SerializesTextRubric()
    {
        AnthropicManagedAgentSendEventsRequest request = new AnthropicManagedAgentSendEventsRequest
        {
            Events =
            [
                new AnthropicManagedAgentUserDefineOutcomeEvent
                {
                    Description = "Write summary.md",
                    Rubric = AnthropicManagedAgentRubric.Text("# Rubric\n- Has title"),
                    MaxIterations = 5
                }
            ]
        };

        string json = request.Serialize();
        JObject body = JObject.Parse(json);
        JObject evt = (JObject)body["events"]![0]!;

        Assert.That(evt["type"]?.ToString(), Is.EqualTo("user.define_outcome"));
        Assert.That(evt["description"]?.ToString(), Is.EqualTo("Write summary.md"));
        Assert.That(evt["rubric"]?["type"]?.ToString(), Is.EqualTo("text"));
        Assert.That(evt["max_iterations"]?.Value<int>(), Is.EqualTo(5));
    }

    [Test]
    public void SessionCreateRequest_SerializesAgentIdString()
    {
        AnthropicManagedAgentSessionCreateRequest request = new AnthropicManagedAgentSessionCreateRequest
        {
            Agent = AnthropicManagedAgentSessionAgent.FromId("agent_abc"),
            EnvironmentId = "env_xyz",
            Title = "Test session"
        };

        string json = request.Serialize();
        JObject body = JObject.Parse(json);

        Assert.That(body["agent"]?.ToString(), Is.EqualTo("agent_abc"));
        Assert.That(body["environment_id"]?.ToString(), Is.EqualTo("env_xyz"));
    }

    [Test]
    [Category("Integration")]
    public async Task Integration_MultiagentSession_WithOutcome()
    {
        if (_api is null)
        {
            Assert.Ignore("ANTHROPIC_API_KEY not set.");
        }

        string suffix = Guid.NewGuid().ToString("N")[..8];

        HttpCallResult<AnthropicManagedAgentEnvironment> envResult = await _api.AnthropicManagedAgentEnvironments.Create(
            new AnthropicManagedAgentEnvironmentCreateRequest
            {
                Name = $"tornado-test-env-{suffix}",
                Description = "LlmTornado managed agents integration test"
            });

        Assert.That(envResult.Ok, Is.True, envResult.Exception?.Message ?? envResult.Response);
        Assert.That(envResult.Data?.Id, Is.Not.Null.And.Not.Empty);

        HttpCallResult<AnthropicManagedAgent> workerResult = await _api.AnthropicManagedAgents.Create(new AnthropicManagedAgentCreateRequest
        {
            Name = $"tornado-worker-{suffix}",
            Model = AnthropicManagedAgentModels.ClaudeHaiku45,
            System = "You are a worker. When asked, reply with exactly: WORKER_DONE",
            Tools = [new AnthropicManagedAgentToolset()]
        });

        Assert.That(workerResult.Ok, Is.True, workerResult.Exception?.Message ?? workerResult.Response);

        HttpCallResult<AnthropicManagedAgent> coordinatorResult = await _api.AnthropicManagedAgents.Create(new AnthropicManagedAgentCreateRequest
        {
            Name = $"tornado-coordinator-{suffix}",
            Model = AnthropicManagedAgentModels.ClaudeHaiku45,
            System = "You coordinate tasks. Delegate simple echo tasks to the worker agent.",
            Tools = [new AnthropicManagedAgentToolset()],
            Multiagent = new AnthropicManagedAgentMultiagentConfig
            {
                Agents = [AnthropicManagedAgentRosterEntry.FromAgentId(workerResult.Data!.Id!)]
            }
        });

        Assert.That(coordinatorResult.Ok, Is.True);
        Assert.That(coordinatorResult.Data?.Multiagent?.Type, Is.EqualTo("coordinator"));
        Assert.That(coordinatorResult.Data?.Multiagent?.Agents, Has.Count.EqualTo(1));

        HttpCallResult<AnthropicManagedAgentSession> sessionResult = await _api.AnthropicManagedAgentSessions.Create(
            new AnthropicManagedAgentSessionCreateRequest
            {
                Agent = AnthropicManagedAgentSessionAgent.FromId(coordinatorResult.Data!.Id!),
                EnvironmentId = envResult.Data!.Id!,
                Title = $"multiagent-outcome-{suffix}"
            });

        Assert.That(sessionResult.Ok, Is.True, sessionResult.Exception?.Message ?? sessionResult.Response);
        string sessionId = sessionResult.Data!.Id!;

        HttpCallResult<object> outcomeResult = await _api.AnthropicManagedAgentSessions.SendEvents(
            sessionId,
            new AnthropicManagedAgentSendEventsRequest
            {
                Events =
                [
                    new AnthropicManagedAgentUserDefineOutcomeEvent
                    {
                        Description = "Create a one-line text file named result.txt containing exactly: OUTCOME_OK",
                        Rubric = AnthropicManagedAgentRubric.Text("- result.txt exists\n- Content is exactly OUTCOME_OK"),
                        MaxIterations = 3
                    }
                ]
            });

        Assert.That(outcomeResult.Ok, Is.True, outcomeResult.Exception?.Message ?? outcomeResult.Response);

        HttpCallResult<ListResponse<AnthropicManagedAgentEvent>> eventsResult =
            await _api.AnthropicManagedAgentSessions.ListEvents(sessionId);

        Assert.That(eventsResult.Ok, Is.True);
        Assert.That(eventsResult.Data?.Items, Is.Not.Null);

        // Poll session until idle or timeout
        AnthropicManagedAgentSession? session = null;
        for (int i = 0; i < 60; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            HttpCallResult<AnthropicManagedAgentSession> poll = await _api.AnthropicManagedAgentSessions.Get(sessionId);
            Assert.That(poll.Ok, Is.True);
            session = poll.Data;
            if (session?.Status is "idle" or "terminated")
            {
                break;
            }
        }

        Assert.That(session, Is.Not.Null);
        Assert.That(session!.Status, Is.AnyOf("idle", "terminated", "running"));

        HttpCallResult<ListResponse<AnthropicManagedAgentEvent>> finalEvents =
            await _api.AnthropicManagedAgentSessions.ListEvents(sessionId);
        Assert.That(finalEvents.Ok, Is.True);
        Assert.That(
            finalEvents.Data?.Items.Any(e => e.Type?.Contains("outcome") == true),
            Is.True,
            "Expected at least one outcome-related session event.");
    }

    [Test]
    [Category("Integration")]
    public async Task Integration_CreateAgentAndSession_Message()
    {
        if (_api is null)
        {
            Assert.Ignore("ANTHROPIC_API_KEY not set.");
        }

        string suffix = Guid.NewGuid().ToString("N")[..8];

        HttpCallResult<AnthropicManagedAgentEnvironment> envResult = await _api.AnthropicManagedAgentEnvironments.Create(
            new AnthropicManagedAgentEnvironmentCreateRequest { Name = $"tornado-msg-env-{suffix}" });
        Assert.That(envResult.Ok, Is.True, envResult.Exception?.Message ?? envResult.Response);

        HttpCallResult<AnthropicManagedAgent> agentResult = await _api.AnthropicManagedAgents.Create(new AnthropicManagedAgentCreateRequest
        {
            Name = $"tornado-agent-{suffix}",
            Model = AnthropicManagedAgentModels.ClaudeHaiku45,
            System = "Reply briefly.",
            Tools = [new AnthropicManagedAgentToolset()]
        });
        Assert.That(agentResult.Ok, Is.True);

        HttpCallResult<AnthropicManagedAgentSession> sessionResult = await _api.AnthropicManagedAgentSessions.Create(
            new AnthropicManagedAgentSessionCreateRequest
            {
                Agent = AnthropicManagedAgentSessionAgent.FromId(agentResult.Data!.Id!),
                EnvironmentId = envResult.Data!.Id!
            });
        Assert.That(sessionResult.Ok, Is.True);

        HttpCallResult<object> msgResult = await _api.AnthropicManagedAgentSessions.SendEvents(
            sessionResult.Data!.Id!,
            AnthropicManagedAgentSendEventsRequest.UserMessage("Say hello in one word."));
        Assert.That(msgResult.Ok, Is.True);
    }
}
