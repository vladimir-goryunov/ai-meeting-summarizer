# AI Meeting Summarizer

![.NET](https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=fff)
![C#](https://custom-icon-badges.demolab.com/badge/C%23-%23239120.svg?logo=cshrp&logoColor=white)
![Ollama](https://img.shields.io/badge/Ollama-000000?logo=ollama&logoColor=white)
![License MIT](https://img.shields.io/badge/License-MIT-green.svg)

A small console application in C# .NET 8, which automatically generates structured summaries from meeting minutes using a local large language model via [Ollama](https://ollama.com). 
All processing happens locally, no data leaves your machine.

## Features

- Accepts plain-text transcripts or structured JSON transcript formats (e.g., Whisper output)
- Generates structured summaries: participants, per-person status, action items
- Evaluates summary quality using an LLM-as-a-Judge approach with a five-criterion rubric
- Pre-flight health check: verifies Ollama is running and the model is installed before processing begins
- Outputs results to both the console and a Markdown file
- Fully local inference (suitable for NDA-sensitive content)

## Project Structure

The solution follows Clean Architecture.
Logical layers are namespaces within a single project, no separate assembly per layer.

```
AiMeetingSummarizer/
│
├── src/AiMeetingSummarizer/
│   │
│   ├── Domain/                         # Core models: no dependencies on other layers
│   │
│   ├── Application/
│   │   └── Interfaces/                 # Contracts: ISummarizer, IEvaluator, IOutputWriter, …
│   │                                   # MeetingAnalysisOrchestrator lives here
│   │
│   ├── Infrastructure/
│   │   ├── Ollama/                     # Ollama API client, summarizer, evaluator, health check
│   │   │   └── Models/                 # JSON request/response DTOs (internal)
│   │   ├── IO/                         # File reader, console writer, Markdown writer, composite
│   │   ├── Processing/                 # Text normalisation and transcript format detection
│   │   └── Templates/                  # Prompt templates (.txt, loaded at build time)
│   │
│   └── Presentation/                   # Entry point, CLI parsing, bootstrapper, exit codes
│
├── tests/AiMeetingSummarizer.Tests/
│   └── Unit/                           # Fast, isolated unit tests: no network, no disk I/O
│
├── results/                            # Sample runs with different models (llama3, phi4, qwen2.5)
│
└── samples/                            # Example transcripts (plain text and JSON formats)
```

The pipeline executed by `MeetingAnalysisOrchestrator` on every run:

```
Read → Preprocess → Summarize → Evaluate → Write
```

| Namespace | Responsibility |
|---|---|
| `Domain` | Core models (`SummaryResult`, `EvaluationResult`) |
| `Application` | Pipeline interfaces and orchestrator |
| `Infrastructure.Ollama` | Ollama API client, summarizer, evaluator, health check |
| `Infrastructure.IO` | File reader, console writer, file writer |
| `Infrastructure.Processing` | Text normalization and transcript format detection |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Ollama](https://ollama.com) running locally
- A supported model pulled and available

### Recommended models

| Model | Size | Command |
|---|---|---|
| `qwen2.5:7b-instruct` | ~4.7 GB | `ollama pull qwen2.5:7b-instruct` |
| `phi4:latest` | ~9.1 GB | `ollama pull phi4` |
| `llama3:latest` | ~4.7 GB | `ollama pull llama3` |
| `mistral` | ~4.1 GB | `ollama pull mistral` |

> For machines with limited VRAM, `qwen2.5:7b-instruct` or `mistral` are lighter alternatives.

## Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/vladimir-goryunov/ai-meeting-summarizer.git
cd ai-meeting-summarizer

# 2. Ensure Ollama is running
ollama serve

# 3. Pull the default model
ollama pull qwen2.5:7b-instruct

# 4. Navigate to the project directory and run
cd src/AiMeetingSummarizer
dotnet run -- --input "..\..\samples\sample-status-meeting.txt"
```

The summary is printed to the console and saved to `summary.md` in the working directory.

## Usage

```
AiMeetingSummarizer --input <path> [--output <path>] [--verbose] [--no-color]
```

| Option | Short | Description |
|---|---|---|
| `--input` | `-i` | Path to the `.txt` or `.json` transcript file **(required)** |
| `--output` | `-o` | Path for the Markdown output file (default: `summary.md`) |
| `--verbose` | `-v` | Enable verbose output |
| `--no-color` | | Disable colored console output |

### Run with `dotnet run` (from `src/AiMeetingSummarizer`)

```bash
# Minimal
dotnet run -- --input "..\..\samples\sample-status-meeting.txt"

# With output file and verbose logging
dotnet run -- --input "..\..\samples\sample-technical-meeting.json" --output report.md --verbose

# Short form
dotnet run -- -i "..\..\samples\sample-technical-meeting.json" -v
```

### Run the compiled binary

First build the project:

```bash
# Release build (recommended)
dotnet build -c Release

# Debug build
dotnet build -c Debug
```

Then run the binary directly:

```bash
# Release
bin\Release\net8.0\AiMeetingSummarizer.exe --input "..\..\samples\sample-technical-meeting.json" --verbose

# Debug
bin\Debug\net8.0\AiMeetingSummarizer.exe --input "..\..\samples\sample-technical-meeting.json" --verbose

# With custom output path
bin\Release\net8.0\AiMeetingSummarizer.exe --input meeting.txt --output reports\2025-01-15.md
```

## Input Format

The application accepts two transcript formats:

**Plain text dialogue:**
```
John: Let's start with the API issue from yesterday.
Anna: I checked the logs — there are a lot of timeout errors.
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

**Console output** — formatted ASCII table with summary and evaluation scores.

**Markdown file** — structured document with a quality evaluation table:

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
    "ModelName": "qwen2.5:7b-instruct",
    "TimeoutSeconds": 180,
    "Temperature": 0.0,
    "Seed": 42
  },
  "Output": {
    "FilePath": "summary.md"
  },
  "Logging": {
    "FilePath": "logs/app-.log",
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

The `--output` CLI argument takes priority over `Output:FilePath` in `appsettings.json`.

## Running Tests

```bash
dotnet test
```

```bash
# With coverage report
dotnet test --collect:"XPlat Code Coverage"
```

Test coverage targets >90% for all new classes. Tests are organized as unit tests only (`*UnitTest`) using xUnit, Moq, and FluentAssertions.

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

## Exit Codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | Invalid arguments |
| 2 | Operation cancelled (Ctrl+C) |
| 3 | Input file not found |
| 4 | Ollama service error during inference |
| 5 | Unexpected error |
| 6 | Ollama not reachable — run `ollama serve` |
| 7 | Model not installed — run `ollama pull <model>` |
| 8 | Invalid configuration — check `appsettings.json` |

## Limitations

- Text input only — audio transcription is not supported
- No speaker diarization for unmarked transcripts
- Context window bounded by the chosen model (typically 4k–8k tokens)
- Long transcripts may be truncated; chunking is not implemented

## Future Directions

- Audio input via [Whisper](https://github.com/openai/whisper) integration
- Speaker diarization for unmarked transcripts
- Long-document chunking and multi-pass summarization
- Web or desktop UI
- Integration with external tools (Jira, Confluence, Slack)

## License

MIT