using System.Diagnostics;
using System.Reflection;
using LlmTornado.Docs.Console;
using LlmTornado.Docs.Webcil;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LlmTornado.Docs.Code;

public class WebAssemblyCodeRunner : ICodeExecutor
{
    private readonly ConsoleOutputService _consoleOutputService;
    private readonly IResourceResolver _resourceResolver;
    private readonly IWebAssemblyHostEnvironment _env;
    private readonly HttpClient _httpClient;

    private static string? Meta;
    private static Dictionary<string, string> MetaDict = [];

    // force System.Threading.Tasks to be emitted in the release build
    [UsedImplicitly] 
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Task))]
    private static readonly Task task = Task.CompletedTask;

    [UsedImplicitly]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TaskCompletionSource))]
    private static readonly TaskCompletionSource taskSource = new TaskCompletionSource();
    
    [UsedImplicitly]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TaskCompletionSource<>))]
    private static readonly TaskCompletionSource<object?> taskSourceGeneric = new TaskCompletionSource<object?>();

    [UsedImplicitly]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AsyncTaskMethodBuilder))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AsyncTaskMethodBuilder<>))]
    private static readonly object refObject = new object();

    private bool IsLocalhost => _env.BaseAddress.Contains("localhost");
    
    public WebAssemblyCodeRunner(ConsoleOutputService consoleOutputService, IResourceResolver resourceResolver, IWebAssemblyHostEnvironment env, HttpClient httpClient)
    {
        _consoleOutputService = consoleOutputService;
        _resourceResolver = resourceResolver;
        _env = env;
        _httpClient = httpClient;
    }

    public async Task ExecuteAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
            _consoleOutputService.AddLog("Parsed syntax tree.", ConsoleSeverity.Debug);
            _consoleOutputService.AddLog($"Env base address: {_env.BaseAddress}", ConsoleSeverity.Debug);
            _consoleOutputService.AddLog($"Env environment: {_env.Environment}", ConsoleSeverity.Debug);
            _consoleOutputService.AddLog($"Resolved IsLocalhost: {(IsLocalhost ? "YES" : "NO")}", ConsoleSeverity.Debug);

            if (Meta is null && !IsLocalhost)
            {
                try
                {
                    await using Stream stream = await _httpClient.GetStreamAsync("/playground/_framework/bmeta.json", cancellationToken); 
                    using StreamReader reader = new StreamReader(stream);
                    Meta = await reader.ReadToEndAsync(cancellationToken);
                    MetaDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Meta) ?? MetaDict;
                    _consoleOutputService.AddLog($"bmeta.json read correctly: {MetaDict.Count} entries found.", ConsoleSeverity.Debug);
                }
                catch (Exception e)
                {
                    _consoleOutputService.AddLog($"bmeta.json file not read: {e.Message}", ConsoleSeverity.Error);
                }
                
            }
            
            List<MetadataReference?> references =
            [
                await GetMetadataReferenceAsync("System.wasm"),
                await GetMetadataReferenceAsync("System.Private.Uri.wasm"),
                await GetMetadataReferenceAsync("System.Private.CoreLib.wasm"),
                await GetMetadataReferenceAsync("System.Runtime.wasm"),
                await GetMetadataReferenceAsync("System.Console.wasm"),
                await GetMetadataReferenceAsync("System.Collections.wasm"),
                await GetMetadataReferenceAsync("System.Runtime.CompilerServices.wasm"),
                await GetMetadataReferenceAsync("System.Threading.Tasks.wasm"),
                await GetMetadataReferenceAsync("System.Net.Http.wasm"),
                await GetMetadataReferenceAsync("System.Text.Json.wasm"),
                await GetMetadataReferenceAsync("LlmTornado.wasm")
            ];
            
            int zz = 1;

            if (references.Any(x => x is null))
            {
                _consoleOutputService.AddLog("Aborted, because some requested references were not resolved.", ConsoleSeverity.Error);
                return;
            }

            stopwatch.Restart();
            CSharpCompilation compilation = CSharpCompilation.Create(
                "InMemoryAssembly",
                [syntaxTree],
                references.OfType<MetadataReference>(),
                new CSharpCompilationOptions(
                    OutputKind.ConsoleApplication,
                    metadataImportOptions: MetadataImportOptions.All,
                    allowUnsafe: true,
                    reportSuppressedDiagnostics: true
                )
            );
            _consoleOutputService.AddLog($"Compilation completed in {stopwatch.ElapsedMilliseconds} ms.", ConsoleSeverity.Debug);

            foreach (Diagnostic diagnostic in compilation.GetDiagnostics(cancellationToken))
            {
                ConsoleSeverity severity = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Error => ConsoleSeverity.Error,
                    DiagnosticSeverity.Warning => ConsoleSeverity.Warning,
                    _ => ConsoleSeverity.Info
                };
                _consoleOutputService.AddLog(diagnostic.GetMessage(), severity);
            }

            stopwatch.Restart();
            using MemoryStream memoryStream = new MemoryStream();
            EmitResult emitResult = compilation.Emit(memoryStream, cancellationToken: cancellationToken);

            _consoleOutputService.AddLog($"Emit completed in {stopwatch.ElapsedMilliseconds} ms.", ConsoleSeverity.Debug);

            if (!emitResult.Success)
            {
                foreach (Diagnostic diagnostic in emitResult.Diagnostics)
                {
                    ConsoleSeverity severity = diagnostic.Severity switch
                    {
                        DiagnosticSeverity.Error => ConsoleSeverity.Error,
                        DiagnosticSeverity.Warning => ConsoleSeverity.Warning,
                        _ => ConsoleSeverity.Info
                    };
                    _consoleOutputService.AddLog(diagnostic.GetMessage(), severity);
                }

                return;
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            Assembly assembly = Assembly.Load(memoryStream.ToArray());
            _consoleOutputService.AddLog($"Assembly loaded: {assembly.FullName}", ConsoleSeverity.Debug);

            MethodInfo? entryPoint = assembly.EntryPoint;
            if (entryPoint == null)
            {
                _consoleOutputService.AddLog("No entry point found in the assembly.", ConsoleSeverity.Error);
                return;
            }

            try
            {
                stopwatch.Restart();
                ParameterInfo[] parameters = entryPoint.GetParameters();
                object?[]? invokeArgs = parameters.Length == 1 && parameters[0].ParameterType == typeof(string[])
                    ? [Array.Empty<string>()]
                    : null;
                
                Type? bridgeType = assembly.GetType("__ExecutionBridge");
                FieldInfo? tcsField = bridgeType.GetField("CompletionSource", BindingFlags.Public | BindingFlags.Static);
                
                
                
                object? result = entryPoint.Invoke(null, invokeArgs);
                
                if (result is Task resultTask)
                {
                    await resultTask;
                }
                
                object? tcsObject = tcsField.GetValue(null);
                PropertyInfo? taskProperty = tcsObject.GetType().GetProperty("Task");
                object? taskObject = taskProperty?.GetValue(tcsObject);
         
                if (taskObject is Task resultTask2)
                {
                    await resultTask2;
                }
                
                _consoleOutputService.AddLog($"Execution completed in {stopwatch.ElapsedMilliseconds} ms.", ConsoleSeverity.Debug);
            }
            catch (Exception ex)
            {
                string exceptionMessage = ex.InnerException != null
                    ? $"Unhandled Exception: {ex.InnerException.Message}"
                    : $"Unhandled Exception: {ex.Message}";
                _consoleOutputService.AddLog(exceptionMessage, ConsoleSeverity.Error);
            }
        }
        catch (Exception e)
        {
            int z = 0;
        }
    }
    
    private async Task<string> ResolveResourceStreamUri(string resource)
    {
        _consoleOutputService.AddLog($"Reading: {resource}", ConsoleSeverity.Info);
        
        // on localhost, we can request the resource directly
        if (IsLocalhost)
        {
            _consoleOutputService.AddLog($"Read from: /_framework/{resource}", ConsoleSeverity.Info);
            return $"/_framework/{resource}";
        }
        
        // on prod, we need to transform the request into hashed version, e.g.
        // System.wasm -> System.82w3kc2qw3.wasm
        string resolvedName = MetaDict.GetValueOrDefault(resource, $"[Entry: {resource} not found!]");
        _consoleOutputService.AddLog($"Read from: /playground/_framework/{resolvedName}", ConsoleSeverity.Info);
        return $"/playground/_framework/{resolvedName}";
    }

    private async Task<PortableExecutableReference?> GetMetadataReferenceAsync(string wasmModule)
    {
        try
        {
            await using Stream stream = await _httpClient.GetStreamAsync(await ResolveResourceStreamUri(wasmModule));
            using MemoryStream ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
        
            byte[] peBytes = WebcilConverterUtil.ConvertFromWebcil(ms);

            using MemoryStream peStream = new MemoryStream(peBytes);
            return MetadataReference.CreateFromStream(peStream);
        }
        catch (Exception e)
        {
            _consoleOutputService.AddLog($"GetMetadataReferenceAsync: {e.Message}", ConsoleSeverity.Error);
            return null;
        }
    }
}

public interface ICodeExecutor
{
    public Task ExecuteAsync(string code, CancellationToken cancellationToken = default);
}
