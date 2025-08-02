using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorWorker.Core;
using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.WorkerBackgroundService;
using System.Text;
using System;
using System.Text.Json;
using LlmTornado.Docs.Arcade.Roslyn.PersistentStorage;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace LlmTornado.Docs.Code.Intellisense;

public class MonacoServiceWrapper {
    
    internal class Request
    {
        public string Code { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
    
    [JSInvokable]
#pragma warning disable CA1822
    public async Task<byte[]?> RunAsync(string name, JsonElement[] objectArgs)
#pragma warning restore CA1822
    {
        System.Console.WriteLine("Intellisense RunAsync:");
        System.Console.WriteLine(JsonConvert.SerializeObject(objectArgs));

        string code = objectArgs[0].GetProperty("code").GetString();
        string arg0 = JsonSerializer.Serialize(objectArgs[0]);

        try
        {
            if (Intellisense.UseWorker)
            {
                switch (name)
                {
                    case "GetCompletionAsync":
                        return await Intellisense.MonacoWorkerWrapper.RunAsync(a => a.GetCompletionAsync(code, arg0));
                    case "GetCompletionResolveAsync":
                        return await Intellisense.MonacoWorkerWrapper.RunAsync(a => a.GetCompletionResolveAsync(arg0));
                    case "GetSignatureHelpAsync":
                        return await Intellisense.MonacoWorkerWrapper.RunAsync(a => a.GetSignatureHelpAsync(code, arg0));
                    case "GetQuickInfoAsync":
                        return await Intellisense.MonacoWorkerWrapper.RunAsync(a => a.GetQuickInfoAsync(arg0));
                    case "GetDiagnosticsAsync":
                        return await Intellisense.MonacoWorkerWrapper.RunAsync(a => a.GetDiagnosticsAsync(code));
                }
            }
            else
            {
                switch (name)
                {
                    case "GetCompletionAsync":
                        return await Intellisense.MainThreadMonacoService.GetCompletionAsync(code, arg0);
                    case "GetCompletionResolveAsync":
                        return await Intellisense.MainThreadMonacoService.GetCompletionResolveAsync(arg0);
                    case "GetSignatureHelpAsync":
                        return await Intellisense.MainThreadMonacoService.GetSignatureHelpAsync(code, arg0);
                    case "GetQuickInfoAsync":
                        return await Intellisense.MainThreadMonacoService.GetQuickInfoAsync(arg0);
                    case "GetDiagnosticsAsync":
                        return await Intellisense.MainThreadMonacoService.GetDiagnosticsAsync(code);
                }
            }
        }
        catch
        {
            return Encoding.UTF8.GetBytes("{}");
        }
        
       return Encoding.UTF8.GetBytes("{}");
    }
}

public static class Intellisense
{
    public static bool UseWorker = true; // Toggle for debugging
    public static NavigationManager? NavigationManager {get;set;}
    public static IWorkerBackgroundService<MonacoService>? MonacoWorkerWrapper {get;set;}
    public static MonacoService? MainThreadMonacoService { get; set; }
    public static IWorker? Worker {get; set;}

    public static async Task Init(IJSRuntime JS, NavigationManager nm, IWorkerFactory wf, HttpClient httpClient)
    {
       NavigationManager = nm;

       if (UseWorker)
       {
           Worker = await wf.CreateAsync();
           System.Console.WriteLine("Creating worker");
           MonacoWorkerWrapper = await Worker.CreateBackgroundServiceAsync<MonacoService>();
           await MonacoWorkerWrapper.RunAsync(a => a.Init(nm.BaseUri));
       }
       else
       {
           System.Console.WriteLine("Creating main-thread service");
           MainThreadMonacoService = new MonacoService(httpClient);
           await MainThreadMonacoService.Init(nm.BaseUri);
       }
       
       DotNetObjectReference<MonacoServiceWrapper> wrapperRef = DotNetObjectReference.Create(new MonacoServiceWrapper());
       await JS.InvokeVoidAsync("initializeGlobalIntellisense", wrapperRef);
    }
}
