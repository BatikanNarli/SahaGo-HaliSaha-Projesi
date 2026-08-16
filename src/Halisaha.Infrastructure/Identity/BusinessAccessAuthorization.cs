using System.Security.Claims;
using Halisaha.Application.Identity;
using Halisaha.Domain.Common;
using Halisaha.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Halisaha.Infrastructure.Identity;

internal sealed class BusinessAccessRequirement : IAuthorizationRequirement;

internal sealed class BusinessAccessHandler(
    HalisahaDbContext dbContext,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<BusinessAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BusinessAccessRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var businessIdValue = httpContext?.Request.RouteValues["businessId"]?.ToString();
        if (!Guid.TryParse(userIdValue, out var userId)
            || !Guid.TryParse(businessIdValue, out var businessId))
        {
            return;
        }

        var isOwner = context.User.IsInRole(AuthRoles.BusinessOwner);
        var isStaff = context.User.IsInRole(AuthRoles.BusinessStaff);
        if (!isOwner && !isStaff) return;

        var hasAccess = await dbContext.BusinessMembers.AsNoTracking().AnyAsync(
            member => member.BusinessId == businessId
                && member.UserId == userId
                && member.IsActive
                && ((isOwner && member.Role == BusinessMemberRole.BusinessOwner)
                    || (isStaff && member.Role == BusinessMemberRole.BusinessStaff)),
            httpContext!.RequestAborted);
        if (hasAccess) context.Succeed(requirement);
    }
}
