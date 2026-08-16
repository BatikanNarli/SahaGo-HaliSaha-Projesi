using Halisaha.Application.Identity;
using Halisaha.Domain.Common;
using Halisaha.Domain.Governance;
using Halisaha.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Halisaha.Infrastructure.Identity;

internal sealed class AuthService(
    HalisahaDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    TokenIssuer tokenIssuer,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(
        RegisterCommand command,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var errors = CredentialPolicy.ValidateRegistration(command);
        if (errors.Count > 0)
        {
            return AuthResult.Failed(AuthFailureCode.Validation, "Kayıt bilgileri geçersiz.", errors);
        }

        var email = NullIfWhiteSpace(command.Email)?.ToLowerInvariant();
        var phoneNumber = NullIfWhiteSpace(command.PhoneNumber);
        if (email is not null && await dbContext.Users.AnyAsync(user => user.NormalizedEmail == userManager.NormalizeEmail(email), cancellationToken))
        {
            return AuthResult.Failed(AuthFailureCode.Conflict, "Bu e-posta veya telefon zaten kullanılıyor.");
        }

        if (phoneNumber is not null && await dbContext.Users.AnyAsync(user => user.PhoneNumber == phoneNumber, cancellationToken))
        {
            return AuthResult.Failed(AuthFailureCode.Conflict, "Bu e-posta veya telefon zaten kullanılıyor.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            DisplayName = command.DisplayName.Trim(),
            Email = email,
            PhoneNumber = phoneNumber,
            UserName = email ?? phoneNumber,
            Status = UserStatus.Active,
            LockoutEnabled = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);
        var createResult = await userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
        {
            return ToRegistrationFailure(createResult);
        }

        var roleResult = await userManager.AddToRoleAsync(user, AuthRoles.Customer);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException("Customer role could not be assigned.");
        }

        var session = await CreateSessionAsync(user, [AuthRoles.Customer], cancellationToken);
        AddAudit(user.Id, "Identity.Register", "User", user.Id.ToString(), correlationId);
        await dbContext.SaveChangesAsync(cancellationToken);
        await CommitAsync(transaction, cancellationToken);
        return AuthResult.Success(session);
    }

    public async Task<AuthResult> LoginAsync(
        LoginCommand command,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var identifier = command.Identifier.Trim();
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(command.Password))
        {
            return AuthResult.Failed(AuthFailureCode.Validation, "Kullanıcı bilgileri gereklidir.");
        }

        var user = await FindByIdentifierAsync(identifier, cancellationToken);
        if (user is null || user.Status != UserStatus.Active || await userManager.IsLockedOutAsync(user))
        {
            return InvalidCredentials();
        }

        if (!await userManager.CheckPasswordAsync(user, command.Password))
        {
            await userManager.AccessFailedAsync(user);
            return InvalidCredentials();
        }

        await userManager.ResetAccessFailedCountAsync(user);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        user.LastLoginAtUtc = now;
        user.UpdatedAtUtc = now;
        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var session = await CreateSessionAsync(user, roles, cancellationToken);
        AddAudit(user.Id, "Identity.Login", "User", user.Id.ToString(), correlationId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AuthResult.Success(session);
    }

    public async Task<AuthResult> RefreshAsync(
        string refreshToken,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return InvalidRefreshToken();
        }

        var tokenHash = TokenIssuer.HashRefreshToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);
        if (storedToken is null)
        {
            return InvalidRefreshToken();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (storedToken.RevokedAtUtc is not null)
        {
            await RevokeAllActiveTokensAsync(storedToken.UserId, "Refresh token reuse detected", now, cancellationToken);
            return InvalidRefreshToken();
        }

        if (storedToken.ExpiresAtUtc <= now)
        {
            storedToken.RevokedAtUtc = now;
            storedToken.RevocationReason = "Expired";
            await dbContext.SaveChangesAsync(cancellationToken);
            return InvalidRefreshToken();
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user is null || user.Status != UserStatus.Active)
        {
            return InvalidRefreshToken();
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var replacement = tokenIssuer.IssueRefreshToken();
        storedToken.RevokedAtUtc = now;
        storedToken.ReplacedByTokenId = replacement.Id;
        storedToken.RevocationReason = "Rotated";
        var session = CreateSession(user, roles, replacement);
        AddRefreshToken(user.Id, replacement, now);
        AddAudit(user.Id, "Identity.Refresh", "User", user.Id.ToString(), correlationId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AuthResult.Success(session);
    }

    public async Task LogoutAsync(
        string? refreshToken,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var tokenHash = TokenIssuer.HashRefreshToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);
        if (storedToken is null || storedToken.RevokedAtUtc is not null) return;

        storedToken.RevokedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        storedToken.RevocationReason = "Logout";
        AddAudit(storedToken.UserId, "Identity.Logout", "User", storedToken.UserId.ToString(), correlationId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken)
    {
        if (identifier.Contains('@', StringComparison.Ordinal))
        {
            return await userManager.FindByEmailAsync(identifier);
        }

        return await dbContext.Users.SingleOrDefaultAsync(
            user => user.PhoneNumber == identifier,
            cancellationToken);
    }

    private async Task<AuthSession> CreateSessionAsync(
        ApplicationUser user,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var refreshToken = tokenIssuer.IssueRefreshToken();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        AddRefreshToken(user.Id, refreshToken, now);
        return CreateSession(user, roles, refreshToken);
    }

    private AuthSession CreateSession(
        ApplicationUser user,
        IReadOnlyCollection<string> roles,
        IssuedRefreshToken refreshToken)
    {
        var accessToken = tokenIssuer.IssueAccessToken(user, roles);
        return new AuthSession(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refreshToken.Value,
            refreshToken.ExpiresAtUtc,
            new AuthUser(user.Id, user.DisplayName, user.Email, user.PhoneNumber, roles));
    }

    private void AddRefreshToken(Guid userId, IssuedRefreshToken refreshToken, DateTime now) =>
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = refreshToken.Id,
            UserId = userId,
            TokenHash = refreshToken.Hash,
            CreatedAtUtc = now,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc
        });

    private void AddAudit(Guid userId, string action, string entityType, string? entityId, string? correlationId) =>
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Outcome = "Succeeded",
            CorrelationId = correlationId,
            OccurredAtUtc = timeProvider.GetUtcNow().UtcDateTime
        });

    private async Task RevokeAllActiveTokensAsync(
        Guid userId,
        string reason,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null && token.ExpiresAtUtc > revokedAtUtc)
            .ToListAsync(cancellationToken);
        foreach (var activeToken in activeTokens)
        {
            activeToken.RevokedAtUtc = revokedAtUtc;
            activeToken.RevocationReason = reason;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken cancellationToken) =>
        dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

    private static Task CommitAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken) =>
        transaction is null ? Task.CompletedTask : transaction.CommitAsync(cancellationToken);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AuthResult InvalidCredentials() =>
        AuthResult.Failed(AuthFailureCode.InvalidCredentials, "Kullanıcı bilgileri geçersiz.");

    private static AuthResult InvalidRefreshToken() =>
        AuthResult.Failed(AuthFailureCode.InvalidRefreshToken, "Oturum yenilenemedi.");

    private static AuthResult ToRegistrationFailure(IdentityResult result)
    {
        var conflict = result.Errors.Any(error => error.Code.StartsWith("Duplicate", StringComparison.Ordinal));
        var errors = result.Errors
            .GroupBy(error => error.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray(), StringComparer.OrdinalIgnoreCase);
        return AuthResult.Failed(
            conflict ? AuthFailureCode.Conflict : AuthFailureCode.Validation,
            conflict ? "Bu e-posta veya telefon zaten kullanılıyor." : "Kayıt bilgileri geçersiz.",
            errors);
    }
}
