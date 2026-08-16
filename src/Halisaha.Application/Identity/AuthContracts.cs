namespace Halisaha.Application.Identity;

public sealed record RegisterCommand(
    string DisplayName,
    string? Email,
    string? PhoneNumber,
    string Password);

public sealed record LoginCommand(string Identifier, string Password);

public sealed record AuthUser(
    Guid Id,
    string DisplayName,
    string? Email,
    string? PhoneNumber,
    IReadOnlyCollection<string> Roles);

public sealed record AuthSession(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    AuthUser User);

public enum AuthFailureCode
{
    Validation,
    Conflict,
    InvalidCredentials,
    InvalidRefreshToken,
    AccountDisabled
}

public sealed record AuthFailure(
    AuthFailureCode Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public sealed record AuthResult(AuthSession? Session, AuthFailure? Failure)
{
    public bool Succeeded => Session is not null;

    public static AuthResult Success(AuthSession session) => new(session, null);

    public static AuthResult Failed(
        AuthFailureCode code,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        new(null, new AuthFailure(code, message, errors));
}

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterCommand command, string? correlationId, CancellationToken cancellationToken);

    Task<AuthResult> LoginAsync(LoginCommand command, string? correlationId, CancellationToken cancellationToken);

    Task<AuthResult> RefreshAsync(string refreshToken, string? correlationId, CancellationToken cancellationToken);

    Task LogoutAsync(string? refreshToken, string? correlationId, CancellationToken cancellationToken);
}
