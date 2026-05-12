// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Domain;
using Microsoft.Extensions.Logging;

namespace AiMeetingSummarizer.Infrastructure.Ollama;

/// <summary>
/// Generates structured meeting summaries by sending the transcript to a local Ollama model.
/// </summary>
public sealed class OllamaSummarizer : ISummarizer
{
    private static readonly string PromptTemplate;
    private readonly IOllamaApiClient _apiClient;
    private readonly ILogger<OllamaSummarizer> _logger;
        
    static OllamaSummarizer()
    {
        var assembly = typeof(OllamaSummarizer).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();
        var resourceName = resourceNames.FirstOrDefault(r => r.Contains("SummarizerPrompt"));
        if (resourceName == null)
        {
            var availableResources = string.Join(", ", resourceNames);
            throw new InvalidOperationException(
                $"Resource 'SummarizerPrompt.txt' not found. Available resources: {availableResources}");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Resource '{resourceName}' found but could not be loaded");
        }

        using var reader = new StreamReader(stream);
        PromptTemplate = reader.ReadToEnd();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaSummarizer"/> class.
    /// </summary>
    /// <param name="apiClient">HTTP client for communicating with Ollama API.</param>
    /// <param name="logger">Logger for diagnostic information during evaluation.</param>
    public OllamaSummarizer(IOllamaApiClient apiClient, ILogger<OllamaSummarizer> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SummaryResult> SummarizeAsync(string transcript, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            throw new ArgumentException("Transcript must not be empty.", nameof(transcript));
        }

        var prompt = PromptTemplate.Replace("{{TRANSCRIPT}}", transcript);

        _logger.LogDebug("Sending summarization prompt. Transcript length: {Length}", transcript.Length);

        var content = await _apiClient.GenerateAsync(prompt, cancellationToken);

        return new SummaryResult
        {
            Content = content.Trim(),
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}