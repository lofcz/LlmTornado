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

public interface IAssemblyCache
{
    Task InitializeAsync(HttpClient httpClient, IIntellisenseStatus? status = null);
    MefHostServices HostServices { get; }
    IReadOnlyList<MetadataReference> MetadataReferences { get; }
    bool IsReady { get; }
}

public class AssemblyCache : IAssemblyCache
{
    private readonly object _lock = new();
    private Task? _initTask;
    private bool _ready;
    private MefHostServices? _hostServices;
    private readonly List<MetadataReference> _metadata = new();

    public MefHostServices HostServices => _hostServices ?? throw new InvalidOperationException("AssemblyCache not initialized");
    public IReadOnlyList<MetadataReference> MetadataReferences => _metadata;
    public bool IsReady => _ready;

    public Task InitializeAsync(HttpClient httpClient, IIntellisenseStatus? status = null)
    {
        if (_ready) return Task.CompletedTask;
        lock (_lock)
        {
            if (_ready) return Task.CompletedTask;
            if (_initTask != null) return _initTask;
            _initTask = DoInitAsync(httpClient, status);
            return _initTask;
        }
    }

    private async Task DoInitAsync(HttpClient httpClient, IIntellisenseStatus? status)
    {
        status?.Publish(IntelliStage.LoadingAssemblies, 10, "Loading host services…");

        ImmutableArray<Assembly> defaultAssemblies = MefHostServices.DefaultAssemblies;
        IEnumerable<Assembly> assembliesToLoad = defaultAssemblies.Concat(new[] { typeof(Arcade.ArcadeAssembly).Assembly });
        _hostServices = MefHostServices.Create(assembliesToLoad);

        if (_metadata.Count == 0)
        {
            string[] neededAssemblies = new[]
            {
                "System.Runtime.dll","System.Collections.dll","netstandard.dll","System.dll","System.Console.dll","System.Private.CoreLib.dll",
                "System.ComponentModel.Primitives.dll","System.Linq.dll","System.Data.Common.dll","System.Private.Xml.dll","System.ObjectModel.dll",
                "System.Linq.Expressions.dll","Microsoft.CodeAnalysis.dll","Microsoft.CodeAnalysis.CSharp.dll","Microsoft.CodeAnalysis.Workspaces.dll",
                "Microsoft.CodeAnalysis.CSharp.Workspaces.dll","System.Reflection.Metadata.dll","System.Collections.Immutable.dll","System.Memory.dll"
            };

            int done = 0;
            foreach (string assemblyName in neededAssemblies)
            {
                try
                {
                    HttpResponseMessage dllResponse = await httpClient.GetAsync($"https://localhost:7025/_framework/{assemblyName}");
                   
                    if (dllResponse.IsSuccessStatusCode)
                    {
                        byte[] bytes = await dllResponse.Content.ReadAsByteArrayAsync();
                        _metadata.Add(MetadataReference.CreateFromImage(bytes));
                        done++;
                        int pct = 10 + (int)(done * 70.0 / neededAssemblies.Length);
                        status?.Publish(IntelliStage.LoadingAssemblies, pct, $"Loaded {assemblyName} ({done}/{neededAssemblies.Length})");
                        System.Console.WriteLine($"Fetching assembly: {assemblyName}");
                    }
                    else
                    {
                        status?.Publish(IntelliStage.LoadingAssemblies, 10, $"Missing metadata: {assemblyName}");
                    }
                }
                catch (Exception e)
                {
                    status?.Publish(IntelliStage.LoadingAssemblies, 10, $"Error {assemblyName}: {e.Message}");
                }
            }
        }

        _ready = true;
    }
}

public class RoslynProject
{
    private readonly string _uri;
    private readonly IAssemblyCache _cache;
    private readonly IIntellisenseStatus? _status;

    public RoslynProject(string uri, IAssemblyCache cache, IIntellisenseStatus? status = null)
    {
        _uri = uri;
        _cache = cache;
        _status = status;
    }

    public async Task Init(HttpClient httpClient, string projectName)
    {
        await _cache.InitializeAsync(httpClient, _status);

        _status?.Publish(IntelliStage.WarmupProject, 85, $"Creating project {projectName}…");

        Workspace = new AdhocWorkspace(_cache.HostServices);

        ProjectInfo projectInfo = ProjectInfo
            .Create(ProjectId.CreateNewId(), VersionStamp.Create(), projectName, projectName, LanguageNames.CSharp)
            .WithMetadataReferences(_cache.MetadataReferences)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.LatestMajor));

        Project? project = Workspace.AddProject(projectInfo);
        UseOnlyOnceDocument = Workspace.AddDocument(project.Id, "Code.cs", SourceText.From(string.Empty));
        DocumentId = UseOnlyOnceDocument.Id;
    }

    public AdhocWorkspace Workspace { get; private set; }
    public Document UseOnlyOnceDocument { get; private set; }
    public DocumentId DocumentId { get; private set; }
}
