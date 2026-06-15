using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LlmTornado.Webhooks;

/// <summary>
/// Standard Webhooks header names used by Gemini static webhook deliveries.
/// </summary>
public static class GeminiWebhookHeaders
{
    /// <summary>Unique delivery id for deduplication.</summary>
    public const string WebhookId = "webhook-id";

    /// <summary>Unix timestamp of the delivery.</summary>
    public const string WebhookTimestamp = "webhook-timestamp";

    /// <summary>HMAC signature (static) or JWT (dynamic) depending on configuration.</summary>
    public const string WebhookSignature = "webhook-signature";
}

/// <summary>
/// Verifies Gemini static webhook deliveries using the Standard Webhooks HMAC-SHA256 scheme.
/// </summary>
public static class GeminiWebhookSignatureVerifier
{
    /// <summary>
    /// Google JWKS endpoint for verifying dynamic webhook JWT signatures.
    /// </summary>
    public const string DynamicJwksUri = "https://generativelanguage.googleapis.com/.well-known/jwks.json";

    /// <summary>
    /// Default replay-protection window recommended by Gemini (5 minutes).
    /// </summary>
    public static TimeSpan DefaultTimestampTolerance { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Verifies a static webhook delivery and returns the parsed event on success.
    /// </summary>
    /// <param name="payload">Raw request body.</param>
    /// <param name="headers">Request headers (keys may be any casing).</param>
    /// <param name="signingSecret">Signing secret from create/rotate (whsec_ prefix optional).</param>
    /// <param name="timestampTolerance">Maximum age of the webhook-timestamp header.</param>
    /// <returns>Parsed event if signature and timestamp are valid.</returns>
    /// <exception cref="GeminiWebhookVerificationException">Verification failed.</exception>
    public static GeminiWebhookEvent VerifyStaticDelivery(
        string payload,
        IReadOnlyDictionary<string, string> headers,
        string signingSecret,
        TimeSpan? timestampTolerance = null)
    {
        if (string.IsNullOrEmpty(signingSecret))
        {
            throw new GeminiWebhookVerificationException("Signing secret is required.");
        }

        string? webhookId = GetHeader(headers, GeminiWebhookHeaders.WebhookId);
        string? webhookTimestamp = GetHeader(headers, GeminiWebhookHeaders.WebhookTimestamp);
        string? webhookSignature = GetHeader(headers, GeminiWebhookHeaders.WebhookSignature);

        if (string.IsNullOrEmpty(webhookId) || string.IsNullOrEmpty(webhookTimestamp) || string.IsNullOrEmpty(webhookSignature))
        {
            throw new GeminiWebhookVerificationException("Missing required Standard Webhooks headers.");
        }

        if (!long.TryParse(webhookTimestamp, out long unixTimestamp))
        {
            throw new GeminiWebhookVerificationException("Invalid webhook-timestamp header.");
        }

        DateTimeOffset eventTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        TimeSpan tolerance = timestampTolerance ?? DefaultTimestampTolerance;

        if (Math.Abs((DateTimeOffset.UtcNow - eventTime).TotalSeconds) > tolerance.TotalSeconds)
        {
            throw new GeminiWebhookVerificationException("Webhook timestamp is outside the allowed tolerance window.");
        }

        byte[] secretBytes = DecodeSigningSecret(signingSecret);
        string signedContent = $"{webhookId}.{webhookTimestamp}.{payload}";
        byte[] expectedSignature = ComputeHmacSha256(secretBytes, signedContent);

        if (!SignatureMatches(webhookSignature, expectedSignature))
        {
            throw new GeminiWebhookVerificationException("Webhook signature is invalid.");
        }

        GeminiWebhookEvent? webhookEvent = GeminiWebhookEvent.Parse(payload);

        if (webhookEvent is null)
        {
            throw new GeminiWebhookVerificationException("Webhook payload could not be parsed.");
        }

        return webhookEvent;
    }

    /// <summary>
    /// Parses a webhook payload without signature verification.
    /// Use only when verification is handled elsewhere.
    /// </summary>
    public static GeminiWebhookEvent? ParseUnverified(string payload)
    {
        return GeminiWebhookEvent.Parse(payload);
    }

    private static string? GetHeader(IReadOnlyDictionary<string, string> headers, string name)
    {
        foreach (KeyValuePair<string, string> header in headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return header.Value;
            }
        }

        return null;
    }

    private static byte[] DecodeSigningSecret(string signingSecret)
    {
        string secret = signingSecret.StartsWith("whsec_", StringComparison.Ordinal)
            ? signingSecret["whsec_".Length..]
            : signingSecret;

        try
        {
            return Convert.FromBase64String(secret);
        }
        catch (FormatException ex)
        {
            throw new GeminiWebhookVerificationException("Signing secret is not valid base64.", ex);
        }
    }

    private static byte[] ComputeHmacSha256(byte[] secret, string content)
    {
        using HMACSHA256 hmac = new HMACSHA256(secret);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(content));
    }

    private static bool SignatureMatches(string signatureHeader, byte[] expectedSignature)
    {
        string expectedBase64 = Convert.ToBase64String(expectedSignature);

        foreach (string part in signatureHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            int commaIndex = part.IndexOf(',');
            if (commaIndex <= 0 || commaIndex >= part.Length - 1)
            {
                continue;
            }

            string version = part[..commaIndex];
            string signature = part[(commaIndex + 1)..];

            if (!string.Equals(version, "v1", StringComparison.Ordinal))
            {
                continue;
            }

            if (FixedTimeEquals(signature, expectedBase64))
            {
                return true;
            }
        }

        return false;
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        int diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }

        return diff == 0;
    }
}

/// <summary>
/// Thrown when a Gemini webhook delivery fails verification.
/// </summary>
public class GeminiWebhookVerificationException : Exception
{
    /// <summary>
    /// Creates a verification exception.
    /// </summary>
    public GeminiWebhookVerificationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a verification exception with an inner cause.
    /// </summary>
    public GeminiWebhookVerificationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
