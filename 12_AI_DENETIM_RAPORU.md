# AI Denetim Raporu

Her faz sonunda aşağıdaki kanıtlar doldurulur:

- İncelenen `REQ-*`, `US-*`, `T-*` kayıtları.
- Değişen dosyalar ve migration SQL özeti.
- Gerçek build/lint/type-check/unit/integration/E2E çıktıları.
- Auth/tenant, concurrency, idempotency, timezone ve PII/secret bulguları.
- Critical/High/Medium/Low bulgular ve revizyon durumu.
- Kapsam dışında bırakılanlar ve insan kararları.

Test edilmemiş davranış başarılı olarak yazılamaz; mevcut olmayan endpoint/tablo/paket uydurulamaz.

## 2026-08-16 — Faz 0 denetimi

- Kapsam: `REQ-NFR-003/005`, Faz 0 iskelet ve migration temeli.
- Build: .NET 10 solution, 0 uyarı/0 hata.
- Test: backend unit 1/1, integration 1/1; frontend Vitest 1/1.
- Frontend: ESLint, strict type-check ve production build başarılı.
- Güvenlik: repo secret pattern taraması temiz; NuGet ve npm bilinen açık taraması temiz.
- Migration: `InitialFoundation` Up/Down boş; script yalnız `__EFMigrationsHistory` oluşturuyor. Drop/alter/veri kaybı yok.
- `High`: Modern `Microsoft.Data.SqlClient`, yerel SQL Server ile TLS el sıkışması kuramıyor. Migration uygulanmadı ve readiness health doğrulanamadı. `Encrypt=False` kullanılmadı.
- Sonuç: Faz 0 kalite kapısı reddedildi; TLS/SQL istemci uyumu çözülüp migration ve ready health doğrulanmalı.

## 2026-08-16 — Faz 0 denetimi revizyon 1

- SQL Server 2022 RTM, Microsoft CU26 (`16.0.4265.3`, KB5093420) seviyesine yükseltildi; paket SHA-256 değeri Microsoft kaydıyla eşleşti.
- TCP yalnız `127.0.0.1` ve `::1` üzerinde 1433 portunda etkinleştirildi; dış firewall kuralı açılmadı.
- `InitialFoundation` migrationı exclusive EF migration lock ile uygulandı; ikinci uygulama “database already up to date” verdi.
- Şema: yalnız `dbo.__EFMigrationsHistory`; migration kaydı `20260816075928_InitialFoundation / 10.0.11`.
- Geçici loopback bağlantısıyla live/ready health ve OpenAPI 200; correlation id mevcut.
- Backend build 0 uyarı/0 hata; unit 1/1 ve integration 1/1. Frontend lint/type-check, Vitest 1/1, build ve npm audit geçti.
- `High`: CU installer `3010` ve Windows `PendingFileRenameOperations=True`. Kalıcı User Secrets `Encrypt=True`; modern SqlClient TLS doğrulaması reboot öncesi hâlâ başarısız. Reboot sonrası yeniden test zorunlu.
- `Low`: Çalışan iki Node süreci nedeniyle `npm ci` native Rolldown dosyasını temizleyemedi; `npm install` bağımlılıkları geri yükledi ve tüm frontend kapıları geçti. Reboot sonrası temiz `npm ci` tekrar çalıştırılmalı.
- Sonuç: Migration kapısı geçti; Faz 0 nihai kabulü reboot sonrası şifreli readiness ve temiz `npm ci` doğrulamasına bağlı.

## 2026-08-16 — Faz 0 frontend denetimi revizyon 2

