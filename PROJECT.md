# PROJECT — Halı Saha Rezervasyon Sistemi

> Durum: Geliştirme öncesi başlangıç sözleşmesi  
> Son güncelleme: 16 Ağustos 2026  
> Sahip: Ürün sahibi / proje geliştiricisi  
> Dil: Kullanıcı arayüzü Türkçe; kod, API alanları ve teknik semboller İngilizce

## 1. Ürün vizyonu

Müşterinin konum, tarih ve saat seçerek uygun halı sahayı bulduğu; çakışma olmadan rezervasyon ve güvenli ödeme yaptığı; işletmenin sahalarını, fiyatlarını, çalışma saatlerini ve rezervasyonlarını yönettiği web tabanlı bir sistem oluşturmak.

## 2. Başarı tanımı

MVP aşağıdaki uçtan uca akışları eksiksiz çalıştırdığında başarılıdır:

- Müşteri kayıt olur, uygun saha arar, fiyatı görür, rezervasyon oluşturur ve ödeme sonucunu izler.
- Aynı saha ve zaman dilimi eşzamanlı iki isteğe yalnızca bir kez tahsis edilir.
- İşletme sahasını, çalışma saatini, blokajını, fiyatını ve rezervasyonlarını yönetir.
- İptal/iade politikası sistem tarafından tutarlı uygulanır.
- Yönetici kullanıcı/işletme denetimi ve temel operasyon kayıtlarını görebilir.
- Kritik işlemler audit log ile izlenebilir, fakat parola/token/kart verisi loglanmaz.

## 3. Kullanıcı rolleri

| Rol | Temel yetkiler |
|---|---|
| Customer | Profil, arama, uygunluk, rezervasyon, ödeme, iptal, kendi geçmişi |
| BusinessOwner | Kendi tesis/saha/fiyat/takvim/personel/rezervasyon yönetimi ve rapor |
| BusinessStaff | Yetki verildiği tesiste rezervasyon görüntüleme, manuel kayıt ve check-in |
| SystemAdmin | Kullanıcı ve işletme denetimi, sistem ayarları, audit erişimi |

`BusinessOwner` ve `BusinessStaff`, başka işletmenin verisini hiçbir endpoint üzerinden okuyamaz veya değiştiremez.

## 4. MVP kapsamı

### Dahil

- E-posta/telefon ile kayıt ve giriş; parola sıfırlama
- Rol tabanlı yetkilendirme
- Tesis, saha, özellik, görsel ve konum yönetimi
- Haftalık çalışma saatleri ve özel gün/blokaj yönetimi
- Tarih/saat/ilçe/özellik bazlı uygun saha arama
- Saat dilimine duyarlı fiyat ve uygunluk gösterimi
- Rezervasyon oluşturma, süre sonu, onay, iptal, tamamlanma, gelmeme
- Online ödeme sağlayıcısı adaptörü ve tesiste ödeme seçeneği (işletme bazlı)
- İade süreci ve webhook işleme
- E-posta/SMS için sağlayıcıdan bağımsız bildirim kuyruğu
- Müşteri, işletme ve yönetici panelleri
- Temel gelir/doluluk/iptal raporları
- OpenAPI, testler, migration, seed ve bu dokümantasyon

### MVP dışında

- Turnuva/lig, takım kadrosu ve oyuncu eşleştirme
- Canlı sohbet, sosyal ağ ve puan tablosu
- Abonelik/üyelik paketi, kupon kampanyası ve sadakat puanı
- Dinamik fiyatı AI'ın otomatik değiştirmesi
- Mobil uygulama
- Çoklu para birimi ve çoklu dil

Bu maddeler yeni bir kapsam kararı ve ADR olmadan geliştirilemez.

## 5. Ürün kuralları

1. Uygunluk ve rezervasyon kararı sunucu tarafında verilir; frontend verisine güvenilmez.
2. Aynı `FieldId + SlotStartUtc` için yalnızca bir aktif rezervasyon dilimi bulunabilir.
3. Rezervasyon oluşturma isteği `Idempotency-Key` taşır; aynı anahtar aynı kullanıcı için aynı sonucu döndürür.
4. Fiyat, rezervasyon anında kalem bazında snapshot olarak saklanır; sonraki fiyat değişikliği eski rezervasyonu değiştirmez.
5. Para alanları `decimal(18,2)` ve ISO 4217 para birimi kodu ile tutulur; `float/double` kullanılmaz.
6. Tüm kalıcı zaman damgaları UTC tutulur. Tesisin IANA/Windows uyumlu saat dilimi ayrıca saklanır.
7. Ödeme webhookları imza doğrulaması, olay kimliği tekilleştirmesi ve idempotent işleme olmadan kabul edilmez.
8. Kart numarası, CVV veya tam kart verisi sisteme alınmaz/saklanmaz; ödeme sağlayıcısının yönlendirmeli/tokenize akışı kullanılır.
9. İptal/iade miktarı rezervasyonda dondurulan politika ve zaman bilgisine göre sunucu tarafından hesaplanır.
10. Hard delete yalnızca yanlış oluşturulmuş teknik/test verisinde tercih edilir; iş kayıtları durum değiştirme ve audit ile korunur.
11. AI çıktısı uygunluk, fiyat, ödeme, yetki veya iade için tek başına karar kaynağı olamaz.
12. Her state-changing endpoint yetki, doğrulama, audit ve yarış durumu açısından test edilir.

