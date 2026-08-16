using Halisaha.Domain.Catalog;
using Halisaha.Domain.Governance;
using Halisaha.Domain.Integration;
using Halisaha.Domain.Payments;
using Halisaha.Domain.Reservations;
using Halisaha.Domain.Scheduling;
using Halisaha.Domain.Tenancy;
using Halisaha.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Halisaha.Infrastructure.Persistence;

public sealed class HalisahaDbContext(DbContextOptions<HalisahaDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessMember> BusinessMembers => Set<BusinessMember>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<FieldAmenity> FieldAmenities => Set<FieldAmenity>();
    public DbSet<FacilityImage> FacilityImages => Set<FacilityImage>();
    public DbSet<WeeklyHours> WeeklyHours => Set<WeeklyHours>();
    public DbSet<SpecialHours> SpecialHours => Set<SpecialHours>();
    public DbSet<FieldBlock> FieldBlocks => Set<FieldBlock>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationSlot> ReservationSlots => Set<ReservationSlot>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ConfigureHalisahaModel();

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
    }
}
