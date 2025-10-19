using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Mcp;

public class MCPToolkits
{

    public static MCPServer PuppeteerToolkit(string[]? disableTools = null)
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

    public static MCPServer GithubToolkit(string githubApiKey, string[]? disableTools = null)
    {
        return new MCPServer("github", "https://api.githubcopilot.com/mcp", additionalConnectionHeaders: new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {githubApiKey}" }
        },
        disableTools:disableTools);
    }

    /// <summary>
    /// File System Toolkit using MCP Server Docker image
    /// </summary>
    /// <param name="workspaceFolder"></param>
    /// <param name="disableTools"></param>
    /// <returns></returns>
    public static MCPServer FileSystemToolkit(string workspaceFolder, string[]? disableTools = null)
    {
        var server = new MCPServer("filesystem", command: "docker", arguments: new[] {
            "run",
            "-i",
            "--rm",
            "--mount", $"type=bind,src={workspaceFolder},dst=/projects/workspace",
            "mcp/filesystem",
            "/projects/workspace"
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

    /// <summary>
    /// Playwright for web interactions
    /// </summary>
    /// <param name="disableTools"></param>
    /// <returns></returns>
    public static MCPServer PlaywrightToolkit(string[]? disableTools = null)
    {
        var server = new MCPServer("playwright", command: "npx", arguments: new[] {
            "@playwright/mcp@latest"
        },
            disableTools: disableTools);
        return server;
    }

    public static MCPServer FetchToolkit(string[]? disableTools = null)
    {
        var server = new MCPServer("fetch", command: "uvx", arguments: new[] {
            "mcp-server-fetch"
        },
            disableTools: disableTools);
        return server;
    }
}
