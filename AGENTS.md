# AGENTS — AI Geliştirici Talimatları

Bu dosya, projede çalışan her AI agent için bağlayıcıdır. Çelişki halinde sıralama: güvenlik ve veri bütünlüğü → `PROJECT.md` → kabul kriterleri → teknik belgeler → görev promptu.

## Başlamadan önce

1. `PROJECT.md` dosyasını tamamen oku.
2. Görevle ilgili gereksinim, veri, API ve test bölümlerini oku.
3. Repo durumunu ve mevcut değişiklikleri incele; kullanıcı değişikliklerini ezme.
4. Kısa bir plan, etki alanı ve doğrulama komutları yaz.
5. Eksik ürün kararını varsayım olarak işaretle; veri/ödeme/yetkiyi etkiliyorsa sor.

## Roller

### Planlayıcı

- Gereksinimi `REQ-*` ve `US-*` kayıtlarına bağlar.
- Kapsam dışı talepleri ve riskleri belirtir.
- Veri migrationı, API breaking change ve güvenlik etkisini açıklar.
- Uygulayıcı için dosya bazlı ve test edilebilir görevler çıkarır.

### Uygulayıcı

- Yalnız onaylı planın küçük bir dilimini uygular.
- Domain kurallarını controller/component içine dağıtmaz.
- Her davranışla birlikte test ve doküman günceller.
- Hata bastırmaz; beklenen domain hatasını standart Problem Details'a çevirir.

### Denetleyici

- Planı ve diff'i gereksinime göre bağımsız inceler.
- Yetki, tenant sınırı, veri sızıntısı, yarış durumu, idempotency ve saat dilimini özellikle kontrol eder.
- “Test var” iddiasını test adı ve çıktısıyla doğrular.
- Kritik bulguda değişikliği reddeder ve somut revizyon ister.

Tek agent çalışıyorsa bu rolleri sırayla uygular ve denetim turunda önceki cevabını doğru kabul etmez.

## Kod kuralları

- Backend: nullable reference types açık; async I/O; `CancellationToken`; DI; domain sonuçları için açık hata tipleri.
- Controller ince; iş kuralları application/domain katmanında.
- EF sorgularında salt okuma için `AsNoTracking`; kontrolsüz N+1 ve sınırsız liste yok.
- Frontend: TypeScript strict; server state için tek veri katmanı; form şeması; erişilebilir etiketler; loading/empty/error/success durumları.
- SQL: UTC; para `decimal(18,2)`; FK/index; `rowversion`; migration adı anlamlı.
- Test: davranış odaklı ad; sahte test veya yalnız mock etkileşimiyle kritik kural kanıtlama yok.
- Secret, token, parola, kişisel veri, kart verisi ve gerçek müşteri verisi commit edilmez.

## Rezervasyon için değişmezler

- Çakışma kontrolü ile slot tahsisi tek transaction içinde olmalıdır.
- Aynı slot için concurrent isteklerden en fazla biri başarılıdır.
- İstemcinin gönderdiği fiyat, indirim, işletme veya kullanıcı kimliği güvenilir değildir.
- İptal ve iade ayrı süreçlerdir; iade hatası rezervasyon geçmişini yok etmez.
- Webhook tekrar gelebilir; olay kimliği tekilleştirilir.
- Retry yalnız idempotent işlemde veya aynı idempotency anahtarıyla yapılır.

## Zorunlu doğrulamalar

- Backend build + unit + integration test
- Frontend lint + type-check + unit/component test
- Kritik akış için E2E
- Migration ve temiz veritabanı başlangıcı
- OpenAPI ile frontend client uyumu
- Yetkisiz ve başka işletme verisine erişim testleri
- Eşzamanlı rezervasyon ve çift webhook testleri

## Durdurma koşulları

Aşağıdakilerden biri varsa uygulamayı durdur, bulguyu yaz ve insan kararı iste:

- Geri döndürülemez/kitlesel veri silme veya riskli migration
- Gerçek ödeme/mesaj gönderme ya da production değişikliği
- Kimlik doğrulama/tenant sınırı belirsizliği
- Kabul kriterleri arasında çelişki
- Secret veya kişisel veri sızıntısı
- Çözümü değiştiren sağlayıcı/iş politikası kararı

## Agent tur ve limitleri

- Planlayıcı → Uygulayıcı → Denetleyici; en fazla 2 revizyon turu.
- Bir turda tek dikey dilim; ilgisiz refactor yapılmaz.
- Denetleyici `Critical` veya `High` bulguda reddeder.
- İki tur sonunda kapanmayan bulgu insan kararına çıkarılır.

## Teslim formatı

Her çalışma sonunda şu kısa kayıt verilir:

```text
Amaç:
Değişen dosyalar:
Kararlar/varsayımlar:
Çalıştırılan doğrulamalar ve sonuçları:
Açık riskler:
Sonraki adım:
```

Kayıt ayrıca `11_AI_CALISMA_GUNLUGU.md` dosyasına eklenir.

