namespace Halisaha.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; set; }
}

public abstract class AuditedEntity : Entity
{
    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
