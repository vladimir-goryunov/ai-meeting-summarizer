// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

namespace AiMeetingSummarizer.Domain;

/// <summary>
/// Timing statistics for a single pipeline run.
/// </summary>
public sealed record RunStatistics
{
    /// <summary>Time taken by the summarization step.</summary>
    public required TimeSpan SummarizationDuration { get; init; }

    /// <summary>Time taken by the evaluation step.</summary>
    public required TimeSpan EvaluationDuration { get; init; }

    /// <summary>Total inference time (summarization + evaluation).</summary>
    public TimeSpan TotalDuration => SummarizationDuration + EvaluationDuration;
}