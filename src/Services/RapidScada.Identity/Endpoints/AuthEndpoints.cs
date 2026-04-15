using Carter;
using Microsoft.AspNetCore.Mvc;
using RapidScada.Identity.Services;

namespace RapidScada.Identity.Endpoints;

public sealed class AuthEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithOpenApi();

        group.MapPost("/refresh", RefreshToken)
            .WithName("RefreshToken")
            .WithOpenApi();

        group.MapPost("/logout", Logout)
            .RequireAuthorization()
            .WithName("Logout")
            .WithOpenApi();

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithOpenApi();
    }

    private async Task<IResult> Login(
        [FromBody] LoginRequest request,
        IAuthenticationService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(
            request.UserName,
            request.Password,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Message });
    }

    private async Task<IResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        IAuthenticationService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error.Message });
    }

    private async Task<IResult> Logout(
        HttpContext context,
        IAuthenticationService authService,
        CancellationToken cancellationToken)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await authService.LogoutAsync(userId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { message = "Logged out successfully" })
            : Results.BadRequest(new { error = result.Error.Message });
    }

    private async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        IAuthenticationService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(
            request.UserName,
            request.Email,
            request.Password,
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/users/{result.Value.Id.Value}", new
            {
                id = result.Value.Id.Value,
                userName = result.Value.UserName,
                email = result.Value.Email
            })
            : Results.BadRequest(new { error = result.Error.Message });
    }
}

public sealed record LoginRequest(string UserName, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record RegisterRequest(string UserName, string Email, string Password);
