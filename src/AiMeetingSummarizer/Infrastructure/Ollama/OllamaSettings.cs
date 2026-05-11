// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace AiMeetingSummarizer.Infrastructure.Ollama;

/// <summary>
/// Configuration options for the Ollama API client.
/// All properties are validated at startup via data annotations.
/// </summary>
public sealed class OllamaSettings
{
    /// <summary>The configuration section name in appsettings.json.</summary>
    public const string SectionName = "Ollama";

    /// <summary>
    /// Base URL of the running Ollama instance.
    /// Must be a valid absolute HTTP or HTTPS URL.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Ollama:BaseUrl is required.")]
    [Url(ErrorMessage = "Ollama:BaseUrl must be a valid URL (e.g. http://localhost:11434).")]
    public string BaseUrl { get; init; } = "http://localhost:11434";

    /// <summary>
    /// Name of the Ollama model to use for generation.
    /// Must match a model installed locally (see: ollama list).
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Ollama:ModelName is required.")]
    [MinLength(1, ErrorMessage = "Ollama:ModelName must not be empty.")]
    public string ModelName { get; init; } = "qwen2.5:7b-instruct";

    /// <summary>
    /// HTTP request timeout in seconds.
    /// Minimum 5 seconds, maximum 600 seconds (10 minutes).
    /// Increase for slow hardware or large transcripts.
    /// </summary>
    [Range(5, 600, ErrorMessage = "Ollama:TimeoutSeconds must be between 5 and 600.")]
    public int TimeoutSeconds { get; init; } = 180;
}