using Core.Application.Handlers.User;
using Core.Application.Interfaces;
using Core.Application.Queries.User;
using Core.Domain.Entities;
using Core.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Core.Application.Tests.UserTests;

public class GetUserProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldFail_WhenUserNotFound()
    {
        var handler = new GetUserProfileQueryHandler(Mock.Of<IUserRepository>());
        var res = await handler.Handle(new GetUserProfileQuery(Guid.NewGuid()), CancellationToken.None);
        res.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnUserDto()
    {
        var user = new Core.Domain.Entities.User("U", Email.Create("a@b.com").Value, "h");
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var handler = new GetUserProfileQueryHandler(repo.Object);

        var res = await handler.Handle(new GetUserProfileQuery(user.Id), CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
        res.Value.Email.Should().Be("a@b.com");
        res.Value.FullName.Should().Be("U");
    }
}

