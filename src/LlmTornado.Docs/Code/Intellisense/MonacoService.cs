using Newtonsoft.Json;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Models;
using OmniSharp.Models.SignatureHelp;
using OmniSharp.Models.v1.Completion;
using OmniSharp.Options;
using System.Text;
using System.Text.Json;
using LlmTornado.Docs.Code.Intellisense;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace LlmTornado.Docs.Code.Intellisense;

public class MonacoService
{
    #region Fields

    RoslynProject _completionProject;
    RoslynProject _diagnosticProject;
    OmniSharpCompletionService _completionService;
    OmniSharpSignatureHelpService _signatureService;
    OmniSharpQuickInfoProvider _quickInfoProvider;
    private HttpClient _httpClient;
    private IAssemblyCache _cache;
    private IIntellisenseStatus _status;
    private IJSRuntime js;
    
    #endregion

    #region Records

    public record Diagnostic()
    {
        public LinePosition Start { get; init; }
        public LinePosition End { get; init; }
        public string Message { get; init; }
        public int Severity { get; init; }
    }

    #endregion

    #region Constructors

    // Primary DI ctor
    public MonacoService(HttpClient httpClient, IAssemblyCache cache, IIntellisenseStatus status, IJSRuntime js)
    {
        this.js = js;
        _httpClient = httpClient;
        _cache = cache;
        _status = status;
        DefaultCode =
$@"using System; 
    class Filter 
    {{               
        public Filter() 
        {{ 
            
        }}
    }} 
";
    }

    // Fallback parameterless ctor for worker scenarios without DI
    public MonacoService()
    {
        _httpClient = new HttpClient();
        _cache = new AssemblyCache();
        _status = new IntellisenseStatus();
        DefaultCode =
$@"using System; 
    class Filter 
    {{               
        public Filter() 
        {{ 
            
        }}
    }} 
";
    }

    #endregion

    #region Properties

    public string DefaultCode { get; init; }

    #endregion

    #region Methods

    internal record ResponsePayload(object? Payload, string? Type);
    
    public async Task Init(string uri, IJSRuntime? js)
    {
        this.js = js;
        
        // Ensure minimal fields exist even if constructed via parameterless ctor
        _cache ??= new AssemblyCache();
        _status ??= new IntellisenseStatus();
        _httpClient ??= new HttpClient();

        _httpClient.BaseAddress = new Uri(uri);
        
        System.Console.WriteLine("HELLO FROM INIT");
        System.Console.WriteLine("=================================");

        try
        {
            await js.InvokeVoidAsync("console.log", "=============WEBWORKER==========");
        }
        catch (Exception e)
        {
            System.Console.WriteLine($"{(js is null ? "JS IS NULL" : "JS IS NOT NULL")}");
            System.Console.WriteLine($"{e} {e.Message} {e.StackTrace} {e.InnerException?.Message}");
        }
        
        _status?.Publish(IntelliStage.SpinningWorker, 20, "Initializing assembly cache…");

        // Explicitly ensure assemblies are loaded BEFORE creating projects
        await _cache.InitializeAsync(_httpClient, _status);

        _status?.Publish(IntelliStage.SpinningWorker, 35, "Preparing workspace…");

        _completionProject = new RoslynProject(uri, _cache, _status);
        await _completionProject.Init(_httpClient, "LlmTornado.Docs.Completion");

        _diagnosticProject = new RoslynProject(uri, _cache, _status);
        await _diagnosticProject.Init(_httpClient, "LlmTornado.Docs.Diagnostics");

        ILoggerFactory loggerFactory = LoggerFactory.Create(configure => { });
        FormattingOptions formattingOptions = new FormattingOptions();

        _status?.Publish(IntelliStage.WiringChannels, 60, "Wiring language services…");

        _completionService = new OmniSharpCompletionService(_completionProject.Workspace, formattingOptions, loggerFactory);
        _signatureService = new OmniSharpSignatureHelpService(_completionProject.Workspace);
        _quickInfoProvider = new OmniSharpQuickInfoProvider(_completionProject.Workspace, formattingOptions, loggerFactory);

        _status?.MarkReady();
    }

    private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    
    public static string Serialize(object obj)
    {
        return  System.Text.Json.JsonSerializer.Serialize(obj, jsonOptions);
    }