- Kapsam: `REQ-NFR-003/005` ile `US-CUST-*`, `US-BIZ-*` ve `US-ADM-*` akışlarının gerçek API'lere bağlanmadan önceki erişilebilir uygulama kabuğu.
- Mimari: Router tek rota ağacında, server state TanStack Query'de, HTTP çağrısı `src/lib/api.ts` sınırında, form şemaları Zod/React Hook Form'da tutuluyor. Controller/tenant davranışı istemciye taşınmadı.
- Test: `renders the customer-first landing page`, `shows a clear degraded state when the API health check fails`, `validates and summarizes search criteria`, `does not pretend to authenticate before the identity API exists`, `renders an accessible not-found route`; 5/5 geçti.
- Statik kapılar: ESLint başarılı, TypeScript strict type-check başarılı, production build başarılı; 164 modül, JS 420,48 kB (gzip 130,53 kB), CSS 13,87 kB (gzip 3,89 kB).
- Güvenlik: `npm audit --audit-level=high` sonucu 0 açık. Frontend içinde connection string, SQL kullanıcısı/parolası, token saklama veya istemciye güvenilen kullanıcı/işletme kimliği yok.
- Tarayıcı QA: masaüstü ana sayfa, readiness, arama hata/başarı durumu; 390×844 mobil yerleşim ve menü doğrulandı. Konsolda warning/error yok.
- `Critical/High/Medium`: bulgu yok.
- `Low`: Üretim paketi henüz rota bazlı bölünmüyor; gerçek ekran sayısı büyüdüğünde lazy route/code splitting izlenmeli. API kapatılırken aktif health isteğinin iptali sandbox Windows Event Log izin uyarısı üretti; normal readiness yanıtı ve istemci davranışı etkilenmedi, backend gözlemleme fazında yeniden ele alınmalı.
- Sonuç: Faz 0 frontend dikey dilimi kabul edildi. Kimlik, gerçek tesis/uygunluk, rezervasyon ve ödeme davranışları backend Faz 1–5 tamamlanmadan başarılı gösterilmiyor.

## 2026-08-16 — Faz 1 kimlik/veri denetimi

- Kapsam: `REQ-AUTH-001/002/003`, `REQ-TENANT-001`, `REQ-AUD-001`, `REQ-NFR-001/003/005`; kayıt/giriş/refresh/logout, rol/tenant temeli ve planlanan SQL tabloları.
- Migration: `CreateCoreSchemaAndIdentity` Up tarafında 28 yeni tablo, 47 index, FK/check constraint ve dört rol seed eder; drop/alter/veri silme yok. İkinci uygulama idempotent kaldı.
- SQL kanıtı: gerçek `Halisahadb` üzerinde beklenen 29 tablo, iki migration, dört rol ve unique `ReservationSlots(FieldId, SlotStartUtc) WHERE IsActive=1` doğrulandı.
- Auth güvenliği: Identity password hash; public register Customer-only; genel login hatası; 5 denemede 15 dakika lockout; 15 dakikalık JWT; ham refresh token yalnız HttpOnly/SameSite cookie, SQL'de SHA-256 hash; rotation/reuse iptali; dakikada 10 auth isteği rate limit.
- Tenant: `BusinessAccess` policy route `businessId`, JWT user id/rolü ve aktif `BusinessMember` rolünü birlikte doğrular. Test başka işletme erişimini reddetti.
- Testler: backend unit 4/4; integration 6/6 (`register/duplicate`, `wrong password/JWT me`, `refresh/logout`, `own/other tenant`, live health, gerçek SQL schema). Frontend Vitest 7/7; lint, strict type-check ve build geçti.
- E2E: gerçek SQL üzerinde test hesabı oluşturuldu; frontend login, refresh sonrası reload ve logout çalıştı. Ready/OpenAPI 200; duplicate register 409; browser warning/error yok.
- Güvenlik taraması: NuGet/npm bilinen açık yok; repo içinde SQL credential/JWT signing key eşleşmesi yok.
- `Critical/High`: yeni kod kapsamında bulgu yok.
- `Medium`: parola sıfırlama ve e-posta/telefon sahiplik doğrulaması uygulanmadı; bu nedenle Faz 1 bütünü kapanmış sayılmaz.
- `High` ortam riski: kalıcı `Encrypt=True` modern SqlClient bağlantısı TLS katmanında hâlâ başarısız; şema ve E2E yalnız loopback geçici `Encrypt=False` süreç override ile doğrulandı.
- `Low`: sandbox koşusunda ASP.NET Data Protection anahtar dizinine yazma izin uyarısı oluşuyor; JWT/refresh akışını etkilemiyor, production key persistence stratejisi release öncesi ayrıca yapılandırılmalı.
- Sonuç: Kullanıcı kayıt/giriş ve SQL şema dilimi kabul edildi; Faz 1 nihai çıkışı parola sıfırlama/iletişim doğrulaması ve kalıcı şifreli SQL bağlantısına bağlı.

## 2026-08-16 — Telefon kayıt düzeltmesi denetimi

