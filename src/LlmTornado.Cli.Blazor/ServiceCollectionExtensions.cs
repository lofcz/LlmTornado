using Microsoft.Extensions.DependencyInjection;
using LlmTornado.Cli.Blazor.Controllers;

namespace LlmTornado.Cli.Blazor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChatRuntime(
        this IServiceCollection services, 
        Action<ChatRuntimeControllerOptions>? configure = null)
    {
        services.AddScoped<IChatUiController>(sp =>
        {
            var options = new ChatRuntimeControllerOptions();
            configure?.Invoke(options);
            return new ChatRuntimeController(options);
        });

        return services;
    }
}
