using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Code.Models;

namespace LlmTornado.Ocr.Models.Mistral;

/// <summary>
/// Known OCR models from Mistral.
/// </summary>
public class OcrModelMistral : BaseVendorModelProvider
{
    /// <inheritdoc cref="BaseVendorModelProvider.Provider"/>
    public override LLmProviders Provider => LLmProviders.Mistral;

    /// <summary>
    /// OCR 3 (mistral-ocr-2512) - Released December 2024.
    /// Features: table_format (markdown/html), extract_header, extract_footer, hyperlinks output.
    /// </summary>
    public static readonly OcrModel ModelOcr2512 = new OcrModel("mistral-ocr-2512", LLmProviders.Mistral, ["mistral-ocr-latest"]);

    /// <summary>
    /// <inheritdoc cref="ModelOcr2512"/>
    /// </summary>
    public readonly OcrModel Ocr2512 = ModelOcr2512;

    /// <summary>
    /// <inheritdoc cref="ModelOcr2512"/>
    /// </summary>
    public readonly OcrModel Ocr3 = ModelOcr2512;

    /// <summary>
    /// Latest OCR model (alias for mistral-ocr-2512).
    /// </summary>
    public static readonly OcrModel ModelOcrLatest = new OcrModel("mistral-ocr-latest", LLmProviders.Mistral);

    /// <summary>
    /// <inheritdoc cref="ModelOcrLatest"/>
    /// </summary>
    public readonly OcrModel Latest = ModelOcrLatest;

    /// <summary>
    /// All known OCR models from Mistral.
    /// </summary>
    public override List<IModel> AllModels => ModelsAll;

    /// <summary>
    /// Checks whether the model is owned by the provider.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public override bool OwnsModel(string model)
    {
        return AllModelsMap.Contains(model);
    }

    /// <summary>
    /// Map of models owned by the provider.
    /// </summary>
    public static HashSet<string> AllModelsMap => LazyAllModelsMap.Value;

    private static readonly Lazy<HashSet<string>> LazyAllModelsMap = new Lazy<HashSet<string>>(() =>
    {
        HashSet<string> map = [];
        ModelsAll.ForEach(x => { map.Add(x.Name); });
        return map;
    });

    /// <summary>
    /// <inheritdoc cref="AllModels"/>
    /// </summary>
    public static List<IModel> ModelsAll => LazyModelsAll.Value;

    private static readonly Lazy<List<IModel>> LazyModelsAll = new Lazy<List<IModel>>(() => [ModelOcr2512, ModelOcrLatest]);

    internal OcrModelMistral()
    {
    }
}