- Kapsam: `REQ-AUTH-001`, `REQ-NFR-003/005`; telefon girişi, frontend–API geliştirme bağlantısı, telefon-only kayıt ve hata yanıtları.
- Kök nedenler: API launch profili `5134`, Vite proxy/API belgesi `5198` bekliyordu. Telefon-only kayıtta üretilen `phone:+90...` kullanıcı adı Identity'nin izin vermediği `:` karakterini içeriyordu. Rate-limit'in boş `429` yanıtı frontend'de genel mesaja düşüyordu.
- Çözüm: Launch profili `5198` ile eşlendi; UI yerel numarayı E.164'e normalize ediyor; telefon E.164 değeri güvenli Identity kullanıcı adı olarak kullanılıyor; validation/429/5xx mesajları somutlaştırıldı ve `429` RFC 7807 içerik türüyle dönüyor.
- Kanıt: `RegisterAndLoginAcceptPhoneWithoutAnEmailAddress`, `AuthRateLimitReturnsProblemDetailsInsteadOfAnEmptyResponse` ve frontend telefon normalizasyon/hata testleri geçti. Toplam backend unit 4/4, integration 7/7; frontend 9/9, lint/type-check/build başarılı. Vite proxy → gerçek API → SQL telefon-only canlı kayıt `201` verdi.
- `Critical/High/Medium`: yeni kod kapsamında bulgu yok.
- `Low`: Ülke seçici yok; mevcut ürün Türkiye/TRY odaklı olduğu için yalnız `+90` mobil numara kabul ediliyor. Çok ülke desteği kapsam kararı gerektirir.
- Sonuç: Bildirilen kayıt hatası giderildi ve dikey dilim kabul edildi.

## 2026-08-16 — Uygun saha arama denetimi

- Kapsam: `REQ-CAT-001`, `REQ-AVL-001`, `REQ-PRICE-001`, `US-CUS-001`, `REQ-NFR-003/005`; development veri seed'i, availability API ve React sonuçları.
- Veri sınırı: Seed yalnız Development config ile açık, production config'te kapalı; PII/secret yok ve deterministik Business kimliği üzerinden idempotent. Yeni migration yok; mevcut tablo/FK/indexler kullanılıyor.
- Uygunluk: Approved/Published filtreleri, tesis yerel saatinden UTC dönüşüm, weekly/special hours, price rule, aktif block ve reservation slot overlap kontrolleri application kontratı arkasındaki Infrastructure servisinde. SQL listeleri 100 aday saha ve arama zamanı çevresindeki UTC pencereyle sınırlı; salt okumalar `AsNoTracking`.
- API/güvenlik: Public GET, alan bazlı RFC 7807/correlation ID ve dakikada 60 IP rate limit. İstemciden fiyat veya işletme kimliği kabul edilmiyor.
- Kanıt: `SearchReturnsPricedPublishedFieldsFromApprovedBusiness`, `ActiveFieldBlockRemovesOverlappingFieldFromSearch`, `InvalidDurationReturnsProblemDetailsWithFieldError` geçti. Backend unit 4/4, integration 11/11 ve gerçek SQL şema testi; frontend 12/12, lint/type-check/build başarılı. Kalıcı şifreli SQL bağlantısıyla direct/proxy API `200` ve gerçek browser iki kart gösterdi.
- `Critical/High`: bulgu yok.
- `Medium`: Arama ile rezervasyon tahsisi arasında hold/transaction henüz yok; sonuç bilgi amaçlıdır ve Faz 3 rezervasyon oluşturma tamamlanmadan kesin tahsis sayılmaz.
- `Low`: Seed fiyatları development gösterim verisidir; işletme tarafından yönetilen fiyat politikası Faz 2'de tamamlanmalıdır.
- Sonuç: Uygun saha arama dilimi kabul edildi; rezervasyon işlemi kapsam dışı bırakıldı.

## 2026-08-16 — Rezervasyon oluşturma, takip ve iptal denetimi

