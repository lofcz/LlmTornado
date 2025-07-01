using LlmTornado.Code;
using LlmTornado.Common;
using ModelContextProtocol.Protocol;

namespace LlmTornado.Mcp;

internal static class Extensions
{
    public static McpAnnotations ToMcpAnnotations(this Annotations annotations)
    {
        return new McpAnnotations
        {
            Priority = annotations.Priority,
            Audience = annotations.Audience?.Select(x => x is Role.User ? ChatMessageRoles.User : ChatMessageRoles.Assistant).ToList(),
            LastModified = annotations.LastModified
        };
    }

    public static NativeMcpContentBlock ToNativeBlock(this ContentBlock cb)
    {
        return new NativeMcpContentBlock
        {
            NativeBlock = cb
        };
    }
}