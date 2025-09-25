// This code was generated with LlmTornado.Internal.OpenRouter
// do not edit manually

using System;
using System.Collections.Generic;
using LlmTornado.Code.Models;
using LlmTornado.Code;

namespace LlmTornado.Chat.Models.OpenRouter;

/// <summary>
/// All models from Open Router.
/// </summary>
public class ChatModelOpenRouterAll : IVendorModelClassProvider
{
    /// <summary>
    /// ai21/jamba-large-1.7
    /// </summary>
    public static readonly ChatModel ModelJambaLarge17 = new ChatModel("ai21/jamba-large-1.7", "ai21/jamba-large-1.7", LLmProviders.OpenRouter, 256000);

    /// <summary>
    /// <inheritdoc cref="ModelJambaLarge17"/>
    /// </summary>
    public readonly ChatModel JambaLarge17 = ModelJambaLarge17;

    /// <summary>
    /// ai21/jamba-mini-1.7
    /// </summary>
    public static readonly ChatModel ModelJambaMini17 = new ChatModel("ai21/jamba-mini-1.7", "ai21/jamba-mini-1.7", LLmProviders.OpenRouter, 256000);

    /// <summary>
    /// <inheritdoc cref="ModelJambaMini17"/>
    /// </summary>
    public readonly ChatModel JambaMini17 = ModelJambaMini17;

    /// <summary>
    /// agentica-org/deepcoder-14b-preview
    /// </summary>
    public static readonly ChatModel ModelDeepcoder14bPreview = new ChatModel("agentica-org/deepcoder-14b-preview", "agentica-org/deepcoder-14b-preview", LLmProviders.OpenRouter, 96000);

    /// <summary>
    /// <inheritdoc cref="ModelDeepcoder14bPreview"/>
    /// </summary>
    public readonly ChatModel Deepcoder14bPreview = ModelDeepcoder14bPreview;

    /// <summary>
    /// agentica-org/deepcoder-14b-preview:free
    /// </summary>
    public static readonly ChatModel ModelDeepcoder14bPreviewFree = new ChatModel("agentica-org/deepcoder-14b-preview:free", "agentica-org/deepcoder-14b-preview:free", LLmProviders.OpenRouter, 96000);

    /// <summary>
    /// <inheritdoc cref="ModelDeepcoder14bPreviewFree"/>
    /// </summary>
    public readonly ChatModel Deepcoder14bPreviewFree = ModelDeepcoder14bPreviewFree;

