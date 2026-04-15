using Microsoft.EntityFrameworkCore;
using RapidScada.Identity.Domain;

namespace RapidScada.Identity.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .HasConversion(
                    id => id.Value,
                    value => UserId.Create(value))
                .HasColumnName("id");

            builder.Property(u => u.UserName)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("user_name");

            builder.HasIndex(u => u.UserName).IsUnique();

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("email");

            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasColumnName("password_hash");

            builder.Property(u => u.IsActive)
                .HasColumnName("is_active");

            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at");

            builder.Property(u => u.LastLoginAt)
                .HasColumnName("last_login_at");

            builder.Property(u => u.RefreshToken)
                .HasColumnName("refresh_token");

            builder.Property(u => u.RefreshTokenExpiry)
                .HasColumnName("refresh_token_expiry");

            builder.Property(u => u.TwoFactorEnabled)
                .HasColumnName("two_factor_enabled");

            builder.Property(u => u.TwoFactorSecret)
                .HasColumnName("two_factor_secret");

            // Store roles as JSON array (PostgreSQL)
            builder.Property(u => u.Roles)
                .HasConversion(
                    roles => System.Text.Json.JsonSerializer.Serialize(roles, (System.Text.Json.JsonSerializerOptions?)null),
                    json => System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("jsonb")
                .HasColumnName("roles");

            // Ignore domain events
            builder.Ignore(u => u.DomainEvents);
        });
    }
}
