using Halisaha.Domain.Common;

namespace Halisaha.Domain.Reservations;

public sealed class Reservation : AuditedEntity
{
    public Guid UserId { get; set; }

    public Guid BusinessId { get; set; }

    public Guid FacilityId { get; set; }

    public Guid FieldId { get; set; }

    public ReservationStatus Status { get; set; }

    public DateTime StartsAtUtc { get; set; }

    public DateTime EndsAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    public string Currency { get; set; } = "TRY";

    public string PriceSnapshotJson { get; set; } = "{}";

    public string CancellationPolicySnapshotJson { get; set; } = "{}";
}

public sealed class ReservationSlot : Entity
{
    public Guid ReservationId { get; set; }

    public Guid FieldId { get; set; }

    public DateTime SlotStartUtc { get; set; }

    public DateTime SlotEndUtc { get; set; }

    public bool IsActive { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

public sealed class IdempotencyRecord : Entity
{
    public Guid UserId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string RequestHash { get; set; } = string.Empty;

    public int ResponseStatusCode { get; set; }

    public string? ResponseBodyJson { get; set; }

    public Guid? ResourceId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
}
