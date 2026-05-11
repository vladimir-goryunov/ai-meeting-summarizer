// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
/// Registers IO providers, HTTP clients, Ollama services, and configuration settings.
/// </summary>
public static class InfrastructionDependencyInjection
{
    /// <summary>
    /// Registers Infrastructure layer services with the dependency injection container.
    /// Configures Ollama settings, HTTP clients, and all infrastructure components.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">Application configuration for reading settings like Ollama endpoints and timeouts.</param>
    /// <returns>The IServiceCollection instance for method chaining.</returns>
    /// <remarks>
    /// Registered services include:
    /// - OllamaSettings (Options pattern with data annotation validation)
    /// - OutputSettings (Options pattern with data annotation validation)
    /// - HTTP client for Ollama API
    /// - IOllamaHealthChecker (OllamaHealthChecker) - pre-flight check before processing
    /// - IInputProvider (FileInputProvider)
    /// - ITextPreprocessor (TextPreprocessor)
    /// - ConsoleOutputWriter, FileOutputWriter, and CompositeOutputWriter
    /// - ISummarizer (OllamaSummarizer)
    /// - IEvaluator (OllamaEvaluator)
    /// </remarks>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Ollama settings
        services.AddOptions<OllamaSettings>()
            .Bind(configuration.GetSection(OllamaSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Output settings
        services.AddOptions<OutputSettings>()
            .Bind(configuration.GetSection(OutputSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // HTTP client shared by OllamaApiClient and OllamaHealthChecker
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
            // Health check uses a short fixed timeout - independently of inference timeout
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // IO services
        services.AddSingleton<IInputProvider, FileInputProvider>();
        services.AddSingleton<ITextPreprocessor, TextPreprocessor>();
        services.AddSingleton<ConsoleOutputWriter>();
        services.AddSingleton<FileOutputWriter>();
        services.AddSingleton<IOutputWriter>(sp => new CompositeOutputWriter(
        [
            sp.GetRequiredService<ConsoleOutputWriter>(),
            sp.GetRequiredService<FileOutputWriter>()
        ]));

        // Ollama services
        services.AddSingleton<ISummarizer, OllamaSummarizer>();
        services.AddSingleton<IEvaluator, OllamaEvaluator>();

        return services;
    }
}