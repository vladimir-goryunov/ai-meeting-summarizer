// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Domain;
using AiMeetingSummarizer.Infrastructure.Ollama;
using Microsoft.Extensions.Options;

namespace AiMeetingSummarizer.Infrastructure.IO;

/// <summary>
/// Writes the meeting summary, quality evaluation, and run statistics to standard output.
/// </summary>
public sealed class ConsoleOutputWriter : IOutputWriter
{
    private const int SeparatorWidth = 72;
    private static readonly string Separator = new('─', SeparatorWidth);
    private static readonly string ThickSeparator = new('═', SeparatorWidth);

    private readonly OllamaSettings _ollamaSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleOutputWriter"/> class.
    /// </summary>
    /// <param name="ollamaSettings">Ollama configuration used to display the model name in the statistics section.</param>
    public ConsoleOutputWriter(IOptions<OllamaSettings> ollamaSettings)
    {
        _ollamaSettings = ollamaSettings.Value;
    }

    /// <inheritdoc />
    public Task WriteAsync(
        SummaryResult summary,
        EvaluationResult evaluation,
        RunStatistics statistics,
        CancellationToken cancellationToken = default)
    {
        WriteSummarySection(summary);
        WriteEvaluationSection(evaluation);
        WriteStatisticsSection(statistics, _ollamaSettings.ModelName);
        return Task.CompletedTask;
    }

    private static void WriteSummarySection(SummaryResult summary)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine();
        Console.WriteLine(ThickSeparator);
        Console.WriteLine("  MEETING SUMMARY");
        Console.WriteLine($"  Generated: {summary.GeneratedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine(ThickSeparator);
        Console.WriteLine();
        Console.ForegroundColor = originalColor;
        Console.WriteLine(summary.Content);
        Console.WriteLine();
    }

    private static void WriteEvaluationSection(EvaluationResult evaluation)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(ThickSeparator);
        Console.WriteLine("  MEETING SUMMARY QUALITY EVALUATION");
        Console.WriteLine(ThickSeparator);  
        Console.WriteLine();
        Console.ForegroundColor = originalColor;

        WriteScoreTable(evaluation);

        Console.WriteLine();
        Console.WriteLine("Overall Summary Assessment:");
        Console.WriteLine($"  {evaluation.OverallComment}");

        if (evaluation.Improvements.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Suggested Improvements:");
            foreach (var improvement in evaluation.Improvements)
                Console.WriteLine($"  • {improvement}");
        }

        Console.WriteLine();
        Console.WriteLine(Separator);
    }

    private static void WriteStatisticsSection(RunStatistics stats, string modelName)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine();
        Console.WriteLine(ThickSeparator);
        Console.WriteLine("  RUN STATISTICS");
        Console.WriteLine(ThickSeparator);
        Console.ForegroundColor = originalColor;
        Console.WriteLine($"  Model               : {modelName}");
        Console.WriteLine($"  Summarization       : {stats.SummarizationDuration.TotalSeconds:0.0}s");
        Console.WriteLine($"  Evaluation          : {stats.EvaluationDuration.TotalSeconds:0.0}s");
        Console.WriteLine($"  Total inference     : {stats.TotalDuration.TotalSeconds:0.0}s");
        Console.WriteLine(Separator);
        Console.WriteLine();
    }

    private static void WriteScoreTable(EvaluationResult evaluation)
    {
        const int labelWidth = 28;
        const int scoreWidth = 7;
        const int commentWidth = 33;

        var headerFmt = $"{{0,-{labelWidth}}} {{1,-{scoreWidth}}} {{2,-{commentWidth}}}";
        var header = string.Format(headerFmt, "Criterion", "Score", "Comment");
        var rowSep = new string('-', labelWidth + scoreWidth + commentWidth + 2);

        Console.WriteLine(header);
        Console.WriteLine(rowSep);

        WriteScoreRow("Completeness", evaluation.Completeness, labelWidth, scoreWidth, commentWidth);
        WriteScoreRow("Accuracy", evaluation.Accuracy, labelWidth, scoreWidth, commentWidth);
        WriteScoreRow("Structure Compliance", evaluation.StructureCompliance, labelWidth, scoreWidth, commentWidth);
        WriteScoreRow("Action Item Extraction", evaluation.ActionItemExtraction, labelWidth, scoreWidth, commentWidth);
        WriteScoreRow("Clarity", evaluation.Clarity, labelWidth, scoreWidth, commentWidth);

        Console.WriteLine(rowSep);

        Console.WriteLine(string.Format(headerFmt,
            "TOTAL", $"{evaluation.TotalScore}/{evaluation.MaxTotalScore}", evaluation.Grade));
    }

    private static void WriteScoreRow(
        string label, EvaluationScore score,
        int labelWidth, int scoreWidth, int commentWidth)
    {
        var scoreStr = $"{score.Score}/{score.MaxScore}";
        var comment = score.Comment.Length > commentWidth - 1
            ? score.Comment[..(commentWidth - 4)] + "..."
            : score.Comment;

        Console.WriteLine(string.Format(
            $"{{0,-{labelWidth}}} {{1,-{scoreWidth}}} {{2,-{commentWidth}}}",
            label, scoreStr, comment));
    }
}
