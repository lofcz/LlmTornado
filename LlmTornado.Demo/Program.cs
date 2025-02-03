using System.Reflection;
using Newtonsoft.Json;
using LlmTornado.Code;

namespace LlmTornado.Demo;

public class Program
{
    public static Keys ApiKeys { get; set; }

    public class AzureKey
    {
        public string Version { get; set; }
        public string ApiUrlFormat { get; set; }
        public string Key { get; set; }
    }
    
    public class Keys
    {
        public string OpenAi { get; set; }
        public string Anthropic { get; set; }
        public string Cohere { get; set; }
        public string Google { get; set; }
        public string Groq { get; set; }
        public AzureKey Azure { get; set; }
    }

    public static TornadoApi ConnectMulti(bool httpStrict = true)
    {
        TornadoApi tornadoApi = new TornadoApi([
            new ProviderAuthentication(LLmProviders.OpenAi, ApiKeys.OpenAi),
            new ProviderAuthentication(LLmProviders.Anthropic, ApiKeys.Anthropic),
            new ProviderAuthentication(LLmProviders.Cohere, ApiKeys.Cohere),
            new ProviderAuthentication(LLmProviders.Google, ApiKeys.Google),
            new ProviderAuthentication(LLmProviders.Groq, ApiKeys.Groq)
        ])
        {
            httpStrict = httpStrict
        };

        return tornadoApi;
    }
    
    public static TornadoApi Connect(LLmProviders provider = LLmProviders.OpenAi)
    {
        return ConnectMulti();
    }

    public static async Task<bool> SetupApi()
    {
        string? projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName;

        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            Console.WriteLine("Failed to read project directory path, see Program.cs, SetupApi()");
            return false;
        }

        string apiKeyFileLocation = Path.Join([projectDirectory, "apiKey.json"]);
        if (!File.Exists(apiKeyFileLocation))
        {
            Console.WriteLine("Please copy and paste apiKeyPrototype.json file in the same folder, rename the copy as apiKey.json and replace the string inside with your API key");
            return false;
        }

        string apiKey = await File.ReadAllTextAsync(apiKeyFileLocation);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("API key not set, please place your API key in apiKey.json file");
            return false;
        }

        ApiKeys = JsonConvert.DeserializeObject<Keys>(apiKey) ?? throw new Exception("Invalid content of apiKey.json");
        return true;
    }

    public static readonly Dictionary<string, MethodInfo> DemoDict = [];
    public static readonly List<Tuple<Type, Type>> DemoEnumTypes = [];
    
    static Program()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type[] types = assembly.GetTypes();

        foreach (Type type in types)
        {
            if (type.FullName is null)
            {
                continue;
            }
            
            object[] attrs = type.GetCustomAttributes(typeof(DemoEnumAttribute), false);

            if (attrs.Length > 0 && attrs[0] is DemoEnumAttribute dea)
            {
                DemoEnumTypes.Add(new Tuple<Type, Type>(type, dea.DemoType));
                continue;
            }

            if (type.FullName.EndsWith("Demo"))
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    DemoDict[$"{type.FullName}.{method.Name}"] = method;
                }   
            }
        }
    }

    static void ListDemos()
    {
        Console.Clear();
        Console.WriteLine($"Found {DemoDict.Count} demos.");

        int i = 1;
        
        foreach (KeyValuePair<string, MethodInfo> demo in DemoDict.OrderBy(x => x.Key, StringComparer.InvariantCultureIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"({i})");
            Console.ResetColor();
            
            Console.Write($" {demo.Key}");
            Console.WriteLine();
            i++;
        }        
        
        Console.WriteLine();
        Console.WriteLine($"Number to play:");
    }

    static async Task Read()
    {
        ListDemos();
        
        string? toPlay = Console.ReadLine();

        if (int.TryParse(toPlay, out int demoN) && demoN > 0 && demoN <= DemoDict.Count)
        {
            KeyValuePair<string, MethodInfo> selected = DemoDict.OrderBy(x => x.Key, StringComparer.InvariantCultureIgnoreCase).Skip(demoN - 1).FirstOrDefault();
            await (Task)selected.Value.Invoke(null, null);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Demo finished");
            Console.ResetColor();
            Console.ReadKey();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Invalid number, expected 1-{DemoDict.Count}");
        Console.ReadKey();
    }
    
    public static async Task Main(string[] args)
    {
        Console.Title = "LlmTornado Demo";

        if (!await SetupApi())
        {
            return;
        }

        while (true)
        {
            await Read();   
        }
    }
}