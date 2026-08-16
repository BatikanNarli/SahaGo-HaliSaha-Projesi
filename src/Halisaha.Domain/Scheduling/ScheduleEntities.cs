using Halisaha.Domain.Common;

namespace Halisaha.Domain.Scheduling;

public sealed class WeeklyHours : Entity
{
    public Guid FacilityId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly? OpensAt { get; set; }

    public TimeOnly? ClosesAt { get; set; }

    public bool IsClosed { get; set; }
}

public sealed class SpecialHours : Entity
{
    public Guid FacilityId { get; set; }

    public DateOnly LocalDate { get; set; }

    public TimeOnly? OpensAt { get; set; }

    public TimeOnly? ClosesAt { get; set; }

    public bool IsClosed { get; set; }

    public string? Reason { get; set; }
}

public sealed class FieldBlock : AuditedEntity
{
    public Guid FieldId { get; set; }

    public DateTime StartsAtUtc { get; set; }

    public DateTime EndsAtUtc { get; set; }

    public string Reason { get; set; } = string.Empty;

    public FieldBlockStatus Status { get; set; }
}

public sealed class PriceRule : AuditedEntity
{
    public Guid FieldId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DayOfWeek? DayOfWeek { get; set; }

    public TimeOnly StartsAt { get; set; }

    public TimeOnly EndsAt { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = "TRY";

    public int Priority { get; set; }

    public bool IsActive { get; set; }
}