    /// <summary>
    /// aion-labs/aion-1.0
    /// </summary>
    public static readonly ChatModel ModelAion10 = new ChatModel("aion-labs/aion-1.0", "aion-labs/aion-1.0", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelAion10"/>
    /// </summary>
    public readonly ChatModel Aion10 = ModelAion10;

    /// <summary>
    /// aion-labs/aion-1.0-mini
    /// </summary>
    public static readonly ChatModel ModelAion10Mini = new ChatModel("aion-labs/aion-1.0-mini", "aion-labs/aion-1.0-mini", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelAion10Mini"/>
    /// </summary>
    public readonly ChatModel Aion10Mini = ModelAion10Mini;

    /// <summary>
    /// aion-labs/aion-rp-llama-3.1-8b
    /// </summary>
    public static readonly ChatModel ModelAionRpLlama318b = new ChatModel("aion-labs/aion-rp-llama-3.1-8b", "aion-labs/aion-rp-llama-3.1-8b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelAionRpLlama318b"/>
    /// </summary>
    public readonly ChatModel AionRpLlama318b = ModelAionRpLlama318b;

    /// <summary>
    /// alfredpros/codellama-7b-instruct-solidity
    /// </summary>
    public static readonly ChatModel ModelCodellama7bInstructSolidity = new ChatModel("alfredpros/codellama-7b-instruct-solidity", "alfredpros/codellama-7b-instruct-solidity", LLmProviders.OpenRouter, 4096);

    /// <summary>
    /// <inheritdoc cref="ModelCodellama7bInstructSolidity"/>
    /// </summary>
    public readonly ChatModel Codellama7bInstructSolidity = ModelCodellama7bInstructSolidity;

    /// <summary>
    /// allenai/molmo-7b-d
    /// </summary>
    public static readonly ChatModel ModelMolmo7bD = new ChatModel("allenai/molmo-7b-d", "allenai/molmo-7b-d", LLmProviders.OpenRouter, 4096);

    /// <summary>
    /// <inheritdoc cref="ModelMolmo7bD"/>
    /// </summary>
    public readonly ChatModel Molmo7bD = ModelMolmo7bD;

    /// <summary>
    /// allenai/olmo-2-0325-32b-instruct
    /// </summary>
    public static readonly ChatModel ModelOlmo2032532bInstruct = new ChatModel("allenai/olmo-2-0325-32b-instruct", "allenai/olmo-2-0325-32b-instruct", LLmProviders.OpenRouter, 4096);

    /// <summary>
    /// <inheritdoc cref="ModelOlmo2032532bInstruct"/>
    /// </summary>
    public readonly ChatModel Olmo2032532bInstruct = ModelOlmo2032532bInstruct;

    /// <summary>
    /// amazon/nova-lite-v1
    /// </summary>
    public static readonly ChatModel ModelNovaLiteV1 = new ChatModel("amazon/nova-lite-v1", "amazon/nova-lite-v1", LLmProviders.OpenRouter, 300000);

    /// <summary>
    /// <inheritdoc cref="ModelNovaLiteV1"/>
    /// </summary>
    public readonly ChatModel NovaLiteV1 = ModelNovaLiteV1;

    /// <summary>
    /// amazon/nova-micro-v1
    /// </summary>
    public static readonly ChatModel ModelNovaMicroV1 = new ChatModel("amazon/nova-micro-v1", "amazon/nova-micro-v1", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelNovaMicroV1"/>
    /// </summary>
    public readonly ChatModel NovaMicroV1 = ModelNovaMicroV1;

    /// <summary>
    /// amazon/nova-pro-v1
    /// </summary>
    public static readonly ChatModel ModelNovaProV1 = new ChatModel("amazon/nova-pro-v1", "amazon/nova-pro-v1", LLmProviders.OpenRouter, 300000);

    /// <summary>
    /// <inheritdoc cref="ModelNovaProV1"/>
    /// </summary>
    public readonly ChatModel NovaProV1 = ModelNovaProV1;

    /// <summary>
    /// anthropic/claude-3-haiku
    /// </summary>
    public static readonly ChatModel ModelClaude3Haiku = new ChatModel("anthropic/claude-3-haiku", "anthropic/claude-3-haiku", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelClaude3Haiku"/>
    /// </summary>
    public readonly ChatModel Claude3Haiku = ModelClaude3Haiku;

    /// <summary>
    /// anthropic/claude-3-opus
    /// </summary>
    public static readonly ChatModel ModelClaude3Opus = new ChatModel("anthropic/claude-3-opus", "anthropic/claude-3-opus", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelClaude3Opus"/>
    /// </summary>
    public readonly ChatModel Claude3Opus = ModelClaude3Opus;

    /// <summary>
    /// anthropic/claude-3.5-haiku
    /// </summary>
    public static readonly ChatModel ModelClaude35Haiku = new ChatModel("anthropic/claude-3.5-haiku", "anthropic/claude-3.5-haiku", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelClaude35Haiku"/>
    /// </summary>
    public readonly ChatModel Claude35Haiku = ModelClaude35Haiku;

    /// <summary>
    /// anthropic/claude-3.5-haiku-20241022
    /// </summary>
    public static readonly ChatModel ModelClaude35Haiku20241022 = new ChatModel("anthropic/claude-3.5-haiku-20241022", "anthropic/claude-3.5-haiku-20241022", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelClaude35Haiku20241022"/>
    /// </summary>
    public readonly ChatModel Claude35Haiku20241022 = ModelClaude35Haiku20241022;

    /// <summary>
    /// anthropic/claude-3.5-sonnet
    /// </summary>
    public static readonly ChatModel ModelClaude35Sonnet = new ChatModel("anthropic/claude-3.5-sonnet", "anthropic/claude-3.5-sonnet", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelClaude35Sonnet"/>
    /// </summary>
    public readonly ChatModel Claude35Sonnet = ModelClaude35Sonnet;

    /// <summary>
    /// anthropic/claude-3.5-sonnet-20240620
    /// </summary>
    public static readonly ChatModel ModelClaude35Sonnet20240620 = new ChatModel("anthropic/claude-3.5-sonnet-20240620", "anthropic/claude-3.5-sonnet-20240620", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelClaude35Sonnet20240620"/>
    /// </summary>
    public readonly ChatModel Claude35Sonnet20240620 = ModelClaude35Sonnet20240620;

    /// <summary>
    /// anthropic/claude-3.7-sonnet
    /// </summary>
    public static readonly ChatModel ModelClaude37Sonnet = new ChatModel("anthropic/claude-3.7-sonnet", "anthropic/claude-3.7-sonnet", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelClaude37Sonnet"/>
    /// </summary>
    public readonly ChatModel Claude37Sonnet = ModelClaude37Sonnet;

    /// <summary>
    /// anthropic/claude-3.7-sonnet:thinking
    /// </summary>
    public static readonly ChatModel ModelClaude37SonnetThinking = new ChatModel("anthropic/claude-3.7-sonnet:thinking", "anthropic/claude-3.7-sonnet:thinking", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelClaude37SonnetThinking"/>
    /// </summary>
    public readonly ChatModel Claude37SonnetThinking = ModelClaude37SonnetThinking;

    /// <summary>
    /// anthropic/claude-opus-4
    /// </summary>
    public static readonly ChatModel ModelClaudeOpus4 = new ChatModel("anthropic/claude-opus-4", "anthropic/claude-opus-4", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelClaudeOpus4"/>
    /// </summary>
    public readonly ChatModel ClaudeOpus4 = ModelClaudeOpus4;

    /// <summary>
    /// anthropic/claude-opus-4.1
    /// </summary>
    public static readonly ChatModel ModelClaudeOpus41 = new ChatModel("anthropic/claude-opus-4.1", "anthropic/claude-opus-4.1", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelClaudeOpus41"/>
    /// </summary>
    public readonly ChatModel ClaudeOpus41 = ModelClaudeOpus41;

    /// <summary>
    /// anthropic/claude-sonnet-4
    /// </summary>
    public static readonly ChatModel ModelClaudeSonnet4 = new ChatModel("anthropic/claude-sonnet-4", "anthropic/claude-sonnet-4", LLmProviders.OpenRouter, 1000000);

    /// <summary>
    /// <inheritdoc cref="ModelClaudeSonnet4"/>
    /// </summary>
    public readonly ChatModel ClaudeSonnet4 = ModelClaudeSonnet4;

    /// <summary>
    /// arcee-ai/afm-4.5b
    /// </summary>
    public static readonly ChatModel ModelAfm45b = new ChatModel("arcee-ai/afm-4.5b", "arcee-ai/afm-4.5b", LLmProviders.OpenRouter, 65536);

    /// <summary>
    /// <inheritdoc cref="ModelAfm45b"/>
    /// </summary>
    public readonly ChatModel Afm45b = ModelAfm45b;

    /// <summary>
    /// arcee-ai/coder-large
    /// </summary>
    public static readonly ChatModel ModelCoderLarge = new ChatModel("arcee-ai/coder-large", "arcee-ai/coder-large", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelCoderLarge"/>
    /// </summary>
    public readonly ChatModel CoderLarge = ModelCoderLarge;

    /// <summary>
    /// arcee-ai/maestro-reasoning
    /// </summary>
    public static readonly ChatModel ModelMaestroReasoning = new ChatModel("arcee-ai/maestro-reasoning", "arcee-ai/maestro-reasoning", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelMaestroReasoning"/>
    /// </summary>
    public readonly ChatModel MaestroReasoning = ModelMaestroReasoning;

    /// <summary>
    /// arcee-ai/spotlight
    /// </summary>
    public static readonly ChatModel ModelSpotlight = new ChatModel("arcee-ai/spotlight", "arcee-ai/spotlight", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelSpotlight"/>
    /// </summary>
    public readonly ChatModel Spotlight = ModelSpotlight;

    /// <summary>
    /// arcee-ai/virtuoso-large
    /// </summary>
    public static readonly ChatModel ModelVirtuosoLarge = new ChatModel("arcee-ai/virtuoso-large", "arcee-ai/virtuoso-large", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelVirtuosoLarge"/>
    /// </summary>
    public readonly ChatModel VirtuosoLarge = ModelVirtuosoLarge;

    /// <summary>
    /// arliai/qwq-32b-arliai-rpr-v1
    /// </summary>
    public static readonly ChatModel ModelQwq32bArliaiRprV1 = new ChatModel("arliai/qwq-32b-arliai-rpr-v1", "arliai/qwq-32b-arliai-rpr-v1", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelQwq32bArliaiRprV1"/>
    /// </summary>
    public readonly ChatModel Qwq32bArliaiRprV1 = ModelQwq32bArliaiRprV1;

    /// <summary>
    /// arliai/qwq-32b-arliai-rpr-v1:free
    /// </summary>
    public static readonly ChatModel ModelQwq32bArliaiRprV1Free = new ChatModel("arliai/qwq-32b-arliai-rpr-v1:free", "arliai/qwq-32b-arliai-rpr-v1:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelQwq32bArliaiRprV1Free"/>
    /// </summary>
    public readonly ChatModel Qwq32bArliaiRprV1Free = ModelQwq32bArliaiRprV1Free;

    /// <summary>
    /// openrouter/auto
    /// </summary>
    public static readonly ChatModel ModelAuto = new ChatModel("openrouter/auto", "openrouter/auto", LLmProviders.OpenRouter, 2000000);

    /// <summary>
    /// <inheritdoc cref="ModelAuto"/>
    /// </summary>
    public readonly ChatModel Auto = ModelAuto;

    /// <summary>
    /// baidu/ernie-4.5-21b-a3b
    /// </summary>
    public static readonly ChatModel ModelErnie4521bA3b = new ChatModel("baidu/ernie-4.5-21b-a3b", "baidu/ernie-4.5-21b-a3b", LLmProviders.OpenRouter, 120000);

    /// <summary>
    /// <inheritdoc cref="ModelErnie4521bA3b"/>
    /// </summary>
    public readonly ChatModel Ernie4521bA3b = ModelErnie4521bA3b;

    /// <summary>
    /// baidu/ernie-4.5-300b-a47b
    /// </summary>
    public static readonly ChatModel ModelErnie45300bA47b = new ChatModel("baidu/ernie-4.5-300b-a47b", "baidu/ernie-4.5-300b-a47b", LLmProviders.OpenRouter, 123000);

    /// <summary>
    /// <inheritdoc cref="ModelErnie45300bA47b"/>
    /// </summary>
    public readonly ChatModel Ernie45300bA47b = ModelErnie45300bA47b;

    /// <summary>
    /// baidu/ernie-4.5-vl-28b-a3b
    /// </summary>
    public static readonly ChatModel ModelErnie45Vl28bA3b = new ChatModel("baidu/ernie-4.5-vl-28b-a3b", "baidu/ernie-4.5-vl-28b-a3b", LLmProviders.OpenRouter, 30000);

    /// <summary>
    /// <inheritdoc cref="ModelErnie45Vl28bA3b"/>
    /// </summary>
    public readonly ChatModel Ernie45Vl28bA3b = ModelErnie45Vl28bA3b;

    /// <summary>
    /// baidu/ernie-4.5-vl-424b-a47b
    /// </summary>
    public static readonly ChatModel ModelErnie45Vl424bA47b = new ChatModel("baidu/ernie-4.5-vl-424b-a47b", "baidu/ernie-4.5-vl-424b-a47b", LLmProviders.OpenRouter, 123000);

    /// <summary>
    /// <inheritdoc cref="ModelErnie45Vl424bA47b"/>
    /// </summary>
    public readonly ChatModel Ernie45Vl424bA47b = ModelErnie45Vl424bA47b;

    /// <summary>
    /// bytedance/seed-oss-36b-instruct
    /// </summary>
    public static readonly ChatModel ModelSeedOss36bInstruct = new ChatModel("bytedance/seed-oss-36b-instruct", "bytedance/seed-oss-36b-instruct", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelSeedOss36bInstruct"/>
    /// </summary>
    public readonly ChatModel SeedOss36bInstruct = ModelSeedOss36bInstruct;

    /// <summary>
    /// bytedance/ui-tars-1.5-7b
    /// </summary>
    public static readonly ChatModel ModelUiTars157b = new ChatModel("bytedance/ui-tars-1.5-7b", "bytedance/ui-tars-1.5-7b", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelUiTars157b"/>
    /// </summary>
    public readonly ChatModel UiTars157b = ModelUiTars157b;

    /// <summary>
    /// deepcogito/cogito-v2-preview-llama-109b-moe
    /// </summary>
    public static readonly ChatModel ModelCogitoV2PreviewLlama109bMoe = new ChatModel("deepcogito/cogito-v2-preview-llama-109b-moe", "deepcogito/cogito-v2-preview-llama-109b-moe", LLmProviders.OpenRouter, 32767);

    /// <summary>
    /// <inheritdoc cref="ModelCogitoV2PreviewLlama109bMoe"/>
    /// </summary>
    public readonly ChatModel CogitoV2PreviewLlama109bMoe = ModelCogitoV2PreviewLlama109bMoe;

    /// <summary>
    /// cohere/command-a
    /// </summary>
    public static readonly ChatModel ModelCommandA = new ChatModel("cohere/command-a", "cohere/command-a", LLmProviders.OpenRouter, 256000);

    /// <summary>
    /// <inheritdoc cref="ModelCommandA"/>
    /// </summary>
    public readonly ChatModel CommandA = ModelCommandA;

    /// <summary>
    /// cohere/command-r-08-2024
    /// </summary>
    public static readonly ChatModel ModelCommandR082024 = new ChatModel("cohere/command-r-08-2024", "cohere/command-r-08-2024", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelCommandR082024"/>
    /// </summary>
    public readonly ChatModel CommandR082024 = ModelCommandR082024;

    /// <summary>
    /// cohere/command-r-plus-08-2024
    /// </summary>
    public static readonly ChatModel ModelCommandRPlus082024 = new ChatModel("cohere/command-r-plus-08-2024", "cohere/command-r-plus-08-2024", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelCommandRPlus082024"/>
    /// </summary>
    public readonly ChatModel CommandRPlus082024 = ModelCommandRPlus082024;

    /// <summary>
    /// cohere/command-r7b-12-2024
    /// </summary>
    public static readonly ChatModel ModelCommandR7b122024 = new ChatModel("cohere/command-r7b-12-2024", "cohere/command-r7b-12-2024", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelCommandR7b122024"/>
    /// </summary>
    public readonly ChatModel CommandR7b122024 = ModelCommandR7b122024;

    /// <summary>
    /// deepcogito/cogito-v2-preview-deepseek-671b
    /// </summary>
    public static readonly ChatModel ModelCogitoV2PreviewDeepseek671b = new ChatModel("deepcogito/cogito-v2-preview-deepseek-671b", "deepcogito/cogito-v2-preview-deepseek-671b", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelCogitoV2PreviewDeepseek671b"/>
    /// </summary>
    public readonly ChatModel CogitoV2PreviewDeepseek671b = ModelCogitoV2PreviewDeepseek671b;

    /// <summary>
    /// deepseek/deepseek-prover-v2
    /// </summary>
    public static readonly ChatModel ModelDeepseekProverV2 = new ChatModel("deepseek/deepseek-prover-v2", "deepseek/deepseek-prover-v2", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekProverV2"/>
    /// </summary>
    public readonly ChatModel DeepseekProverV2 = ModelDeepseekProverV2;

    /// <summary>
    /// deepseek/deepseek-chat
    /// </summary>
    public static readonly ChatModel ModelDeepseekChat = new ChatModel("deepseek/deepseek-chat", "deepseek/deepseek-chat", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekChat"/>
    /// </summary>
    public readonly ChatModel DeepseekChat = ModelDeepseekChat;

    /// <summary>
    /// deepseek/deepseek-chat-v3-0324
    /// </summary>
    public static readonly ChatModel ModelDeepseekChatV30324 = new ChatModel("deepseek/deepseek-chat-v3-0324", "deepseek/deepseek-chat-v3-0324", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekChatV30324"/>
    /// </summary>
    public readonly ChatModel DeepseekChatV30324 = ModelDeepseekChatV30324;

    /// <summary>
    /// deepseek/deepseek-chat-v3-0324:free
    /// </summary>
    public static readonly ChatModel ModelDeepseekChatV30324Free = new ChatModel("deepseek/deepseek-chat-v3-0324:free", "deepseek/deepseek-chat-v3-0324:free", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekChatV30324Free"/>
    /// </summary>
    public readonly ChatModel DeepseekChatV30324Free = ModelDeepseekChatV30324Free;

    /// <summary>
    /// deepseek/deepseek-chat-v3.1
    /// </summary>
    public static readonly ChatModel ModelDeepseekChatV31 = new ChatModel("deepseek/deepseek-chat-v3.1", "deepseek/deepseek-chat-v3.1", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekChatV31"/>
    /// </summary>
    public readonly ChatModel DeepseekChatV31 = ModelDeepseekChatV31;

    /// <summary>
    /// deepseek/deepseek-chat-v3.1:free
    /// </summary>
    public static readonly ChatModel ModelDeepseekChatV31Free = new ChatModel("deepseek/deepseek-chat-v3.1:free", "deepseek/deepseek-chat-v3.1:free", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekChatV31Free"/>
    /// </summary>
    public readonly ChatModel DeepseekChatV31Free = ModelDeepseekChatV31Free;

    /// <summary>
    /// deepseek/deepseek-v3.1-base
    /// </summary>
    public static readonly ChatModel ModelDeepseekV31Base = new ChatModel("deepseek/deepseek-v3.1-base", "deepseek/deepseek-v3.1-base", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekV31Base"/>
    /// </summary>
    public readonly ChatModel DeepseekV31Base = ModelDeepseekV31Base;

    /// <summary>
    /// deepseek/deepseek-v3.1-terminus
    /// </summary>
    public static readonly ChatModel ModelDeepseekV31Terminus = new ChatModel("deepseek/deepseek-v3.1-terminus", "deepseek/deepseek-v3.1-terminus", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekV31Terminus"/>
    /// </summary>
    public readonly ChatModel DeepseekV31Terminus = ModelDeepseekV31Terminus;

    /// <summary>
    /// deepseek/deepseek-r1-0528-qwen3-8b
    /// </summary>
    public static readonly ChatModel ModelDeepseekR10528Qwen38b = new ChatModel("deepseek/deepseek-r1-0528-qwen3-8b", "deepseek/deepseek-r1-0528-qwen3-8b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR10528Qwen38b"/>
    /// </summary>
    public readonly ChatModel DeepseekR10528Qwen38b = ModelDeepseekR10528Qwen38b;

    /// <summary>
    /// deepseek/deepseek-r1-0528-qwen3-8b:free
    /// </summary>
    public static readonly ChatModel ModelDeepseekR10528Qwen38bFree = new ChatModel("deepseek/deepseek-r1-0528-qwen3-8b:free", "deepseek/deepseek-r1-0528-qwen3-8b:free", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR10528Qwen38bFree"/>
    /// </summary>
    public readonly ChatModel DeepseekR10528Qwen38bFree = ModelDeepseekR10528Qwen38bFree;

    /// <summary>
    /// deepseek/deepseek-r1
    /// </summary>
    public static readonly ChatModel ModelDeepseekR1 = new ChatModel("deepseek/deepseek-r1", "deepseek/deepseek-r1", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR1"/>
    /// </summary>
    public readonly ChatModel DeepseekR1 = ModelDeepseekR1;

    /// <summary>
    /// deepseek/deepseek-r1:free
    /// </summary>
    public static readonly ChatModel ModelDeepseekR1Free = new ChatModel("deepseek/deepseek-r1:free", "deepseek/deepseek-r1:free", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR1Free"/>
    /// </summary>
    public readonly ChatModel DeepseekR1Free = ModelDeepseekR1Free;

    /// <summary>
    /// deepseek/deepseek-r1-0528
    /// </summary>
    public static readonly ChatModel ModelDeepseekR10528 = new ChatModel("deepseek/deepseek-r1-0528", "deepseek/deepseek-r1-0528", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR10528"/>
    /// </summary>
    public readonly ChatModel DeepseekR10528 = ModelDeepseekR10528;

    /// <summary>
    /// deepseek/deepseek-r1-0528:free
    /// </summary>
    public static readonly ChatModel ModelDeepseekR10528Free = new ChatModel("deepseek/deepseek-r1-0528:free", "deepseek/deepseek-r1-0528:free", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR10528Free"/>
    /// </summary>
    public readonly ChatModel DeepseekR10528Free = ModelDeepseekR10528Free;

    /// <summary>
    /// deepseek/deepseek-r1-distill-llama-70b
    /// </summary>
    public static readonly ChatModel ModelDeepseekR1DistillLlama70b = new ChatModel("deepseek/deepseek-r1-distill-llama-70b", "deepseek/deepseek-r1-distill-llama-70b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR1DistillLlama70b"/>
    /// </summary>
    public readonly ChatModel DeepseekR1DistillLlama70b = ModelDeepseekR1DistillLlama70b;

    /// <summary>
    /// deepseek/deepseek-r1-distill-llama-70b:free
    /// </summary>
    public static readonly ChatModel ModelDeepseekR1DistillLlama70bFree = new ChatModel("deepseek/deepseek-r1-distill-llama-70b:free", "deepseek/deepseek-r1-distill-llama-70b:free", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR1DistillLlama70bFree"/>
    /// </summary>
    public readonly ChatModel DeepseekR1DistillLlama70bFree = ModelDeepseekR1DistillLlama70bFree;

    /// <summary>
    /// deepseek/deepseek-r1-distill-llama-8b
    /// </summary>
    public static readonly ChatModel ModelDeepseekR1DistillLlama8b = new ChatModel("deepseek/deepseek-r1-distill-llama-8b", "deepseek/deepseek-r1-distill-llama-8b", LLmProviders.OpenRouter, 32000);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR1DistillLlama8b"/>
    /// </summary>
    public readonly ChatModel DeepseekR1DistillLlama8b = ModelDeepseekR1DistillLlama8b;

    /// <summary>
    /// deepseek/deepseek-r1-distill-qwen-14b
    /// </summary>
    public static readonly ChatModel ModelDeepseekR1DistillQwen14b = new ChatModel("deepseek/deepseek-r1-distill-qwen-14b", "deepseek/deepseek-r1-distill-qwen-14b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR1DistillQwen14b"/>
    /// </summary>
    public readonly ChatModel DeepseekR1DistillQwen14b = ModelDeepseekR1DistillQwen14b;

    /// <summary>
    /// deepseek/deepseek-r1-distill-qwen-32b
    /// </summary>
    public static readonly ChatModel ModelDeepseekR1DistillQwen32b = new ChatModel("deepseek/deepseek-r1-distill-qwen-32b", "deepseek/deepseek-r1-distill-qwen-32b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR1DistillQwen32b"/>
    /// </summary>
    public readonly ChatModel DeepseekR1DistillQwen32b = ModelDeepseekR1DistillQwen32b;

    /// <summary>
    /// cognitivecomputations/dolphin3.0-mistral-24b
    /// </summary>
    public static readonly ChatModel ModelDolphin30Mistral24b = new ChatModel("cognitivecomputations/dolphin3.0-mistral-24b", "cognitivecomputations/dolphin3.0-mistral-24b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelDolphin30Mistral24b"/>
    /// </summary>
    public readonly ChatModel Dolphin30Mistral24b = ModelDolphin30Mistral24b;

    /// <summary>
    /// cognitivecomputations/dolphin3.0-mistral-24b:free
    /// </summary>
    public static readonly ChatModel ModelDolphin30Mistral24bFree = new ChatModel("cognitivecomputations/dolphin3.0-mistral-24b:free", "cognitivecomputations/dolphin3.0-mistral-24b:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelDolphin30Mistral24bFree"/>
    /// </summary>
    public readonly ChatModel Dolphin30Mistral24bFree = ModelDolphin30Mistral24bFree;

    /// <summary>
    /// cognitivecomputations/dolphin3.0-r1-mistral-24b
    /// </summary>
    public static readonly ChatModel ModelDolphin30R1Mistral24b = new ChatModel("cognitivecomputations/dolphin3.0-r1-mistral-24b", "cognitivecomputations/dolphin3.0-r1-mistral-24b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelDolphin30R1Mistral24b"/>
    /// </summary>
    public readonly ChatModel Dolphin30R1Mistral24b = ModelDolphin30R1Mistral24b;

    /// <summary>
    /// cognitivecomputations/dolphin3.0-r1-mistral-24b:free
    /// </summary>
    public static readonly ChatModel ModelDolphin30R1Mistral24bFree = new ChatModel("cognitivecomputations/dolphin3.0-r1-mistral-24b:free", "cognitivecomputations/dolphin3.0-r1-mistral-24b:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelDolphin30R1Mistral24bFree"/>
    /// </summary>
    public readonly ChatModel Dolphin30R1Mistral24bFree = ModelDolphin30R1Mistral24bFree;

    /// <summary>
    /// eleutherai/llemma_7b
    /// </summary>
    public static readonly ChatModel ModelLlemma7b = new ChatModel("eleutherai/llemma_7b", "eleutherai/llemma_7b", LLmProviders.OpenRouter, 4096);

    /// <summary>
    /// <inheritdoc cref="ModelLlemma7b"/>
    /// </summary>
    public readonly ChatModel Llemma7b = ModelLlemma7b;

    /// <summary>
    /// google/gemini-2.5-flash-image-preview
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashImagePreview = new ChatModel("google/gemini-2.5-flash-image-preview", "google/gemini-2.5-flash-image-preview", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashImagePreview"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashImagePreview = ModelGemini25FlashImagePreview;

    /// <summary>
    /// alpindale/goliath-120b
    /// </summary>
    public static readonly ChatModel ModelGoliath120b = new ChatModel("alpindale/goliath-120b", "alpindale/goliath-120b", LLmProviders.OpenRouter, 6144);

    /// <summary>
    /// <inheritdoc cref="ModelGoliath120b"/>
    /// </summary>
    public readonly ChatModel Goliath120b = ModelGoliath120b;

    /// <summary>
    /// google/gemini-flash-1.5-8b
    /// </summary>
    public static readonly ChatModel ModelGeminiFlash158b = new ChatModel("google/gemini-flash-1.5-8b", "google/gemini-flash-1.5-8b", LLmProviders.OpenRouter, 1000000);

    /// <summary>
    /// <inheritdoc cref="ModelGeminiFlash158b"/>
    /// </summary>
    public readonly ChatModel GeminiFlash158b = ModelGeminiFlash158b;

    /// <summary>
    /// google/gemini-2.0-flash-001
    /// </summary>
    public static readonly ChatModel ModelGemini20Flash001 = new ChatModel("google/gemini-2.0-flash-001", "google/gemini-2.0-flash-001", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini20Flash001"/>
    /// </summary>
    public readonly ChatModel Gemini20Flash001 = ModelGemini20Flash001;

    /// <summary>
    /// google/gemini-2.0-flash-exp:free
    /// </summary>
    public static readonly ChatModel ModelGemini20FlashExpFree = new ChatModel("google/gemini-2.0-flash-exp:free", "google/gemini-2.0-flash-exp:free", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini20FlashExpFree"/>
    /// </summary>
    public readonly ChatModel Gemini20FlashExpFree = ModelGemini20FlashExpFree;

    /// <summary>
    /// google/gemini-2.0-flash-lite-001
    /// </summary>
    public static readonly ChatModel ModelGemini20FlashLite001 = new ChatModel("google/gemini-2.0-flash-lite-001", "google/gemini-2.0-flash-lite-001", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini20FlashLite001"/>
    /// </summary>
    public readonly ChatModel Gemini20FlashLite001 = ModelGemini20FlashLite001;

    /// <summary>
    /// google/gemini-2.5-flash
    /// </summary>
    public static readonly ChatModel ModelGemini25Flash = new ChatModel("google/gemini-2.5-flash", "google/gemini-2.5-flash", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini25Flash"/>
    /// </summary>
    public readonly ChatModel Gemini25Flash = ModelGemini25Flash;

    /// <summary>
    /// google/gemini-2.5-flash-lite
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashLite = new ChatModel("google/gemini-2.5-flash-lite", "google/gemini-2.5-flash-lite", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashLite"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashLite = ModelGemini25FlashLite;

    /// <summary>
    /// google/gemini-2.5-flash-lite-preview-06-17
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashLitePreview0617 = new ChatModel("google/gemini-2.5-flash-lite-preview-06-17", "google/gemini-2.5-flash-lite-preview-06-17", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashLitePreview0617"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashLitePreview0617 = ModelGemini25FlashLitePreview0617;

    /// <summary>
    /// google/gemini-2.5-flash-lite-preview-09-2025
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashLitePreview092025 = new ChatModel("google/gemini-2.5-flash-lite-preview-09-2025", "google/gemini-2.5-flash-lite-preview-09-2025", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashLitePreview092025"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashLitePreview092025 = ModelGemini25FlashLitePreview092025;

    /// <summary>
    /// google/gemini-2.5-flash-preview-09-2025
    /// </summary>
    public static readonly ChatModel ModelGemini25FlashPreview092025 = new ChatModel("google/gemini-2.5-flash-preview-09-2025", "google/gemini-2.5-flash-preview-09-2025", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini25FlashPreview092025"/>
    /// </summary>
    public readonly ChatModel Gemini25FlashPreview092025 = ModelGemini25FlashPreview092025;

    /// <summary>
    /// google/gemini-2.5-pro
    /// </summary>
    public static readonly ChatModel ModelGemini25Pro = new ChatModel("google/gemini-2.5-pro", "google/gemini-2.5-pro", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini25Pro"/>
    /// </summary>
    public readonly ChatModel Gemini25Pro = ModelGemini25Pro;

    /// <summary>
    /// google/gemini-2.5-pro-preview-05-06
    /// </summary>
    public static readonly ChatModel ModelGemini25ProPreview0506 = new ChatModel("google/gemini-2.5-pro-preview-05-06", "google/gemini-2.5-pro-preview-05-06", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini25ProPreview0506"/>
    /// </summary>
    public readonly ChatModel Gemini25ProPreview0506 = ModelGemini25ProPreview0506;

    /// <summary>
    /// google/gemini-2.5-pro-preview
    /// </summary>
    public static readonly ChatModel ModelGemini25ProPreview = new ChatModel("google/gemini-2.5-pro-preview", "google/gemini-2.5-pro-preview", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelGemini25ProPreview"/>
    /// </summary>
    public readonly ChatModel Gemini25ProPreview = ModelGemini25ProPreview;

    /// <summary>
    /// google/gemma-2-27b-it
    /// </summary>
    public static readonly ChatModel ModelGemma227bIt = new ChatModel("google/gemma-2-27b-it", "google/gemma-2-27b-it", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelGemma227bIt"/>
    /// </summary>
    public readonly ChatModel Gemma227bIt = ModelGemma227bIt;

    /// <summary>
    /// google/gemma-2-9b-it
    /// </summary>
    public static readonly ChatModel ModelGemma29bIt = new ChatModel("google/gemma-2-9b-it", "google/gemma-2-9b-it", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelGemma29bIt"/>
    /// </summary>
    public readonly ChatModel Gemma29bIt = ModelGemma29bIt;

    /// <summary>
    /// google/gemma-2-9b-it:free
    /// </summary>
    public static readonly ChatModel ModelGemma29bItFree = new ChatModel("google/gemma-2-9b-it:free", "google/gemma-2-9b-it:free", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelGemma29bItFree"/>
    /// </summary>
    public readonly ChatModel Gemma29bItFree = ModelGemma29bItFree;

    /// <summary>
    /// google/gemma-3-12b-it
    /// </summary>
    public static readonly ChatModel ModelGemma312bIt = new ChatModel("google/gemma-3-12b-it", "google/gemma-3-12b-it", LLmProviders.OpenRouter, 96000);

    /// <summary>
    /// <inheritdoc cref="ModelGemma312bIt"/>
    /// </summary>
    public readonly ChatModel Gemma312bIt = ModelGemma312bIt;

    /// <summary>
    /// google/gemma-3-12b-it:free
    /// </summary>
    public static readonly ChatModel ModelGemma312bItFree = new ChatModel("google/gemma-3-12b-it:free", "google/gemma-3-12b-it:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelGemma312bItFree"/>
    /// </summary>
    public readonly ChatModel Gemma312bItFree = ModelGemma312bItFree;

    /// <summary>
    /// google/gemma-3-27b-it
    /// </summary>
    public static readonly ChatModel ModelGemma327bIt = new ChatModel("google/gemma-3-27b-it", "google/gemma-3-27b-it", LLmProviders.OpenRouter, 96000);

    /// <summary>
    /// <inheritdoc cref="ModelGemma327bIt"/>
    /// </summary>
    public readonly ChatModel Gemma327bIt = ModelGemma327bIt;

    /// <summary>
    /// google/gemma-3-27b-it:free
    /// </summary>
    public static readonly ChatModel ModelGemma327bItFree = new ChatModel("google/gemma-3-27b-it:free", "google/gemma-3-27b-it:free", LLmProviders.OpenRouter, 96000);

    /// <summary>
    /// <inheritdoc cref="ModelGemma327bItFree"/>
    /// </summary>
    public readonly ChatModel Gemma327bItFree = ModelGemma327bItFree;

    /// <summary>
    /// google/gemma-3-4b-it
    /// </summary>
    public static readonly ChatModel ModelGemma34bIt = new ChatModel("google/gemma-3-4b-it", "google/gemma-3-4b-it", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelGemma34bIt"/>
    /// </summary>
    public readonly ChatModel Gemma34bIt = ModelGemma34bIt;

    /// <summary>
    /// google/gemma-3-4b-it:free
    /// </summary>
    public static readonly ChatModel ModelGemma34bItFree = new ChatModel("google/gemma-3-4b-it:free", "google/gemma-3-4b-it:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelGemma34bItFree"/>
    /// </summary>
    public readonly ChatModel Gemma34bItFree = ModelGemma34bItFree;

    /// <summary>
    /// google/gemma-3n-e2b-it:free
    /// </summary>
    public static readonly ChatModel ModelGemma3nE2bItFree = new ChatModel("google/gemma-3n-e2b-it:free", "google/gemma-3n-e2b-it:free", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelGemma3nE2bItFree"/>
    /// </summary>
    public readonly ChatModel Gemma3nE2bItFree = ModelGemma3nE2bItFree;

    /// <summary>
    /// google/gemma-3n-e4b-it
    /// </summary>
    public static readonly ChatModel ModelGemma3nE4bIt = new ChatModel("google/gemma-3n-e4b-it", "google/gemma-3n-e4b-it", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelGemma3nE4bIt"/>
    /// </summary>
    public readonly ChatModel Gemma3nE4bIt = ModelGemma3nE4bIt;

    /// <summary>
    /// google/gemma-3n-e4b-it:free
    /// </summary>
    public static readonly ChatModel ModelGemma3nE4bItFree = new ChatModel("google/gemma-3n-e4b-it:free", "google/gemma-3n-e4b-it:free", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelGemma3nE4bItFree"/>
    /// </summary>
    public readonly ChatModel Gemma3nE4bItFree = ModelGemma3nE4bItFree;

    /// <summary>
    /// inception/mercury
    /// </summary>
    public static readonly ChatModel ModelMercury = new ChatModel("inception/mercury", "inception/mercury", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelMercury"/>
    /// </summary>
    public readonly ChatModel Mercury = ModelMercury;

    /// <summary>
    /// inception/mercury-coder
    /// </summary>
    public static readonly ChatModel ModelMercuryCoder = new ChatModel("inception/mercury-coder", "inception/mercury-coder", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelMercuryCoder"/>
    /// </summary>
    public readonly ChatModel MercuryCoder = ModelMercuryCoder;

    /// <summary>
    /// inflection/inflection-3-pi
    /// </summary>
    public static readonly ChatModel ModelInflection3Pi = new ChatModel("inflection/inflection-3-pi", "inflection/inflection-3-pi", LLmProviders.OpenRouter, 8000);

    /// <summary>
    /// <inheritdoc cref="ModelInflection3Pi"/>
    /// </summary>
    public readonly ChatModel Inflection3Pi = ModelInflection3Pi;

    /// <summary>
    /// inflection/inflection-3-productivity
    /// </summary>
    public static readonly ChatModel ModelInflection3Productivity = new ChatModel("inflection/inflection-3-productivity", "inflection/inflection-3-productivity", LLmProviders.OpenRouter, 8000);

    /// <summary>
    /// <inheritdoc cref="ModelInflection3Productivity"/>
    /// </summary>
    public readonly ChatModel Inflection3Productivity = ModelInflection3Productivity;

    /// <summary>
    /// liquid/lfm-3b
    /// </summary>
    public static readonly ChatModel ModelLfm3b = new ChatModel("liquid/lfm-3b", "liquid/lfm-3b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelLfm3b"/>
    /// </summary>
    public readonly ChatModel Lfm3b = ModelLfm3b;

    /// <summary>
    /// liquid/lfm-7b
    /// </summary>
    public static readonly ChatModel ModelLfm7b = new ChatModel("liquid/lfm-7b", "liquid/lfm-7b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelLfm7b"/>
    /// </summary>
    public readonly ChatModel Lfm7b = ModelLfm7b;

    /// <summary>
    /// meta-llama/llama-guard-3-8b
    /// </summary>
    public static readonly ChatModel ModelLlamaGuard38b = new ChatModel("meta-llama/llama-guard-3-8b", "meta-llama/llama-guard-3-8b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelLlamaGuard38b"/>
    /// </summary>
    public readonly ChatModel LlamaGuard38b = ModelLlamaGuard38b;

    /// <summary>
    /// anthracite-org/magnum-v2-72b
    /// </summary>
    public static readonly ChatModel ModelMagnumV272b = new ChatModel("anthracite-org/magnum-v2-72b", "anthracite-org/magnum-v2-72b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelMagnumV272b"/>
    /// </summary>
    public readonly ChatModel MagnumV272b = ModelMagnumV272b;

    /// <summary>
    /// anthracite-org/magnum-v4-72b
    /// </summary>
    public static readonly ChatModel ModelMagnumV472b = new ChatModel("anthracite-org/magnum-v4-72b", "anthracite-org/magnum-v4-72b", LLmProviders.OpenRouter, 16384);

    /// <summary>
    /// <inheritdoc cref="ModelMagnumV472b"/>
    /// </summary>
    public readonly ChatModel MagnumV472b = ModelMagnumV472b;

    /// <summary>
    /// mancer/weaver
    /// </summary>
    public static readonly ChatModel ModelWeaver = new ChatModel("mancer/weaver", "mancer/weaver", LLmProviders.OpenRouter, 8000);

    /// <summary>
    /// <inheritdoc cref="ModelWeaver"/>
    /// </summary>
    public readonly ChatModel Weaver = ModelWeaver;

    /// <summary>
    /// meituan/longcat-flash-chat
    /// </summary>
    public static readonly ChatModel ModelLongcatFlashChat = new ChatModel("meituan/longcat-flash-chat", "meituan/longcat-flash-chat", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelLongcatFlashChat"/>
    /// </summary>
    public readonly ChatModel LongcatFlashChat = ModelLongcatFlashChat;

    /// <summary>
    /// meta-llama/llama-3-70b-instruct
    /// </summary>
    public static readonly ChatModel ModelLlama370bInstruct = new ChatModel("meta-llama/llama-3-70b-instruct", "meta-llama/llama-3-70b-instruct", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelLlama370bInstruct"/>
    /// </summary>
    public readonly ChatModel Llama370bInstruct = ModelLlama370bInstruct;

    /// <summary>
    /// meta-llama/llama-3-8b-instruct
    /// </summary>
    public static readonly ChatModel ModelLlama38bInstruct = new ChatModel("meta-llama/llama-3-8b-instruct", "meta-llama/llama-3-8b-instruct", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelLlama38bInstruct"/>
    /// </summary>
    public readonly ChatModel Llama38bInstruct = ModelLlama38bInstruct;

    /// <summary>
    /// meta-llama/llama-3.1-405b
    /// </summary>
    public static readonly ChatModel ModelLlama31405b = new ChatModel("meta-llama/llama-3.1-405b", "meta-llama/llama-3.1-405b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelLlama31405b"/>
    /// </summary>
    public readonly ChatModel Llama31405b = ModelLlama31405b;

    /// <summary>
    /// meta-llama/llama-3.1-405b-instruct
    /// </summary>
    public static readonly ChatModel ModelLlama31405bInstruct = new ChatModel("meta-llama/llama-3.1-405b-instruct", "meta-llama/llama-3.1-405b-instruct", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelLlama31405bInstruct"/>
    /// </summary>
    public readonly ChatModel Llama31405bInstruct = ModelLlama31405bInstruct;

    /// <summary>
    /// meta-llama/llama-3.1-70b-instruct
    /// </summary>
    public static readonly ChatModel ModelLlama3170bInstruct = new ChatModel("meta-llama/llama-3.1-70b-instruct", "meta-llama/llama-3.1-70b-instruct", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelLlama3170bInstruct"/>
    /// </summary>
    public readonly ChatModel Llama3170bInstruct = ModelLlama3170bInstruct;

    /// <summary>
    /// meta-llama/llama-3.1-8b-instruct
    /// </summary>
    public static readonly ChatModel ModelLlama318bInstruct = new ChatModel("meta-llama/llama-3.1-8b-instruct", "meta-llama/llama-3.1-8b-instruct", LLmProviders.OpenRouter, 16384);

    /// <summary>
    /// <inheritdoc cref="ModelLlama318bInstruct"/>
    /// </summary>
    public readonly ChatModel Llama318bInstruct = ModelLlama318bInstruct;

    /// <summary>
    /// meta-llama/llama-3.2-11b-vision-instruct
    /// </summary>
    public static readonly ChatModel ModelLlama3211bVisionInstruct = new ChatModel("meta-llama/llama-3.2-11b-vision-instruct", "meta-llama/llama-3.2-11b-vision-instruct", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelLlama3211bVisionInstruct"/>
    /// </summary>
    public readonly ChatModel Llama3211bVisionInstruct = ModelLlama3211bVisionInstruct;

    /// <summary>
    /// meta-llama/llama-3.2-1b-instruct
    /// </summary>
    public static readonly ChatModel ModelLlama321bInstruct = new ChatModel("meta-llama/llama-3.2-1b-instruct", "meta-llama/llama-3.2-1b-instruct", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelLlama321bInstruct"/>
    /// </summary>
    public readonly ChatModel Llama321bInstruct = ModelLlama321bInstruct;

    /// <summary>
    /// meta-llama/llama-3.2-3b-instruct
    /// </summary>
    public static readonly ChatModel ModelLlama323bInstruct = new ChatModel("meta-llama/llama-3.2-3b-instruct", "meta-llama/llama-3.2-3b-instruct", LLmProviders.OpenRouter, 16384);

    /// <summary>
    /// <inheritdoc cref="ModelLlama323bInstruct"/>
    /// </summary>
    public readonly ChatModel Llama323bInstruct = ModelLlama323bInstruct;

    /// <summary>
    /// meta-llama/llama-3.2-3b-instruct:free
    /// </summary>
    public static readonly ChatModel ModelLlama323bInstructFree = new ChatModel("meta-llama/llama-3.2-3b-instruct:free", "meta-llama/llama-3.2-3b-instruct:free", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelLlama323bInstructFree"/>
    /// </summary>
    public readonly ChatModel Llama323bInstructFree = ModelLlama323bInstructFree;

    /// <summary>
    /// meta-llama/llama-3.2-90b-vision-instruct
    /// </summary>
    public static readonly ChatModel ModelLlama3290bVisionInstruct = new ChatModel("meta-llama/llama-3.2-90b-vision-instruct", "meta-llama/llama-3.2-90b-vision-instruct", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelLlama3290bVisionInstruct"/>
    /// </summary>
    public readonly ChatModel Llama3290bVisionInstruct = ModelLlama3290bVisionInstruct;

    /// <summary>
    /// meta-llama/llama-3.3-70b-instruct
    /// </summary>
    public static readonly ChatModel ModelLlama3370bInstruct = new ChatModel("meta-llama/llama-3.3-70b-instruct", "meta-llama/llama-3.3-70b-instruct", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelLlama3370bInstruct"/>
    /// </summary>
    public readonly ChatModel Llama3370bInstruct = ModelLlama3370bInstruct;

    /// <summary>
    /// meta-llama/llama-3.3-70b-instruct:free
    /// </summary>
    public static readonly ChatModel ModelLlama3370bInstructFree = new ChatModel("meta-llama/llama-3.3-70b-instruct:free", "meta-llama/llama-3.3-70b-instruct:free", LLmProviders.OpenRouter, 65536);

    /// <summary>
    /// <inheritdoc cref="ModelLlama3370bInstructFree"/>
    /// </summary>
    public readonly ChatModel Llama3370bInstructFree = ModelLlama3370bInstructFree;

    /// <summary>
    /// meta-llama/llama-3.3-8b-instruct:free
    /// </summary>
    public static readonly ChatModel ModelLlama338bInstructFree = new ChatModel("meta-llama/llama-3.3-8b-instruct:free", "meta-llama/llama-3.3-8b-instruct:free", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelLlama338bInstructFree"/>
    /// </summary>
    public readonly ChatModel Llama338bInstructFree = ModelLlama338bInstructFree;

    /// <summary>
    /// meta-llama/llama-4-maverick
    /// </summary>
    public static readonly ChatModel ModelLlama4Maverick = new ChatModel("meta-llama/llama-4-maverick", "meta-llama/llama-4-maverick", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelLlama4Maverick"/>
    /// </summary>
    public readonly ChatModel Llama4Maverick = ModelLlama4Maverick;

    /// <summary>
    /// meta-llama/llama-4-maverick:free
    /// </summary>
    public static readonly ChatModel ModelLlama4MaverickFree = new ChatModel("meta-llama/llama-4-maverick:free", "meta-llama/llama-4-maverick:free", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelLlama4MaverickFree"/>
    /// </summary>
    public readonly ChatModel Llama4MaverickFree = ModelLlama4MaverickFree;

    /// <summary>
    /// meta-llama/llama-4-scout
    /// </summary>
    public static readonly ChatModel ModelLlama4Scout = new ChatModel("meta-llama/llama-4-scout", "meta-llama/llama-4-scout", LLmProviders.OpenRouter, 1048576);

    /// <summary>
    /// <inheritdoc cref="ModelLlama4Scout"/>
    /// </summary>
    public readonly ChatModel Llama4Scout = ModelLlama4Scout;

    /// <summary>
    /// meta-llama/llama-4-scout:free
    /// </summary>
    public static readonly ChatModel ModelLlama4ScoutFree = new ChatModel("meta-llama/llama-4-scout:free", "meta-llama/llama-4-scout:free", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelLlama4ScoutFree"/>
    /// </summary>
    public readonly ChatModel Llama4ScoutFree = ModelLlama4ScoutFree;

    /// <summary>
    /// meta-llama/llama-guard-4-12b
    /// </summary>
    public static readonly ChatModel ModelLlamaGuard412b = new ChatModel("meta-llama/llama-guard-4-12b", "meta-llama/llama-guard-4-12b", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelLlamaGuard412b"/>
    /// </summary>
    public readonly ChatModel LlamaGuard412b = ModelLlamaGuard412b;

    /// <summary>
    /// meta-llama/llama-guard-2-8b
    /// </summary>
    public static readonly ChatModel ModelLlamaGuard28b = new ChatModel("meta-llama/llama-guard-2-8b", "meta-llama/llama-guard-2-8b", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelLlamaGuard28b"/>
    /// </summary>
    public readonly ChatModel LlamaGuard28b = ModelLlamaGuard28b;

    /// <summary>
    /// microsoft/mai-ds-r1
    /// </summary>
    public static readonly ChatModel ModelMaiDsR1 = new ChatModel("microsoft/mai-ds-r1", "microsoft/mai-ds-r1", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelMaiDsR1"/>
    /// </summary>
    public readonly ChatModel MaiDsR1 = ModelMaiDsR1;

    /// <summary>
    /// microsoft/mai-ds-r1:free
    /// </summary>
    public static readonly ChatModel ModelMaiDsR1Free = new ChatModel("microsoft/mai-ds-r1:free", "microsoft/mai-ds-r1:free", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelMaiDsR1Free"/>
    /// </summary>
    public readonly ChatModel MaiDsR1Free = ModelMaiDsR1Free;

    /// <summary>
    /// microsoft/phi-4
    /// </summary>
    public static readonly ChatModel ModelPhi4 = new ChatModel("microsoft/phi-4", "microsoft/phi-4", LLmProviders.OpenRouter, 16384);

    /// <summary>
    /// <inheritdoc cref="ModelPhi4"/>
    /// </summary>
    public readonly ChatModel Phi4 = ModelPhi4;

    /// <summary>
    /// microsoft/phi-4-multimodal-instruct
    /// </summary>
    public static readonly ChatModel ModelPhi4MultimodalInstruct = new ChatModel("microsoft/phi-4-multimodal-instruct", "microsoft/phi-4-multimodal-instruct", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelPhi4MultimodalInstruct"/>
    /// </summary>
    public readonly ChatModel Phi4MultimodalInstruct = ModelPhi4MultimodalInstruct;

    /// <summary>
    /// microsoft/phi-4-reasoning-plus
    /// </summary>
    public static readonly ChatModel ModelPhi4ReasoningPlus = new ChatModel("microsoft/phi-4-reasoning-plus", "microsoft/phi-4-reasoning-plus", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelPhi4ReasoningPlus"/>
    /// </summary>
    public readonly ChatModel Phi4ReasoningPlus = ModelPhi4ReasoningPlus;

    /// <summary>
    /// microsoft/phi-3-medium-128k-instruct
    /// </summary>
    public static readonly ChatModel ModelPhi3Medium128kInstruct = new ChatModel("microsoft/phi-3-medium-128k-instruct", "microsoft/phi-3-medium-128k-instruct", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelPhi3Medium128kInstruct"/>
    /// </summary>
    public readonly ChatModel Phi3Medium128kInstruct = ModelPhi3Medium128kInstruct;

    /// <summary>
    /// microsoft/phi-3-mini-128k-instruct
    /// </summary>
    public static readonly ChatModel ModelPhi3Mini128kInstruct = new ChatModel("microsoft/phi-3-mini-128k-instruct", "microsoft/phi-3-mini-128k-instruct", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelPhi3Mini128kInstruct"/>
    /// </summary>
    public readonly ChatModel Phi3Mini128kInstruct = ModelPhi3Mini128kInstruct;

    /// <summary>
    /// microsoft/phi-3.5-mini-128k-instruct
    /// </summary>
    public static readonly ChatModel ModelPhi35Mini128kInstruct = new ChatModel("microsoft/phi-3.5-mini-128k-instruct", "microsoft/phi-3.5-mini-128k-instruct", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelPhi35Mini128kInstruct"/>
    /// </summary>
    public readonly ChatModel Phi35Mini128kInstruct = ModelPhi35Mini128kInstruct;

    /// <summary>
    /// minimax/minimax-m1
    /// </summary>
    public static readonly ChatModel ModelMinimaxM1 = new ChatModel("minimax/minimax-m1", "minimax/minimax-m1", LLmProviders.OpenRouter, 1000000);

    /// <summary>
    /// <inheritdoc cref="ModelMinimaxM1"/>
    /// </summary>
    public readonly ChatModel MinimaxM1 = ModelMinimaxM1;

    /// <summary>
    /// minimax/minimax-01
    /// </summary>
    public static readonly ChatModel ModelMinimax01 = new ChatModel("minimax/minimax-01", "minimax/minimax-01", LLmProviders.OpenRouter, 1000192);

    /// <summary>
    /// <inheritdoc cref="ModelMinimax01"/>
    /// </summary>
    public readonly ChatModel Minimax01 = ModelMinimax01;

    /// <summary>
    /// mistralai/mistral-large
    /// </summary>
    public static readonly ChatModel ModelMistralLarge = new ChatModel("mistralai/mistral-large", "mistralai/mistral-large", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelMistralLarge"/>
    /// </summary>
    public readonly ChatModel MistralLarge = ModelMistralLarge;

    /// <summary>
    /// mistralai/mistral-large-2407
    /// </summary>
    public static readonly ChatModel ModelMistralLarge2407 = new ChatModel("mistralai/mistral-large-2407", "mistralai/mistral-large-2407", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelMistralLarge2407"/>
    /// </summary>
    public readonly ChatModel MistralLarge2407 = ModelMistralLarge2407;

    /// <summary>
    /// mistralai/mistral-large-2411
    /// </summary>
    public static readonly ChatModel ModelMistralLarge2411 = new ChatModel("mistralai/mistral-large-2411", "mistralai/mistral-large-2411", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelMistralLarge2411"/>
    /// </summary>
    public readonly ChatModel MistralLarge2411 = ModelMistralLarge2411;

    /// <summary>
    /// mistralai/mistral-small
    /// </summary>
    public static readonly ChatModel ModelMistralSmall = new ChatModel("mistralai/mistral-small", "mistralai/mistral-small", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall"/>
    /// </summary>
    public readonly ChatModel MistralSmall = ModelMistralSmall;

    /// <summary>
    /// mistralai/mistral-tiny
    /// </summary>
    public static readonly ChatModel ModelMistralTiny = new ChatModel("mistralai/mistral-tiny", "mistralai/mistral-tiny", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelMistralTiny"/>
    /// </summary>
    public readonly ChatModel MistralTiny = ModelMistralTiny;

    /// <summary>
    /// mistralai/codestral-2501
    /// </summary>
    public static readonly ChatModel ModelCodestral2501 = new ChatModel("mistralai/codestral-2501", "mistralai/codestral-2501", LLmProviders.OpenRouter, 262144);

    /// <summary>
    /// <inheritdoc cref="ModelCodestral2501"/>
    /// </summary>
    public readonly ChatModel Codestral2501 = ModelCodestral2501;

    /// <summary>
    /// mistralai/codestral-2508
    /// </summary>
    public static readonly ChatModel ModelCodestral2508 = new ChatModel("mistralai/codestral-2508", "mistralai/codestral-2508", LLmProviders.OpenRouter, 256000);

    /// <summary>
    /// <inheritdoc cref="ModelCodestral2508"/>
    /// </summary>
    public readonly ChatModel Codestral2508 = ModelCodestral2508;

    /// <summary>
    /// mistralai/devstral-medium
    /// </summary>
    public static readonly ChatModel ModelDevstralMedium = new ChatModel("mistralai/devstral-medium", "mistralai/devstral-medium", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelDevstralMedium"/>
    /// </summary>
    public readonly ChatModel DevstralMedium = ModelDevstralMedium;

    /// <summary>
    /// mistralai/devstral-small
    /// </summary>
    public static readonly ChatModel ModelDevstralSmall = new ChatModel("mistralai/devstral-small", "mistralai/devstral-small", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelDevstralSmall"/>
    /// </summary>
    public readonly ChatModel DevstralSmall = ModelDevstralSmall;

    /// <summary>
    /// mistralai/devstral-small-2505
    /// </summary>
    public static readonly ChatModel ModelDevstralSmall2505 = new ChatModel("mistralai/devstral-small-2505", "mistralai/devstral-small-2505", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelDevstralSmall2505"/>
    /// </summary>
    public readonly ChatModel DevstralSmall2505 = ModelDevstralSmall2505;

    /// <summary>
    /// mistralai/devstral-small-2505:free
    /// </summary>
    public static readonly ChatModel ModelDevstralSmall2505Free = new ChatModel("mistralai/devstral-small-2505:free", "mistralai/devstral-small-2505:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelDevstralSmall2505Free"/>
    /// </summary>
    public readonly ChatModel DevstralSmall2505Free = ModelDevstralSmall2505Free;

    /// <summary>
    /// mistralai/magistral-medium-2506
    /// </summary>
    public static readonly ChatModel ModelMagistralMedium2506 = new ChatModel("mistralai/magistral-medium-2506", "mistralai/magistral-medium-2506", LLmProviders.OpenRouter, 40960);

    /// <summary>
    /// <inheritdoc cref="ModelMagistralMedium2506"/>
    /// </summary>
    public readonly ChatModel MagistralMedium2506 = ModelMagistralMedium2506;

    /// <summary>
    /// mistralai/magistral-medium-2506:thinking
    /// </summary>
    public static readonly ChatModel ModelMagistralMedium2506Thinking = new ChatModel("mistralai/magistral-medium-2506:thinking", "mistralai/magistral-medium-2506:thinking", LLmProviders.OpenRouter, 40960);

    /// <summary>
    /// <inheritdoc cref="ModelMagistralMedium2506Thinking"/>
    /// </summary>
    public readonly ChatModel MagistralMedium2506Thinking = ModelMagistralMedium2506Thinking;

    /// <summary>
    /// mistralai/magistral-small-2506
    /// </summary>
    public static readonly ChatModel ModelMagistralSmall2506 = new ChatModel("mistralai/magistral-small-2506", "mistralai/magistral-small-2506", LLmProviders.OpenRouter, 40000);

    /// <summary>
    /// <inheritdoc cref="ModelMagistralSmall2506"/>
    /// </summary>
    public readonly ChatModel MagistralSmall2506 = ModelMagistralSmall2506;

    /// <summary>
    /// mistralai/ministral-3b
    /// </summary>
    public static readonly ChatModel ModelMinistral3b = new ChatModel("mistralai/ministral-3b", "mistralai/ministral-3b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelMinistral3b"/>
    /// </summary>
    public readonly ChatModel Ministral3b = ModelMinistral3b;

    /// <summary>
    /// mistralai/ministral-8b
    /// </summary>
    public static readonly ChatModel ModelMinistral8b = new ChatModel("mistralai/ministral-8b", "mistralai/ministral-8b", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelMinistral8b"/>
    /// </summary>
    public readonly ChatModel Ministral8b = ModelMinistral8b;

    /// <summary>
    /// mistralai/mistral-7b-instruct
    /// </summary>
    public static readonly ChatModel ModelMistral7bInstruct = new ChatModel("mistralai/mistral-7b-instruct", "mistralai/mistral-7b-instruct", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelMistral7bInstruct"/>
    /// </summary>
    public readonly ChatModel Mistral7bInstruct = ModelMistral7bInstruct;

    /// <summary>
    /// mistralai/mistral-7b-instruct:free
    /// </summary>
    public static readonly ChatModel ModelMistral7bInstructFree = new ChatModel("mistralai/mistral-7b-instruct:free", "mistralai/mistral-7b-instruct:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelMistral7bInstructFree"/>
    /// </summary>
    public readonly ChatModel Mistral7bInstructFree = ModelMistral7bInstructFree;

    /// <summary>
    /// mistralai/mistral-7b-instruct-v0.1
    /// </summary>
    public static readonly ChatModel ModelMistral7bInstructV01 = new ChatModel("mistralai/mistral-7b-instruct-v0.1", "mistralai/mistral-7b-instruct-v0.1", LLmProviders.OpenRouter, 2824);

    /// <summary>
    /// <inheritdoc cref="ModelMistral7bInstructV01"/>
    /// </summary>
    public readonly ChatModel Mistral7bInstructV01 = ModelMistral7bInstructV01;

    /// <summary>
    /// mistralai/mistral-7b-instruct-v0.3
    /// </summary>
    public static readonly ChatModel ModelMistral7bInstructV03 = new ChatModel("mistralai/mistral-7b-instruct-v0.3", "mistralai/mistral-7b-instruct-v0.3", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelMistral7bInstructV03"/>
    /// </summary>
    public readonly ChatModel Mistral7bInstructV03 = ModelMistral7bInstructV03;

    /// <summary>
    /// mistralai/mistral-medium-3
    /// </summary>
    public static readonly ChatModel ModelMistralMedium3 = new ChatModel("mistralai/mistral-medium-3", "mistralai/mistral-medium-3", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelMistralMedium3"/>
    /// </summary>
    public readonly ChatModel MistralMedium3 = ModelMistralMedium3;

    /// <summary>
    /// mistralai/mistral-medium-3.1
    /// </summary>
    public static readonly ChatModel ModelMistralMedium31 = new ChatModel("mistralai/mistral-medium-3.1", "mistralai/mistral-medium-3.1", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelMistralMedium31"/>
    /// </summary>
    public readonly ChatModel MistralMedium31 = ModelMistralMedium31;

    /// <summary>
    /// mistralai/mistral-nemo
    /// </summary>
    public static readonly ChatModel ModelMistralNemo = new ChatModel("mistralai/mistral-nemo", "mistralai/mistral-nemo", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelMistralNemo"/>
    /// </summary>
    public readonly ChatModel MistralNemo = ModelMistralNemo;

    /// <summary>
    /// mistralai/mistral-nemo:free
    /// </summary>
    public static readonly ChatModel ModelMistralNemoFree = new ChatModel("mistralai/mistral-nemo:free", "mistralai/mistral-nemo:free", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelMistralNemoFree"/>
    /// </summary>
    public readonly ChatModel MistralNemoFree = ModelMistralNemoFree;

    /// <summary>
    /// mistralai/mistral-small-24b-instruct-2501
    /// </summary>
    public static readonly ChatModel ModelMistralSmall24bInstruct2501 = new ChatModel("mistralai/mistral-small-24b-instruct-2501", "mistralai/mistral-small-24b-instruct-2501", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall24bInstruct2501"/>
    /// </summary>
    public readonly ChatModel MistralSmall24bInstruct2501 = ModelMistralSmall24bInstruct2501;

    /// <summary>
    /// mistralai/mistral-small-24b-instruct-2501:free
    /// </summary>
    public static readonly ChatModel ModelMistralSmall24bInstruct2501Free = new ChatModel("mistralai/mistral-small-24b-instruct-2501:free", "mistralai/mistral-small-24b-instruct-2501:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall24bInstruct2501Free"/>
    /// </summary>
    public readonly ChatModel MistralSmall24bInstruct2501Free = ModelMistralSmall24bInstruct2501Free;

    /// <summary>
    /// mistralai/mistral-small-3.1-24b-instruct
    /// </summary>
    public static readonly ChatModel ModelMistralSmall3124bInstruct = new ChatModel("mistralai/mistral-small-3.1-24b-instruct", "mistralai/mistral-small-3.1-24b-instruct", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall3124bInstruct"/>
    /// </summary>
    public readonly ChatModel MistralSmall3124bInstruct = ModelMistralSmall3124bInstruct;

    /// <summary>
    /// mistralai/mistral-small-3.1-24b-instruct:free
    /// </summary>
    public static readonly ChatModel ModelMistralSmall3124bInstructFree = new ChatModel("mistralai/mistral-small-3.1-24b-instruct:free", "mistralai/mistral-small-3.1-24b-instruct:free", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall3124bInstructFree"/>
    /// </summary>
    public readonly ChatModel MistralSmall3124bInstructFree = ModelMistralSmall3124bInstructFree;

    /// <summary>
    /// mistralai/mistral-small-3.2-24b-instruct
    /// </summary>
    public static readonly ChatModel ModelMistralSmall3224bInstruct = new ChatModel("mistralai/mistral-small-3.2-24b-instruct", "mistralai/mistral-small-3.2-24b-instruct", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall3224bInstruct"/>
    /// </summary>
    public readonly ChatModel MistralSmall3224bInstruct = ModelMistralSmall3224bInstruct;

    /// <summary>
    /// mistralai/mistral-small-3.2-24b-instruct:free
    /// </summary>
    public static readonly ChatModel ModelMistralSmall3224bInstructFree = new ChatModel("mistralai/mistral-small-3.2-24b-instruct:free", "mistralai/mistral-small-3.2-24b-instruct:free", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelMistralSmall3224bInstructFree"/>
    /// </summary>
    public readonly ChatModel MistralSmall3224bInstructFree = ModelMistralSmall3224bInstructFree;

    /// <summary>
    /// mistralai/mixtral-8x22b-instruct
    /// </summary>
    public static readonly ChatModel ModelMixtral8x22bInstruct = new ChatModel("mistralai/mixtral-8x22b-instruct", "mistralai/mixtral-8x22b-instruct", LLmProviders.OpenRouter, 65536);

    /// <summary>
    /// <inheritdoc cref="ModelMixtral8x22bInstruct"/>
    /// </summary>
    public readonly ChatModel Mixtral8x22bInstruct = ModelMixtral8x22bInstruct;

    /// <summary>
    /// mistralai/mixtral-8x7b-instruct
    /// </summary>
    public static readonly ChatModel ModelMixtral8x7bInstruct = new ChatModel("mistralai/mixtral-8x7b-instruct", "mistralai/mixtral-8x7b-instruct", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelMixtral8x7bInstruct"/>
    /// </summary>
    public readonly ChatModel Mixtral8x7bInstruct = ModelMixtral8x7bInstruct;

    /// <summary>
    /// mistralai/pixtral-12b
    /// </summary>
    public static readonly ChatModel ModelPixtral12b = new ChatModel("mistralai/pixtral-12b", "mistralai/pixtral-12b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelPixtral12b"/>
    /// </summary>
    public readonly ChatModel Pixtral12b = ModelPixtral12b;

    /// <summary>
    /// mistralai/pixtral-large-2411
    /// </summary>
    public static readonly ChatModel ModelPixtralLarge2411 = new ChatModel("mistralai/pixtral-large-2411", "mistralai/pixtral-large-2411", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelPixtralLarge2411"/>
    /// </summary>
    public readonly ChatModel PixtralLarge2411 = ModelPixtralLarge2411;

    /// <summary>
    /// mistralai/mistral-saba
    /// </summary>
    public static readonly ChatModel ModelMistralSaba = new ChatModel("mistralai/mistral-saba", "mistralai/mistral-saba", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelMistralSaba"/>
    /// </summary>
    public readonly ChatModel MistralSaba = ModelMistralSaba;

    /// <summary>
    /// moonshotai/kimi-dev-72b
    /// </summary>
    public static readonly ChatModel ModelKimiDev72b = new ChatModel("moonshotai/kimi-dev-72b", "moonshotai/kimi-dev-72b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelKimiDev72b"/>
    /// </summary>
    public readonly ChatModel KimiDev72b = ModelKimiDev72b;

    /// <summary>
    /// moonshotai/kimi-dev-72b:free
    /// </summary>
    public static readonly ChatModel ModelKimiDev72bFree = new ChatModel("moonshotai/kimi-dev-72b:free", "moonshotai/kimi-dev-72b:free", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelKimiDev72bFree"/>
    /// </summary>
    public readonly ChatModel KimiDev72bFree = ModelKimiDev72bFree;

    /// <summary>
    /// moonshotai/kimi-k2
    /// </summary>
    public static readonly ChatModel ModelKimiK2 = new ChatModel("moonshotai/kimi-k2", "moonshotai/kimi-k2", LLmProviders.OpenRouter, 63000);

    /// <summary>
    /// <inheritdoc cref="ModelKimiK2"/>
    /// </summary>
    public readonly ChatModel KimiK2 = ModelKimiK2;

    /// <summary>
    /// moonshotai/kimi-k2:free
    /// </summary>
    public static readonly ChatModel ModelKimiK2Free = new ChatModel("moonshotai/kimi-k2:free", "moonshotai/kimi-k2:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelKimiK2Free"/>
    /// </summary>
    public readonly ChatModel KimiK2Free = ModelKimiK2Free;

    /// <summary>
    /// moonshotai/kimi-k2-0905
    /// </summary>
    public static readonly ChatModel ModelKimiK20905 = new ChatModel("moonshotai/kimi-k2-0905", "moonshotai/kimi-k2-0905", LLmProviders.OpenRouter, 262144);

    /// <summary>
    /// <inheritdoc cref="ModelKimiK20905"/>
    /// </summary>
    public readonly ChatModel KimiK20905 = ModelKimiK20905;

    /// <summary>
    /// moonshotai/kimi-vl-a3b-thinking
    /// </summary>
    public static readonly ChatModel ModelKimiVlA3bThinking = new ChatModel("moonshotai/kimi-vl-a3b-thinking", "moonshotai/kimi-vl-a3b-thinking", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelKimiVlA3bThinking"/>
    /// </summary>
    public readonly ChatModel KimiVlA3bThinking = ModelKimiVlA3bThinking;

    /// <summary>
    /// moonshotai/kimi-vl-a3b-thinking:free
    /// </summary>
    public static readonly ChatModel ModelKimiVlA3bThinkingFree = new ChatModel("moonshotai/kimi-vl-a3b-thinking:free", "moonshotai/kimi-vl-a3b-thinking:free", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelKimiVlA3bThinkingFree"/>
    /// </summary>
    public readonly ChatModel KimiVlA3bThinkingFree = ModelKimiVlA3bThinkingFree;

    /// <summary>
    /// morph/morph-v3-fast
    /// </summary>
    public static readonly ChatModel ModelMorphV3Fast = new ChatModel("morph/morph-v3-fast", "morph/morph-v3-fast", LLmProviders.OpenRouter, 81920);

    /// <summary>
    /// <inheritdoc cref="ModelMorphV3Fast"/>
    /// </summary>
    public readonly ChatModel MorphV3Fast = ModelMorphV3Fast;

    /// <summary>
    /// morph/morph-v3-large
    /// </summary>
    public static readonly ChatModel ModelMorphV3Large = new ChatModel("morph/morph-v3-large", "morph/morph-v3-large", LLmProviders.OpenRouter, 81920);

    /// <summary>
    /// <inheritdoc cref="ModelMorphV3Large"/>
    /// </summary>
    public readonly ChatModel MorphV3Large = ModelMorphV3Large;

    /// <summary>
    /// gryphe/mythomax-l2-13b
    /// </summary>
    public static readonly ChatModel ModelMythomaxL213b = new ChatModel("gryphe/mythomax-l2-13b", "gryphe/mythomax-l2-13b", LLmProviders.OpenRouter, 4096);

    /// <summary>
    /// <inheritdoc cref="ModelMythomaxL213b"/>
    /// </summary>
    public readonly ChatModel MythomaxL213b = ModelMythomaxL213b;

    /// <summary>
    /// nvidia/llama-3.1-nemotron-70b-instruct
    /// </summary>
    public static readonly ChatModel ModelLlama31Nemotron70bInstruct = new ChatModel("nvidia/llama-3.1-nemotron-70b-instruct", "nvidia/llama-3.1-nemotron-70b-instruct", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelLlama31Nemotron70bInstruct"/>
    /// </summary>
    public readonly ChatModel Llama31Nemotron70bInstruct = ModelLlama31Nemotron70bInstruct;

    /// <summary>
    /// nvidia/llama-3.1-nemotron-ultra-253b-v1
    /// </summary>
    public static readonly ChatModel ModelLlama31NemotronUltra253bV1 = new ChatModel("nvidia/llama-3.1-nemotron-ultra-253b-v1", "nvidia/llama-3.1-nemotron-ultra-253b-v1", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelLlama31NemotronUltra253bV1"/>
    /// </summary>
    public readonly ChatModel Llama31NemotronUltra253bV1 = ModelLlama31NemotronUltra253bV1;

    /// <summary>
    /// nvidia/nemotron-nano-9b-v2
    /// </summary>
    public static readonly ChatModel ModelNemotronNano9bV2 = new ChatModel("nvidia/nemotron-nano-9b-v2", "nvidia/nemotron-nano-9b-v2", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelNemotronNano9bV2"/>
    /// </summary>
    public readonly ChatModel NemotronNano9bV2 = ModelNemotronNano9bV2;

    /// <summary>
    /// nvidia/nemotron-nano-9b-v2:free
    /// </summary>
    public static readonly ChatModel ModelNemotronNano9bV2Free = new ChatModel("nvidia/nemotron-nano-9b-v2:free", "nvidia/nemotron-nano-9b-v2:free", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelNemotronNano9bV2Free"/>
    /// </summary>
    public readonly ChatModel NemotronNano9bV2Free = ModelNemotronNano9bV2Free;

    /// <summary>
    /// neversleep/llama-3-lumimaid-70b
    /// </summary>
    public static readonly ChatModel ModelLlama3Lumimaid70b = new ChatModel("neversleep/llama-3-lumimaid-70b", "neversleep/llama-3-lumimaid-70b", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelLlama3Lumimaid70b"/>
    /// </summary>
    public readonly ChatModel Llama3Lumimaid70b = ModelLlama3Lumimaid70b;

    /// <summary>
    /// neversleep/llama-3.1-lumimaid-8b
    /// </summary>
    public static readonly ChatModel ModelLlama31Lumimaid8b = new ChatModel("neversleep/llama-3.1-lumimaid-8b", "neversleep/llama-3.1-lumimaid-8b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelLlama31Lumimaid8b"/>
    /// </summary>
    public readonly ChatModel Llama31Lumimaid8b = ModelLlama31Lumimaid8b;

    /// <summary>
    /// neversleep/noromaid-20b
    /// </summary>
    public static readonly ChatModel ModelNoromaid20b = new ChatModel("neversleep/noromaid-20b", "neversleep/noromaid-20b", LLmProviders.OpenRouter, 4096);

    /// <summary>
    /// <inheritdoc cref="ModelNoromaid20b"/>
    /// </summary>
    public readonly ChatModel Noromaid20b = ModelNoromaid20b;

    /// <summary>
    /// nousresearch/deephermes-3-llama-3-8b-preview
    /// </summary>
    public static readonly ChatModel ModelDeephermes3Llama38bPreview = new ChatModel("nousresearch/deephermes-3-llama-3-8b-preview", "nousresearch/deephermes-3-llama-3-8b-preview", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelDeephermes3Llama38bPreview"/>
    /// </summary>
    public readonly ChatModel Deephermes3Llama38bPreview = ModelDeephermes3Llama38bPreview;

    /// <summary>
    /// nousresearch/deephermes-3-llama-3-8b-preview:free
    /// </summary>
    public static readonly ChatModel ModelDeephermes3Llama38bPreviewFree = new ChatModel("nousresearch/deephermes-3-llama-3-8b-preview:free", "nousresearch/deephermes-3-llama-3-8b-preview:free", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelDeephermes3Llama38bPreviewFree"/>
    /// </summary>
    public readonly ChatModel Deephermes3Llama38bPreviewFree = ModelDeephermes3Llama38bPreviewFree;

    /// <summary>
    /// nousresearch/deephermes-3-mistral-24b-preview
    /// </summary>
    public static readonly ChatModel ModelDeephermes3Mistral24bPreview = new ChatModel("nousresearch/deephermes-3-mistral-24b-preview", "nousresearch/deephermes-3-mistral-24b-preview", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelDeephermes3Mistral24bPreview"/>
    /// </summary>
    public readonly ChatModel Deephermes3Mistral24bPreview = ModelDeephermes3Mistral24bPreview;

    /// <summary>
    /// nousresearch/hermes-3-llama-3.1-405b
    /// </summary>
    public static readonly ChatModel ModelHermes3Llama31405b = new ChatModel("nousresearch/hermes-3-llama-3.1-405b", "nousresearch/hermes-3-llama-3.1-405b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelHermes3Llama31405b"/>
    /// </summary>
    public readonly ChatModel Hermes3Llama31405b = ModelHermes3Llama31405b;

    /// <summary>
    /// nousresearch/hermes-3-llama-3.1-70b
    /// </summary>
    public static readonly ChatModel ModelHermes3Llama3170b = new ChatModel("nousresearch/hermes-3-llama-3.1-70b", "nousresearch/hermes-3-llama-3.1-70b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelHermes3Llama3170b"/>
    /// </summary>
    public readonly ChatModel Hermes3Llama3170b = ModelHermes3Llama3170b;

    /// <summary>
    /// nousresearch/hermes-4-405b
    /// </summary>
    public static readonly ChatModel ModelHermes4405b = new ChatModel("nousresearch/hermes-4-405b", "nousresearch/hermes-4-405b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelHermes4405b"/>
    /// </summary>
    public readonly ChatModel Hermes4405b = ModelHermes4405b;

    /// <summary>
    /// nousresearch/hermes-4-70b
    /// </summary>
    public static readonly ChatModel ModelHermes470b = new ChatModel("nousresearch/hermes-4-70b", "nousresearch/hermes-4-70b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelHermes470b"/>
    /// </summary>
    public readonly ChatModel Hermes470b = ModelHermes470b;

    /// <summary>
    /// nousresearch/hermes-2-pro-llama-3-8b
    /// </summary>
    public static readonly ChatModel ModelHermes2ProLlama38b = new ChatModel("nousresearch/hermes-2-pro-llama-3-8b", "nousresearch/hermes-2-pro-llama-3-8b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelHermes2ProLlama38b"/>
    /// </summary>
    public readonly ChatModel Hermes2ProLlama38b = ModelHermes2ProLlama38b;

    /// <summary>
    /// openai/chatgpt-4o-latest
    /// </summary>
    public static readonly ChatModel ModelChatgpt4oLatest = new ChatModel("openai/chatgpt-4o-latest", "openai/chatgpt-4o-latest", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelChatgpt4oLatest"/>
    /// </summary>
    public readonly ChatModel Chatgpt4oLatest = ModelChatgpt4oLatest;

    /// <summary>
    /// openai/codex-mini
    /// </summary>
    public static readonly ChatModel ModelCodexMini = new ChatModel("openai/codex-mini", "openai/codex-mini", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelCodexMini"/>
    /// </summary>
    public readonly ChatModel CodexMini = ModelCodexMini;

    /// <summary>
    /// openai/gpt-3.5-turbo
    /// </summary>
    public static readonly ChatModel ModelGpt35Turbo = new ChatModel("openai/gpt-3.5-turbo", "openai/gpt-3.5-turbo", LLmProviders.OpenRouter, 16385);

    /// <summary>
    /// <inheritdoc cref="ModelGpt35Turbo"/>
    /// </summary>
    public readonly ChatModel Gpt35Turbo = ModelGpt35Turbo;

    /// <summary>
    /// openai/gpt-3.5-turbo-0613
    /// </summary>
    public static readonly ChatModel ModelGpt35Turbo0613 = new ChatModel("openai/gpt-3.5-turbo-0613", "openai/gpt-3.5-turbo-0613", LLmProviders.OpenRouter, 4095);

    /// <summary>
    /// <inheritdoc cref="ModelGpt35Turbo0613"/>
    /// </summary>
    public readonly ChatModel Gpt35Turbo0613 = ModelGpt35Turbo0613;

    /// <summary>
    /// openai/gpt-3.5-turbo-16k
    /// </summary>
    public static readonly ChatModel ModelGpt35Turbo16k = new ChatModel("openai/gpt-3.5-turbo-16k", "openai/gpt-3.5-turbo-16k", LLmProviders.OpenRouter, 16385);

    /// <summary>
    /// <inheritdoc cref="ModelGpt35Turbo16k"/>
    /// </summary>
    public readonly ChatModel Gpt35Turbo16k = ModelGpt35Turbo16k;

    /// <summary>
    /// openai/gpt-3.5-turbo-instruct
    /// </summary>
    public static readonly ChatModel ModelGpt35TurboInstruct = new ChatModel("openai/gpt-3.5-turbo-instruct", "openai/gpt-3.5-turbo-instruct", LLmProviders.OpenRouter, 4095);

    /// <summary>
    /// <inheritdoc cref="ModelGpt35TurboInstruct"/>
    /// </summary>
    public readonly ChatModel Gpt35TurboInstruct = ModelGpt35TurboInstruct;

    /// <summary>
    /// openai/gpt-4
    /// </summary>
    public static readonly ChatModel ModelGpt4 = new ChatModel("openai/gpt-4", "openai/gpt-4", LLmProviders.OpenRouter, 8191);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4"/>
    /// </summary>
    public readonly ChatModel Gpt4 = ModelGpt4;

    /// <summary>
    /// openai/gpt-4-0314
    /// </summary>
    public static readonly ChatModel ModelGpt40314 = new ChatModel("openai/gpt-4-0314", "openai/gpt-4-0314", LLmProviders.OpenRouter, 8191);

    /// <summary>
    /// <inheritdoc cref="ModelGpt40314"/>
    /// </summary>
    public readonly ChatModel Gpt40314 = ModelGpt40314;

    /// <summary>
    /// openai/gpt-4-turbo
    /// </summary>
    public static readonly ChatModel ModelGpt4Turbo = new ChatModel("openai/gpt-4-turbo", "openai/gpt-4-turbo", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4Turbo"/>
    /// </summary>
    public readonly ChatModel Gpt4Turbo = ModelGpt4Turbo;

    /// <summary>
    /// openai/gpt-4-1106-preview
    /// </summary>
    public static readonly ChatModel ModelGpt41106Preview = new ChatModel("openai/gpt-4-1106-preview", "openai/gpt-4-1106-preview", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt41106Preview"/>
    /// </summary>
    public readonly ChatModel Gpt41106Preview = ModelGpt41106Preview;

    /// <summary>
    /// openai/gpt-4-turbo-preview
    /// </summary>
    public static readonly ChatModel ModelGpt4TurboPreview = new ChatModel("openai/gpt-4-turbo-preview", "openai/gpt-4-turbo-preview", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4TurboPreview"/>
    /// </summary>
    public readonly ChatModel Gpt4TurboPreview = ModelGpt4TurboPreview;

    /// <summary>
    /// openai/gpt-4.1
    /// </summary>
    public static readonly ChatModel ModelGpt41 = new ChatModel("openai/gpt-4.1", "openai/gpt-4.1", LLmProviders.OpenRouter, 1047576);

    /// <summary>
    /// <inheritdoc cref="ModelGpt41"/>
    /// </summary>
    public readonly ChatModel Gpt41 = ModelGpt41;

    /// <summary>
    /// openai/gpt-4.1-mini
    /// </summary>
    public static readonly ChatModel ModelGpt41Mini = new ChatModel("openai/gpt-4.1-mini", "openai/gpt-4.1-mini", LLmProviders.OpenRouter, 1047576);

    /// <summary>
    /// <inheritdoc cref="ModelGpt41Mini"/>
    /// </summary>
    public readonly ChatModel Gpt41Mini = ModelGpt41Mini;

    /// <summary>
    /// openai/gpt-4.1-nano
    /// </summary>
    public static readonly ChatModel ModelGpt41Nano = new ChatModel("openai/gpt-4.1-nano", "openai/gpt-4.1-nano", LLmProviders.OpenRouter, 1047576);

    /// <summary>
    /// <inheritdoc cref="ModelGpt41Nano"/>
    /// </summary>
    public readonly ChatModel Gpt41Nano = ModelGpt41Nano;

    /// <summary>
    /// openai/gpt-4o
    /// </summary>
    public static readonly ChatModel ModelGpt4o = new ChatModel("openai/gpt-4o", "openai/gpt-4o", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4o"/>
    /// </summary>
    public readonly ChatModel Gpt4o = ModelGpt4o;

    /// <summary>
    /// openai/gpt-4o-2024-05-13
    /// </summary>
    public static readonly ChatModel ModelGpt4o20240513 = new ChatModel("openai/gpt-4o-2024-05-13", "openai/gpt-4o-2024-05-13", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4o20240513"/>
    /// </summary>
    public readonly ChatModel Gpt4o20240513 = ModelGpt4o20240513;

    /// <summary>
    /// openai/gpt-4o-2024-08-06
    /// </summary>
    public static readonly ChatModel ModelGpt4o20240806 = new ChatModel("openai/gpt-4o-2024-08-06", "openai/gpt-4o-2024-08-06", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4o20240806"/>
    /// </summary>
    public readonly ChatModel Gpt4o20240806 = ModelGpt4o20240806;

    /// <summary>
    /// openai/gpt-4o-2024-11-20
    /// </summary>
    public static readonly ChatModel ModelGpt4o20241120 = new ChatModel("openai/gpt-4o-2024-11-20", "openai/gpt-4o-2024-11-20", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4o20241120"/>
    /// </summary>
    public readonly ChatModel Gpt4o20241120 = ModelGpt4o20241120;

    /// <summary>
    /// openai/gpt-4o:extended
    /// </summary>
    public static readonly ChatModel ModelGpt4oExtended = new ChatModel("openai/gpt-4o:extended", "openai/gpt-4o:extended", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4oExtended"/>
    /// </summary>
    public readonly ChatModel Gpt4oExtended = ModelGpt4oExtended;

    /// <summary>
    /// openai/gpt-4o-audio-preview
    /// </summary>
    public static readonly ChatModel ModelGpt4oAudioPreview = new ChatModel("openai/gpt-4o-audio-preview", "openai/gpt-4o-audio-preview", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4oAudioPreview"/>
    /// </summary>
    public readonly ChatModel Gpt4oAudioPreview = ModelGpt4oAudioPreview;

    /// <summary>
    /// openai/gpt-4o-search-preview
    /// </summary>
    public static readonly ChatModel ModelGpt4oSearchPreview = new ChatModel("openai/gpt-4o-search-preview", "openai/gpt-4o-search-preview", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4oSearchPreview"/>
    /// </summary>
    public readonly ChatModel Gpt4oSearchPreview = ModelGpt4oSearchPreview;

    /// <summary>
    /// openai/gpt-4o-mini
    /// </summary>
    public static readonly ChatModel ModelGpt4oMini = new ChatModel("openai/gpt-4o-mini", "openai/gpt-4o-mini", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4oMini"/>
    /// </summary>
    public readonly ChatModel Gpt4oMini = ModelGpt4oMini;

    /// <summary>
    /// openai/gpt-4o-mini-2024-07-18
    /// </summary>
    public static readonly ChatModel ModelGpt4oMini20240718 = new ChatModel("openai/gpt-4o-mini-2024-07-18", "openai/gpt-4o-mini-2024-07-18", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4oMini20240718"/>
    /// </summary>
    public readonly ChatModel Gpt4oMini20240718 = ModelGpt4oMini20240718;

    /// <summary>
    /// openai/gpt-4o-mini-search-preview
    /// </summary>
    public static readonly ChatModel ModelGpt4oMiniSearchPreview = new ChatModel("openai/gpt-4o-mini-search-preview", "openai/gpt-4o-mini-search-preview", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt4oMiniSearchPreview"/>
    /// </summary>
    public readonly ChatModel Gpt4oMiniSearchPreview = ModelGpt4oMiniSearchPreview;

    /// <summary>
    /// openai/gpt-5
    /// </summary>
    public static readonly ChatModel ModelGpt5 = new ChatModel("openai/gpt-5", "openai/gpt-5", LLmProviders.OpenRouter, 400000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt5"/>
    /// </summary>
    public readonly ChatModel Gpt5 = ModelGpt5;

    /// <summary>
    /// openai/gpt-5-chat
    /// </summary>
    public static readonly ChatModel ModelGpt5Chat = new ChatModel("openai/gpt-5-chat", "openai/gpt-5-chat", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt5Chat"/>
    /// </summary>
    public readonly ChatModel Gpt5Chat = ModelGpt5Chat;

    /// <summary>
    /// openai/gpt-5-codex
    /// </summary>
    public static readonly ChatModel ModelGpt5Codex = new ChatModel("openai/gpt-5-codex", "openai/gpt-5-codex", LLmProviders.OpenRouter, 400000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt5Codex"/>
    /// </summary>
    public readonly ChatModel Gpt5Codex = ModelGpt5Codex;

    /// <summary>
    /// openai/gpt-5-mini
    /// </summary>
    public static readonly ChatModel ModelGpt5Mini = new ChatModel("openai/gpt-5-mini", "openai/gpt-5-mini", LLmProviders.OpenRouter, 400000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt5Mini"/>
    /// </summary>
    public readonly ChatModel Gpt5Mini = ModelGpt5Mini;

    /// <summary>
    /// openai/gpt-5-nano
    /// </summary>
    public static readonly ChatModel ModelGpt5Nano = new ChatModel("openai/gpt-5-nano", "openai/gpt-5-nano", LLmProviders.OpenRouter, 400000);

    /// <summary>
    /// <inheritdoc cref="ModelGpt5Nano"/>
    /// </summary>
    public readonly ChatModel Gpt5Nano = ModelGpt5Nano;

    /// <summary>
    /// openai/gpt-oss-120b
    /// </summary>
    public static readonly ChatModel ModelGptOss120b = new ChatModel("openai/gpt-oss-120b", "openai/gpt-oss-120b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelGptOss120b"/>
    /// </summary>
    public readonly ChatModel GptOss120b = ModelGptOss120b;

    /// <summary>
    /// openai/gpt-oss-120b:free
    /// </summary>
    public static readonly ChatModel ModelGptOss120bFree = new ChatModel("openai/gpt-oss-120b:free", "openai/gpt-oss-120b:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelGptOss120bFree"/>
    /// </summary>
    public readonly ChatModel GptOss120bFree = ModelGptOss120bFree;

    /// <summary>
    /// openai/gpt-oss-20b
    /// </summary>
    public static readonly ChatModel ModelGptOss20b = new ChatModel("openai/gpt-oss-20b", "openai/gpt-oss-20b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelGptOss20b"/>
    /// </summary>
    public readonly ChatModel GptOss20b = ModelGptOss20b;

    /// <summary>
    /// openai/gpt-oss-20b:free
    /// </summary>
    public static readonly ChatModel ModelGptOss20bFree = new ChatModel("openai/gpt-oss-20b:free", "openai/gpt-oss-20b:free", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelGptOss20bFree"/>
    /// </summary>
    public readonly ChatModel GptOss20bFree = ModelGptOss20bFree;

    /// <summary>
    /// openai/o1
    /// </summary>
    public static readonly ChatModel ModelO1 = new ChatModel("openai/o1", "openai/o1", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelO1"/>
    /// </summary>
    public readonly ChatModel O1 = ModelO1;

    /// <summary>
    /// openai/o1-mini
    /// </summary>
    public static readonly ChatModel ModelO1Mini = new ChatModel("openai/o1-mini", "openai/o1-mini", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelO1Mini"/>
    /// </summary>
    public readonly ChatModel O1Mini = ModelO1Mini;

    /// <summary>
    /// openai/o1-mini-2024-09-12
    /// </summary>
    public static readonly ChatModel ModelO1Mini20240912 = new ChatModel("openai/o1-mini-2024-09-12", "openai/o1-mini-2024-09-12", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelO1Mini20240912"/>
    /// </summary>
    public readonly ChatModel O1Mini20240912 = ModelO1Mini20240912;

    /// <summary>
    /// openai/o1-pro
    /// </summary>
    public static readonly ChatModel ModelO1Pro = new ChatModel("openai/o1-pro", "openai/o1-pro", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelO1Pro"/>
    /// </summary>
    public readonly ChatModel O1Pro = ModelO1Pro;

    /// <summary>
    /// openai/o3
    /// </summary>
    public static readonly ChatModel ModelO3 = new ChatModel("openai/o3", "openai/o3", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelO3"/>
    /// </summary>
    public readonly ChatModel O3 = ModelO3;

    /// <summary>
    /// openai/o3-mini
    /// </summary>
    public static readonly ChatModel ModelO3Mini = new ChatModel("openai/o3-mini", "openai/o3-mini", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelO3Mini"/>
    /// </summary>
    public readonly ChatModel O3Mini = ModelO3Mini;

    /// <summary>
    /// openai/o3-mini-high
    /// </summary>
    public static readonly ChatModel ModelO3MiniHigh = new ChatModel("openai/o3-mini-high", "openai/o3-mini-high", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelO3MiniHigh"/>
    /// </summary>
    public readonly ChatModel O3MiniHigh = ModelO3MiniHigh;

    /// <summary>
    /// openai/o3-pro
    /// </summary>
    public static readonly ChatModel ModelO3Pro = new ChatModel("openai/o3-pro", "openai/o3-pro", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelO3Pro"/>
    /// </summary>
    public readonly ChatModel O3Pro = ModelO3Pro;

    /// <summary>
    /// openai/o4-mini
    /// </summary>
    public static readonly ChatModel ModelO4Mini = new ChatModel("openai/o4-mini", "openai/o4-mini", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelO4Mini"/>
    /// </summary>
    public readonly ChatModel O4Mini = ModelO4Mini;

    /// <summary>
    /// openai/o4-mini-high
    /// </summary>
    public static readonly ChatModel ModelO4MiniHigh = new ChatModel("openai/o4-mini-high", "openai/o4-mini-high", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelO4MiniHigh"/>
    /// </summary>
    public readonly ChatModel O4MiniHigh = ModelO4MiniHigh;

    /// <summary>
    /// opengvlab/internvl3-78b
    /// </summary>
    public static readonly ChatModel ModelInternvl378b = new ChatModel("opengvlab/internvl3-78b", "opengvlab/internvl3-78b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelInternvl378b"/>
    /// </summary>
    public readonly ChatModel Internvl378b = ModelInternvl378b;

    /// <summary>
    /// perplexity/r1-1776
    /// </summary>
    public static readonly ChatModel ModelR11776 = new ChatModel("perplexity/r1-1776", "perplexity/r1-1776", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelR11776"/>
    /// </summary>
    public readonly ChatModel R11776 = ModelR11776;

    /// <summary>
    /// perplexity/sonar
    /// </summary>
    public static readonly ChatModel ModelSonar = new ChatModel("perplexity/sonar", "perplexity/sonar", LLmProviders.OpenRouter, 127072);

    /// <summary>
    /// <inheritdoc cref="ModelSonar"/>
    /// </summary>
    public readonly ChatModel Sonar = ModelSonar;

    /// <summary>
    /// perplexity/sonar-deep-research
    /// </summary>
    public static readonly ChatModel ModelSonarDeepResearch = new ChatModel("perplexity/sonar-deep-research", "perplexity/sonar-deep-research", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelSonarDeepResearch"/>
    /// </summary>
    public readonly ChatModel SonarDeepResearch = ModelSonarDeepResearch;

    /// <summary>
    /// perplexity/sonar-pro
    /// </summary>
    public static readonly ChatModel ModelSonarPro = new ChatModel("perplexity/sonar-pro", "perplexity/sonar-pro", LLmProviders.OpenRouter, 200000);

    /// <summary>
    /// <inheritdoc cref="ModelSonarPro"/>
    /// </summary>
    public readonly ChatModel SonarPro = ModelSonarPro;

    /// <summary>
    /// perplexity/sonar-reasoning
    /// </summary>
    public static readonly ChatModel ModelSonarReasoning = new ChatModel("perplexity/sonar-reasoning", "perplexity/sonar-reasoning", LLmProviders.OpenRouter, 127000);

    /// <summary>
    /// <inheritdoc cref="ModelSonarReasoning"/>
    /// </summary>
    public readonly ChatModel SonarReasoning = ModelSonarReasoning;

    /// <summary>
    /// perplexity/sonar-reasoning-pro
    /// </summary>
    public static readonly ChatModel ModelSonarReasoningPro = new ChatModel("perplexity/sonar-reasoning-pro", "perplexity/sonar-reasoning-pro", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelSonarReasoningPro"/>
    /// </summary>
    public readonly ChatModel SonarReasoningPro = ModelSonarReasoningPro;

    /// <summary>
    /// qwen/qwen-2.5-72b-instruct
    /// </summary>
    public static readonly ChatModel ModelQwen2572bInstruct = new ChatModel("qwen/qwen-2.5-72b-instruct", "qwen/qwen-2.5-72b-instruct", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2572bInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen2572bInstruct = ModelQwen2572bInstruct;

    /// <summary>
    /// qwen/qwen-2.5-72b-instruct:free
    /// </summary>
    public static readonly ChatModel ModelQwen2572bInstructFree = new ChatModel("qwen/qwen-2.5-72b-instruct:free", "qwen/qwen-2.5-72b-instruct:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelQwen2572bInstructFree"/>
    /// </summary>
    public readonly ChatModel Qwen2572bInstructFree = ModelQwen2572bInstructFree;

    /// <summary>
    /// qwen/qwen-2.5-7b-instruct
    /// </summary>
    public static readonly ChatModel ModelQwen257bInstruct = new ChatModel("qwen/qwen-2.5-7b-instruct", "qwen/qwen-2.5-7b-instruct", LLmProviders.OpenRouter, 65536);

    /// <summary>
    /// <inheritdoc cref="ModelQwen257bInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen257bInstruct = ModelQwen257bInstruct;

    /// <summary>
    /// qwen/qwen-2.5-coder-32b-instruct
    /// </summary>
    public static readonly ChatModel ModelQwen25Coder32bInstruct = new ChatModel("qwen/qwen-2.5-coder-32b-instruct", "qwen/qwen-2.5-coder-32b-instruct", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelQwen25Coder32bInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen25Coder32bInstruct = ModelQwen25Coder32bInstruct;

    /// <summary>
    /// qwen/qwen-2.5-coder-32b-instruct:free
    /// </summary>
    public static readonly ChatModel ModelQwen25Coder32bInstructFree = new ChatModel("qwen/qwen-2.5-coder-32b-instruct:free", "qwen/qwen-2.5-coder-32b-instruct:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelQwen25Coder32bInstructFree"/>
    /// </summary>
    public readonly ChatModel Qwen25Coder32bInstructFree = ModelQwen25Coder32bInstructFree;

    /// <summary>
    /// qwen/qwq-32b
    /// </summary>
    public static readonly ChatModel ModelQwq32b = new ChatModel("qwen/qwq-32b", "qwen/qwq-32b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelQwq32b"/>
    /// </summary>
    public readonly ChatModel Qwq32b = ModelQwq32b;

    /// <summary>
    /// qwen/qwen-plus-2025-07-28
    /// </summary>
    public static readonly ChatModel ModelQwenPlus20250728 = new ChatModel("qwen/qwen-plus-2025-07-28", "qwen/qwen-plus-2025-07-28", LLmProviders.OpenRouter, 1000000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenPlus20250728"/>
    /// </summary>
    public readonly ChatModel QwenPlus20250728 = ModelQwenPlus20250728;

    /// <summary>
    /// qwen/qwen-plus-2025-07-28:thinking
    /// </summary>
    public static readonly ChatModel ModelQwenPlus20250728Thinking = new ChatModel("qwen/qwen-plus-2025-07-28:thinking", "qwen/qwen-plus-2025-07-28:thinking", LLmProviders.OpenRouter, 1000000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenPlus20250728Thinking"/>
    /// </summary>
    public readonly ChatModel QwenPlus20250728Thinking = ModelQwenPlus20250728Thinking;

    /// <summary>
    /// qwen/qwen-vl-max
    /// </summary>
    public static readonly ChatModel ModelQwenVlMax = new ChatModel("qwen/qwen-vl-max", "qwen/qwen-vl-max", LLmProviders.OpenRouter, 7500);

    /// <summary>
    /// <inheritdoc cref="ModelQwenVlMax"/>
    /// </summary>
    public readonly ChatModel QwenVlMax = ModelQwenVlMax;

    /// <summary>
    /// qwen/qwen-vl-plus
    /// </summary>
    public static readonly ChatModel ModelQwenVlPlus = new ChatModel("qwen/qwen-vl-plus", "qwen/qwen-vl-plus", LLmProviders.OpenRouter, 7500);

    /// <summary>
    /// <inheritdoc cref="ModelQwenVlPlus"/>
    /// </summary>
    public readonly ChatModel QwenVlPlus = ModelQwenVlPlus;

    /// <summary>
    /// qwen/qwen-max
    /// </summary>
    public static readonly ChatModel ModelQwenMax = new ChatModel("qwen/qwen-max", "qwen/qwen-max", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelQwenMax"/>
    /// </summary>
    public readonly ChatModel QwenMax = ModelQwenMax;

    /// <summary>
    /// qwen/qwen-plus
    /// </summary>
    public static readonly ChatModel ModelQwenPlus = new ChatModel("qwen/qwen-plus", "qwen/qwen-plus", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelQwenPlus"/>
    /// </summary>
    public readonly ChatModel QwenPlus = ModelQwenPlus;

    /// <summary>
    /// qwen/qwen-turbo
    /// </summary>
    public static readonly ChatModel ModelQwenTurbo = new ChatModel("qwen/qwen-turbo", "qwen/qwen-turbo", LLmProviders.OpenRouter, 1000000);

    /// <summary>
    /// <inheritdoc cref="ModelQwenTurbo"/>
    /// </summary>
    public readonly ChatModel QwenTurbo = ModelQwenTurbo;

    /// <summary>
    /// qwen/qwen2.5-vl-32b-instruct
    /// </summary>
    public static readonly ChatModel ModelQwen25Vl32bInstruct = new ChatModel("qwen/qwen2.5-vl-32b-instruct", "qwen/qwen2.5-vl-32b-instruct", LLmProviders.OpenRouter, 16384);

    /// <summary>
    /// <inheritdoc cref="ModelQwen25Vl32bInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen25Vl32bInstruct = ModelQwen25Vl32bInstruct;

    /// <summary>
    /// qwen/qwen2.5-vl-32b-instruct:free
    /// </summary>
    public static readonly ChatModel ModelQwen25Vl32bInstructFree = new ChatModel("qwen/qwen2.5-vl-32b-instruct:free", "qwen/qwen2.5-vl-32b-instruct:free", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelQwen25Vl32bInstructFree"/>
    /// </summary>
    public readonly ChatModel Qwen25Vl32bInstructFree = ModelQwen25Vl32bInstructFree;

    /// <summary>
    /// qwen/qwen2.5-vl-72b-instruct
    /// </summary>
    public static readonly ChatModel ModelQwen25Vl72bInstruct = new ChatModel("qwen/qwen2.5-vl-72b-instruct", "qwen/qwen2.5-vl-72b-instruct", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelQwen25Vl72bInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen25Vl72bInstruct = ModelQwen25Vl72bInstruct;

    /// <summary>
    /// qwen/qwen2.5-vl-72b-instruct:free
    /// </summary>
    public static readonly ChatModel ModelQwen25Vl72bInstructFree = new ChatModel("qwen/qwen2.5-vl-72b-instruct:free", "qwen/qwen2.5-vl-72b-instruct:free", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelQwen25Vl72bInstructFree"/>
    /// </summary>
    public readonly ChatModel Qwen25Vl72bInstructFree = ModelQwen25Vl72bInstructFree;

    /// <summary>
    /// qwen/qwen-2.5-vl-7b-instruct
    /// </summary>
    public static readonly ChatModel ModelQwen25Vl7bInstruct = new ChatModel("qwen/qwen-2.5-vl-7b-instruct", "qwen/qwen-2.5-vl-7b-instruct", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelQwen25Vl7bInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen25Vl7bInstruct = ModelQwen25Vl7bInstruct;

    /// <summary>
    /// qwen/qwen3-14b
    /// </summary>
    public static readonly ChatModel ModelQwen314b = new ChatModel("qwen/qwen3-14b", "qwen/qwen3-14b", LLmProviders.OpenRouter, 40960);

    /// <summary>
    /// <inheritdoc cref="ModelQwen314b"/>
    /// </summary>
    public readonly ChatModel Qwen314b = ModelQwen314b;

    /// <summary>
    /// qwen/qwen3-14b:free
    /// </summary>
    public static readonly ChatModel ModelQwen314bFree = new ChatModel("qwen/qwen3-14b:free", "qwen/qwen3-14b:free", LLmProviders.OpenRouter, 40960);

    /// <summary>
    /// <inheritdoc cref="ModelQwen314bFree"/>
    /// </summary>
    public readonly ChatModel Qwen314bFree = ModelQwen314bFree;

    /// <summary>
    /// qwen/qwen3-235b-a22b
    /// </summary>
    public static readonly ChatModel ModelQwen3235bA22b = new ChatModel("qwen/qwen3-235b-a22b", "qwen/qwen3-235b-a22b", LLmProviders.OpenRouter, 40960);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3235bA22b"/>
    /// </summary>
    public readonly ChatModel Qwen3235bA22b = ModelQwen3235bA22b;

    /// <summary>
    /// qwen/qwen3-235b-a22b:free
    /// </summary>
    public static readonly ChatModel ModelQwen3235bA22bFree = new ChatModel("qwen/qwen3-235b-a22b:free", "qwen/qwen3-235b-a22b:free", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3235bA22bFree"/>
    /// </summary>
    public readonly ChatModel Qwen3235bA22bFree = ModelQwen3235bA22bFree;

    /// <summary>
    /// qwen/qwen3-235b-a22b-2507
    /// </summary>
    public static readonly ChatModel ModelQwen3235bA22b2507 = new ChatModel("qwen/qwen3-235b-a22b-2507", "qwen/qwen3-235b-a22b-2507", LLmProviders.OpenRouter, 262144);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3235bA22b2507"/>
    /// </summary>
    public readonly ChatModel Qwen3235bA22b2507 = ModelQwen3235bA22b2507;

    /// <summary>
    /// qwen/qwen3-235b-a22b-thinking-2507
    /// </summary>
    public static readonly ChatModel ModelQwen3235bA22bThinking2507 = new ChatModel("qwen/qwen3-235b-a22b-thinking-2507", "qwen/qwen3-235b-a22b-thinking-2507", LLmProviders.OpenRouter, 262144);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3235bA22bThinking2507"/>
    /// </summary>
    public readonly ChatModel Qwen3235bA22bThinking2507 = ModelQwen3235bA22bThinking2507;

    /// <summary>
    /// qwen/qwen3-30b-a3b
    /// </summary>
    public static readonly ChatModel ModelQwen330bA3b = new ChatModel("qwen/qwen3-30b-a3b", "qwen/qwen3-30b-a3b", LLmProviders.OpenRouter, 40960);

    /// <summary>
    /// <inheritdoc cref="ModelQwen330bA3b"/>
    /// </summary>
    public readonly ChatModel Qwen330bA3b = ModelQwen330bA3b;

    /// <summary>
    /// qwen/qwen3-30b-a3b:free
    /// </summary>
    public static readonly ChatModel ModelQwen330bA3bFree = new ChatModel("qwen/qwen3-30b-a3b:free", "qwen/qwen3-30b-a3b:free", LLmProviders.OpenRouter, 40960);

    /// <summary>
    /// <inheritdoc cref="ModelQwen330bA3bFree"/>
    /// </summary>
    public readonly ChatModel Qwen330bA3bFree = ModelQwen330bA3bFree;

    /// <summary>
    /// qwen/qwen3-30b-a3b-instruct-2507
    /// </summary>
    public static readonly ChatModel ModelQwen330bA3bInstruct2507 = new ChatModel("qwen/qwen3-30b-a3b-instruct-2507", "qwen/qwen3-30b-a3b-instruct-2507", LLmProviders.OpenRouter, 262144);

    /// <summary>
    /// <inheritdoc cref="ModelQwen330bA3bInstruct2507"/>
    /// </summary>
    public readonly ChatModel Qwen330bA3bInstruct2507 = ModelQwen330bA3bInstruct2507;

    /// <summary>
    /// qwen/qwen3-30b-a3b-thinking-2507
    /// </summary>
    public static readonly ChatModel ModelQwen330bA3bThinking2507 = new ChatModel("qwen/qwen3-30b-a3b-thinking-2507", "qwen/qwen3-30b-a3b-thinking-2507", LLmProviders.OpenRouter, 262144);

    /// <summary>
    /// <inheritdoc cref="ModelQwen330bA3bThinking2507"/>
    /// </summary>
    public readonly ChatModel Qwen330bA3bThinking2507 = ModelQwen330bA3bThinking2507;

    /// <summary>
    /// qwen/qwen3-32b
    /// </summary>
    public static readonly ChatModel ModelQwen332b = new ChatModel("qwen/qwen3-32b", "qwen/qwen3-32b", LLmProviders.OpenRouter, 40960);

    /// <summary>
    /// <inheritdoc cref="ModelQwen332b"/>
    /// </summary>
    public readonly ChatModel Qwen332b = ModelQwen332b;

    /// <summary>
    /// qwen/qwen3-4b:free
    /// </summary>
    public static readonly ChatModel ModelQwen34bFree = new ChatModel("qwen/qwen3-4b:free", "qwen/qwen3-4b:free", LLmProviders.OpenRouter, 40960);

    /// <summary>
    /// <inheritdoc cref="ModelQwen34bFree"/>
    /// </summary>
    public readonly ChatModel Qwen34bFree = ModelQwen34bFree;

    /// <summary>
    /// qwen/qwen3-8b
    /// </summary>
    public static readonly ChatModel ModelQwen38b = new ChatModel("qwen/qwen3-8b", "qwen/qwen3-8b", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen38b"/>
    /// </summary>
    public readonly ChatModel Qwen38b = ModelQwen38b;

    /// <summary>
    /// qwen/qwen3-8b:free
    /// </summary>
    public static readonly ChatModel ModelQwen38bFree = new ChatModel("qwen/qwen3-8b:free", "qwen/qwen3-8b:free", LLmProviders.OpenRouter, 40960);

    /// <summary>
    /// <inheritdoc cref="ModelQwen38bFree"/>
    /// </summary>
    public readonly ChatModel Qwen38bFree = ModelQwen38bFree;

    /// <summary>
    /// qwen/qwen3-coder-30b-a3b-instruct
    /// </summary>
    public static readonly ChatModel ModelQwen3Coder30bA3bInstruct = new ChatModel("qwen/qwen3-coder-30b-a3b-instruct", "qwen/qwen3-coder-30b-a3b-instruct", LLmProviders.OpenRouter, 262144);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Coder30bA3bInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen3Coder30bA3bInstruct = ModelQwen3Coder30bA3bInstruct;

    /// <summary>
    /// qwen/qwen3-coder
    /// </summary>
    public static readonly ChatModel ModelQwen3Coder = new ChatModel("qwen/qwen3-coder", "qwen/qwen3-coder", LLmProviders.OpenRouter, 262144);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Coder"/>
    /// </summary>
    public readonly ChatModel Qwen3Coder = ModelQwen3Coder;

    /// <summary>
    /// qwen/qwen3-coder:free
    /// </summary>
    public static readonly ChatModel ModelQwen3CoderFree = new ChatModel("qwen/qwen3-coder:free", "qwen/qwen3-coder:free", LLmProviders.OpenRouter, 262144);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3CoderFree"/>
    /// </summary>
    public readonly ChatModel Qwen3CoderFree = ModelQwen3CoderFree;

    /// <summary>
    /// qwen/qwen3-coder-flash
    /// </summary>
    public static readonly ChatModel ModelQwen3CoderFlash = new ChatModel("qwen/qwen3-coder-flash", "qwen/qwen3-coder-flash", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3CoderFlash"/>
    /// </summary>
    public readonly ChatModel Qwen3CoderFlash = ModelQwen3CoderFlash;

    /// <summary>
    /// qwen/qwen3-coder-plus
    /// </summary>
    public static readonly ChatModel ModelQwen3CoderPlus = new ChatModel("qwen/qwen3-coder-plus", "qwen/qwen3-coder-plus", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3CoderPlus"/>
    /// </summary>
    public readonly ChatModel Qwen3CoderPlus = ModelQwen3CoderPlus;

    /// <summary>
    /// qwen/qwen3-max
    /// </summary>
    public static readonly ChatModel ModelQwen3Max = new ChatModel("qwen/qwen3-max", "qwen/qwen3-max", LLmProviders.OpenRouter, 256000);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Max"/>
    /// </summary>
    public readonly ChatModel Qwen3Max = ModelQwen3Max;

    /// <summary>
    /// qwen/qwen3-next-80b-a3b-instruct
    /// </summary>
    public static readonly ChatModel ModelQwen3Next80bA3bInstruct = new ChatModel("qwen/qwen3-next-80b-a3b-instruct", "qwen/qwen3-next-80b-a3b-instruct", LLmProviders.OpenRouter, 262144);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Next80bA3bInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen3Next80bA3bInstruct = ModelQwen3Next80bA3bInstruct;

    /// <summary>
    /// qwen/qwen3-next-80b-a3b-thinking
    /// </summary>
    public static readonly ChatModel ModelQwen3Next80bA3bThinking = new ChatModel("qwen/qwen3-next-80b-a3b-thinking", "qwen/qwen3-next-80b-a3b-thinking", LLmProviders.OpenRouter, 262144);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Next80bA3bThinking"/>
    /// </summary>
    public readonly ChatModel Qwen3Next80bA3bThinking = ModelQwen3Next80bA3bThinking;

    /// <summary>
    /// qwen/qwen3-vl-235b-a22b-instruct
    /// </summary>
    public static readonly ChatModel ModelQwen3Vl235bA22bInstruct = new ChatModel("qwen/qwen3-vl-235b-a22b-instruct", "qwen/qwen3-vl-235b-a22b-instruct", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Vl235bA22bInstruct"/>
    /// </summary>
    public readonly ChatModel Qwen3Vl235bA22bInstruct = ModelQwen3Vl235bA22bInstruct;

    /// <summary>
    /// qwen/qwen3-vl-235b-a22b-thinking
    /// </summary>
    public static readonly ChatModel ModelQwen3Vl235bA22bThinking = new ChatModel("qwen/qwen3-vl-235b-a22b-thinking", "qwen/qwen3-vl-235b-a22b-thinking", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelQwen3Vl235bA22bThinking"/>
    /// </summary>
    public readonly ChatModel Qwen3Vl235bA22bThinking = ModelQwen3Vl235bA22bThinking;

    /// <summary>
    /// undi95/remm-slerp-l2-13b
    /// </summary>
    public static readonly ChatModel ModelRemmSlerpL213b = new ChatModel("undi95/remm-slerp-l2-13b", "undi95/remm-slerp-l2-13b", LLmProviders.OpenRouter, 6144);

    /// <summary>
    /// <inheritdoc cref="ModelRemmSlerpL213b"/>
    /// </summary>
    public readonly ChatModel RemmSlerpL213b = ModelRemmSlerpL213b;

    /// <summary>
    /// sao10k/l3-lunaris-8b
    /// </summary>
    public static readonly ChatModel ModelL3Lunaris8b = new ChatModel("sao10k/l3-lunaris-8b", "sao10k/l3-lunaris-8b", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelL3Lunaris8b"/>
    /// </summary>
    public readonly ChatModel L3Lunaris8b = ModelL3Lunaris8b;

    /// <summary>
    /// sao10k/l3.1-euryale-70b
    /// </summary>
    public static readonly ChatModel ModelL31Euryale70b = new ChatModel("sao10k/l3.1-euryale-70b", "sao10k/l3.1-euryale-70b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelL31Euryale70b"/>
    /// </summary>
    public readonly ChatModel L31Euryale70b = ModelL31Euryale70b;

    /// <summary>
    /// sao10k/l3.3-euryale-70b
    /// </summary>
    public static readonly ChatModel ModelL33Euryale70b = new ChatModel("sao10k/l3.3-euryale-70b", "sao10k/l3.3-euryale-70b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelL33Euryale70b"/>
    /// </summary>
    public readonly ChatModel L33Euryale70b = ModelL33Euryale70b;

    /// <summary>
    /// sao10k/l3-euryale-70b
    /// </summary>
    public static readonly ChatModel ModelL3Euryale70b = new ChatModel("sao10k/l3-euryale-70b", "sao10k/l3-euryale-70b", LLmProviders.OpenRouter, 8192);

    /// <summary>
    /// <inheritdoc cref="ModelL3Euryale70b"/>
    /// </summary>
    public readonly ChatModel L3Euryale70b = ModelL3Euryale70b;

    /// <summary>
    /// shisa-ai/shisa-v2-llama3.3-70b
    /// </summary>
    public static readonly ChatModel ModelShisaV2Llama3370b = new ChatModel("shisa-ai/shisa-v2-llama3.3-70b", "shisa-ai/shisa-v2-llama3.3-70b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelShisaV2Llama3370b"/>
    /// </summary>
    public readonly ChatModel ShisaV2Llama3370b = ModelShisaV2Llama3370b;

    /// <summary>
    /// shisa-ai/shisa-v2-llama3.3-70b:free
    /// </summary>
    public static readonly ChatModel ModelShisaV2Llama3370bFree = new ChatModel("shisa-ai/shisa-v2-llama3.3-70b:free", "shisa-ai/shisa-v2-llama3.3-70b:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelShisaV2Llama3370bFree"/>
    /// </summary>
    public readonly ChatModel ShisaV2Llama3370bFree = ModelShisaV2Llama3370bFree;

    /// <summary>
    /// raifle/sorcererlm-8x22b
    /// </summary>
    public static readonly ChatModel ModelSorcererlm8x22b = new ChatModel("raifle/sorcererlm-8x22b", "raifle/sorcererlm-8x22b", LLmProviders.OpenRouter, 16000);

    /// <summary>
    /// <inheritdoc cref="ModelSorcererlm8x22b"/>
    /// </summary>
    public readonly ChatModel Sorcererlm8x22b = ModelSorcererlm8x22b;

    /// <summary>
    /// stepfun-ai/step3
    /// </summary>
    public static readonly ChatModel ModelStep3 = new ChatModel("stepfun-ai/step3", "stepfun-ai/step3", LLmProviders.OpenRouter, 65536);

    /// <summary>
    /// <inheritdoc cref="ModelStep3"/>
    /// </summary>
    public readonly ChatModel Step3 = ModelStep3;

    /// <summary>
    /// switchpoint/router
    /// </summary>
    public static readonly ChatModel ModelRouter = new ChatModel("switchpoint/router", "switchpoint/router", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelRouter"/>
    /// </summary>
    public readonly ChatModel Router = ModelRouter;

    /// <summary>
    /// thudm/glm-4.1v-9b-thinking
    /// </summary>
    public static readonly ChatModel ModelGlm41v9bThinking = new ChatModel("thudm/glm-4.1v-9b-thinking", "thudm/glm-4.1v-9b-thinking", LLmProviders.OpenRouter, 65536);

    /// <summary>
    /// <inheritdoc cref="ModelGlm41v9bThinking"/>
    /// </summary>
    public readonly ChatModel Glm41v9bThinking = ModelGlm41v9bThinking;

    /// <summary>
    /// thudm/glm-z1-32b
    /// </summary>
    public static readonly ChatModel ModelGlmZ132b = new ChatModel("thudm/glm-z1-32b", "thudm/glm-z1-32b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelGlmZ132b"/>
    /// </summary>
    public readonly ChatModel GlmZ132b = ModelGlmZ132b;

    /// <summary>
    /// tngtech/deepseek-r1t-chimera
    /// </summary>
    public static readonly ChatModel ModelDeepseekR1tChimera = new ChatModel("tngtech/deepseek-r1t-chimera", "tngtech/deepseek-r1t-chimera", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR1tChimera"/>
    /// </summary>
    public readonly ChatModel DeepseekR1tChimera = ModelDeepseekR1tChimera;

    /// <summary>
    /// tngtech/deepseek-r1t-chimera:free
    /// </summary>
    public static readonly ChatModel ModelDeepseekR1tChimeraFree = new ChatModel("tngtech/deepseek-r1t-chimera:free", "tngtech/deepseek-r1t-chimera:free", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR1tChimeraFree"/>
    /// </summary>
    public readonly ChatModel DeepseekR1tChimeraFree = ModelDeepseekR1tChimeraFree;

    /// <summary>
    /// tngtech/deepseek-r1t2-chimera:free
    /// </summary>
    public static readonly ChatModel ModelDeepseekR1t2ChimeraFree = new ChatModel("tngtech/deepseek-r1t2-chimera:free", "tngtech/deepseek-r1t2-chimera:free", LLmProviders.OpenRouter, 163840);

    /// <summary>
    /// <inheritdoc cref="ModelDeepseekR1t2ChimeraFree"/>
    /// </summary>
    public readonly ChatModel DeepseekR1t2ChimeraFree = ModelDeepseekR1t2ChimeraFree;

    /// <summary>
    /// tencent/hunyuan-a13b-instruct
    /// </summary>
    public static readonly ChatModel ModelHunyuanA13bInstruct = new ChatModel("tencent/hunyuan-a13b-instruct", "tencent/hunyuan-a13b-instruct", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelHunyuanA13bInstruct"/>
    /// </summary>
    public readonly ChatModel HunyuanA13bInstruct = ModelHunyuanA13bInstruct;

    /// <summary>
    /// tencent/hunyuan-a13b-instruct:free
    /// </summary>
    public static readonly ChatModel ModelHunyuanA13bInstructFree = new ChatModel("tencent/hunyuan-a13b-instruct:free", "tencent/hunyuan-a13b-instruct:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelHunyuanA13bInstructFree"/>
    /// </summary>
    public readonly ChatModel HunyuanA13bInstructFree = ModelHunyuanA13bInstructFree;

    /// <summary>
    /// thedrummer/anubis-70b-v1.1
    /// </summary>
    public static readonly ChatModel ModelAnubis70bV11 = new ChatModel("thedrummer/anubis-70b-v1.1", "thedrummer/anubis-70b-v1.1", LLmProviders.OpenRouter, 4096);

    /// <summary>
    /// <inheritdoc cref="ModelAnubis70bV11"/>
    /// </summary>
    public readonly ChatModel Anubis70bV11 = ModelAnubis70bV11;

    /// <summary>
    /// thedrummer/anubis-pro-105b-v1
    /// </summary>
    public static readonly ChatModel ModelAnubisPro105bV1 = new ChatModel("thedrummer/anubis-pro-105b-v1", "thedrummer/anubis-pro-105b-v1", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelAnubisPro105bV1"/>
    /// </summary>
    public readonly ChatModel AnubisPro105bV1 = ModelAnubisPro105bV1;

    /// <summary>
    /// thedrummer/rocinante-12b
    /// </summary>
    public static readonly ChatModel ModelRocinante12b = new ChatModel("thedrummer/rocinante-12b", "thedrummer/rocinante-12b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelRocinante12b"/>
    /// </summary>
    public readonly ChatModel Rocinante12b = ModelRocinante12b;

    /// <summary>
    /// thedrummer/skyfall-36b-v2
    /// </summary>
    public static readonly ChatModel ModelSkyfall36bV2 = new ChatModel("thedrummer/skyfall-36b-v2", "thedrummer/skyfall-36b-v2", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelSkyfall36bV2"/>
    /// </summary>
    public readonly ChatModel Skyfall36bV2 = ModelSkyfall36bV2;

    /// <summary>
    /// thedrummer/unslopnemo-12b
    /// </summary>
    public static readonly ChatModel ModelUnslopnemo12b = new ChatModel("thedrummer/unslopnemo-12b", "thedrummer/unslopnemo-12b", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelUnslopnemo12b"/>
    /// </summary>
    public readonly ChatModel Unslopnemo12b = ModelUnslopnemo12b;

    /// <summary>
    /// alibaba/tongyi-deepresearch-30b-a3b
    /// </summary>
    public static readonly ChatModel ModelTongyiDeepresearch30bA3b = new ChatModel("alibaba/tongyi-deepresearch-30b-a3b", "alibaba/tongyi-deepresearch-30b-a3b", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelTongyiDeepresearch30bA3b"/>
    /// </summary>
    public readonly ChatModel TongyiDeepresearch30bA3b = ModelTongyiDeepresearch30bA3b;

    /// <summary>
    /// cognitivecomputations/dolphin-mistral-24b-venice-edition:free
    /// </summary>
    public static readonly ChatModel ModelDolphinMistral24bVeniceEditionFree = new ChatModel("cognitivecomputations/dolphin-mistral-24b-venice-edition:free", "cognitivecomputations/dolphin-mistral-24b-venice-edition:free", LLmProviders.OpenRouter, 32768);

    /// <summary>
    /// <inheritdoc cref="ModelDolphinMistral24bVeniceEditionFree"/>
    /// </summary>
    public readonly ChatModel DolphinMistral24bVeniceEditionFree = ModelDolphinMistral24bVeniceEditionFree;

    /// <summary>
    /// microsoft/wizardlm-2-8x22b
    /// </summary>
    public static readonly ChatModel ModelWizardlm28x22b = new ChatModel("microsoft/wizardlm-2-8x22b", "microsoft/wizardlm-2-8x22b", LLmProviders.OpenRouter, 65536);

    /// <summary>
    /// <inheritdoc cref="ModelWizardlm28x22b"/>
    /// </summary>
    public readonly ChatModel Wizardlm28x22b = ModelWizardlm28x22b;

    /// <summary>
    /// z-ai/glm-4-32b
    /// </summary>
    public static readonly ChatModel ModelGlm432b = new ChatModel("z-ai/glm-4-32b", "z-ai/glm-4-32b", LLmProviders.OpenRouter, 128000);

    /// <summary>
    /// <inheritdoc cref="ModelGlm432b"/>
    /// </summary>
    public readonly ChatModel Glm432b = ModelGlm432b;

    /// <summary>
    /// z-ai/glm-4.5
    /// </summary>
    public static readonly ChatModel ModelGlm45 = new ChatModel("z-ai/glm-4.5", "z-ai/glm-4.5", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelGlm45"/>
    /// </summary>
    public readonly ChatModel Glm45 = ModelGlm45;

    /// <summary>
    /// z-ai/glm-4.5-air
    /// </summary>
    public static readonly ChatModel ModelGlm45Air = new ChatModel("z-ai/glm-4.5-air", "z-ai/glm-4.5-air", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelGlm45Air"/>
    /// </summary>
    public readonly ChatModel Glm45Air = ModelGlm45Air;

    /// <summary>
    /// z-ai/glm-4.5-air:free
    /// </summary>
    public static readonly ChatModel ModelGlm45AirFree = new ChatModel("z-ai/glm-4.5-air:free", "z-ai/glm-4.5-air:free", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelGlm45AirFree"/>
    /// </summary>
    public readonly ChatModel Glm45AirFree = ModelGlm45AirFree;

    /// <summary>
    /// z-ai/glm-4.5v
    /// </summary>
    public static readonly ChatModel ModelGlm45v = new ChatModel("z-ai/glm-4.5v", "z-ai/glm-4.5v", LLmProviders.OpenRouter, 65536);

    /// <summary>
    /// <inheritdoc cref="ModelGlm45v"/>
    /// </summary>
    public readonly ChatModel Glm45v = ModelGlm45v;

    /// <summary>
    /// x-ai/grok-3
    /// </summary>
    public static readonly ChatModel ModelGrok3 = new ChatModel("x-ai/grok-3", "x-ai/grok-3", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelGrok3"/>
    /// </summary>
    public readonly ChatModel Grok3 = ModelGrok3;

    /// <summary>
    /// x-ai/grok-3-beta
    /// </summary>
    public static readonly ChatModel ModelGrok3Beta = new ChatModel("x-ai/grok-3-beta", "x-ai/grok-3-beta", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelGrok3Beta"/>
    /// </summary>
    public readonly ChatModel Grok3Beta = ModelGrok3Beta;

    /// <summary>
    /// x-ai/grok-3-mini
    /// </summary>
    public static readonly ChatModel ModelGrok3Mini = new ChatModel("x-ai/grok-3-mini", "x-ai/grok-3-mini", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelGrok3Mini"/>
    /// </summary>
    public readonly ChatModel Grok3Mini = ModelGrok3Mini;

    /// <summary>
    /// x-ai/grok-3-mini-beta
    /// </summary>
    public static readonly ChatModel ModelGrok3MiniBeta = new ChatModel("x-ai/grok-3-mini-beta", "x-ai/grok-3-mini-beta", LLmProviders.OpenRouter, 131072);

    /// <summary>
    /// <inheritdoc cref="ModelGrok3MiniBeta"/>
    /// </summary>
    public readonly ChatModel Grok3MiniBeta = ModelGrok3MiniBeta;

    /// <summary>
    /// x-ai/grok-4
    /// </summary>
    public static readonly ChatModel ModelGrok4 = new ChatModel("x-ai/grok-4", "x-ai/grok-4", LLmProviders.OpenRouter, 256000);

    /// <summary>
    /// <inheritdoc cref="ModelGrok4"/>
    /// </summary>
    public readonly ChatModel Grok4 = ModelGrok4;

    /// <summary>
    /// x-ai/grok-4-fast
    /// </summary>
    public static readonly ChatModel ModelGrok4Fast = new ChatModel("x-ai/grok-4-fast", "x-ai/grok-4-fast", LLmProviders.OpenRouter, 2000000);

    /// <summary>
    /// <inheritdoc cref="ModelGrok4Fast"/>
    /// </summary>
    public readonly ChatModel Grok4Fast = ModelGrok4Fast;

    /// <summary>
    /// x-ai/grok-4-fast:free
    /// </summary>
    public static readonly ChatModel ModelGrok4FastFree = new ChatModel("x-ai/grok-4-fast:free", "x-ai/grok-4-fast:free", LLmProviders.OpenRouter, 2000000);

    /// <summary>
    /// <inheritdoc cref="ModelGrok4FastFree"/>
    /// </summary>
    public readonly ChatModel Grok4FastFree = ModelGrok4FastFree;

    /// <summary>
    /// x-ai/grok-code-fast-1
    /// </summary>
    public static readonly ChatModel ModelGrokCodeFast1 = new ChatModel("x-ai/grok-code-fast-1", "x-ai/grok-code-fast-1", LLmProviders.OpenRouter, 256000);

    /// <summary>
    /// <inheritdoc cref="ModelGrokCodeFast1"/>
    /// </summary>
    public readonly ChatModel GrokCodeFast1 = ModelGrokCodeFast1;

    /// <summary>
    /// All known models from Open Router.
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelJambaLarge17, ModelJambaMini17, ModelDeepcoder14bPreview, ModelDeepcoder14bPreviewFree, ModelAion10, ModelAion10Mini, ModelAionRpLlama318b, ModelCodellama7bInstructSolidity, ModelMolmo7bD, ModelOlmo2032532bInstruct, ModelNovaLiteV1, ModelNovaMicroV1, ModelNovaProV1, ModelClaude3Haiku, ModelClaude3Opus, ModelClaude35Haiku, ModelClaude35Haiku20241022, ModelClaude35Sonnet, ModelClaude35Sonnet20240620, ModelClaude37Sonnet, ModelClaude37SonnetThinking, ModelClaudeOpus4, ModelClaudeOpus41, ModelClaudeSonnet4, ModelAfm45b, ModelCoderLarge, ModelMaestroReasoning, ModelSpotlight, ModelVirtuosoLarge, ModelQwq32bArliaiRprV1, ModelQwq32bArliaiRprV1Free, ModelAuto, ModelErnie4521bA3b, ModelErnie45300bA47b, ModelErnie45Vl28bA3b, ModelErnie45Vl424bA47b, ModelSeedOss36bInstruct, ModelUiTars157b, ModelCogitoV2PreviewLlama109bMoe, ModelCommandA, ModelCommandR082024, ModelCommandRPlus082024, ModelCommandR7b122024, ModelCogitoV2PreviewDeepseek671b, ModelDeepseekProverV2, ModelDeepseekChat, ModelDeepseekChatV30324, ModelDeepseekChatV30324Free, ModelDeepseekChatV31, ModelDeepseekChatV31Free, ModelDeepseekV31Base, ModelDeepseekV31Terminus, ModelDeepseekR10528Qwen38b, ModelDeepseekR10528Qwen38bFree, ModelDeepseekR1, ModelDeepseekR1Free, ModelDeepseekR10528, ModelDeepseekR10528Free, ModelDeepseekR1DistillLlama70b, ModelDeepseekR1DistillLlama70bFree, ModelDeepseekR1DistillLlama8b, ModelDeepseekR1DistillQwen14b, ModelDeepseekR1DistillQwen32b, ModelDolphin30Mistral24b, ModelDolphin30Mistral24bFree, ModelDolphin30R1Mistral24b, ModelDolphin30R1Mistral24bFree, ModelLlemma7b, ModelGemini25FlashImagePreview, ModelGoliath120b, ModelGeminiFlash158b, ModelGemini20Flash001, ModelGemini20FlashExpFree, ModelGemini20FlashLite001, ModelGemini25Flash, ModelGemini25FlashLite, ModelGemini25FlashLitePreview0617, ModelGemini25FlashLitePreview092025, ModelGemini25FlashPreview092025, ModelGemini25Pro, ModelGemini25ProPreview0506, ModelGemini25ProPreview, ModelGemma227bIt, ModelGemma29bIt, ModelGemma29bItFree, ModelGemma312bIt, ModelGemma312bItFree, ModelGemma327bIt, ModelGemma327bItFree, ModelGemma34bIt, ModelGemma34bItFree, ModelGemma3nE2bItFree, ModelGemma3nE4bIt, ModelGemma3nE4bItFree, ModelMercury, ModelMercuryCoder, ModelInflection3Pi, ModelInflection3Productivity, ModelLfm3b, ModelLfm7b, ModelLlamaGuard38b, ModelMagnumV272b, ModelMagnumV472b, ModelWeaver, ModelLongcatFlashChat, ModelLlama370bInstruct, ModelLlama38bInstruct, ModelLlama31405b, ModelLlama31405bInstruct, ModelLlama3170bInstruct, ModelLlama318bInstruct, ModelLlama3211bVisionInstruct, ModelLlama321bInstruct, ModelLlama323bInstruct, ModelLlama323bInstructFree, ModelLlama3290bVisionInstruct, ModelLlama3370bInstruct, ModelLlama3370bInstructFree, ModelLlama338bInstructFree, ModelLlama4Maverick, ModelLlama4MaverickFree, ModelLlama4Scout, ModelLlama4ScoutFree, ModelLlamaGuard412b, ModelLlamaGuard28b, ModelMaiDsR1, ModelMaiDsR1Free, ModelPhi4, ModelPhi4MultimodalInstruct, ModelPhi4ReasoningPlus, ModelPhi3Medium128kInstruct, ModelPhi3Mini128kInstruct, ModelPhi35Mini128kInstruct, ModelMinimaxM1, ModelMinimax01, ModelMistralLarge, ModelMistralLarge2407, ModelMistralLarge2411, ModelMistralSmall, ModelMistralTiny, ModelCodestral2501, ModelCodestral2508, ModelDevstralMedium, ModelDevstralSmall, ModelDevstralSmall2505, ModelDevstralSmall2505Free, ModelMagistralMedium2506, ModelMagistralMedium2506Thinking, ModelMagistralSmall2506, ModelMinistral3b, ModelMinistral8b, ModelMistral7bInstruct, ModelMistral7bInstructFree, ModelMistral7bInstructV01, ModelMistral7bInstructV03, ModelMistralMedium3, ModelMistralMedium31, ModelMistralNemo, ModelMistralNemoFree, ModelMistralSmall24bInstruct2501, ModelMistralSmall24bInstruct2501Free, ModelMistralSmall3124bInstruct, ModelMistralSmall3124bInstructFree, ModelMistralSmall3224bInstruct, ModelMistralSmall3224bInstructFree, ModelMixtral8x22bInstruct, ModelMixtral8x7bInstruct, ModelPixtral12b, ModelPixtralLarge2411, ModelMistralSaba, ModelKimiDev72b, ModelKimiDev72bFree, ModelKimiK2, ModelKimiK2Free, ModelKimiK20905, ModelKimiVlA3bThinking, ModelKimiVlA3bThinkingFree, ModelMorphV3Fast, ModelMorphV3Large, ModelMythomaxL213b, ModelLlama31Nemotron70bInstruct, ModelLlama31NemotronUltra253bV1, ModelNemotronNano9bV2, ModelNemotronNano9bV2Free, ModelLlama3Lumimaid70b, ModelLlama31Lumimaid8b, ModelNoromaid20b, ModelDeephermes3Llama38bPreview, ModelDeephermes3Llama38bPreviewFree, ModelDeephermes3Mistral24bPreview, ModelHermes3Llama31405b, ModelHermes3Llama3170b, ModelHermes4405b, ModelHermes470b, ModelHermes2ProLlama38b, ModelChatgpt4oLatest, ModelCodexMini, ModelGpt35Turbo, ModelGpt35Turbo0613, ModelGpt35Turbo16k, ModelGpt35TurboInstruct, ModelGpt4, ModelGpt40314, ModelGpt4Turbo, ModelGpt41106Preview, ModelGpt4TurboPreview, ModelGpt41, ModelGpt41Mini, ModelGpt41Nano, ModelGpt4o, ModelGpt4o20240513, ModelGpt4o20240806, ModelGpt4o20241120, ModelGpt4oExtended, ModelGpt4oAudioPreview, ModelGpt4oSearchPreview, ModelGpt4oMini, ModelGpt4oMini20240718, ModelGpt4oMiniSearchPreview, ModelGpt5, ModelGpt5Chat, ModelGpt5Codex, ModelGpt5Mini, ModelGpt5Nano, ModelGptOss120b, ModelGptOss120bFree, ModelGptOss20b, ModelGptOss20bFree, ModelO1, ModelO1Mini, ModelO1Mini20240912, ModelO1Pro, ModelO3, ModelO3Mini, ModelO3MiniHigh, ModelO3Pro, ModelO4Mini, ModelO4MiniHigh, ModelInternvl378b, ModelR11776, ModelSonar, ModelSonarDeepResearch, ModelSonarPro, ModelSonarReasoning, ModelSonarReasoningPro, ModelQwen2572bInstruct, ModelQwen2572bInstructFree, ModelQwen257bInstruct, ModelQwen25Coder32bInstruct, ModelQwen25Coder32bInstructFree, ModelQwq32b, ModelQwenPlus20250728, ModelQwenPlus20250728Thinking, ModelQwenVlMax, ModelQwenVlPlus, ModelQwenMax, ModelQwenPlus, ModelQwenTurbo, ModelQwen25Vl32bInstruct, ModelQwen25Vl32bInstructFree, ModelQwen25Vl72bInstruct, ModelQwen25Vl72bInstructFree, ModelQwen25Vl7bInstruct, ModelQwen314b, ModelQwen314bFree, ModelQwen3235bA22b, ModelQwen3235bA22bFree, ModelQwen3235bA22b2507, ModelQwen3235bA22bThinking2507, ModelQwen330bA3b, ModelQwen330bA3bFree, ModelQwen330bA3bInstruct2507, ModelQwen330bA3bThinking2507, ModelQwen332b, ModelQwen34bFree, ModelQwen38b, ModelQwen38bFree, ModelQwen3Coder30bA3bInstruct, ModelQwen3Coder, ModelQwen3CoderFree, ModelQwen3CoderFlash, ModelQwen3CoderPlus, ModelQwen3Max, ModelQwen3Next80bA3bInstruct, ModelQwen3Next80bA3bThinking, ModelQwen3Vl235bA22bInstruct, ModelQwen3Vl235bA22bThinking, ModelRemmSlerpL213b, ModelL3Lunaris8b, ModelL31Euryale70b, ModelL33Euryale70b, ModelL3Euryale70b, ModelShisaV2Llama3370b, ModelShisaV2Llama3370bFree, ModelSorcererlm8x22b, ModelStep3, ModelRouter, ModelGlm41v9bThinking, ModelGlmZ132b, ModelDeepseekR1tChimera, ModelDeepseekR1tChimeraFree, ModelDeepseekR1t2ChimeraFree, ModelHunyuanA13bInstruct, ModelHunyuanA13bInstructFree, ModelAnubis70bV11, ModelAnubisPro105bV1, ModelRocinante12b, ModelSkyfall36bV2, ModelUnslopnemo12b, ModelTongyiDeepresearch30bA3b, ModelDolphinMistral24bVeniceEditionFree, ModelWizardlm28x22b, ModelGlm432b, ModelGlm45, ModelGlm45Air, ModelGlm45AirFree, ModelGlm45v, ModelGrok3, ModelGrok3Beta, ModelGrok3Mini, ModelGrok3MiniBeta, ModelGrok4, ModelGrok4Fast, ModelGrok4FastFree, ModelGrokCodeFast1]);

    /// <summary>
    /// <inheritdoc cref="ModelsAll"/>
    /// </summary>
    public List<IModel> AllModels => ModelsAll;

    internal ChatModelOpenRouterAll()
    {

    }
}