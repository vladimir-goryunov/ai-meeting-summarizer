// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Domain;
using Microsoft.Extensions.Logging;

namespace AiMeetingSummarizer.Application;

/// <summary>
/// Orchestrates the full meeting analysis pipeline:
/// read -> preprocess -> summarize -> evaluate -> write.
/// </summary>
public sealed class MeetingAnalysisOrchestrator
{
    private readonly IInputProvider _inputProvider;
    private readonly ITextPreprocessor _preprocessor;
    private readonly ISummarizer _summarizer;
    private readonly IEvaluator _evaluator;
    private readonly IOutputWriter _outputWriter;
    private readonly ILogger<MeetingAnalysisOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="MeetingAnalysisOrchestrator"/>.
    /// </summary>
    public MeetingAnalysisOrchestrator(
        IInputProvider inputProvider,
        ITextPreprocessor preprocessor,
        ISummarizer summarizer,
        IEvaluator evaluator,
        IOutputWriter outputWriter,
        ILogger<MeetingAnalysisOrchestrator> logger)
    {
        _inputProvider = inputProvider;
        _preprocessor = preprocessor;
        _summarizer = summarizer;
        _evaluator = evaluator;
        _outputWriter = outputWriter;
        _logger = logger;
    }

    /// <summary>
    /// Executes the full analysis pipeline for the transcript at the given path.
    /// </summary>
    /// <param name="inputPath">Path to the meeting transcript file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task RunAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting meeting analysis for: {InputPath}", inputPath);

        var rawText = await _inputProvider.ReadAsync(inputPath, cancellationToken);
        _logger.LogDebug("Transcript loaded. Length: {Length} characters", rawText.Length);

        var cleanText = _preprocessor.Preprocess(rawText);
        _logger.LogDebug("Preprocessing complete. Cleaned length: {Length} characters", cleanText.Length);

        _logger.LogInformation("Generating summary...");
        var summaryWatch = Stopwatch.StartNew();
        var summary = await _summarizer.SummarizeAsync(cleanText, cancellationToken);
        summaryWatch.Stop();
        _logger.LogInformation("Summary generated in {Elapsed:0.0}s", summaryWatch.Elapsed.TotalSeconds);

        _logger.LogInformation("Evaluating summary quality...");
        var evalWatch = Stopwatch.StartNew();
        var evaluation = await _evaluator.EvaluateAsync(cleanText, summary.Content, cancellationToken);
        evalWatch.Stop();
        _logger.LogInformation(
            "Evaluation complete in {Elapsed:0.0}s. Score: {Score}/{Max} ({Grade})",
            evalWatch.Elapsed.TotalSeconds, evaluation.TotalScore, evaluation.MaxTotalScore, evaluation.Grade);

        var statistics = new RunStatistics
        {
            SummarizationDuration = summaryWatch.Elapsed,
            EvaluationDuration = evalWatch.Elapsed
        };

        await _outputWriter.WriteAsync(summary, evaluation, statistics, cancellationToken);

        _logger.LogInformation(
            "Analysis complete. Total inference time: {Total:0.0}s",
            statistics.TotalDuration.TotalSeconds);
    }
}