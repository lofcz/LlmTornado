using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Code;
using LlmTornado.Files.Vendors;
using LlmTornado.Files.Vendors.Google;
using Newtonsoft.Json;

namespace LlmTornado.Files;

internal enum FileUploadRequestStates
{
    Unknown,
    PayloadUrlObtained
}

/// <summary>
/// Request to upload a file.
/// </summary>
public class FileUploadRequest
{
    /// <summary>
    /// Bytes of the file.
    /// </summary>
    public byte[] Bytes { get; set; }
    
    /// <summary>
    /// File name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Purpose of the file, supported only by OpenAi.
    /// </summary>
    public FilePurpose? Purpose { get; set; }
    
    /// <summary>
    /// MIME type
    /// </summary>
    public string? MimeType { get; set; }
    
    /// <summary>
    /// Display name
    /// </summary>
    public string? DisplayName { get; set; }
    
    internal FileUploadRequestStates? InternalState { get; set; } 
    
    private static string GetPurpose(FilePurpose purpose)
    {
        return purpose switch
        {
            FilePurpose.Finetune => "fine-tune",
            FilePurpose.Assistants => "assistants",
            _ => string.Empty
        };
    }
    
    internal static TornadoFile? Deserialize(LLmProviders provider, string jsonData, string? postData)
    {
        return provider switch
        {
            LLmProviders.Google => JsonConvert.DeserializeObject<VendorGoogleTornadoFile>(jsonData)?.ToFile(postData),
            _ => JsonConvert.DeserializeObject<TornadoFile>(jsonData)
        };
    }
    
    private static readonly Dictionary<LLmProviders, Func<FileUploadRequest, IEndpointProvider, object>> SerializeMap = new Dictionary<LLmProviders, Func<FileUploadRequest, IEndpointProvider, object>>
    {
        { 
            LLmProviders.OpenAi, (x, y) =>
            {
                ByteArrayContent bc = new ByteArrayContent(x.Bytes);
                StringContent sc = new StringContent(x.Purpose is null ? "assistants" : GetPurpose(x.Purpose.Value));
                
                MultipartFormDataContent content = new MultipartFormDataContent();
                content.Add(sc, "purpose");
                content.Add(bc, "file", x.Name);

                return content;
            }
        },
        { 
            LLmProviders.Anthropic, (x, y) =>
            {
                ByteArrayContent bc = new ByteArrayContent(x.Bytes);
                bc.Headers.ContentType = new MediaTypeHeaderValue(x.MimeType ?? "application/pdf");

                MultipartFormDataContent content = new MultipartFormDataContent();
                content.Add(bc, "file", x.Name);

                return content;

            }
        },
        { 
            LLmProviders.Google, (x, y) =>
            {
                if (x.InternalState is FileUploadRequestStates.PayloadUrlObtained)
                {
                    ByteArrayContent content = new ByteArrayContent(x.Bytes);
                    content.Headers.ContentLength = x.Bytes.Length;
            
                    return content;
                }
                
                return new
                {
                    file = new
                    {
                        display_name = x.DisplayName
                    }
                };
            } 
        }
    };
    
    /// <summary>
    ///	Serializes the file upload request into the request body, based on the conventions used by the LLM provider.
    /// </summary>
    public TornadoRequestContent Serialize(IEndpointProvider provider)
    {
        return SerializeMap.TryGetValue(provider.Provider, out Func<FileUploadRequest, IEndpointProvider, object>? serializerFn) ? new TornadoRequestContent(serializerFn.Invoke(this, provider), null, null, provider, CapabilityEndpoints.Files) : new TornadoRequestContent(string.Empty, null, null, provider, CapabilityEndpoints.Files);
    }
}