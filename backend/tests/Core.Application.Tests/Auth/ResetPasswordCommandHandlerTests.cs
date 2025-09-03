using Core.Application.Commands.Auth;
using Core.Application.Handlers.Auth;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Core.Application.Tests.Auth;

public class ResetPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldFail_OnShortPassword()
    {
        var handler = new ResetPasswordCommandHandler(Mock.Of<IUserRepository>(), Mock.Of<IPasswordService>());
        var res = await handler.Handle(new ResetPasswordCommand("token", "short"), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenUserNotFound()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByPasswordResetTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var handler = new ResetPasswordCommandHandler(repo.Object, Mock.Of<IPasswordService>());
        var res = await handler.Handle(new ResetPasswordCommand("tok", "password1"), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTokenInvalid()
    {
        var user = new User("U", Email.Create("a@b.com").Value, "hash");
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByPasswordResetTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        // user.IsPasswordResetTokenValid will be false by default
        var handler = new ResetPasswordCommandHandler(repo.Object, Mock.Of<IPasswordService>());
        var res = await handler.Handle(new ResetPasswordCommand("tok", "password1"), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldUpdatePassword_OnSuccess()
    {
        var user = new User("U", Email.Create("a@b.com").Value, "hash");
        user.SetPasswordResetToken("tok", DateTime.UtcNow.AddMinutes(10));

        var repo = new Mock<IUserRepository>();
        var pw = new Mock<IPasswordService>();
        repo.Setup(r => r.GetByPasswordResetTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        pw.Setup(p => p.HashPassword("password1")).Returns("newhash");
        repo.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();

        var handler = new ResetPasswordCommandHandler(repo.Object, pw.Object);
        var res = await handler.Handle(new ResetPasswordCommand("tok", "password1"), CancellationToken.None);

        res.IsSuccess.Should().BeTrue();
        repo.Verify();
    }
}

