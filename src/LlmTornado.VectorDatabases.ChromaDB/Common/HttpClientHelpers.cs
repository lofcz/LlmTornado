using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LlmTornado.VectorDatabases.ChromaDB.Client;
using LlmTornado.VectorDatabases.ChromaDB.Client.Models.Requests;
using LlmTornado.VectorDatabases.ChromaDB.Client.Models.Responses;

namespace LlmTornado.VectorDatabases.ChromaDB.Common;

internal static partial class HttpClientHelpers
{
	private static readonly JsonSerializerOptions PostJsonSerializerOptions = new()
	{
		AllowTrailingCommas = false,
		ReferenceHandler = ReferenceHandler.IgnoreCycles,
		ReadCommentHandling = JsonCommentHandling.Skip,
	};

	private static readonly JsonSerializerOptions DeserializerJsonSerializerOptions = new()
	{
		Converters =
		{
			new ObjectToInferredTypesJsonConverter(),
		},
	};

	public static async Task<TResponse> Get<TResponse>(this HttpClient httpClient, string endpoint, RequestQueryParams queryParams)
	{
		using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri: ValidateAndPrepareEndpoint(endpoint, queryParams));
		return await Send<TResponse>(httpClient, httpRequestMessage);
	}
	public static async Task Get(this HttpClient httpClient, string endpoint, RequestQueryParams queryParams)
	{
		using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri: ValidateAndPrepareEndpoint(endpoint, queryParams));
		await Send(httpClient, httpRequestMessage);
	}

	public static async Task<TResponse> Post<TInput, TResponse>(this HttpClient httpClient, string endpoint, TInput? input, RequestQueryParams queryParams)
	{
		using var content = new StringContent(JsonSerializer.Serialize(input, PostJsonSerializerOptions) ?? string.Empty, Encoding.UTF8, "application/json");
		using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri: ValidateAndPrepareEndpoint(endpoint, queryParams))
		{
			Content = content,
			Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
		};
		return await Send<TResponse>(httpClient, httpRequestMessage);
	}
	public static async Task Post<TInput>(this HttpClient httpClient, string endpoint, TInput? input, RequestQueryParams queryParams)
	{
		using var content = new StringContent(JsonSerializer.Serialize(input, PostJsonSerializerOptions) ?? string.Empty, Encoding.UTF8, "application/json");
		using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri: ValidateAndPrepareEndpoint(endpoint, queryParams))
		{
			Content = content,
			Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
		};
		await Send(httpClient, httpRequestMessage);
	}

	public static async Task<TResponse> Put<TInput, TResponse>(this HttpClient httpClient, string endpoint, TInput? input, RequestQueryParams queryParams)
	{
		using var content = new StringContent(JsonSerializer.Serialize(input, PostJsonSerializerOptions) ?? string.Empty, Encoding.UTF8, "application/json");
		using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri: ValidateAndPrepareEndpoint(endpoint, queryParams))
		{
			Content = content,
			Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
		};
		return await Send<TResponse>(httpClient, httpRequestMessage);
	}
	public static async Task Put<TInput>(this HttpClient httpClient, string endpoint, TInput? input, RequestQueryParams queryParams)
	{
		using var content = new StringContent(JsonSerializer.Serialize(input, PostJsonSerializerOptions) ?? string.Empty, Encoding.UTF8, "application/json");
		using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri: ValidateAndPrepareEndpoint(endpoint, queryParams))
		{
			Content = content,
			Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
		};
		await Send(httpClient, httpRequestMessage);
	}

	public static async Task<TResponse> Delete<TResponse>(this HttpClient httpClient, string endpoint, RequestQueryParams queryParams)
	{
		using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri: ValidateAndPrepareEndpoint(endpoint, queryParams));
		return await Send<TResponse>(httpClient, httpRequestMessage);
	}
	public static async Task Delete(this HttpClient httpClient, string endpoint, RequestQueryParams queryParams)
	{
		using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri: ValidateAndPrepareEndpoint(endpoint, queryParams));
		await Send(httpClient, httpRequestMessage);
	}

	private static async Task<TResponse> Send<TResponse>(HttpClient httpClient, HttpRequestMessage httpRequestMessage)
	{
		try
		{
			using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
			return (int)httpResponseMessage.StatusCode switch
			{
				>= 200 and <= 299 => JsonSerializer.Deserialize<TResponse>(await httpResponseMessage.Content.ReadAsStringAsync(), DeserializerJsonSerializerOptions)!,
				_ => throw await HandleErrorStatusCode(httpResponseMessage),
			};
		}
		catch (Exception ex) when (ex is not ChromaException)
		{
			throw new ChromaException(ex.Message, ex);
		}
	}
	private static async Task Send(HttpClient httpClient, HttpRequestMessage httpRequestMessage)
	{
		try
		{
			using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
			switch ((int)httpResponseMessage.StatusCode)
			{
				case >= 200 and <= 299:
					return;
				default:
					throw await HandleErrorStatusCode(httpResponseMessage);
			};
		}
		catch (Exception ex) when (ex is not ChromaException)
		{
			throw new ChromaException(ex.Message, ex);
		}
	}

	private static async Task<ChromaException> HandleErrorStatusCode(HttpResponseMessage httpResponseMessage)
	{
		return httpResponseMessage.StatusCode switch
		{
			HttpStatusCode.BadRequest
#if NETSTANDARD2_0
				or (HttpStatusCode)422
#else
				or HttpStatusCode.UnprocessableContent
#endif
				or HttpStatusCode.InternalServerError
				=> new ChromaException(ParseErrorMessageBody(await httpResponseMessage.Content.ReadAsStringAsync())),
			_ => new ChromaException($"Unexpected status code: {httpResponseMessage.StatusCode}."),
		};
	}

	private static string? ParseErrorMessageBody(string? errorMessageBody)
	{
		if (string.IsNullOrEmpty(errorMessageBody))
		{
			return null;
		}

		try
		{
			var deserialized = JsonSerializer.Deserialize<GeneralError>(errorMessageBody, DeserializerJsonSerializerOptions)!;
#if NETSTANDARD2_0
			var match = ParseErrorMessageBodyRegex.Match(deserialized?.Error ?? string.Empty);
#else
			var match = ParseErrorMessageBodyRegex().Match(deserialized?.Error ?? string.Empty);
#endif

			return match.Success
				? match.Groups["errorMessage"]?.Value
				: $"Couldn't identify the error message: {errorMessageBody}";
		}
		catch
		{
			return $"Couldn't parse the incoming error message body: {errorMessageBody}";
		}
	}

	private static List<string> PrepareQueryParams(string input)
	{
#if NETSTANDARD2_0
		return ParseErrorMessageBodyRegex.Matches(input)
			.Cast<Match>()
			.Select(x => x.Value)
			.ToList();
#else
		return PrepareQueryParamsRegex().Matches(input)
			.Select(x => x.Value)
			.ToList();
#endif
	}

