using Halisaha.Application.Identity;
using Halisaha.Domain.Catalog;
using Halisaha.Domain.Governance;
using Halisaha.Domain.Integration;
using Halisaha.Domain.Payments;
using Halisaha.Domain.Reservations;
using Halisaha.Domain.Scheduling;
using Halisaha.Domain.Tenancy;
using Halisaha.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Halisaha.Infrastructure.Persistence;

internal static class HalisahaModelConfiguration
{
    public static void ConfigureHalisahaModel(this ModelBuilder builder)
    {
        ConfigureIdentity(builder);
        ConfigureTenancy(builder);
        ConfigureCatalog(builder);
        ConfigureSchedule(builder);
        ConfigureReservations(builder);
        ConfigurePayments(builder);
        ConfigureIntegration(builder);
        ConfigureGovernance(builder);
    }

    private static void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Status).HasConversion<string>().HasMaxLength(24);
            entity.Property(user => user.CreatedAtUtc).HasColumnType("datetime2");
            entity.Property(user => user.UpdatedAtUtc).HasColumnType("datetime2");
            entity.Property(user => user.LastLoginAtUtc).HasColumnType("datetime2");
            entity.Property(user => user.RowVersion).IsRowVersion();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
            entity.HasIndex(user => user.PhoneNumber).IsUnique().HasFilter("[PhoneNumber] IS NOT NULL");
        });

        var roles = new[]
        {
            CreateRole("72c7f3aa-c6ef-41cb-9d4a-65e814cb6310", AuthRoles.Customer),
            CreateRole("0d2207f6-e7ea-4b19-bfb1-e8f185db6c37", AuthRoles.BusinessOwner),
            CreateRole("952f4d94-9a53-4a96-a4c3-6404ef1c8283", AuthRoles.BusinessStaff),
            CreateRole("1816f9b7-bf5e-4216-910c-5849b2fd5d87", AuthRoles.SystemAdmin)
        };
        builder.Entity<ApplicationRole>().HasData(roles);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasColumnType("char(64)").IsRequired();
            entity.Property(token => token.RevocationReason).HasMaxLength(100);
            entity.Property(token => token.CreatedAtUtc).HasColumnType("datetime2");
            entity.Property(token => token.ExpiresAtUtc).HasColumnType("datetime2");
            entity.Property(token => token.RevokedAtUtc).HasColumnType("datetime2");
            entity.Property(token => token.RowVersion).IsRowVersion();
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.ExpiresAtUtc });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<RefreshToken>().WithMany().HasForeignKey(token => token.ReplacedByTokenId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTenancy(ModelBuilder builder)
    {
        builder.Entity<Business>(entity =>
        {
            entity.ToTable("Businesses");
            ConfigureAudited(entity);
            entity.Property(business => business.Name).HasMaxLength(160).IsRequired();
            entity.Property(business => business.LegalName).HasMaxLength(200);
            entity.Property(business => business.Status).HasConversion<string>().HasMaxLength(24);
            entity.Property(business => business.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.HasIndex(business => business.Status);
        });

        builder.Entity<BusinessMember>(entity =>
        {
            entity.ToTable("BusinessMembers");
            entity.HasKey(member => member.Id);
            entity.Property(member => member.Role).HasConversion<string>().HasMaxLength(32);
            entity.Property(member => member.CreatedAtUtc).HasColumnType("datetime2");
            entity.HasIndex(member => new { member.BusinessId, member.UserId }).IsUnique();
            entity.HasIndex(member => new { member.UserId, member.IsActive });
            entity.HasOne<Business>().WithMany().HasForeignKey(member => member.BusinessId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCatalog(ModelBuilder builder)
    {
        builder.Entity<Facility>(entity =>
        {
            entity.ToTable("Facilities");
            ConfigureAudited(entity);
            entity.Property(facility => facility.Name).HasMaxLength(160).IsRequired();
            entity.Property(facility => facility.Description).HasMaxLength(2000);
            entity.Property(facility => facility.City).HasMaxLength(100).IsRequired();
            entity.Property(facility => facility.District).HasMaxLength(100).IsRequired();
            entity.Property(facility => facility.AddressLine).HasMaxLength(500).IsRequired();
            entity.Property(facility => facility.Latitude).HasPrecision(9, 6);
            entity.Property(facility => facility.Longitude).HasPrecision(9, 6);
            entity.Property(facility => facility.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.Property(facility => facility.Status).HasConversion<string>().HasMaxLength(24);
            entity.HasIndex(facility => new { facility.City, facility.District, facility.Status });
            entity.HasIndex(facility => facility.BusinessId);
            entity.HasOne<Business>().WithMany().HasForeignKey(facility => facility.BusinessId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Field>(entity =>
        {
            entity.ToTable("Fields");
            ConfigureAudited(entity);
            entity.Property(field => field.Name).HasMaxLength(120).IsRequired();
            entity.Property(field => field.Description).HasMaxLength(2000);
            entity.Property(field => field.SurfaceType).HasMaxLength(60).IsRequired();
            entity.Property(field => field.Status).HasConversion<string>().HasMaxLength(24);
            entity.HasIndex(field => new { field.FacilityId, field.Status });
            entity.HasOne<Facility>().WithMany().HasForeignKey(field => field.FacilityId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Amenity>(entity =>
        {
            entity.ToTable("Amenities");
            entity.HasKey(amenity => amenity.Id);
            entity.Property(amenity => amenity.Name).HasMaxLength(100).IsRequired();
            entity.Property(amenity => amenity.Slug).HasMaxLength(100).IsRequired();
            entity.HasIndex(amenity => amenity.Slug).IsUnique();
        });

        builder.Entity<FieldAmenity>(entity =>
        {
            entity.ToTable("FieldAmenities");
            entity.HasKey(item => new { item.FieldId, item.AmenityId });
            entity.HasOne<Field>().WithMany().HasForeignKey(item => item.FieldId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Amenity>().WithMany().HasForeignKey(item => item.AmenityId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FacilityImage>(entity =>
        {
            entity.ToTable("FacilityImages");
            entity.HasKey(image => image.Id);
            entity.Property(image => image.StorageKey).HasMaxLength(500).IsRequired();
            entity.Property(image => image.AltText).HasMaxLength(200).IsRequired();
            entity.Property(image => image.CreatedAtUtc).HasColumnType("datetime2");
            entity.HasIndex(image => new { image.FacilityId, image.SortOrder });
            entity.HasOne<Facility>().WithMany().HasForeignKey(image => image.FacilityId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSchedule(ModelBuilder builder)
    {
        builder.Entity<WeeklyHours>(entity =>
        {
            entity.ToTable("WeeklyHours");
            entity.HasKey(hours => hours.Id);
            entity.Property(hours => hours.DayOfWeek).HasConversion<byte>();
            entity.Property(hours => hours.OpensAt).HasColumnType("time(0)");
            entity.Property(hours => hours.ClosesAt).HasColumnType("time(0)");
            entity.HasIndex(hours => new { hours.FacilityId, hours.DayOfWeek }).IsUnique();
            entity.HasOne<Facility>().WithMany().HasForeignKey(hours => hours.FacilityId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SpecialHours>(entity =>
        {
            entity.ToTable("SpecialHours");
            entity.HasKey(hours => hours.Id);
            entity.Property(hours => hours.LocalDate).HasColumnType("date");
            entity.Property(hours => hours.OpensAt).HasColumnType("time(0)");
            entity.Property(hours => hours.ClosesAt).HasColumnType("time(0)");
            entity.Property(hours => hours.Reason).HasMaxLength(250);
            entity.HasIndex(hours => new { hours.FacilityId, hours.LocalDate }).IsUnique();
            entity.HasOne<Facility>().WithMany().HasForeignKey(hours => hours.FacilityId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FieldBlock>(entity =>
        {
            entity.ToTable("FieldBlocks", table => table.HasCheckConstraint("CK_FieldBlocks_TimeRange", "[StartsAtUtc] < [EndsAtUtc]"));
            ConfigureAudited(entity);
            entity.Property(block => block.StartsAtUtc).HasColumnType("datetime2");
            entity.Property(block => block.EndsAtUtc).HasColumnType("datetime2");
            entity.Property(block => block.Reason).HasMaxLength(250).IsRequired();
            entity.Property(block => block.Status).HasConversion<string>().HasMaxLength(24);
            entity.HasIndex(block => new { block.FieldId, block.StartsAtUtc, block.EndsAtUtc });
            entity.HasOne<Field>().WithMany().HasForeignKey(block => block.FieldId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PriceRule>(entity =>
        {
            entity.ToTable("PriceRules", table => table.HasCheckConstraint("CK_PriceRules_Price", "[Price] >= 0"));
            ConfigureAudited(entity);
            entity.Property(rule => rule.Name).HasMaxLength(120).IsRequired();
            entity.Property(rule => rule.DayOfWeek).HasConversion<byte?>();
            entity.Property(rule => rule.StartsAt).HasColumnType("time(0)");
            entity.Property(rule => rule.EndsAt).HasColumnType("time(0)");
            entity.Property(rule => rule.EffectiveFrom).HasColumnType("date");
            entity.Property(rule => rule.EffectiveTo).HasColumnType("date");
            entity.Property(rule => rule.Price).HasPrecision(18, 2);
            entity.Property(rule => rule.Currency).HasColumnType("char(3)");
            entity.HasIndex(rule => new { rule.FieldId, rule.IsActive, rule.EffectiveFrom });
            entity.HasOne<Field>().WithMany().HasForeignKey(rule => rule.FieldId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureReservations(ModelBuilder builder)
    {
        builder.Entity<Reservation>(entity =>
        {
            entity.ToTable("Reservations", table => table.HasCheckConstraint("CK_Reservations_TimeRange", "[StartsAtUtc] < [EndsAtUtc]"));
            ConfigureAudited(entity);
            entity.Property(reservation => reservation.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(reservation => reservation.StartsAtUtc).HasColumnType("datetime2");
            entity.Property(reservation => reservation.EndsAtUtc).HasColumnType("datetime2");
            entity.Property(reservation => reservation.ExpiresAtUtc).HasColumnType("datetime2");
            entity.Property(reservation => reservation.CancelledAtUtc).HasColumnType("datetime2");
            entity.Property(reservation => reservation.Subtotal).HasPrecision(18, 2);
            entity.Property(reservation => reservation.Total).HasPrecision(18, 2);
            entity.Property(reservation => reservation.Currency).HasColumnType("char(3)");
            entity.Property(reservation => reservation.PriceSnapshotJson).HasColumnType("nvarchar(max)");
            entity.Property(reservation => reservation.CancellationPolicySnapshotJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(reservation => new { reservation.UserId, reservation.CreatedAtUtc });
            entity.HasIndex(reservation => new { reservation.BusinessId, reservation.StartsAtUtc });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(reservation => reservation.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Business>().WithMany().HasForeignKey(reservation => reservation.BusinessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Facility>().WithMany().HasForeignKey(reservation => reservation.FacilityId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Field>().WithMany().HasForeignKey(reservation => reservation.FieldId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ReservationSlot>(entity =>
        {
            entity.ToTable("ReservationSlots", table => table.HasCheckConstraint("CK_ReservationSlots_TimeRange", "[SlotStartUtc] < [SlotEndUtc]"));
            entity.HasKey(slot => slot.Id);
            entity.Property(slot => slot.SlotStartUtc).HasColumnType("datetime2");
            entity.Property(slot => slot.SlotEndUtc).HasColumnType("datetime2");
            entity.Property(slot => slot.RowVersion).IsRowVersion();
            entity.HasIndex(slot => new { slot.FieldId, slot.SlotStartUtc })
                .IsUnique()
                .HasFilter("[IsActive] = 1");
            entity.HasIndex(slot => slot.ReservationId);
            entity.HasOne<Reservation>().WithMany().HasForeignKey(slot => slot.ReservationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Field>().WithMany().HasForeignKey(slot => slot.FieldId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("IdempotencyRecords");
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Key).HasMaxLength(100).IsRequired();
            entity.Property(record => record.RequestHash).HasColumnType("char(64)").IsRequired();
            entity.Property(record => record.ResponseBodyJson).HasColumnType("nvarchar(max)");
            entity.Property(record => record.CreatedAtUtc).HasColumnType("datetime2");
            entity.Property(record => record.ExpiresAtUtc).HasColumnType("datetime2");
            entity.HasIndex(record => new { record.UserId, record.Key }).IsUnique();
            entity.HasIndex(record => record.ExpiresAtUtc);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(record => record.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePayments(ModelBuilder builder)
    {
        builder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments", table => table.HasCheckConstraint("CK_Payments_Amount", "[Amount] >= 0"));
            ConfigureAudited(entity);
            entity.Property(payment => payment.Provider).HasMaxLength(60).IsRequired();
            entity.Property(payment => payment.ProviderPaymentId).HasMaxLength(200);
            entity.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(24);
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.Property(payment => payment.Currency).HasColumnType("char(3)");
            entity.Property(payment => payment.CapturedAtUtc).HasColumnType("datetime2");
            entity.HasIndex(payment => payment.ReservationId);
            entity.HasIndex(payment => new { payment.Provider, payment.ProviderPaymentId }).IsUnique().HasFilter("[ProviderPaymentId] IS NOT NULL");
            entity.HasOne<Reservation>().WithMany().HasForeignKey(payment => payment.ReservationId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentEvent>(entity =>
        {
            entity.ToTable("PaymentEvents");
            entity.HasKey(paymentEvent => paymentEvent.Id);
            entity.Property(paymentEvent => paymentEvent.Provider).HasMaxLength(60).IsRequired();
            entity.Property(paymentEvent => paymentEvent.ProviderEventId).HasMaxLength(200).IsRequired();
            entity.Property(paymentEvent => paymentEvent.EventType).HasMaxLength(100).IsRequired();
            entity.Property(paymentEvent => paymentEvent.PayloadHash).HasColumnType("char(64)").IsRequired();
            entity.Property(paymentEvent => paymentEvent.Status).HasConversion<string>().HasMaxLength(24);
            entity.Property(paymentEvent => paymentEvent.ReceivedAtUtc).HasColumnType("datetime2");
            entity.Property(paymentEvent => paymentEvent.ProcessedAtUtc).HasColumnType("datetime2");
            entity.Property(paymentEvent => paymentEvent.ErrorCode).HasMaxLength(100);
            entity.HasIndex(paymentEvent => new { paymentEvent.Provider, paymentEvent.ProviderEventId }).IsUnique();
            entity.HasOne<Payment>().WithMany().HasForeignKey(paymentEvent => paymentEvent.PaymentId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Refund>(entity =>
        {
            entity.ToTable("Refunds", table => table.HasCheckConstraint("CK_Refunds_Amount", "[Amount] >= 0"));
            ConfigureAudited(entity);
            entity.Property(refund => refund.ProviderRefundId).HasMaxLength(200);
            entity.Property(refund => refund.Status).HasConversion<string>().HasMaxLength(24);
            entity.Property(refund => refund.Amount).HasPrecision(18, 2);
            entity.Property(refund => refund.Currency).HasColumnType("char(3)");
            entity.Property(refund => refund.Reason).HasMaxLength(250).IsRequired();
            entity.HasIndex(refund => refund.PaymentId);
            entity.HasIndex(refund => refund.ReservationId);
            entity.HasOne<Payment>().WithMany().HasForeignKey(refund => refund.PaymentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Reservation>().WithMany().HasForeignKey(refund => refund.ReservationId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureIntegration(ModelBuilder builder)
    {
        builder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.MessageType).HasMaxLength(200).IsRequired();
            entity.Property(message => message.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(message => message.Status).HasConversion<string>().HasMaxLength(24);
            entity.Property(message => message.OccurredAtUtc).HasColumnType("datetime2");
            entity.Property(message => message.ProcessedAtUtc).HasColumnType("datetime2");
            entity.Property(message => message.NextAttemptAtUtc).HasColumnType("datetime2");
            entity.Property(message => message.LastErrorCode).HasMaxLength(100);
            entity.Property(message => message.RowVersion).IsRowVersion();
            entity.HasIndex(message => new { message.Status, message.NextAttemptAtUtc });
        });

        builder.Entity<NotificationDelivery>(entity =>
        {
            entity.ToTable("NotificationDeliveries");
            entity.HasKey(delivery => delivery.Id);
            entity.Property(delivery => delivery.Channel).HasConversion<string>().HasMaxLength(16);
            entity.Property(delivery => delivery.DestinationHash).HasColumnType("char(64)").IsRequired();
            entity.Property(delivery => delivery.TemplateName).HasMaxLength(120).IsRequired();
            entity.Property(delivery => delivery.Status).HasConversion<string>().HasMaxLength(24);
            entity.Property(delivery => delivery.CreatedAtUtc).HasColumnType("datetime2");
            entity.Property(delivery => delivery.SentAtUtc).HasColumnType("datetime2");
            entity.HasIndex(delivery => new { delivery.Status, delivery.CreatedAtUtc });
            entity.HasOne<OutboxMessage>().WithMany().HasForeignKey(delivery => delivery.OutboxMessageId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(delivery => delivery.UserId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureGovernance(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.Action).HasMaxLength(120).IsRequired();
            entity.Property(audit => audit.EntityType).HasMaxLength(120).IsRequired();
            entity.Property(audit => audit.EntityId).HasMaxLength(100);
            entity.Property(audit => audit.Outcome).HasMaxLength(32).IsRequired();
            entity.Property(audit => audit.CorrelationId).HasMaxLength(64);
            entity.Property(audit => audit.IpAddressHash).HasColumnType("char(64)");
            entity.Property(audit => audit.MetadataJson).HasColumnType("nvarchar(max)");
            entity.Property(audit => audit.OccurredAtUtc).HasColumnType("datetime2");
            entity.HasIndex(audit => new { audit.UserId, audit.OccurredAtUtc });
            entity.HasIndex(audit => new { audit.BusinessId, audit.OccurredAtUtc });
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(audit => audit.UserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Business>().WithMany().HasForeignKey(audit => audit.BusinessId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static ApplicationRole CreateRole(string id, string name) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = id
    };

    private static void ConfigureAudited<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : Domain.Common.AuditedEntity
    {
        entity.HasKey(item => item.Id);
        entity.Property(item => item.CreatedAtUtc).HasColumnType("datetime2");
        entity.Property(item => item.UpdatedAtUtc).HasColumnType("datetime2");
        entity.Property(item => item.RowVersion).IsRowVersion();
    }
}
