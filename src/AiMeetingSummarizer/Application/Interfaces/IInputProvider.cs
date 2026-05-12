// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

namespace AiMeetingSummarizer.Application.Interfaces;

/// <summary>
/// Reads raw meeting transcript text from a source.
/// </summary>
public interface IInputProvider
{
    /// <summary>
    /// Reads the full text content from the specified path.
    /// </summary>
    /// <param name="path">Absolute or relative path to the transcript file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The raw text content of the transcript.</returns>
    Task<string> ReadAsync(string path, CancellationToken cancellationToken = default);
}
