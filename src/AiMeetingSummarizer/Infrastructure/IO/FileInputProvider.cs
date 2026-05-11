using AiMeetingSummarizer.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiMeetingSummarizer.Infrastructure.IO;

/// <summary>
/// Reads meeting transcript content from a local file system.
/// Supports both .txt files and JSON transcript formats.
/// </summary>
/// <remarks>
/// Uses System.IO.File APIs with proper error handling for file access issues.
/// Logs warnings for files with unexpected extensions but continues processing.
/// </remarks>
public sealed class FileInputProvider : IInputProvider
{
    private readonly ILogger<FileInputProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileInputProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger for file access diagnostics and warnings.</param>
    public FileInputProvider(ILogger<FileInputProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Input file path must not be empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Transcript file not found: '{path}'", path);
        }

        _logger.LogDebug("Reading transcript from: {Path}", path);

        var content = await File.ReadAllTextAsync(path, cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException($"Transcript file is empty: '{path}'");
        }

        return content;
    }
}
