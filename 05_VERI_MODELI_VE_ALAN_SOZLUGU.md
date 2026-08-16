# Veri Modeli ve Alan Sözlüğü

## Uygulanan kümeler

- Identity: Users, Roles, UserRoles, RefreshTokens.
- Tenant: Businesses, BusinessMembers.
- Catalog: Facilities, Fields, Amenities, FieldAmenities, FacilityImages.
- Schedule: WeeklyHours, SpecialHours, FieldBlocks, PriceRules.
- Reservation: Reservations, ReservationSlots, IdempotencyRecords.
- Payment: Payments, PaymentEvents, Refunds.
- Integration: OutboxMessages, NotificationDeliveries.
- Governance: AuditLogs.

## Ortak alan kuralları

- Kimlikler `uniqueidentifier`; dışarıdan gelen kimlik doğrulanır.
- Zaman damgaları `datetime2` UTC; yerel saat kalıcı karar kaynağı değildir.
- Para `decimal(18,2)` ve `char(3)` currency.
- Değişebilir aggregate köklerinde `rowversion`.
- FK ve sorgu desenlerine uygun index zorunludur.
- İş kayıtları hard delete yerine durum/soft-delete ve audit kullanır.

ReservationSlot aktifliği SQL filtered unique index ile `FieldId + SlotStartUtc` üzerinde korunur. Kesin kolonlar ilgili fazın migration planında onaylanır.

## `CreateCoreSchemaAndIdentity` migrationı

`20260816090501_CreateCoreSchemaAndIdentity` migrationı 28 iş/identity tablosunu oluşturur. ASP.NET Core Identity gereği planlanan identity tablolarına `UserClaims`, `UserLogins`, `RoleClaims` ve `UserTokens` da eklenmiştir. `Customer`, `BusinessOwner`, `BusinessStaff` ve `SystemAdmin` rolleri deterministik kimliklerle seed edilir.

- `Users`: e-posta ve telefon nullable fakat ayrı filtered unique indexlidir; parola yalnız Identity `PasswordHash` alanında tutulur.
- `RefreshTokens`: ham token tutulmaz; `char(64)` SHA-256 hash, süre, iptal ve rotation zinciri saklanır.
- `BusinessMembers`: `BusinessId + UserId` unique; aktif üyelik ve işletme rolü server-side tenant kararının kaynağıdır.
- `Reservations`: kullanıcı, işletme, tesis ve saha FK'leri; UTC aralık, fiyat ve iptal politikası snapshotları içerir.
- `ReservationSlots`: aktif `FieldId + SlotStartUtc` için `IX_ReservationSlots_FieldId_SlotStartUtc` filtered unique indexine sahiptir.
- `PaymentEvents`: `Provider + ProviderEventId`; `IdempotencyRecords`: `UserId + Key` unique tutulur.
- Değişebilir aggregate köklerinde SQL Server `rowversion`; para alanlarında `decimal(18,2)` kullanılır.

Migration `Up` tarafında mevcut tablo/kolon silmez veya değiştirmez. `Down` yalnız açık rollback istendiğinde bu migrationın oluşturduğu tabloları bağımlılık sırasıyla kaldırır.
