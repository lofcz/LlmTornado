using System.Diagnostics;
using System.Reflection;
using LlmTornado.Docs.Console;
using LlmTornado.Docs.Webcil;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LlmTornado.Docs.Code;

public class WebAssemblyCodeRunner : ICodeExecutor
{
    private readonly ConsoleOutputService _consoleOutputService;
    private readonly IResourceResolver _resourceResolver;
    private readonly IWebAssemblyHostEnvironment _env;
    private readonly HttpClient _httpClient;

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
            var stopwatch = Stopwatch.StartNew();

            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            _consoleOutputService.AddLog("Parsed syntax tree.", ConsoleSeverity.Debug);

            var references = new List<MetadataReference>
            {
                await GetMetadataReferenceAsync("System.wasm"),
                await GetMetadataReferenceAsync("System.Private.Uri.wasm"),
                await GetMetadataReferenceAsync("System.Private.CoreLib.wasm"),
                await GetMetadataReferenceAsync("System.Runtime.wasm"),
                await GetMetadataReferenceAsync("System.Console.wasm"),
                await GetMetadataReferenceAsync("System.Collections.wasm"),
                await GetMetadataReferenceAsync("System.Threading.Tasks.wasm"),
                await GetMetadataReferenceAsync("System.Net.Http.wasm"),
                await GetMetadataReferenceAsync("System.Text.Json.wasm"),
                await GetMetadataReferenceAsync("LlmTornado.wasm")
            };
            
            /*
             * await GetMetadataReferenceAsync("System.Runtime.wasm"),
                await GetMetadataReferenceAsync("System.Console.wasm"),
                await GetMetadataReferenceAsync("System.Collections.wasm"),
                await GetMetadataReferenceAsync("System.Net.Http.wasm"),
                await GetMetadataReferenceAsync("System.Text.Json.wasm"),
                await GetMetadataReferenceAsync("LlmTornado.wasm")
             */

            int zz = 1;

            stopwatch.Restart();
            var compilation = CSharpCompilation.Create(
                "InMemoryAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(
                    OutputKind.ConsoleApplication,
                    metadataImportOptions: MetadataImportOptions.All,
                    allowUnsafe: true,
                    reportSuppressedDiagnostics: true
                )
            );
            _consoleOutputService.AddLog($"Compilation completed in {stopwatch.ElapsedMilliseconds} ms.", ConsoleSeverity.Debug);

            foreach (var diagnostic in compilation.GetDiagnostics())
            {
                var severity = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Error => ConsoleSeverity.Error,
                    DiagnosticSeverity.Warning => ConsoleSeverity.Warning,
                    _ => ConsoleSeverity.Info
                };
                _consoleOutputService.AddLog(diagnostic.GetMessage(), severity);
            }

            stopwatch.Restart();
            using var memoryStream = new MemoryStream();
            var emitResult = compilation.Emit(memoryStream);

            _consoleOutputService.AddLog($"Emit completed in {stopwatch.ElapsedMilliseconds} ms.", ConsoleSeverity.Debug);

            if (!emitResult.Success)
            {
                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    var severity = diagnostic.Severity switch
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
            var assembly = Assembly.Load(memoryStream.ToArray());
            _consoleOutputService.AddLog($"Assembly loaded: {assembly.FullName}", ConsoleSeverity.Debug);

            var entryPoint = assembly.EntryPoint;
            if (entryPoint == null)
            {
                _consoleOutputService.AddLog("No entry point found in the assembly.", ConsoleSeverity.Error);
                return;
            }

            try
            {
                stopwatch.Restart();
                var parameters = entryPoint.GetParameters();
                var invokeArgs = parameters.Length == 1 && parameters[0].ParameterType == typeof(string[])
                    ? new object?[] { Array.Empty<string>() }
                    : null;
                
                var bridgeType = assembly.GetType("__ExecutionBridge");
                var tcsField = bridgeType.GetField("CompletionSource", BindingFlags.Public | BindingFlags.Static);
                
                
                
                object? result = entryPoint.Invoke(null, invokeArgs);
                
                if (result is Task resultTask)
                {
                    await resultTask;
                }
                
                object? tcsObject = tcsField.GetValue(null);
                var taskProperty = tcsObject.GetType().GetProperty("Task");
                object? taskObject = taskProperty?.GetValue(tcsObject);
         
                if (taskObject is Task resultTask2)
                {
                    await resultTask2;
                }
                
                _consoleOutputService.AddLog($"Execution completed in {stopwatch.ElapsedMilliseconds} ms.", ConsoleSeverity.Debug);
            }
            catch (Exception ex)
            {
                var exceptionMessage = ex.InnerException != null
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
        //var resolved = await _resourceResolver.ResolveResource(resource);
        return $"/_framework/{resource}";
    }

    private async Task<PortableExecutableReference> GetMetadataReferenceAsync(string wasmModule)
    {
        await using var stream = await _httpClient.GetStreamAsync(await ResolveResourceStreamUri(wasmModule));
        using MemoryStream ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Seek(0, SeekOrigin.Begin);
        
        var peBytes = WebcilConverterUtil.ConvertFromWebcil(ms);

        using var peStream = new MemoryStream(peBytes);
        return MetadataReference.CreateFromStream(peStream);
    }
}

public interface ICodeExecutor
{
    public Task ExecuteAsync(string code, CancellationToken cancellationToken = default);
}