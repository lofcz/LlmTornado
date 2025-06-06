using System;
using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Chat;

internal class ChatMessageRolesCls
{
    internal static readonly Dictionary<string, ChatMessageRoles> MemberRolesDict = new Dictionary<string, ChatMessageRoles>
    {
        { "system", ChatMessageRoles.System },
        { "user", ChatMessageRoles.User },
        { "assistant", ChatMessageRoles.Assistant },
        { "tool", ChatMessageRoles.Tool }
    };
    
    internal static readonly Dictionary<ChatMessageRoles, string> MemberRolesDictInverse = new Dictionary<ChatMessageRoles, string>
    {
        { ChatMessageRoles.System, "system" },
        { ChatMessageRoles.User, "user" },
        { ChatMessageRoles.Assistant, "assistant" },
        { ChatMessageRoles.Tool, "tool" }
    };
    
    internal static ChatMessageRoles? MemberFromString(string? roleName)
    {
        return MemberRolesDict.GetValueOrDefault(roleName?.ToLowerInvariant().Trim() ?? string.Empty);
    }
    
    internal static string? MemberToString(ChatMessageRoles? role)
    {
        return MemberRolesDictInverse.GetValueOrDefault(role ?? ChatMessageRoles.User);
    }
    
    internal class ChatMessageRoleJsonConverter : JsonConverter<ChatMessageRoles>
    {
        public override void WriteJson(JsonWriter writer, ChatMessageRoles value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override ChatMessageRoles ReadJson(JsonReader reader, Type objectType, ChatMessageRoles existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType is JsonToken.String)
            {
                string? str = reader.Value as string;
                return MemberRolesDict.GetValueOrDefault(str ?? string.Empty, ChatMessageRoles.Unknown);
            }

            return MemberRolesDict.GetValueOrDefault(reader.ReadAsString() ?? string.Empty, ChatMessageRoles.Unknown);
        }
    }
}