namespace AiMeetingSummarizer.Infrastructure.IO;

/// <summary>
/// Configuration options for the file output writer.
/// </summary>
public sealed class OutputSettings
{
    /// <summary>Name of section in config</summary>
    public const string SectionName = "Output";

    /// <summary>Path of the output Markdown file.</summary>
    public string FilePath { get; set; } = "summary.md";
}
