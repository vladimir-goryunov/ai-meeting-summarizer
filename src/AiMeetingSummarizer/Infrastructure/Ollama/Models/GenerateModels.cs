using System.Text.Json.Serialization;

namespace AiMeetingSummarizer.Infrastructure.Ollama.Models;

/// <summary>
/// Represents a request to the Ollama API's /api/generate endpoint.
/// Encapsulates all parameters needed to generate text completions using an LLM model.
/// </summary>
/// <remarks>
/// This request follows the Ollama API specification documented at:
/// https://github.com/ollama/ollama/blob/main/docs/api.md#generate-a-completion
/// 
/// Typical usage:
/// <code>
/// var request = new GenerateRequest
/// {
///     Model = "llama3",
///     Prompt = "Summarize this meeting...",
///     Stream = false,
///     Options = new GenerateOptions { Temperature = 0.7 }
/// };
/// </code>
/// </remarks>
internal sealed record GenerateRequest
{
    /// <summary>
    /// Gets or sets the name of the Ollama model to use for generation.
    /// Examples: "llama3:latest", "mistral", "phi4:latest", "qwen2.5:7b-instruct"
    /// The model must be already pulled locally via `ollama pull &lt;model-name&gt;`.
    /// </summary>
    /// <value>The model identifier as configured in Ollama (see `ollama list`)</value>
    /// <remarks>
    /// This field is required and will be validated on the Ollama server side.
    /// If the model is not available, the API will return a 404 error.
    /// </remarks>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// Gets or sets the input prompt text for the model to complete.
    /// This is the main instruction or context for generation.
    /// </summary>
    /// <value>The full prompt text including any formatting, instructions, or context.</value>
    /// <remarks>
    /// For meeting summarization, this typically includes:
    /// - Instruction/Rubric (role and task description)
    /// - Original transcript
    /// - Generated summary to evaluate
    /// 
    /// The prompt length is limited by the model's context window (typically 2048-4096 tokens for smaller models).
    /// Consider truncating very long transcripts to fit within context limits.
    /// </remarks>
    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the response should be streamed as Server-Sent Events (SSE).
    /// When set to true, responses are delivered incrementally as tokens are generated.
    /// When false, the complete response is returned in a single JSON object.
    /// </summary>
    /// <value>
    /// false for batch processing (default), true for real-time streaming.
    /// </value>
    /// <remarks>
    /// For this application, we use streaming = false because:
    /// - We need complete responses for parsing structured JSON output
    /// - Meeting summaries are relatively short, so latency is acceptable
    /// - Simpler error handling and response processing
    /// 
    /// Set to true if implementing real-time progress indicators or for very long generations.
    /// </remarks>
    [JsonPropertyName("stream")]
    public bool Stream { get; init; } = false;

    /// <summary>
    /// Gets or sets optional generation parameters that control the model's behavior.
    /// When null, Ollama uses its default values for all parameters.
    /// </summary>
    /// <value>
    /// A <see cref="GenerateOptions"/> instance containing sampling parameters, or null to use defaults.
    /// </value>
    /// <remarks>
    /// Key parameters that affect summary quality:
    /// - Temperature (0.0-1.0): Lower values (0.1-0.3) produce more deterministic, factual summaries
    /// - NumPredict: Controls maximum response length, should be set high enough for complete summaries
    /// 
    /// For evaluation tasks where consistency is critical, use low temperature (0.1-0.2).
    /// For creative summarization, use moderate temperature (0.4-0.7).
    /// </remarks>
    [JsonPropertyName("options")]
    public GenerateOptions? Options { get; init; }
}


