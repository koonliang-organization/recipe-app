using Amazon.Lambda.AspNetCoreServer;
using BuildingBlocks.Observability;
using Infrastructure.Persistence;
using Core.Application.Configuration;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;

// Load .env file for local development
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

// Validate configuration on startup
try
{
    builder.Configuration.ValidateConfigurationOnStartup();
}
catch (Exception ex)
{
    Console.WriteLine($"Configuration validation failed: {ex.Message}");
    throw;
}

builder.Host.UseSerilogLogging();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add configuration options with validation
builder.Services.AddConfigurationOptions(builder.Configuration);

// Add database
builder.Services.AddDatabase(builder.Configuration);

// Add infrastructure services
builder.Services.AddInfrastructureServices();

// Add MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Core.Application.Commands.Auth.SignupCommand).Assembly);
});

// Add JWT Authentication
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<Core.Application.Configuration.JwtOptions>();
if (jwtOptions != null)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtOptions.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
}

builder.Services.AddAuthorization();

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var app = builder.Build();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RecipeAppDbContext>();
    context.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    Console.WriteLine("Dev mode swagger");
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Use permissive CORS in development for easier frontend integration
    app.UseCors("AllowAll");
}
else
{
    // Use more restrictive CORS in production
    app.UseCors("AllowFrontend");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();