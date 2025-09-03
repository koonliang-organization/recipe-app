using Core.Domain.Entities;
using Core.Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Persistence.Tests;

public class UserRepositoryTests
{
    private static RecipeAppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<RecipeAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RecipeAppDbContext(options);
    }

    private static User NewUser(string name = "User1", string email = "user1@test.com", string hash = "hash")
        => new(name, Email.Create(email).Value, hash);

    [Fact]
    public async Task Add_And_GetById_And_Exists()
    {
        using var db = NewDb();
        var repo = new UserRepository(db);
        var user = NewUser();

        await repo.AddAsync(user);

        var fetched = await repo.GetByIdAsync(user.Id);
        fetched.Should().NotBeNull();
        fetched!.Email.Value.Should().Be("user1@test.com");
        (await repo.ExistsAsync(Email.Create("user1@test.com").Value)).Should().BeTrue();
        (await repo.ExistsAsync(Email.Create("other@test.com").Value)).Should().BeFalse();
    }

    [Fact]
    public async Task GetByEmail_ShouldBeCaseInsensitive()
    {
        using var db = NewDb();
        var repo = new UserRepository(db);
        var user = NewUser(email: "User2@Test.Com");
        await repo.AddAsync(user);

        var fetched = await repo.GetByEmailAsync(Email.Create("user2@test.com").Value);
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Update_ShouldPersistChanges()
    {
        using var db = NewDb();
        var repo = new UserRepository(db);
        var user = NewUser();
        await repo.AddAsync(user);

        user.UpdateName("New Name");
        await repo.UpdateAsync(user);

        var fetched = await repo.GetByIdAsync(user.Id);
        fetched!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task GetByPasswordResetToken_Works()
    {
        using var db = NewDb();
        var repo = new UserRepository(db);
        var user = NewUser();
        await repo.AddAsync(user);

        user.SetPasswordResetToken("tok", DateTime.UtcNow.AddMinutes(15));
        await repo.UpdateAsync(user);

        var fetched = await repo.GetByPasswordResetTokenAsync("tok");
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(user.Id);
    }
}