- Kapsam: `REQ-RES-001/002/003`, `REQ-PAY-001`, `REQ-AUD-001`, `US-CUS-002/003`; müşterinin saha/tarih/saat seçerek rezervasyon oluşturması, kendi rezervasyonlarını listelemesi ve maç başlamadan iptal etmesi.
- Veri bütünlüğü: Fiyat, işletme ve kullanıcı kimliği sunucuda türetiliyor. Çakışma kontrolü ile 30 dakikalık slot tahsisi serializable transaction içinde; aktif `ReservationSlots(FieldId, SlotStartUtc)` filtered unique indexi son SQL koruması. Tekrarlanan create/cancel istekleri `Idempotency-Key` ile aynı sonucu döndürüyor.
- Yetki: Tüm rezervasyon uçları `Customer` policy ile korumalı; liste/detay/iptal sorguları JWT kullanıcı kimliğiyle sınırlandırılıyor. Başka müşterinin rezervasyonuna erişim testi `404` doğruladı.
- Ödeme/iptal: Gerçek sağlayıcı çağrısı yok; development akışı `FakeSandbox` capture/refund kayıtları üretiyor. Geçmiş veya başlamış maç iptal edilemiyor; iptal rezervasyonu/audit geçmişini silmiyor ve slotları inaktif yapıyor.
- Örnek veri: İstanbul'un 39 resmi ilçesi için development-only idempotent katalog üretildi; her ilçede bir tesis ve üç saha var. Gerçek SQL kanıtı `Districts=39`, `MinFields=3`, `PendikFields=3`.
- Kanıt: `CreateReplayListAndCancelReservation`, `OtherCustomerCannotReadOrCancelReservation`, `ReservationEndpointsRequireAuthenticationAndIdempotencyKey` ile katalog testleri geçti. Backend build 0 uyarı/0 hata; unit 4/4, integration 14/14 ve gerçek SQL şema testi 1/1. Frontend lint/type-check/build ve Vitest 14/14 geçti.
- Eşzamanlılık E2E: Gerçek SQL'e aynı saha/saat için 50 paralel create isteğinde `201=1`, `409=29`, `429=20`, `500=0`; tek kazanan doğrulandı. Deneme kayıtları geçmiş korunarak iptal/refund durumuna alındı, aktif test rezervasyonu ve slotu `0`.
- Tarayıcı QA: Pendik, 27.08.2026 20:00 için üç gerçek saha kartı; `Rezervasyonlarını takip et` sayfasının anonim giriş durumu ve sıfır browser warning/error doğrulandı. Kimlikli create/list/cancel davranışı component testi ve doğrudan gerçek HTTP/SQL E2E ile doğrulandı.
- `Critical/High`: bulgu yok.
- `Medium`: Slot hold süreci ve işletme tanımlı iptal/iade politikası henüz yok; bu dilimde onay anında tahsis ve maç başlamadan iptal varsayımı kullanılıyor.
- `Low`: Katalog/fiyatlar development örnek verisidir; işletme yönetim ekranından CRUD ayrı roadmap dilimidir. Rezervasyon rate limit'i yük testinde beklenen `429` üretti.
- Sonuç: Rezervasyon oluşturma, müşteriye özel takip ve maç öncesi iptal dikey dilimi kabul edildi.

## 2026-08-16 — Rezervasyon genel hata düzeltmesi denetimi

- Kapsam: `REQ-RES-002`, `REQ-AUTH-002`, `REQ-AVL-001`, `US-CUS-002`; access token expiry, refresh rotation, idempotent retry ve geçmiş slot sunumu.
- Kök neden: 15 dakikalık access token süresi dolduğunda React kullanıcı bilgisini açık tutuyor, rezervasyon API'sinin gövdesiz `401` yanıtını genel `400` mesajı gibi gösteriyordu. Availability ayrıca geçmiş slotu sunabildiği halde create bunu reddediyordu.
- Çözüm: Korunan istek `401` alırsa tek ortak refresh çağrısı yapılır; başarılıysa orijinal `RequestInit` ve aynı `Idempotency-Key` ile yalnız bir retry yapılır. Başarısız refresh bellekteki token/kullanıcıyı temizler. API challenge/forbidden yanıtları RFC 7807 oldu; geçmiş slotlar availability sonucundan çıkarıldı.
- Kanıt: `refreshes an expired access token and safely retries the reservation once`, `ends the stale session with a clear message when token refresh fails`, `SearchDoesNotOfferPastSlotsThatReservationWouldReject` ve reservation authentication Problem Details testi geçti. Frontend 16/16; backend unit 4/4, integration 15/15; build/lint/type-check/build kapıları geçti.
- Gerçek E2E: Ready `200`, register `201`, invalid Bearer `401 problem+json`, refresh `200`, aynı idempotency anahtarıyla create `201`, cancel `200`. Tarayıcıda geçmiş saat boş, gelecek saat saha kartları ve konsol temiz.
- Güvenlik: Retry sınırsız değil; login/register yenileme döngüsüne girmez; parola/token/connection string kaynak koda eklenmedi. Terminalde görünür olan yerel JWT signing key yeni rastgele secret ile döndürüldü.
- `Critical/High`: yeni kod kapsamında bulgu yok.
- `High` ortam riski: Kalıcı `Encrypt=True;TrustServerCertificate=True` SQL bağlantısı istemci TLS hatası verdi. Şema testi bu nedenle başarısız; API yalnız loopback süreç override `Encrypt=False` ile çalışıyor. Bu kod değişikliğinin SQL şemasına etkisi yok ve migration oluşturulmadı.
- Sonuç: Bildirilen rezervasyon hata yolu kod ve gerçek E2E açısından kabul edildi; kalıcı şifreli SQL readiness ortam kapısı ayrı açık risk olarak kaldı.

