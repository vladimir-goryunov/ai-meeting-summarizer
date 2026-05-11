// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace AiMeetingSummarizer.Domain;

/// <summary>
/// Represents a score for a single evaluation criterion, including a descriptive comment.
/// </summary>
public sealed record EvaluationScore(int Score, int MaxScore, string Comment);

/// <summary>
/// Represents the quality evaluation of a meeting summary, scored by an LLM-as-a-Judge approach.
/// Each criterion is scored independently; the total is the sum of all criterion scores.
/// </summary>
public sealed record EvaluationResult
{
    /// <summary>Score for completeness of information extraction (0-2).</summary>
    public required EvaluationScore Completeness { get; init; }

    /// <summary>Score for factual accuracy relative to the transcript (0-2).</summary>
    public required EvaluationScore Accuracy { get; init; }

    /// <summary>Score for adherence to the required output structure (0-2).</summary>
    public required EvaluationScore StructureCompliance { get; init; }

    /// <summary>Score for quality of action item extraction and ownership assignment (0-2).</summary>
    public required EvaluationScore ActionItemExtraction { get; init; }

    /// <summary>Score for readability and clarity of the summary (0-2).</summary>
    public required EvaluationScore Clarity { get; init; }

    /// <summary>Overall narrative assessment of the summary quality.</summary>
    public required string OverallComment { get; init; }

    /// <summary>Specific, actionable suggestions for improving the summary.</summary>
    public required IReadOnlyList<string> Improvements { get; init; }

    /// <summary>Sum of all criterion scores.</summary>
    public int TotalScore =>
        Completeness.Score + 
        Accuracy.Score + 
        StructureCompliance.Score +
        ActionItemExtraction.Score + 
        Clarity.Score;

    /// <summary>Maximum achievable total score.</summary>
    public int MaxTotalScore => 10;

    /// <summary>Human-readable grade derived from the total score.</summary>
    public string Grade => TotalScore switch
    {
        >= 9 => "Excellent",
        >= 7 => "Good",
        >= 5 => "Acceptable",
        _ => "Needs Improvement"
    };
}
