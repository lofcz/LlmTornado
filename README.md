[![OpenAI](https://badgen.net/nuget/v/OpenAiNg)](https://www.nuget.org/packages/OpenAiNg/)

# .NET SDK for accessing the OpenAI Compatible APIs such as GPT-4 (Vision), GPT-3.5-instruct, and DALL-E 3

OpenAiNextGeneration is a simple .NET library to use with various OpenAI compatible providers, such
as [OpenAI](https://platform.openai.com/docs/api-reference), [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service), and [KoboldCpp](https://github.com/LostRuins/koboldcpp/releases) (
v1.45.2+). Supports features such as function calling in conjunction with streaming, caches its own `HttpClient`s.

Supported features compared to [OpenAI-API-dotnet](https://github.com/OkGoDoIt/OpenAI-API-dotnet):
- Supports new models.
- Parallel function calling.
- Improved memory usage, and function calling in conjunction with streaming.
- Manages its pool of HttpClients.
- Improved credentials passing.
- Supports OpenAI-compatible API providers, such as KoboldCpp.
- Improved Azure OpenAI integration.
- Nullability annotations.
- Actively maintained, [backed by a company I work for](https://www.scio.cz/).

Features scheduled for open-sourcing:

- High-level plugin API.
- Approximate token counting for streaming & function calling.

## ⚡Getting Started

Install the library via NuGet:

```
Install-Package OpenAiNg
```

## 🔮 Quick Inference

```csharp
var api = new OpenAiNg.OpenAiApi("YOUR_API_KEY");
var result = await api.Completions.GetCompletion("One Two Three One Two");
Console.WriteLine(result);
// should print something starting with "Three"
```

## 📖 Readme

* [Requirements](#requirements)
* [Installation](#install-from-nuget)
* [Authentication](#authentication)
* [Additonal Documentation](#documentation)
* [License](#license)

## Requirements

Unlike the original library, OpenAiNg supports only .NET Core >= 6.0, if you need .NET Standard 2.0 /.NET Framework
support, please use [OpenAI-API-DotNet](https://github.com/OkGoDoIt/OpenAI-API-dotnet).

## Getting started

### Install from NuGet

Install package [`OpenAiNg` from Nuget](https://www.nuget.org/packages/OpenAiNg/). Here's how via the command line:

```powershell
Install-Package OpenAiNg
```

### Authentication

Pass keys directly to `ApiAuthentication(string key)` constructor.

You use the `APIAuthentication` when you initialize the API as shown:

```csharp
// for example
var api = new OpenAiApi("YOUR_API_KEY"); // shorthand
// or
var api = new OpenAiApi(new APIAuthentication("YOUR_API_KEY")); // create object manually
```

You may optionally include an OpenAiOrganization if you have multiple Organizations under one account.

```csharp
// for example
var api = new OpenAiApi(new ApiAuthentication("YOUR_API_KEY", "org-yourOrgHere"));
```

### Audio Transcription

You can transcribe an audio file with OpenAiNg using the following code snippet:

```csharp
// Create the audioFile object
AudioFile audioFile = new()
{
    File = fileStream,       // your FileStream instance here
    ContentType = "audio/ogg",  // content type may vary depending on the file type
    Name = Path.GetFileName(filePath) // name of the file
};

// Create the transcriptionRequest object
TranscriptionRequest transcriptionRequest = new()
{
    File = audioFile, // the audio file to be transcribed
    Model = OpenAiNg.Models.Model.Whisper_1, // the model to be used for transcription
};

// Async call to create transcriptions
TranscriptionVerboseJsonResult? result =
    await api.Audio.CreateTranscriptionAsync(transcriptionRequest);

// Get the transcript text from the result
return result.Text;
```

### Create a Speech

Here is an example of how you can generate speech from a given text.

```csharp
SpeechTtsResult? ttsResult = await api.Audio.CreateSpeechAsync(new SpeechRequest
{
    Input = text,  // Text that need to convert into speech
    Model = OpenAiNg.Models.Model.TTS_1_HD,  // Model that will be used for text-to-speech conversation
    Voice = SpeechVoice.Nova,  // OpenAi's Nova voice will be used for speech output
    ResponseFormat = SpeechResponseFormat.Mp3,  // Output will be in Mp3 format
});

string path = Path.Combine(Path.GetTempPath(), // getting directory path for temp files
    Path.ChangeExtension(Path.GetTempFileName(), "mp3"));  // Generating a unique temp file and changing its extension to .mp3

// Save the audio and dispose the source stream
await ttsResult.SaveAndDispose(path);
```

## Documentation

Every single class, method, and property has extensive XML documentation, so it should show up automatically in
IntelliSense. That combined with the official OpenAI documentation should be enough to get started. Feel free to open an
issue here if you have any questions.

## License

This library is licensed under [MIT license](https://github.com/lofcz/OpenAiNg/blob/master/LICENSE).
