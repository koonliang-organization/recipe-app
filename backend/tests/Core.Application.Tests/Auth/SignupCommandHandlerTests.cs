using Core.Application.Commands.Auth;
using Core.Application.Handlers.Auth;
using Core.Application.Interfaces;
using Core.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Core.Application.Tests.Auth;

public class SignupCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldFail_OnInvalidEmail()
    {
        var handler = new SignupCommandHandler(Mock.Of<IUserRepository>(), Mock.Of<IPasswordService>(), Mock.Of<IJwtTokenService>());
        var res = await handler.Handle(new SignupCommand("U", "bad", "password1"), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenEmailExists()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.ExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new SignupCommandHandler(repo.Object, Mock.Of<IPasswordService>(), Mock.Of<IJwtTokenService>());
        var res = await handler.Handle(new SignupCommand("U", "a@b.com", "password1"), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
        res.Error.Should().Contain("exists");
    }

    [Fact]
    public async Task Handle_ShouldFail_OnShortPassword()
    {
        var handler = new SignupCommandHandler(Mock.Of<IUserRepository>(), Mock.Of<IPasswordService>(), Mock.Of<IJwtTokenService>());
        var res = await handler.Handle(new SignupCommand("U", "a@b.com", "short"), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
        res.Error.Should().Contain("Password must be at least 8 characters");
    }

    [Fact]
    public async Task Handle_ShouldCreateUser_AndReturnToken()
    {
        var repo = new Mock<IUserRepository>();
        var pw = new Mock<IPasswordService>();
        var jwt = new Mock<IJwtTokenService>();
        repo.Setup(r => r.ExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.AddAsync(It.IsAny<Core.Domain.Entities.User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        pw.Setup(p => p.HashPassword("password1")).Returns("hash");
        jwt.Setup(j => j.GenerateToken(It.IsAny<Core.Domain.Entities.User>())).Returns("token");

        var handler = new SignupCommandHandler(repo.Object, pw.Object, jwt.Object);
        var res = await handler.Handle(new SignupCommand("U", "a@b.com", "password1"), CancellationToken.None);

        res.IsSuccess.Should().BeTrue();
        res.Value.Token.Should().Be("token");
        res.Value.User.Email.Should().Be("a@b.com");
    }
}

