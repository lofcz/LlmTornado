using BlazorWorker.Core;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LlmTornado.Docs.Code;
using LlmTornado.Docs.Code.Intellisense;
using LlmTornado.Docs.Console;

namespace LlmTornado.Docs;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        builder.Services.AddSingleton<ICodeExecutor, WebAssemblyCodeRunner>();
        builder.Services.AddSingleton<ConsoleOutputService>();
        builder.Services.AddSingleton<IResourceResolver, ResourceResolver>();
        builder.Services.AddWorkerFactory();
        builder.Services.AddSingleton<IIntellisenseStatus, IntellisenseStatus>();
        builder.Services.AddSingleton<IAssemblyCache, AssemblyCache>();

        await builder.Build().RunAsync();
    }
}
