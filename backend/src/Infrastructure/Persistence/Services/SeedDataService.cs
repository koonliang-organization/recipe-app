using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Core.Application.Configuration;

namespace Infrastructure.Persistence.Services;

public class SeedDataService : ISeedDataService
{
    private readonly RecipeAppDbContext _context;
    private readonly ILogger<SeedDataService> _logger;
    private readonly SeedDataOptions _options;
    private readonly IPasswordService _passwordService;

    public SeedDataService(
        RecipeAppDbContext context,
        ILogger<SeedDataService> logger,
        IOptions<SeedDataOptions> options,
        IPasswordService passwordService)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;
        _passwordService = passwordService;
    }

    public async Task SeedAsync()
    {
        try
        {
            if (!_options.EnableSeeding)
            {
                _logger.LogInformation("Database seeding is disabled");
                return;
            }

            if (_options.SeedOnlyIfEmpty && !await IsDatabaseEmptyAsync())
            {
                _logger.LogInformation("Database is not empty, skipping seeding");
                return;
            }

            _logger.LogInformation("Starting database seeding...");

            var demoUser = await SeedUsersAsync();
            if (demoUser != null)
            {
                await SeedRecipesAsync(demoUser);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Demo user could not be created or found, skipping recipe seeding");
            }

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding database");
            throw;
        }
    }

    public async Task<bool> IsDatabaseEmptyAsync()
    {
        var hasUsers = await _context.Users.AnyAsync();
        var hasRecipes = await _context.Recipes.AnyAsync();
        return !hasUsers && !hasRecipes;
    }

    private async Task<User?> SeedUsersAsync()
    {
        // Check if demo user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == "demo@example.com");
        
        if (existingUser != null)
        {
            _logger.LogDebug("Demo user already exists, returning existing user");
            return existingUser;
        }

        var passwordHash = _passwordService.HashPassword("pass1234");
        var emailResult = Email.Create("demo@example.com");
        if (emailResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to create demo user email: {emailResult.Error}");
        }
        
        var demoUser = new User(
            "Demo User",
            emailResult.Value,
            passwordHash
        );
        demoUser.VerifyEmail();

        _context.Users.Add(demoUser);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Demo user seeded successfully");
        
        return demoUser;
    }

    private async Task SeedRecipesAsync(User demoUser)
    {
        if (await _context.Recipes.AnyAsync())
        {
            _logger.LogDebug("Recipes already exist, skipping recipe seeding");
            return;
        }

        var recipes = GetSeedRecipes(demoUser.Id);

        _context.Recipes.AddRange(recipes);
        _logger.LogInformation("Seeded {RecipeCount} recipes", recipes.Count);
    }

    private List<Recipe> GetSeedRecipes(Guid userId)
    {
        var recipes = new List<Recipe>();

        // Recipe 1: Classic Spaghetti Carbonara
        var carbonara = new Recipe(
            "Classic Spaghetti Carbonara",
            "A traditional Italian pasta dish with eggs, cheese, and pancetta",
            "Italian",
            userId,
            "https://therecipecritic.com/wp-content/uploads/2018/04/pasta-carbonara-15.jpg"
        );

        carbonara.AddIngredient("Spaghetti", "400", "g");
        carbonara.AddIngredient("Pancetta", "150", "g");
        carbonara.AddIngredient("Eggs", "3", "large");
        carbonara.AddIngredient("Parmesan cheese", "100", "g");
        carbonara.AddIngredient("Black pepper", "1", "tsp");

        carbonara.AddStep(1, "Cook spaghetti in salted boiling water until al dente");
        carbonara.AddStep(2, "Cook pancetta in a large pan until crispy");
        carbonara.AddStep(3, "Whisk eggs with grated Parmesan and black pepper");
        carbonara.AddStep(4, "Drain pasta and add to pancetta pan");
        carbonara.AddStep(5, "Remove from heat and quickly stir in egg mixture");

        recipes.Add(carbonara);

        // Recipe 2: Chicken Tikka Masala
        var tikkamasala = new Recipe(
            "Chicken Tikka Masala",
            "Creamy and flavorful Indian curry with tender chicken pieces",
            "Indian",
            userId,
            "https://images.unsplash.com/photo-1565557623262-b51c2513a641?w=400"
        );

        tikkamasala.AddIngredient("Chicken breast", "500", "g");
        tikkamasala.AddIngredient("Yogurt", "200", "ml");
        tikkamasala.AddIngredient("Tomato sauce", "400", "ml");
        tikkamasala.AddIngredient("Heavy cream", "100", "ml");
        tikkamasala.AddIngredient("Garam masala", "2", "tbsp");
        tikkamasala.AddIngredient("Garlic", "4", "cloves");
        tikkamasala.AddIngredient("Ginger", "1", "inch");

        tikkamasala.AddStep(1, "Marinate chicken in yogurt and spices for 30 minutes");
        tikkamasala.AddStep(2, "Cook marinated chicken until browned");
        tikkamasala.AddStep(3, "Sauté garlic and ginger until fragrant");
        tikkamasala.AddStep(4, "Add tomato sauce and garam masala, simmer");
        tikkamasala.AddStep(5, "Add chicken back to sauce and stir in cream");

        recipes.Add(tikkamasala);

        // Recipe 3: Chocolate Chip Cookies
        var cookies = new Recipe(
            "Chocolate Chip Cookies",
            "Classic homemade cookies with chocolate chips",
            "Dessert",
            userId,
            "https://images.unsplash.com/photo-1499636136210-6f4ee915583e?w=400"
        );

        cookies.AddIngredient("All-purpose flour", "2", "cups");
        cookies.AddIngredient("Butter", "1", "cup");
        cookies.AddIngredient("Brown sugar", "3/4", "cup");
        cookies.AddIngredient("White sugar", "1/4", "cup");
        cookies.AddIngredient("Eggs", "2", "large");
        cookies.AddIngredient("Chocolate chips", "2", "cups");
        cookies.AddIngredient("Vanilla extract", "1", "tsp");

        cookies.AddStep(1, "Preheat oven to 375°F (190°C)");
        cookies.AddStep(2, "Cream butter and sugars until fluffy");
        cookies.AddStep(3, "Beat in eggs and vanilla extract");
        cookies.AddStep(4, "Gradually mix in flour until combined");
        cookies.AddStep(5, "Fold in chocolate chips");
        cookies.AddStep(6, "Drop spoonfuls on baking sheet and bake 9-11 minutes");

        recipes.Add(cookies);

        // Recipe 4: Caesar Salad
        var caesar = new Recipe(
            "Caesar Salad",
            "Fresh romaine lettuce with classic Caesar dressing",
            "Salad",
            userId,
            "https://images.unsplash.com/photo-1546793665-c74683f339c1?w=400"
        );

        caesar.AddIngredient("Romaine lettuce", "2", "heads");
        caesar.AddIngredient("Parmesan cheese", "1/2", "cup");
        caesar.AddIngredient("Croutons", "1", "cup");
        caesar.AddIngredient("Mayonnaise", "1/2", "cup");
        caesar.AddIngredient("Lemon juice", "2", "tbsp");
        caesar.AddIngredient("Garlic", "2", "cloves");

        caesar.AddStep(1, "Wash and chop romaine lettuce");
        caesar.AddStep(2, "Make dressing with mayo, lemon, and minced garlic");
        caesar.AddStep(3, "Toss lettuce with dressing");
        caesar.AddStep(4, "Top with croutons and grated Parmesan");

        recipes.Add(caesar);

        // Recipe 5: Beef Tacos
        var tacos = new Recipe(
            "Beef Tacos",
            "Spicy ground beef tacos with fresh toppings",
            "Mexican",
            userId,
            "https://bing.com/th?id=OSK.142b082b5c3ce860574bd1f2bbf98695"
        );

        tacos.AddIngredient("Ground beef", "500", "g");
        tacos.AddIngredient("Taco shells", "8", "pieces");
        tacos.AddIngredient("Lettuce", "1", "head");
        tacos.AddIngredient("Tomatoes", "2", "medium");
        tacos.AddIngredient("Cheddar cheese", "200", "g");
        tacos.AddIngredient("Taco seasoning", "1", "packet");

        tacos.AddStep(1, "Brown ground beef in a large skillet");
        tacos.AddStep(2, "Add taco seasoning and water, simmer");
        tacos.AddStep(3, "Warm taco shells in oven");
        tacos.AddStep(4, "Prepare toppings: shred lettuce, dice tomatoes, grate cheese");
        tacos.AddStep(5, "Assemble tacos with beef and desired toppings");

        recipes.Add(tacos);

        return recipes;
    }
}