using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Mcp;

public class MCPToolkits
{
    public static readonly string[] SupportedToolkits = new string[]
    {
        "python_repl",
        "requests",
        "serpapi",
        "wikipedia",
        "llm-math",
        "open-meteo",
        "news-api",
        "wolfram-alpha",
        "weatherapi-com",
        "tmdbv3",
        "google-search",
        "zillow",
        "yelp-fusion",
        "air-quality",
        "currency-exchange",
        "flight-info",
        "crypto-prices",
        "stock-prices",
        "real-time-traffic",
        "event-discovery",
        "restaurant-reservations"
    };

    public static async Task<MCPServer> PuppeteerToolkit(string[]? disableTools)
    {
        var server = new MCPServer("puppeteer", command: "docker", arguments: new[] {
            "run",
            "-i",
            "--rm",
            "--init",
            "-e",
            "DOCKER_CONTAINER=true",
            "mcp/puppeteer" },
            disableTools: disableTools);
        await server.InitializeAsync();
        return server;
    }

    public static MCPServer GithubToolkit(string githubApiKey, string[]? disableTools)
    {
        return new MCPServer("github", "https://api.githubcopilot.com/mcp", additionalConnectionHeaders: new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {githubApiKey}" }
        });
    }

    public static async Task<MCPServer> LocalToolkit(string workspaceFolder, string[]? disableTools = null)
    {
        var server = new MCPServer("filesystem", command: "docker", arguments: new[] {
            "run",
            "-i",
            "--rm",
            "--mount", $"type=bind,src={workspaceFolder},dst=/projects/workspace",
            "mcp/filesystem",
            "/projects"
        },
            disableTools: disableTools);
        await server.InitializeAsync();
        return server;
    }

    public static async Task<MCPServer> GmailToolkit(string[]? disableTools = null)
    {
        var server = new MCPServer("gmail", command: "npx", arguments: new[] {
            "@gongrzhe/server-gmail-autoauth-mcp",
            "auth"
        },
            disableTools: disableTools);
        await server.InitializeAsync();
        return server;
    }


}
