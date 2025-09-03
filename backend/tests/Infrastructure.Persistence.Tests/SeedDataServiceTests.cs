using Core.Application.Configuration;
using Core.Application.Interfaces;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Infrastructure.Persistence.Tests;

public class SeedDataServiceTests
{
    private static RecipeAppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<RecipeAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RecipeAppDbContext(options);
    }

    private static SeedDataService NewService(RecipeAppDbContext db, bool enable, bool onlyIfEmpty)
    {
        var opts = Options.Create(new SeedDataOptions { EnableSeeding = enable, SeedOnlyIfEmpty = onlyIfEmpty });
        return new SeedDataService(db, new NullLogger<SeedDataService>(), opts, new Infrastructure.Persistence.Services.PasswordService());
    }

    [Fact]
    public async Task Seed_Disabled_DoesNothing()
    {
        using var db = NewDb();
        var svc = NewService(db, enable: false, onlyIfEmpty: true);
        await svc.SeedAsync();
        (await db.Users.CountAsync()).Should().Be(0);
        (await db.Recipes.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Seed_Skips_WhenNotEmpty_AndOnlyIfEmpty()
    {
        using var db = NewDb();
        // Prime DB with a user to make it non-empty
        db.Users.Add(new Core.Domain.Entities.User("U", Core.Domain.ValueObjects.Email.Create("a@b.com").Value, "h"));
        await db.SaveChangesAsync();

        var svc = NewService(db, enable: true, onlyIfEmpty: true);
        await svc.SeedAsync();

        // No recipes were added because DB not empty
        (await db.Recipes.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Seed_EmptyDb_SeedsUserAndRecipes()
    {
        using var db = NewDb();
        var svc = NewService(db, enable: true, onlyIfEmpty: true);
        await svc.SeedAsync();

        (await db.Users.CountAsync()).Should().Be(1);
        (await db.Recipes.CountAsync()).Should().Be(5);

        var user = await db.Users.FirstAsync();
        user.Email.Value.Should().Be("demo@example.com");
        user.EmailVerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task IsDatabaseEmpty_Works()
    {
        using var db = NewDb();
        var svc = NewService(db, enable: true, onlyIfEmpty: true);
        (await svc.IsDatabaseEmptyAsync()).Should().BeTrue();
        db.Users.Add(new Core.Domain.Entities.User("U", Core.Domain.ValueObjects.Email.Create("a@b.com").Value, "h"));
        await db.SaveChangesAsync();
        (await svc.IsDatabaseEmptyAsync()).Should().BeFalse();
    }
}

