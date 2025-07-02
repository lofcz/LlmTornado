using System.Text;
using Flurl.Http;
using LlmTornado.Code;
using LlmTornado.Models;

namespace LlmTornado.Internal.OpenRouter;

class Program
{
    static async Task Main(string[] args)
    {
        // no auth needed
        List<RetrievedModel>? models = await new TornadoApi(LLmProviders.OpenRouter).Models.GetModels(LLmProviders.OpenRouter);
        models = models?.OrderBy(x => x.Name, StringComparer.Ordinal).ToList();
        
        StringBuilder sb = new StringBuilder();

        AppendLine("// This code was generated with LlmTornado.Internal.OpenRouter");
        AppendLine("// do not edit manually");
        AppendLine();
        AppendLine("using System.Collections.Generic;");
        AppendLine("using LlmTornado.Code.Models;");
        AppendLine("using LlmTornado.Code;");
        AppendLine();
        AppendLine("namespace LlmTornado.Chat.Models.OpenRouter;");
        AppendLine();
        AppendLine($"/// <summary>");
        AppendLine($"/// All models from Open Router.");
        AppendLine($"/// </summary>");
        AppendLine("public class ChatModelOpenRouterAll : IVendorModelClassProvider");
        AppendLine("{");

        int i = 0;
        
        foreach (RetrievedModel model in models)
        {
            string identifier = Normalize(model.Id);
            
            AppendLine($"/// <summary>", 1);
            AppendLine($"/// {model.Id}", 1);
            AppendLine($"/// </summary>", 1);
            AppendLine($"public static readonly ChatModel Model{identifier} = new ChatModel(\"{model.Id}\", \"{model.Id}\", LLmProviders.OpenRouter, {model.ContextLength});", 1);
            AppendLine();
            
            AppendLine($"/// <summary>", 1);
            AppendLine($"/// <inheritdoc cref=\"Model{identifier}\"/>", 1);
            AppendLine($"/// </summary>", 1);
            AppendLine($"public readonly ChatModel {identifier} = Model{identifier};", 1);

            if (i < models.Count - 1)
            {
                AppendLine();    
            }
            
            i++;
        }

        AppendLine();
        
        AppendLine($"/// <summary>", 1);
        AppendLine($"/// All known models from Open Router.", 1);
        AppendLine($"/// </summary>", 1);
        AppendLine("public static readonly List<IModel> ModelsAll =", 1);
        AppendLine("[", 1);
        
        foreach (RetrievedModel model in models)
        {
            string identifier = Normalize(model.Id);
            AppendLine($"Model{identifier},", 2);
        }
        
        AppendLine("];", 1);
        AppendLine();
        
        AppendLine($"/// <summary>", 1);
        AppendLine($"/// <inheritdoc cref=\"ModelsAll\"/>", 1);
        AppendLine($"/// </summary>", 1);
        AppendLine("public List<IModel> AllModels => ModelsAll;", 1);
        AppendLine();
        AppendLine("internal ChatModelOpenRouterAll()", 1);
        AppendLine("{", 1);
        AppendLine();
        AppendLine("}", 1);
        
        AppendLine("}");
        int z = 0;

        string code = sb.ToString().Trim();

        string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string combined = Path.Combine(assemblyPath, "..", "..", "..", "..", "LlmTornado", "Chat", "Models", "OpenRouter", "ChatModelOpenRouterAll.cs");
        string abs = Path.GetFullPath(combined);
        
        if (File.Exists(abs))
        {
            await File.WriteAllTextAsync(abs, code);
        }
        
        return;
        
        void AppendLine(string content = "", int identLevel = 0)
        {
            if (content.Length is 0)
            {
                sb.AppendLine();
                return;
            }

            if (identLevel > 0)
            {
                sb.Append(new string(' ', identLevel * 4));   
            }
            
            sb.AppendLine(content);
        }
    }
    
    static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        
        int lastSlashIndex = input.LastIndexOf('/');
        string relevantPart = lastSlashIndex > -1 ? input[(lastSlashIndex + 1)..] : input;
        string withoutDots = relevantPart.Replace(".", string.Empty);
        char[] delimiters = ['-', '_', ':'];
        string[] segments = withoutDots.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder resultBuilder = new StringBuilder();
        
        foreach (string segment in segments)
        {
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }

            resultBuilder.Append(char.ToUpperInvariant(segment[0]));
            
            if (segment.Length > 1)
            {
                resultBuilder.Append(segment.AsSpan(1));
            }
        }

        return resultBuilder.ToString();
    }
}