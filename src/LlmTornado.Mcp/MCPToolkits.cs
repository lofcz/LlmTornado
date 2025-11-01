using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Mcp;

public class MCPToolkits
{

    public static MCPServer PuppeteerToolkit(string[]? allowedTools = null)
    {
        var server = new MCPServer("puppeteer", command: "docker", arguments: new[] {
            "run",
            "-i",
            "--rm",
            "--init",
            "-e",
            "DOCKER_CONTAINER=true",
            "mcp/puppeteer" },
            allowedTools: allowedTools);
        return server;
    }

    public static MCPServer GithubToolkit(string githubApiKey, string[]? allowedTools = null)
    {
        return new MCPServer("github", "https://api.githubcopilot.com/mcp", additionalConnectionHeaders: new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {githubApiKey}" }
        },
        allowedTools:allowedTools);
    }
    
    /// <summary>
    /// Meme generator using MCP Server Docker image
    /// </summary>
    /// <returns></returns>
    public static MCPServer MemeToolkit(string[]? allowedTools = null)
    {
        var server = new MCPServer("memegen", command: "docker", arguments: new[] {
                "run",
                "-i",
                "--rm",
                "-p",
                "5000:5000",
                "lofcz1/memegen-mcp"
            },
            allowedTools: allowedTools);
        return server;
    }

    /// <summary>
    /// File System Toolkit using MCP Server Docker image
    /// </summary>
    /// <param name="workspaceFolder"></param>
    /// <param name="disableTools"></param>
    /// <returns></returns>
    public static MCPServer FileSystemToolkit(string workspaceFolder, string[]? allowedTools = null)
    {
        var server = new MCPServer("filesystem", command: "docker", arguments: new[] {
            "run",
            "-i",
            "--rm",
            "--mount", $"type=bind,src={workspaceFolder},dst=/projects/workspace",
            "mcp/filesystem",
            "/projects/workspace"
        },
            allowedTools: allowedTools);
        return server;
    }

    public static MCPServer GmailToolkit(string[]? allowedTools = null)
    {
        var server = new MCPServer("gmail", command: "npx", arguments: new[] {
            "@gongrzhe/server-gmail-autoauth-mcp"
        },
            allowedTools: allowedTools);
        return server;
    }

    /// <summary>
    /// Playwright for web interactions
    /// </summary>
    /// <param name="disableTools"></param>
    /// <returns></returns>
    public static MCPServer PlaywrightToolkit(string[]? allowedTools = null)
    {
        var server = new MCPServer("playwright", command: "npx", arguments: new[] {
            "@playwright/mcp@latest"
        },
            allowedTools: allowedTools);
        return server;
    }

    public static MCPServer FetchToolkit(string[]? allowedTools = null)
    {
        var server = new MCPServer("fetch", command: "uvx", arguments: new[] {
            "mcp-server-fetch"
        },
            allowedTools: allowedTools);
        return server;
    }
}