/// <summary>
/// Represents optional generation parameters for the Ollama API request.
/// These parameters control the sampling behavior and output characteristics of the LLM.
/// </summary>
/// <remarks>
/// These parameters correspond to standard LLM sampling parameters.
/// For more advanced options, extend this record as needed (e.g., top_p, top_k, repeat_penalty).
/// </remarks>
internal sealed record GenerateOptions
{
    /// <summary>
    /// Gets or sets the temperature parameter controlling response randomness.
    /// Higher values (e.g., 0.8) produce more creative but less predictable outputs.
    /// Lower values (e.g., 0.1) produce more deterministic, focused outputs.
    /// </summary>
    /// <value>
    /// A floating point value between 0.0 and 1.0.
    /// Default is 0.1 for this application (conservative, factual responses).
    /// </value>
    /// <remarks>
    /// Temperature vs. Response characteristics:
    /// - 0.0 - 0.2: Very deterministic, best for evaluation and structured data extraction
    /// - 0.3 - 0.5: Balanced, good for general summarization
    /// - 0.6 - 0.8: Creative, better for brainstorming or paraphrasing
    /// - 0.9 - 1.0: Highly random, may produce hallucinations
    /// </remarks>
    [JsonPropertyName("temperature")]
    public double Temperature { get; init; } = 0.1;

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate in the response.
    /// This limits the length of the generated text.
    /// </summary>
    /// <value>
    /// Maximum token count. Default is 2048 tokens for this application.
    /// </value>
    /// <remarks>
    /// Token vs. Character approximation:
    /// - 1 token ≈ 4 characters in English text
    /// - 2048 tokens ≈ 8,000-10,000 characters or ~1500-2000 words
    /// 
    /// Sizing guidelines:
    /// - Meeting summaries typically require 500-1000 tokens
    /// - Evaluation responses require 300-500 tokens  
    /// - 2048 tokens provides a generous safety margin
    /// 
    /// If generating very long summaries or detailed evaluations, consider increasing to 4096 or 
    /// using chunking or multi-pass summarization.
    /// Note: Maximum is limited by the model's context window and available memory.
    /// </remarks>
    [JsonPropertyName("num_predict")]
    public int NumPredict { get; init; } = 2048;
}

/// <summary>
/// Represents a response from the Ollama API's /api/generate endpoint.
/// Contains the generated text completion and metadata about the request.
/// </summary>
/// <remarks>
/// When streaming is enabled (Stream = true), multiple partial responses are received.
/// When streaming is disabled, a single complete response is returned with Done = true.
/// 
/// Error handling:
/// - If the API returns an error response, it will not deserialize to this type
/// - HTTP errors (4xx, 5xx) should be handled by the HttpClient
/// - Check for null or empty Response values as potential generation failures
/// </remarks>
internal sealed record GenerateResponse
{
    /// <summary>
    /// Gets or sets the name of the Ollama model that generated this response.
    /// This should match the model name from the original request.
    /// </summary>
    /// <value>The model identifier that produced the completion.</value>
    /// <remarks>
    /// This field is useful for auditing which model version was used,
    /// especially when load-balancing across multiple models or versions.
    /// Always verify this matches the requested model.
    /// </remarks>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// Gets or sets the generated text completion from the LLM.
    /// Contains the model's response to the prompt, which could be a summary, evaluation, or other output.
    /// </summary>
    /// <value>The complete generated text response.</value>
    /// <remarks>
    /// For evaluations, this field should contain a JSON string that can be parsed into
    /// an <see cref="EvaluationMeetingResponse"/> object.
    /// 
    /// For summaries, this field should contain formatted Markdown text.
    /// 
    /// Always validate and sanitize this response before using:
    /// - Check for empty strings (failed generation)
    /// - Remove any markdown code blocks if returning JSON
    /// - Handle potential hallucinations or off-topic responses
    /// </remarks>
    [JsonPropertyName("response")]
    public required string Response { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the generation is complete.
    /// When streaming is disabled, this should always be true in the single response.
    /// When streaming is enabled, this will be false for intermediate responses and true for the final response.
    /// </summary>
    /// <value>
    /// True if generation is complete, false if more chunks are expected (streaming mode only).
    /// </value>
    /// <remarks>
    /// Usage in streaming scenarios:
    /// <code>
    /// while (!response.Done)
    /// {
    ///     Console.Write(response.Response); // Append partial response
    ///     response = await GetNextChunkAsync();
    /// }
    /// </code>
    /// 
    /// For our non-streaming implementation, we assume Done is always true.
    /// Always check this flag when implementing response processing to avoid incomplete data.
    /// </remarks>
    [JsonPropertyName("done")]
    public bool Done { get; init; }
}
