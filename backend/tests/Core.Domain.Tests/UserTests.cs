using Core.Domain.Entities;
using Core.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Core.Domain.Tests;

public class UserTests
{
    [Fact]
    public void UpdateName_ShouldChangeName_AndTimestamp()
    {
        var user = new User("Old", Email.Create("a@b.com").Value, "hash");
        user.UpdatedAt.Should().BeNull();
        user.UpdateName("New");
        user.Name.Should().Be("New");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void PasswordResetToken_Flow_ShouldWork()
    {
        var user = new User("U", Email.Create("a@b.com").Value, "hash");
        user.SetPasswordResetToken("tok", DateTime.UtcNow.AddMinutes(5));
        user.IsPasswordResetTokenValid("tok").Should().BeTrue();

        user.UpdatePassword("newhash");
        user.IsPasswordResetTokenValid("tok").Should().BeFalse();
        user.PasswordResetToken.Should().BeNull();
    }

    [Fact]
    public void VerifyEmail_ShouldSetTimestamp()
    {
        var user = new User("U", Email.Create("a@b.com").Value, "hash");
        user.EmailVerifiedAt.Should().BeNull();
        user.VerifyEmail();
        user.EmailVerifiedAt.Should().NotBeNull();
    }
}

