using Core.Application.Commands.Auth;
using Core.Application.DTOs;
using Core.Application.Handlers.Auth;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Core.Application.Tests.Auth;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldFail_OnInvalidEmailFormat()
    {
        var handler = new LoginCommandHandler(Mock.Of<IUserRepository>(), Mock.Of<IPasswordService>(), Mock.Of<IJwtTokenService>());
        var res = await handler.Handle(new LoginCommand("bad", "pw"), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenUserNotFound()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var handler = new LoginCommandHandler(userRepo.Object, Mock.Of<IPasswordService>(), Mock.Of<IJwtTokenService>());

        var res = await handler.Handle(new LoginCommand("a@b.com", "pw"), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenPasswordInvalid()
    {
        var user = new User("U", Email.Create("a@b.com").Value, "hash");
        var userRepo = new Mock<IUserRepository>();
        var pw = new Mock<IPasswordService>();
        userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        pw.Setup(p => p.VerifyPassword("pw", user.PasswordHash)).Returns(false);

        var handler = new LoginCommandHandler(userRepo.Object, pw.Object, Mock.Of<IJwtTokenService>());
        var res = await handler.Handle(new LoginCommand("a@b.com", "pw"), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnToken_OnSuccess()
    {
        var user = new User("U", Email.Create("a@b.com").Value, "hash");
        var userRepo = new Mock<IUserRepository>();
        var pw = new Mock<IPasswordService>();
        var jwt = new Mock<IJwtTokenService>();
        userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        pw.Setup(p => p.VerifyPassword("pw", user.PasswordHash)).Returns(true);
        jwt.Setup(j => j.GenerateToken(user)).Returns("token");

        var handler = new LoginCommandHandler(userRepo.Object, pw.Object, jwt.Object);
        var res = await handler.Handle(new LoginCommand("a@b.com", "pw"), CancellationToken.None);

        res.IsSuccess.Should().BeTrue();
        res.Value.Token.Should().Be("token");
        res.Value.User.Email.Should().Be("a@b.com");
    }
}

