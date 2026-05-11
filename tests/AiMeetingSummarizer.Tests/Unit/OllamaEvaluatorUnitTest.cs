// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

using AiMeetingSummarizer.Infrastructure.Ollama;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AiMeetingSummarizer.Tests.Unit;

public sealed class OllamaEvaluatorUnitTest
{
    private readonly Mock<IOllamaApiClient> _apiClient = new(MockBehavior.Strict);
    private readonly OllamaEvaluator _sut;

    public OllamaEvaluatorUnitTest()
    {
        _sut = new OllamaEvaluator(_apiClient.Object, NullLogger<OllamaEvaluator>.Instance);
    }

    [Fact]
    public void ParseEvaluationResponse_MapsAllCriteriaCorrectly_WhenJsonIsValid()
    {
        var json = """
                   {
                     "completeness":          { "score": 2, "comment": "All covered" },
                     "accuracy":              { "score": 1, "comment": "Minor issues" },
                     "structure_compliance":  { "score": 2, "comment": "Perfect" },
                     "action_item_extraction":{ "score": 1, "comment": "Some missing" },
                     "clarity":               { "score": 2, "comment": "Very clear" },
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
        result.TotalScore.Should().Be(8);
        result.OverallComment.Should().Be("Good summary overall.");
        result.Improvements.Should().Equal("Add more detail", "Clarify blockers");
    }

    [Fact]
    public void ParseEvaluationResponse_ComputesGrade_BasedOnTotalScore()
    {
        var json = BuildJsonWithAllScores(2); // 10/10

        var result = _sut.ParseEvaluationResponse(json);

        result.Grade.Should().Be("Excellent");
    }

    [Fact]
    public void ParseEvaluationResponse_ReturnsFallback_WhenJsonIsCompletelyInvalid()
    {
        var result = _sut.ParseEvaluationResponse("This is not JSON at all.");

        result.TotalScore.Should().Be(0);
        result.OverallComment.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ParseEvaluationResponse_ExtractsJsonFromWrappedText()
    {
        // LLMs sometimes add preamble before the JSON block
        var response = "Here is the evaluation:\n" + BuildJsonWithAllScores(1) + "\nEnd.";

        var result = _sut.ParseEvaluationResponse(response);

        result.TotalScore.Should().Be(5); // 5 × 1
    }

    [Fact]
    public void ParseEvaluationResponse_ClampsScoreAboveMax_ToMaximum()
    {
        var json = """
                   {
                     "completeness":          { "score": 99, "comment": "Over the top" },
                     "accuracy":              { "score": 2,  "comment": "Ok" },
                     "structure_compliance":  { "score": 2,  "comment": "Ok" },
                     "action_item_extraction":{ "score": 2,  "comment": "Ok" },
                     "clarity":              { "score": 2,  "comment": "Ok" },
                     "overall_comment": "Test",
                     "improvements": []
                   }
                   """;

        var result = _sut.ParseEvaluationResponse(json);

        result.Completeness.Score.Should().Be(2, "scores must be clamped to their maximum");
    }

    [Fact]
    public async Task EvaluateAsync_ThrowsArgumentException_WhenTranscriptIsEmpty()
    {
        var act = async () => await _sut.EvaluateAsync(string.Empty, "some summary");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("transcript");
    }

    [Fact]
    public async Task EvaluateAsync_ThrowsArgumentException_WhenSummaryIsEmpty()
    {
        var act = async () => await _sut.EvaluateAsync("some transcript", string.Empty);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("summary");
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsEvaluationResult_WhenApiClientReturnsValidJson()
    {
        _apiClient
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildJsonWithAllScores(2));

        var result = await _sut.EvaluateAsync("transcript", "summary");

        result.TotalScore.Should().Be(10);
        result.Grade.Should().Be("Excellent");
    }

    private static string BuildJsonWithAllScores(int score) =>
        $$"""
          {
            "completeness":          { "score": {{score}}, "comment": "comment" },
            "accuracy":              { "score": {{score}}, "comment": "comment" },
            "structure_compliance":  { "score": {{score}}, "comment": "comment" },
            "action_item_extraction":{ "score": {{score}}, "comment": "comment" },
            "clarity":               { "score": {{score}}, "comment": "comment" },
            "overall_comment": "Overall comment.",
            "improvements": []
          }
          """;
}
