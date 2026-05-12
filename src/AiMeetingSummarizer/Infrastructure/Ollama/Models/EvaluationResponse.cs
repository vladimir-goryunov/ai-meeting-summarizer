// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace AiMeetingSummarizer.Infrastructure.Ollama.Models;

/// <summary>
/// Represents the serializable response structure from the Ollama evaluation API.
/// Contains scores and feedback for all five evaluation criteria.
/// </summary>
internal sealed class EvaluationResponse
{
    /// <summary>Score and comment for content completeness (0-2)</summary>
    [JsonPropertyName("completeness")]
    public CriterionScore? Completeness { get; init; }

    /// <summary>Score and comment for factual accuracy (0-2)</summary>
    [JsonPropertyName("accuracy")]
    public CriterionScore? Accuracy { get; init; }

    /// <summary>Score and comment for format compliance (0-2)</summary>
    [JsonPropertyName("structure_compliance")]
    public CriterionScore? StructureCompliance { get; init; }

    /// <summary>Score and comment for action item extraction (0-2)</summary>
    [JsonPropertyName("action_item_extraction")]
    public CriterionScore? ActionItemExtraction { get; init; }

    /// <summary>Score and comment for language clarity (0-2)</summary>
    [JsonPropertyName("clarity")]
    public CriterionScore? Clarity { get; init; }

    /// <summary>High-level assessment of summary quality</summary>
    [JsonPropertyName("overall_comment")]
    public string? OverallComment { get; init; }

    /// <summary>Specific actionable suggestions for improvement</summary>
    [JsonPropertyName("improvements")]
    public List<string>? Improvements { get; init; }
}

/// <summary>
/// Represents a single evaluation criterion with its score and justification.
/// </summary>
internal sealed class CriterionScore
{
    /// <summary>Numeric score between 0 and 2 based on the evaluation rubric</summary>
    [JsonPropertyName("score")]
    public int Score { get; init; }

    /// <summary>Specific evidence or justification for the assigned score</summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; init; }
}
