using System.Text;
using System.Text.Json;
using Halisaha.Application.Identity;
using Halisaha.Application.Availability;
using Halisaha.Application.Reservations;
using Halisaha.Application.Payments;
using Halisaha.Infrastructure.Availability;
using Halisaha.Infrastructure.Identity;
using Halisaha.Infrastructure.Persistence;
using Halisaha.Infrastructure.Payments;
using Halisaha.Infrastructure.Reservations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Halisaha.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Halisaha")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Halisaha is required. Configure it with .NET User Secrets.");

        services.AddDbContext<HalisahaDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(HalisahaDbContext).Assembly.FullName)));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<HalisahaDbContext>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                options => Encoding.UTF8.GetByteCount(options.SigningKey) >= 32,
                $"{JwtOptions.SectionName}:SigningKey must be at least 32 bytes and configured with .NET User Secrets.")
            .ValidateOnStart();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddOptions<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
            {
                var jwtOptions = jwtOptionsAccessor.Value;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await WriteAuthenticationProblemAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            "Oturum geçersiz veya süresi dolmuş.");
                    },
                    OnForbidden = context => WriteAuthenticationProblemAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        "Bu işlem için yetkiniz bulunmuyor.")
                };
            });
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthRoles.Customer, policy => policy.RequireRole(AuthRoles.Customer))
            .AddPolicy(AuthRoles.BusinessOwner, policy => policy.RequireRole(AuthRoles.BusinessOwner))
            .AddPolicy(AuthRoles.BusinessStaff, policy => policy.RequireRole(AuthRoles.BusinessStaff))
            .AddPolicy(AuthRoles.SystemAdmin, policy => policy.RequireRole(AuthRoles.SystemAdmin))
            .AddPolicy(AuthPolicies.BusinessAccess, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new BusinessAccessRequirement());
            });

        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthorizationHandler, BusinessAccessHandler>();
        services.AddScoped<TokenIssuer>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAvailabilitySearchService, AvailabilitySearchService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IPaymentGateway, FakeSandboxPaymentGateway>();
        services.AddScoped<DevelopmentDataSeeder>();

        return services;
    }

    private static async Task WriteAuthenticationProblemAsync(HttpContext context, int status, string title)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new ProblemDetails
            {
                Status = status,
                Title = title,
                Extensions = { ["correlationId"] = context.TraceIdentifier }
            },
            cancellationToken: context.RequestAborted);
    }
}
