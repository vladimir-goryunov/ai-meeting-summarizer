namespace AiMeetingSummarizer.Presentation
{
    /// <summary>
    /// Defines standardized exit codes for the application.
    /// These codes are returned to the operating system to indicate execution status.
    /// </summary>
    public enum ExitCode
    {
        /// <summary>
        /// Operation completed successfully
        /// </summary>
        Success = 0,

        /// <summary>
        /// Invalid command-line arguments or parse error
        /// </summary>
        InvalidArguments = 1,

        /// <summary>
        /// Operation was cancelled by user (Ctrl+C)
        /// </summary>
        OperationCancelled = 2,

        /// <summary>
        /// Input file not found or inaccessible
        /// </summary>
        InputFileNotFound = 3,

        /// <summary>
        /// Ollama service error (connection, timeout, or invalid response)
        /// </summary>
        OllamaServiceError = 4,

        /// <summary>
        /// Unexpected internal error
        /// </summary>
        UnexpectedError = 5
    }
}
