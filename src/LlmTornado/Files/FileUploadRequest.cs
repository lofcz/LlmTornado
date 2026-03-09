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
using LlmTornado.Files.Vendors.MiniMax;
using LlmTornado.Files.Vendors.Zai;
using Newtonsoft.Json;

namespace LlmTornado.Files;

internal enum FileUploadRequestStates
{
    Unknown,
    PayloadUrlObtained
}

/// <summary>
/// Anchor timestamp after which the expiration policy applies.
/// </summary>
public enum FileUploadExpirationAnchor
{
    /// <summary>
    /// Anchor to the file creation timestamp.
    /// </summary>
    CreatedAt
}

/// <summary>
/// Expiration policy for a file. By default, files with purpose=batch expire after 30 days and all other files are persisted until they are manually deleted.
/// </summary>
public class FileUploadExpiration
{
    /// <summary>
    /// Anchor timestamp after which the expiration policy applies. Supported anchors: created_at.
    /// </summary>
    [JsonProperty("anchor")]
    public FileUploadExpirationAnchor Anchor { get; set; } = FileUploadExpirationAnchor.CreatedAt;
    
    /// <summary>
    /// The number of seconds after the anchor time that the file will expire. Must be between 3600 (1 hour) and 2592000 (30 days).
    /// </summary>
    [JsonProperty("seconds")]
    public int Seconds { get; set; }

    /// <summary>
    /// Creates an empty expiration
    /// </summary>
    public FileUploadExpiration()
    {
        
    }

    /// <summary>
    /// Creates an expiration from a timespan.
    /// </summary>
    /// <param name="ttl"></param>
    public FileUploadExpiration(TimeSpan ttl)
    {
        Seconds = (int)ttl.TotalSeconds;
    }
}


/// <summary>
/// Request to upload a file.
/// </summary>
public class FileUploadRequest
{
    /// <summary>
    /// Creates a request to upload a file from a local path.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <param name="purpose">Purpose of the file.</param>
    public FileUploadRequest(string path, FilePurpose purpose)
    {
        Bytes = System.IO.File.ReadAllBytes(path);
        Name = System.IO.Path.GetFileName(path);
        Purpose = purpose;
    }

    /// <summary>
    /// Creates an empty file upload request.
    /// </summary>
    public FileUploadRequest()
    {
    }

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
    
    /// <summary>
    /// Optional expiration policy for the file. Supported only by OpenAI.
    /// </summary>
    public FileUploadExpiration? Expiration { get; set; }
    
    internal FileUploadRequestStates? InternalState { get; set; } 
    
    private static string GetPurpose(FilePurpose purpose, LLmProviders provider)
    {
        // purposes supported only by specific providers
        if (provider is LLmProviders.Mistral)
        {
            if (purpose is FilePurpose.Ocr)
            {
                return "ocr";
            }
        }
        
        // Groq only supports "batch" purpose
        if (provider is LLmProviders.Groq)
        {
            return "batch";
        }
        
        // MiniMax-specific purposes
        if (provider is LLmProviders.MiniMax)
        {
            return purpose switch
            {
                FilePurpose.VoiceClone => "voice_clone",
                FilePurpose.PromptAudio => "prompt_audio",
                FilePurpose.TextToAudioAsyncInput => "t2a_async_input",
                _ => "voice_clone"
            };
        }
        
        // general fallback
        return purpose switch
        {
            FilePurpose.Finetune => "fine-tune",
            FilePurpose.Assistants => "assistants",
            FilePurpose.Agent => "agent",
            FilePurpose.Batch => "batch",
            FilePurpose.Vision => "vision",
            FilePurpose.UserData => "user_data",
            FilePurpose.Evals => "evals",
            _ => "user_data"
        };
    }
    
    private static string GetExpirationAnchor(FileUploadExpirationAnchor anchor)
    {
        return anchor switch
        {
            FileUploadExpirationAnchor.CreatedAt => "created_at",
            _ => "created_at"
        };
    }
    
    internal static TornadoFile? Deserialize(LLmProviders provider, string jsonData, string? postData)
    {
        return provider switch
        {
            LLmProviders.Google => JsonConvert.DeserializeObject<VendorGoogleTornadoFile>(jsonData)?.ToFile(postData),
            LLmProviders.Zai => JsonConvert.DeserializeObject<VendorZaiTornadoFile>(jsonData)?.ToFile(),
            LLmProviders.MiniMax => JsonConvert.DeserializeObject<VendorMiniMaxUploadResponse>(jsonData)?.File?.ToFile(),
            _ => JsonConvert.DeserializeObject<TornadoFile>(jsonData)
        };
    }

    private static object SerializeOpenAiLike(FileUploadRequest x, IEndpointProvider y)
    {
        ByteArrayContent bc = new ByteArrayContent(x.Bytes);
        StringContent sc = new StringContent(x.Purpose is null ? "user_data" : GetPurpose(x.Purpose.Value, y.Provider));
                
        MultipartFormDataContent content = new MultipartFormDataContent();
        content.Add(sc, "purpose");
        content.Add(bc, "file", x.Name);
                
        if (x.Expiration is not null)
        {
            string anchorValue = GetExpirationAnchor(x.Expiration.Anchor);
            string secondsValue = x.Expiration.Seconds.ToString();
                    
            StringContent anchorContent = new StringContent(anchorValue);
            content.Add(anchorContent, "expires_after[anchor]");
                    
            StringContent secondsContent = new StringContent(secondsValue);
            content.Add(secondsContent, "expires_after[seconds]");
        }

        return content;
    }
    
    private static readonly Dictionary<LLmProviders, Func<FileUploadRequest, IEndpointProvider, object>> SerializeMap = new Dictionary<LLmProviders, Func<FileUploadRequest, IEndpointProvider, object>>
    {
        { 
            LLmProviders.OpenAi, SerializeOpenAiLike
        },
        { 
            LLmProviders.Mistral, SerializeOpenAiLike
        },
        { 
            LLmProviders.Groq, SerializeOpenAiLike
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
        },
        { 
            LLmProviders.Zai, (x, y) =>
            {
                ByteArrayContent bc = new ByteArrayContent(x.Bytes);
                StringContent sc = new StringContent(x.Purpose is null ? "agent" : GetPurpose(x.Purpose.Value, y.Provider));
                
                MultipartFormDataContent content = new MultipartFormDataContent();
                content.Add(sc, "purpose");
                content.Add(bc, "file", x.Name);

                return content;
            }
        },
        { 
            LLmProviders.MiniMax, (x, y) =>
            {
                ByteArrayContent bc = new ByteArrayContent(x.Bytes);
                StringContent sc = new StringContent(x.Purpose is null ? "voice_clone" : GetPurpose(x.Purpose.Value, y.Provider));
                
                MultipartFormDataContent content = new MultipartFormDataContent();
                content.Add(sc, "purpose");
                content.Add(bc, "file", x.Name);

                return content;
            }
        }
    };
    
    /// <summary>
    ///	Serializes the file upload request into the request body, based on the conventions used by the LLM provider.
    /// </summary>
    public TornadoRequestContent Serialize(IEndpointProvider provider)
    {
        return SerializeMap.TryGetValue(provider.Provider, out Func<FileUploadRequest, IEndpointProvider, object>? serializerFn) ? 
            new TornadoRequestContent(serializerFn.Invoke(this, provider), null, null, provider, CapabilityEndpoints.Files) : 
            new TornadoRequestContent(string.Empty, null, null, provider, CapabilityEndpoints.Files);
    }
}