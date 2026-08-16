using Halisaha.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Halisaha.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

public sealed class ApplicationRole : IdentityRole<Guid>;

public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public string? RevocationReason { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
