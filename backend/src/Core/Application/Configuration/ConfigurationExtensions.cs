using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.Application.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // Add and validate DatabaseOptions
        services.Configure<DatabaseOptions>(options => configuration.GetSection(DatabaseOptions.SectionName).Bind(options));
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DataAnnotationValidateOptions<DatabaseOptions>>();

        // Add and validate JwtOptions
        services.Configure<JwtOptions>(options => configuration.GetSection(JwtOptions.SectionName).Bind(options));
        services.AddSingleton<IValidateOptions<JwtOptions>, DataAnnotationValidateOptions<JwtOptions>>();

        // Add LoggingOptions (no validation needed as it has defaults)
        services.Configure<LoggingOptions>(options => configuration.GetSection(LoggingOptions.SectionName).Bind(options));

        // Add and validate ApiGatewayOptions
        services.Configure<ApiGatewayOptions>(options => configuration.GetSection(ApiGatewayOptions.SectionName).Bind(options));
        services.AddSingleton<IValidateOptions<ApiGatewayOptions>, DataAnnotationValidateOptions<ApiGatewayOptions>>();

        // Add SeedDataOptions with environment variable override
        services.Configure<SeedDataOptions>(options =>
        {
            configuration.GetSection(SeedDataOptions.SectionName).Bind(options);
            
            // Override with environment variable if present
            var enableSeedingEnv = Environment.GetEnvironmentVariable("ENABLE_SEEDING");
            if (!string.IsNullOrEmpty(enableSeedingEnv) && bool.TryParse(enableSeedingEnv, out var enableSeeding))
            {
                options.EnableSeeding = enableSeeding;
            }

            // Allow overriding SeedOnlyIfEmpty via environment variable as well
            var seedOnlyIfEmptyEnv = Environment.GetEnvironmentVariable("SEED_ONLY_IF_EMPTY");
            if (!string.IsNullOrEmpty(seedOnlyIfEmptyEnv) && bool.TryParse(seedOnlyIfEmptyEnv, out var seedOnlyIfEmpty))
            {
                options.SeedOnlyIfEmpty = seedOnlyIfEmpty;
            }
        });

        return services;
    }

    public static T GetValidatedOptions<T>(this IConfiguration configuration, string sectionName) where T : class, new()
    {
        var options = new T();
        configuration.GetSection(sectionName).Bind(options);
        
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(options, validationContext, validationResults, true))
        {
            var errors = string.Join(Environment.NewLine, validationResults.Select(vr => vr.ErrorMessage));
            throw new InvalidOperationException($"Configuration validation failed for section '{sectionName}':{Environment.NewLine}{errors}");
        }
        
        return options;
    }

    public static void ValidateConfigurationOnStartup(this IConfiguration configuration)
    {
        try
        {
            // Validate Database configuration
            configuration.GetValidatedOptions<DatabaseOptions>(DatabaseOptions.SectionName);
            
            // Validate JWT configuration  
            configuration.GetValidatedOptions<JwtOptions>(JwtOptions.SectionName);
            
            // Validate API Gateway configuration
            configuration.GetValidatedOptions<ApiGatewayOptions>(ApiGatewayOptions.SectionName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Configuration validation failed during startup", ex);
        }
    }
}

public class DataAnnotationValidateOptions<T> : IValidateOptions<T> where T : class
{
    public ValidateOptionsResult Validate(string? name, T options)
    {
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();

        if (Validator.TryValidateObject(options, validationContext, validationResults, true))
        {
            return ValidateOptionsResult.Success;
        }

        var errors = validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
        return ValidateOptionsResult.Fail(errors);
    }
}
