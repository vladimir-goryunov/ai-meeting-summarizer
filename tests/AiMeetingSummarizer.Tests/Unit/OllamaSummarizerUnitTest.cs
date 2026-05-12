// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using AiMeetingSummarizer.Infrastructure.Ollama;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AiMeetingSummarizer.Tests.Unit;

public sealed class OllamaSummarizerUnitTest
{
    private const string AnySystemPrompt = "You are a summarizer.";

    private readonly Mock<IOllamaApiClient> _apiClient = new(MockBehavior.Strict);
    private readonly OllamaSummarizer _sut;

    public OllamaSummarizerUnitTest()
    {
        _sut = new OllamaSummarizer(
            _apiClient.Object, 
            AnySystemPrompt, 
            NullLogger<OllamaSummarizer>.Instance);
    }


    [Fact]
    public async Task SummarizeAsync_ReturnsTrimmedContent_FromApiResponse()
    {
        // Deleting `.Trim()` on the content assignment makes this test fail.
        const string rawResponse = "  \nParticipants: Alice, Bob\n\nSummary:\nDiscussed the project.  ";
        SetupApiClient(rawResponse);

        var result = await _sut.SummarizeAsync("some transcript");

        result.Content.Should().Be(rawResponse.Trim());
    }

    [Fact]
    public async Task SummarizeAsync_SetsGeneratedAtToApproximatelyUtcNow()
    {
        // Deleting `GeneratedAt = DateTimeOffset.UtcNow` makes this test fail.
        SetupApiClient("summary content");

        var before = DateTimeOffset.UtcNow;
        var result = await _sut.SummarizeAsync("transcript");
        var after = DateTimeOffset.UtcNow;

        result.GeneratedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task SummarizeAsync_ThrowsArgumentException_WhenTranscriptIsEmpty()
    {
        // Deleting the IsNullOrWhiteSpace guard makes this test fail.
        var act = async () => await _sut.SummarizeAsync(string.Empty);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("transcript");
    }

    [Fact]
    public async Task SummarizeAsync_PropagatesOllamaException_FromApiClient()
    {
        // Deleting the await (or wrapping it in try-catch) makes this test fail.
        _apiClient
            .Setup(x => x.GenerateAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OllamaException("Cannot connect to Ollama"));

        var act = async () => await _sut.SummarizeAsync("valid transcript");

        await act.Should()
            .ThrowAsync<OllamaException>()
            .WithMessage("Cannot connect to Ollama");
    }

    [Fact]
    public async Task SummarizeAsync_PassesInjectedSystemPromptToApiClient()
    {
        // Deleting `_systemPrompt` field and passing empty string to GenerateAsync makes this test fail.
        const string expectedSystemPrompt = "Custom injected system prompt.";
        var sut = new OllamaSummarizer(
            _apiClient.Object, expectedSystemPrompt, NullLogger<OllamaSummarizer>.Instance);

        string? capturedSystem = null;
        _apiClient
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((sys, _, _) => capturedSystem = sys)
            .ReturnsAsync("summary");

        await sut.SummarizeAsync("transcript");

        capturedSystem.Should().Be(expectedSystemPrompt);
    }

    [Fact]
    public async Task SummarizeAsync_TranscriptIsPassedInUserPrompt()
    {
        // Deleting the `userPrompt` variable and passing transcript to system argument makes this test fail.
        const string transcript = "John: This specific transcript text.";
        string? capturedUserPrompt = null;

        _apiClient
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, user, _) => capturedUserPrompt = user)
            .ReturnsAsync("summary");

        await _sut.SummarizeAsync(transcript);

        capturedUserPrompt.Should().Contain(transcript);
    }
        
    private void SetupApiClient(string response)
    {
        _apiClient
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}