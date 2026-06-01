using System;
using LlmTornado.Images.Models;

namespace LlmTornado.Images;

/// <summary>
/// Helpers for GPT Image model families, including gpt-image-2-specific API constraints.
/// </summary>
internal static class GptImageModelHelper
{
	/// <summary>
	/// Returns true for gpt-image-* and chatgpt-image-* models.
	/// </summary>
	public static bool IsGptImageModel(ImageModel? model)
	{
		string? name = model?.GetApiName;
		return name is not null && (name.StartsWith("gpt-image", StringComparison.OrdinalIgnoreCase) || name.StartsWith("chatgpt-image", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Returns true for gpt-image-2 and dated snapshots (e.g. gpt-image-2-2026-04-21).
	/// </summary>
	public static bool IsGptImage2Model(ImageModel? model)
	{
		string? name = model?.GetApiName;
		return name is not null && name.StartsWith("gpt-image-2", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Applies gpt-image-2 rules: no response_format, no transparent background.
	/// </summary>
	public static ImageGenerationRequest SanitizeGenerationRequest(ImageGenerationRequest request)
	{
		bool stripResponseFormat = IsGptImageModel(request.Model) && request.ResponseFormat is not null;
		bool stripTransparent = IsGptImage2Model(request.Model) && request.Background == ImageBackgroundTypes.Transparent;

		if (!stripResponseFormat && !stripTransparent)
		{
			return request;
		}

		ImageGenerationRequest clone = new ImageGenerationRequest(request);

		if (stripResponseFormat)
		{
			clone.ResponseFormat = null;
		}

		if (stripTransparent)
		{
			clone.Background = null;
		}

		return clone;
	}

	/// <summary>
	/// Applies gpt-image-2 rules: input_fidelity must be omitted (high fidelity is automatic).
	/// Transparent backgrounds are not supported.
	/// </summary>
	public static ImageEditRequest SanitizeEditRequest(ImageEditRequest request)
	{
		bool stripInputFidelity = IsGptImage2Model(request.Model) && request.InputFidelity is not null;
		bool stripTransparent = IsGptImage2Model(request.Model) && request.Background == TornadoImageBackgrounds.Transparent;

		if (!stripInputFidelity && !stripTransparent)
		{
			return request;
		}

		ImageEditRequest clone = new ImageEditRequest(request);

		if (stripInputFidelity)
		{
			clone.InputFidelity = null;
		}

		if (stripTransparent)
		{
			clone.Background = null;
		}

		return clone;
	}
}
