using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Codex;

internal sealed class CodexOAuthLoginOperation
{
    private readonly CodexOAuthSession session;
    private readonly TcpListener listener;
    private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
    private readonly TaskCompletionSource<CodexOAuthLoginResult> completion =
        new TaskCompletionSource<CodexOAuthLoginResult>(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly string state;
    private readonly string codeVerifier;
    private readonly Uri redirectUri;

    private CodexOAuthLoginOperation(
        CodexOAuthSession session,
        TcpListener listener,
        int callbackPort,
        string state,
        string codeVerifier,
        Uri redirectUri,
        Uri authorizationUrl)
    {
        this.session = session;
        this.listener = listener;
        this.state = state;
        this.codeVerifier = codeVerifier;
        this.redirectUri = redirectUri;
        CallbackPort = callbackPort;
        AuthorizationUrl = authorizationUrl;
        _ = ListenAsync();
    }

    internal Uri AuthorizationUrl { get; }
    internal int CallbackPort { get; }

    internal static CodexOAuthLoginOperation Start(CodexOAuthSession session, CodexOAuthOptions options)
    {
        TcpListener listener = Bind(options.CallbackPort, options.FallbackCallbackPort);
        int callbackPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        Uri redirectUri = new Uri($"http://localhost:{callbackPort}/auth/callback");
        string state = CodexOAuthProtocol.GenerateRandomBase64Url(32);
        string codeVerifier = CodexOAuthProtocol.GenerateRandomBase64Url(64);
        string codeChallenge = CodexOAuthProtocol.CreateCodeChallenge(codeVerifier);
        Uri authorizationUrl = CodexOAuthProtocol.BuildAuthorizationUrl(
            options,
            redirectUri,
            state,
            codeChallenge);

        return new CodexOAuthLoginOperation(
            session,
            listener,
            callbackPort,
            state,
            codeVerifier,
            redirectUri,
            authorizationUrl);
    }

    internal Task<CodexOAuthLoginResult> WaitAsync(CancellationToken cancellationToken)
        => CodexTask.WithCancellation(completion.Task, cancellationToken);

    internal Task CancelAsync()
    {
        try
        {
            cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        listener.Stop();
        completion.TrySetResult(new CodexOAuthLoginResult(false, null, "Login cancelled."));
        return Task.CompletedTask;
    }

    private async Task ListenAsync()
    {
        try
        {
            using (cancellation.Token.Register(state => ((TcpListener)state!).Stop(), listener))
            using (TcpClient client = await CodexTask.WithCancellation(listener.AcceptTcpClientAsync(), cancellation.Token).ConfigureAwait(false))
            using (NetworkStream stream = client.GetStream())
            {
                string? requestTarget = await ReadRequestTargetAsync(stream, cancellation.Token).ConfigureAwait(false);
                if (requestTarget is null)
                {
                    await WriteResponseAsync(stream, 400, "Invalid OAuth callback.").ConfigureAwait(false);
                    completion.TrySetResult(new CodexOAuthLoginResult(false, null, "Invalid OAuth callback."));
                    return;
                }

                Uri callback = new Uri("http://localhost" + requestTarget);
                if (!string.Equals(callback.AbsolutePath, "/auth/callback", StringComparison.Ordinal))
                {
                    await WriteResponseAsync(stream, 404, "Invalid OAuth callback.").ConfigureAwait(false);
                    completion.TrySetResult(new CodexOAuthLoginResult(false, null, "Invalid OAuth callback."));
                    return;
                }

                Dictionary<string, string> query = CodexOAuthProtocol.ParseQuery(callback.Query);

                if (!query.TryGetValue("state", out string? returnedState)
                    || !CodexOAuthProtocol.FixedTimeEquals(state, returnedState))
                {
                    await WriteResponseAsync(stream, 400, "OAuth state validation failed.").ConfigureAwait(false);
                    completion.TrySetResult(new CodexOAuthLoginResult(false, null, "OAuth state validation failed."));
                    return;
                }

                if (query.TryGetValue("error", out string? oauthError))
                {
                    query.TryGetValue("error_description", out string? description);
                    string message = string.IsNullOrWhiteSpace(description) ? oauthError : description;
                    await WriteResponseAsync(stream, 400, "ChatGPT sign-in was not completed.").ConfigureAwait(false);
                    completion.TrySetResult(new CodexOAuthLoginResult(false, null, message));
                    return;
                }

                if (!query.TryGetValue("code", out string? code) || string.IsNullOrWhiteSpace(code))
                {
                    await WriteResponseAsync(stream, 400, "Authorization code is missing.").ConfigureAwait(false);
                    completion.TrySetResult(new CodexOAuthLoginResult(false, null, "Authorization code is missing."));
                    return;
                }

                CodexAccount account = await session.CompleteBrowserLoginAsync(
                    code,
                    redirectUri,
                    codeVerifier,
                    cancellation.Token).ConfigureAwait(false);

                await WriteResponseAsync(stream, 200, "ChatGPT sign-in completed. You can close this window.").ConfigureAwait(false);
                completion.TrySetResult(new CodexOAuthLoginResult(true, account, null));
            }
        }
        catch (OperationCanceledException)
        {
            completion.TrySetResult(new CodexOAuthLoginResult(false, null, "Login cancelled."));
        }
        catch (ObjectDisposedException) when (cancellation.IsCancellationRequested)
        {
            completion.TrySetResult(new CodexOAuthLoginResult(false, null, "Login cancelled."));
        }
        catch (SocketException) when (cancellation.IsCancellationRequested)
        {
            completion.TrySetResult(new CodexOAuthLoginResult(false, null, "Login cancelled."));
        }
        catch (Exception exception)
        {
            completion.TrySetResult(new CodexOAuthLoginResult(false, null, exception.Message));
        }
        finally
        {
            listener.Stop();
            cancellation.Dispose();
        }
    }

    private static TcpListener Bind(int preferredPort, int fallbackPort)
    {
        TcpListener preferred = new TcpListener(IPAddress.Loopback, preferredPort);
        try
        {
            preferred.Start();
            return preferred;
        }
        catch (SocketException) when (fallbackPort != preferredPort)
        {
            preferred.Stop();
            TcpListener fallback = new TcpListener(IPAddress.Loopback, fallbackPort);
            fallback.Start();
            return fallback;
        }
    }

    private static async Task<string?> ReadRequestTargetAsync(Stream stream, CancellationToken cancellationToken)
    {
        using StreamReader reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
        string? requestLine = await CodexTask.WithCancellation(reader.ReadLineAsync(), cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            return null;
        }

        string[] parts = requestLine.Split(' ');
        if (parts.Length < 2 || !string.Equals(parts[0], "GET", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        while (!string.IsNullOrEmpty(await CodexTask.WithCancellation(reader.ReadLineAsync(), cancellationToken).ConfigureAwait(false)))
        {
        }

        return parts[1];
    }

    private static async Task WriteResponseAsync(Stream stream, int statusCode, string message)
    {
        string statusText = statusCode switch
        {
            200 => "OK",
            404 => "Not Found",
            _ => "Bad Request"
        };
        string html = $"<!doctype html><html><body><p>{WebUtility.HtmlEncode(message)}</p></body></html>";
        byte[] body = Encoding.UTF8.GetBytes(html);
        string headers =
            $"HTTP/1.1 {statusCode} {statusText}\r\n" +
            "Content-Type: text/html; charset=utf-8\r\n" +
            $"Content-Length: {body.Length}\r\n" +
            "Connection: close\r\n\r\n";
        byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
        await stream.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);
        await stream.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
    }
}

internal static class CodexOAuthProtocol
{
    private const string Scope = "openid profile email offline_access api.connectors.read api.connectors.invoke";

    internal static Uri BuildAuthorizationUrl(
        CodexOAuthOptions options,
        Uri redirectUri,
        string state,
        string codeChallenge)
    {
        Dictionary<string, string> query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = redirectUri.AbsoluteUri,
            ["scope"] = Scope,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["id_token_add_organizations"] = "true",
            ["codex_cli_simplified_flow"] = "true",
            ["state"] = state,
            ["originator"] = options.Originator
        };

        string queryString = string.Join(
            "&",
            query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return new Uri($"{options.Issuer.AbsoluteUri.TrimEnd('/')}/oauth/authorize?{queryString}");
    }

    internal static Uri GetIssuerEndpoint(CodexOAuthOptions options, string path)
        => new Uri($"{options.Issuer.AbsoluteUri.TrimEnd('/')}/{path.TrimStart('/')}");

    internal static Uri GetApiEndpoint(CodexOAuthOptions options, string path)
        => new Uri(options.ApiBaseUri, path.TrimStart('/'));

    internal static string GenerateRandomBase64Url(int byteCount)
    {
        byte[] bytes = new byte[byteCount];
        using RandomNumberGenerator random = RandomNumberGenerator.Create();
        random.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    internal static string CreateCodeChallenge(string codeVerifier)
    {
        using SHA256 sha256 = SHA256.Create();
        return Base64UrlEncode(sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier)));
    }

    internal static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string pair in query.TrimStart('?').Split('&'))
        {
            if (string.IsNullOrWhiteSpace(pair))
            {
                continue;
            }

            int separator = pair.IndexOf('=');
            string key = separator < 0 ? pair : pair.Substring(0, separator);
            string value = separator < 0 ? string.Empty : pair.Substring(separator + 1);
            values[DecodeQueryValue(key)] = DecodeQueryValue(value);
        }

        return values;
    }

