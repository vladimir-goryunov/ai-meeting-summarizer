namespace AiMeetingSummarizer.Application.Interfaces;

/// <summary>
/// Normalizes and cleans raw transcript text before it is sent to the language model.
/// </summary>
public interface ITextPreprocessor
{
    /// <summary>
    /// Normalizes the input text: removes excessive whitespace, normalizes line breaks,
    /// and converts structured transcript formats (e.g., JSON) into plain dialogue.
    /// </summary>
    /// <param name="rawText">The raw transcript content.</param>
    /// <returns>Cleaned, model-ready text.</returns>
    string Preprocess(string rawText);
}
