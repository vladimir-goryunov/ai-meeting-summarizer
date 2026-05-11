// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net.Http.Json;
using System.Text.Json;
using AiMeetingSummarizer.Infrastructure.Ollama.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiMeetingSummarizer.Infrastructure.Ollama;

/// <summary>
/// Sends text generation requests to a local Ollama instance.
/// </summary>
public interface IOllamaApiClient
{
    /// <summary>
    /// Sends a prompt to the configured model and returns the generated response text.
    /// </summary>
    /// <param name="prompt">The full prompt to send.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The raw text response from the model.</returns>
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}

/// <summary>
/// HTTP-based implementation of <see cref="IOllamaApiClient"/> targeting the Ollama /api/generate endpoint.
/// Handles HTTP communication, request serialization, and response deserialization.
/// </summary>
/// <remarks>
/// Implements retry logic for transient failures and timeout handling.
/// Configured via OllamaSettings which can be overridden by command-line arguments.
/// </remarks>
public sealed class OllamaApiClient : IOllamaApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaApiClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">Configured HTTP client with base address and timeouts.</param>
    /// <param name="settings">Ollama configuration including model name and endpoint URL.</param>
    /// <param name="logger">Logger for request/response diagnostics and errors.</param>
    public OllamaApiClient(
        HttpClient httpClient,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var request = new GenerateRequest
        {
            Model = _settings.ModelName,
            Prompt = prompt,
            Stream = false,
            Options = new GenerateOptions { Temperature = 0.1, NumPredict = 2048 }
        };

        _logger.LogDebug("Sending generate request to Ollama. Model: {Model}", _settings.ModelName);

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "/api/generate", request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new OllamaException(
                $"Unable to connect to Ollama at '{_settings.BaseUrl}'. " +
                "Ensure Ollama is running and accessible.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OllamaException(
                $"Request to Ollama timed out after {_settings.TimeoutSeconds} seconds. " +
                "Consider increasing 'Ollama:TimeoutSeconds' or using a smaller model.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new OllamaException(
                $"Ollama returned HTTP {(int)response.StatusCode}: {body}. " +
                $"Ensure the model '{_settings.ModelName}' is installed (run: ollama pull {_settings.ModelName}).");
        }

        var generateResponse = await response.Content.ReadFromJsonAsync<GenerateResponse>(
            JsonOptions, cancellationToken)
            ?? throw new OllamaException("Ollama returned an empty or malformed response.");

        _logger.LogDebug("Received response from Ollama. Done: {Done}", generateResponse.Done);

        return generateResponse.Response;
    }
}
