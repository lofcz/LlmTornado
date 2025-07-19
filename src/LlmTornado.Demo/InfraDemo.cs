using System.ComponentModel;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;

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
    public static async Task TornadoFunction()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool((string location, Continents continent, ComplexClass cls, List<string> names, List<Person> people) =>
                {
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
    
    [TornadoTest]
    public static async Task TornadoStructuredFunction()
    {
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            ResponseFormat = ChatRequestResponseFormats.StructuredJson((string location, Continents continent, ComplexClass cls, List<string> names, List<Person> people, Dictionary<string, string> gameShortcutNamePairs) =>
            {
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