## 2026-08-16 — Rezervasyon iptal kalıcılığı denetimi

- Kapsam: `REQ-CAN-001`, `REQ-AUD-001`, `US-CUS-003`; müşteri iptali, refund tekilleştirme, slot serbest bırakma ve kalıcı rezervasyon durumu.
- Kök neden: `QueryMine(tracking: true)` içindeki diğer join kaynaklarına eklenen `AsNoTracking`, bileşik sorgunun `Reservation` entity'sini de takip dışına çıkardı. Servis bellekte statusu değiştirip iptal yanıtı veriyor; SQL slot/refund/audit yazdığı halde `Reservations` için `UPDATE` üretmiyordu.
- Çözüm: Tracking ve no-tracking sorgu kaynakları tüm join boyunca tutarlı ayrıldı. Mevcut `Refunded` kaydı kontrol edilerek önceki yarım iptallerde duplicate refund engellendi. Frontend başarılı response'u cache'e anında uyguluyor.
- Kanıt: Integration test artık persisted `ReservationStatus.CancelledByCustomer`, dolu `CancelledAtUtc`, sıfır aktif slot, tam bir refunded kayıt ve farklı idempotency anahtarıyla başarılı tekrarı assert ediyor. Backend unit 4/4, integration 15/15; frontend 16/16 ve statik/build kapıları geçti.
- Gerçek SQL/API: Düzeltme öncesi yeniden liste `Confirmed`; düzeltme sonrası create `201`, cancel `200`, repeat cancel `200`, yeniden liste `CancelledByCustomer/canCancel=false`. EF SQL logunda `UPDATE [Reservations]` kanıtlandı.
- `Critical/High`: yeni kod kapsamında bulgu yok.
- `High` ortam riski: Kalıcı `Encrypt=True` SQL TLS sorunu bu kod değişikliğinden bağımsız olarak açıktır; API loopback süreç override'ıyla çalışıyor.
- Sonuç: İptal düğmesi ve kalıcı durum davranışı kabul edildi; duplicate fake refund regresyonu kapatıldı.

## 2026-08-16 — İşletme frontend sayfası kaldırma denetimi

- Kapsam: Kullanıcı kararıyla public **İşletmeler için** navigasyonu, ana sayfa CTA/kartı ve `/isletme` rotası.
- Kod: Route kaldırıldı; kalan `PortalPage` yalnız `admin` audience kabul ediyor. Tek oyuncu kartı masaüstünde tam genişlikte kalıyor.
- Kanıt: `does not expose the cancelled business page route` ve ana sayfa link yokluğu testiyle frontend 17/17 geçti; lint, strict type-check ve build başarılı. Release backend build/unit/integration kapıları da geçti.
- Tarayıcı QA: Menüde işletme bağlantısı yok; `/isletme` 404; warning/error yok.
- `Critical/High/Medium`: bulgu yok.
- Sonuç: İptal edilen public işletme sayfası erişim ve içerik yüzeylerinden kaldırıldı.

## 2026-08-16 — Yönetim frontend sayfası kaldırma denetimi

- Kapsam: Kullanıcı kararıyla public **Yönetim** navigasyonu ve `/yonetim` rotası.
- Kod: Menü/route kaldırıldı; kullanılmayan portal bileşeni ve yalnız ona ait CSS temizlendi. Backend rol/yetki sınırı değiştirilmedi.
- Kanıt: `does not expose the cancelled management page route` ve ana sayfa link yokluğu testiyle frontend 18/18 geçti; lint, strict type-check ve build başarılı. Backend unit 4/4 ve integration 15/15 geçti.
- Tarayıcı QA: Menü yalnız **Saha bul / Giriş yap**; `/yonetim` 404; warning/error yok.
- `Critical/High/Medium`: bulgu yok.
- Sonuç: İptal edilen public yönetim sayfası erişim ve kod yüzeylerinden kaldırıldı.
