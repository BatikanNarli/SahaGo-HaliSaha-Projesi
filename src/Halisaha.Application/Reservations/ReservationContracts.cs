namespace Halisaha.Application.Reservations;

public sealed record CreateReservationCommand(
    Guid FieldId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string IdempotencyKey);

public sealed record CancelReservationCommand(Guid ReservationId, string IdempotencyKey);

public sealed record ReservationView(
    Guid Id,
    string Status,
    Guid FieldId,
    string BusinessName,
    string FacilityName,
    string FieldName,
    string District,
    string AddressLine,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string LocalDate,
    string LocalStart,
    string LocalEnd,
    decimal Total,
    string Currency,
    bool CanCancel,
    DateTime CreatedAtUtc,
    DateTime? CancelledAtUtc);

public enum ReservationFailureCode
{
    Validation,
    NotFound,
    Conflict,
    IdempotencyConflict
}

public sealed record ReservationFailure(
    ReservationFailureCode Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public sealed record ReservationOperationResult(
    ReservationView? Reservation,
    ReservationFailure? Failure,
    bool Replayed = false)
{
    public bool Succeeded => Failure is null;

    public static ReservationOperationResult Success(ReservationView reservation, bool replayed = false) =>
        new(reservation, null, replayed);

    public static ReservationOperationResult Fail(
        ReservationFailureCode code,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        new(null, new ReservationFailure(code, message, errors));
}

public interface IReservationService
{
    Task<ReservationOperationResult> CreateAsync(
        Guid userId,
        CreateReservationCommand command,
        string correlationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ReservationView>> ListMineAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ReservationOperationResult> GetMineAsync(
        Guid userId,
        Guid reservationId,
        CancellationToken cancellationToken);

    Task<ReservationOperationResult> CancelAsync(
        Guid userId,
        CancelReservationCommand command,
        string correlationId,
        CancellationToken cancellationToken);
}
