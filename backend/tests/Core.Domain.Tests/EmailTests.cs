using Core.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Core.Domain.Tests;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com", "test@example.com")]
    [InlineData("  USER@Example.COM ", "user@example.com")]
    public void Create_ShouldSucceed_ForValidEmails(string input, string expected)
    {
        var result = Email.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
        result.Error.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    [InlineData("user@no-tld")]
    public void Create_ShouldFail_ForInvalidEmails(string? input)
    {
        var result = Email.Create(input ?? string.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_ShouldFail_WhenTooLong()
    {
        var local = new string('a', 200);
        var domain = new string('b', 60);
        var input = $"{local}@{domain}.com"; // >254

        var result = Email.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("too long");
    }

    [Fact]
    public void Equality_ShouldWork_ForSameValue()
    {
        var e1 = Email.Create("same@example.com").Value;
        var e2 = Email.Create("SAME@example.com").Value;

        e1.Should().Be(e2);
        e1.GetHashCode().Should().Be(e2.GetHashCode());
    }
}

