// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Domain;
using Microsoft.Extensions.Logging;

namespace AiMeetingSummarizer.Infrastructure.Ollama;

/// <summary>
/// Generates structured meeting summaries by sending a transcript to a local Ollama model.
/// </summary>
public sealed class OllamaSummarizer : ISummarizer
{
    private readonly IOllamaApiClient _apiClient;
    private readonly string _systemPrompt;
    private readonly ILogger<OllamaSummarizer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaSummarizer"/> class.
    /// </summary>
    /// <param name="apiClient">Ollama API client used to send generation requests.</param>
    /// <param name="systemPrompt">
    /// Pre-loaded system prompt text from <c>SummarizerPrompt.txt</c>.
    /// Injected by the DI container via <see cref="PromptLoader"/>.
    /// </param>
    /// <param name="logger">Logger for request diagnostics.</param>
    public OllamaSummarizer(
        IOllamaApiClient apiClient,
        string systemPrompt,
        ILogger<OllamaSummarizer> logger)
    {
        _apiClient = apiClient;
        _systemPrompt = systemPrompt;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="transcript"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="OllamaException">
    /// Thrown when the Ollama service is unreachable, times out, or returns an error response.
    /// </exception>
    public async Task<SummaryResult> SummarizeAsync(string transcript, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            throw new ArgumentException("Transcript must not be empty.", nameof(transcript));
        }

        // User prompt carries only the variable data; rules and format are in the system prompt.
        var userPrompt = $"TRANSCRIPT:\n{transcript}";

        _logger.LogDebug("Sending summarization request. Transcript length: {Length}", transcript.Length);

        var content = await _apiClient.GenerateAsync(_systemPrompt, userPrompt, cancellationToken);

        return new SummaryResult
        {
            Content = content.Trim(),
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}