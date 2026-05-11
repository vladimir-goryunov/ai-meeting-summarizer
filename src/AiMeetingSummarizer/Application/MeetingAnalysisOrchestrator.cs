// Copyright (c) 2026 Vladimir Goryunov https://github.com/vladimir-goryunov
// Licensed under the MIT License. See LICENSE in the project root for license information.

using AiMeetingSummarizer.Application.Interfaces;
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
    /// Initializes new instance of MeetingAnalysisOrchestrator
    /// </summary>
    /// <param name="inputProvider">Provide raw meeting transcript text from a source.</param>
    /// <param name="preprocessor">Handles entry transcript formats.</param>
    /// <param name="summarizer">Generates structured meeting summaries by sending the transcript to a local Ollama model.</param>
    /// <param name="evaluator">Evaluates summary quality.</param>
    /// <param name="outputWriter">Writes the summary and evaluation results</param>
    /// <param name="logger">Writes the log of configured type</param>
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
        var summary = await _summarizer.SummarizeAsync(cleanText, cancellationToken);

        _logger.LogInformation("Evaluating summary quality...");
        var evaluation = await _evaluator.EvaluateAsync(cleanText, summary.Content, cancellationToken);

        _logger.LogInformation(
            "Evaluation complete. Score: {Score}/{Max} ({Grade})",
            evaluation.TotalScore, evaluation.MaxTotalScore, evaluation.Grade);

        await _outputWriter.WriteAsync(summary, evaluation, cancellationToken);

        _logger.LogInformation("Analysis complete");
    }
}
