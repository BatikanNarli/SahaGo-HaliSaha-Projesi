using System.Security.Cryptography;
using System.Text;
using Halisaha.Domain.Catalog;
using Halisaha.Domain.Common;
using Halisaha.Domain.Scheduling;
using Halisaha.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Halisaha.Infrastructure.Persistence;

public sealed class DevelopmentDataSeeder(HalisahaDbContext dbContext, TimeProvider timeProvider)
{
    private static readonly Guid BusinessOneId = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f4629e01");
    private static readonly Guid BusinessTwoId = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f4629e02");
    private static readonly Guid KadikoyFacilityId = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f4629f01");
    private static readonly Guid AtasehirFacilityId = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f4629f02");
    private static readonly Guid UskudarFacilityId = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f4629f03");
    private static readonly Guid DistrictCatalogBusinessId = DeterministicGuid("business:istanbul-ilce-sahalari");
    private static readonly string[] IstanbulDistricts =
    [
        "Adalar", "Arnavutköy", "Ataşehir", "Avcılar", "Bağcılar", "Bahçelievler", "Bakırköy",
        "Başakşehir", "Bayrampaşa", "Beşiktaş", "Beykoz", "Beylikdüzü", "Beyoğlu", "Büyükçekmece",
        "Çatalca", "Çekmeköy", "Esenler", "Esenyurt", "Eyüpsultan", "Fatih", "Gaziosmanpaşa",
        "Güngören", "Kadıköy", "Kağıthane", "Kartal", "Küçükçekmece", "Maltepe", "Pendik",
        "Sancaktepe", "Sarıyer", "Silivri", "Sultanbeyli", "Sultangazi", "Şile", "Şişli", "Tuzla",
        "Ümraniye", "Üsküdar", "Zeytinburnu"
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (!await dbContext.Businesses.AnyAsync(business => business.Id == BusinessOneId, cancellationToken))
        {
            SeedOriginalSamples(now);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await SeedIstanbulDistrictCatalogAsync(now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private void SeedOriginalSamples(DateTime now)
    {
        dbContext.Businesses.AddRange(
            Audited(new Business { Id = BusinessOneId, Name = "İstanbul Spor Tesisleri", Status = BusinessStatus.Approved, TimeZoneId = "Europe/Istanbul" }, now),
            Audited(new Business { Id = BusinessTwoId, Name = "Anadolu Saha İşletmeleri", Status = BusinessStatus.Approved, TimeZoneId = "Europe/Istanbul" }, now));

        dbContext.Facilities.AddRange(
            Audited(new Facility { Id = KadikoyFacilityId, BusinessId = BusinessOneId, Name = "Kadıköy Arena", Description = "Ulaşımı kolay, aydınlatmalı örnek spor tesisi.", City = "İstanbul", District = "Kadıköy", AddressLine = "Örnek Mahallesi, Kadıköy", Latitude = 40.990500m, Longitude = 29.028900m, TimeZoneId = "Europe/Istanbul", Status = PublishStatus.Published }, now),
            Audited(new Facility { Id = AtasehirFacilityId, BusinessId = BusinessTwoId, Name = "Ataşehir Park", Description = "Kapalı ve açık saha seçenekleri bulunan örnek tesis.", City = "İstanbul", District = "Ataşehir", AddressLine = "Örnek Bulvarı, Ataşehir", Latitude = 40.992600m, Longitude = 29.124400m, TimeZoneId = "Europe/Istanbul", Status = PublishStatus.Published }, now),
            Audited(new Facility { Id = UskudarFacilityId, BusinessId = BusinessTwoId, Name = "Üsküdar Sahil Spor", Description = "Toplu taşımaya yakın örnek saha tesisi.", City = "İstanbul", District = "Üsküdar", AddressLine = "Örnek Caddesi, Üsküdar", Latitude = 41.025600m, Longitude = 29.015800m, TimeZoneId = "Europe/Istanbul", Status = PublishStatus.Published }, now));

        var fields = new[]
        {
            Audited(new Field { Id = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f462a001"), FacilityId = KadikoyFacilityId, Name = "Arena 1", Description = "FIFA kalite sentetik çim örnek saha.", SurfaceType = "Sentetik çim", Capacity = 14, IsIndoor = false, Status = PublishStatus.Published }, now),
            Audited(new Field { Id = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f462a002"), FacilityId = KadikoyFacilityId, Name = "Arena Kapalı", Description = "Dört mevsim kullanıma uygun kapalı örnek saha.", SurfaceType = "Sentetik çim", Capacity = 12, IsIndoor = true, Status = PublishStatus.Published }, now),
            Audited(new Field { Id = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f462a003"), FacilityId = AtasehirFacilityId, Name = "Park Saha", Description = "Geniş oyun alanlı örnek saha.", SurfaceType = "Hibrit çim", Capacity = 14, IsIndoor = false, Status = PublishStatus.Published }, now),
            Audited(new Field { Id = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f462a004"), FacilityId = UskudarFacilityId, Name = "Sahil Saha", Description = "Akşam aydınlatmalı örnek saha.", SurfaceType = "Sentetik çim", Capacity = 12, IsIndoor = false, Status = PublishStatus.Published }, now)
        };
        dbContext.Fields.AddRange(fields);

        var amenities = new[]
        {
            new Amenity { Id = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f462b001"), Name = "Soyunma odası", Slug = "soyunma-odasi" },
            new Amenity { Id = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f462b002"), Name = "Duş", Slug = "dus" },
            new Amenity { Id = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f462b003"), Name = "Otopark", Slug = "otopark" },
            new Amenity { Id = Guid.Parse("d137f01a-47ec-4f93-93bd-9cf6f462b004"), Name = "Kafeterya", Slug = "kafeterya" }
        };
        dbContext.Amenities.AddRange(amenities);
        dbContext.FieldAmenities.AddRange(fields.SelectMany(field => amenities
            .Take(field.IsIndoor ? 4 : 3)
            .Select(amenity => new FieldAmenity { FieldId = field.Id, AmenityId = amenity.Id })));

        dbContext.WeeklyHours.AddRange(new[] { KadikoyFacilityId, AtasehirFacilityId, UskudarFacilityId }.SelectMany(CreateWeeklyHours));
        dbContext.PriceRules.AddRange(fields.SelectMany(field => CreatePriceRules(field, now)));
    }

    private async Task SeedIstanbulDistrictCatalogAsync(DateTime now, CancellationToken cancellationToken)
    {
        if (!await dbContext.Businesses.AnyAsync(business => business.Id == DistrictCatalogBusinessId, cancellationToken))
        {
            dbContext.Businesses.Add(Audited(new Business
            {
                Id = DistrictCatalogBusinessId,
                Name = "İstanbul İlçe Sahaları",
                LegalName = "Yerel Geliştirme Örnek Kataloğu",
                Status = BusinessStatus.Approved,
                TimeZoneId = "Europe/Istanbul"
            }, now));
        }

        var facilityIds = IstanbulDistricts.ToDictionary(district => district, district => DeterministicGuid($"facility:{district}"));
        var candidateFacilityIds = facilityIds.Values.ToArray();
        var existingFacilityIds = await dbContext.Facilities.AsNoTracking()
            .Where(facility => candidateFacilityIds.Contains(facility.Id))
            .Select(facility => facility.Id)
            .ToHashSetAsync(cancellationToken);
        var newFacilities = IstanbulDistricts
            .Where(district => !existingFacilityIds.Contains(facilityIds[district]))
            .Select(district => Audited(new Facility
            {
                Id = facilityIds[district],
                BusinessId = DistrictCatalogBusinessId,
                Name = $"{district} Saha Merkezi",
                Description = $"{district} ilçesinde farklı kapasite ve zemin seçenekleri sunan geliştirme/test tesisi.",
                City = "İstanbul",
                District = district,
                AddressLine = $"Merkez Mahallesi, Spor Caddesi No: {Array.IndexOf(IstanbulDistricts, district) + 1}, {district}",
                TimeZoneId = "Europe/Istanbul",
                Status = PublishStatus.Published
            }, now))
            .ToArray();
        dbContext.Facilities.AddRange(newFacilities);
        dbContext.WeeklyHours.AddRange(newFacilities.SelectMany(facility => CreateWeeklyHours(facility.Id)));

        var fieldTemplates = new[]
        {
            new FieldTemplate("Merkez Arena", "Yeni nesil sentetik çim ve güçlü akşam aydınlatması.", "Sentetik çim", 14, false, 1_200m),
            new FieldTemplate("Park Halı Saha", "Soyunma odası ve otopark erişimli açık saha.", "Hibrit çim", 12, false, 1_300m),
            new FieldTemplate("Kapalı Futbol Sahası", "Dört mevsim kullanıma uygun kapalı futbol sahası.", "Sentetik çim", 14, true, 1_500m)
        };
        var plannedFields = IstanbulDistricts.SelectMany(district => fieldTemplates.Select((template, index) => new
        {
            Id = DeterministicGuid($"field:{district}:{index + 1}"),
            FacilityId = facilityIds[district],
            Template = template
        })).ToArray();
        var plannedFieldIds = plannedFields.Select(field => field.Id).ToArray();
        var existingFieldIds = await dbContext.Fields.AsNoTracking()
            .Where(field => plannedFieldIds.Contains(field.Id))
            .Select(field => field.Id)
            .ToHashSetAsync(cancellationToken);
        var newFields = plannedFields
            .Where(field => !existingFieldIds.Contains(field.Id))
            .Select(field => Audited(new Field
            {
                Id = field.Id,
                FacilityId = field.FacilityId,
                Name = field.Template.Name,
                Description = field.Template.Description,
                SurfaceType = field.Template.SurfaceType,
                Capacity = field.Template.Capacity,
                IsIndoor = field.Template.IsIndoor,
                Status = PublishStatus.Published
            }, now))
            .ToArray();
        dbContext.Fields.AddRange(newFields);
        dbContext.PriceRules.AddRange(newFields.SelectMany(field =>
        {
            var basePrice = fieldTemplates.Single(template => template.Name == field.Name).BasePrice;
            return CreatePriceRules(field, now, basePrice);
        }));

        var amenityIds = await dbContext.Amenities.AsNoTracking()
            .OrderBy(amenity => amenity.Slug)
            .Select(amenity => amenity.Id)
            .ToArrayAsync(cancellationToken);
        dbContext.FieldAmenities.AddRange(newFields.SelectMany(field => amenityIds.Take(field.IsIndoor ? 4 : 3)
            .Select(amenityId => new FieldAmenity { FieldId = field.Id, AmenityId = amenityId })));
    }

    private static IEnumerable<WeeklyHours> CreateWeeklyHours(Guid facilityId) =>
        Enum.GetValues<DayOfWeek>().Select(day => new WeeklyHours
        {
            Id = Guid.NewGuid(),
            FacilityId = facilityId,
            DayOfWeek = day,
            OpensAt = new TimeOnly(8, 0),
            ClosesAt = new TimeOnly(23, 59, 59),
            IsClosed = false
        });

    private static IEnumerable<PriceRule> CreatePriceRules(Field field, DateTime now, decimal? configuredBasePrice = null)
    {
        var basePrice = configuredBasePrice ?? (field.IsIndoor ? 1_400m : 1_150m);
        yield return Audited(new PriceRule
        {
            Id = Guid.NewGuid(), FieldId = field.Id, Name = "Gündüz tarifesi", StartsAt = new TimeOnly(8, 0), EndsAt = new TimeOnly(18, 0),
            EffectiveFrom = new DateOnly(2025, 1, 1), Price = basePrice, Currency = "TRY", Priority = 10, IsActive = true
        }, now);
        yield return Audited(new PriceRule
        {
            Id = Guid.NewGuid(), FieldId = field.Id, Name = "Akşam tarifesi", StartsAt = new TimeOnly(18, 0), EndsAt = new TimeOnly(23, 59, 59),
            EffectiveFrom = new DateOnly(2025, 1, 1), Price = basePrice + 350m, Currency = "TRY", Priority = 20, IsActive = true
        }, now);
    }

    private static Guid DeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static T Audited<T>(T entity, DateTime now) where T : AuditedEntity
    {
        entity.CreatedAtUtc = now;
        entity.UpdatedAtUtc = now;
        return entity;
    }

    private sealed record FieldTemplate(string Name, string Description, string SurfaceType, int Capacity, bool IsIndoor, decimal BasePrice);
}
