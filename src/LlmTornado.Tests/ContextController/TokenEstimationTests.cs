using LlmTornado.Agents.Samples.ContextController;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using NUnit.Framework;

namespace LlmTornado.Tests.ContextController;

/// <summary>
/// Unit tests for TokenEstimator utility class.
/// </summary>
[TestFixture]
public class TokenEstimationTests
{
    [Test]
    public void EstimateTokens_EmptyString_ReturnsZero()
    {
        // Arrange
        string text = string.Empty;

        // Act
        int tokens = TokenEstimator.EstimateTokens(text);

        // Assert
        Assert.That(tokens, Is.EqualTo(0));
    }

    [Test]
    public void EstimateTokens_SimpleText_ReturnsCorrectEstimate()
    {
        // Arrange
        string text = "This is a test message";
        // Expected: 22 characters / 4 = ~5.5 -> 6 tokens

        // Act
        int tokens = TokenEstimator.EstimateTokens(text);

        // Assert
        Assert.That(tokens, Is.EqualTo(6));
    }

    [Test]
    public void EstimateTokens_ExactMultipleOfFour_ReturnsCorrectEstimate()
    {
        // Arrange
        string text = "Test"; // 4 characters = 1 token

        // Act
        int tokens = TokenEstimator.EstimateTokens(text);

        // Assert
        Assert.That(tokens, Is.EqualTo(1));
    }

    [Test]
    public void EstimateTokens_LargeText_ReturnsCorrectEstimate()
    {
        // Arrange
        string text = new string('a', 10000); // 10000 characters = 2500 tokens

        // Act
        int tokens = TokenEstimator.EstimateTokens(text);

        // Assert
        Assert.That(tokens, Is.EqualTo(2500));
    }

    [Test]
    public void EstimateTokens_NullString_ReturnsZero()
    {
        // Arrange
        string? text = null;

        // Act
        int tokens = TokenEstimator.EstimateTokens(text);

        // Assert
        Assert.That(tokens, Is.EqualTo(0));
    }

    [Test]
    public void EstimateTokens_Message_WithTextContent_ReturnsCorrectEstimate()
    {
        // Arrange
        var message = new ChatMessage(ChatMessageRoles.User, "Hello world");
        // "Hello world" = 11 characters / 4 = ~2.75 -> 3 tokens

        // Act
        int tokens = TokenEstimator.EstimateTokens(message);

        // Assert
        Assert.That(tokens, Is.EqualTo(3));
    }

    [Test]
    public void EstimateTokens_Message_EmptyContent_ReturnsZero()
    {
        // Arrange
        var message = new ChatMessage(ChatMessageRoles.User, string.Empty);

        // Act
        int tokens = TokenEstimator.EstimateTokens(message);

        // Assert
        Assert.That(tokens, Is.EqualTo(0));
    }

    [Test]
    public void GetContextWindowSize_Gpt4_ReturnsCorrectSize()
    {
        // Arrange
        var model = ChatModel.OpenAi.Gpt41.V41Mini;

        // Act
        int contextWindow = TokenEstimator.GetContextWindowSize(model);

        // Assert
        Assert.That(contextWindow, Is.GreaterThan(0));
    }

    [Test]
    public void GetContextWindowSize_Gpt35Turbo_ReturnsCorrectSize()
    {
        // Arrange
        var model = ChatModel.OpenAi.Gpt35.Turbo;

        // Act
        int contextWindow = TokenEstimator.GetContextWindowSize(model);

        // Assert
        Assert.That(contextWindow, Is.EqualTo(16385));
    }

    [Test]
    public void GetContextWindowSize_UnknownModel_ReturnsDefaultSize()
    {
        // Arrange
        var model = new ChatModel("unknown", LLmProviders.OpenAi, 0);

        // Act
        int contextWindow = TokenEstimator.GetContextWindowSize(model);

        // Assert
        Assert.That(contextWindow, Is.EqualTo(128000)); // Updated to match DEFAULT_CONTEXT_WINDOW
    }

    [Test]
    public void CalculateUtilization_HalfUsed_ReturnsPointFive()
    {
        // Arrange
        int usedTokens = 5000;
        int totalTokens = 10000;

        // Act
        double utilization = TokenEstimator.CalculateUtilization(usedTokens, totalTokens);

        // Assert
        Assert.That(utilization, Is.EqualTo(0.5).Within(0.001));
    }

    [Test]
    public void CalculateUtilization_ZeroUsed_ReturnsZero()
    {
        // Arrange
        int usedTokens = 0;
        int totalTokens = 10000;

        // Act
        double utilization = TokenEstimator.CalculateUtilization(usedTokens, totalTokens);

        // Assert
        Assert.That(utilization, Is.EqualTo(0.0));
    }

    [Test]
    public void CalculateUtilization_FullyUsed_ReturnsOne()
    {
        // Arrange
        int usedTokens = 10000;
        int totalTokens = 10000;

        // Act
        double utilization = TokenEstimator.CalculateUtilization(usedTokens, totalTokens);

        // Assert
        Assert.That(utilization, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateUtilization_OverUsed_ReturnsOverOne()
    {
        // Arrange
        int usedTokens = 15000;
        int totalTokens = 10000;

        // Act
        double utilization = TokenEstimator.CalculateUtilization(usedTokens, totalTokens);

        // Assert
        Assert.That(utilization, Is.EqualTo(1.5).Within(0.001));
    }

    [Test]
    public void CalculateUtilization_ZeroTotal_ReturnsZero()
    {
        // Arrange
        int usedTokens = 100;
        int totalTokens = 0;

        // Act
        double utilization = TokenEstimator.CalculateUtilization(usedTokens, totalTokens);

        // Assert
        Assert.That(utilization, Is.EqualTo(0.0));
    }

    [Test]
    public void EstimateTokens_MultilineMessage_ReturnsCorrectEstimate()
    {
        // Arrange
        string text = @"Line 1
Line 2
Line 3";
        int expectedLength = text.Length; // Includes newline characters
        int expectedTokens = (int)Math.Ceiling(expectedLength / 4.0);

        // Act
        int tokens = TokenEstimator.EstimateTokens(text);

        // Assert
        Assert.That(tokens, Is.EqualTo(expectedTokens));
    }
}
