using Halisaha.Domain.Common;

namespace Halisaha.Domain.Integration;

public sealed class OutboxMessage : Entity
{
    public string MessageType { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public ProcessingStatus Status { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public DateTime? NextAttemptAtUtc { get; set; }

    public int AttemptCount { get; set; }

    public string? LastErrorCode { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

public sealed class NotificationDelivery : Entity
{
    public Guid OutboxMessageId { get; set; }

    public Guid? UserId { get; set; }

    public NotificationChannel Channel { get; set; }

    public string DestinationHash { get; set; } = string.Empty;

    public string TemplateName { get; set; } = string.Empty;

    public DeliveryStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? SentAtUtc { get; set; }
}