    public async Task<byte[]> GetCompletionAsync(string code, string completionRequestString)
    {
        Solution updatedSolution;
        CompletionRequest? completionRequest = JsonConvert.DeserializeObject<CompletionRequest>(completionRequestString);
        do
        {
            updatedSolution = _completionProject.Workspace.CurrentSolution.WithDocumentText(_completionProject.DocumentId, SourceText.From(code));
        } while (!_completionProject.Workspace.TryApplyChanges(updatedSolution));

        Document? document = updatedSolution.GetDocument(_completionProject.DocumentId);
        CompletionResponse completionResponse = await _completionService.Handle(completionRequest, document);
        

        ResponsePayload p = new ResponsePayload(completionResponse, "GetCompletionAsync");
        string serialized = Serialize(p);
        
        return Encoding.UTF8.GetBytes(serialized);
    }

    public async Task<byte[]> GetCompletionResolveAsync(string completionResolveRequestString)
    {
        CompletionResolveRequest? completionResolveRequest = JsonConvert.DeserializeObject<CompletionResolveRequest>(completionResolveRequestString);
        Document? document = _completionProject.Workspace.CurrentSolution.GetDocument(_completionProject.DocumentId);
        CompletionResolveResponse completionResponse = await _completionService.Handle(completionResolveRequest, document);

        ResponsePayload p = new ResponsePayload(completionResponse, "GetCompletionResolveAsync");

        return Encoding.UTF8.GetBytes(Serialize(p));
    }

    public async Task<byte[]> GetSignatureHelpAsync(string code, string signatureHelpRequestString)
    {
        Solution updatedSolution;
        SignatureHelpRequest? signatureHelpRequest = JsonConvert.DeserializeObject<SignatureHelpRequest>(signatureHelpRequestString);
        do
        {
            updatedSolution = _completionProject.Workspace.CurrentSolution.WithDocumentText(_completionProject.DocumentId, SourceText.From(code));
        } while (!_completionProject.Workspace.TryApplyChanges(updatedSolution));

        Document? document = updatedSolution.GetDocument(_completionProject.DocumentId);
        SignatureHelpResponse signatureHelpResponse = await _signatureService.Handle(signatureHelpRequest, document);

        ResponsePayload p = new ResponsePayload(signatureHelpResponse, "GetSignatureHelpAsync");
        return Encoding.UTF8.GetBytes(Serialize(p));
    }

    public async Task<byte[]> GetQuickInfoAsync(string quickInfoRequestString)
    {
        QuickInfoRequest? quickInfoRequest = JsonConvert.DeserializeObject<QuickInfoRequest>(quickInfoRequestString);
        
        Document? document = _diagnosticProject.Workspace.CurrentSolution.GetDocument(_diagnosticProject.DocumentId);
        QuickInfoResponse quickInfoResponse = await _quickInfoProvider.Handle(quickInfoRequest, document);
        
        ResponsePayload p = new ResponsePayload(quickInfoResponse, "GetQuickInfoAsync");
        return Encoding.UTF8.GetBytes(Serialize(p));
    }

    public async Task<byte[]> GetDiagnosticsAsync(string code)
    {
        Solution updatedSolution;
        do
        {
            updatedSolution = _diagnosticProject.Workspace.CurrentSolution.WithDocumentText(_diagnosticProject.DocumentId, SourceText.From(code));
        } while (!_diagnosticProject.Workspace.TryApplyChanges(updatedSolution));
        Document? document = updatedSolution.GetDocument(_diagnosticProject.DocumentId);
        SyntaxTree? st = await document.GetSyntaxTreeAsync();

        CSharpCompilation compilation =
        CSharpCompilation
            .Create("Temp",
                [st],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: true,
                optimizationLevel: OptimizationLevel.Debug),
                references: _cache.MetadataReferences
            );

        using (MemoryStream temp = new MemoryStream())
        {
            EmitResult result = compilation.Emit(temp);
            SemanticModel semanticModel = compilation.GetSemanticModel(st, true);

            ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> dotnetDiagnostics = result.Diagnostics;

            List<Diagnostic> diagnostics = dotnetDiagnostics.Select(current =>
            {
                FileLinePositionSpan lineSpan = current.Location.GetLineSpan();

                return new Diagnostic()
                {
                    Start = lineSpan.StartLinePosition,
                    End = lineSpan.EndLinePosition,
                    Message = current.GetMessage(),
                    Severity = this.GetSeverity(current.Severity)
                };
            }).ToList();
            ResponsePayload p = new ResponsePayload(diagnostics, "GetDiagnosticsAsync");
            return Encoding.UTF8.GetBytes(Serialize(p));
        }
    }

    private int GetSeverity(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Hidden => 1,
            DiagnosticSeverity.Info => 2,
            DiagnosticSeverity.Warning => 4,
            DiagnosticSeverity.Error => 8,
            _ => throw new Exception("Unknown diagnostic severity.")
        };
    }

    #endregion
}
