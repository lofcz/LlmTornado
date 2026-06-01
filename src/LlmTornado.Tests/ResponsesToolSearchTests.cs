using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Demo;
using LlmTornado.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

[TestFixture]
public class ResponsesToolSearchTests
{
    [SetUp]
    public async Task Setup()
    {
        await Program.SetupApi();
    }

    [Test]
    public void ToolSearchRequest_SerializesHostedNamespaceConfiguration()
    {
        ResponseRequest request = new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            InputString = "Look up customer_42 and list open orders.",
            Tools =
            [
                new ResponseToolNamespace
                {
                    Name = "crm",
                    Description = "CRM tools for customer lookup and order management.",
                    Tools =
                    [
                        new ResponseFunctionTool
                        {
                            Name = "get_customer_profile",
                            Description = "Fetch a customer profile by customer ID.",
                            Parameters = JObject.Parse("""
                                {
                                  "type": "object",
                                  "properties": { "customer_id": { "type": "string" } },
                                  "required": ["customer_id"],
                                  "additionalProperties": false
                                }
                                """),
                            DeferLoading = true
                        },
                        new ResponseFunctionTool
                        {
                            Name = "list_open_orders",
                            Description = "List open orders for a customer ID.",
                            Parameters = JObject.Parse("""
                                {
                                  "type": "object",
                                  "properties": { "customer_id": { "type": "string" } },
                                  "required": ["customer_id"],
                                  "additionalProperties": false
                                }
                                """),
                            DeferLoading = true
                        }
                    ]
                },
                new ResponseToolSearchTool()
            ]
        };

        string json = request.ToJson();
        JObject parsed = JObject.Parse(json);

        Assert.That(parsed["tools"]?.Type, Is.EqualTo(JTokenType.Array));
        JArray tools = (JArray)parsed["tools"]!;
        Assert.That(tools.Count, Is.EqualTo(2));

        JObject ns = (JObject)tools[0];
        Assert.That(ns["type"]?.ToString(), Is.EqualTo("namespace"));
        Assert.That(ns["name"]?.ToString(), Is.EqualTo("crm"));
        Assert.That(ns["tools"]?[0]?["defer_loading"]?.Value<bool>(), Is.True);

