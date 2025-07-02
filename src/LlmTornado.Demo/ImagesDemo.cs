using System.Diagnostics;
using LlmTornado.Code;
using LlmTornado.Images;
using LlmTornado.Images.Models;
using LlmTornado.Images.Vendors.Google;
using LlmTornado.Models;


namespace LlmTornado.Demo;

public class ImagesDemo : DemoBase
{
    static async Task DisplayImage(ImageGenerationResult generatedImg)
    {
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
    public static async Task GenerateGrok2Image()
    {
        ImageGenerationResult? generatedImg = await Program.Connect().ImageGenerations.CreateImage(new ImageGenerationRequest
        {
            Prompt = "a cute cat",
            ResponseFormat = TornadoImageResponseFormats.Base64,
            Model = ImageModel.XAi.Grok.V2241212
        });

        await DisplayImage(generatedImg);
    }
    
    [TornadoTest]
    [Flaky("expensive")]
    public static async Task GenerateGpt1()
    {
        ImageGenerationResult? generatedImg = await Program.Connect().ImageGenerations.CreateImage(new ImageGenerationRequest("a cute cat", quality: TornadoImageQualities.Medium, model: ImageModel.OpenAi.Gpt.V1)
        {
            Background = ImageBackgroundTypes.Transparent,
            Moderation = ImageModerationTypes.Low
        });
        
        await DisplayImage(generatedImg);
    }
    
    [TornadoTest]
    [Flaky("expensive")]
    public static async Task EditGpt1()
    {
        ImageGenerationResult? generatedImg = await Program.Connect().ImageGenerations.CreateImage(new ImageGenerationRequest("a cute cat", quality: TornadoImageQualities.Medium, model: ImageModel.OpenAi.Gpt.V1)
        {
            Background = ImageBackgroundTypes.Transparent,
            Moderation = ImageModerationTypes.Low
        });
        
        await DisplayImage(generatedImg);

        ImageGenerationResult? edited = await Program.Connect().ImageEdit.EditImage(new ImageEditRequest("make this cat look more dangerous")
        {
            Quality = TornadoImageQualities.Medium,
            Model = ImageModel.OpenAi.Gpt.V1,
            Image = new TornadoInputFile(generatedImg.Data[0].Base64, "image/png")
        });
        
        await DisplayImage(edited);
    }
    
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
        await DisplayImage(generatedImg);
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
        
        await DisplayImage(generatedImg);
    }
    
    [TornadoTest]
    [Flaky("expensive")]
    public static async Task GenerateImagen4Preview()
    {
        ImageGenerationResult? generatedImg = await Program.Connect().ImageGenerations.CreateImage(new ImageGenerationRequest("a cute cat", responseFormat: TornadoImageResponseFormats.Base64, model: ImageModel.Google.ImagenPreview.V4Preview250606)
        {
            VendorExtensions = new ImageGenerationRequestVendorExtensions(new ImageGenerationRequestGoogleExtensions
            {
                MimeType = ImageGenerationRequestGoogleExtensionsMimeTypes.Jpeg,
                CompressionQuality = 90
            })
        });
        
        await DisplayImage(generatedImg);
    }
}