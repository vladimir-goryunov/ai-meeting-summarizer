using AiMeetingSummarizer.Infrastructure.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AiMeetingSummarizer.Tests.Unit;

public sealed class FileInputProviderUnitTest : IDisposable
{
    private readonly FileInputProvider _sut = new(NullLogger<FileInputProvider>.Instance);
    private readonly List<string> _tempFiles = [];

    [Fact]
    public async Task ReadAsync_ReturnsFileContent_WhenFileExists()
    {
        var path = CreateTempFile("John: Hello.\nAnna: Hi there.");

        var result = await _sut.ReadAsync(path);

        result.Should().Be("John: Hello.\nAnna: Hi there.");
    }

    [Fact]
    public async Task ReadAsync_ThrowsFileNotFoundException_WhenFileDoesNotExist()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");

        var act = async () => await _sut.ReadAsync(nonExistentPath);

        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage($"*{nonExistentPath}*");
    }

    [Fact]
    public async Task ReadAsync_ThrowsArgumentException_WhenPathIsEmpty()
    {
        var act = async () => await _sut.ReadAsync(string.Empty);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("path");
    }

    [Fact]
    public async Task ReadAsync_ThrowsArgumentException_WhenPathIsWhitespace()
    {
        var act = async () => await _sut.ReadAsync("   ");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("path");
    }

    [Fact]
    public async Task ReadAsync_ThrowsInvalidOperationException_WhenFileIsEmpty()
    {
        var path = CreateTempFile(string.Empty);

        var act = async () => await _sut.ReadAsync(path);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{path}*");
    }

    [Fact]
    public async Task ReadAsync_ReturnsUtf8Content_Correctly()
    {
        var content = "Участник: Привет команда.";
        var path = CreateTempFile(content);

        var result = await _sut.ReadAsync(path);

        result.Should().Be(content);
    }

    private string CreateTempFile(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); }
            catch { /* best-effort cleanup */ }
        }
    }
}
