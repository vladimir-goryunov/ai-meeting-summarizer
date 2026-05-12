// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace AiMeetingSummarizer.Infrastructure.Ollama.Models;

/// <summary>
/// Represents the response from the Ollama /api/tags endpoint,
/// which lists all locally available models.
/// </summary>
internal sealed class OllamaTagsResponse
{
    /// <summary>
    /// Models meta info
    /// </summary>
    [JsonPropertyName("models")]
    public List<OllamaModelInfo>? Models { get; init; }
}

/// <summary>
/// Metadata for a single model returned by /api/tags.
/// </summary>
internal sealed class OllamaModelInfo
{
    /// <summary>
    /// Fully qualified model name including tag, e.g. "llama3:latest".
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}