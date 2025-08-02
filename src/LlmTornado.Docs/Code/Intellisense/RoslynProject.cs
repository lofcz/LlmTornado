using System.Collections.Immutable;
using System.Reflection;
using System.ComponentModel;
using System.Data;
using System.Text.Json;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace LlmTornado.Docs.Code.Intellisense;

public class RoslynProject
{
    public static List<MetadataReference> MetadataReferences = new();
    private string Uri { get; init; }
    
    public RoslynProject(string uri)
    {
        Uri = uri;
    }

    public async Task Init(HttpClient httpClient)
    {
        ImmutableArray<Assembly> defaultAssemblies = MefHostServices.DefaultAssemblies;
        IEnumerable<Assembly> assembliesToLoad = defaultAssemblies.Concat([
            typeof(Arcade.ArcadeAssembly).Assembly
        ]);
        
        MefHostServices host = MefHostServices.Create(assembliesToLoad);
        Workspace = new AdhocWorkspace(host);

        if (MetadataReferences.Count == 0)
        {
            try
            {
                string[] neededAssemblies = new[]
                {
                    "System.Runtime.dll",
                    "System.Collections.dll",
                    "netstandard.dll",
                    "System.dll",
                    "System.Console.dll",
                    "System.Private.CoreLib.dll",
                    "System.ComponentModel.Primitives.dll",
                    "System.Linq.dll",
                    "System.Data.Common.dll",
                    "System.Private.Xml.dll",
                    "System.ObjectModel.dll",
                    "System.Linq.Expressions.dll",
                    "Microsoft.CodeAnalysis.dll",
                    "Microsoft.CodeAnalysis.CSharp.dll",
                    "Microsoft.CodeAnalysis.Workspaces.dll",
                    "Microsoft.CodeAnalysis.CSharp.Workspaces.dll",
                    "System.Reflection.Metadata.dll",
                    "System.Collections.Immutable.dll",
                    "System.Memory.dll"
                };

                foreach (string assemblyName in neededAssemblies)
                {
                    try
                    {
                        HttpResponseMessage dllResponse = await httpClient.GetAsync($"https://localhost:7025/_framework/{assemblyName}");
                                                    if (dllResponse.IsSuccessStatusCode)
                                                    {
                                                        System.Console.WriteLine($"Fetching assembly: {assemblyName}");
                                                        byte[] bytes = await dllResponse.Content.ReadAsByteArrayAsync();
                                                        MetadataReferences.Add(MetadataReference.CreateFromImage(bytes));
                                                    }
                        else
                        {
                            System.Console.WriteLine($"Did not get metadata ref for {assemblyName}");
                        }
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine($"Could not add metadata reference for {assemblyName}: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Error fetching metadata list: {e.Message} {e.InnerException?.Message} {e.StackTrace}");
            }
        }

        ProjectInfo projectInfo = ProjectInfo
            .Create(ProjectId.CreateNewId(), VersionStamp.Create(), "LlmTornado.Docs", "LlmTornado.Docs", LanguageNames.CSharp)
            .WithMetadataReferences(MetadataReferences)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.LatestMajor));

        Project? project = Workspace.AddProject(projectInfo);
        UseOnlyOnceDocument = Workspace.AddDocument(project.Id, "Code.cs", SourceText.From(string.Empty));
        DocumentId = UseOnlyOnceDocument.Id;
    }

    public AdhocWorkspace Workspace { get; set; }
    public Document UseOnlyOnceDocument { get; set; }
    public DocumentId DocumentId { get; set; }
}
