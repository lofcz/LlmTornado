using System.Text;
using Scriban;
using Scriban.Runtime;

namespace LlmTornado.Docs.Generator;

public class ScribanGenerator
{
    public static void Generate()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\.."));
        string contentDir = Path.Combine(projectRoot, "LlmTornado.Docs", "Content");
        string pagesDir = Path.Combine(projectRoot, "LlmTornado.Docs", "Pages", "Generated");
        
        if (!Directory.Exists(pagesDir))
        {
            Directory.CreateDirectory(pagesDir);
        }

        foreach (string file in Directory.GetFiles(contentDir, "*.sbn", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(file);
            Template template = Template.Parse(content);
            
            var scriptObject = new ScriptObject();
            scriptObject.MemberRenamer = member => member.Name;
            scriptObject.Import(typeof(ScribanFunctions));
            
            var context = new TemplateContext();
            context.PushGlobal(scriptObject);
            
            string result = template.Render(context);

            string relativePath = Path.GetRelativePath(contentDir, file);
            string newFileName = Path.ChangeExtension(relativePath, ".razor");
            string newFilePath = Path.Combine(pagesDir, newFileName);

            string? newFileDir = Path.GetDirectoryName(newFilePath);
            
            if (newFileDir is not null && !Directory.Exists(newFileDir))
            {
                Directory.CreateDirectory(newFileDir);
            }
            
            File.WriteAllText(newFilePath, result);
        }
    }
}

public static class ScribanFunctions
{
    public static string CodeSnippet(string code)
    {
        var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
        var codeVariableName = $"code_{guid}";
    
        return $@"
@{{
    var {codeVariableName} = @""{code.Trim().Replace("\"", "\"\"")}"";
}}
<CodeSnippet Code=""@{codeVariableName}"" />";
    }
}