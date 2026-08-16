using Halisaha.Domain.Common;

namespace Halisaha.Domain.Tenancy;

public sealed class Business : AuditedEntity
{
    public string Name { get; set; } = string.Empty;

    public string? LegalName { get; set; }

    public BusinessStatus Status { get; set; }

    public string TimeZoneId { get; set; } = "Europe/Istanbul";
}

public sealed class BusinessMember : Entity
{
    public Guid BusinessId { get; set; }

    public Guid UserId { get; set; }

    public BusinessMemberRole Role { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
