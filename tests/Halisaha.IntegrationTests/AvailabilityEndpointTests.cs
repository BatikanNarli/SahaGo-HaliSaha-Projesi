using System.Net;
using System.Net.Http.Json;
using Halisaha.Domain.Common;
using Halisaha.Domain.Scheduling;
using Halisaha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Halisaha.IntegrationTests;

public sealed class AvailabilityEndpointTests
{
    private const string SearchUrl = "/api/v1/availability?district=Kad%C4%B1k%C3%B6y&localDate=2026-08-20&localTime=20%3A00&durationMinutes=60";

    [Fact]
    public async Task SearchReturnsPricedPublishedFieldsFromApprovedBusiness()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(SearchUrl);
        response.EnsureSuccessStatusCode();
        var fields = await response.Content.ReadFromJsonAsync<AvailableField[]>();

        Assert.NotNull(fields);
        Assert.True(fields.Length >= 3);
        Assert.All(fields, field =>
        {
            Assert.Equal("Kadıköy", field.District);
            Assert.Equal("TRY", field.Currency);
            Assert.True(field.Price > 0);
            Assert.Equal("20:00", field.LocalStart);
            Assert.Equal("21:00", field.LocalEnd);
            Assert.NotEmpty(field.Amenities);
        });
    }

    [Fact]
    public async Task ActiveFieldBlockRemovesOverlappingFieldFromSearch()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        Guid blockedFieldId;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<HalisahaDbContext>();
            blockedFieldId = await dbContext.Fields
                .Where(field => field.Name == "Arena 1")
                .Select(field => field.Id)
                .SingleAsync();
            var now = DateTime.UtcNow;
            dbContext.FieldBlocks.Add(new FieldBlock
            {
                Id = Guid.NewGuid(),
                FieldId = blockedFieldId,
                StartsAtUtc = new DateTime(2026, 8, 20, 17, 0, 0, DateTimeKind.Utc),
                EndsAtUtc = new DateTime(2026, 8, 20, 18, 0, 0, DateTimeKind.Utc),
                Reason = "Test blokajı",
                Status = FieldBlockStatus.Active,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            await dbContext.SaveChangesAsync();
        }

        using var response = await client.GetAsync(SearchUrl);
        response.EnsureSuccessStatusCode();
        var fields = await response.Content.ReadFromJsonAsync<AvailableField[]>();

        Assert.NotNull(fields);
        Assert.DoesNotContain(fields, field => field.FieldId == blockedFieldId);
    }

    [Fact]
    public async Task InvalidDurationReturnsProblemDetailsWithFieldError()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/v1/availability?district=Kad%C4%B1k%C3%B6y&localDate=2026-08-20&localTime=20%3A00&durationMinutes=45");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("durationMinutes", body, StringComparison.Ordinal);
        Assert.Contains("correlationId", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchDoesNotOfferPastSlotsThatReservationWouldReject()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/v1/availability?district=Kad%C4%B1k%C3%B6y&localDate=2026-08-15&localTime=20%3A00&durationMinutes=60");

        response.EnsureSuccessStatusCode();
        var fields = await response.Content.ReadFromJsonAsync<AvailableField[]>();
        Assert.Empty(fields!);
    }

    [Fact]
    public async Task DevelopmentCatalogContainsAtLeastThreeFieldsForEveryIstanbulDistrict()
    {
        await using var factory = new TestApiFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HalisahaDbContext>();

        var districtCounts = await (
                from field in dbContext.Fields.AsNoTracking()
                join facility in dbContext.Facilities.AsNoTracking() on field.FacilityId equals facility.Id
                where facility.City == "İstanbul" && field.Status == PublishStatus.Published
                group field by facility.District into district
                select new { District = district.Key, Count = district.Count() })
            .ToListAsync();

        Assert.Equal(39, districtCounts.Count);
        Assert.All(districtCounts, district => Assert.True(district.Count >= 3, $"{district.District} için en az 3 saha bekleniyordu."));
        Assert.Contains(districtCounts, district => district.District == "Pendik");
        Assert.Contains(districtCounts, district => district.District == "Kartal");
    }

    private sealed record AvailableField(
        Guid FieldId,
        string District,
        decimal Price,
        string Currency,
        string LocalStart,
        string LocalEnd,
        string[] Amenities);
}
