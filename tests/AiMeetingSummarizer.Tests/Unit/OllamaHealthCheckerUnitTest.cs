// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using AiMeetingSummarizer.Infrastructure.Ollama;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace AiMeetingSummarizer.Tests.Unit;

public sealed class OllamaHealthCheckerUnitTest
{
    private const string BaseUrl = "http://localhost:11434";
    private const string DefaultModel = "llama3";

    private readonly Mock<HttpMessageHandler> _handlerMock = new(MockBehavior.Strict);
    
    #region Init
    private OllamaHealthChecker BuildSut(string modelName = DefaultModel)
    {
        var httpClient = new HttpClient(_handlerMock.Object) { BaseAddress = new Uri(BaseUrl) };
        var settings = Options.Create(new OllamaSettings { BaseUrl = BaseUrl, ModelName = modelName });
        return new OllamaHealthChecker(httpClient, settings, NullLogger<OllamaHealthChecker>.Instance);
    }

    private void SetupResponse(HttpStatusCode statusCode, object? body = null)
    {
        var json = body is null ? "{}" : JsonSerializer.Serialize(body);
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupException(Exception ex)
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(ex);
    }
    #endregion

    #region Reachability
    [Fact]
    public async Task CheckAsync_ReturnsNotReachable_WhenHttpRequestExceptionThrown()
    {
        SetupException(new HttpRequestException("Connection refused"));

        var result = await BuildSut().CheckAsync();

        result.IsOllamaReachable.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_ReturnsNotReachable_WhenTimeoutOccurs()
    {
        SetupException(new TaskCanceledException("Timeout"));

        var result = await BuildSut().CheckAsync();

        result.IsOllamaReachable.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_ErrorMessage_ContainsBaseUrl_WhenOllamaNotReachable()
    {
        SetupException(new HttpRequestException());

        var result = await BuildSut().CheckAsync();

        result.ErrorMessage.Should().Contain(BaseUrl);
    }

    [Fact]
    public async Task CheckAsync_IsHealthy_ReturnsFalse_WhenOllamaNotReachable()
    {
        SetupException(new HttpRequestException());

        var result = await BuildSut().CheckAsync();

        result.IsHealthy.Should().BeFalse();
    }
    #endregion

    #region LLM availability
    [Fact]
    public async Task CheckAsync_ReturnsModelAvailable_WhenModelNameMatchesExactly()
    {
        SetupResponse(HttpStatusCode.OK, new
        {
            models = new[] { new { name = DefaultModel } }
        });

        var result = await BuildSut(DefaultModel).CheckAsync();

        result.IsModelAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAsync_ReturnsModelNotAvailable_WhenModelAbsentFromList()
    {
        SetupResponse(HttpStatusCode.OK, new
        {
            models = new[] { new { name = "mistral:latest" } }
        });

        var result = await BuildSut(DefaultModel).CheckAsync();

        result.IsModelAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_NormalisesModelName_WhenConfiguredWithoutTag()
    {
        // "llama3" (no tag) must match "llama3:latest" returned by Ollama
        SetupResponse(HttpStatusCode.OK, new
        {
            models = new[] { new { name = "llama3:latest" } }
        });

        var result = await BuildSut("llama3").CheckAsync();

        result.IsModelAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAsync_ErrorMessage_ContainsModelName_WhenModelNotFound()
    {
        SetupResponse(HttpStatusCode.OK, new
        {
            models = new[] { new { name = "other-model:latest" } }
        });

        var result = await BuildSut(DefaultModel).CheckAsync();

        result.ErrorMessage.Should().Contain(DefaultModel);
    }

    [Fact]
    public async Task CheckAsync_IsHealthy_ReturnsFalse_WhenModelNotAvailable()
    {
        SetupResponse(HttpStatusCode.OK, new
        {
            models = new[] { new { name = "other-model:latest" } }
        });

        var result = await BuildSut(DefaultModel).CheckAsync();

        result.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_IsHealthy_ReturnsTrue_WhenOllamaReachableAndModelAvailable()
    {
        SetupResponse(HttpStatusCode.OK, new
        {
            models = new[] { new { name = DefaultModel } }
        });

        var result = await BuildSut(DefaultModel).CheckAsync();

        result.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAsync_ReturnsModelNotAvailable_WhenModelsListIsEmpty()
    {
        SetupResponse(HttpStatusCode.OK, new { models = Array.Empty<object>() });

        var result = await BuildSut(DefaultModel).CheckAsync();

        result.IsModelAvailable.Should().BeFalse();
    }
    #endregion

    #region Never throws
    [Fact]
    public async Task CheckAsync_NeverThrows_WhenOllamaIsUnreachable()
    {
        SetupException(new HttpRequestException("Simulated failure"));

        var act = async () => await BuildSut().CheckAsync();

        await act.Should().NotThrowAsync();
    }
    #endregion
}