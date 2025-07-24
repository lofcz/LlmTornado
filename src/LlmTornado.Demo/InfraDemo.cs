using System.Collections.Concurrent;
using System.ComponentModel;
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
                new Tool((ConcurrentDictionary<string, string> gameShortcutNamePairs, ToolArguments args) => { return ""; })
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
                    Ignore = ["wonGameOfCheckers3x3useXOchars"]
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