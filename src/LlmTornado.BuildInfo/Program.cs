using System;
using System.IO;
using System.Text.Json;
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

    foreach (JsonProperty property in fingerprinting.EnumerateObject())
    {
        var key = property.Value.GetString();
        if (key is not null)
        {
            invertedFingerprinting[key] = property.Name;
        }
    }

    string? outputDir = Path.GetDirectoryName(filePath);
    Console.WriteLine($"Output directory resolved to: {outputDir}");
    
    if (outputDir is not null)
    {
        string outputPath = Path.Combine(outputDir, "bmeta.json");
        string outputJson = JsonSerializer.Serialize(invertedFingerprinting, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(outputPath, outputJson);
        Console.WriteLine($"Successfully created bmeta.json at {outputPath}");
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