    internal static bool FixedTimeEquals(string expected, string actual)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);
        int difference = expectedBytes.Length ^ actualBytes.Length;
        int count = Math.Max(expectedBytes.Length, actualBytes.Length);

        for (int i = 0; i < count; i++)
        {
            byte left = i < expectedBytes.Length ? expectedBytes[i] : (byte)0;
            byte right = i < actualBytes.Length ? actualBytes[i] : (byte)0;
            difference |= left ^ right;
        }

        return difference == 0;
    }

    internal static CodexOAuthCredentials MergeCredentials(
        CodexOAuthTokenResponse response,
        CodexOAuthCredentials? previous)
    {
        string idToken = response.IdToken ?? previous?.IdToken ?? string.Empty;
        string accessToken = response.AccessToken ?? previous?.AccessToken ?? string.Empty;
        string refreshToken = response.RefreshToken ?? previous?.RefreshToken ?? string.Empty;

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new CodexOAuthException("OpenAI did not return complete OAuth credentials.");
        }

        JObject idClaims = DecodeJwtClaims(idToken);
        JObject accessClaims = DecodeJwtClaims(accessToken);
        JObject? idAuth = idClaims["https://api.openai.com/auth"] as JObject;
        JObject? accessAuth = accessClaims["https://api.openai.com/auth"] as JObject;
        JObject? profile = idClaims["https://api.openai.com/profile"] as JObject;
        long? expiration = accessClaims.Value<long?>("exp");

        return new CodexOAuthCredentials
        {
            IdToken = idToken,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccountId = idAuth?.Value<string>("chatgpt_account_id")
                        ?? accessAuth?.Value<string>("chatgpt_account_id")
                        ?? previous?.AccountId,
            Email = idClaims.Value<string>("email")
                    ?? profile?.Value<string>("email")
                    ?? previous?.Email,
            PlanType = idAuth?.Value<string>("chatgpt_plan_type")
                       ?? accessAuth?.Value<string>("chatgpt_plan_type")
                       ?? previous?.PlanType,
            IsFedRamp = idAuth?.Value<bool?>("chatgpt_account_is_fedramp")
                        ?? accessAuth?.Value<bool?>("chatgpt_account_is_fedramp")
                        ?? previous?.IsFedRamp
                        ?? false,
            ExpiresAtUtc = expiration.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(expiration.Value)
                : response.ExpiresIn.HasValue
                    ? DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn.Value)
                    : previous?.ExpiresAtUtc,
            LastRefreshUtc = DateTimeOffset.UtcNow
        };
    }

    private static JObject DecodeJwtClaims(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new JObject();
        }

        string[] segments = token.Split('.');
        if (segments.Length < 2)
        {
            return new JObject();
        }

        try
        {
            string payload = segments[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');
            return JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        }
        catch (Exception)
        {
            return new JObject();
        }
    }

    private static string Base64UrlEncode(byte[] value)
        => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string DecodeQueryValue(string value)
        => Uri.UnescapeDataString(value.Replace("+", " "));
}
