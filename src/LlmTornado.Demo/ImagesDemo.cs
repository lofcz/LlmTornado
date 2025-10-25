using LlmTornado.Code;
using LlmTornado.Images;
using LlmTornado.Images.Models;
using LlmTornado.Images.Vendors.Google;
using LlmTornado.Models;
using PuppeteerSharp;
using System.Diagnostics;


namespace LlmTornado.Demo;

public class ImagesDemo : DemoBase
{
    public static async Task DisplayImage(ImageGenerationResult generatedImg)
    {
        if (generatedImg?.Data == null || generatedImg.Data.Count == 0)
        {
            Console.WriteLine("No image data available.");
            return;
        }

        string tempFile = $"{Path.GetTempFileName()}.jpg";

        try
        {
            // Check if we have base64 data
            byte[] imageBytes;
            if (!string.IsNullOrEmpty(generatedImg.Data[0].Base64))
            {
                imageBytes = Convert.FromBase64String(generatedImg.Data[0].Base64);
                await File.WriteAllBytesAsync(tempFile, imageBytes);
            }
            // Check if we have a URL instead
            else if (!string.IsNullOrEmpty(generatedImg.Data[0].Url))
            {
                Console.WriteLine($"Downloading image from URL: {generatedImg.Data[0].Url}");
                
                using (HttpClient httpClient = new HttpClient())
                {
                    imageBytes = await httpClient.GetByteArrayAsync(generatedImg.Data[0].Url);
                    await File.WriteAllBytesAsync(tempFile, imageBytes);
                }
                
                Console.WriteLine("Image downloaded successfully.");
            }
            else
            {
                Console.WriteLine("No base64 data or URL available in the image result.");
                return;
            }

            // Display image using chafa if available
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
                    Console.WriteLine($"Failed to display image with chafa: {e.Message}");
                }
            }
            else
            {
                Console.WriteLine("chafa not found. Install it to display images in the terminal.");
                Console.WriteLine($"Image saved temporarily to: {tempFile}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error processing image: {e.Message}");
        }
        finally
        {
            // Clean up temporary file
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
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
        ImageGenerationResult? generatedImg = await Program.Connect().ImageGenerations.CreateImage(new ImageGenerationRequest("a cute cat", responseFormat: TornadoImageResponseFormats.Base64, model: ImageModel.Google.Imagen.V4Generate001)
        {
            VendorExtensions = new ImageGenerationRequestVendorExtensions(new ImageGenerationRequestGoogleExtensions
            {
                MimeType = ImageGenerationRequestGoogleExtensionsMimeTypes.Png
            })
        });
        
        await DisplayImage(generatedImg);
    }
   
    [TornadoTest]
    [Flaky("expensive")]
    public static async Task EditLogoGpt1()
    {
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/logo.png");
        string base64 = Convert.ToBase64String(bytes);
        var request = new ImageEditRequest("Can you Redesign our logo make it more modern and sophisticated")
        {
            Quality = TornadoImageQualities.High,
            NumOfImages = 2,
            Model = ImageModel.OpenAi.Gpt.V1,
            Image = new TornadoInputFile(base64, "image/png")
        };

        ImageGenerationResult? edited = await Program.Connect().ImageEdit.EditImage(request);

        await SaveImages("logo", edited);
    }

    public static async Task SaveImages(string imageName, ImageGenerationResult generatedImg)
    {
        if (generatedImg?.Data == null || generatedImg.Data.Count == 0)
        {
            Console.WriteLine("No image data available.");
            return;
        }

        foreach(var imgData in generatedImg.Data)
        {
            byte[] imageBytes;
            if (!string.IsNullOrEmpty(imgData.Base64))
            {
                imageBytes = Convert.FromBase64String(imgData.Base64);
            }
            else if (!string.IsNullOrEmpty(imgData.Url))
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    imageBytes = await httpClient.GetByteArrayAsync(imgData.Url);
                }
            }
            else
            {
                Console.WriteLine("No base64 data or URL available in the image result.");
                continue;
            }
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), $"{imageName}_{Guid.NewGuid()}.png");
            await File.WriteAllBytesAsync(filePath, imageBytes);
            Console.WriteLine($"Image saved to: {filePath}");
        }
    }
}