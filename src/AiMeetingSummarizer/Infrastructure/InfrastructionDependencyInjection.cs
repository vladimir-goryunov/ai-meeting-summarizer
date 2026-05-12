// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Infrastructure.IO;
using AiMeetingSummarizer.Infrastructure.Ollama;
using AiMeetingSummarizer.Infrastructure.Processing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AiMeetingSummarizer.Infrastructure;

/// <summary>
/// Dependency injection registration extension methods for the Infrastructure layer.
/// </summary>
public static class InfrastructionDependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure layer services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">Application configuration for Ollama and output settings.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// Registered services:
    /// <list type="bullet">
    ///   <item><see cref="OllamaSettings"/> - validated via data annotations on startup</item>
    ///   <item><see cref="OutputSettings"/> - validated via data annotations on startup</item>
    ///   <item>HTTP client for Ollama API (<see cref="IOllamaApiClient"/>)</item>
    ///   <item>HTTP client for health check (<see cref="IOllamaHealthChecker"/>)</item>
    ///   <item><see cref="IInputProvider"/>, <see cref="ITextPreprocessor"/>, <see cref="IOutputWriter"/></item>
    ///   <item><see cref="ISummarizer"/> - prompt loaded from <c>SummarizerPrompt.txt</c></item>
    ///   <item><see cref="IEvaluator"/> - prompt loaded from <c>EvaluatorPrompt.txt</c></item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<OllamaSettings>()
            .Bind(configuration.GetSection(OllamaSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OutputSettings>()
            .Bind(configuration.GetSection(OutputSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IOllamaApiClient, OllamaApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<OllamaSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        services.AddHttpClient<IOllamaHealthChecker, OllamaHealthChecker>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<OllamaSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddSingleton<IInputProvider, FileInputProvider>();
        services.AddSingleton<ITextPreprocessor, TextPreprocessor>();
        services.AddSingleton<ConsoleOutputWriter>();
        services.AddSingleton<FileOutputWriter>();
        services.AddSingleton<IOutputWriter>(sp => new CompositeOutputWriter(
        [
            sp.GetRequiredService<ConsoleOutputWriter>(),
            sp.GetRequiredService<FileOutputWriter>()
        ]));

        // Prompts are loaded once at startup from .txt files — no recompile needed to change them.
        var promptLoader = new PromptLoader();

        services.AddSingleton<ISummarizer>(sp => new OllamaSummarizer(
            sp.GetRequiredService<IOllamaApiClient>(),
            promptLoader.Load("SummarizerPrompt.txt"),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OllamaSummarizer>>()));

        services.AddSingleton<IEvaluator>(sp => new OllamaEvaluator(
            sp.GetRequiredService<IOllamaApiClient>(),
            promptLoader.Load("EvaluatorPrompt.txt"),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OllamaEvaluator>>()));

        return services;
    }
}