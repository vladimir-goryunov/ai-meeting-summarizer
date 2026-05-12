// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using AiMeetingSummarizer.Domain;

namespace AiMeetingSummarizer.Application.Interfaces;

/// <summary>
/// Writes the meeting summary, its quality evaluation, and run statistics to one or more outputs.
/// </summary>
public interface IOutputWriter
{
    /// <summary>
    /// Writes the summary, evaluation, and run statistics to the configured output destination(s).
    /// </summary>
    /// <param name="summary">The generated meeting summary.</param>
    /// <param name="evaluation">The quality evaluation of the summary.</param>
    /// <param name="statistics">Timing statistics for the pipeline run.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task WriteAsync(
        SummaryResult summary,
        EvaluationResult evaluation,
        RunStatistics statistics,
        CancellationToken cancellationToken = default);
}
