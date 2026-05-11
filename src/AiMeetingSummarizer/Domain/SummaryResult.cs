// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace AiMeetingSummarizer.Domain;

/// <summary>
/// Represents the structured result of a meeting summarization.
/// </summary>
public sealed record SummaryResult
{
    /// <summary>
    /// The full structured content as returned by the language model.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The UTC timestamp when the summary was generated.
    /// </summary>
    public required DateTimeOffset GeneratedAt { get; init; }
}
