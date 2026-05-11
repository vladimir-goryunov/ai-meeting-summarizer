using Microsoft.Extensions.DependencyInjection;

namespace AiMeetingSummarizer.Presentation;

/// <summary>
/// Dependency injection registration extension methods for the Presentation layer.
/// Configures CLI-specific services and application bootstrapping components.
/// </summary>
public static class PresentationDependencyInjection
{
    /// <summary>
    /// Registers Presentation layer services with the dependency injection container.
    /// Adds CommandLineOptions as a singleton and ApplicationBootstrapper as scoped.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="options">Command-line options to be registered as a singleton service.</param>
    /// <returns>The IServiceCollection instance for method chaining.</returns>
    /// <remarks>
    /// Registered services:
    /// - CommandLineOptions (Singleton) - Contains parsed CLI arguments
    /// - ApplicationBootstrapper (Scoped) - Manages application lifecycle
    /// </remarks>
    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        CommandLineOptions options)
    {
        services.AddSingleton(options);
        services.AddScoped<ApplicationBootstrapper>();

        return services;
    }
}
