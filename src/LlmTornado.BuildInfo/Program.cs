using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Collections.Generic;

if (args.Length == 0)
{
    Console.WriteLine("Please provide the path to dotnet.js");
    return;
}

string filePath = args[0];
Console.WriteLine($"Reading dotnet.js from: {filePath}");
string content = File.ReadAllText(filePath);

Match match = JsonRegex().Match(content);

if (match.Success)
{
    string json = match.Groups[1].ToString().Trim();
    JsonDocument bootConfig = JsonDocument.Parse(json);
    JsonElement resources = bootConfig.RootElement.GetProperty("resources");
    JsonElement fingerprinting = resources.GetProperty("fingerprinting");

    Dictionary<string, string> invertedFingerprinting = new Dictionary<string, string>();
    JsonObject assemblyResources = new JsonObject();
    
    foreach (JsonProperty property in fingerprinting.EnumerateObject())
    {
        var key = property.Value.GetString();
        if (key is not null)
        {
            invertedFingerprinting[key] = property.Name;

            if (key.EndsWith(".wasm"))
            {
                var dllKey = key.Replace(".wasm", ".dll");
                assemblyResources[dllKey] = property.Name;
            }
        }
    }

    string? outputDir = Path.GetDirectoryName(filePath);
    Console.WriteLine($"Output directory resolved to: {outputDir}");
    
    if (outputDir is not null)
    {
        // Write bmeta.json
        string bmetaOutputPath = Path.Combine(outputDir, "bmeta.json");
        string bmetaOutputJson = JsonSerializer.Serialize(invertedFingerprinting, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(bmetaOutputPath, bmetaOutputJson);
        Console.WriteLine($"Successfully created bmeta.json at {bmetaOutputPath}");
        
        // Write blazor.boot.json
        var bootJsonResources = new JsonObject();
        bootJsonResources["assembly"] = assemblyResources;
        bootJsonResources["pdb"] = new JsonObject();
        bootJsonResources["runtime"] = new JsonObject();
        
        var bootJson = new JsonObject
        {
            ["cacheBootResources"] = true,
            ["entryAssembly"] = "LlmTornado.Docs.dll",
            ["resources"] = bootJsonResources
        };
        
        string bootJsonOutputPath = Path.Combine(outputDir, "blazor.boot.json");
        string bootJsonOutputJson = bootJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(bootJsonOutputPath, bootJsonOutputJson);
        Console.WriteLine($"Successfully created blazor.boot.json at {bootJsonOutputPath}");
    }
    else
    {
        Console.WriteLine("Error: Could not determine the output directory.");
    }
}
else
{
    Console.WriteLine("Could not find json-start/json-end comments in the file.");
}
partial class Program
{
    [GeneratedRegex(@"\/\*json-start\*\/(.*?)\/\*json-end\*\/", RegexOptions.Singleline)]
    private static partial Regex JsonRegex();
}
