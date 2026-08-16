# Roadmap ve Görevler

Süreler ekip kapasitesi görülmeden tahmin olarak verilmez. Her fazın çıkışı doğrulama kapısına bağlıdır; takvim değil kalite kriteri önceliklidir.

## Faz 0 — Ürün kararları ve repo iskeleti

- [ ] Ödeme/bildirim sağlayıcısı, slot süresi, iptal/iade, komisyon ve hosting kararlarını onayla
- [x] ADR-0001–0006 taslaklarını oluştur
- [x] Backend solution, React TypeScript ve test projelerini oluştur
- [x] SDK/Node/package sürümlerini sabitle
- [x] CI build/lint/test/security tabanını kur
- [x] Local SQL Server, migration ve rol seed akışını kur
- [x] OpenAPI ve ortak Problem Details/correlation id standardını kur

**Çıkış:** Temiz ortamdan tek komut/script akışıyla build/test ve örnek health çalışır; gerçek secret yoktur.

## Faz 1 — Kimlik ve işletme sınırı

- [x] Kayıt/giriş/refresh/logout
- [ ] Parola sıfırlama
- [x] Customer, BusinessOwner, BusinessStaff, SystemAdmin policyleri
- [x] Business/BusinessMember ve tenant authorization handler
- [x] Auth/tenant negatif integration testleri
- [x] Audit temeli

**Çıkış:** `REQ-AUTH-*`, `T-AUTH-*` geçer; başka işletme verisi sızmaz.

## Faz 2 — Katalog ve işletme paneli

- [x] Facility/Field/Amenity modelleri ve migration
- [ ] İşletme paneli CRUD ekranları
- [ ] Çalışma saatleri, özel gün ve blokaj
- [ ] Fiyat kuralları ve fiyat snapshot servisi
- [ ] Görsel yükleme güvenliği
- [x] Public uygun saha arama
- [ ] Public tesis detay

**Çıkış:** Onaylı işletme bir tesisi/sahayı yayınlayıp doğru fiyatlı takvim ön izlemesi yapar.

## Faz 3 — Uygunluk ve çakışmasız rezervasyon

- [x] Yerel saat → UTC uygunluk arama motoru
- [x] Reservation/ReservationSlot/idempotency/outbox tabloları
- [x] Oluşturma ve müşteri liste/detay
- [ ] Gerçek sağlayıcı politikası sonrası hold expiry
- [x] Aktif slot filtered unique index
- [x] 50 paralel istek gerçek SQL concurrency doğrulaması
- [x] Müşteri uygun saha arama ekranı
- [x] Rezervasyon oluşturma ve Rezervasyonlarım takip/iptal ekranları

**Çıkış:** `US-CUS-001/002/003` ve `T-RES-001/002/003/004` geçer.

## Faz 4 — Ödeme, iptal ve iade

- [ ] `IPaymentGateway` + sandbox adapter
- [ ] Ödeme oturumu, webhook imza/event tekilliği
- [ ] Payment/PaymentEvent/Refund state machine
- [ ] İptal politikası snapshot ve iade hesabı
- [ ] Geç gelen capture telafi akışı
- [ ] Çift webhook ve refund integration testleri

**Çıkış:** Gerçek kart verisi uygulamaya girmeden sandbox E2E; `T-PAY-*` geçer.

## Faz 5 — Operasyon, bildirim ve rapor

- [ ] Manuel rezervasyon, check-in, complete, no-show
- [ ] Outbox worker, e-posta/SMS adapterları ve retry/dead-letter
- [ ] İşletme takvimi ve temel raporlar
- [ ] Admin işletme onayı ve audit ekranı
- [ ] Müşteri/işletme bildirim şablonları

**Çıkış:** Personel uçtan uca operasyonu ve admin denetimi yapar; sağlayıcı kesintisi ana transactionı bozmaz.

## Faz 6 — Sertleştirme ve release

- [ ] Erişilebilirlik/responsive/browser kontrolleri
- [ ] Performans ve concurrency yük testi
- [ ] Security/dependency/secret/container taraması
- [ ] Backup/restore ve rollback tatbikatı
- [ ] Observability dashboard ve alarmlar
- [ ] Kullanım kılavuzuna gerçek ekran görüntüleri
- [ ] 5 dakikalık demo, sürüm notu ve AI denetim raporu

**Çıkış:** Definition of Done, release checklist ve GO kararı.

## Faz 7 — Opsiyonel AI öneri/SSS

- [ ] Kısıtlı availability DTO şeması ve tool/servis
- [ ] Natural language filtre ayrıştırma
- [ ] Kaynak doğrulama, schema guard ve normal arama fallbacki
- [ ] Prompt injection/PII/eval seti
- [ ] Uydurma oranı 0 hedefi ve gözlemleme

**Çıkış:** AI kapalıyken ana ürün çalışır; AI yalnız gerçek saha/slot/fiyatı açıklar.

## Ortak görev kartı şablonu

```markdown
### TASK-XXX — Başlık
- Amaç:
- İlgili gereksinim/hikâye:
- Kapsam içi / dışı:
- Bağımlılıklar:
- Veri/API/UI etkisi:
- Güvenlik/concurrency riski:
- Kabul kriterleri:
- Testler:
- Doküman güncellemeleri:
- Rollback:
- Durum/sahip:
```

## Definition of Ready

- Gereksinim ve kabul kriteri açık
- Tasarım/sağlayıcı kararı gerekliyse onaylı
- Veri/API etkisi biliniyor
- Güvenlik ve test yaklaşımı yazılı
- Kapsam bir agent/PR turunda yönetilebilir büyüklükte

## Definition of Done

`PROJECT.md` bölüm 12'nin tüm maddeleri uygulanır. Eksik doküman, çalışan ürünü teknik teslim bakımından tamamlanmış yapmaz.
