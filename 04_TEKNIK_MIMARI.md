# Teknik Mimari

Sistem .NET 10 modüler monolit ve React TypeScript istemcisidir. Katman bağımlılığı `Api → Infrastructure → Application → Domain`; Domain framework ve I/O bağımlılığı taşımaz.

## Modüller

Identity, Catalog, Availability, Reservations, Payments, Notifications, Reporting ve Administration aynı deployment içinde açık application kontratlarıyla ayrılır.

## Veri ve entegrasyon

- EF Core Code First ve SQL Server.
- Tüm kalıcı zamanlar UTC; tesisin saat dilimi ayrıca saklanır.
- Transactional outbox, webhook event tekilliği ve kullanıcı kapsamlı idempotency.
- Sağlayıcılar interface arkasında; geliştirmede fake/sandbox.
- React server state için tek API veri katmanı kullanır; SQL/ödeme sağlayıcısına doğrudan erişmez.

## Çapraz kesen kurallar

RFC 7807, `X-Correlation-Id`, yapılandırılmış ve PII maskeli log, health checks, rate limit ve tenant authorization zorunludur.
