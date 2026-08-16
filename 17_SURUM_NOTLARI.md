# Sürüm Notları

## 2026-08-16 — Yönetim sayfasının kaldırılması

- Üst menüdeki **Yönetim** bağlantısı kaldırıldı.
- `/yonetim` rotası ve artık kullanılmayan portal bileşeni/stilleri kaldırıldı.
- `/yonetim` adresi artık standart 404 ekranını gösteriyor.

## 2026-08-16 — İşletme sayfasının kaldırılması

- Üst menüdeki **İşletmeler için** bağlantısı kaldırıldı.
- Ana sayfadaki işletme çağrıları ve kartı kaldırıldı.
- `/isletme` rotası devre dışı bırakıldı ve artık 404 ekranına yönleniyor.

## 2026-08-16 — Rezervasyon iptalinin kalıcı durum düzeltmesi

- İptal sorgusundaki EF Core tracking sınırı düzeltildi; `Reservations.Status`, `CancelledAtUtc` ve `UpdatedAtUtc` artık SQL'de kalıcı güncelleniyor.
- Başarılı iptal yanıtı React Query önbelleğine hemen uygulanıyor; buton gecikmeden kalkıp durum **Sen iptal ettin** oluyor.
- Önceki hatalı denemeden refund oluşmuşsa yeni tıklama ikinci fake iade üretmeden rezervasyon durumunu onarıyor.
- Integration testi kalıcı rezervasyon durumunu, tek refundı, inaktif slotları ve tekrarlı iptali doğruluyor.

## 2026-08-16 — Süresi dolan oturumda rezervasyon düzeltmesi

- Access token süresi dolduğunda rezervasyon isteği refresh cookie ile bir kez yenilenip aynı `Idempotency-Key` ile güvenle tekrar ediliyor.
- Refresh oturumu da bitmişse genel “bilgilerinizi kontrol edin” mesajı yerine oturum süresi açıklaması ve giriş bağlantısı gösteriliyor.
- API'nin `401/403` yetki yanıtları `application/problem+json` ve `correlationId` içeriyor.
- Geçmiş saatler uygunluk sonucundan çıkarılarak tıklanınca reddedilecek saha kartlarının gösterilmesi engellendi.

## 2026-08-16 — Rezervasyon ve İstanbul saha kataloğu

- İstanbul'un 39 ilçesine development ortamında en az üç yayınlanmış saha, çalışma saati, fiyat ve özellik verisi eklendi.
- Customer için idempotent rezervasyon oluşturma/liste/detay/iptal API'leri ve `FakeSandbox` ödeme adaptörü eklendi.
- 30 dakikalık SQL slot tahsisi, serializable transaction, filtered unique index ve deadlock/unique yarışının `409` dönüşümü uygulandı.
- React aramasına ilçe seçimi ve rezervasyon düğmesi; kullanıcı navigasyonuna Rezervasyonlarım paneli, maç özeti ve iptal akışı eklendi.

## Unreleased

### Added

- Geliştirme öncesi ürün, mimari, veri, API, test ve güvenlik sözleşmeleri.
- .NET 10 solution, React TypeScript istemci ve test iskeleti planı.
- SQL Server health check, RFC 7807 ve correlation id temeli.
- SQL Server 2022 CU26 yerel geliştirme doğrulaması, loopback-only TCP yapılandırma scripti ve `InitialFoundation` migrationı.
- SahaGo responsive uygulama kabuğu; müşteri ana sayfası, saha arama, giriş, işletme, yönetim ve 404 rotaları.
- React Router, TanStack Query, React Hook Form ve Zod tabanlı frontend veri/yönlendirme/form temeli.
- Merkezi health API istemcisi, loading/error/success durumları, mobil menü ve erişilebilir form doğrulaması.
- Beş davranış odaklı frontend rota/durum testi ve Vite yerel API proxy yapılandırması.
- ASP.NET Core Identity tabanlı e-posta/telefon register, login, JWT, refresh rotation, logout ve `me` API'leri.
- 28 tablolu `CreateCoreSchemaAndIdentity` migrationı; identity, tenant, katalog, takvim, rezervasyon, ödeme, outbox ve audit veri temeli.
- Telefon-only kayıt kullanıcı adı hatası giderildi; kayıt ekranında `+90` sabit ülke kodu ve yerel 10 haneli giriş kullanılıyor. API port profili frontend proxy ile eşitlendi, rate-limit ve boş API hataları açıklayıcı hale getirildi.
- SQL-backed uygun saha araması eklendi: development örnek katalog/takvim/fiyat verisi, `/api/v1/availability`, çalışma saati/blok/aktif slot kontrolleri ve React loading/empty/error/success sonuç kartları.
- Dört seed rolü, rol policyleri ve aktif `BusinessMember` üyeliğini doğrulayan tenant authorization handler.
- Gerçek API'ye bağlı `/giris` ve `/kayit` ekranları; access token yalnız bellekte, refresh token HttpOnly cookie'de.
- Gerçek SQL şeması, auth lifecycle ve tenant negatif yollarını doğrulayan unit/integration testleri.

### Security

- Connection string kaynak koddan çıkarılıp .NET User Secrets için yapılandırıldı.
- Gerçek ödeme ve bildirim sağlayıcıları devre dışı bırakıldı.
- JWT imzalama anahtarı .NET User Secrets'a alındı; refresh tokenlar yalnız SHA-256 hash olarak saklanıyor.
- Public register rol seçemez ve yalnız `Customer` olabilir; login hataları hesap varlığını açıklamaz.
