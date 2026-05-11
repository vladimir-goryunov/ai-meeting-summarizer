// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

using AiMeetingSummarizer.Application;
using AiMeetingSummarizer.Infrastructure;
using AiMeetingSummarizer.Infrastructure.IO;
using AiMeetingSummarizer.Infrastructure.Ollama;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

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
    /// Validates inputs, runs a pre-flight Ollama health check, then invokes the orchestrator.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for gracefully stopping the operation.</param>
    /// <returns>
    /// Standardized exit codes:
    /// 0 - Success
    /// 1 - Parse error or invalid arguments
    /// 2 - Operation cancelled by user
    /// 3 - Input file not found
    /// 4 - Ollama service error during inference
    /// 5 - Unexpected error
    /// 6 - Ollama not reachable (pre-flight)
    /// 7 - Ollama model not installed (pre-flight)
    /// 8 - Invalid configuration (appsettings.json)
    /// </returns>
    public async Task<ExitCode> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateConfiguration();
            ValidateOptions();

            if (_options.Verbose)
            {
                PrintStartupInfo();
            }

            var healthResult = await RunHealthCheckAsync(cancellationToken);
            if (!healthResult.IsHealthy)
            {
                Console.Error.WriteLine(healthResult.ErrorMessage);

                if (!healthResult.IsOllamaReachable)
                    return ExitCode.OllamaNotReachable;

                return ExitCode.OllamaModelNotFound;
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
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Output directory does not exist");
            return ExitCode.InvalidArguments;
        }
        catch (OptionsValidationException ex)
        {
            _logger.LogError("Configuration error: {Failures}", string.Join("; ", ex.Failures));
            return ExitCode.InvalidConfiguration;
        }
        catch (OllamaException ex)
        {
            _logger.LogError(ex, "Ollama service error");
            return ExitCode.OllamaServiceError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
            return ExitCode.UnexpectedError;
        }
    }

    /// <inheritdoc />
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

    private void ValidateConfiguration()
    {
        // Eagerly resolve to trigger ValidateDataAnnotations before any work starts.
        _ = _host.Services.GetRequiredService<IOptions<OllamaSettings>>().Value;
        _ = _host.Services.GetRequiredService<IOptions<OutputSettings>>().Value;
    }

    private void ValidateOptions()
    {
        if (!File.Exists(_options.InputPath))
        {
            throw new FileNotFoundException($"Input file not found: {_options.InputPath}");
        }

        if (_options.OutputPath is not null)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(_options.OutputPath));
            if (directory is not null && !Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(
                    $"Output directory does not exist: '{directory}'. " +
                    "Create the directory first or use a path within an existing directory.");
            }
        }
    }

    private async Task<OllamaHealthCheckResult> RunHealthCheckAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking Ollama availability...");
        var checker = _host.Services.GetRequiredService<IOllamaHealthChecker>();
        return await checker.CheckAsync(cancellationToken);
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