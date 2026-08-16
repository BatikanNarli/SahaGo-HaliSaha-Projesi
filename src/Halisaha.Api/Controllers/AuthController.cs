using System.Security.Claims;
using Halisaha.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Halisaha.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(
    IAuthService authService,
    IWebHostEnvironment environment) : ControllerBase
{
    private const string RefreshCookieName = "halisaha.refresh";

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(
            new RegisterCommand(request.DisplayName, request.Email, request.PhoneNumber, request.Password),
            HttpContext.TraceIdentifier,
            cancellationToken);
        if (!result.Succeeded) return FromFailure(result.Failure!);

        SetRefreshCookie(result.Session!);
        return StatusCode(StatusCodes.Status201Created, ToResponse(result.Session!));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(
            new LoginCommand(request.Identifier, request.Password),
            HttpContext.TraceIdentifier,
            cancellationToken);
        if (!result.Succeeded) return FromFailure(result.Failure!);

        SetRefreshCookie(result.Session!);
        return Ok(ToResponse(result.Session!));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken);
        var result = await authService.RefreshAsync(
            refreshToken ?? string.Empty,
            HttpContext.TraceIdentifier,
            cancellationToken);
        if (!result.Succeeded)
        {
            DeleteRefreshCookie();
            return FromFailure(result.Failure!);
        }

        SetRefreshCookie(result.Session!);
        return Ok(ToResponse(result.Session!));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken);
        await authService.LogoutAsync(refreshToken, HttpContext.TraceIdentifier, cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        return Ok(new CurrentUserResponse(Guid.Parse(userId!), displayName, roles));
    }

    private ObjectResult FromFailure(AuthFailure failure)
    {
        var status = failure.Code switch
        {
            AuthFailureCode.Validation => StatusCodes.Status400BadRequest,
            AuthFailureCode.Conflict => StatusCodes.Status409Conflict,
            AuthFailureCode.InvalidCredentials or AuthFailureCode.InvalidRefreshToken => StatusCodes.Status401Unauthorized,
            AuthFailureCode.AccountDisabled => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };
        var details = new ProblemDetails
        {
            Status = status,
            Title = failure.Message,
            Type = $"https://httpstatuses.com/{status}"
        };
        details.Extensions["correlationId"] = HttpContext.TraceIdentifier;
        if (failure.Errors is not null) details.Extensions["errors"] = failure.Errors;
        return StatusCode(status, details);
    }

    private void SetRefreshCookie(AuthSession session) =>
        Response.Cookies.Append(
            RefreshCookieName,
            session.RefreshToken,
            CreateCookieOptions(session.RefreshTokenExpiresAtUtc));

    private void DeleteRefreshCookie() =>
        Response.Cookies.Delete(RefreshCookieName, CreateCookieOptions(DateTime.UnixEpoch));

    private CookieOptions CreateCookieOptions(DateTime expiresAtUtc) => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = "/api/v1/auth",
        Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)),
        IsEssential = true
    };

    private static AuthResponse ToResponse(AuthSession session) => new(
        session.AccessToken,
        session.AccessTokenExpiresAtUtc,
        new AuthUserResponse(
            session.User.Id,
            session.User.DisplayName,
            session.User.Email,
            session.User.PhoneNumber,
            session.User.Roles));
}

public sealed record RegisterRequest(string DisplayName, string? Email, string? PhoneNumber, string Password);
public sealed record LoginRequest(string Identifier, string Password);
public sealed record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, AuthUserResponse User);
public sealed record AuthUserResponse(Guid Id, string DisplayName, string? Email, string? PhoneNumber, IReadOnlyCollection<string> Roles);
public sealed record CurrentUserResponse(Guid Id, string DisplayName, IReadOnlyCollection<string> Roles);
