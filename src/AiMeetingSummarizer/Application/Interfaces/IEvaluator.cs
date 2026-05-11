using AiMeetingSummarizer.Domain;

namespace AiMeetingSummarizer.Application.Interfaces;

/// <summary>
/// Evaluates the quality of a generated summary against the original transcript
/// using an LLM-as-a-Judge approach.
/// </summary>
public interface IEvaluator
{
    /// <summary>
    /// Scores the summary against the provided transcript across multiple quality criteria.
    /// </summary>
    /// <param name="transcript">The original, pre-processed transcript used as ground truth.</param>
    /// <param name="summary">The generated summary to evaluate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An <see cref="EvaluationResult"/> with per-criterion scores and improvement suggestions.</returns>
    Task<EvaluationResult> EvaluateAsync(string transcript, string summary, CancellationToken cancellationToken = default);
}
