// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

namespace AiMeetingSummarizer.Infrastructure.Ollama;

/// <summary>
/// Loads LLM prompt text files from the <c>Infrastructure/Templates</c> directory.
/// </summary>
/// <remarks>
/// Prompts are stored as plain <c>.txt</c> files so they can be edited without recompiling.
/// This class is used exclusively in the DI registration layer to load prompts once at startup
/// and inject the resulting strings into <see cref="OllamaSummarizer"/> and <see cref="OllamaEvaluator"/>.
/// </remarks>
public sealed class PromptLoader
{
    private readonly string _templatesDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptLoader"/> class.
    /// </summary>
    /// <param name="baseDirectory">
    /// Root directory from which the <c>Infrastructure/Templates</c> sub-path is resolved.
    /// Defaults to <see cref="AppContext.BaseDirectory"/> when not supplied.
    /// </param>
    public PromptLoader(string? baseDirectory = null)
    {
        _templatesDirectory = Path.Combine(
            baseDirectory ?? AppContext.BaseDirectory,
            "Infrastructure", "Templates");
    }

    /// <summary>
    /// Reads and returns the full content of the specified prompt file.
    /// </summary>
    /// <param name="fileName">
    /// File name including extension (e.g. <c>SummarizerPrompt.txt</c>).
    /// The file must exist in the <c>Infrastructure/Templates</c> directory.
    /// </param>
    /// <returns>The full text content of the prompt file.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the specified prompt file does not exist.
    /// </exception>
    public string Load(string fileName)
    {
        var path = Path.Combine(_templatesDirectory, fileName);

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Prompt file not found: '{path}'. " +
                "Ensure the file exists and is set to 'Copy to Output Directory'.", path);

        return File.ReadAllText(path);
    }
}