using BlazorWorker.Demo.SharedPages.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorWorker.Demo.SharedPages.Shared
{
    public class NavMenuLinksModel
    {
        static string BlazorWorkerVersion { get; } =
        $"v{typeof(BlazorWorker.BackgroundServiceFactory.WorkerBackgroundServiceExtensions).Assembly.GetName().Version}";

        static string BlazorCoreWorkerVersion { get; } =
          $"v{typeof(BlazorWorker.WorkerCore.IWorkerMessageService).Assembly.GetName().Version}";

        public static IEnumerable<NavMenuLinkInfo> NavMenuLinks { get; } = new List<NavMenuLinkInfo>()
        {
            {
                new() { Icon = "cog", Href="", Text = "Simple Worker", Match= NavLinkMatch.All } 
            },
            {
                new() { Icon = "copywriting", Href="BackgroundServiceMulti", Text = "Multiple Workers" }
            },
            {
                new() { Icon = "command", Href="CoreExample", Text = "Core Example" }
            },
            {
                new() { Icon = "globe", Href="Http", Text = "HttpClient Example" }
            },
            {
                new() { Icon = "transfer", Href="IoCExample", Text = "IoC / DI Example" }
            },
            {
                new() { Icon = "document", Href="IndexedDb", Text = "IndexedDB" }
            },
            {
                new() { Icon = "document", Href="ComplexSerialization", Text = "ComplexSerialization" }
            },
            {
                new() { Icon = "document", Href="JsDirect", Text = "JsDirect" }
            },
            {
                new() { Icon = "document", Href="JsInteractions", Text = "JsInteractions" }
            },
            {
                new() { Icon = "fork", Href="https://github.com/tewr/BlazorWorker", Text = "To the source!" }
            },
            {
                new() { Icon = "info", Href="https://www.nuget.org/packages/Tewr.BlazorWorker.BackgroundService", Small=true, Text = $"BackgroundService {BlazorWorkerVersion}" }
            },
            {
                new() { Icon = "info", Href="https://www.nuget.org/packages/Tewr.BlazorWorker.Core", Small=true, Text = $"Core {BlazorCoreWorkerVersion}" }
            }
        };
    }

    public class NavMenuLinkInfo
    {
        
        public string Icon { get; set; }

        
        public string Href { get; set; }

        public bool Small { get; set; }

        
        public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;
        public string Text { get; internal set; }
    }
}
