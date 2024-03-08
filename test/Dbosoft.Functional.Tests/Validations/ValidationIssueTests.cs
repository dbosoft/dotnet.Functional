using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.Functional.Validations;

namespace Dbosoft.Functional.Tests.Validations;

public class ValidationIssueTests
{
    [Fact]
    public void ToError_WitMember_ReturnsCorrectError()
    {
        var issue = new ValidationIssue("MyMember", "Some message");
        var error = issue.ToError();

        error.Message.Should().Be("MyMember: Some message");
        error.Exception.Should().BeNone();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ToError_WithoutMember_ReturnsCorrectError(string? member)
    {
        var issue = new ValidationIssue(member, "Some message");
        var error = issue.ToError();

        error.Message.Should().Be("Some message");
        error.Exception.Should().BeNone();
    }

    [Fact]
    public void ToString_WitMember_ReturnsCorrectError()
    {
        var issue = new ValidationIssue("MyMember", "Some message");
        issue.ToString().Should().Be("MyMember: Some message");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ToString_WithoutMember_ReturnsCorrectError(string? member)
    {
        var issue = new ValidationIssue(member, "Some message");
        issue.ToString().Should().Be("Some message");
    }
}
