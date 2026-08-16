using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Halisaha.Application.Reservations;
using Halisaha.Application.Payments;
using Halisaha.Domain.Catalog;
using Halisaha.Domain.Common;
using Halisaha.Domain.Governance;
using Halisaha.Domain.Payments;
using Halisaha.Domain.Reservations;
using Halisaha.Domain.Tenancy;
using Halisaha.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Halisaha.Infrastructure.Reservations;

internal sealed class ReservationService(
    HalisahaDbContext dbContext,
    TimeProvider timeProvider,
    IPaymentGateway paymentGateway)
    : IReservationService
{
    private const int SlotMinutes = 30;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ReservationOperationResult> CreateAsync(
        Guid userId,
        CreateReservationCommand command,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCreate(command);
        if (validation is not null)
        {
            return validation;
        }

        var requestHash = Hash($"create|{command.FieldId:D}|{command.StartsAtUtc:O}|{command.EndsAtUtc:O}");
        var replay = await FindReplayAsync(userId, command.IdempotencyKey, requestHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (command.StartsAtUtc <= now)
            {
                return ReservationOperationResult.Fail(
                    ReservationFailureCode.Validation,
                    "Geçmiş bir saat için rezervasyon oluşturulamaz.",
                    new Dictionary<string, string[]> { ["startsAtUtc"] = ["Başlangıç saati gelecekte olmalıdır."] });
            }

            var candidate = await (
                    from field in dbContext.Fields
                    join facility in dbContext.Facilities on field.FacilityId equals facility.Id
                    join business in dbContext.Businesses on facility.BusinessId equals business.Id
                    where field.Id == command.FieldId
                        && field.Status == PublishStatus.Published
                        && facility.Status == PublishStatus.Published
                        && business.Status == BusinessStatus.Approved
                    select new Candidate(
                        business.Id,
                        business.Name,
                        facility.Id,
                        facility.Name,
                        facility.District,
                        facility.AddressLine,
                        facility.TimeZoneId,
                        field.Id,
                        field.Name))
                .SingleOrDefaultAsync(cancellationToken);

            if (candidate is null)
            {
                return ReservationOperationResult.Fail(ReservationFailureCode.NotFound, "Saha bulunamadı veya rezervasyona açık değil.");
            }

            var localStart = ConvertFromUtc(command.StartsAtUtc, candidate.TimeZoneId);
            var localEnd = ConvertFromUtc(command.EndsAtUtc, candidate.TimeZoneId);
            var localDate = DateOnly.FromDateTime(localStart);
            if (DateOnly.FromDateTime(localEnd) != localDate)
            {
                return ReservationOperationResult.Fail(ReservationFailureCode.Validation, "Rezervasyon aynı yerel takvim günü içinde kalmalıdır.");
            }

            var localStartTime = TimeOnly.FromDateTime(localStart);
            var localEndTime = TimeOnly.FromDateTime(localEnd);
            var specialHours = await dbContext.SpecialHours.AsNoTracking()
                .SingleOrDefaultAsync(hours => hours.FacilityId == candidate.FacilityId && hours.LocalDate == localDate, cancellationToken);
            var weeklyHours = await dbContext.WeeklyHours.AsNoTracking()
                .SingleOrDefaultAsync(hours => hours.FacilityId == candidate.FacilityId && hours.DayOfWeek == localDate.DayOfWeek, cancellationToken);
            var opensAt = specialHours?.OpensAt ?? weeklyHours?.OpensAt;
            var closesAt = specialHours?.ClosesAt ?? weeklyHours?.ClosesAt;
            if ((specialHours?.IsClosed ?? weeklyHours?.IsClosed) != false
                || opensAt is null
                || closesAt is null
                || localStartTime < opensAt
                || localEndTime > closesAt)
            {
                return ReservationOperationResult.Fail(ReservationFailureCode.Conflict, "Saha seçilen saatlerde rezervasyona açık değil.");
            }

            var blocked = await dbContext.FieldBlocks.AsNoTracking().AnyAsync(
                block => block.FieldId == candidate.FieldId
                    && block.Status == FieldBlockStatus.Active
                    && block.StartsAtUtc < command.EndsAtUtc
                    && block.EndsAtUtc > command.StartsAtUtc,
                cancellationToken);
            var reserved = await dbContext.ReservationSlots.AsNoTracking().AnyAsync(
                slot => slot.FieldId == candidate.FieldId
                    && slot.IsActive
                    && slot.SlotStartUtc < command.EndsAtUtc
                    && slot.SlotEndUtc > command.StartsAtUtc,
                cancellationToken);
            if (blocked || reserved)
            {
                return ReservationOperationResult.Fail(ReservationFailureCode.Conflict, "Bu saha ve saat başka bir rezervasyon tarafından alındı.");
            }

            var priceRule = await dbContext.PriceRules.AsNoTracking()
                .Where(rule => rule.FieldId == candidate.FieldId
                    && rule.IsActive
                    && rule.EffectiveFrom <= localDate
                    && (rule.EffectiveTo == null || rule.EffectiveTo >= localDate)
                    && (rule.DayOfWeek == null || rule.DayOfWeek == localDate.DayOfWeek)
                    && rule.StartsAt <= localStartTime
                    && rule.EndsAt >= localEndTime)
                .OrderByDescending(rule => rule.Priority)
                .ThenBy(rule => rule.Price)
                .FirstOrDefaultAsync(cancellationToken);
            if (priceRule is null)
            {
                return ReservationOperationResult.Fail(ReservationFailureCode.Conflict, "Seçilen saat için geçerli fiyat bulunamadı.");
            }

            var reservationId = Guid.NewGuid();
            var paymentCapture = await paymentGateway.CaptureAsync(
                reservationId,
                priceRule.Price,
                priceRule.Currency,
                cancellationToken);
            var reservation = new Reservation
            {
                Id = reservationId,
                UserId = userId,
                BusinessId = candidate.BusinessId,
                FacilityId = candidate.FacilityId,
                FieldId = candidate.FieldId,
                Status = ReservationStatus.Confirmed,
                StartsAtUtc = command.StartsAtUtc,
                EndsAtUtc = command.EndsAtUtc,
                Subtotal = priceRule.Price,
                Total = priceRule.Price,
                Currency = priceRule.Currency,
                PriceSnapshotJson = JsonSerializer.Serialize(new
                {
                    priceRuleId = priceRule.Id,
                    priceRule.Name,
                    priceRule.Price,
                    priceRule.Currency
                }, JsonOptions),
                CancellationPolicySnapshotJson = JsonSerializer.Serialize(new
                {
                    customerCancellation = "BeforeStart",
                    refund = "FakeSandboxFullRefund",
                    paymentMode = paymentCapture.Provider
                }, JsonOptions),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.Reservations.Add(reservation);

            for (var slotStart = command.StartsAtUtc; slotStart < command.EndsAtUtc; slotStart = slotStart.AddMinutes(SlotMinutes))
            {
                dbContext.ReservationSlots.Add(new ReservationSlot
                {
                    Id = Guid.NewGuid(),
                    ReservationId = reservation.Id,
                    FieldId = candidate.FieldId,
                    SlotStartUtc = slotStart,
                    SlotEndUtc = slotStart.AddMinutes(SlotMinutes),
                    IsActive = true
                });
            }

            dbContext.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(),
                ReservationId = reservation.Id,
                Provider = paymentCapture.Provider,
                ProviderPaymentId = paymentCapture.ProviderPaymentId,
                Status = PaymentStatus.Captured,
                Amount = reservation.Total,
                Currency = reservation.Currency,
                CapturedAtUtc = paymentCapture.CapturedAtUtc,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

            var view = Map(reservation, candidate, now);
            dbContext.IdempotencyRecords.Add(CreateIdempotencyRecord(
                userId,
                command.IdempotencyKey,
                requestHash,
                201,
                view,
                reservation.Id,
                now));
            dbContext.AuditLogs.Add(CreateAudit(userId, candidate.BusinessId, "Reservation.Created", reservation.Id, correlationId, now));

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return ReservationOperationResult.Success(view);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await TryRollbackAsync(transaction, cancellationToken);

            dbContext.ChangeTracker.Clear();
            var concurrentReplay = await FindReplayAsync(userId, command.IdempotencyKey, requestHash, cancellationToken);
            return concurrentReplay ?? ReservationOperationResult.Fail(
                ReservationFailureCode.Conflict,
                "Bu saha ve saat başka bir rezervasyon tarafından alındı.");
        }
        catch (Exception exception) when (IsDeadlock(exception))
        {
            await TryRollbackAsync(transaction, cancellationToken);
            dbContext.ChangeTracker.Clear();
            return ReservationOperationResult.Fail(
                ReservationFailureCode.Conflict,
                "Bu saha ve saat eşzamanlı başka bir rezervasyon tarafından alındı.");
        }
    }

    public async Task<IReadOnlyCollection<ReservationView>> ListMineAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var rows = await QueryMine(
                userId,
                nowForOrdering: now,
                skip: (page - 1) * pageSize,
                take: pageSize)
            .ToListAsync(cancellationToken);
        return rows.Select(row => Map(row.Reservation, row.Candidate, now)).ToArray();
    }

    public async Task<ReservationOperationResult> GetMineAsync(
        Guid userId,
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        var row = await QueryMine(userId, reservationId: reservationId)
            .SingleOrDefaultAsync(cancellationToken);
        return row is null
            ? ReservationOperationResult.Fail(ReservationFailureCode.NotFound, "Rezervasyon bulunamadı.")
            : ReservationOperationResult.Success(Map(row.Reservation, row.Candidate, timeProvider.GetUtcNow().UtcDateTime));
    }

    public async Task<ReservationOperationResult> CancelAsync(
        Guid userId,
        CancelReservationCommand command,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var keyFailure = ValidateIdempotencyKey(command.IdempotencyKey);
        if (keyFailure is not null)
        {
            return keyFailure;
        }

        var requestHash = Hash($"cancel|{command.ReservationId:D}");
        var replay = await FindReplayAsync(userId, command.IdempotencyKey, requestHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var row = await QueryMine(userId, tracking: true, reservationId: command.ReservationId)
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return ReservationOperationResult.Fail(ReservationFailureCode.NotFound, "Rezervasyon bulunamadı.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (row.Reservation.Status is ReservationStatus.CancelledByCustomer)
        {
            return ReservationOperationResult.Success(Map(row.Reservation, row.Candidate, now));
        }

        if (row.Reservation.Status is not (ReservationStatus.PendingPayment or ReservationStatus.Confirmed)
            || row.Reservation.StartsAtUtc <= now)
        {
            return ReservationOperationResult.Fail(
                ReservationFailureCode.Conflict,
                "Başlamış, tamamlanmış veya kapatılmış rezervasyon iptal edilemez.");
        }

        row.Reservation.Status = ReservationStatus.CancelledByCustomer;
        row.Reservation.CancelledAtUtc = now;
        row.Reservation.UpdatedAtUtc = now;
        var slots = await dbContext.ReservationSlots
            .Where(slot => slot.ReservationId == row.Reservation.Id && slot.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var slot in slots)
        {
            slot.IsActive = false;
        }

        var payment = await dbContext.Payments
            .SingleOrDefaultAsync(item => item.ReservationId == row.Reservation.Id && item.Status == PaymentStatus.Captured, cancellationToken);
        var alreadyRefunded = await dbContext.Refunds.AsNoTracking().AnyAsync(
            refund => refund.ReservationId == row.Reservation.Id && refund.Status == RefundStatus.Refunded,
            cancellationToken);
        if (payment is not null && !alreadyRefunded)
        {
            var refundResult = await paymentGateway.RefundAsync(
                payment.Id,
                row.Reservation.Id,
                payment.Amount,
                payment.Currency,
                cancellationToken);
            dbContext.Refunds.Add(new Refund
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                ReservationId = row.Reservation.Id,
                ProviderRefundId = refundResult.ProviderRefundId,
                Status = RefundStatus.Refunded,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Reason = "CustomerCancellationBeforeStart",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        var view = Map(row.Reservation, row.Candidate, now);
        dbContext.IdempotencyRecords.Add(CreateIdempotencyRecord(
            userId,
            command.IdempotencyKey,
            requestHash,
            200,
            view,
            row.Reservation.Id,
            now));
        dbContext.AuditLogs.Add(CreateAudit(userId, row.Candidate.BusinessId, "Reservation.CancelledByCustomer", row.Reservation.Id, correlationId, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return ReservationOperationResult.Success(view);
    }

    private IQueryable<ReservationRow> QueryMine(
        Guid userId,
        bool tracking = false,
        Guid? reservationId = null,
        DateTime? nowForOrdering = null,
        int skip = 0,
        int? take = null)
    {
        IQueryable<Reservation> reservations = tracking ? dbContext.Reservations : dbContext.Reservations.AsNoTracking();
        IQueryable<Field> fields = tracking ? dbContext.Fields : dbContext.Fields.AsNoTracking();
        IQueryable<Facility> facilities = tracking ? dbContext.Facilities : dbContext.Facilities.AsNoTracking();
        IQueryable<Business> businesses = tracking ? dbContext.Businesses : dbContext.Businesses.AsNoTracking();
        reservations = reservations.Where(reservation => reservation.UserId == userId);
        if (reservationId is not null)
        {
            reservations = reservations.Where(reservation => reservation.Id == reservationId.Value);
        }

        if (nowForOrdering is not null)
        {
            var now = nowForOrdering.Value;
            reservations = reservations
                .OrderByDescending(reservation => reservation.StartsAtUtc >= now)
                .ThenBy(reservation => reservation.StartsAtUtc >= now ? reservation.StartsAtUtc : DateTime.MaxValue)
                .ThenByDescending(reservation => reservation.StartsAtUtc)
                .Skip(skip);
            if (take is not null)
            {
                reservations = reservations.Take(take.Value);
            }
        }

        return from reservation in reservations
               join field in fields on reservation.FieldId equals field.Id
               join facility in facilities on reservation.FacilityId equals facility.Id
               join business in businesses on reservation.BusinessId equals business.Id
               select new ReservationRow(
                   reservation,
                   new Candidate(
                       business.Id,
                       business.Name,
                       facility.Id,
                       facility.Name,
                       facility.District,
                       facility.AddressLine,
                       facility.TimeZoneId,
                       field.Id,
                       field.Name));
    }

    private async Task<ReservationOperationResult?> FindReplayAsync(
        Guid userId,
        string key,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId && item.Key == key, cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(record.RequestHash),
                Encoding.ASCII.GetBytes(requestHash)))
        {
            return ReservationOperationResult.Fail(
                ReservationFailureCode.IdempotencyConflict,
                "Bu Idempotency-Key daha önce farklı bir istek için kullanıldı.");
        }

        var view = record.ResponseBodyJson is null
            ? null
            : JsonSerializer.Deserialize<ReservationView>(record.ResponseBodyJson, JsonOptions);
        return view is null
            ? ReservationOperationResult.Fail(ReservationFailureCode.Conflict, "Önceki istek sonucu okunamadı.")
            : ReservationOperationResult.Success(view, replayed: true);
    }

    private static ReservationOperationResult? ValidateCreate(CreateReservationCommand command)
    {
        var keyFailure = ValidateIdempotencyKey(command.IdempotencyKey);
        if (keyFailure is not null)
        {
            return keyFailure;
        }

        var errors = new Dictionary<string, string[]>();
        if (command.FieldId == Guid.Empty)
        {
            errors["fieldId"] = ["Saha seçilmelidir."];
        }

        var duration = command.EndsAtUtc - command.StartsAtUtc;
        if (duration.TotalMinutes is not (60 or 90 or 120))
        {
            errors["endsAtUtc"] = ["Rezervasyon süresi 60, 90 veya 120 dakika olmalıdır."];
        }

        if (command.StartsAtUtc.Kind != DateTimeKind.Utc || command.EndsAtUtc.Kind != DateTimeKind.Utc)
        {
            errors["startsAtUtc"] = ["Başlangıç ve bitiş UTC (Z) biçiminde gönderilmelidir."];
        }
        else if (command.StartsAtUtc.Second != 0
                 || command.StartsAtUtc.Millisecond != 0
                 || command.StartsAtUtc.Minute % SlotMinutes != 0)
        {
            errors["startsAtUtc"] = ["Başlangıç saati 30 dakikalık dilime hizalanmalıdır."];
        }

        return errors.Count == 0
            ? null
            : ReservationOperationResult.Fail(ReservationFailureCode.Validation, "Rezervasyon bilgileri geçersiz.", errors);
    }

    private static ReservationOperationResult? ValidateIdempotencyKey(string key) =>
        string.IsNullOrWhiteSpace(key) || key.Length is < 8 or > 100
            ? ReservationOperationResult.Fail(
                ReservationFailureCode.Validation,
                "Idempotency-Key geçersiz.",
                new Dictionary<string, string[]> { ["idempotencyKey"] = ["Idempotency-Key 8 ile 100 karakter arasında olmalıdır."] })
            : null;

    private static DateTime ConvertFromUtc(DateTime utc, string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        }
        catch (TimeZoneNotFoundException)
        {
            throw new InvalidOperationException("Tesis saat dilimi yapılandırılamadı.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new InvalidOperationException("Tesis saat dilimi yapılandırılamadı.");
        }
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken) =>
        dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

    private static IdempotencyRecord CreateIdempotencyRecord(
        Guid userId,
        string key,
        string requestHash,
        int responseStatusCode,
        ReservationView view,
        Guid resourceId,
        DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Key = key,
            RequestHash = requestHash,
            ResponseStatusCode = responseStatusCode,
            ResponseBodyJson = JsonSerializer.Serialize(view, JsonOptions),
            ResourceId = resourceId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddHours(24)
        };

    private static AuditLog CreateAudit(
        Guid userId,
        Guid businessId,
        string action,
        Guid reservationId,
        string correlationId,
        DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BusinessId = businessId,
            Action = action,
            EntityType = "Reservation",
            EntityId = reservationId.ToString("D"),
            Outcome = "Succeeded",
            CorrelationId = correlationId,
            OccurredAtUtc = now
        };

    private static ReservationView Map(Reservation reservation, Candidate candidate, DateTime now)
    {
        var localStart = ConvertFromUtc(reservation.StartsAtUtc, candidate.TimeZoneId);
        var localEnd = ConvertFromUtc(reservation.EndsAtUtc, candidate.TimeZoneId);
        var canCancel = reservation.Status is ReservationStatus.PendingPayment or ReservationStatus.Confirmed
            && reservation.StartsAtUtc > now;
        return new ReservationView(
            reservation.Id,
            reservation.Status.ToString(),
            reservation.FieldId,
            candidate.BusinessName,
            candidate.FacilityName,
            candidate.FieldName,
            candidate.District,
            candidate.AddressLine,
            reservation.StartsAtUtc,
            reservation.EndsAtUtc,
            DateOnly.FromDateTime(localStart).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly.FromDateTime(localStart).ToString("HH:mm", CultureInfo.InvariantCulture),
            TimeOnly.FromDateTime(localEnd).ToString("HH:mm", CultureInfo.InvariantCulture),
            reservation.Total,
            reservation.Currency,
            canCancel,
            reservation.CreatedAtUtc,
            reservation.CancelledAtUtc);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static bool IsDeadlock(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 1205 })
            {
                return true;
            }
        }

        return false;
    }

    private static async Task TryRollbackAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is null)
        {
            return;
        }

        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // SQL Server deadlock victims are already rolled back by the database.
        }
        catch (SqlException)
        {
            // The connection may already have discarded the aborted transaction.
        }
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed record Candidate(
        Guid BusinessId,
        string BusinessName,
        Guid FacilityId,
        string FacilityName,
        string District,
        string AddressLine,
        string TimeZoneId,
        Guid FieldId,
        string FieldName);

    private sealed record ReservationRow(Reservation Reservation, Candidate Candidate);
}
