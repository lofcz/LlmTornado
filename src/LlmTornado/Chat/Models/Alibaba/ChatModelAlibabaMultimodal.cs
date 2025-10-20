using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Alibaba;

/// <summary>
/// Alibaba multimodal models - text, images, audio, and video processing.
/// </summary>
public class ChatModelAlibabaMultimodal : IVendorModelClassProvider
{
    /// <summary>
    /// Qwen-Image-Plus - First image generation foundation model with complex text rendering
    /// </summary>
    public static readonly ChatModel ModelQwenImagePlus = new ChatModel("qwen-image-plus", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenImagePlus"/>
    /// </summary>
    public readonly ChatModel QwenImagePlus = ModelQwenImagePlus;

    /// <summary>
    /// Qwen-Image - First image generation foundation model
    /// </summary>
    public static readonly ChatModel ModelQwenImage = new ChatModel("qwen-image", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenImage"/>
    /// </summary>
    public readonly ChatModel QwenImage = ModelQwenImage;

    /// <summary>
    /// Qwen-Image-Edit - First Tongyi Qwen image editing model
    /// </summary>
    public static readonly ChatModel ModelQwenImageEdit = new ChatModel("qwen-image-edit", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenImageEdit"/>
    /// </summary>
    public readonly ChatModel QwenImageEdit = ModelQwenImageEdit;

    /// <summary>
    /// Qwen3-Omni-Flash - Multimodal large-scale model with Thinker-Talker MoE architecture
    /// </summary>
    public static readonly ChatModel ModelQwen3OmniFlash = new ChatModel("qwen3-omni-flash", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3OmniFlash"/>
    /// </summary>
    public readonly ChatModel Qwen3OmniFlash = ModelQwen3OmniFlash;

    /// <summary>
    /// Qwen3-Omni-Flash-Realtime - Real-time version of Qwen3-Omni-Flash
    /// </summary>
    public static readonly ChatModel ModelQwen3OmniFlashRealtime = new ChatModel("qwen3-omni-flash-realtime", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3OmniFlashRealtime"/>
    /// </summary>
    public readonly ChatModel Qwen3OmniFlashRealtime = ModelQwen3OmniFlashRealtime;

    /// <summary>
    /// Fun-ASR - Next-generation end-to-end speech recognition model
    /// </summary>
    public static readonly ChatModel ModelFunAsr = new ChatModel("fun-asr", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelFunAsr"/>
    /// </summary>
    public readonly ChatModel FunAsr = ModelFunAsr;

    /// <summary>
    /// Qwen3-ASR-Flash - Highly accurate multilingual speech recognition model
    /// </summary>
    public static readonly ChatModel ModelQwen3AsrFlash = new ChatModel("qwen3-asr-flash", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3AsrFlash"/>
    /// </summary>
    public readonly ChatModel Qwen3AsrFlash = ModelQwen3AsrFlash;

    /// <summary>
    /// Qwen3-LiveTranslate-Flash-Realtime - Real-time multilingual simultaneous audio/video interpretation
    /// </summary>
    public static readonly ChatModel ModelQwen3LiveTranslateFlashRealtime = new ChatModel("qwen3-livetranslate-flash-realtime", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3LiveTranslateFlashRealtime"/>
    /// </summary>
    public readonly ChatModel Qwen3LiveTranslateFlashRealtime = ModelQwen3LiveTranslateFlashRealtime;

    /// <summary>
    /// Qwen3-TTS-Flash - Latest offline text-to-speech foundation model
    /// </summary>
    public static readonly ChatModel ModelQwen3TtsFlash = new ChatModel("qwen3-tts-flash", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3TtsFlash"/>
    /// </summary>
    public readonly ChatModel Qwen3TtsFlash = ModelQwen3TtsFlash;

    /// <summary>
    /// Qwen3-TTS-Flash-Realtime - Latest real-time speech synthesis foundation model
    /// </summary>
    public static readonly ChatModel ModelQwen3TtsFlashRealtime = new ChatModel("qwen3-tts-flash-realtime", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3TtsFlashRealtime"/>
    /// </summary>
    public readonly ChatModel Qwen3TtsFlashRealtime = ModelQwen3TtsFlashRealtime;

    /// <summary>
    /// Qwen3-LiveTranslate-Flash-Realtime-2025-09-22 - Snapshot from September 22, 2025
    /// </summary>
    public static readonly ChatModel ModelQwen3LiveTranslateFlashRealtime20250922 = new ChatModel("qwen3-livetranslate-flash-realtime-2025-09-22", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3LiveTranslateFlashRealtime20250922"/>
    /// </summary>
    public readonly ChatModel Qwen3LiveTranslateFlashRealtime20250922 = ModelQwen3LiveTranslateFlashRealtime20250922;

    /// <summary>
    /// Fun-ASR-2025-08-25 - Snapshot from August 25, 2025
    /// </summary>
    public static readonly ChatModel ModelFunAsr20250825 = new ChatModel("fun-asr-2025-08-25", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelFunAsr20250825"/>
    /// </summary>
    public readonly ChatModel FunAsr20250825 = ModelFunAsr20250825;

    /// <summary>
    /// Qwen3-TTS-Flash-2025-09-18 - Snapshot from September 18, 2025
    /// </summary>
    public static readonly ChatModel ModelQwen3TtsFlash20250918 = new ChatModel("qwen3-tts-flash-2025-09-18", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3TtsFlash20250918"/>
    /// </summary>
    public readonly ChatModel Qwen3TtsFlash20250918 = ModelQwen3TtsFlash20250918;

    /// <summary>
    /// Qwen3-TTS-Flash-Realtime-2025-09-18 - Snapshot from September 18, 2025
    /// </summary>
    public static readonly ChatModel ModelQwen3TtsFlashRealtime20250918 = new ChatModel("qwen3-tts-flash-realtime-2025-09-18", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3TtsFlashRealtime20250918"/>
    /// </summary>
    public readonly ChatModel Qwen3TtsFlashRealtime20250918 = ModelQwen3TtsFlashRealtime20250918;

    /// <summary>
    /// Qwen3-Omni-Flash-2025-09-15 - Snapshot from September 15, 2025
    /// </summary>
    public static readonly ChatModel ModelQwen3OmniFlash20250915 = new ChatModel("qwen3-omni-flash-2025-09-15", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3OmniFlash20250915"/>
    /// </summary>
    public readonly ChatModel Qwen3OmniFlash20250915 = ModelQwen3OmniFlash20250915;

    /// <summary>
    /// Qwen3-Omni-Flash-Realtime-2025-09-15 - Snapshot from September 15, 2025
    /// </summary>
    public static readonly ChatModel ModelQwen3OmniFlashRealtime20250915 = new ChatModel("qwen3-omni-flash-realtime-2025-09-15", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3OmniFlashRealtime20250915"/>
    /// </summary>
    public readonly ChatModel Qwen3OmniFlashRealtime20250915 = ModelQwen3OmniFlashRealtime20250915;

    /// <summary>
    /// Qwen3-ASR-Flash-2025-09-08 - Snapshot from September 8, 2025
    /// </summary>
    public static readonly ChatModel ModelQwen3AsrFlash20250908 = new ChatModel("qwen3-asr-flash-2025-09-08", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3AsrFlash20250908"/>
    /// </summary>
    public readonly ChatModel Qwen3AsrFlash20250908 = ModelQwen3AsrFlash20250908;

    /// <summary>
    /// Qwen3-Omni-30b-a3b-Captioner - Fine-grained audio analysis model
    /// </summary>
    public static readonly ChatModel ModelQwen3Omni30bA3bCaptioner = new ChatModel("qwen3-omni-30b-a3b-captioner", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Omni30bA3bCaptioner"/>
    /// </summary>
    public readonly ChatModel Qwen3Omni30bA3bCaptioner = ModelQwen3Omni30bA3bCaptioner;

    /// <summary>
    /// Qwen2.5-Omni-7B - Multimodal understanding and generation large model
    /// </summary>
    public static readonly ChatModel ModelQwen2_5Omni7B = new ChatModel("qwen2.5-omni-7b", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2_5Omni7B"/>
    /// </summary>
    public readonly ChatModel Qwen2_5Omni7B = ModelQwen2_5Omni7B;

    /// <summary>
    /// All known multimodal models from Alibaba.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelQwenImagePlus, ModelQwenImage, ModelQwenImageEdit, ModelQwen3OmniFlash, ModelQwen3OmniFlashRealtime,
        ModelFunAsr, ModelQwen3AsrFlash, ModelQwen3LiveTranslateFlashRealtime, ModelQwen3TtsFlash, ModelQwen3TtsFlashRealtime,
        ModelQwen3LiveTranslateFlashRealtime20250922, ModelFunAsr20250825, ModelQwen3TtsFlash20250918, ModelQwen3TtsFlashRealtime20250918,
        ModelQwen3OmniFlash20250915, ModelQwen3OmniFlashRealtime20250915, ModelQwen3AsrFlash20250908, ModelQwen3Omni30bA3bCaptioner,
        ModelQwen2_5Omni7B
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAlibabaMultimodal()
    {
    }
}
