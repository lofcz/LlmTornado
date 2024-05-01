using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado;

/// <summary>
///     A base object for any OpenAI API endpoint, encompassing common functionality
/// </summary>
public abstract class EndpointBase
{
    private const string DataString = "data:";
    private const string DoneString = "[DONE]";
    private static string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.0.0 Safari/537.36";
    internal static readonly JsonSerializerSettings NullSettings = new() { NullValueHandling = NullValueHandling.Ignore };

    private static readonly HttpClient EndpointClient = new(new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 10000,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    })
    {
        Timeout = TimeSpan.FromSeconds(600),
        DefaultRequestVersion = new Version(2, 0)
    };

    /// <summary>
    ///     Constructor of the api endpoint base, to be called from the contructor of any devived classes.  Rather than
    ///     instantiating any endpoint yourself, access it through an instance of <see cref="TornadoApi" />.
    /// </summary>
    /// <param name="api"></param>
    internal EndpointBase(TornadoApi api)
    {
        Api = api;
    }

    internal string GetUrl(IEndpointProvider provider, string? url = null)
    {
        return provider.ApiUrl(Endpoint, url);
    }

    /// <summary>
    ///     The internal reference to the API, mostly used for authentication
    /// </summary>
    internal TornadoApi Api { get; }

    /// <summary>
    ///     The name of the endpoint, which is the final path segment in the API URL.  Must be overriden in a derived class.
    /// </summary>
    protected abstract CapabilityEndpoints Endpoint { get; }

    /// <summary>
    ///     Gets the timeout for all http requests
    /// </summary>
    /// <returns></returns>
    public static int GetRequestsTimeout()
    {
        return (int)EndpointClient.Timeout.TotalSeconds;
    }

    /// <summary>
    ///     Sets the timeout for all http requests
    /// </summary>
    /// <returns></returns>
    public static void SetRequestsTimeout(int seconds)
    {
        EndpointClient.Timeout = TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    ///     Sets the user agent header used in all http requests
    /// </summary>
    /// <param name="ua">User agent</param>
    public static void SetUserAgent(string ua)
    {
        userAgent = ua;
    }

    /// <summary>
    ///     Gets the user agent header used in all http requests
    /// </summary>
    public static string GetUserAgent()
    {
        return userAgent;
    }

    /// <summary>
    ///     Default max processing time of a http request in seconds
    /// </summary>
    public static void SetDefaultHttpTimeout(int timeoutSec)
    {
        EndpointClient.Timeout = TimeSpan.FromSeconds(timeoutSec);
    }

    /// <summary>
    ///     Gets an HTTPClient with the appropriate authorization and other headers set
    /// </summary>
    /// <returns>The fully initialized HttpClient</returns>
    /// <exception cref="AuthenticationException">
    ///     Thrown if there is no valid authentication.  Please refer to
    ///     <see href="https://github.com/OkGoDoIt/OpenAI-API-dotnet#authentication" /> for details.
    /// </exception>
    private static HttpClient GetClient()
    {
        return EndpointClient;
    }

    /// <summary>
    ///     Formats a human-readable error message relating to calling the API and parsing the response
    /// </summary>
    /// <param name="resultAsString">The full content returned in the http response</param>
    /// <param name="response">The http response object itself</param>
    /// <param name="name">The name of the endpoint being used</param>
    /// <param name="description">Additional details about the endpoint of this request (optional)</param>
    /// <param name="input">Additional details about the endpoint of this request (optional)</param>
    /// <returns>A human-readable string error message.</returns>
    private static string GetErrorMessage(string? resultAsString, HttpResponseMessage response, string name, string description, HttpRequestMessage input)
    {
        return $"Error at {name} ({description}) with HTTP status code: {response.StatusCode}. Content: {resultAsString ?? "<no content>"}. Request: {JsonConvert.SerializeObject(input.Headers)}";
    }

    /// <summary>
    ///     Sends an HTTP request and returns the response.  Does not do any parsing, but does do error handling.
    /// </summary>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="verb">
    ///     (optional) The HTTP verb to use, for example "<see cref="HttpMethod.Get" />".  If omitted, then
    ///     "GET" is assumed.
    /// </param>
    /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
    /// <param name="streaming">
    ///     (optional) If true, streams the response.  Otherwise waits for the entire response before
    ///     returning.
    /// </param>
    /// <param name="ct">(optional) A cancellation token.</param>
    /// <param name="provider"></param>
    /// <param name="endpoint"></param>
    /// <returns>The HttpResponseMessage of the response, which is confirmed to be successful.</returns>
    /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned</exception>
    private async Task<HttpResponseMessage> HttpRequestRaw(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? postData = null, bool streaming = false, CancellationToken? ct = null)
    {
        url ??= url?.StartsWith("http") ?? false ? url : provider.ApiUrl(endpoint, url);
        verb ??= HttpMethod.Get;

        HttpClient client = GetClient();
        using HttpRequestMessage req = provider.OutboundMessage(url, verb, postData, streaming);
        
        if (postData is not null)
        {
            switch (postData)
            {
                case HttpContent hData:
                    req.Content = hData;
                    break;
                case string str:
                {
                    StringContent stringContent = new(str, Encoding.UTF8, "application/json");
                    req.Content = stringContent;
                    break;
                }
                default:
                {
                    string jsonContent = JsonConvert.SerializeObject(postData, NullSettings);
                    StringContent stringContent = new(jsonContent, Encoding.UTF8, "application/json");
                    req.Content = stringContent;
                    break;
                }
            }
        }

        try
        {
            HttpResponseMessage response = await client.SendAsync(req, streaming ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, ct ?? CancellationToken.None).ConfigureAwait(ConfigureAwaitOptions.None);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            string resultAsString;

            try
            {
                resultAsString = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                resultAsString = $"Additionally, the following error was thrown when attempting to read the response content: {e}";
            }

            ProviderAuthentication? auth = Api.GetProvider(provider.Provider).Auth;

            if (auth is not null)
            {
                if (auth.ApiKey is not null)
                {
                    resultAsString = resultAsString.Replace(auth.ApiKey, "[API KEY REDACTED FOR SECURITY]");   
                }

                if (auth.Organization is not null)
                {
                    resultAsString = resultAsString.Replace(auth.Organization, "[ORGANIZATION REDACTED FOR SECURITY]");   
                }
            }
     
            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new AuthenticationException($"The API provider rejected your authorization, most likely due to an invalid API Key. Check your API key and/or other authentication requirements of the service. Full API response follows: {resultAsString}"),
                HttpStatusCode.InternalServerError => new HttpRequestException($"The API provider had an internal server error. Please retry your request. Server response: {GetErrorMessage(resultAsString, response, Endpoint.ToString(), url, req)}"),
                _ => new HttpRequestException(GetErrorMessage(resultAsString, response, Endpoint.ToString(), url, req))
            };
        }
        catch (Exception e)
        {
            throw e switch
            {
                HttpRequestException => new HttpRequestException($"An error occured when trying to contact {verb.Method} {url}. Message: {e.Message}. {(e.InnerException is not null ? $" Inner exception: {e.InnerException.Message}" : string.Empty)}"),
                TaskCanceledException => new TaskCanceledException($"An error occured when trying to contact {verb.Method} {url}. The operation timed out. Consider increasing the timeout in TornadoApi config. {(e.InnerException is not null ? $" Inner exception: {e.InnerException.Message}" : string.Empty)}"),
                _ => new Exception($"An error occured when trying to contact {verb.Method} {url}. Exception type: {e.GetType()}. Message: {e.Message}. {(e.InnerException is not null ? $" Inner exception: {e.InnerException.Message}" : string.Empty)}")
            };
        }
    }

    /*private async Task<HttpResponseMessage> HttpRequestRawWithCodes(string? url = null, HttpMethod? verb = null, object? postData = null, bool streaming = false, CancellationToken? ct = null)
    {
        url ??= Url;
        verb ??= HttpMethod.Get;

        HttpClient client = GetClient();
        using HttpRequestMessage req = new(verb, url);

        req.Headers.Add("User-Agent", userAgent);
        req.Headers.Add("OpenAI-Beta", "assistants=v2");

        if (Api.Auth is not null)
        {
            if (Api.Auth.ApiKey is not null)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Api.Auth.ApiKey);
                req.Headers.Add("api-key", Api.Auth.ApiKey);
            }

            if (Api.Auth.Organization is not null) req.Headers.Add("OpenAI-Organization", Api.Auth.Organization);
        }

        if (postData != null)
        {
            if (postData is HttpContent data)
            {
                req.Content = data;
            }
            else
            {
                string jsonContent = JsonConvert.SerializeObject(postData, NullSettings);
                StringContent stringContent = new(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        HttpResponseMessage response = await client.SendAsync(req, streaming ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, ct ?? CancellationToken.None).ConfigureAwait(ConfigureAwaitOptions.None);

        if (response.IsSuccessStatusCode || response.StatusCode is HttpStatusCode.NotFound) return response;

        string resultAsString;

        try
        {
            resultAsString = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            resultAsString = $"Additionally, the following error was thrown when attemping to read the response content: {e}";
        }

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new AuthenticationException($"The API provider rejected your authorization, most likely due to an invalid API Key. Check your API Key and see https://github.com/lofcz/LlmTornado#authentication for guidance. Full API response follows: {resultAsString}"),
            HttpStatusCode.InternalServerError => new HttpRequestException($"The API provider had an internal server error. Please retry your request. Server response: {GetErrorMessage(resultAsString, response, Endpoint, url, req)}"),
            _ => new HttpRequestException(GetErrorMessage(resultAsString, response, Endpoint, url, req))
        };
    }*/

    private async Task<RestDataOrException<HttpResponseMessage>> HttpRequestRawWithAllCodes(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? content = null, bool streaming = false, CancellationToken? ct = null)
    {
        url ??= url?.StartsWith("http") ?? false ? url : provider.ApiUrl(endpoint, url);
        verb ??= HttpMethod.Get;

        HttpClient client = GetClient();
        using HttpRequestMessage req = provider.OutboundMessage(url, verb, content, streaming);
        
        if (content is not null)
        {
            switch (content)
            {
                case HttpContent hData:
                    req.Content = hData;
                    break;
                case string str:
                {
                    StringContent stringContent = new(str, Encoding.UTF8, "application/json");
                    req.Content = stringContent;
                    break;
                }
                default:
                {
                    string jsonContent = JsonConvert.SerializeObject(content, NullSettings);
                    StringContent stringContent = new(jsonContent, Encoding.UTF8, "application/json");
                    req.Content = stringContent;
                    break;
                }
            }
        }

        if (content is not null)
        {
            if (content is HttpContent data)
            {
                req.Content = data;
            }
            else
            {
                string jsonContent = JsonConvert.SerializeObject(content, NullSettings);
                StringContent stringContent = new(jsonContent, Encoding.UTF8, "application/json");
                req.Content = stringContent;
            }
        }

        try
        {
            return new RestDataOrException<HttpResponseMessage>(await client.SendAsync(req, streaming ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, ct ?? CancellationToken.None).ConfigureAwait(ConfigureAwaitOptions.None));
        }
        catch (Exception e)
        {
            return new RestDataOrException<HttpResponseMessage>(e);
        }
    }

    /// <summary>
    ///     Sends an HTTP Get request and return the string content of the response without parsing, and does error handling.
    /// </summary>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="ct">A cancellation token</param>
    /// <returns>The text string of the response, which is confirmed to be successful.</returns>
    /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned</exception>
    internal async Task<string> HttpGetContent(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, CancellationToken? ct = null)
    {
        using HttpResponseMessage response = await HttpRequestRaw(provider, endpoint, url, ct: ct).ConfigureAwait(ConfigureAwaitOptions.None);
        return await response.Content.ReadAsStringAsync().ConfigureAwait(ConfigureAwaitOptions.None);
    }

    /// <summary>
    ///     Sends an HTTP Request and does initial parsing
    /// </summary>
    /// <typeparam name="T">The <see cref="ApiResultBase" />-derived class for the result</typeparam>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="verb">
    ///     (optional) The HTTP verb to use, for example "<see cref="HttpMethod.Get" />".  If omitted, then
    ///     "GET" is assumed.
    /// </param>
    /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
    /// <param name="ct">(optional) A cancellation token</param>
    /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T" /></returns>
    /// <exception cref="HttpRequestException">
    ///     Throws an exception if a non-success HTTP response was returned or if the result
    ///     couldn't be parsed.
    /// </exception>
    private async Task<T?> HttpRequest<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? postData = null, CancellationToken? ct = null) where T : ApiResultBase
    {
        using HttpResponseMessage response = await HttpRequestRaw(provider, endpoint, url, verb, postData, false, ct).ConfigureAwait(ConfigureAwaitOptions.None);
        string resultAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(ConfigureAwaitOptions.None);
        T? res = provider.InboundMessage<T>(resultAsString, postData?.ToString());

        try
        {
            if (res is not null)
            {
                res.Provider = provider;
                
                if (response.Headers.TryGetValues("Openai-Organization", out IEnumerable<string>? orgH)) res.Organization = orgH.FirstOrDefault();
                if (response.Headers.TryGetValues("X-Request-ID", out IEnumerable<string>? xreqId)) res.RequestId = xreqId.FirstOrDefault();

                if (response.Headers.TryGetValues("Openai-Processing-Ms", out IEnumerable<string>? pms))
                {
                    string? processing = pms.FirstOrDefault();
                    if (processing is not null && int.TryParse(processing, out int n)) res.ProcessingTime = TimeSpan.FromMilliseconds(n);
                }

                if (response.Headers.TryGetValues("Openai-Version", out IEnumerable<string>? oav)) res.RequestId = oav.FirstOrDefault();
                if (res.Model != null && string.IsNullOrEmpty(res.Model))
                    if (response.Headers.TryGetValues("Openai-Model", out IEnumerable<string>? omd))
                        res.Model = omd.FirstOrDefault();
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Error parsing metadata: {e.Message}");
        }

        return res;
    }

    private async Task<HttpCallResult<T>> HttpRequestRaw<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? postData = null, CancellationToken? ct = null)
    {
        RestDataOrException<HttpResponseMessage> response = await HttpRequestRawWithAllCodes(provider, endpoint, url, verb, postData, false, ct).ConfigureAwait(ConfigureAwaitOptions.None);

        if (response.Exception is not null)
        {
            return new HttpCallResult<T>(HttpStatusCode.ServiceUnavailable, null, default, false)
            {
                Exception = response.Exception
            };
        }

        string resultAsString = await response.Data!.Content.ReadAsStringAsync().ConfigureAwait(ConfigureAwaitOptions.None);
        HttpCallResult<T> result = new(response.Data.StatusCode, resultAsString, default, response.Data.StatusCode is >= HttpStatusCode.OK and < HttpStatusCode.InternalServerError);

        if (response.Data.IsSuccessStatusCode)
        {
            result.Data = provider.InboundMessage<T>(resultAsString, postData?.ToString());
        }

        response.Data?.Dispose();
        return result;
    }

    private async Task<StreamResponse?> HttpRequestStream(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? postData = null, CancellationToken ct = default)
    {
        HttpResponseMessage response = await HttpRequestRaw(provider, endpoint, url, verb, postData, ct: ct).ConfigureAwait(ConfigureAwaitOptions.None);
        Stream resultAsStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(ConfigureAwaitOptions.None);

        StreamResponse res = new()
        {
            Headers = new ApiResultBase(),
            Stream = resultAsStream,
            Response = response
        };

        try
        {
            if (response.Headers.TryGetValues("Openai-Organization", out IEnumerable<string>? orgH)) res.Headers.Organization = orgH.FirstOrDefault();
            if (response.Headers.TryGetValues("X-Request-ID", out IEnumerable<string>? xreqId)) res.Headers.RequestId = xreqId.FirstOrDefault();

            if (response.Headers.TryGetValues("Openai-Processing-Ms", out IEnumerable<string>? pms))
            {
                string? processing = pms.FirstOrDefault();
                if (processing is not null && int.TryParse(processing, out int n)) res.Headers.ProcessingTime = TimeSpan.FromMilliseconds(n);
            }

            if (response.Headers.TryGetValues("Openai-Version", out IEnumerable<string>? oav)) res.Headers.RequestId = oav.FirstOrDefault();
            if (res.Headers.Model != null && string.IsNullOrEmpty(res.Headers.Model))
                if (response.Headers.TryGetValues("Openai-Model", out IEnumerable<string>? omd))
                    res.Headers.Model = omd.FirstOrDefault();
        }
        catch (Exception e)
        {
            throw new Exception($"Error parsing metadata: {e.Message}");
        }

        return res;
    }

    /// <summary>
    ///     Sends an HTTP Get request and does initial parsing
    /// </summary>
    /// <typeparam name="T">The <see cref="ApiResultBase" />-derived class for the result</typeparam>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="ct">A cancellation token</param>
    /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T" /></returns>
    /// <exception cref="HttpRequestException">
    ///     Throws an exception if a non-success HTTP response was returned or if the result
    ///     couldn't be parsed.
    /// </exception>
    internal Task<T?> HttpGet<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, CancellationToken? ct = null) where T : ApiResultBase
    {
        return HttpRequest<T>(provider, endpoint, url, HttpMethod.Get, ct: ct);
    }

    internal Task<HttpCallResult<T>> HttpGetRaw<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, CancellationToken? ct = null, bool allowNon200Codes = false)
    {
        return HttpRequestRaw<T>(provider, endpoint, url, HttpMethod.Get, ct: ct);
    }

    internal Task<T?> HttpPost1<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, object? postData = null, CancellationToken? ct = default) where T : ApiResultBase
    {
        return HttpRequest<T>(provider, endpoint, url, HttpMethod.Post, postData, ct);
    }

    /// <summary>
    ///     Sends an HTTP Post request and does initial parsing
    /// </summary>
    /// <typeparam name="T">The <see cref="ApiResultBase" />-derived class for the result</typeparam>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
    /// <param name="ct">(optional) Cancellation token.</param>
    /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T" /></returns>
    /// <exception cref="HttpRequestException">
    ///     Throws an exception if a non-success HTTP response was returned or if the result
    ///     couldn't be parsed.
    /// </exception>
    internal Task<HttpCallResult<T>> HttpPost<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, object? postData = null, CancellationToken? ct = default) where T : ApiResultBase
    {
        return HttpRequestRaw<T>(provider, endpoint, url, HttpMethod.Post, postData, ct);
    }

    internal Task<HttpCallResult<T>> HttpPostRaw<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, object? postData = null, CancellationToken? ct = default, bool allowNon200Codes = false)
    {
        return HttpRequestRaw<T>(provider, endpoint, url, HttpMethod.Post, postData, ct);
    }

    internal Task<StreamResponse?> HttpPostStream(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, object? postData = null, CancellationToken ct = default)
    {
        return HttpRequestStream(provider, endpoint, url, HttpMethod.Post, postData, ct);
    }

    /// <summary>
    ///     Sends an HTTP Delete request and does initial parsing
    /// </summary>
    /// <typeparam name="T">The <see cref="ApiResultBase" />-derived class for the result</typeparam>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
    /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T" /></returns>
    /// <exception cref="HttpRequestException">
    ///     Throws an exception if a non-success HTTP response was returned or if the result
    ///     couldn't be parsed.
    /// </exception>
    internal Task<T?> HttpDelete<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, object? postData = null, CancellationToken? ct = default) where T : ApiResultBase
    {
        return HttpRequest<T>(provider, endpoint, url, HttpMethod.Delete, postData, ct);
    }

    internal Task<HttpCallResult<T>> HttpAtomic<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, HttpMethod method, string? url = null, object? postData = null, CancellationToken? ct = default, bool allowNon200Codes = false)
    {
        return HttpRequestRaw<T>(provider, endpoint, url, method, postData, ct);
    }

    /// <summary>
    ///     Sends an HTTP Put request and does initial parsing
    /// </summary>
    /// <typeparam name="T">The <see cref="ApiResultBase" />-derived class for the result</typeparam>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request.  If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
    /// <returns>An awaitable Task with the parsed result of type <typeparamref name="T" /></returns>
    /// <exception cref="HttpRequestException">
    ///     Throws an exception if a non-success HTTP response was returned or if the result
    ///     couldn't be parsed.
    /// </exception>
    internal Task<T?> HttpPut<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, object? postData = null, CancellationToken ct = default) where T : ApiResultBase
    {
        return HttpRequest<T>(provider, endpoint, url, HttpMethod.Put, postData, ct);
    }

    /// <summary>
    ///     Sends an HTTP request and handles a streaming response.  Does basic line splitting and error handling.
    /// </summary>
    /// <param name="url">
    ///     (optional) If provided, overrides the url endpoint for this request. If omitted, then
    ///     <see cref="Url" /> will be used.
    /// </param>
    /// <param name="verb">
    ///     (optional) The HTTP verb to use, for example "<see cref="HttpMethod.Get" />".  If omitted, then
    ///     "GET" is assumed.
    /// </param>
    /// <param name="postData">(optional) A json-serializable object to include in the request body.</param>
    /// <param name="requestRef">(optional) A container for JSON-encoded outbound request.</param>
    /// <returns>The HttpResponseMessage of the response, which is confirmed to be successful.</returns>
    /// <exception cref="HttpRequestException">Throws an exception if a non-success HTTP response was returned</exception>
    protected async IAsyncEnumerable<T> HttpStreamingRequest<T>(IEndpointProvider provider, CapabilityEndpoints endpoint, string? url = null, HttpMethod? verb = null, object? postData = null, Ref<string>? requestRef = null) where T : ApiResultBase
    {
        using HttpResponseMessage response = await HttpRequestRaw(provider, endpoint, url, verb, postData, true).ConfigureAwait(ConfigureAwaitOptions.None);

        string? organization = null;
        string? requestId = null;
        TimeSpan processingTime = TimeSpan.Zero;
        string? openaiVersion = null;
        string? modelFromHeaders = null;

        try
        {
            if (response.Headers.TryGetValues("Openai-Organization", out IEnumerable<string>? orgH)) organization = orgH.FirstOrDefault();
            if (response.Headers.TryGetValues("X-Request-ID", out IEnumerable<string>? xreqId)) requestId = xreqId.FirstOrDefault();

            if (response.Headers.TryGetValues("Openai-Processing-Ms", out IEnumerable<string>? pms))
            {
                string? processing = pms.FirstOrDefault();
                if (processing is not null && int.TryParse(processing, out int n)) processingTime = TimeSpan.FromMilliseconds(n);
            }

            if (response.Headers.TryGetValues("Openai-Version", out IEnumerable<string>? oav)) openaiVersion = oav.FirstOrDefault();
            if (response.Headers.TryGetValues("Openai-Model", out IEnumerable<string>? omd)) modelFromHeaders = omd.FirstOrDefault();
        }
        catch (Exception e)
        {
            Debug.Print($"Issue parsing metadata of OpenAi Response. Url: {url}, Error: {e}. This is probably ignorable.");
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync();
        using StreamReader reader = new(stream);

        await foreach (T? x in provider.InboundStream<T>(reader))
        {
            if (x is null)
            {
                continue;
            }

            x.Provider = provider;
            yield return x;
        }
    }
}