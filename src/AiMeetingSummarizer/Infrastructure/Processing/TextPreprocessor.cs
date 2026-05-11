using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AiMeetingSummarizer.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiMeetingSummarizer.Infrastructure.Processing;

/// <summary>
/// Normalizes meeting transcript text before LLM processing.
/// Handles both plain-text dialogue and structured JSON transcript formats.
/// </summary>
/// <remarks>
/// Processing pipeline:
/// 1. Trim whitespace and validate input
/// 2. Detect JSON format (arrays of speaker/message objects)
/// 3. Extract and format dialogue from JSON
/// 4. Normalize whitespace (single spaces, trim lines)
/// 5. Fallback to plain text normalization if JSON parsing fails
/// </remarks>
public sealed partial class TextPreprocessor : ITextPreprocessor
{
    private readonly ILogger<TextPreprocessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextPreprocessor"/> class.
    /// </summary>
    /// <param name="logger">Logger for debugging and warning messages during text processing.</param>
    public TextPreprocessor(ILogger<TextPreprocessor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Preprocess(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        var trimmed = rawText.Trim();

        if (IsJsonArray(trimmed))
        {
            _logger.LogDebug("Detected JSON transcript format - extracting dialogue");
            var extracted = TryExtractDialogueFromJson(trimmed);

            if (extracted is not null)
            {
                return NormalizeWhitespace(extracted);
            }

            _logger.LogWarning("JSON transcript detected but could not be parsed - falling back to plain text normalization");
        }

        return NormalizeWhitespace(trimmed);
    }

    private static bool IsJsonArray(string text) =>
        text.StartsWith('[') && text.EndsWith(']');

    private string? TryExtractDialogueFromJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var sb = new StringBuilder();

            foreach (var element in root.EnumerateArray())
            {
                var speaker = element.TryGetProperty("speaker", out var s) ? s.GetString() : null;
                var text = element.TryGetProperty("text", out var t) ? t.GetString() : null;

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(speaker))
                {
                    sb.AppendLine($"{speaker}: {text.Trim()}");
                }
                else
                {
                    sb.AppendLine(text.Trim());
                }
            }

            return sb.ToString();
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "JSON parse failed during transcript extraction");
            return null;
        }
    }

    private static string NormalizeWhitespace(string text)
    {
        // Collapse multiple spaces into one
        var singleSpaced = MultipleSpaces().Replace(text, " ");

        // Collapse more than two consecutive newlines into two
        var normalizedNewlines = ExcessiveNewlines().Replace(singleSpaced, "\n\n");

        return normalizedNewlines.Trim();
    }

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex MultipleSpaces();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessiveNewlines();
}
