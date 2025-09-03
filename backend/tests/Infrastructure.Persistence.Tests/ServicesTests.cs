using Core.Application.Configuration;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Infrastructure.Persistence.Tests;

public class ServicesTests
{
    [Fact]
    public void PasswordService_HashAndVerify()
    {
        IPasswordService svc = new PasswordService();
        var hash = svc.HashPassword("secret");
        hash.Should().NotBeNullOrWhiteSpace();
        svc.VerifyPassword("secret", hash).Should().BeTrue();
        svc.VerifyPassword("wrong", hash).Should().BeFalse();
    }

    [Fact]
    public void JwtTokenService_Generate_And_Validate()
    {
        var opts = Options.Create(new JwtOptions
        {
            SecretKey = new string('x', 64),
            Issuer = "test-issuer",
            Audience = "test-aud",
            ExpirationMinutes = 15
        });

        var svc = new JwtTokenService(opts);
        var user = new User("U", Email.Create("a@b.com").Value, "hash");
        var token = svc.GenerateToken(user);
        token.Should().NotBeNullOrWhiteSpace();

        var principal = svc.ValidateToken(token);
        principal.Should().NotBeNull();
    }

    [Fact]
    public async Task EmailService_DoesNotThrow()
    {
        var logger = new NullLogger<EmailService>();
        var svc = new EmailService(logger);
        await svc.SendPasswordResetEmailAsync("a@b.com", "token");
    }
}

