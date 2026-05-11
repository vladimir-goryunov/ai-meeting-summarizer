// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.RegularExpressions;
using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Domain;
using AiMeetingSummarizer.Infrastructure.Ollama.Models;
using Microsoft.Extensions.Logging;

namespace AiMeetingSummarizer.Infrastructure.Ollama;

/// <summary>
/// Evaluates summary quality using an LLM-as-a-Judge approach.
/// The model scores the summary against the original transcript across five criteria.
/// </summary>
public sealed class OllamaEvaluator : IEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly string PromptTemplate;
    private readonly IOllamaApiClient _apiClient;
    private readonly ILogger<OllamaEvaluator> _logger;

    static OllamaEvaluator()
    {
        var assembly = typeof(OllamaEvaluator).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();
        var resourceName = resourceNames.FirstOrDefault(r => r.Contains("EvaluatorPrompt"));
        if (resourceName == null)
        {
            var availableResources = string.Join(", ", resourceNames);
            throw new InvalidOperationException(
                $"Resource 'EvaluatorPrompt.txt' not found. Available resources: {availableResources}");
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
    /// Initializes a new instance of the <see cref="OllamaEvaluator"/> class.
    /// </summary>
    /// <param name="apiClient">HTTP client for communicating with Ollama API.</param>
    /// <param name="logger">Logger for diagnostic information during evaluation.</param>
    public OllamaEvaluator(IOllamaApiClient apiClient, ILogger<OllamaEvaluator> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
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
            throw new ArgumentException("Summary must not be empty.", nameof(summary));
        }

        var prompt = PromptTemplate
            .Replace("{{TRANSCRIPT}}", transcript)
            .Replace("{{SUMMARY}}", summary);

        _logger.LogDebug("Sending evaluation prompt to Ollama");

        var rawResponse = await _apiClient.GenerateAsync(prompt, cancellationToken);

        return ParseEvaluationResponse(rawResponse);
    }

    internal EvaluationResult ParseEvaluationResponse(string rawResponse)
    {
        var parsed = TryDeserialize(rawResponse)
                     ?? TryDeserialize(ExtractJsonBlock(rawResponse));

        if (parsed is null)
        {
            _logger.LogWarning(
                "Could not parse evaluation JSON from model response. Returning fallback evaluation.");

            return CreateFallbackResult("Evaluation could not be parsed from the model response.");
        }

        return MapToEvaluationResult(parsed);
    }

    private static EvaluationMeetingResponse? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<EvaluationMeetingResponse>(json, JsonOptions);
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

    private static EvaluationResult MapToEvaluationResult(EvaluationMeetingResponse response)
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