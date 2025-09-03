using Core.Application.DTOs;
using Core.Application.Handlers.Recipe;
using Core.Application.Interfaces;
using Core.Application.Queries.Recipe;
using Core.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Core.Application.Tests;

public class GetRecipesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoRecipesInDb()
    {
        var repo = new Mock<IRecipeRepository>();
        var logger = Mock.Of<ILogger<GetRecipesQueryHandler>>();
        repo.Setup(r => r.GetTotalRecipeCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var handler = new GetRecipesQueryHandler(repo.Object, Mock.Of<IUserRepository>(), logger);

        var res = await handler.Handle(new GetRecipesQuery(null, null, 1, 20, Guid.NewGuid()), CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
        res.Value.Items.Should().BeEmpty();
        res.Value.Pagination.Total.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldClampPagination_AndMapFavorites()
    {
        var repo = new Mock<IRecipeRepository>(MockBehavior.Strict);
        var logger = Mock.Of<ILogger<GetRecipesQueryHandler>>();
        var userRepo = Mock.Of<IUserRepository>();
        var userId = Guid.NewGuid();

        var r1 = new Recipe("A", "", "C", userId) { };
        var r2 = new Recipe("B", "", "C", userId) { };
        var paged = new PagedResult<Recipe>
        {
            Items = new List<Recipe> { r1, r2 },
            Pagination = new PaginationInfo { Page = 1, Limit = 100, Total = 2, TotalPages = 1 }
        };

        repo.Setup(r => r.GetTotalRecipeCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        repo.Setup(r => r.GetRecipesAsync(null, null, It.Is<int>(p => p == 1), It.Is<int>(l => l == 100), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged)
            .Verifiable();
        repo.Setup(r => r.IsRecipeFavoriteAsync(It.Is<Guid>(id => id == r1.Id), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.IsRecipeFavoriteAsync(It.Is<Guid>(id => id == r2.Id), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new GetRecipesQueryHandler(repo.Object, userRepo, logger);
        var res = await handler.Handle(new GetRecipesQuery(null, null, 0, 1000, userId), CancellationToken.None);

        repo.Verify();
        res.IsSuccess.Should().BeTrue();
        res.Value.Items.Should().HaveCount(2);
        res.Value.Items.First(x => x.Id == r1.Id).IsFavorite.Should().BeTrue();
        res.Value.Items.First(x => x.Id == r2.Id).IsFavorite.Should().BeFalse();
    }
}