        JObject toolSearch = (JObject)tools[1];
        Assert.That(toolSearch["type"]?.ToString(), Is.EqualTo("tool_search"));
    }

    [Test]
    public void ToolSearchOutputItem_DeserializesFromApiShape()
    {
        const string json = """
            {
              "type": "tool_search_output",
              "id": "ts_out_1",
              "call_id": null,
              "execution": "server",
              "status": "completed",
              "tools": [
                {
                  "type": "function",
                  "name": "list_open_orders",
                  "description": "List open orders for a customer ID.",
                  "parameters": {
                    "type": "object",
                    "properties": { "customer_id": { "type": "string" } },
                    "required": ["customer_id"],
                    "additionalProperties": false
                  }
                }
              ]
            }
            """;

        ResponseToolSearchOutputItem? item = JsonConvert.DeserializeObject<ResponseToolSearchOutputItem>(json);

        Assert.That(item, Is.Not.Null);
        Assert.That(item!.Type, Is.EqualTo(ResponseOutputTypes.ToolSearchOutput));
        Assert.That(item.Execution, Is.EqualTo(ResponseToolSearchExecution.Server));
        Assert.That(item.Tools, Has.Count.EqualTo(1));
        Assert.That(item.Tools[0], Is.TypeOf<ResponseFunctionTool>());
        Assert.That(((ResponseFunctionTool)item.Tools[0]).Name, Is.EqualTo("list_open_orders"));
    }

    [Test]
    public async Task HostedToolSearch_Gpt54_LoadsDeferredToolsAndCallsFunction()
    {
        if (string.IsNullOrWhiteSpace(Program.ApiKeys?.OpenAi))
        {
            Assert.Ignore("OpenAI API key not configured.");
        }

        TornadoApi api = Program.Connect();
        Dictionary<string, string> customerProfiles = new Dictionary<string, string>
        {
            ["customer_42"] = """{"customer_id":"customer_42","full_name":"Avery Chen","tier":"enterprise"}"""
        };

        Dictionary<string, string> openOrders = new Dictionary<string, string>
        {
            ["customer_42"] = """[{"order_id":"ord_1042","status":"awaiting fulfillment"}]"""
        };

        ResponseRequest request = new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt54.V54,
            Instructions =
                "For customer questions, load the crm namespace before calling tools. " +
                "After calling tools, summarize the result briefly.",
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User,
                    "Look up customer_42 and list their open orders.")
            ],
            Tools =
            [
                new ResponseToolNamespace
                {
                    Name = "crm",
                    Description = "CRM tools for customer lookups and open orders.",
                    Tools =
                    [
                        new ResponseFunctionTool
                        {
                            Name = "get_customer_profile",
                            Description = "Fetch a CRM customer profile by customer ID.",
                            Parameters = JObject.Parse("""
                                {
                                  "type": "object",
                                  "properties": { "customer_id": { "type": "string" } },
                                  "required": ["customer_id"],
                                  "additionalProperties": false
                                }
                                """),
                            DeferLoading = true
                        },
                        new ResponseFunctionTool
                        {
                            Name = "list_open_orders",
                            Description = "List open orders for a customer ID.",
                            Parameters = JObject.Parse("""
                                {
                                  "type": "object",
                                  "properties": { "customer_id": { "type": "string" } },
                                  "required": ["customer_id"],
                                  "additionalProperties": false
                                }
                                """),
                            DeferLoading = true
                        }
                    ]
                },
                new ResponseToolSearchTool()
            ],
            ParallelToolCalls = false,
            Store = false
        };

        ResponseResult response = await api.Responses.CreateResponse(request);
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Output, Is.Not.Empty);

        List<ResponseToolSearchCallItem> searchCalls = response.Output.OfType<ResponseToolSearchCallItem>().ToList();
        List<ResponseToolSearchOutputItem> searchOutputs = response.Output.OfType<ResponseToolSearchOutputItem>().ToList();
        List<ResponseFunctionToolCallItem> functionCalls = response.Output.OfType<ResponseFunctionToolCallItem>().ToList();

        Console.WriteLine($"Tool search calls: {searchCalls.Count}");
        Console.WriteLine($"Tool search outputs: {searchOutputs.Count}");
        Console.WriteLine($"Function calls: {functionCalls.Count}");

        Assert.That(searchCalls, Is.Not.Empty, "Expected hosted tool search to emit tool_search_call items.");
        Assert.That(searchOutputs, Is.Not.Empty, "Expected hosted tool search to emit tool_search_output items.");
        Assert.That(searchOutputs[0].Tools, Is.Not.Empty, "Expected loaded tools in tool_search_output.");

        int iteration = 0;
        while (functionCalls.Count > 0 && iteration++ < 6)
        {
            List<ResponseInputItem> toolOutputs = [];

            foreach (ResponseFunctionToolCallItem call in functionCalls)
            {
                string output = call.Name switch
                {
                    "get_customer_profile" => customerProfiles.GetValueOrDefault(
                        JObject.Parse(call.Arguments ?? "{}")["customer_id"]?.ToString() ?? string.Empty,
                        """{"error":"customer not found"}"""),
                    "list_open_orders" => openOrders.GetValueOrDefault(
                        JObject.Parse(call.Arguments ?? "{}")["customer_id"]?.ToString() ?? string.Empty,
                        "[]"),
                    _ => """{"error":"unknown tool"}"""
                };

                toolOutputs.Add(new FunctionToolCallOutput
                {
                    CallId = call.CallId ?? call.Id ?? Guid.NewGuid().ToString("N"),
                    Output = output
                });
            }

            response = await api.Responses.CreateResponse(new ResponseRequest
            {
                Model = ChatModel.OpenAi.Gpt54.V54,
                PreviousResponseId = response.Id,
                InputItems = toolOutputs,
                Tools = request.Tools,
                ParallelToolCalls = false,
                Store = false
            });

            functionCalls = response.Output.OfType<ResponseFunctionToolCallItem>().ToList();
        }

        string? finalText = response.Output
            .OfType<ResponseOutputMessageItem>()
            .SelectMany(m => m.Content.OfType<ResponseOutputTextContent>())
            .Select(c => c.Text)
            .LastOrDefault(t => !string.IsNullOrWhiteSpace(t));

        Console.WriteLine($"Final response text: {finalText}");

        Assert.That(finalText, Is.Not.Null.And.Not.Empty);
        StringAssert.Contains("customer_42", finalText!, StringComparison.OrdinalIgnoreCase);
    }
}
