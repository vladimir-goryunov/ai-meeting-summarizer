// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using AiMeetingSummarizer.Presentation;
using FluentAssertions;
using Xunit;

namespace AiMeetingSummarizer.Tests.Unit;

public sealed class ExitCodeUnitTest
{
    [Theory]
    [InlineData(ExitCode.Success, 0)]
    [InlineData(ExitCode.InvalidArguments, 1)]
    [InlineData(ExitCode.OperationCancelled, 2)]
    [InlineData(ExitCode.InputFileNotFound, 3)]
    [InlineData(ExitCode.OllamaServiceError, 4)]
    [InlineData(ExitCode.UnexpectedError, 5)]
    [InlineData(ExitCode.OllamaNotReachable, 6)]
    [InlineData(ExitCode.OllamaModelNotFound, 7)]
    [InlineData(ExitCode.InvalidConfiguration, 8)]
    public void ExitCode_HasExpectedIntegerValue(ExitCode code, int expectedValue)
    {
        ((int)code).Should().Be(expectedValue);
    }
}