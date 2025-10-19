using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Common;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Tool = LlmTornado.Common.Tool;

namespace LlmTornado.Mcp;

public static class McpExtensions
{
    public static AnthropicMcpServer ToAnthropicMcpServerAsync(this MCPServer client)
    {
        return new AnthropicMcpServer()
        {
            Name = client.ServerLabel,
            Url = client.ServerUrl,
            AuthorizationToken = client.AdditionalConnectionHeaders?.FirstOrDefault(key => key.Key.ToLower().Contains("auth")).Value ?? string.Empty,
            Configuration = new AnthropicMcpConfiguration()
            {
                Enabled = true,
                AllowedTools = client.AllowedTools ?? []
            }
        };
    }

    public static async ValueTask<List<Tool>> ListTornadoToolsAsync(this McpClient client)
    {
        IList<McpClientTool> tools = await client.ListToolsAsync();
        List<Tool> converted = tools.Select(x => x.ToTornadoTool()).ToList();
        return converted;
    }

    public static Tool ToTornadoTool(this McpClientTool tool)
    {
        Tool tornadoTool = new Tool(new ToolFunction(tool.Name, tool.Description, tool.JsonSchema))
        {
            RemoteTool = new McpTool
            {
                CallAsync = async (args, progress, serializer, fillContent, ct) =>
                {
                    CallToolResult callToolResult = await tool.CallAsync(args, progress is null
                        ? null
                        : new Progress<ProgressNotificationValue>(x =>
                        {
                            progress.Report(new ToolCallProgress
                            {
                                Message = x.Message,
                                Progress = x.Progress,
                                Total = x.Total
                            });
                        }), serializer, ct ?? CancellationToken.None);

                    string? result = null;
                    
                    if (fillContent && callToolResult.Content.Count > 0)
                    {
                        ContentBlock firstBlock = callToolResult.Content[0];

                        result = firstBlock switch
                        {
                            TextContentBlock textBlock => textBlock.Text,
                            ImageContentBlock imageBlock => imageBlock.Data,
                            AudioContentBlock audioBlock => audioBlock.Data,
                            EmbeddedResourceBlock embeddedResourceBlock => embeddedResourceBlock.Resource.Uri,
                            ResourceLinkBlock resourceLinkBlock => resourceLinkBlock.Uri,
                            _ => result
                        };
                    }

                    McpContent mcpContent = new McpContent
                    {
                        IsError = callToolResult.IsError ?? false,
                        StructuredContent = callToolResult.StructuredContent,
                        McpContentBlocks = callToolResult.Content.Select(x =>
                        {
                            IMcpContentBlock? blockToAdd = null;
                            
                            switch (x)
                            {
                                case TextContentBlock textBlock:
                                {
                                    blockToAdd = new McpContentBlockText
                                    {
                                        Annotations = x.Annotations?.ToMcpAnnotations(),
                                        Text = textBlock.Text,
                                        Type = x.Type,
                                        NativeBlock = x.ToNativeBlock()
                                    };

                                    break;
                                }
                                case ImageContentBlock imageBlock:
                                {
                                    blockToAdd = new McpContentBlockImage
                                    {
                                        Annotations = x.Annotations?.ToMcpAnnotations(),
                                        Data = imageBlock.Data,
                                        MimeType = imageBlock.MimeType,
                                        Type = x.Type,
                                        NativeBlock = x.ToNativeBlock()
                                    };
                                    
                                    break;
                                }
                                case AudioContentBlock audioBlock:
                                {
                                    blockToAdd = new McpContentBlockAudio
                                    {
                                        Annotations = x.Annotations?.ToMcpAnnotations(),
                                        Data = audioBlock.Data,
                                        MimeType = audioBlock.MimeType,
                                        Type = x.Type,
                                        NativeBlock = x.ToNativeBlock()
                                    };
                                    
                                    break;
                                }
                                case EmbeddedResourceBlock embeddedResourceBlock:
                                {
                                    McpContentBlockEmbeddedResourceContents content = embeddedResourceBlock.Resource switch
                                    {
                                        TextResourceContents textResource => new McpContentBlockEmbeddedResourceContentsText { Text = textResource.Text },
                                        BlobResourceContents blobResource => new McpContentBlockEmbeddedResourceContentsBlob { Blob = blobResource.Blob },
                                        _ => new McpContentBlockEmbeddedResourceContentsUnknown { Native = x }
                                    };

                                    blockToAdd = new McpContentBlockEmbeddedResource
                                    {
                                        Annotations = x.Annotations?.ToMcpAnnotations(),
                                        Resource = content,
                                        Type = x.Type,
                                        NativeBlock = x.ToNativeBlock()
                                    };
                                    
                                    break;
                                }
                                case ResourceLinkBlock resourceLinkBlock:
                                {
                                    blockToAdd = new McpContentBlockLinkResource
                                    {
                                        Annotations = x.Annotations?.ToMcpAnnotations(),
                                        Name = resourceLinkBlock.Name,
                                        Uri = resourceLinkBlock.Uri,
                                        MimeType = resourceLinkBlock.MimeType,
                                        Description = resourceLinkBlock.Description,
                                        Size = resourceLinkBlock.Size,
                                        Type = x.Type,
                                        NativeBlock = x.ToNativeBlock()
                                    };
                                    
                                    break;
                                }
                            }

                            return blockToAdd ?? new McpContentBlockUnknown
                            {
                                NativeBlock = x.ToNativeBlock(),
                                Annotations = x.Annotations?.ToMcpAnnotations(),
                                Type = x.Type
                            };
                        }).ToList()
                    };
                    
                    FunctionResult fr = new FunctionResult
                    {
                        Content = result ?? string.Empty,
                        RemoteContent = mcpContent,
                        InvocationSucceeded = !callToolResult.IsError,
                        ContentJsonType = typeof(string),
                        Name = tool.Name
                    };

                    return fr;
                }
            }
        };

        return tornadoTool;
    }
}