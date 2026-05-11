// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace AiMeetingSummarizer.Infrastructure.Ollama;

/// <summary>
/// Represents errors that occur during communication with the Ollama API.
/// </summary>
/// <remarks>
/// This exception is thrown for:
/// - HTTP request failures (connection refused, timeouts)
/// - Invalid JSON responses
/// - Ollama API error responses (e.g., model not found)
/// Does NOT include validation errors from prompt formatting.
/// </remarks>
public sealed class OllamaException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaException"/> class.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    public OllamaException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaException"/> class.
    /// </summary>
    /// <param name="message">Error message describing the failure.</param>
    /// <param name="innerException">Inner exception that caused this failure.</param>
    public OllamaException(string message, Exception innerException)
        : base(message, innerException) { }
}
