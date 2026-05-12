// Copyright (c) 2026 Vladimir Goryunov
// SPDX-License-Identifier: MIT

using AiMeetingSummarizer.Domain;
using FluentAssertions;
using Xunit;

namespace AiMeetingSummarizer.Tests.Unit;

public sealed class EvaluationResultUnitTest
{
    [Theory]
    [InlineData(2, 2, 2, 2, 2, "Excellent")]
    [InlineData(2, 2, 2, 1, 1, "Good")]      // 8
    [InlineData(1, 1, 1, 1, 2, "Acceptable")] // 6
    [InlineData(0, 1, 1, 0, 1, "Needs Improvement")] // 3
    public void Grade_ReturnsCorrectLabel_BasedOnTotalScore(
        int c, int a, int s, int ai, int cl, string expectedGrade)
    {
        var result = BuildResult(c, a, s, ai, cl);

        result.Grade.Should().Be(expectedGrade);
    }

    [Fact]
    public void TotalScore_IsTheSumOfAllCriterionScores()
    {
        var result = BuildResult(2, 1, 2, 0, 2);

        result.TotalScore.Should().Be(7);
    }

    [Fact]
    public void MaxTotalScore_IsAlwaysTen()
    {
        var result = BuildResult(0, 0, 0, 0, 0);

        result.MaxTotalScore.Should().Be(10);
    }

    private static EvaluationResult BuildResult(int c, int a, int s, int ai, int cl) =>
        new()
        {
            Completeness = new EvaluationScore(c, 2, "comment"),
            Accuracy = new EvaluationScore(a, 2, "comment"),
            StructureCompliance = new EvaluationScore(s, 2, "comment"),
            ActionItemExtraction = new EvaluationScore(ai, 2, "comment"),
            Clarity = new EvaluationScore(cl, 2, "comment"),
            OverallComment = "overall",
            Improvements = []
        };
}
