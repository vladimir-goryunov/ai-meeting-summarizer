// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

using AiMeetingSummarizer.Application;
using AiMeetingSummarizer.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AiMeetingSummarizer.Presentation;

/// <summary>
/// Provides extension methods for configuring IHostBuilder with AI Meeting Summarizer specific services.
/// </summary>
public static class HostBuilderExtensions
{

    /// <summary>
    /// Configures the IHostBuilder with AI Meeting Summarizer application services including
    /// configuration sources, dependency injection containers, and logging.
    /// </summary>
    /// <param name="hostBuilder">The IHostBuilder instance to configure.</param>
    /// <param name="options">Command-line options that override configuration settings.</param>
    /// <returns>The configured IHostBuilder instance for method chaining.</returns>
    /// <remarks>
    /// This method adds:
    /// - JSON configuration from appsettings.json and environment-specific files
    /// - Environment variables as configuration sources
    /// - Presentation layer services
    /// - Application layer services  
    /// - Infrastructure layer services
    /// </remarks>
    public static IHostBuilder ConfigureAiMeetingSummarizer(
        this IHostBuilder hostBuilder,
        CommandLineOptions options)
    {
        return hostBuilder
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddPresentation(options);
                services.AddApplication();
                services.AddInfrastructure(context.Configuration);
            });
    }
}