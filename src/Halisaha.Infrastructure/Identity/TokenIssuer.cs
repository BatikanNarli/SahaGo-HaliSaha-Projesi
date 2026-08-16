using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Halisaha.Infrastructure.Identity;

internal sealed record IssuedAccessToken(string Value, DateTime ExpiresAtUtc);
internal sealed record IssuedRefreshToken(Guid Id, string Value, string Hash, DateTime ExpiresAtUtc);

internal sealed class TokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JwtOptions jwtOptions = options.Value;

    public IssuedAccessToken IssueAccessToken(ApplicationUser user, IReadOnlyCollection<string> roles)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = now.AddMinutes(jwtOptions.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            jwtOptions.Issuer,
            jwtOptions.Audience,
            claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public IssuedRefreshToken IssueRefreshToken()
    {
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        var hash = HashRefreshToken(value);
        var expiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddDays(jwtOptions.RefreshTokenDays);
        return new IssuedRefreshToken(Guid.NewGuid(), value, hash, expiresAtUtc);
    }

    public static string HashRefreshToken(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
