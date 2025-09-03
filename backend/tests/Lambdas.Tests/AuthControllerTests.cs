using Core.Application.Commands.Auth;
using Core.Application.DTOs;
using Core.Application.Queries.User;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using User.Controllers;
using Xunit;

namespace Lambdas.Tests;

public class AuthControllerTests
{
    private static AuthController NewController(Mock<IMediator> mediator, Guid? userId = null)
    {
        var ctrl = new AuthController(mediator.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (userId.HasValue)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            }, "TestAuth");
            ctrl.ControllerContext.HttpContext!.User = new ClaimsPrincipal(identity);
        }

        return ctrl;
    }

    [Fact]
    public async Task Signup_ReturnsOk_OnSuccess()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<SignupCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Common.Result.Success(new AuthenticationResult { Token = "t", User = new UserDto() }));
        var ctrl = NewController(mediator);
        var res = await ctrl.Signup(new("u","a@b.com","password1")) as OkObjectResult;
        res.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_OnFailure()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Common.Result.Failure<AuthenticationResult>("bad"));
        var ctrl = NewController(mediator);
        var res = await ctrl.Login(new("a@b.com","pw"));
        res.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ForgotPassword_AlwaysOk_OnSuccessPath()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<ForgotPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Common.Result.Success());
        var ctrl = NewController(mediator);
        var res = await ctrl.ForgotPassword(new("a@b.com")) as OkObjectResult;
        res.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetPassword_ReturnsOk_OnSuccess()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Common.Result.Success());
        var ctrl = NewController(mediator);
        var res = await ctrl.ResetPassword(new("tok","password1")) as OkObjectResult;
        res.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProfile_ReturnsUnauthorized_WithoutUser()
    {
        var ctrl = NewController(new Mock<IMediator>());
        var res = await ctrl.GetProfile();
        res.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetProfile_ReturnsOk_WithUser()
    {
        var mediator = new Mock<IMediator>();
        var uid = Guid.NewGuid();
        mediator.Setup(m => m.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildingBlocks.Common.Result.Success(new UserDto { Id = uid, Email = "a@b.com", FullName = "U" }));
        var ctrl = NewController(mediator, uid);
        var res = await ctrl.GetProfile() as OkObjectResult;
        res.Should().NotBeNull();
    }
}

