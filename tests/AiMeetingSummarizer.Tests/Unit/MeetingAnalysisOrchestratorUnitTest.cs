using AiMeetingSummarizer.Application;
using AiMeetingSummarizer.Application.Interfaces;
using AiMeetingSummarizer.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AiMeetingSummarizer.Tests.Unit;

/// <summary>
/// Verifies the orchestration pipeline: correct delegation to each stage
/// and correct data flow between stages.
/// </summary>
public sealed class MeetingAnalysisOrchestratorUnitTest
{
    private const string InputPath = "/some/path/meeting.txt";
    private const string RawTranscript = "raw transcript content";
    private const string CleanTranscript = "clean transcript content";
    private const string SummaryContent = "summary content";

    private readonly Mock<IInputProvider> _inputProvider = new(MockBehavior.Strict);
    private readonly Mock<ITextPreprocessor> _preprocessor = new(MockBehavior.Strict);
    private readonly Mock<ISummarizer> _summarizer = new(MockBehavior.Strict);
    private readonly Mock<IEvaluator> _evaluator = new(MockBehavior.Strict);
    private readonly Mock<IOutputWriter> _outputWriter = new(MockBehavior.Strict);

    private readonly MeetingAnalysisOrchestrator _sut;

    private static readonly SummaryResult DefaultSummary = new()
    {
        Content = SummaryContent,
        GeneratedAt = DateTimeOffset.UtcNow
    };

    private static readonly EvaluationResult DefaultEvaluation = new()
    {
        Completeness = new EvaluationScore(2, 2, "Good"),
        Accuracy = new EvaluationScore(2, 2, "Good"),
        StructureCompliance = new EvaluationScore(2, 2, "Good"),
        ActionItemExtraction = new EvaluationScore(2, 2, "Good"),
        Clarity = new EvaluationScore(2, 2, "Good"),
        OverallComment = "Excellent summary",
        Improvements = []
    };

    public MeetingAnalysisOrchestratorUnitTest()
    {
        _sut = new MeetingAnalysisOrchestrator(
            _inputProvider.Object,
            _preprocessor.Object,
            _summarizer.Object,
            _evaluator.Object,
            _outputWriter.Object,
            NullLogger<MeetingAnalysisOrchestrator>.Instance);
    }

    [Fact]
    public async Task RunAsync_ReadsTranscriptFromProvidedPath()
    {
        SetupHappyPath();

        await _sut.RunAsync(InputPath);

        _inputProvider.Verify(x => x.ReadAsync(InputPath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_PassesPreprocessedTranscriptToSummarizer_NotRawText()
    {
        // Arrange: raw and clean texts are explicitly different to prove the preprocessor output is used
        SetupHappyPath();

        await _sut.RunAsync(InputPath);

        _summarizer.Verify(x => x.SummarizeAsync(CleanTranscript, It.IsAny<CancellationToken>()), Times.Once);
        _summarizer.Verify(x => x.SummarizeAsync(RawTranscript, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_PassesBothTranscriptAndSummaryToEvaluator()
    {
        SetupHappyPath();

        await _sut.RunAsync(InputPath);

        _evaluator.Verify(
            x => x.EvaluateAsync(CleanTranscript, SummaryContent, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_PassesSummaryAndEvaluationToOutputWriter()
    {
        SetupHappyPath();

        await _sut.RunAsync(InputPath);

        _outputWriter.Verify(
            x => x.WriteAsync(DefaultSummary, DefaultEvaluation, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_PropagatesCancellation_WhenTokenIsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _inputProvider
            .Setup(x => x.ReadAsync(InputPath, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await _sut.RunAsync(InputPath, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RunAsync_PropagatesOllamaException_FromSummarizer()
    {
        _inputProvider
            .Setup(x => x.ReadAsync(InputPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RawTranscript);

        _preprocessor
            .Setup(x => x.Preprocess(RawTranscript))
            .Returns(CleanTranscript);

        _summarizer
            .Setup(x => x.SummarizeAsync(CleanTranscript, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Infrastructure.Ollama.OllamaException("Connection refused"));

        var act = async () => await _sut.RunAsync(InputPath);

        await act.Should().ThrowAsync<Infrastructure.Ollama.OllamaException>()
            .WithMessage("Connection refused");
    }

    private void SetupHappyPath()
    {
        _inputProvider
            .Setup(x => x.ReadAsync(InputPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RawTranscript);

        _preprocessor
            .Setup(x => x.Preprocess(RawTranscript))
            .Returns(CleanTranscript);

        _summarizer
            .Setup(x => x.SummarizeAsync(CleanTranscript, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DefaultSummary);

        _evaluator
            .Setup(x => x.EvaluateAsync(CleanTranscript, SummaryContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DefaultEvaluation);

        _outputWriter
            .Setup(x => x.WriteAsync(DefaultSummary, DefaultEvaluation, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
