using Core.Application.DTOs;
using Core.Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Infrastructure.Persistence.Tests;

public class RecipeRepositoryTests
{
    private static RecipeAppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<RecipeAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;
        return new RecipeAppDbContext(options);
    }

    private static Recipe NewRecipe(string title, string cat, Guid userId)
    {
        var r = new Recipe(title, $"{title} desc", cat, userId);
        r.AddIngredient("I1", "1", "u");
        r.AddStep(1, "S1");
        return r;
    }

    [Fact]
    public async Task Add_And_GetById_IncludesChildren()
    {
        using var db = NewDb();
        var repo = new RecipeRepository(db, new NullLogger<RecipeRepository>());
        var userId = Guid.NewGuid();
        var r = NewRecipe("T1", "C1", userId);

        await repo.AddAsync(r);
        var fetched = await repo.GetByIdAsync(r.Id);

        fetched.Should().NotBeNull();
        fetched!.Ingredients.Should().HaveCount(1);
        fetched.Steps.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRecipes_Filters_Category_Search_User_Paginates()
    {
        using var db = NewDb();
        var repo = new RecipeRepository(db, new NullLogger<RecipeRepository>());
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();

        await repo.AddAsync(NewRecipe("Apple Pie", "Dessert", u1));
        await repo.AddAsync(NewRecipe("Banana Bread", "Dessert", u1));
        await repo.AddAsync(NewRecipe("Caesar Salad", "Salad", u1));
        await repo.AddAsync(NewRecipe("Burger", "Dinner", u2));

        var page1 = await repo.GetRecipesAsync("Dessert", "Banana", 1, 10, u1);
        page1.Pagination.Total.Should().Be(1);
        page1.Items.Single().Title.Should().Be("Banana Bread");

        var userOnly = await repo.GetRecipesAsync(null, null, 1, 10, u2);
        userOnly.Items.Should().OnlyContain(r => r.UserId == u2);

        var paged = await repo.GetRecipesAsync(null, null, 1, 1, u1);
        paged.Items.Should().HaveCount(1);
        paged.Pagination.Total.Should().Be(3); // u1 has 3
        paged.Pagination.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Update_ReplacesChildren()
    {
        using var db = NewDb();
        var repo = new RecipeRepository(db, new NullLogger<RecipeRepository>());
        var userId = Guid.NewGuid();
        var r = NewRecipe("T1", "C1", userId);
        await repo.AddAsync(r);

        var existing = await repo.GetByIdAsync(r.Id);
        existing.Should().NotBeNull();
        existing!.UpdateBasicInfo("T2", "D2", "C2", null);
        var newIngs = new List<Ingredient>
        {
            new("Flour", "1", "kg", existing.Id)
        };
        existing.UpdateIngredients(newIngs);
        var newSteps = new List<Step>
        {
            new(1, "Mix", existing.Id),
            new(2, "Bake", existing.Id)
        };
        existing.UpdateSteps(newSteps);

        await repo.UpdateAsync(existing);

        var updated = await repo.GetByIdAsync(r.Id);
        updated!.Title.Should().Be("T2");
        updated.Ingredients.Should().HaveCount(1);
        updated.Steps.Should().HaveCount(2);
    }

    [Fact]
    public async Task Exists_Count_Delete_Favorites()
    {
        using var db = NewDb();
        var repo = new RecipeRepository(db, new NullLogger<RecipeRepository>());
        var user = Guid.NewGuid();
        var r = NewRecipe("T1", "C1", user);
        await repo.AddAsync(r);

        (await repo.RecipeExistsAsync(r.Id)).Should().BeTrue();
        (await repo.GetTotalRecipeCountAsync()).Should().Be(1);

        // favorites
        (await repo.IsRecipeFavoriteAsync(r.Id, user)).Should().BeFalse();
        await repo.AddFavoriteAsync(r.Id, user);
        (await repo.IsRecipeFavoriteAsync(r.Id, user)).Should().BeTrue();
        await repo.RemoveFavoriteAsync(r.Id, user);
        (await repo.IsRecipeFavoriteAsync(r.Id, user)).Should().BeFalse();

        // delete
        await repo.DeleteAsync(r);
        (await repo.RecipeExistsAsync(r.Id)).Should().BeFalse();
    }
}

