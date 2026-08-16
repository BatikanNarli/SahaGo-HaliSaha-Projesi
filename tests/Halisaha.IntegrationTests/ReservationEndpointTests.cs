using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Halisaha.Domain.Common;
using Halisaha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Halisaha.IntegrationTests;

public sealed class ReservationEndpointTests
{
    private const string SearchUrl = "/api/v1/availability?district=Pendik&localDate=2026-08-20&localTime=20%3A00&durationMinutes=60";

    [Fact]
    public async Task CustomerCanCreateReplayListAndCancelOwnReservation()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client, "rezervasyon@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var availableFields = await client.GetFromJsonAsync<AvailableField[]>(SearchUrl);
        var selected = Assert.Single(availableFields!, field => field.FieldName == "Merkez Arena");

        const string createKey = "test-create-reservation-001";
        using var created = await SendCreateAsync(client, selected, createKey);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var reservation = await created.Content.ReadFromJsonAsync<ReservationResponse>();
        Assert.NotNull(reservation);
        Assert.Equal("Confirmed", reservation.Status);
        Assert.True(reservation.CanCancel);
        Assert.Equal(selected.Price, reservation.Total);

        using var replayed = await SendCreateAsync(client, selected, createKey);
        Assert.Equal(HttpStatusCode.OK, replayed.StatusCode);
        var replayedReservation = await replayed.Content.ReadFromJsonAsync<ReservationResponse>();
        Assert.Equal(reservation.Id, replayedReservation!.Id);

        var mine = await client.GetFromJsonAsync<ReservationResponse[]>("/api/v1/reservations");
        Assert.Contains(mine!, item => item.Id == reservation.Id);

        using var cancelled = await SendCancelAsync(client, reservation.Id, "test-cancel-reservation-001");
        cancelled.EnsureSuccessStatusCode();
        var cancelledReservation = await cancelled.Content.ReadFromJsonAsync<ReservationResponse>();
        Assert.Equal("CancelledByCustomer", cancelledReservation!.Status);
        Assert.False(cancelledReservation.CanCancel);

        using var repeatedCancel = await SendCancelAsync(client, reservation.Id, "test-cancel-reservation-002");
        repeatedCancel.EnsureSuccessStatusCode();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HalisahaDbContext>();
        var persistedReservation = await dbContext.Reservations.AsNoTracking().SingleAsync(item => item.Id == reservation.Id);
        Assert.Equal(ReservationStatus.CancelledByCustomer, persistedReservation.Status);
        Assert.NotNull(persistedReservation.CancelledAtUtc);
        Assert.False(await dbContext.ReservationSlots.AnyAsync(slot => slot.ReservationId == reservation.Id && slot.IsActive));
        Assert.Equal(1, await dbContext.Refunds.CountAsync(refund => refund.ReservationId == reservation.Id && refund.Status == RefundStatus.Refunded));
        Assert.True(await dbContext.AuditLogs.AnyAsync(audit => audit.EntityId == reservation.Id.ToString() && audit.Action == "Reservation.CancelledByCustomer"));
    }

    [Fact]
    public async Task AnotherCustomerCannotReadOrCancelReservation()
    {
        await using var factory = new TestApiFactory();
        using var ownerClient = factory.CreateClient();
        var owner = await RegisterAsync(ownerClient, "sahibi@example.test");
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        var selected = (await ownerClient.GetFromJsonAsync<AvailableField[]>(SearchUrl))![0];
        using var created = await SendCreateAsync(ownerClient, selected, "owner-create-reservation-001");
        var reservation = await created.Content.ReadFromJsonAsync<ReservationResponse>();

        using var otherClient = factory.CreateClient();
        var other = await RegisterAsync(otherClient, "diger@example.test");
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other.AccessToken);

        using var get = await otherClient.GetAsync($"/api/v1/reservations/{reservation!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        using var cancel = await SendCancelAsync(otherClient, reservation.Id, "other-cancel-reservation-001");
        Assert.Equal(HttpStatusCode.NotFound, cancel.StatusCode);
    }

    [Fact]
    public async Task ReservationRequiresCustomerAuthenticationAndIdempotencyKey()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        var selected = (await client.GetFromJsonAsync<AvailableField[]>(SearchUrl))![0];

        using var anonymous = await client.PostAsJsonAsync("/api/v1/reservations", new
        {
            selected.FieldId,
            selected.StartsAtUtc,
            selected.EndsAtUtc
        });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal("application/problem+json", anonymous.Content.Headers.ContentType?.MediaType);
        using (var problem = await JsonDocument.ParseAsync(await anonymous.Content.ReadAsStreamAsync()))
        {
            Assert.Equal("Oturum geçersiz veya süresi dolmuş.", problem.RootElement.GetProperty("title").GetString());
            Assert.True(problem.RootElement.TryGetProperty("correlationId", out _));
        }

        var auth = await RegisterAsync(client, "anahtar@example.test");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        using var missingKey = await client.PostAsJsonAsync("/api/v1/reservations", new
        {
            selected.FieldId,
            selected.StartsAtUtc,
            selected.EndsAtUtc
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingKey.StatusCode);
    }

    private static async Task<HttpResponseMessage> SendCreateAsync(HttpClient client, AvailableField field, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(new { field.FieldId, field.StartsAtUtc, field.EndsAtUtc })
        };
        request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendCancelAsync(HttpClient client, Guid reservationId, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/reservations/{reservationId}/cancel");
        request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            displayName = "Rezervasyon Testi",
            email,
            phoneNumber = (string?)null,
            password = "Guvenli1234"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private sealed record AvailableField(Guid FieldId, string FieldName, decimal Price, DateTime StartsAtUtc, DateTime EndsAtUtc);
    private sealed record ReservationResponse(Guid Id, string Status, decimal Total, bool CanCancel);
    private sealed record AuthResponse(string AccessToken, JsonElement User);
}
