// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiMeetingSummarizer.Infrastructure.IO;

/// <summary>
/// Writes the meeting summary and quality evaluation to a Markdown file.
/// Creates the output directory if it doesn't exist and handles file write permissions.
/// </summary>
public sealed class FileOutputWriter : IOutputWriter
{
    private readonly OutputSettings _settings;
    private readonly ILogger<FileOutputWriter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileOutputWriter"/> class.
    /// </summary>
    /// <param name="settings">Configuration settings including output file path.</param>
    /// <param name="logger">Logger for file write operations and errors.</param>
    public FileOutputWriter(IOptions<OutputSettings> settings, ILogger<FileOutputWriter> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task WriteAsync(SummaryResult summary, EvaluationResult evaluation, CancellationToken cancellationToken = default)
    {
        var markdown = BuildMarkdown(summary, evaluation);

        await File.WriteAllTextAsync(_settings.FilePath, markdown, cancellationToken);

        _logger.LogInformation("Output written to: {FilePath}", Path.GetFullPath(_settings.FilePath));
    }

    private static string BuildMarkdown(SummaryResult summary, EvaluationResult evaluation)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# Meeting Summary");
        sb.AppendLine();
        sb.AppendLine($"*Generated: {summary.GeneratedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss zzz}*");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(summary.Content);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Meeting Summary Quality Evaluation");
        sb.AppendLine();
        sb.AppendLine("| Criterion | Score | Comment |");
        sb.AppendLine("|---|---|---|");
        AppendScoreRow(sb, "Completeness", evaluation.Completeness);
        AppendScoreRow(sb, "Accuracy", evaluation.Accuracy);
        AppendScoreRow(sb, "Structure Compliance", evaluation.StructureCompliance);
        AppendScoreRow(sb, "Action Item Extraction", evaluation.ActionItemExtraction);
        AppendScoreRow(sb, "Clarity", evaluation.Clarity);
        sb.AppendLine($"| **Total** | **{evaluation.TotalScore}/{evaluation.MaxTotalScore}** | **{evaluation.Grade}** |");
        sb.AppendLine();
        sb.AppendLine("### Overall Assessment");
        sb.AppendLine();
        sb.AppendLine(evaluation.OverallComment);

        if (evaluation.Improvements.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### Suggested Improvements");
            sb.AppendLine();
            foreach (var improvement in evaluation.Improvements)
            {
                sb.AppendLine($"- {improvement}");
            }
        }

        return sb.ToString();
    }

    private static void AppendScoreRow(System.Text.StringBuilder sb, string label, EvaluationScore score)
    {
        sb.AppendLine($"| {label} | {score.Score}/{score.MaxScore} | {EscapeMarkdown(score.Comment)} |");
    }

    private static string EscapeMarkdown(string text) =>
        text.Replace("|", "\\|").Replace("\n", " ").Replace("\r", string.Empty);
}
