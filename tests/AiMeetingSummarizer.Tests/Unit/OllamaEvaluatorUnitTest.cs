// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using AiMeetingSummarizer.Infrastructure.Ollama;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AiMeetingSummarizer.Tests.Unit;

public sealed class OllamaEvaluatorUnitTest
{
    private const string AnySystemPrompt = "You are an evaluator.";

    private readonly Mock<IOllamaApiClient> _apiClient = new(MockBehavior.Strict);
    private readonly OllamaEvaluator _sut;

    public OllamaEvaluatorUnitTest()
    {
        _sut = new OllamaEvaluator(_apiClient.Object, AnySystemPrompt, NullLogger<OllamaEvaluator>.Instance);
    }

    [Fact]
    public void ParseEvaluationResponse_MapsAllCriteriaCorrectly_WhenJsonIsValid()
    {
        // Deleting any field assignment in MapToEvaluationResult makes this test fail.
        var json = """
                   {
                     "completeness":           { "score": 2, "comment": "All covered" },
                     "accuracy":               { "score": 1, "comment": "Minor issues" },
                     "structure_compliance":   { "score": 2, "comment": "Perfect" },
                     "action_item_extraction": { "score": 1, "comment": "Some missing" },
                     "clarity":                { "score": 2, "comment": "Very clear" },
                     "overall_comment": "Good summary overall.",
                     "improvements": ["Add more detail", "Clarify blockers"]
                   }
                   """;

        var result = _sut.ParseEvaluationResponse(json);

        result.Completeness.Score.Should().Be(2);
        result.Accuracy.Score.Should().Be(1);
        result.StructureCompliance.Score.Should().Be(2);
        result.ActionItemExtraction.Score.Should().Be(1);
        result.Clarity.Score.Should().Be(2);
        result.OverallComment.Should().Be("Good summary overall.");
        result.Improvements.Should().Equal("Add more detail", "Clarify blockers");
    }

    [Fact]
    public void ParseEvaluationResponse_ReturnsFallback_WhenJsonIsCompletelyInvalid()
    {
        // Deleting the `if (parsed is null)` fallback branch makes this test fail.
        var result = _sut.ParseEvaluationResponse("This is not JSON at all.");

        result.TotalScore.Should().Be(0);
        result.OverallComment.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ParseEvaluationResponse_ExtractsJsonFromWrappedText()
    {
        // Deleting ExtractJsonBlock (the regex fallback) makes this test fail.
        // LLMs sometimes add preamble before the JSON block.
        var response = "Here is the evaluation:\n" + BuildJsonWithAllScores(1) + "\nEnd.";

        var result = _sut.ParseEvaluationResponse(response);

        result.TotalScore.Should().Be(5);
    }

    [Fact]
    public void ParseEvaluationResponse_ClampsScoreAboveMax_ToMaximum()
    {
        // Deleting `Math.Clamp(...)` in ToScore makes this test fail.
        var json = """
                   {
                     "completeness":           { "score": 99, "comment": "Over the top" },
                     "accuracy":               { "score": 2,  "comment": "Ok" },
                     "structure_compliance":   { "score": 2,  "comment": "Ok" },
                     "action_item_extraction": { "score": 2,  "comment": "Ok" },
                     "clarity":               { "score": 2,  "comment": "Ok" },
                     "overall_comment": "Test",
                     "improvements": []
                   }
                   """;

        var result = _sut.ParseEvaluationResponse(json);

        result.Completeness.Score.Should().Be(2, "scores must be clamped to their maximum of 2");
    }

    [Fact]
    public async Task EvaluateAsync_ThrowsArgumentException_WhenTranscriptIsEmpty()
    {
        // Deleting the transcript IsNullOrWhiteSpace guard makes this test fail.
        var act = async () => await _sut.EvaluateAsync(string.Empty, "some summary");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("transcript");
    }

    [Fact]
    public async Task EvaluateAsync_ThrowsArgumentException_WhenSummaryIsEmpty()
    {
        // Deleting the summary IsNullOrWhiteSpace guard makes this test fail.
        var act = async () => await _sut.EvaluateAsync("some transcript", string.Empty);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("summary");
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsParsedEvaluationResult_WhenApiClientReturnsValidJson()
    {
        // Deleting ParseEvaluationResponse call (or the await) makes this test fail.
        SetupApiClient(BuildJsonWithAllScores(2));

        var result = await _sut.EvaluateAsync("transcript", "summary");

        result.TotalScore.Should().Be(10);
    }

    [Fact]
    public async Task EvaluateAsync_PassesInjectedSystemPromptToApiClient()
    {
        // Deleting `_systemPrompt` field and passing empty string to GenerateAsync makes this test fail.
        const string expectedSystemPrompt = "Custom injected evaluator prompt.";
        var sut = new OllamaEvaluator(
            _apiClient.Object, expectedSystemPrompt, NullLogger<OllamaEvaluator>.Instance);

        string? capturedSystem = null;
        _apiClient
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((sys, _, _) => capturedSystem = sys)
            .ReturnsAsync(BuildJsonWithAllScores(1));

        await sut.EvaluateAsync("transcript", "summary");

        capturedSystem.Should().Be(expectedSystemPrompt);
    }

    [Fact]
    public async Task EvaluateAsync_TranscriptAndSummaryArePassedInUserPrompt()
    {
        // Deleting the userPrompt variable and inlining data into system argument makes this test fail.
        const string transcript = "John: This specific transcript text.";
        const string summary = "Participants: John\nSummary: Discussed the API.";
        string? capturedUserPrompt = null;

        _apiClient
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, user, _) => capturedUserPrompt = user)
            .ReturnsAsync(BuildJsonWithAllScores(1));

        await _sut.EvaluateAsync(transcript, summary);

        capturedUserPrompt.Should().Contain(transcript);
        capturedUserPrompt.Should().Contain(summary);
    }

    private void SetupApiClient(string response)
    {
        _apiClient
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private static string BuildJsonWithAllScores(int score) =>
        $$"""
          {
            "completeness":           { "score": {{score}}, "comment": "comment" },
            "accuracy":               { "score": {{score}}, "comment": "comment" },
            "structure_compliance":   { "score": {{score}}, "comment": "comment" },
            "action_item_extraction": { "score": {{score}}, "comment": "comment" },
            "clarity":                { "score": {{score}}, "comment": "comment" },
            "overall_comment": "Overall comment.",
            "improvements": []
          }
          """;
}