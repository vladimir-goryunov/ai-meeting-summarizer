// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

using CommandLine;

namespace AiMeetingSummarizer.Presentation;

/// <summary>
/// The main entry point for the AI Meeting Summarizer console application.
/// Handles command-line argument parsing and application lifecycle management.
/// </summary>
public class Program
{

    /// <summary>
    /// The main entry point of the application.
    /// Parses command-line arguments using CommandLineParser library and executes the appropriate workflow.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application. Supports --input and --output options.</param>
    /// <returns>
    /// A task representing the asynchronous operation with an exit code:
    /// 0 - Success
    /// 1 - Parse error or invalid arguments
    /// 2 - Operation cancelled by user
    /// 3 - Input file not found
    /// 4 - Ollama service error
    /// 5 - Unexpected error
    /// </returns>
    public static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<CommandLineOptions>(args)
            .MapResult(
                async options => (int) await RunWithOptionsAsync(options),
                async _ => (int) await Task.FromResult(1)
            );
    }

    private static async Task<ExitCode> RunWithOptionsAsync(CommandLineOptions options)
    {
        await using var bootstrapper = new ApplicationBootstrapper(options);
        using var cts = SetupCancellation(options);

        return await bootstrapper.RunAsync(cts.Token);
    }

    private static CancellationTokenSource SetupCancellation(CommandLineOptions options)
    {
        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            if (options.Verbose)
            {
                Console.WriteLine("\nCancellation requested. Stopping operation...");
            }
            cts.Cancel();
        };

        return cts;
    }
}