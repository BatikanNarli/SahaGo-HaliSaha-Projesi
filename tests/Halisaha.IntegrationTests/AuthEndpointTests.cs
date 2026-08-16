using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Halisaha.IntegrationTests;

public sealed class AuthEndpointTests
{
    [Fact]
    public async Task RegisterCreatesCustomerAndRejectsDuplicateContact()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new() { HandleCookies = true });
        var request = new
        {
            displayName = "Test Oyuncu",
            email = "oyuncu@example.test",
            phoneNumber = "+905551112233",
            password = "Guvenli1234"
        };

        using var created = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var response = await created.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.Contains("Customer", response.User.Roles);
        Assert.Contains(created.Headers.GetValues("Set-Cookie"), value => value.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase));

        using var duplicate = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task RegisterAndLoginAcceptPhoneWithoutAnEmailAddress()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new() { HandleCookies = true });
        const string phoneNumber = "+905559998877";

        using var created = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            displayName = "Telefonlu Oyuncu",
            email = (string?)null,
            phoneNumber,
            password = "Guvenli1234"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var registered = await created.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(registered);
        Assert.Equal(phoneNumber, registered.User.PhoneNumber);

        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = phoneNumber,
            password = "Guvenli1234"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task LoginRejectsWrongPasswordAndAuthorizesBearerToken()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new() { HandleCookies = true });
        await RegisterAsync(client, "giris@example.test");

        using var rejected = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = "giris@example.test",
            password = "Yanlis12345"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);

        using var accepted = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = "giris@example.test",
            password = "Guvenli1234"
        });
        accepted.EnsureSuccessStatusCode();
        var auth = await accepted.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        using var me = await client.GetAsync("/api/v1/auth/me");
        Assert.True(me.IsSuccessStatusCode, $"Status: {me.StatusCode}; Challenge: {string.Join(", ", me.Headers.WwwAuthenticate)}");
        var body = await me.Content.ReadAsStringAsync();
        Assert.Contains("Customer", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefreshRotatesCookieAndLogoutRevokesSession()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient(new() { HandleCookies = true });
        await RegisterAsync(client, "refresh@example.test");

        using var refreshed = await client.PostAsync("/api/v1/auth/refresh", null);
        refreshed.EnsureSuccessStatusCode();
        Assert.Contains(refreshed.Headers.GetValues("Set-Cookie"), value => value.Contains("halisaha.refresh", StringComparison.Ordinal));

        using var logout = await client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        using var rejected = await client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
    }

    [Fact]
    public async Task AuthRateLimitReturnsProblemDetailsInsteadOfAnEmptyResponse()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        HttpResponseMessage? response = null;
        for (var attempt = 0; attempt < 11; attempt++)
        {
            response?.Dispose();
            response = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                identifier = "olmayan@example.test",
                password = "Yanlis12345"
            });
        }

        using (response)
        {
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Çok fazla deneme yapıldı. Bir dakika sonra tekrar deneyin.", problem.GetProperty("title").GetString());
            Assert.True(problem.TryGetProperty("correlationId", out _));
        }
    }

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            displayName = "Test Oyuncu",
            email,
            phoneNumber = (string?)null,
            password = "Guvenli1234"
        });
        response.EnsureSuccessStatusCode();
    }

    private sealed record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, AuthUser User);
    private sealed record AuthUser(Guid Id, string DisplayName, string? Email, string? PhoneNumber, string[] Roles);
}
