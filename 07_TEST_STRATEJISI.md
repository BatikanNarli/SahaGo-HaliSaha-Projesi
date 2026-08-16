# Test Stratejisi

## Katmanlar

- Unit: domain durum geçişleri, fiyat, iptal/iade ve saat dilimi kuralları.
- Integration: gerçek SQL Server üzerinde EF/migration, transaction, unique index, tenant ve webhook.
- Component: React form şemaları ve loading/empty/error/success durumları.
- E2E: kayıt → arama → rezervasyon → fake ödeme → iptal akışı.

## Zorunlu kritik testler

- 50 eşzamanlı aynı-slot isteğinde en fazla bir başarı.
- Aynı idempotency anahtarında aynı sonuç.
- Yetkisiz ve farklı işletme erişiminin reddi.
- Çift webhook olayında tek işleme.
- Refund hatasında rezervasyon/audit geçmişinin korunması.
- DST geçişlerinde invalid/ambiguous yerel saat davranışı.
- Temiz veritabanına migration ve ileri script incelemesi.
- OpenAPI ile frontend client uyumu ve secret/PII log taraması.

Test adı davranışı açıklar; kritik kurallar yalnız mock etkileşimiyle kanıtlanmaz.
