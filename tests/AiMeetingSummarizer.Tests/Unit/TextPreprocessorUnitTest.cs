using AiMeetingSummarizer.Infrastructure.Processing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AiMeetingSummarizer.Tests.Unit;

public sealed class TextPreprocessorUnitTest
{
    private readonly TextPreprocessor _sut = new(NullLogger<TextPreprocessor>.Instance);

    [Fact]
    public void Preprocess_ReturnsEmpty_WhenInputIsEmpty()
    {
        var result = _sut.Preprocess(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Preprocess_ReturnsEmpty_WhenInputIsWhitespace()
    {
        var result = _sut.Preprocess("   \n\t  ");

        result.Should().BeEmpty();
    }

    [Fact]
    public void Preprocess_NormalizesMultipleSpacesToSingle()
    {
        var result = _sut.Preprocess("John:   hello   world");

        result.Should().Be("John: hello world");
    }

    [Fact]
    public void Preprocess_TrimsLeadingAndTrailingWhitespace()
    {
        var result = _sut.Preprocess("  some text  ");

        result.Should().Be("some text");
    }

    [Fact]
    public void Preprocess_CollapsesMoreThanTwoConsecutiveNewlines()
    {
        var result = _sut.Preprocess("line one\n\n\n\nline two");

        result.Should().Be("line one\n\nline two");
    }

    [Fact]
    public void Preprocess_PreservesDoubleLinesInPlainText()
    {
        var input = "line one\n\nline two";

        var result = _sut.Preprocess(input);

        result.Should().Be("line one\n\nline two");
    }

    [Fact]
    public void Preprocess_ExtractsSpeakerDialogueFromJsonArray()
    {
        var json = """
                   [
                     { "speaker": "Alice", "text": "Hello team." },
                     { "speaker": "Bob",   "text": "Hi Alice." }
                   ]
                   """;

        var result = _sut.Preprocess(json);

        result.Should().Contain("Alice: Hello team.");
        result.Should().Contain("Bob: Hi Alice.");
    }

    [Fact]
    public void Preprocess_SkipsJsonElementsWithEmptyText()
    {
        var json = """
                   [
                     { "speaker": "Alice", "text": "" },
                     { "speaker": "Bob",   "text": "Non-empty." }
                   ]
                   """;

        var result = _sut.Preprocess(json);

        result.Should().NotContain("Alice:");
        result.Should().Contain("Bob: Non-empty.");
    }

    [Fact]
    public void Preprocess_FallsBackToPlainText_WhenJsonIsInvalid()
    {
        var malformed = "[not valid json";

        var result = _sut.Preprocess(malformed);

        // Should not throw; falls back to whitespace normalization
        result.Should().Be("[not valid json");
    }

    [Fact]
    public void Preprocess_HandlesJsonWithNoSpeakerProperty()
    {
        var json = """[ { "text": "Anonymous line." } ]""";

        var result = _sut.Preprocess(json);

        result.Should().Contain("Anonymous line.");
        result.Should().NotContain(":");
    }
}
