using System.Security.Claims;
using Halisaha.Application.Identity;
using Halisaha.Domain.Common;
using Halisaha.Domain.Tenancy;
using Halisaha.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Halisaha.IntegrationTests;

public sealed class BusinessAccessAuthorizationTests
{
    [Fact]
    public async Task BusinessOwnerCanAccessOwnBusinessButNotAnotherTenant()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var userId = Guid.NewGuid();
        var ownBusinessId = Guid.NewGuid();
        var otherBusinessId = Guid.NewGuid();
        var dbContext = scope.ServiceProvider.GetRequiredService<HalisahaDbContext>();
        dbContext.BusinessMembers.Add(new BusinessMember
        {
            Id = Guid.NewGuid(),
            BusinessId = ownBusinessId,
            UserId = userId,
            Role = BusinessMemberRole.BusinessOwner,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, AuthRoles.BusinessOwner)
        ], "test"));
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        httpContextAccessor.HttpContext = CreateHttpContext(ownBusinessId);
        var ownResult = await authorization.AuthorizeAsync(principal, null, AuthPolicies.BusinessAccess);
        Assert.True(ownResult.Succeeded);

        httpContextAccessor.HttpContext = CreateHttpContext(otherBusinessId);
        var otherResult = await authorization.AuthorizeAsync(principal, null, AuthPolicies.BusinessAccess);
        Assert.False(otherResult.Succeeded);
    }

    private static DefaultHttpContext CreateHttpContext(Guid businessId)
    {
        var context = new DefaultHttpContext();
        context.Request.RouteValues["businessId"] = businessId.ToString();
        return context;
    }
}
