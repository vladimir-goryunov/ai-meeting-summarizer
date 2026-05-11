// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net.Http.Json;
using AiMeetingSummarizer.Infrastructure.Ollama.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiMeetingSummarizer.Infrastructure.Ollama;

/// <summary>
/// Result of a pre-flight health check against the Ollama service.
/// </summary>
public sealed record OllamaHealthCheckResult
{
    /// <summary>Whether the Ollama service responded to the health probe.</summary>
    public required bool IsOllamaReachable { get; init; }

    /// <summary>Whether the configured model is present in the local Ollama model list.</summary>
    public required bool IsModelAvailable { get; init; }

    /// <summary>Human-readable description of the failure, or null if healthy.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>True only when Ollama is reachable and the model is installed.</summary>
    public bool IsHealthy => IsOllamaReachable && IsModelAvailable;
}

/// <summary>
/// Performs a pre-flight health check against the Ollama service before
/// any transcript processing begins.
/// </summary>
public interface IOllamaHealthChecker
{
    /// <summary>
    /// Checks whether the Ollama service is reachable and whether the configured
    /// model is available locally.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="OllamaHealthCheckResult"/> describing reachability and model availability.
    /// Never throws - all errors are captured in the result.
    /// </returns>
    Task<OllamaHealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implements <see cref="IOllamaHealthChecker"/> by probing the Ollama /api/tags endpoint.
/// </summary>
/// <remarks>
/// Model name matching is normalised: if the configured name has no colon (e.g. "llama3"),
/// it is matched against both "llama3" and "llama3:latest" in the tags list.
/// </remarks>
public sealed class OllamaHealthChecker : IOllamaHealthChecker
{
    private const string TagsEndpoint = "/api/tags";

    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaHealthChecker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaHealthChecker"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured with the Ollama base address.</param>
    /// <param name="settings">Ollama configuration including model name and base URL.</param>
    /// <param name="logger">Logger for health check diagnostics.</param>
    public OllamaHealthChecker(
        HttpClient httpClient,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaHealthChecker> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OllamaHealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Running pre-flight health check against {BaseUrl}", _settings.BaseUrl);

        OllamaTagsResponse? tagsResponse;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            tagsResponse = await _httpClient.GetFromJsonAsync<OllamaTagsResponse>(
                TagsEndpoint, cts.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogDebug(ex, "Ollama health check: service not reachable");

            return new OllamaHealthCheckResult
            {
                IsOllamaReachable = false,
                IsModelAvailable = false,
                ErrorMessage =
                    $"Ollama is not reachable at '{_settings.BaseUrl}'. " +
                    "Ensure Ollama is running: start it with 'ollama serve' or from the Ollama desktop application."
            };
        }

        var installedModels = tagsResponse?.Models ?? [];
        var isModelAvailable = IsModelInstalled(installedModels, _settings.ModelName);

        if (!isModelAvailable)
        {
            var available = installedModels.Count > 0
                ? string.Join(", ", installedModels.Select(m => m.Name))
                : "(none)";

            _logger.LogDebug("Ollama health check: model '{Model}' not found. Available: {Available}",
                _settings.ModelName, available);

            return new OllamaHealthCheckResult
            {
                IsOllamaReachable = true,
                IsModelAvailable = false,
                ErrorMessage =
                    $"Model '{_settings.ModelName}' is not installed in Ollama. " +
                    $"Run: ollama pull {_settings.ModelName}\n" +
                    $"Currently available models: {available}"
            };
        }

        _logger.LogDebug("Ollama health check passed. Model '{Model}' is available", _settings.ModelName);

        return new OllamaHealthCheckResult
        {
            IsOllamaReachable = true,
            IsModelAvailable = true
        };
    }

    /// <summary>
    /// Checks whether <paramref name="modelName"/> matches any installed model,
    /// normalising tag-less names (e.g. "llama3" → also matches "llama3:latest").
    /// </summary>
    private static bool IsModelInstalled(IEnumerable<OllamaModelInfo> models, string modelName)
    {
        var normalised = modelName.Contains(':') ? modelName : modelName + ":latest";

        return models.Any(m =>
            string.Equals(m.Name, modelName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.Name, normalised, StringComparison.OrdinalIgnoreCase));
    }
}