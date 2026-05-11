using AiMeetingSummarizer.Domain;

namespace AiMeetingSummarizer.Application.Interfaces;

/// <summary>
/// Writes the meeting summary and its quality evaluation to one or more outputs.
/// </summary>
public interface IOutputWriter
{
    /// Writes the summary and evaluation results to the configured output destination(s).
    /// </summary>
    /// <param name="summary">The generated meeting summary.</param>
    /// <param name="evaluation">The quality evaluation of the summary.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task WriteAsync(SummaryResult summary, EvaluationResult evaluation, CancellationToken cancellationToken = default);
}
