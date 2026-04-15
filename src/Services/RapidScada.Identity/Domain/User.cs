using RapidScada.Domain.Common;

namespace RapidScada.Identity.Domain;

/// <summary>
/// User entity for authentication
/// </summary>
public sealed class User : Entity<UserId>
{
    private User(UserId id, string userName, string email, string passwordHash)
        : base(id)
    {
        UserName = userName;
        Email = email;
        PasswordHash = passwordHash;
        _roles = new List<string>();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public string UserName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    
    private readonly List<string> _roles;
    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();
    
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }

    public static Result<User> Create(UserId id, string userName, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return Result.Failure<User>(Error.Validation("UserName cannot be empty"));

        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<User>(Error.Validation("Email cannot be empty"));

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<User>(Error.Validation("PasswordHash cannot be empty"));

        return Result.Success(new User(id, userName, email, passwordHash));
    }

    public void AddRole(string role)
    {
        if (!_roles.Contains(role))
        {
            _roles.Add(role);
        }
    }

    public void RemoveRole(string role)
    {
        _roles.Remove(role);
    }

    public bool HasRole(string role) => _roles.Contains(role);

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void SetRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = expiry;
    }

    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void EnableTwoFactor(string secret)
    {
        TwoFactorEnabled = true;
        TwoFactorSecret = secret;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorSecret = null;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }
}

/// <summary>
/// Strongly-typed User ID
/// </summary>
public sealed record UserId
{
    public int Value { get; }

    private UserId(int value) => Value = value;

    public static UserId Create(int value) => new(value);
    public static UserId New() => new(0); // EF will assign on insert

    public override string ToString() => Value.ToString();
}
