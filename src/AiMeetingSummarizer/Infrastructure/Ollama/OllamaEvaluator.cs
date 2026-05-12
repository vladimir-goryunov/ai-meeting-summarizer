// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.RegularExpressions;
using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Domain;
using AiMeetingSummarizer.Infrastructure.Ollama.Models;
using Microsoft.Extensions.Logging;

namespace AiMeetingSummarizer.Infrastructure.Ollama;

/// <summary>
/// Evaluates meeting summary quality using an LLM-as-a-Judge approach.
/// </summary>
/// <remarks>
/// <para>
/// The prompt is split into system and user parts: scoring rules travel as the system prompt,
/// transcript and summary as the user prompt.
/// The system prompt text is loaded from <c>Infrastructure/Templates/EvaluatorPrompt.txt</c>
/// at application startup and injected via constructor, so it can be edited without recompiling.
/// If the model returns a response that cannot be parsed as JSON, a fallback
/// <see cref="EvaluationResult"/> with all scores set to 0 is returned instead of throwing.
/// </para>
/// </remarks>
public sealed class OllamaEvaluator : IEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IOllamaApiClient _apiClient;
    private readonly string _systemPrompt;
    private readonly ILogger<OllamaEvaluator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaEvaluator"/> class.
    /// </summary>
    /// <param name="apiClient">Ollama API client used to send generation requests.</param>
    /// <param name="systemPrompt">
    /// Pre-loaded system prompt text from <c>EvaluatorPrompt.txt</c>.
    /// Injected by the DI container via <see cref="PromptLoader"/>.
    /// </param>
    /// <param name="logger">Logger for request diagnostics and JSON parse failures.</param>
    public OllamaEvaluator(
        IOllamaApiClient apiClient,
        string systemPrompt,
        ILogger<OllamaEvaluator> logger)
    {
        _apiClient = apiClient;
        _systemPrompt = systemPrompt;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="transcript"/> or <paramref name="summary"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="OllamaException">
    /// Thrown when the Ollama service is unreachable, times out, or returns an error response.
    /// </exception>
    public async Task<EvaluationResult> EvaluateAsync(
        string transcript,
        string summary,
        CancellationToken cancellationToken = default)
    {

        if (string.IsNullOrWhiteSpace(transcript))
        {
            throw new ArgumentException("Transcript must not be empty.", nameof(transcript));
        }
        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Summary1 must not be empty.", nameof(summary));
        }

        // User prompt carries only the variable data; scoring rules are in the system prompt.
        var userPrompt =
            $"""
             ORIGINAL TRANSCRIPT:
             {transcript}

             GENERATED SUMMARY:
             {summary}
             """;

        _logger.LogDebug("Sending evaluation request to Ollama");

        var rawResponse = await _apiClient.GenerateAsync(_systemPrompt, userPrompt, cancellationToken);

        return ParseEvaluationResponse(rawResponse);
    }

    internal EvaluationResult ParseEvaluationResponse(string rawResponse)
    {
        var parsed = TryDeserialize(rawResponse)
                     ?? TryDeserialize(ExtractJsonBlock(rawResponse));

        if (parsed is not null)
            return MapToEvaluationResult(parsed); // ← компилятор гарантированно знает: non-null

        _logger.LogWarning("Could not parse evaluation JSON. Returning fallback evaluation.");
        return CreateFallbackResult("Evaluation could not be parsed from the model response.");
    }

    private static EvaluationResponse? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<EvaluationResponse>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractJsonBlock(string text)
    {
        var match = Regex.Match(text, @"\{[\s\S]*\}", RegexOptions.Multiline);
        return match.Success ? match.Value : null;
    }

    private static EvaluationResult MapToEvaluationResult(EvaluationResponse response)
    {
        static EvaluationScore ToScore(CriterionScore? criterion, string fallback)
            => new(
                Score: Math.Clamp(criterion?.Score ?? 0, 0, 2),
                MaxScore: 2,
                Comment: criterion?.Comment ?? fallback);

        return new EvaluationResult
        {
            Completeness = ToScore(response.Completeness, "Not evaluated"),
            Accuracy = ToScore(response.Accuracy, "Not evaluated"),
            StructureCompliance = ToScore(response.StructureCompliance, "Not evaluated"),
            ActionItemExtraction = ToScore(response.ActionItemExtraction, "Not evaluated"),
            Clarity = ToScore(response.Clarity, "Not evaluated"),
            OverallComment = response.OverallComment ?? "No overall comment provided.",
            Improvements = (IReadOnlyList<string>?)response.Improvements ?? Array.Empty<string>()
        };
    }

    private static EvaluationResult CreateFallbackResult(string reason) =>
        new()
        {
            Completeness = new EvaluationScore(0, 2, reason),
            Accuracy = new EvaluationScore(0, 2, reason),
            StructureCompliance = new EvaluationScore(0, 2, reason),
            ActionItemExtraction = new EvaluationScore(0, 2, reason),
            Clarity = new EvaluationScore(0, 2, reason),
            OverallComment = reason,
            Improvements = new[] { "Retry evaluation or inspect the model response manually." }
        };
}