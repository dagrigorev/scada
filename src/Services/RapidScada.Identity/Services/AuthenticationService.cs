using RapidScada.Domain.Common;
using RapidScada.Identity.Domain;
using RapidScada.Identity.Repositories;

namespace RapidScada.Identity.Services;

public interface IAuthenticationService
{
    Task<Result<AuthenticationResult>> LoginAsync(string userName, string password, CancellationToken cancellationToken = default);
    Task<Result<AuthenticationResult>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(int userId, CancellationToken cancellationToken = default);
    Task<Result<User>> RegisterAsync(string userName, string email, string password, CancellationToken cancellationToken = default);
}

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<AuthenticationResult>> LoginAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUserNameAsync(userName, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Login failed: user {UserName} not found", userName);
            return Result.Failure<AuthenticationResult>(Error.NotFound("User", userName));
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: user {UserName} is deactivated", userName);
            return Result.Failure<AuthenticationResult>(Error.Validation("User account is deactivated"));
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: invalid password for user {UserName}", userName);
            return Result.Failure<AuthenticationResult>(Error.Validation("Invalid credentials"));
        }

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Update user
        user.UpdateLastLogin();
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("User {UserName} logged in successfully", userName);

        return Result.Success(new AuthenticationResult(
            accessToken,
            refreshToken,
            user.Id.Value,
            user.UserName,
            user.Email,
            user.Roles.ToList()));
    }

    public async Task<Result<AuthenticationResult>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Refresh token not found");
            return Result.Failure<AuthenticationResult>(Error.Validation("Invalid refresh token"));
        }

        if (user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token expired for user {UserId}", user.Id.Value);
            return Result.Failure<AuthenticationResult>(Error.Validation("Refresh token expired"));
        }

        // Generate new tokens
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Tokens refreshed for user {UserId}", user.Id.Value);

        return Result.Success(new AuthenticationResult(
            newAccessToken,
            newRefreshToken,
            user.Id.Value,
            user.UserName,
            user.Email,
            user.Roles.ToList()));
    }

    public async Task<Result> LogoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(UserId.Create(userId), cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("User", userId));
        }

        user.ClearRefreshToken();
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("User {UserId} logged out", userId);

        return Result.Success();
    }

    public async Task<Result<User>> RegisterAsync(
        string userName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var existingUser = await _userRepository.GetByUserNameAsync(userName, cancellationToken);
        if (existingUser is not null)
        {
            return Result.Failure<User>(Error.Validation("Username already exists"));
        }

        var existingEmail = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existingEmail is not null)
        {
            return Result.Failure<User>(Error.Validation("Email already registered"));
        }

        // Create user
        var passwordHash = _passwordHasher.HashPassword(password);
        var userResult = User.Create(UserId.New(), userName, email, passwordHash);

        if (userResult.IsFailure)
        {
            return Result.Failure<User>(userResult.Error);
        }

        var user = userResult.Value;
        
        // Add default role
        user.AddRole("User");

        await _userRepository.AddAsync(user, cancellationToken);

        _logger.LogInformation("User {UserName} registered successfully", userName);

        return Result.Success(user);
    }
}

public sealed record AuthenticationResult(
    string AccessToken,
    string RefreshToken,
    int UserId,
    string UserName,
    string Email,
    List<string> Roles);
