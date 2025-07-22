using LlmTornado.Code;
using LlmTornado.Uploads;

namespace LlmTornado.Demo;

public class UploadsDemo : DemoBase
{
    [TornadoTest]
    public static async Task CreateUpload()
    {
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/catBoi.jpg");

        Upload upload = await Program.Connect().Uploads.CreateUpload(new CreateUploadRequest
        {
            Bytes = bytes.Length,
            Filename = "catBoi.jpg",
            MimeType = "image/jpeg",
            Purpose = UploadPurpose.UserData
        });

        UploadPart part = await Program.Connect().Uploads.AddUploadPart(upload.Id, bytes);

        Upload completed = await Program.Connect().Uploads.CompleteUpload(upload.Id, new CompleteUploadRequest
        {
            PartIds = [part.Id]
        });
        
        Assert.That(upload.Id, Is.NotNull);
        Assert.That(completed.File.Id, Is.NotNull);
        
        Console.WriteLine(completed.File.Id);
    }
    
    [TornadoTest]
    public static async Task CreateUploadAuto()
    {
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/catBoi.jpg");

        Upload upload = await Program.Connect().Uploads.CreateUploadAutoChunk(new CreateUploadRequest
        {
            Filename = "catBoi.jpg",
            MimeType = "image/jpeg",
            Purpose = UploadPurpose.UserData
        }, bytes, new UploadOptions
        {
            DegreeOfParallelism = 2,
            Progress = new Progress<UploadProgress>(val =>
            {
                Console.WriteLine($"Progress: {val.ProgressPercent}"); 
            })
        });


        Assert.That(upload.File.Id, Is.NotNull);
        Console.WriteLine(upload.File.Id);
    }
    
    [TornadoTest]
    public static async Task CancelUpload()
    {
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/catBoi.jpg");

        Upload upload = await Program.Connect().Uploads.CreateUpload(new CreateUploadRequest
        {
            Bytes = bytes.Length,
            Filename = "catBoi.jpg",
            MimeType = "image/jpeg",
            Purpose = UploadPurpose.UserData
        });
        
        Upload cancelled = await Program.Connect().Uploads.CancelUpload(upload.Id);
        
        Assert.That(upload.Id, Is.NotNull);
        Assert.That(cancelled.Status, Is.EqualTo(UploadStatus.Cancelled));
        Console.WriteLine(cancelled.Status);
    }
}