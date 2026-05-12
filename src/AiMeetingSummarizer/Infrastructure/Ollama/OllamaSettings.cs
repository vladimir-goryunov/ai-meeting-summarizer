// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

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
    /// </summary>
    [Range(5, 600, ErrorMessage = "Ollama:TimeoutSeconds must be between 5 and 600.")]
    public int TimeoutSeconds { get; init; } = 180;

    /// <summary>
    /// Sampling temperature controlling response randomness (0.0–1.0).
    /// 0.0 = fully deterministic (requires <see cref="Seed"/> to be set for strict reproducibility).
    /// Higher values produce more varied output; lower values produce more consistent output.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Ollama:Temperature must be between 0.0 and 1.0.")]
    public double Temperature { get; init; } = 0.0;

    /// <summary>
    /// Random seed for deterministic generation.
    /// When combined with <see cref="Temperature"/> = 0.0, the same transcript will always
    /// produce the same summary and evaluation score across runs.
    /// Set to null to disable fixed seeding (non-reproducible output).
    /// </summary>
    public int? Seed { get; init; } = 42;
}