using System.Net.Http;
using System.Threading.Tasks;

namespace BlazorWorker.Demo.Shared
{
    public class WebCallerService
    {
        public HttpClient HttpClient { get; }

        public WebCallerService(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public async Task<string> CallGitHubApi()
        {
            var github = await HttpClient.GetAsync("https://api.github.com");
            return await github.Content.ReadAsStringAsync();
        }
    }
}
