using CommandLine;

namespace AiMeetingSummarizer.Presentation
{
    /// <summary>
    /// Represents the command-line options structure for the AI Meeting Summarizer application.
    /// Uses CommandLineParser library attributes for automatic argument parsing and validation.
    /// </summary>
    public sealed class CommandLineOptions
    {
        /// <summary>
        /// Gets the path to the input meeting transcript file (.txt or .json format).
        /// This option is required and can be specified using -i or --input.
        /// </summary>
        /// <example>
        /// # Basic usage
        /// --input transcript.txt
        /// 
        /// # Verbose mode
        /// --i transcript.txt -o summary.md -v
        /// </example>
        [Option('i', "input", Required = true, HelpText = "Path to the .txt or .json meeting transcript")]
        public string InputPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets the optional output file path for the generated Markdown summary.
        /// If not specified, defaults to "output.md" in the current directory.
        /// Can be specified using -o or --output.
        /// </summary>
        /// <example>
        /// # With output file
        /// --input transcript.txt --output summary.md
        /// 
        /// # Verbose mode
        /// --i transcript.txt -o summary.md -v
        /// </example>
        [Option('o', "output", Required = false, HelpText = "Path for the Markdown output (default: output.md)")]
        public string? OutputPath { get; set; }

        /// <summary>
        /// Gets a value indicating whether verbose logging should be enabled.
        /// When enabled, additional diagnostic information is displayed during execution.
        /// Can be enabled using -v or --verbose.
        /// </summary>
        /// <example>
        /// # Verbose mode
        /// --i transcript.txt -o summary.md -v
        /// </example>
        [Option('v', "verbose", Required = false, HelpText = "Enable verbose output")]
        public bool Verbose { get; set; }

        /// <summary>
        /// Gets a value indicating whether colored console output should be disabled.
        /// Useful for environments that don't support ANSI color codes or for log file redirection.
        /// Can be enabled using --no-color.
        /// </summary>
        /// <example>
        /// # Disable colors
        /// --input transcript.txt --no-color
        /// </example>
        [Option("no-color", Required = false, HelpText = "Disable colored console output")]
        public bool NoColor { get; set; }
    }
}
