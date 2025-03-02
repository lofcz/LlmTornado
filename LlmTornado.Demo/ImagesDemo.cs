using System.Diagnostics;
using LlmTornado.Images;
using LlmTornado.Images.Models;
using LlmTornado.Images.Vendors.Google;
using LlmTornado.Models;

namespace LlmTornado.Demo;

public class ImagesDemo
{
    [TornadoTest]
    [Flaky("expensive")]
    public static async Task GenerateDalle3()
    {
        ImageGenerationResult? generatedImg = await Program.Connect().ImageGenerations.CreateImage(new ImageGenerationRequest("a cute cat", quality: TornadoImageQualities.Hd, responseFormat: TornadoImageResponseFormats.Url, model: ImageModel.OpenAi.Dalle.V3));
        Console.WriteLine(generatedImg?.Data?[0].Url);
    }
    
    [TornadoTest]
    [Flaky("expensive")]
    public static async Task GenerateDalle3Base64()
    {
        ImageGenerationResult? generatedImg = await Program.Connect().ImageGenerations.CreateImage(new ImageGenerationRequest("a cute cat", quality: TornadoImageQualities.Hd, responseFormat: TornadoImageResponseFormats.Base64, model: ImageModel.OpenAi.Dalle.V3));
       
        byte[] imageBytes = Convert.FromBase64String(generatedImg.Data[0].Base64);
        string tempFile = $"{Path.GetTempFileName()}.jpg";
        await File.WriteAllBytesAsync(tempFile, imageBytes);

        if (await Helpers.ProgramExists("chafa"))
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "chafa";
                process.StartInfo.Arguments = $"{tempFile}";
                process.StartInfo.UseShellExecute = false;
                process.Start();
                await process.WaitForExitAsync();
            }
            catch (Exception e)
            {
                
            }
        }
        
        File.Delete(tempFile);
    }
    
    [TornadoTest]
    [Flaky("expensive")]
    public static async Task GenerateImagen3()
    {
        ImageGenerationResult? generatedImg = await Program.Connect().ImageGenerations.CreateImage(new ImageGenerationRequest("a cute cat", responseFormat: TornadoImageResponseFormats.Base64, model: ImageModel.Google.Imagen.V3Generate002)
        {
            VendorExtensions = new ImageGenerationRequestVendorExtensions(new ImageGenerationRequestGoogleExtensions
            {
                MimeType = ImageGenerationRequestGoogleExtensionsMimeTypes.Jpeg,
                CompressionQuality = 90
            })
        });
        
        byte[] imageBytes = Convert.FromBase64String(generatedImg.Data[0].Base64);
        string tempFile = $"{Path.GetTempFileName()}.jpg";
        await File.WriteAllBytesAsync(tempFile, imageBytes);

        if (await Helpers.ProgramExists("chafa"))
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "chafa";
                process.StartInfo.Arguments = $"{tempFile}";
                process.StartInfo.UseShellExecute = false;
                process.Start();
                await process.WaitForExitAsync();
            }
            catch (Exception e)
            {
                
            }
        }
        
        File.Delete(tempFile);
    }
}