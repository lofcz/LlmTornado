using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Microsoft.OpenApi.YamlReader;

namespace LlmTornado.OpenApiGenerator;

class Program
{
    static async Task Main(string[] args)
    {
        await using FileStream fs = new FileStream("openai.yml", FileMode.Open);
        OpenApiYamlReader openApiDocument = new OpenApiYamlReader();
        ReadResult result =  await openApiDocument.ReadAsync(fs, new Uri("https://github.com/microsoft/OpenAPI.NET"), new OpenApiReaderSettings
        {
            
        });

        foreach (KeyValuePair<string, IOpenApiPathItem> path in result.Document.Paths)
        {

        }
        
        int z = 0;
    }
}