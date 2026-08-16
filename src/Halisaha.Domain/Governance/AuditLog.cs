namespace Halisaha.Domain.Governance;

public sealed class AuditLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid? BusinessId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string Outcome { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }

    public string? IpAddressHash { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
