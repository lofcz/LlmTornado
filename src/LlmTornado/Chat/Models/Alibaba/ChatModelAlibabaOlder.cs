using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Chat.Models.Alibaba;

/// <summary>
/// Alibaba older models - legacy models no longer updated, may be deprecated.
/// </summary>
public class ChatModelAlibabaOlder : IVendorModelClassProvider
{
    /// <summary>
    /// Qwen-Max - Supports hundreds of billions parameters, rolling updates
    /// </summary>
    public static readonly ChatModel ModelQwenMax = new ChatModel("qwen-max", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenMax"/>
    /// </summary>
    public readonly ChatModel QwenMax = ModelQwenMax;

    /// <summary>
    /// Qwen-Omni-Turbo - Brand-new multimodal understanding and generation large model
    /// </summary>
    public static readonly ChatModel ModelQwenOmniTurbo = new ChatModel("qwen-omni-turbo", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenOmniTurbo"/>
    /// </summary>
    public readonly ChatModel QwenOmniTurbo = ModelQwenOmniTurbo;

    /// <summary>
    /// Qwen-Omni-Turbo-Realtime - Real-time version for audio interaction scenarios
    /// </summary>
    public static readonly ChatModel ModelQwenOmniTurboRealtime = new ChatModel("qwen-omni-turbo-realtime", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenOmniTurboRealtime"/>
    /// </summary>
    public readonly ChatModel QwenOmniTurboRealtime = ModelQwenOmniTurboRealtime;

    /// <summary>
    /// Qwen-Omni-Turbo-Latest - Dynamically updated version
    /// </summary>
    public static readonly ChatModel ModelQwenOmniTurboLatest = new ChatModel("qwen-omni-turbo-latest", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenOmniTurboLatest"/>
    /// </summary>
    public readonly ChatModel QwenOmniTurboLatest = ModelQwenOmniTurboLatest;

    /// <summary>
    /// Qwen-Omni-Turbo-Realtime-Latest - Dynamically updated real-time version
    /// </summary>
    public static readonly ChatModel ModelQwenOmniTurboRealtimeLatest = new ChatModel("qwen-omni-turbo-realtime-latest", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenOmniTurboRealtimeLatest"/>
    /// </summary>
    public readonly ChatModel QwenOmniTurboRealtimeLatest = ModelQwenOmniTurboRealtimeLatest;

    /// <summary>
    /// Qwen-Omni-Turbo-2025-03-26 - Snapshot from March 26, 2025
    /// </summary>
    public static readonly ChatModel ModelQwenOmniTurbo20250326 = new ChatModel("qwen-omni-turbo-2025-03-26", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenOmniTurbo20250326"/>
    /// </summary>
    public readonly ChatModel QwenOmniTurbo20250326 = ModelQwenOmniTurbo20250326;

    /// <summary>
    /// Qwen-Omni-Turbo-Realtime-2025-05-08 - Snapshot from May 8, 2025
    /// </summary>
    public static readonly ChatModel ModelQwenOmniTurboRealtime20250508 = new ChatModel("qwen-omni-turbo-realtime-2025-05-08", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenOmniTurboRealtime20250508"/>
    /// </summary>
    public readonly ChatModel QwenOmniTurboRealtime20250508 = ModelQwenOmniTurboRealtime20250508;

    /// <summary>
    /// Qwen-Max-Latest - Dynamically updated most effective model
    /// </summary>
    public static readonly ChatModel ModelQwenMaxLatest = new ChatModel("qwen-max-latest", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenMaxLatest"/>
    /// </summary>
    public readonly ChatModel QwenMaxLatest = ModelQwenMaxLatest;

    /// <summary>
    /// Qwen-Max-2025-01-25 - Snapshot from January 25, 2025
    /// </summary>
    public static readonly ChatModel ModelQwenMax20250125 = new ChatModel("qwen-max-2025-01-25", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenMax20250125"/>
    /// </summary>
    public readonly ChatModel QwenMax20250125 = ModelQwenMax20250125;

    /// <summary>
    /// Wan2.1-T2I-Turbo - Text-to-Image Turbo version with faster generation
    /// </summary>
    public static readonly ChatModel ModelWan2_1T2ITurbo = new ChatModel("wan2.1-t2i-turbo", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelWan2_1T2ITurbo"/>
    /// </summary>
    public readonly ChatModel Wan2_1T2ITurbo = ModelWan2_1T2ITurbo;

    /// <summary>
    /// Wan2.1-T2I-Plus - Text-to-Image Plus version with more details
    /// </summary>
    public static readonly ChatModel ModelWan2_1T2IPlus = new ChatModel("wan2.1-t2i-plus", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelWan2_1T2IPlus"/>
    /// </summary>
    public readonly ChatModel Wan2_1T2IPlus = ModelWan2_1T2IPlus;

    /// <summary>
    /// Wan2.1-T2V-Plus - Text-to-Video Plus version with better video quality
    /// </summary>
    public static readonly ChatModel ModelWan2_1T2VPlus = new ChatModel("wan2.1-t2v-plus", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelWan2_1T2VPlus"/>
    /// </summary>
    public readonly ChatModel Wan2_1T2VPlus = ModelWan2_1T2VPlus;

    /// <summary>
    /// Wan2.1-T2V-Turbo - Text-to-Video Turbo version with faster generation
    /// </summary>
    public static readonly ChatModel ModelWan2_1T2VTurbo = new ChatModel("wan2.1-t2v-turbo", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelWan2_1T2VTurbo"/>
    /// </summary>
    public readonly ChatModel Wan2_1T2VTurbo = ModelWan2_1T2VTurbo;

    /// <summary>
    /// Wan2.1-I2V-Plus - Image-to-Video Plus version with better video quality
    /// </summary>
    public static readonly ChatModel ModelWan2_1I2VPlus = new ChatModel("wan2.1-i2v-plus", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelWan2_1I2VPlus"/>
    /// </summary>
    public readonly ChatModel Wan2_1I2VPlus = ModelWan2_1I2VPlus;

    /// <summary>
    /// Wan2.1-I2V-Turbo - Image-to-Video Turbo version with faster generation
    /// </summary>
    public static readonly ChatModel ModelWan2_1I2VTurbo = new ChatModel("wan2.1-i2v-turbo", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelWan2_1I2VTurbo"/>
    /// </summary>
    public readonly ChatModel Wan2_1I2VTurbo = ModelWan2_1I2VTurbo;

    /// <summary>
    /// Qwen-Turbo - Ultra-large language model with extended context length
    /// </summary>
    public static readonly ChatModel ModelQwenTurbo = new ChatModel("qwen-turbo", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenTurbo"/>
    /// </summary>
    public readonly ChatModel QwenTurbo = ModelQwenTurbo;

    /// <summary>
    /// Qwen-QwQ-Plus - Enhanced reasoning model with RL improvements
    /// </summary>
    public static readonly ChatModel ModelQwenQwqPlus = new ChatModel("qwen-qwq-plus", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenQwqPlus"/>
    /// </summary>
    public readonly ChatModel QwenQwqPlus = ModelQwenQwqPlus;

    /// <summary>
    /// Qwen-Turbo-Latest - Dynamically updated fastest and most cost-effective model
    /// </summary>
    public static readonly ChatModel ModelQwenTurboLatest = new ChatModel("qwen-turbo-latest", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenTurboLatest"/>
    /// </summary>
    public readonly ChatModel QwenTurboLatest = ModelQwenTurboLatest;

    /// <summary>
    /// Qwen-Turbo-2025-04-28 - Snapshot from April 28, 2025
    /// </summary>
    public static readonly ChatModel ModelQwenTurbo20250428 = new ChatModel("qwen-turbo-2025-04-28", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenTurbo20250428"/>
    /// </summary>
    public readonly ChatModel QwenTurbo20250428 = ModelQwenTurbo20250428;

    /// <summary>
    /// Qwen2.5-7B-Instruct-1M - 7B model with 1M context support
    /// </summary>
    public static readonly ChatModel ModelQwen2_57BInstruct1M = new ChatModel("qwen2.5-7b-instruct-1m", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2_57BInstruct1M"/>
    /// </summary>
    public readonly ChatModel Qwen2_57BInstruct1M = ModelQwen2_57BInstruct1M;

    /// <summary>
    /// Qwen2.5-14B-Instruct-1M - 14B model with 1M context support
    /// </summary>
    public static readonly ChatModel ModelQwen2_514BInstruct1M = new ChatModel("qwen2.5-14b-instruct-1m", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2_514BInstruct1M"/>
    /// </summary>
    public readonly ChatModel Qwen2_514BInstruct1M = ModelQwen2_514BInstruct1M;

    /// <summary>
    /// Qwen-Turbo-2024-11-01 - Snapshot from November 1, 2024
    /// </summary>
    public static readonly ChatModel ModelQwenTurbo20241101 = new ChatModel("qwen-turbo-2024-11-01", LLmProviders.Alibaba, 1_000_000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenTurbo20241101"/>
    /// </summary>
    public readonly ChatModel QwenTurbo20241101 = ModelQwenTurbo20241101;

    /// <summary>
    /// Qwen2.5-72B-Instruct - Open source instruction-tuned model with 72B parameters
    /// </summary>
    public static readonly ChatModel ModelQwen2_572BInstruct = new ChatModel("qwen2.5-72b-instruct", LLmProviders.Alibaba, 131_072);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2_572BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen2_572BInstruct = ModelQwen2_572BInstruct;

    /// <summary>
    /// Qwen2.5-32B-Instruct - Open source instruction-tuned model with 32B parameters
    /// </summary>
    public static readonly ChatModel ModelQwen2_532BInstruct = new ChatModel("qwen2.5-32b-instruct", LLmProviders.Alibaba, 131_072);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2_532BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen2_532BInstruct = ModelQwen2_532BInstruct;

    /// <summary>
    /// Qwen2.5-14B-Instruct - Open source instruction-tuned model with 14B parameters
    /// </summary>
    public static readonly ChatModel ModelQwen2_514BInstruct = new ChatModel("qwen2.5-14b-instruct", LLmProviders.Alibaba, 131_072);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2_514BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen2_514BInstruct = ModelQwen2_514BInstruct;

    /// <summary>
    /// Qwen2.5-7B-Instruct - Open source instruction-tuned model with 7B parameters
    /// </summary>
    public static readonly ChatModel ModelQwen2_57BInstruct = new ChatModel("qwen2.5-7b-instruct", LLmProviders.Alibaba, 131_072);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2_57BInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen2_57BInstruct = ModelQwen2_57BInstruct;

    /// <summary>
    /// All known older models from Alibaba.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [
        ModelQwenMax, ModelQwenOmniTurbo, ModelQwenOmniTurboRealtime, ModelQwenOmniTurboLatest, ModelQwenOmniTurboRealtimeLatest,
        ModelQwenOmniTurbo20250326, ModelQwenOmniTurboRealtime20250508, ModelQwenMaxLatest, ModelQwenMax20250125,
        ModelWan2_1T2ITurbo, ModelWan2_1T2IPlus, ModelWan2_1T2VPlus, ModelWan2_1T2VTurbo, ModelWan2_1I2VPlus, ModelWan2_1I2VTurbo,
        ModelQwenTurbo, ModelQwenQwqPlus, ModelQwenTurboLatest, ModelQwenTurbo20250428, ModelQwen2_57BInstruct1M, ModelQwen2_514BInstruct1M,
        ModelQwenTurbo20241101, ModelQwen2_572BInstruct, ModelQwen2_532BInstruct, ModelQwen2_514BInstruct, ModelQwen2_57BInstruct
    ]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelAlibabaOlder()
    {
    }
}
