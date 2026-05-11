namespace AiMeetingSummarizer.Infrastructure.Ollama;

/// <summary>
/// Configuration options for the Ollama API client.
/// </summary>
public sealed class OllamaSettings
{
    /// <summary>The configuration section name in appsettings.json.</summary>
    public const string SectionName = "Ollama";

    /// <summary>Base URL of the running Ollama instance.</summary>
    public string BaseUrl { get; init; } = "http://localhost:11434";

    /// <summary>Name of the model to use for generation.</summary>
    public string ModelName { get; init; } = "qwen2.5:7b-instruct";

    /// <summary>HTTP request timeout in seconds.</summary>
    public int TimeoutSeconds { get; init; } = 180;
}
