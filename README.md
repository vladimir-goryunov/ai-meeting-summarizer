# AI Meeting Summarizer

A .NET 8 console application that automatically generates structured summaries from meeting transcripts using a local large language model via [Ollama](https://ollama.com). All processing happens locally - no data leaves your machine

## Features

- Accepts plain-text transcripts or structured JSON transcript formats (e.g., Whisper output)
- Generates structured summaries: participants, per-person status, action items
- Evaluates summary quality using an LLM-as-a-Judge approach with a five-criterion rubric
- Outputs results to both the console and a Markdown file
- Fully local inference - suitable for NDA-sensitive content

## Architecture

The solution follows Clean Architecture principles with the pipeline structured as:

```
Read -> Preprocess -> Summarize -> Evaluate -> Write
```

Logical layers are organized as namespaces within a single project:

| Namespace | Responsibility |
|---|---|
| `Domain` | Core models (`SummaryResult`, `EvaluationResult`) |
| `Application` | Pipeline interfaces and orchestrator |
| `Infrastructure.Ollama` | Ollama API client, summarizer, evaluator |
| `Infrastructure.IO` | File reader, console writer, file writer |
| `Infrastructure.Processing` | Text normalization and transcript format detection |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Ollama](https://ollama.com) running locally
- A supported model pulled and available

### Recommended models

The following models are recommended based on a balance of quality and performance on consumer hardware:

| Model | Size | Command |
|---|---|---|
| `qwen2.5:7b-instruct` | ~4.7 GB | `ollama pull qwen2.5` |
| `phi4: latest` | ~9.1 GB | `ollama pull phi4` |
| `llama3: latest` | ~4.7 GB | `ollama pull llama3` |
| `mistral` | ~4.1 GB | `ollama pull mistral` |


> For machines with limited VRAM, `mistral` is a lighter alternative.

## Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/vladimir-goryunov/ai-meeting-summarizer.git
cd ai-meeting-summarizer

# 2. Ensure Ollama is running
ollama serve

# 3. Pull the default model
ollama pull llama3

# 4. Run against a transcript
dotnet run --project src/AiMeetingSummarizer -- samples/sample-status-meeting.txt
```

The summary is printed to the console and saved to `sumamry.md` in the working directory.

## Usage

```
AiMeetingSummarizer <path-to-transcript> [output-file]
```

| Argument | Description |
|---|---|
| `path-to-transcript` | Path to the `.txt` or `.json` transcript file (required) |
| `output-file` | Path for the Markdown output file (optional, default: `summary.md`) |

**Examples:**

```
# Use default output path
dotnet run --project src/AiMeetingSummarizer -- meeting.txt

# Specify custom output path
dotnet run --project src/AiMeetingSummarizer -- meeting.txt reports/2025-01-15.md

# Run the compiled binary directly
./AiMeetingSummarizer meeting.txt
```

## Input Format

The application accepts two transcript formats:

**Plain text dialogue:**
```
John: Let's start with the API issue from yesterday.
Anna: I checked the logs - there are a lot of timeout errors.
Mike: Could be a database bottleneck.
```

**JSON array (e.g., Whisper output):**
```json
[
  { "start": 0.0, "end": 3.5, "speaker": "John", "text": "Let's start with the API issue." },
  { "start": 3.6, "end": 6.2, "speaker": "Anna", "text": "I checked the logs." }
]
```

JSON input is automatically detected and converted to plain dialogue before processing.

## Output Format

**Console output** - formatted ASCII table with summary and evaluation scores.

**Markdown file** - structured document with a quality evaluation table:

```markdown
# Meeting Summary

Participants: John, Anna, Mike

Summary:
The team discussed production instability in the orders API...

Personally (one by one):

John:
- Completed/What was done: Identified the issue and initiated investigation
- In Progress: Coordinating the fix
- Blockers: Root cause not yet confirmed

...

Action Items:
- Anna: Investigate database timeouts
- Mike: Sync with frontend team on notification feature

---

## Quality Evaluation

| Criterion | Score | Comment |
|---|---|---|
| Completeness | 2/2 | All participants covered |
| ...
| **Total** | **8/10** | **Good** |
```

## Configuration

Settings are controlled via `appsettings.json`:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ModelName": "llama3",
    "TimeoutSeconds": 180
  },
  "Output": {
    "FilePath": "summary.md"
  }
}
```

## Running Tests

```
dotnet test
```

Test coverage targets >90% for all new classes. Tests are organized as unit tests only (`*UnitTest`) using xUnit and Moq.

```
# With coverage report
dotnet test --collect:"XPlat Code Coverage"
```

## Quality Evaluation Rubric

The LLM-as-a-Judge evaluator scores summaries on five criteria (0–2 each):

| Criterion | 2 | 1 | 0 |
|---|---|---|---|
| Completeness | All participants + all topics | Minor omissions | Missing key parts |
| Accuracy | No hallucinations | Minor assumptions | Incorrect information |
| Structure Compliance | Fully matches format | Minor deviations | Broken structure |
| Action Item Extraction | All tasks extracted and assigned | Some missing | No usable items |
| Clarity | Clear and concise | Slightly verbose | Hard to interpret |

**Final score interpretation:**
- 9–10: Excellent
- 7–8: Good
- 5–6: Acceptable
- < 5: Needs Improvement

## Limitations

- Text input only - audio transcription is not supported yet
- No speaker diarization for unmarked transcripts
- Context window bounded by the chosen model (typically 4k–8k tokens)
- Long transcripts may be truncated; chunking is not implemented yet

## Future Directions

- Audio input via [Whisper](https://github.com/openai/whisper) integration
- Speaker diarization for unmarked transcripts
- Long-document chunking and multi-pass summarization
- Web or desktop UI
- Integration with external tools (Jira, Confluence, Slack)

## License

MIT
