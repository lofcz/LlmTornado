using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.RegularExpressions;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Infra;

namespace LlmTornado.Demo;

public class InfraDemo : DemoBase
{
    enum Continents
    {
        Asia,
        Africa,
        NorthAmerica,
        SouthAmerica,
        Antarctica,
        Europe,
        Australia
    }

    class ComplexClass
    {
        public string ComplexClassString { get; set; }
        public ComplexClass2 Class2 { get; set; }
    }

    class ComplexClass2
    {
        public string ComplexClass2String { get; set; }
        public bool ComplexClass2Bool { get; set; }
    }

    class Person
    {
        public int Age { get; set; }
        public string Name { get; set; }
        public List<Hobby> Hobbies { get; set; }
        public List<string> Kids { get; set; }
    }

    public class Hobby
    {
        public string Name { get; set; }
    }

    [TornadoTest]
    public static async Task TornadoFunctionConcurrentCollection()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool((ConcurrentDictionary<string, string> gameShortcutNamePairs, ToolArguments args) =>
                {
                    Assert.That(gameShortcutNamePairs.Count, Is.GreaterThan(0));
                    return "";
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        conversation.AddUserMessage("Fill the provided JSON structure with mock data");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();

        int z = 0;
    }

    public interface IPaymentMethod
    {
    }

    public class CreditCard : IPaymentMethod
    {
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string CVV { get; set; }
        public string HolderName { get; set; }
    }

    public class BankTransfer : IPaymentMethod
    {
        public string AccountNumber { get; set; }
        public string BankCode { get; set; }
        public string IBAN { get; set; }
    }

    public class PayPal : IPaymentMethod
    {
        public string Email { get; set; }
    }

    [TornadoTest]
    public static async Task TornadoFunctionAnyOf()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool(([SchemaAnyOf(typeof(PayPal), typeof(BankTransfer))] IPaymentMethod paymentMethod, ToolArguments args) =>
                {
                    Assert.That(paymentMethod, Is.NotNull);
                    return $"Payment processed using {paymentMethod.GetType().Name}";
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        conversation.AddUserMessage("Process a payment using BankTransfer available payment method. Use realistic mock data.");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task TornadoTuple()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool((Tuple<string, int, bool, CreditCard> someTuple, ToolArguments args) =>
                {
                    Assert.That(someTuple.Item4.CVV.Length, Is.GreaterThan(0));
                    return;
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        conversation.AddUserMessage("Use realistic mock data for the provided function.");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task TornadoDescription()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool((
                    [Description("capital of Czech Republic")] string capital, 
                    ToolArguments args
                ) =>
                {
                    Assert.That(capital.ToLowerInvariant().Trim(), Is.EqualTo("prague"));
                    return;
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        conversation.AddUserMessage("Use realistic mock data for the provided function.");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task TornadoTupleSchema()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool(([SchemaTuple("gameName", "numberOfTheBeast", "ozzyIsAlive", "mockCard")] Tuple<string, int, bool, CreditCard> someTuple, ToolArguments args) =>
                {
                    Assert.That(someTuple.Item1.Length, Is.GreaterThan(0));
                    return;
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        conversation.AddUserMessage("Use realistic mock data for the provided function.");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task TornadoGenericNullable()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool((
                   [Description("a one-hot vector with length 10")] List<string?> oneHotVector, 
                    ToolArguments args) =>
                {
                    Assert.That(oneHotVector.Count, Is.EqualTo(10));
                    
                    // flaky, model randomly uses "0" / null
                    // Assert.That(oneHotVector.Count(x => x is null), Is.EqualTo(9));
                    // Assert.That(oneHotVector.Count(x => x is not null), Is.EqualTo(1));
                    return "";
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        conversation.AddUserMessage("Create mock data meeting the schema.");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task TornadoNullable()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool((
                    string? thisIsNull,
                    string? thisIsNotNull,
                    ToolArguments args) =>
                {
                    Assert.That(thisIsNull, Is.Null);
                    Assert.That(thisIsNotNull, Is.NotNull);
                    return "";
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        conversation.AddUserMessage("Create mock data meeting the schema. Call the function only once, make sure to provide null as type for thisIsNull argument.");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task TornadoFunctionAnyOfIList()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool(([SchemaAnyOf(typeof(PayPal), typeof(BankTransfer))] IList<IPaymentMethod> paymentMethod, ToolArguments args) =>
                {
                    // model sometimes creates only one, flaky
                    Assert.That(paymentMethod.Count, Is.GreaterThan(0));
                    return $"Payment processed using {paymentMethod.GetType().Name}";
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        conversation.AddUserMessage("Process mock payments using both available payment methods. Use both payment methods once.");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task TornadoRegex()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool(([Description("regex user asked to create")] Regex regex, ToolArguments args) =>
                {
                    Assert.That(regex, Is.NotNull);
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        conversation.AddUserMessage("Create a regex for validating e-mail address.");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task TornadoByteArray()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool((byte[] pngMagicHeader, Guid randomGuid, TimeSpan twoHoursThirtyMinutes, ToolArguments args) =>
                {
                    Assert.That(twoHoursThirtyMinutes.Hours, Is.EqualTo(2));
                    Assert.That(twoHoursThirtyMinutes.Minutes, Is.EqualTo(30));
                    return "";
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        conversation.AddUserMessage("Use realistic mock data for the provided function.");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task TornadoTask()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool(async (Task<CreditCard> creditCard, ToolArguments args) =>
                {
                    CreditCard cc = await creditCard;
                    Assert.That(cc.CVV.Length, Is.GreaterThan(0));
                    return "";
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        conversation.AddUserMessage("Use realistic mock data for the provided function.");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();

        int z = 0;
    }

    [TornadoTest]
    public static async Task TornadoFunction()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool((
                    string location,
                    Continents continent,
                    ComplexClass cls,
                    List<string> names,
                    List<Person> people,
                    string[] popularGames,
                    string[,] wonGameOfCheckers3x3useXOchars,
                    Continents[] allContinents,
                    string[][] rpgInventoryItemsUseXForEmpty,
                    HashSet<int> setOfUniqueInts,
                    object someDataAboutGames,
                    DateTime dateBattleOfVerdunStarted,
                    ToolArguments args) =>
                {
                    Assert.That(setOfUniqueInts.Count, Is.GreaterThan(0));
                    Assert.That(allContinents.Length, Is.EqualTo(2));
                    
                    // manual decoding example
                    if (args.TryGetArgument("people", out List<Person>? fetchedPeople))
                    {
                        foreach (Person person in fetchedPeople)
                        {
                            Console.WriteLine(person.Name);
                        }
                    }

                    return "";
                }, new ToolMetadata
                {
                    Params =
                    [
                        new ToolParamDefinition("allContinents", new ToolParamListEnum("continents", [nameof(Continents.Africa), nameof(Continents.Antarctica)]))
                    ],
                    Ignore = ["location"]
                })
            ],
            ToolChoice = OutboundToolChoice.Required
        });

        conversation.AddUserMessage("Fill the provided JSON structure with mock data");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();

        int z = 0;
    }

    [TornadoTest]
    public static async Task TornadoStructuredFunction()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            ResponseFormat = ChatRequestResponseFormats.StructuredJson(async (string location, Continents continent, ComplexClass cls, List<string> names, List<Person> people, Dictionary<string, string> gameShortcutNamePairs, HashSet<int> setOfInts) =>
            {
                await Task.Delay(500);
                Console.WriteLine("test");
                return "";
            })
        });

        conversation.AddUserMessage("Fill the provided JSON structure with mock data");

        TornadoRequestContent serialized = conversation.Serialize(new ChatRequestSerializeOptions
        {
            Pretty = true
        });

        Console.Write(serialized);

        var data = await conversation.GetResponseRich();


        int z = 0;
    }
}