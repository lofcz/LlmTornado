using LlmTornado.Agents.VectorDatabases.ChromaDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;


namespace LlmTornado.Agents.VectorDatabases.ChromaDB;

public class ApiV1ToV2DelegatingHandler : DelegatingHandler
{
    public class AuthIdentityResponse
    {
        public string User_id { get; set; } = "";
        public string Tenant { get; set; } = "default_tenant";
        public List<string> Databases { get; set; } = ["default_database"];
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var baseAddress = new Uri($"{request.RequestUri.Scheme}://{request.RequestUri.Host}:{request.RequestUri.Port}/api/v2/");

        var queries = HttpUtility.ParseQueryString(request.RequestUri.Query);

        var tenant = queries["tenant"];
        queries.Remove("tenant");

        var database = queries["database"];
        queries.Remove("database");

        if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(database))
        {
            var httpClient = new HttpClient
            {
                BaseAddress = baseAddress
            };
            
            var authIdentityResponse = await httpClient.GetFromJsonAsync<AuthIdentityResponse>("auth/identity");

            tenant ??= authIdentityResponse?.Tenant;
            database ??= authIdentityResponse?.Databases.FirstOrDefault();
        }

        var uriBuilder = new UriBuilder(request.RequestUri)
        {
            Query = queries.ToString()
        };

        request.RequestUri = new Uri(uriBuilder.Uri.ToString().Replace(baseAddress.ToString(), $"{baseAddress}tenants/{tenant}/databases/{database}/"));

        var response = await base.SendAsync(request, cancellationToken);

        if (request.RequestUri.AbsolutePath.EndsWith("/query") || request.RequestUri.AbsolutePath.EndsWith("/get"))
        {
            var originalContent = await response.Content.ReadAsStringAsync();

            var payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(originalContent) ?? [];

            payload["data"] = originalContent;

            var modifiedContent = JsonSerializer.Serialize(payload);

            response.Content = new StringContent(modifiedContent, Encoding.UTF8, "application/json");
        }

        return response;
    }
}
