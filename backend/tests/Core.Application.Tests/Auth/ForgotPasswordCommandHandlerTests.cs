using Core.Application.Commands.Auth;
using Core.Application.Handlers.Auth;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Core.Application.Tests.Auth;

public class ForgotPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSucceed_ForInvalidEmailFormat()
    {
        var handler = new ForgotPasswordCommandHandler(Mock.Of<IUserRepository>(), Mock.Of<IEmailService>());
        var res = await handler.Handle(new ForgotPasswordCommand("bad"), CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenUserNotFound()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var handler = new ForgotPasswordCommandHandler(repo.Object, Mock.Of<IEmailService>());
        var res = await handler.Handle(new ForgotPasswordCommand("a@b.com"), CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldSetToken_AndSendEmail()
    {
        var user = new User("U", Email.Create("a@b.com").Value, "hash");
        var repo = new Mock<IUserRepository>();
        var email = new Mock<IEmailService>();
        repo.Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        repo.Setup(r => r.UpdateAsync(user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        email.Setup(e => e.SendPasswordResetEmailAsync(user.Email.Value, It.IsAny<string>())).Returns(Task.CompletedTask).Verifiable();

        var handler = new ForgotPasswordCommandHandler(repo.Object, email.Object);
        var res = await handler.Handle(new ForgotPasswordCommand("a@b.com"), CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
        repo.Verify();
        email.Verify();
    }
}