#if NETSTANDARD2_0
	private static readonly Regex ParseErrorMessageBodyRegex = new(@"\('(?<errorMessage>.*)'\)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
#else
	[GeneratedRegex(@"\('(?<errorMessage>.*)'\)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
	private static partial Regex ParseErrorMessageBodyRegex();
#endif

#if NETSTANDARD2_0
		private static readonly Regex PrepareQueryParamsRegex = new(@"{[a-zA-Z0-9\-_]+}", RegexOptions.CultureInvariant | RegexOptions.Compiled);
#else
	[GeneratedRegex(@"{[a-zA-Z0-9\-_]+}", RegexOptions.CultureInvariant)]
	private static partial Regex PrepareQueryParamsRegex();
#endif

	private static string ValidateAndPrepareEndpoint(string endpoint, RequestQueryParams queryParams)
	{
		var queryArgs = PrepareQueryParams(endpoint);
		return queryArgs.Count > 0
        ? FormatRequestUri(endpoint, queryParams)
		: endpoint;
	}

	private static string FormatRequestUri(string endpoint, RequestQueryParams queryParams)
	{
		var formattedEndpoint = endpoint;
		foreach (var (key, value) in queryParams)
		{
			var urlEncodedQueryParam = Uri.EscapeDataString(value);
			formattedEndpoint = formattedEndpoint.Replace(key, urlEncodedQueryParam);
		}
		return formattedEndpoint;
	}

    public static async Task<T> GetFromJsonAsync<T>(this HttpClient client, string requestUri)
    {
        using (var response = await client.GetAsync(requestUri).ConfigureAwait(false))
        {
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<T>(stream).ConfigureAwait(false);
        }
    }
}
