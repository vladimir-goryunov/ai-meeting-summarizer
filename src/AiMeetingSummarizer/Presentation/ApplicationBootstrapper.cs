using AiMeetingSummarizer.Application;
using AiMeetingSummarizer.Infrastructure;
using AiMeetingSummarizer.Infrastructure.IO;
using AiMeetingSummarizer.Infrastructure.Ollama;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace AiMeetingSummarizer.Presentation;

/// <summary>
/// Bootstraps and manages the application lifecycle including dependency injection,
/// configuration, and orchestration of the meeting analysis workflow.
/// Implements IAsyncDisposable for proper resource cleanup.
/// </summary>
public class ApplicationBootstrapper : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly CommandLineOptions _options;
    private readonly ILogger<ApplicationBootstrapper> _logger;

    /// <summary>
    /// Initializes a new instance of the ApplicationBootstrapper class.
    /// Builds the dependency injection container and configures the application host.
    /// </summary>
    /// <param name="options">Command-line options containing runtime configuration parameters.</param>
    public ApplicationBootstrapper(CommandLineOptions options)
    {
        _options = options;
        _host = CreateHostBuilder(options).Build();
        _logger = _host.Services.GetRequiredService<ILogger<ApplicationBootstrapper>>();
    }

    /// <summary>
    /// Executes the meeting analysis workflow asynchronously.
    /// Validates input, sets up logging, runs the orchestrator, and handles all exceptions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for gracefully stopping the operation.</param>
    /// <returns>Standardized exit codes for the application</returns>
    public async Task<ExitCode> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateOptions();

            if (_options.Verbose)
            {
                PrintStartupInfo();
            }

            var orchestrator = _host.Services.GetRequiredService<MeetingAnalysisOrchestrator>();
            await orchestrator.RunAsync(_options.InputPath, cancellationToken);

            if (_options.Verbose)
            {
                Console.WriteLine("Meeting analysis completed successfully.");
            }

            return ExitCode.Success;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Operation cancelled by user.");
            return ExitCode.OperationCancelled;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Input file not found: {FilePath}", _options.InputPath);
            return ExitCode.InputFileNotFound;
        }
        catch (OllamaException ex)
        {
            _logger.LogError(ex, "Ollama service error.");
            return ExitCode.OllamaServiceError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
            return ExitCode.UnexpectedError;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _host.Dispose();
        }
    }

    private void ValidateOptions()
    {
        if (!File.Exists(_options.InputPath))
        {
            throw new FileNotFoundException($"Input file not found: {_options.InputPath}");
        }
    }
    
    private void PrintStartupInfo()
    {
        Console.WriteLine($"Input file: {_options.InputPath}");
        Console.WriteLine($"Output file: {_options.OutputPath ?? "output.md (default)"}");
        Console.WriteLine("Starting meeting analysis...");
    }

    private static IHostBuilder CreateHostBuilder(CommandLineOptions options) =>
        Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(options);
                services.AddApplication();
                services.AddInfrastructure(context.Configuration);

                if (!string.IsNullOrWhiteSpace(options.OutputPath))
                {
                    services.PostConfigure<OutputSettings>(opts => opts.FilePath = options.OutputPath);
                }
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(consoleOptions =>
                {
                    consoleOptions.SingleLine = true;
                    consoleOptions.TimestampFormat = "HH:mm:ss ";
                    if (options.NoColor)
                    {
                        consoleOptions.ColorBehavior = LoggerColorBehavior.Disabled;
                    }
                });

                if (!options.Verbose)
                {
                    logging.AddFilter("Microsoft", LogLevel.Warning);
                    logging.AddFilter("System", LogLevel.Warning);
                }
            });
}