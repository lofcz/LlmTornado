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
        public string DeepSeek { get; set; }
        public string Mistral { get; set; }
        public string XAi { get; set; }
        public string Perplexity { get; set; }
        public string Voyage { get; set; }
        public string DeepInfra { get; set; }
    }

    public static TornadoApi ConnectMulti(bool httpStrict = true)
    {
        TornadoApi tornadoApi = new TornadoApi([
            new ProviderAuthentication(LLmProviders.OpenAi, ApiKeys.OpenAi),
            new ProviderAuthentication(LLmProviders.Anthropic, ApiKeys.Anthropic),
            new ProviderAuthentication(LLmProviders.Cohere, ApiKeys.Cohere),
            new ProviderAuthentication(LLmProviders.Google, ApiKeys.Google),
            new ProviderAuthentication(LLmProviders.Groq, ApiKeys.Groq),
            new ProviderAuthentication(LLmProviders.DeepSeek, ApiKeys.DeepSeek),
            new ProviderAuthentication(LLmProviders.Mistral, ApiKeys.Mistral),
            new ProviderAuthentication(LLmProviders.XAi, ApiKeys.XAi),
            new ProviderAuthentication(LLmProviders.Voyage, ApiKeys.Voyage),
            new ProviderAuthentication(LLmProviders.DeepInfra, ApiKeys.DeepInfra)
        ])
        {
            httpStrict = httpStrict
        };

        return tornadoApi;
    }
    
    public static TornadoApi Connect(bool httpStrict = true)
    {
        return ConnectMulti(httpStrict);
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

    public static readonly Dictionary<string, Tuple<MethodInfo, string?, Type, FlakyAttribute?>> DemoDict = [];
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
                    object[] testAttrs = method.GetCustomAttributes(typeof(TornadoTestAttribute), false);

                    if (testAttrs.Length > 0 && testAttrs[0] is TornadoTestAttribute tta)
                    {
                        object[] flaky = method.GetCustomAttributes(typeof(FlakyAttribute), false);
                        DemoDict[$"{type.FullName}.{method.Name}"] = new Tuple<MethodInfo, string?, Type, FlakyAttribute?>(method, tta.FriendlyName, type, flaky.Length is 0 ? null : flaky[0] as FlakyAttribute);   
                    }
                }   
            }
        }
    }

    static void ListDemos()
    {
        Console.Clear();
        Console.WriteLine($"Found {DemoDict.Count} demos.");

        int i = 1;
        
        foreach (KeyValuePair<string, Tuple<MethodInfo, string?, Type, FlakyAttribute?>> demo in DemoDict.OrderBy(x => x.Key, StringComparer.InvariantCultureIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"({i}");

            if (demo.Value.Item2?.Length > 0)
            {
                Console.Write($", {demo.Value.Item2}");
            }
            
            Console.Write(")");
            Console.ResetColor();
            
            Console.Write($" {demo.Key}");
            Console.WriteLine();
            i++;
        }        
        
        Console.WriteLine();
        Console.WriteLine($"Enter number of the demo (yellow) / friendly name of the demo / method name of demo to run:");
    }

    static async Task Read()
    {
        ListDemos();
        
        string? toPlay = Console.ReadLine();

        // 1. try to interpret as numeric input
        if (int.TryParse(toPlay, out int demoN) && demoN > 0 && demoN <= DemoDict.Count)
        {
            KeyValuePair<string, Tuple<MethodInfo, string?, Type, FlakyAttribute?>> selected = DemoDict.OrderBy(x => x.Key, StringComparer.InvariantCultureIgnoreCase).Skip(demoN - 1).FirstOrDefault();
            await (Task)selected.Value.Item1.Invoke(null, null);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Demo finished");
            Console.ResetColor();
            Console.ReadKey();
            return;
        }

        // 2. try looking up by friendly name (grossly ineffective..)
        foreach (KeyValuePair<string, Tuple<MethodInfo, string?, Type, FlakyAttribute?>> x in DemoDict)
        {
            if (x.Value.Item2 is null)
            {
                continue;
            }

            if (string.Equals(x.Value.Item2.Trim(), toPlay?.Trim()))
            {
                await (Task)x.Value.Item1.Invoke(null, null);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Demo finished");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }
        }
        
        // 3. interpret the input as method name
        foreach (KeyValuePair<string, Tuple<MethodInfo, string?, Type, FlakyAttribute?>> x in DemoDict)
        {
            if (x.Value.Item1.Name == toPlay?.Trim())
            {
                await (Task)x.Value.Item1.Invoke(null, null);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Demo finished");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }
        }

        // 4. yell at user
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