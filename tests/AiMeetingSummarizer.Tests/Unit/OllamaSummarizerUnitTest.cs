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
    private readonly Mock<IOllamaApiClient> _apiClient = new(MockBehavior.Strict);
    private readonly OllamaSummarizer _sut;

    public OllamaSummarizerUnitTest()
    {
        _sut = new OllamaSummarizer(_apiClient.Object, NullLogger<OllamaSummarizer>.Instance);
    }

    [Fact]
    public async Task SummarizeAsync_ReturnsTrimmedContent_FromApiResponse()
    {
        const string rawResponse = "  \nParticipants: Alice, Bob\n\nSummary:\nDiscussed the project.  ";

        _apiClient
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawResponse);

        var result = await _sut.SummarizeAsync("some transcript");

        result.Content.Should().Be(rawResponse.Trim());
    }

    [Fact]
    public async Task SummarizeAsync_SetsGeneratedAtToApproximatelyUtcNow()
    {
        _apiClient
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("summary content");

        var before = DateTimeOffset.UtcNow;
        var result = await _sut.SummarizeAsync("transcript");
        var after = DateTimeOffset.UtcNow;

        result.GeneratedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task SummarizeAsync_ThrowsArgumentException_WhenTranscriptIsEmpty()
    {
        var act = async () => await _sut.SummarizeAsync(string.Empty);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("transcript");
    }

    [Fact]
    public async Task SummarizeAsync_ThrowsArgumentException_WhenTranscriptIsWhitespace()
    {
        var act = async () => await _sut.SummarizeAsync("   ");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("transcript");
    }

    [Fact]
    public async Task SummarizeAsync_PropagatesOllamaException_FromApiClient()
    {
        _apiClient
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OllamaException("Cannot connect to Ollama"));

        var act = async () => await _sut.SummarizeAsync("valid transcript");

        await act.Should().ThrowAsync<OllamaException>()
            .WithMessage("Cannot connect to Ollama");
    }

    [Fact]
    public async Task SummarizeAsync_IncludesTranscriptInPrompt_SentToApiClient()
    {
        const string transcript = "John: This specific transcript text.";
        string? capturedPrompt = null;

        _apiClient
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, _) => capturedPrompt = prompt)
            .ReturnsAsync("summary");

        await _sut.SummarizeAsync(transcript);

        capturedPrompt.Should().Contain(transcript);
    }
}
