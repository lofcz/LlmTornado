using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OpenAiNg
{
	/// <summary>
	/// Represents authentication to the OpenAI-compatible API endpoint
	/// </summary>
	public class ApiAuthentication
	{
		/// <summary>
		/// The API key, required to access the API endpoint. Set to null if no API key is required (eg. for locally hosted models)
		/// </summary>
		public string? ApiKey { get; set; }
		/// <summary>
		/// The Organization ID to count API requests against. This can be found at https://beta.openai.com/account/org-settings.
		/// </summary>
		public string? Organization { get; set; }

		/// <summary>
		/// Allows implicit casting from a string, so that a simple string API key can be provided in place of an instance of <see cref="ApiAuthentication"/>
		/// </summary>
		/// <param name="key">The API key to convert into a <see cref="ApiAuthentication"/>.</param>
		public static implicit operator ApiAuthentication(string? key)
		{
			return new ApiAuthentication(key);
		}

		/// <summary>
		/// Instantiates a new Authentication object with the given <paramref name="apiKey"/>, which may be <see langword="null"/>.
		/// </summary>
		/// <param name="apiKey">The API key, required to access the API endpoint.</param>
		public ApiAuthentication(string? apiKey)
		{
			ApiKey = apiKey;
		}

		/// <summary>
		/// Instantiates a new Authentication object with the given <paramref name="apiKey"/>, which may be <see langword="null"/>.  For users who belong to multiple organizations, you can specify which organization is used. Usage from these API requests will count against the specified organization's subscription quota.
		/// </summary>
		/// <param name="apiKey">The API key, required to access the API endpoint.</param>
		/// <param name="organization">The Organization ID to count API requests against.  This can be found at https://beta.openai.com/account/org-settings.</param>
		public ApiAuthentication(string apiKey, string organization)
		{
			ApiKey = apiKey;
			Organization = organization;
		}
	}
}