## 6. Rezervasyon yaşam döngüsü

`PendingPayment → Confirmed → Completed | NoShow`

Yan yollar:

- `PendingPayment → Expired`
- `PendingPayment | Confirmed → CancelledByCustomer | CancelledByBusiness`
- Uygun iptalde `Payment: Captured → RefundPending → Refunded`

Durum geçişleri `03_IS_AKISLARI_VE_DURUMLAR.md` içindeki tablo dışında yapılamaz.

## 7. Mimari sınırlar

- Backend modülleri: Identity, Catalog, Availability, Reservations, Payments, Notifications, Reporting, Administration.
- Domain katmanı ASP.NET, EF Core, HTTP ve sağlayıcı SDK'larına bağımlı olmaz.
- React doğrudan SQL'e veya ödeme sağlayıcısına erişmez; tüm iş akışı API üzerinden yürür.
- Modüller arası çağrılar application servisleri/kontratları üzerinden yapılır.
- İlk sürüm modüler monolittir. Mikroservis bölünmesi yalnız ölçülmüş ihtiyaçla yapılır.

## 8. API ve hata standardı

- Kök: `/api/v1`
- JSON: `camelCase`; UTF-8; ISO-8601 UTC (`2026-08-16T09:00:00Z`)
- Hatalar: RFC 7807 Problem Details
- Listeleme: `page`, `pageSize`, `sort`; `pageSize` üst sınırı 100
- İzleme: her yanıt `X-Correlation-Id` taşır
- State-changing isteklerde CSRF/CORS/kimlik modeline uygun koruma uygulanır
- Breaking change yeni API ana sürümü gerektirir

## 9. Güvenlik tabanı

- Parolalar ASP.NET Core Identity standardıyla hashlenir.
- Kısa ömürlü access token, döndürülen/iptal edilebilir refresh token kullanılır.
- Rate limit: login, parola sıfırlama, uygunluk ve rezervasyon endpointlerinde zorunlu.
- Veri erişiminde işletme kapsamı (`BusinessId`) server-side filtrelenir.
- Secret'lar kodda, `.env` örneğinde veya dokümanda gerçek değerle bulunmaz.
- Loglar yapılandırılmıştır; PII maskeleme ve erişim kısıtı uygulanır.
- Bağımlılık ve güvenlik taraması CI içinde çalışır.

## 10. Performans ve işletilebilirlik hedefleri

- Uygunluk sorgusu: normal yükte p95 ≤ 800 ms (harici servis hariç).
- Rezervasyon oluşturma: p95 ≤ 1.5 sn (ödeme sağlayıcısına yönlendirme hariç).
- Eşzamanlı 50 aynı-slot isteğinde en fazla bir başarılı rezervasyon.
- Sağlık kontrolleri: liveness, readiness ve SQL bağlantısı.
- Log, metric ve trace üzerinde correlation id ortak kullanılır.
- Günlük otomatik veritabanı yedeği ve geri yükleme tatbikatı yayın öncesi kanıtlanır.

## 11. AI ile geliştirme sözleşmesi

AI önce bu dosyayı ve görevle ilgili alt belgeleri okur. Sonra:

1. Planı, etkilenen dosyaları, varsayımları ve riskleri yazar.
2. En küçük doğrulanabilir değişikliği uygular.
3. Test/migration/dokümanı aynı değişiklikte günceller.
4. Ürettiği iddiayı test, derleme, diff veya kaynakla kanıtlar.
5. `11_AI_CALISMA_GUNLUGU.md` içine karar ve insan düzeltmesini kaydeder.
6. Belirsiz ürün kararı, geri döndürülemez veri değişikliği veya güvenlik açığı riski varsa durur.

AI; mevcut olmayan endpoint, tablo, paket, test sonucu veya iş kuralı uyduramaz. `dotnet test`/frontend testleri çalıştırılmadıysa “başarılı” diyemez.

## 12. Definition of Done

Bir iş ancak tümü sağlandığında tamamdır:

- Kabul kriteri karşılanmış ve izlenebilirlik kimliğiyle ilişkilendirilmiş
- Kod derlenmiş, lint/type-check geçmiş
- Unit ve uygun seviyede integration/E2E test yazılmış ve geçmiş
- Yetkilendirme, validasyon, concurrency ve hata yolu test edilmiş
- Migration ileri/geri dönüş etkisi gözden geçirilmiş
- OpenAPI ve ilgili MD dosyaları güncel
- Loglarda secret/PII olmadığı kontrol edilmiş
- Reviewer kritik bulgu bırakmamış
- AI çalışma günlüğü ve gerekiyorsa denetim raporu güncellenmiş

## 13. Açık kararlar

Uygulamaya başlamadan ürün sahibi aşağıdaki değerleri onaylamalıdır:

- İlk ödeme sağlayıcısı
- SMS/e-posta sağlayıcısı
- Varsayılan rezervasyon dilimi (öneri: 30 dakika) ve minimum/maksimum süre
- İptal/iade eşikleri ve komisyon modeli
- İşletme onay akışı
- Üretim barındırma, alan adı, dosya depolama ve gözlemleme sağlayıcısı

Onaylanmayan değerler interface/configuration arkasında kalır; koda sağlayıcıya özel sabit gömülmez.
