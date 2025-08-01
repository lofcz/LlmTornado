using Microsoft.JSInterop;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorWorker.Extensions.JSRuntime
{
    internal sealed class DotNetObjectReferenceJsonConverter<TValue> : JsonConverter<DotNetObjectReference<TValue>> where TValue : class
    {
        public DotNetObjectReferenceJsonConverter(BlazorWorkerJSRuntime jsRuntime)
        {
            CallbackJSRuntime = jsRuntime;
        }

        public BlazorWorkerJSRuntime CallbackJSRuntime { get; }

        public override DotNetObjectReference<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            long dotNetObjectId = 0;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (dotNetObjectId == 0 && reader.ValueTextEquals(DotNetObjectReferenceTracker.DotNetObjectRefKey.EncodedUtf8Bytes))
                    {
                        reader.Read();
                        dotNetObjectId = reader.GetInt64();
                    }
                    else
                    {
                        throw new JsonException($"Unexcepted JSON property {reader.GetString()}.");
                    }
                }
                else
                {
                    throw new JsonException($"Unexcepted JSON Token {reader.TokenType}.");
                }
            }

            if (dotNetObjectId is 0)
            {
                throw new JsonException($"Required property {DotNetObjectReferenceTracker.DotNetObjectRefKey} not found.");
            }

            var value = DotNetObjectReferenceTracker.GetObjectReference<TValue>(dotNetObjectId);
            return value;
        }

        public override void Write(Utf8JsonWriter writer, DotNetObjectReference<TValue> value, JsonSerializerOptions options)
        {
            DotNetObjectReferenceTracker.SetCallbackJSRuntime(value, CallbackJSRuntime);
            var objectId = DotNetObjectReferenceTracker.TrackObjectReference(value);

            writer.WriteStartObject();
            writer.WriteNumber(DotNetObjectReferenceTracker.DotNetObjectRefKey, objectId);
            
            writer.WriteEndObject();
        }
    }
}
