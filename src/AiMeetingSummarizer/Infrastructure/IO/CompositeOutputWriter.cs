// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Domain;

namespace AiMeetingSummarizer.Infrastructure.IO;

/// <summary>
/// Writes output to multiple destinations simultaneously using the Composite pattern.
/// Allows writing meeting summaries and evaluations to several output writers at once.
/// </summary>
/// <example>
/// var composite = new CompositeOutputWriter(new IOutputWriter[] { 
///     new ConsoleOutputWriter(), 
///     new FileOutputWriter() 
/// });
/// await composite.WriteAsync(content); // Writes to both console and file
/// </example>
public sealed class CompositeOutputWriter : IOutputWriter
{
    private readonly IReadOnlyList<IOutputWriter> _writers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeOutputWriter"/> class.
    /// </summary>
    /// <param name="writers">Collection of output writers to delegate to.</param>
    public CompositeOutputWriter(IReadOnlyList<IOutputWriter> writers)
    {
        _writers = writers;
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        SummaryResult summary,
        EvaluationResult evaluation,
        RunStatistics statistics,
        CancellationToken cancellationToken = default)
    {
        foreach (var writer in _writers)
        {
            await writer.WriteAsync(summary, evaluation, statistics, cancellationToken);
        }
    }
}
