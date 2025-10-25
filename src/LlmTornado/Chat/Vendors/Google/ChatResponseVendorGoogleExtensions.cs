using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Google;

/// <summary>
/// Chat response features supported only by Google.
/// </summary>
public class ChatResponseVendorGoogleExtensions
{
    /// <summary>
    /// Optional. Resource name of the Google Maps widget context token that can be used with the PlacesContextElement widget in order to render contextual data. Only populated in the case that grounding with Google Maps is enabled.
    /// </summary>
    [JsonProperty("googleMapsWidgetContextToken")]
    public string? GoogleMapsWidgetContextToken { get; set; }
    
    /// <summary>
    /// Empty Google response extensions.
    /// </summary>
    public ChatResponseVendorGoogleExtensions()
    {
        
    }
}
