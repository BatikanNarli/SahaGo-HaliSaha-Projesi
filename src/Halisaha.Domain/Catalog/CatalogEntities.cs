using Halisaha.Domain.Common;

namespace Halisaha.Domain.Catalog;

public sealed class Facility : AuditedEntity
{
    public Guid BusinessId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string City { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string AddressLine { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string TimeZoneId { get; set; } = "Europe/Istanbul";

    public PublishStatus Status { get; set; }
}

public sealed class Field : AuditedEntity
{
    public Guid FacilityId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string SurfaceType { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public bool IsIndoor { get; set; }

    public PublishStatus Status { get; set; }
}

public sealed class Amenity : Entity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
}

public sealed class FieldAmenity
{
    public Guid FieldId { get; set; }

    public Guid AmenityId { get; set; }
}

public sealed class FacilityImage : Entity
{
    public Guid FacilityId { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public string AltText { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
