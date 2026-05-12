// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

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
    /// Sends a generation request to the configured model and returns the response text.
    /// </summary>
    /// <param name="systemPrompt">
    /// Role definition, behavioural rules, and output format instructions.
    /// Kept separate from <paramref name="userPrompt"/> so RLHF fine-tuned models
    /// (llama3, qwen, phi4) apply maximum compliance to the instructions.
    /// This value is static and does not change between calls.
    /// </param>
    /// <param name="userPrompt">
    /// The concrete data for this request: transcript text, generated summary, etc.
    /// Changes with every call.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The raw text response from the model.</returns>
    Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// HTTP-based implementation of <see cref="IOllamaApiClient"/> targeting
/// the Ollama <c>/api/generate</c> endpoint.
/// </summary>
/// <remarks>
/// Translates the two-part prompt (system + user) into the corresponding
/// Ollama API request fields and handles all HTTP-level errors with
/// actionable exception messages.
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
    /// <param name="httpClient">Configured HTTP client with Ollama base address and timeout.</param>
    /// <param name="settings">Ollama configuration: model name, temperature, seed, base URL.</param>
    /// <param name="logger">Logger for request/response diagnostics.</param>
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
    public async Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var request = new GenerateRequest
        {
            Model = _settings.ModelName,
            System = systemPrompt,
            Prompt = userPrompt,
            Stream = false,
            Options = new GenerateOptions
            {
                Temperature = _settings.Temperature,
                NumPredict = 2048,
                Seed = _settings.Seed
            }
        };

        _logger.LogDebug(
            "Sending generate request. Model: {Model}, Temperature: {Temperature}, Seed: {Seed}",
            _settings.ModelName, _settings.Temperature, _settings.Seed?.ToString() ?? "none");

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync("/api/generate", request, cancellationToken);
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
                $"Ensure the model '{_settings.ModelName}' is installed " +
                $"(run: ollama pull {_settings.ModelName}).");
        }

        var generateResponse = await response.Content.ReadFromJsonAsync<GenerateResponse>(
            JsonOptions, cancellationToken)
            ?? throw new OllamaException("Ollama returned an empty or malformed response.");

        _logger.LogDebug("Received response from Ollama. Done: {Done}", generateResponse.Done);

        return generateResponse.Response;
    }
}