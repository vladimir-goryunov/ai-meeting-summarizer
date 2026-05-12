// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Domain;
using AiMeetingSummarizer.Infrastructure.Ollama;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiMeetingSummarizer.Infrastructure.IO;

/// <summary>
/// Writes the meeting summary, quality evaluation, and run statistics to a Markdown file.
/// Creates the output directory if it doesn't exist and handles file write permissions.
/// </summary>
public sealed class FileOutputWriter : IOutputWriter
{
    private readonly OutputSettings _outputSettings;
    private readonly OllamaSettings _ollamaSettings;
    private readonly ILogger<FileOutputWriter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileOutputWriter"/> class.
    /// </summary>
    /// <param name="outputSettings">Configuration settings including output file path.</param>
    /// <param name="ollamaSettings">Ollama configuration used to display the model name in the statistics section.</param>
    /// <param name="logger">Logger for file write operations and errors.</param>
    public FileOutputWriter(
        IOptions<OutputSettings> outputSettings,
        IOptions<OllamaSettings> ollamaSettings,
        ILogger<FileOutputWriter> logger)
    {
        _outputSettings = outputSettings.Value;
        _ollamaSettings = ollamaSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task WriteAsync(
        SummaryResult summary,
        EvaluationResult evaluation,
        RunStatistics statistics,
        CancellationToken cancellationToken = default)
    {
        var markdown = BuildMarkdown(summary, evaluation, statistics, _ollamaSettings.ModelName);

        await File.WriteAllTextAsync(_outputSettings.FilePath, markdown, cancellationToken);

        _logger.LogInformation("Output written to: {FilePath}", Path.GetFullPath(_outputSettings.FilePath));
    }

    private static string BuildMarkdown(
        SummaryResult summary,
        EvaluationResult evaluation,
        RunStatistics statistics,
        string modelName)
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
        sb.AppendLine("## Quality Evaluation");
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

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Run Statistics");
        sb.AppendLine();
        sb.AppendLine("| | |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| Model | `{modelName}` |");
        sb.AppendLine($"| Summarization | {statistics.SummarizationDuration.TotalSeconds:0.0}s |");
        sb.AppendLine($"| Evaluation | {statistics.EvaluationDuration.TotalSeconds:0.0}s |");
        sb.AppendLine($"| Total inference | {statistics.TotalDuration.TotalSeconds:0.0}s |");

        return sb.ToString();
    }

    private static void AppendScoreRow(System.Text.StringBuilder sb, string label, EvaluationScore score)
    {
        sb.AppendLine($"| {label} | {score.Score}/{score.MaxScore} | {EscapeMarkdown(score.Comment)} |");
    }

    private static string EscapeMarkdown(string text) =>
        text.Replace("|", "\\|").Replace("\n", " ").Replace("\r", string.Empty);
}
