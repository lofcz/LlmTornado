using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Mcp;

public class MCPToolkits
{

    public static MCPServer PuppeteerToolkit(string[]? disableTools)
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
        return server;
    }

    public static MCPServer GithubToolkit(string githubApiKey, string[]? disableTools)
    {
        return new MCPServer("github", "https://api.githubcopilot.com/mcp", additionalConnectionHeaders: new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {githubApiKey}" }
        });
    }

    public static MCPServer LocalToolkit(string workspaceFolder, string[]? disableTools = null)
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
        return server;
    }

    public static MCPServer GmailToolkit(string[]? disableTools = null)
    {
        var server = new MCPServer("gmail", command: "npx", arguments: new[] {
            "@gongrzhe/server-gmail-autoauth-mcp"
        },
            disableTools: disableTools);
        return server;
    }

    public static MCPServer PlaywrightToolkit(string[]? disableTools = null)
    {
        var server = new MCPServer("gmail", command: "npx", arguments: new[] {
            "@playwright/mcp@latest"
        },
            disableTools: disableTools);
        return server;
    }

    public static MCPServer fetchToolkit(string[]? disableTools = null)
    {
        var server = new MCPServer("fetch", command: "uvx", arguments: new[] {
            "mcp-server-fetch"
        },
            disableTools: disableTools);
        return server;
    }
}
