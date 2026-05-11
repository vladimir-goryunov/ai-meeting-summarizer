// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

using AiMeetingSummarizer.Domain;

namespace AiMeetingSummarizer.Application.Interfaces;

/// <summary>
/// Generates a structured meeting summary from a transcript using a language model.
/// </summary>
public interface ISummarizer
{
    /// <summary>
    /// Sends the transcript to the language model and returns a structured summary.
    /// </summary>
    /// <param name="transcript">Pre-processed meeting transcript text.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="SummaryResult"/> containing the generated summary.</returns>
    Task<SummaryResult> SummarizeAsync(string transcript, CancellationToken cancellationToken = default);
}
