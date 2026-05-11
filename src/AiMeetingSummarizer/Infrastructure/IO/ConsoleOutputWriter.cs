using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Domain;

namespace AiMeetingSummarizer.Infrastructure.IO;

/// <summary>
/// Writes the meeting summary and quality evaluation to standard output.
/// </summary>
public sealed class ConsoleOutputWriter : IOutputWriter
{
    private const int SeparatorWidth = 72;
    private static readonly string Separator = new('─', SeparatorWidth);
    private static readonly string ThickSeparator = new('═', SeparatorWidth);

    /// <inheritdoc />
    public Task WriteAsync(SummaryResult summary, EvaluationResult evaluation, CancellationToken cancellationToken = default)
    {
        WriteSummarySection(summary);
        WriteEvaluationSection(evaluation);
        return Task.CompletedTask;
    }

    private static void WriteSummarySection(SummaryResult summary)
    {
        Console.WriteLine();
        Console.WriteLine(ThickSeparator);
        Console.WriteLine("  MEETING SUMMARY");
        Console.WriteLine($"  Generated: {summary.GeneratedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine(ThickSeparator);
        Console.WriteLine();
        Console.WriteLine(summary.Content);
        Console.WriteLine();
    }

    private static void WriteEvaluationSection(EvaluationResult evaluation)
    {
        Console.WriteLine(ThickSeparator);
        Console.WriteLine("  QUALITY EVALUATION");
        Console.WriteLine(ThickSeparator);
        Console.WriteLine();

        WriteScoreTable(evaluation);

        Console.WriteLine();
        Console.WriteLine("Overall Assessment:");
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

        var totalLabel = "TOTAL";
        var totalScore = $"{evaluation.TotalScore}/{evaluation.MaxTotalScore}";
        var grade = evaluation.Grade;
        Console.WriteLine(string.Format(headerFmt, totalLabel, totalScore, grade));
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
