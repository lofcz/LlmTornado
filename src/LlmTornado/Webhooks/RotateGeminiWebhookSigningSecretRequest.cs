using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Webhooks;

/// <summary>
/// Controls how previously active signing secrets are revoked during rotation.
/// </summary>
public enum GeminiWebhookSecretRevocationBehavior
{
    /// <summary>
    /// Immediately invalidate the previous signing secret.
    /// </summary>
    RevokePreviousSecretsImmediately,

    /// <summary>
    /// Keep the previous signing secret valid for 24 hours after rotation.
    /// </summary>
    RevokePreviousSecretsAfterH24
}

/// <summary>
/// Request body for rotating a Gemini webhook signing secret.
/// </summary>
public class RotateGeminiWebhookSigningSecretRequest
{
    /// <summary>
    /// How to revoke the previous signing secret.
    /// </summary>
    [JsonProperty("revocation_behavior")]
    public GeminiWebhookSecretRevocationBehavior RevocationBehavior { get; set; } = GeminiWebhookSecretRevocationBehavior.RevokePreviousSecretsAfterH24;

    /// <summary>
    /// Serializes the request for the Gemini REST API.
    /// </summary>
    public string Serialize()
    {
        return JsonConvert.SerializeObject(new
        {
            revocation_behavior = RevocationBehavior switch
            {
                GeminiWebhookSecretRevocationBehavior.RevokePreviousSecretsImmediately => "REVOKE_PREVIOUS_SECRETS_IMMEDIATELY",
                GeminiWebhookSecretRevocationBehavior.RevokePreviousSecretsAfterH24 => "REVOKE_PREVIOUS_SECRETS_AFTER_H24",
                _ => "REVOKE_PREVIOUS_SECRETS_AFTER_H24"
            }
        }, EndpointBase.NullSettings);
    }
}

/// <summary>
/// Response from rotating a Gemini webhook signing secret.
/// </summary>
public class RotateGeminiWebhookSigningSecretResponse
{
    /// <summary>
    /// New signing secret returned once at rotation time. Store securely.
    /// </summary>
    [JsonProperty("new_signing_secret")]
    public string? NewSigningSecret { get; set; }
}
