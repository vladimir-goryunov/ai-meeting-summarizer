using Microsoft.Extensions.DependencyInjection;

namespace AiMeetingSummarizer.Application;

/// <summary>
/// Dependency injection registration extension methods for the Application layer.
/// Registers core application services and business logic components.
/// </summary>
public static class ApplicationDependencyInjection
{

    /// <summary>
    /// Registers Application layer services with the dependency injection container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The IServiceCollection instance for method chaining.</returns>
    /// <remarks>
    /// Registered services:
    /// - MeetingAnalysisOrchestrator (Singleton) - Orchestrates the meeting analysis workflow
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<MeetingAnalysisOrchestrator>();
        return services;
    }
}
