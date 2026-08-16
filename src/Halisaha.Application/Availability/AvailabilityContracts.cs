namespace Halisaha.Application.Availability;

public sealed record AvailabilitySearchQuery(
    string District,
    DateOnly LocalDate,
    TimeOnly LocalTime,
    int DurationMinutes);

public sealed record AvailableField(
    Guid FieldId,
    Guid FacilityId,
    string BusinessName,
    string FacilityName,
    string FieldName,
    string District,
    string AddressLine,
    string SurfaceType,
    int Capacity,
    bool IsIndoor,
    decimal Price,
    string Currency,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string LocalStart,
    string LocalEnd,
    IReadOnlyCollection<string> Amenities);

public sealed record AvailabilityFailure(
    string Message,
    IReadOnlyDictionary<string, string[]> Errors);

public sealed record AvailabilitySearchResult(
    IReadOnlyCollection<AvailableField>? Fields,
    AvailabilityFailure? Failure)
{
    public bool Succeeded => Failure is null;

    public static AvailabilitySearchResult Success(IReadOnlyCollection<AvailableField> fields) =>
        new(fields, null);

    public static AvailabilitySearchResult Invalid(
        string message,
        IReadOnlyDictionary<string, string[]> errors) =>
        new(null, new AvailabilityFailure(message, errors));
}

public interface IAvailabilitySearchService
{
    Task<AvailabilitySearchResult> SearchAsync(
        AvailabilitySearchQuery query,
        CancellationToken cancellationToken);
}
