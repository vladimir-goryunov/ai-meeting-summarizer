using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Domain;

namespace AiMeetingSummarizer.Infrastructure.IO;

/// <summary>
/// Writes output to multiple destinations simultaneously using the Composite pattern.
/// Allows writing meeting summaries and evaluations to several output writers at once.
/// </summary>
/// <example>
/// var composite = new CompositeOutputWriter(new IOutputWriter[] { 
///     new ConsoleOutputWriter(), 
///     new FileOutputWriter() 
/// });
/// await composite.WriteAsync(content); // Writes to both console and file
/// </example>
public sealed class CompositeOutputWriter : IOutputWriter
{
    private readonly IReadOnlyList<IOutputWriter> _writers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeOutputWriter"/> class.
    /// </summary>
    /// <param name="writers">Collection of output writers to composite together.</param>
    /// <exception cref="ArgumentNullException">Thrown when writers is null.</exception>
    /// <exception cref="ArgumentException">Thrown when writers collection is empty.</exception>
    public CompositeOutputWriter(IReadOnlyList<IOutputWriter> writers)
    {
        _writers = writers;
    }

    /// <inheritdoc />
    public async Task WriteAsync(SummaryResult summary, EvaluationResult evaluation, CancellationToken cancellationToken = default)
    {
        foreach (var writer in _writers)
        {
            await writer.WriteAsync(summary, evaluation, cancellationToken);
        }
    }
}
