using System.Globalization;
using Halisaha.Application.Availability;
using Halisaha.Domain.Common;
using Halisaha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Halisaha.Infrastructure.Availability;

internal sealed class AvailabilitySearchService(HalisahaDbContext dbContext, TimeProvider timeProvider)
    : IAvailabilitySearchService
{
    private const int MaximumResults = 100;

    public async Task<AvailabilitySearchResult> SearchAsync(
        AvailabilitySearchQuery query,
        CancellationToken cancellationToken)
    {
        var errors = Validate(query);
        if (errors.Count > 0)
        {
            return AvailabilitySearchResult.Invalid("Arama kriterleri geçersiz.", errors);
        }

        var localStart = query.LocalDate.ToDateTime(query.LocalTime, DateTimeKind.Unspecified);
        var localEnd = localStart.AddMinutes(query.DurationMinutes);
        if (DateOnly.FromDateTime(localEnd) != query.LocalDate)
        {
            return AvailabilitySearchResult.Invalid(
                "Arama kriterleri geçersiz.",
                new Dictionary<string, string[]>
                {
                    ["durationMinutes"] = ["Arama süresi aynı takvim günü içinde kalmalıdır."]
                });
        }

        var district = query.District.Trim();
        var candidates = await (
                from field in dbContext.Fields.AsNoTracking()
                join facility in dbContext.Facilities.AsNoTracking() on field.FacilityId equals facility.Id
                join business in dbContext.Businesses.AsNoTracking() on facility.BusinessId equals business.Id
                where business.Status == BusinessStatus.Approved
                    && facility.Status == PublishStatus.Published
                    && field.Status == PublishStatus.Published
                    && facility.District == district
                orderby facility.Name, field.Name
                select new Candidate(
                    business.Name,
                    facility.Id,
                    facility.Name,
                    facility.District,
                    facility.AddressLine,
                    facility.TimeZoneId,
                    field.Id,
                    field.Name,
                    field.SurfaceType,
                    field.Capacity,
                    field.IsIndoor))
            .Take(MaximumResults)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return AvailabilitySearchResult.Success([]);
        }

        var facilityIds = candidates.Select(candidate => candidate.FacilityId).Distinct().ToArray();
        var fieldIds = candidates.Select(candidate => candidate.FieldId).ToArray();
        var rangeStartUtc = DateTime.SpecifyKind(localStart.AddHours(-14), DateTimeKind.Utc);
        var rangeEndUtc = DateTime.SpecifyKind(localEnd.AddHours(14), DateTimeKind.Utc);

        var weeklyHours = await dbContext.WeeklyHours.AsNoTracking()
            .Where(hours => facilityIds.Contains(hours.FacilityId)
                && hours.DayOfWeek == query.LocalDate.DayOfWeek)
            .ToListAsync(cancellationToken);
        var specialHours = await dbContext.SpecialHours.AsNoTracking()
            .Where(hours => facilityIds.Contains(hours.FacilityId)
                && hours.LocalDate == query.LocalDate)
            .ToListAsync(cancellationToken);
        var priceRules = await dbContext.PriceRules.AsNoTracking()
            .Where(rule => fieldIds.Contains(rule.FieldId)
                && rule.IsActive
                && rule.EffectiveFrom <= query.LocalDate
                && (rule.EffectiveTo == null || rule.EffectiveTo >= query.LocalDate))
            .ToListAsync(cancellationToken);
        var fieldBlocks = await dbContext.FieldBlocks.AsNoTracking()
            .Where(block => fieldIds.Contains(block.FieldId)
                && block.Status == FieldBlockStatus.Active
                && block.StartsAtUtc < rangeEndUtc
                && block.EndsAtUtc > rangeStartUtc)
            .ToListAsync(cancellationToken);
        var reservationSlots = await dbContext.ReservationSlots.AsNoTracking()
            .Where(slot => fieldIds.Contains(slot.FieldId)
                && slot.IsActive
                && slot.SlotStartUtc < rangeEndUtc
                && slot.SlotEndUtc > rangeStartUtc)
            .ToListAsync(cancellationToken);
        var amenities = await (
                from item in dbContext.FieldAmenities.AsNoTracking()
                join amenity in dbContext.Amenities.AsNoTracking() on item.AmenityId equals amenity.Id
                where fieldIds.Contains(item.FieldId)
                orderby amenity.Name
                select new { item.FieldId, amenity.Name })
            .ToListAsync(cancellationToken);

        var results = new List<AvailableField>();
        foreach (var candidate in candidates)
        {
            var special = specialHours.SingleOrDefault(hours => hours.FacilityId == candidate.FacilityId);
            var weekly = weeklyHours.SingleOrDefault(hours => hours.FacilityId == candidate.FacilityId);
            var opensAt = special?.OpensAt ?? weekly?.OpensAt;
            var closesAt = special?.ClosesAt ?? weekly?.ClosesAt;
            if ((special?.IsClosed ?? weekly?.IsClosed) != false
                || opensAt is null
                || closesAt is null
                || query.LocalTime < opensAt
                || TimeOnly.FromDateTime(localEnd) > closesAt)
            {
                continue;
            }

            if (!TryConvertToUtc(candidate.TimeZoneId, localStart, localEnd, out var startsAtUtc, out var endsAtUtc, out var timeError))
            {
                return AvailabilitySearchResult.Invalid(
                    "Seçilen yerel saat kullanılamıyor.",
                    new Dictionary<string, string[]> { ["localTime"] = [timeError] });
            }

            if (startsAtUtc <= timeProvider.GetUtcNow().UtcDateTime)
            {
                continue;
            }

            var isBlocked = fieldBlocks.Any(block => block.FieldId == candidate.FieldId
                && block.StartsAtUtc < endsAtUtc
                && block.EndsAtUtc > startsAtUtc);
            var isReserved = reservationSlots.Any(slot => slot.FieldId == candidate.FieldId
                && slot.SlotStartUtc < endsAtUtc
                && slot.SlotEndUtc > startsAtUtc);
            if (isBlocked || isReserved)
            {
                continue;
            }

            var priceRule = priceRules
                .Where(rule => rule.FieldId == candidate.FieldId
                    && (rule.DayOfWeek is null || rule.DayOfWeek == query.LocalDate.DayOfWeek)
                    && rule.StartsAt <= query.LocalTime
                    && rule.EndsAt >= TimeOnly.FromDateTime(localEnd))
                .OrderByDescending(rule => rule.Priority)
                .ThenBy(rule => rule.Price)
                .FirstOrDefault();
            if (priceRule is null)
            {
                continue;
            }

            results.Add(new AvailableField(
                candidate.FieldId,
                candidate.FacilityId,
                candidate.BusinessName,
                candidate.FacilityName,
                candidate.FieldName,
                candidate.District,
                candidate.AddressLine,
                candidate.SurfaceType,
                candidate.Capacity,
                candidate.IsIndoor,
                priceRule.Price,
                priceRule.Currency,
                startsAtUtc,
                endsAtUtc,
                query.LocalTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                TimeOnly.FromDateTime(localEnd).ToString("HH:mm", CultureInfo.InvariantCulture),
                amenities
                    .Where(amenity => amenity.FieldId == candidate.FieldId)
                    .Select(amenity => amenity.Name)
                    .ToArray()));
        }

        return AvailabilitySearchResult.Success(results);
    }

    private static Dictionary<string, string[]> Validate(AvailabilitySearchQuery query)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (query.District.Trim().Length is < 2 or > 100)
        {
            errors["district"] = ["İlçe 2 ile 100 karakter arasında olmalıdır."];
        }

        if (query.DurationMinutes is not (60 or 90 or 120))
        {
            errors["durationMinutes"] = ["Süre 60, 90 veya 120 dakika olmalıdır."];
        }

        if (query.LocalTime.Minute % 30 != 0 || query.LocalTime.Second != 0)
        {
            errors["localTime"] = ["Başlangıç saati 30 dakikalık dilimlerden biri olmalıdır."];
        }

        return errors;
    }

    private static bool TryConvertToUtc(
        string timeZoneId,
        DateTime localStart,
        DateTime localEnd,
        out DateTime startsAtUtc,
        out DateTime endsAtUtc,
        out string error)
    {
        startsAtUtc = default;
        endsAtUtc = default;
        error = string.Empty;

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            error = "Tesis saat dilimi yapılandırılamadı.";
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            error = "Tesis saat dilimi yapılandırılamadı.";
            return false;
        }

        if (timeZone.IsInvalidTime(localStart) || timeZone.IsInvalidTime(localEnd))
        {
            error = "Seçilen saat, yaz saati geçişi nedeniyle mevcut değil.";
            return false;
        }

        if (timeZone.IsAmbiguousTime(localStart) || timeZone.IsAmbiguousTime(localEnd))
        {
            error = "Seçilen saat, yaz saati geçişi nedeniyle belirsiz.";
            return false;
        }

        startsAtUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        endsAtUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);
        return true;
    }

    private sealed record Candidate(
        string BusinessName,
        Guid FacilityId,
        string FacilityName,
        string District,
        string AddressLine,
        string TimeZoneId,
        Guid FieldId,
        string FieldName,
        string SurfaceType,
        int Capacity,
        bool IsIndoor);
}
