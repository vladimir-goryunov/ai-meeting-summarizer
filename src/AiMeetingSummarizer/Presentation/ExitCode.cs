// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace AiMeetingSummarizer.Presentation;

/// <summary>
/// Defines standardized exit codes for the application.
/// These codes are returned to the operating system to indicate execution status.
/// </summary>
public enum ExitCode
{
    /// <summary>Operation completed successfully.</summary>
    Success = 0,

    /// <summary>Invalid command-line arguments or parse error.</summary>
    InvalidArguments = 1,

    /// <summary>Operation was cancelled by user (Ctrl+C).</summary>
    OperationCancelled = 2,

    /// <summary>Input file not found or inaccessible.</summary>
    InputFileNotFound = 3,

    /// <summary>Ollama service error during inference (connection lost mid-run, invalid response).</summary>
    OllamaServiceError = 4,

    /// <summary>Unexpected internal error.</summary>
    UnexpectedError = 5,

    /// <summary>
    /// Ollama process is not running or not reachable at the configured base URL.
    /// Detected during pre-flight check before processing begins.
    /// Resolution: start Ollama with 'ollama serve'.
    /// </summary>
    OllamaNotReachable = 6,

    /// <summary>
    /// The configured model is not installed in Ollama.
    /// Detected during pre-flight check before processing begins.
    /// Resolution: run 'ollama pull &lt;model-name&gt;'.
    /// </summary>
    OllamaModelNotFound = 7,

    /// <summary>
    /// Application configuration is invalid (e.g. empty model name, bad URL, out-of-range timeout).
    /// Resolution: check appsettings.json.
    /// </summary>
    InvalidConfiguration = 8
}