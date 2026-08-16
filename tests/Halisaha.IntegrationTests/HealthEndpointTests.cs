namespace Halisaha.IntegrationTests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task LiveHealthReturnsSuccessWithoutDatabaseDependency()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

}
