// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using AiMeetingSummarizer.Infrastructure.Ollama;
using FluentAssertions;
using Xunit;

namespace AiMeetingSummarizer.Tests.Unit;

public sealed class PromptLoaderUnitTest : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public PromptLoaderUnitTest()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "Infrastructure", "Templates"));
    }

    [Fact]
    public void Load_ReturnsFileContent_WhenFileExists()
    {
        // Deleting `File.ReadAllText` call makes this test fail.
        WritePromptFile("TestPrompt.txt", "You are a test assistant.");
        var sut = new PromptLoader(_tempDir);

        var result = sut.Load("TestPrompt.txt");

        result.Should().Be("You are a test assistant.");
    }

    [Fact]
    public void Load_ReturnsMultilineContent_Correctly()
    {
        // Deleting `File.ReadAllText` call makes this test fail.
        const string content = "Line one.\nLine two.\nLine three.";
        WritePromptFile("MultiLine.txt", content);
        var sut = new PromptLoader(_tempDir);

        var result = sut.Load("MultiLine.txt");

        result.Should().Be(content);
    }

    [Fact]
    public void Load_ThrowsFileNotFoundException_WhenFileDoesNotExist()
    {
        // Deleting the `if (!File.Exists)` guard makes this test fail.
        var sut = new PromptLoader(_tempDir);

        var act = () => sut.Load("NonExistent.txt");

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*NonExistent.txt*");
    }

    private void WritePromptFile(string fileName, string content)
    {
        var path = Path.Combine(_tempDir, "Infrastructure", "Templates", fileName);
        File.WriteAllText(path, content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}