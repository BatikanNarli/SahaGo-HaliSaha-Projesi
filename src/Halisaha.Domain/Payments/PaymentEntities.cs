using Halisaha.Domain.Common;

namespace Halisaha.Domain.Payments;

public sealed class Payment : AuditedEntity
{
    public Guid ReservationId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string? ProviderPaymentId { get; set; }

    public PaymentStatus Status { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public DateTime? CapturedAtUtc { get; set; }
}

public sealed class PaymentEvent : Entity
{
    public Guid? PaymentId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ProviderEventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string PayloadHash { get; set; } = string.Empty;

    public ProcessingStatus Status { get; set; }

    public DateTime ReceivedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public string? ErrorCode { get; set; }
}

public sealed class Refund : AuditedEntity
{
    public Guid PaymentId { get; set; }

    public Guid ReservationId { get; set; }

    public string? ProviderRefundId { get; set; }

    public RefundStatus Status { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Reason { get; set; } = string.Empty;
}
