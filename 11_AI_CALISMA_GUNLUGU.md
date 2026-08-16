# AI Çalışma Günlüğü

## 2026-08-16 — Geliştirme öncesi keşif ve planlama

Amaç: Mevcut proje belgelerini ve klasör yapısını incelemek; yerel geliştirme araçları ile SQL Server bağlantısını salt okunur biçimde kontrol etmek; kod yazmadan fazlı uygulama planı hazırlamak.

Değişen dosyalar: Yalnızca bu zorunlu çalışma günlüğü oluşturuldu; uygulama kodu veya yapılandırma değiştirilmedi.

Kararlar/varsayımlar: Klasördeki `PROJECT (1).md`, adı düzeltilene kadar ana ürün sözleşmesi kabul edildi. Sağlayıcı kararları verilene kadar ödeme ve bildirimler arayüz/yapılandırma arkasında fake/sandbox olarak planlandı. Eksik gereksinim ve teknik belgeler uygulama başlamadan tamamlanacak bir Faz 0 kapısı olarak ele alındı.

Çalıştırılan doğrulamalar ve sonuçları: Dört mevcut Markdown dosyasının tamamı okundu. Klasörün Git deposu olmadığı görüldü. .NET SDK 8.0.403, 9.0.101 ve 9.0.315; Node.js 22.12.0; npm 10.9.0; sqlcmd 16.0.1000.6 bulundu. Varsayılan SQL Server ve SQLEXPRESS servisleri çalışıyor. `Halisahadb` için verilen SQL kullanıcısıyla bağlantı sunucuya ulaştı ancak hesap parolasının değiştirilmesi zorunlu olduğu için giriş reddedildi; veri veya şema değiştirilmedi.

Açık riskler: Doküman haritasındaki gereksinim, kullanıcı hikâyesi, durum makinesi, mimari, veri modeli, API, test, güvenlik ve denetim belgeleri dahil 13 dosya eksik. `PROJECT.md` adıyla beklenen dosya `PROJECT (1).md` adında. Git geçmişi/çalışma ağacı yok. Yerel SQL hesabı kullanılabilir değil. Slot süresi, rezervasyon süre sınırları, iptal/iade, komisyon, işletme onayı ve sağlayıcı/hosting kararları açık.

Sonraki adım: Ürün sahibinin planı ve kritik kararları onaylamasından sonra yalnız Faz 0'ın ilk küçük dikey dilimini uygulamak.

## 2026-08-16 — Faz 0 uygulama turu 1

Amaç: SQL bağlantısını yeniden doğrulamak; Faz 0 belge, backend/frontend/test, CI, User Secrets ve migration temelini kurmak.

Değişen dosyalar: `PROJECT.md` doğru ada taşındı; 01–10, 12–14, 16–17 belgeleri ve altı ADR oluşturuldu. .NET solution, API/Application/Domain/Infrastructure ve iki test projesi; React TypeScript frontend; CI, doğrulama scripti, global SDK ve local EF tool manifesti eklendi. `InitialFoundation` migrationı üretildi.

Kararlar/varsayımlar: .NET 10 LTS ve Node.js 24 LTS seçildi. Faz 0 migrationı iş tablosu yaratmadan yalnız migration geçmişini başlatacak şekilde sınırlandı. Ödeme/bildirim fake/sandbox ve diğer açık ürün politikaları ilgili faza ertelendi. Önceki günlükte eksik belge sayısı 13 yazılmıştı; doğru sayı 15’tir ve bu turda tamamı oluşturuldu.

Çalıştırılan doğrulamalar ve sonuçları: SQL kullanıcı girişi ve boş tablo envanteri başarılı. Backend build 0 uyarı/0 hata; unit 1/1 ve integration 1/1 geçti. Frontend lint/type-check, Vitest 1/1 ve build geçti. NuGet/npm taramalarında bilinen açık yok; repo secret pattern taraması temiz. Migration scripti destructive değil.

Açık riskler: Modern `Microsoft.Data.SqlClient` ile `Encrypt=True`/`Strict` TLS el sıkışması başarısız. EF migration veritabanına uygulanmadı; SQL readiness health ve temiz DB başlangıcı doğrulanamadı. SQL Server güncellemesi veya TLS yapılandırma değişikliği insan kararı gerektiriyor. Faz 1 öncesinde slot/hold/iptal-iade/komisyon/onay kararları da açık.

Sonraki adım: Yerel SQL Server TLS uyumluluğunun nasıl giderileceği onaylandıktan sonra migrationı uygulamak, readiness health’i doğrulamak ve Faz 0 kalite kapısını yeniden denetlemek.

## 2026-08-16 — Faz 0 uygulama turu 2

Amaç: SQL Server TLS/migration engelini öncelikli gidermek ve Faz 0 kalite kapılarını yeniden doğrulamak.

Değişen dosyalar: `scripts/configure-local-sql.ps1` eklendi; kullanım kılavuzu, sürüm notu, denetim raporu ve bu günlük güncellendi. Kullanılmayan Vite demo varlıkları ve yanlışlıkla oluşan kök lockfile kaldırıldı.

Kararlar/varsayımlar: SQL Server CU26 yalnız `MSSQLSERVER` instance’ına uygulandı. TCP dış ağa açılmadan loopback IP’lerine sınırlandı. Kalıcı User Secrets `tcp:127.0.0.1,1433 + Encrypt=True + TrustServerCertificate=True` olarak tutuldu. Migration uygulamasında reboot öncesi tek seferlik `Encrypt=False` yalnız loopback üzerinde süreç override olarak kullanıldı; kaynak veya secret’a yazılmadı.

Çalıştırılan doğrulamalar ve sonuçları: CU26 hash ve build doğrulandı. TCP loopback 1433 erişilebilir. Migration uygulandı ve idempotent tekrar testi geçti. Şemada yalnız `__EFMigrationsHistory` ve beklenen tek kayıt var. Geçici loopback override ile live/ready/OpenAPI 200. Backend build 0/0, unit 1/1, integration 1/1; frontend lint/type-check/Vitest 1/1/build/audit başarılı.

Açık riskler: CU kurulumu exit 3010 ve pending file rename üretti; bilgisayar yeniden başlatılmadan kalıcı `Encrypt=True` modern SqlClient health testi geçmiyor. İki Node süreci native Rolldown dosyasını kilitlediği için temiz `npm ci` tekrar doğrulaması reboot sonrasına kaldı.

Sonraki adım: Bilgisayarı yeniden başlatmak; ardından kalıcı User Secrets ile `dotnet ef migrations list`, `/health/ready`, temiz `npm ci` ve tam doğrulama scriptini çalıştırıp Faz 0'ı kapatmak.

## 2026-08-16 — Faz 0 frontend tamamlama

Amaç: Faz 0 frontend iskeletini üretim kalitesinde, responsive ve erişilebilir bir uygulama kabuğuna dönüştürmek; mevcut olmayan backend davranışlarını taklit etmeden sonraki dikey dilimlere hazır hale getirmek.

Değişen dosyalar: `frontend/src` altında uygulama/rota, layout ve ortak bileşenler, ana sayfa, arama, giriş, işletme/yönetim ve 404 sayfaları, merkezi health API istemcisi, responsive stil sistemi ve beş test eklendi. Vite proxy/test yapılandırması, paket bağımlılıkları, HTML metadata, `.env.example`, kullanım kılavuzu, sürüm notları, denetim raporu ve bu günlük güncellendi.

Kararlar/varsayımlar: Bu tur Faz 0 frontend dikey dilimiyle sınırlandı. Backend'de kimlik, tesis, uygunluk, rezervasyon ve ödeme endpointleri henüz bulunmadığı için arayüz bunlar için sahte başarı, oturum veya saha verisi üretmiyor. Readiness dışındaki gerçek server state ilgili roadmap fazında aynı merkezi API sınırına eklenecek.

Çalıştırılan doğrulamalar ve sonuçları: ESLint ve TypeScript strict type-check geçti. Vitest 1 dosyada 5/5 testi geçti. Vite production build 164 modülle başarılı; JS 420,48 kB (gzip 130,53 kB), CSS 13,87 kB (gzip 3,89 kB). npm audit 0 açık. Canlı API + Vite ile gerçek tarayıcıda masaüstü ana sayfa/readiness, arama hata ve başarı durumları, 390×844 mobil yerleşim ve menü doğrulandı; konsolda warning/error yok.

Açık riskler: Kalıcı `Encrypt=True` SQL bağlantısının reboot sonrası doğrulanması ve temiz `npm ci` önceki Faz 0 kapısından açıktır. Gerçek rol/tenant, tesis, uygunluk, rezervasyon ve ödeme UI'ları ilgili backend fazlarına bağlıdır. Rota sayısı büyüdüğünde bundle code splitting değerlendirilmelidir. Sandbox API kapanışında Windows Event Log izin uyarısı gözlendi.

Sonraki adım: Kullanıcı onayıyla önce reboot sonrası Faz 0 SQL/npm kapılarını kapatmak; ardından Faz 1 kimlik ve rol/tenant dilimini backend, frontend ve testleriyle birlikte geliştirmek.

## 2026-08-16 — Kimlik API'si ve kalıcı SQL şeması

Amaç: Kullanıcı kayıt/giriş/refresh/logout akışını frontend–API–SQL Server arasında bağlamak; dokümanlarda planlanan önemli tabloları destructive olmayan migration ile oluşturmak; rol ve tenant sınırı temelini kurmak.

Değişen dosyalar: Domain'e tenant, katalog, takvim, rezervasyon, ödeme, entegrasyon ve audit modelleri; Application'a auth kontratları/credential policy; Infrastructure'a ASP.NET Core Identity, JWT, refresh rotation, tenant handler ve EF model konfigurasyonu; API'ye auth controller/rate limit; frontend'e auth context, gerçek giriş/kayıt ekranları ve merkezi API çağrıları eklendi. `CreateCoreSchemaAndIdentity` migrationı, unit/integration/component testleri ve ilgili MD belgeleri güncellendi.

Kararlar/varsayımlar: Public kayıt güvenlik nedeniyle rol girdisi kabul etmiyor ve yalnız Customer oluşturuyor. Access token 15 dakika; refresh token 30 gün, HttpOnly/SameSite Strict cookie ve veritabanında yalnız SHA-256 hash. E-posta veya E.164 telefon yeterli; doğrulama mesajı gönderilmiyor. Gelecek faz tabloları veri bütünlüğü temeli olarak oluşturuldu fakat endpoint/iş davranışları roadmap fazları tamamlanmadan aktif sayılmıyor. Migration yalnız yeni tablo/index/FK/rol seed ekliyor.

Çalıştırılan doğrulamalar ve sonuçları: Migration uygulandı ve idempotent tekrar “database already up to date” verdi. Gerçek SQL testi 29 tabloyu (`__EFMigrationsHistory` dahil), dört rolü, iki migrationı ve aktif slot filtered unique indexini geçti. Backend build 0 uyarı/0 hata; unit 4/4 ve integration 6/6. Frontend lint/type-check, Vitest 7/7, build ve npm audit başarılı. NuGet taramasında açık yok; repo secret taraması temiz. Canlı SQL + gerçek tarayıcıda kayıt doğrulama, Customer login, reload sonrası refresh rotation ve logout geçti; OpenAPI/ready 200, duplicate register 409, tarayıcı konsolu temiz.

Açık riskler: Kalıcı User Secrets `Encrypt=True` bağlantısı ortam TLS sorunu nedeniyle modern SqlClient ile hâlâ doğrulanamadı; migration ve canlı testler yalnız loopback süreç override `Encrypt=False` ile çalıştı. Parola sıfırlama ve e-posta/telefon sahiplik doğrulaması henüz uygulanmadı. Gelecek faz tablolarının iş servisleri/API'leri yok. Yerel veritabanında yalnız test amaçlı `codex.e2e.20260816@local.test` Customer hesabı bırakıldı.

Sonraki adım: Kullanıcı onayıyla parola sıfırlama ve iletişim doğrulamasını tamamlayarak Faz 1'i kapatmak; ardından Faz 2 katalog CRUD/API/UI dikey dilimine geçmek.

## 2026-08-16 — Telefonla kayıt ve API bağlantı düzeltmesi

Amaç: `REQ-AUTH-001` ve `REQ-NFR-003/005` kapsamında kayıt ekranındaki manuel `+90` beklentisini kaldırmak ve telefon-only kaydın genel hata vermesine yol açan frontend/API sorunlarını gidermek.

Değişen dosyalar: `frontend/src/lib/phone.ts`, kayıt/giriş formu, merkezi API hata eşlemesi, form stilleri ve component testleri; API launch profili, rate-limit Problem Details yanıtı, `AuthService` telefon kullanıcı adı üretimi ve integration testleri; API/kullanım/sürüm belgeleri güncellendi.

Kararlar/varsayımlar: Ürün Türkiye odaklı olduğu için form sabit `+90` ülke kodu yanında yalnız 10 haneli mobil numara alıyor; başında `0`, `90` veya `+90` ile yapıştırılan değerleri de normalize ediyor. API sözleşmesi ve SQL saklama biçimi E.164 kalıyor. Public rol davranışı değişmedi ve kayıt yalnız `Customer` oluşturuyor.

Çalıştırılan doğrulamalar ve sonuçları: Backend build başarılı; unit 4/4, integration 7/7 geçti, gerçek SQL şema testi bu turda environment variable verilmediği için 1 adet kontrollü skip. Frontend lint/type-check/build başarılı ve Vitest 9/9 geçti. Gerçek tarayıcıda sabit `+90` + yerel numara alanı görsel/erişilebilirlik kontrolünden geçti. Normal `dotnet run` API'yi `127.0.0.1:5198` üzerinde açtı; Vite proxy üzerinden gerçek API ve `Halisahadb` telefon-only kayıt isteği `201`, E.164 numara ve `Customer` rolüyle doğrulandı.

Açık riskler: Kalıcı `Encrypt=True` yerel TLS problemi önceki kayıttaki gibi açıktır; canlı test loopback süreç override `Encrypt=False` ile yapıldı. Telefon sahiplik doğrulaması ve parola sıfırlama henüz yok. Canlı test gerçek yerel veritabanında `Codex Telefon Akis Testi` adlı bir test Customer hesabı bıraktı.

Sonraki adım: Kullanıcı onayıyla iletişim doğrulaması/parola sıfırlama dilimini tamamlamak veya Faz 2 katalog API/UI çalışmalarına geçmek.

## 2026-08-16 — Kayıt/giriş butonu operasyonel doğrulaması

Amaç: Kullanıcının talep ettiği kayıt ve giriş butonu davranışının React → ASP.NET Core API → Identity/EF Core → SQL Server zincirinde çalıştığını doğrulamak ve `502` üreten kapalı servisleri ayağa kaldırmak.

Değişen dosyalar: Uygulama kaynak kodu değiştirilmedi; yalnız bu çalışma günlüğü güncellendi. Mevcut buton stilleri ve API handlerları korundu.

Kararlar/varsayımlar: React içinde `asp:Button`/Web Forms server control ve istemciden doğrudan `INSERT INTO` kullanılmadı; bu yaklaşım mevcut React/Web API mimarisini ve secret/parola güvenliğini ihlal eder. Eşdeğer davranış mevcut `POST /api/v1/auth/register` ve `POST /api/v1/auth/login` uçlarıyla sağlandı.

Çalıştırılan doğrulamalar ve sonuçları: API `127.0.0.1:5198`, frontend `127.0.0.1:5173` üzerinde başlatıldı. Vite proxy üzerinden readiness `200`, gerçek `Halisahadb` register `201`, aynı bilgilerle login `200`; iki auth yanıtında da `Customer` rolü doğrulandı.

Açık riskler: Servis süreçleri kapatılırsa frontend yeniden `502 Bad Gateway` döndürür. Kalıcı `Encrypt=True` yerel TLS problemi nedeniyle API bu çalışma oturumunda önceki gibi yalnız loopback süreç override `Encrypt=False` ile çalıştırıldı. Canlı doğrulama `Buton Akis Testi` adlı yerel test Customer hesabı oluşturdu.

Sonraki adım: Kullanıcı arayüzünden kayıt/giriş denenebilir; oturum sona erdiğinde iki geliştirme servisi yeniden başlatılmalıdır.

## 2026-08-16 — SQL-backed uygun saha arama

Amaç: `REQ-CAT-001`, `REQ-AVL-001`, `REQ-PRICE-001`, `US-CUS-001` ve `REQ-NFR-003/005` kapsamında boş arama ekranını gerçek SQL katalog/takvim/fiyat/uygunluk verisine bağlamak.

Değişen dosyalar: Application'a availability kontratları; Infrastructure'a sınırlı/as-no-tracking SQL arama servisi ve development-only idempotent seeder; API'ye `/api/v1/availability` controller/rate limit/startup seed; frontend'e merkezi availability istemcisi, gerçek arama sonuçları ve responsive kartlar; backend/frontend davranış testleri ile API/roadmap/kullanım/sürüm belgeleri eklendi veya güncellendi.

Kararlar/varsayımlar: Örnek veri yalnız Development ayarıyla, deterministik kimliklerle ve PII olmadan ekleniyor; production yapılandırması seed'i açmıyor. Sonuçlar yalnız Approved işletme ve Published tesis/sahadan geliyor; haftalık/özel saat, fiyat kuralı, aktif blok ve rezervasyon slotu kontrol ediliyor. Bu tur arama ile sınırlı; rezervasyon oluşturma/ödeme yok. 60/90/120 dakika mevcut UI seçenekleri olarak korundu.

Çalıştırılan doğrulamalar ve sonuçları: Debug build 0 uyarı/0 hata. Release backend unit 4/4, integration 11/11; gerçek `Halisahadb` şema testi dahil 0 skip. Frontend lint/type-check başarılı; Vitest 12/12 ve production build başarılı. Kalıcı User Secrets `Encrypt=True;TrustServerCertificate=True` bağlantısıyla API startup seed'i tamamlandı; ready/OpenAPI/availability doğrudan ve Vite proxy üzerinden `200`, OpenAPI route mevcut ve Kadıköy sorgusu iki sonuç verdi. Gerçek tarayıcıda 2 saha, saat, fiyat, adres, kapasite ve özellik kartları doğrulandı.

Açık riskler: Örnek fiyatlar geliştirme verisidir; işletme CRUD/fiyat yönetimi henüz yok. Rezervasyon oluşturma ve concurrency akışı tamamlanmadığı için arama sonucu tahsis garantisi değildir. Açık kullanıcı Vite süreci korunmuş; eski API süreci güncel binary için kontrollü olarak yeniden başlatılmıştır.

Sonraki adım: Kullanıcı onayıyla tesis detay veya rezervasyon özet/oluşturma dikey dilimine geçmek.

## 2026-08-16 — Rezervasyon oluşturma, takip ve iptal

Amaç: `REQ-RES-001/002/003`, `REQ-PAY-001`, `REQ-AUD-001` ve `US-CUS-002/003` kapsamında müşterinin saha/tarih/saat seçerek rezervasyon yapmasını, kendi maçlarını panelinde izlemesini ve maç başlamadan iptal etmesini uçtan uca tamamlamak; İstanbul'un tüm ilçelerinde yeterli development verisi sağlamak.

Değişen dosyalar: Application'a rezervasyon ve ödeme gateway kontratları; Infrastructure'a transaction/idempotency/audit/fake ödeme destekli rezervasyon servisi, 39 ilçe × en az 3 saha idempotent seed'i ve arama hizalama kontrolü; API'ye müşteri yetkili reservation controller/rate limit; frontend'e create/list/cancel istemcisi, `Rezervasyonlarım` rotası/paneli, arama kartı rezervasyon eylemleri ve navigasyon eklendi. Backend integration ve frontend component testleri ile API sözleşmesi, ADR, kullanım kılavuzu, roadmap, sürüm notları ve denetim raporu güncellendi.

Kararlar/varsayımlar: “İstediği zaman iptal” veri bütünlüğü açısından maç başlangıcına kadar iptal olarak yorumlandı. Süreler 60/90/120 dakika ve başlangıçlar 30 dakikalık hizalı. Gerçek ödeme yapılmıyor; `FakeSandbox` onay/iade kayıtları kullanılıyor. Kullanıcı, fiyat ve işletme bilgileri JWT/SQL verisinden türetiliyor; istemci değerlerine güvenilmiyor. Yeni tablo/kolon gerekmediği için migration oluşturulmadı veya uygulanmadı.

Çalıştırılan doğrulamalar ve sonuçları: Backend build 0 uyarı/0 hata; unit 4/4 ve integration 14/14 geçti, gerçek `Halisahadb` şema testi 1/1 geçti. Frontend ESLint, TypeScript strict type-check, Vitest 14/14 ve production build başarılı. Gerçek SQL'de 39 ilçe ve her ilçede en az 3 saha doğrulandı. Doğrudan HTTP E2E health `200`, register `201`, Pendik availability `3`, create `201`, list `1`, cancel `200`, final durum `CancelledByCustomer`. Aynı slot için 50 paralel istekte `201=1`, `409=29`, `429=20`, `500=0`; aktif deneme rezervasyonu/slotu kalmadı. Tarayıcıda Pendik araması üç kart gösterdi, `Rezervasyonlarını takip et` anonim durumu doğru ve konsol temizdi.

Açık riskler: Slot hold/geri sayım, işletme tarafından yönetilen iptal-iade politikası ve gerçek ödeme sağlayıcısı kapsam dışıdır. Development katalog/fiyatları örnektir; işletme CRUD ekranları ayrı roadmap dilimidir. Tam kimlikli browser form gönderimi bu son QA turunda tekrarlanmadı; aynı akış component testi ve gerçek HTTP/SQL E2E ile doğrulandı.

Sonraki adım: İşletme katalog/fiyat yönetimi ile rezervasyon detay/ödeme geçmişi ekranını geliştirmek; ürün kararı alındığında hold ve iptal/iade politikasını eklemek.

## 2026-08-16 — Süresi dolan oturumda rezervasyon hatası düzeltmesi

Amaç: `REQ-RES-002`, `REQ-AUTH-002`, `REQ-AVL-001` ve `US-CUS-002` kapsamında **Bu saati rezerve et** eyleminin genel hata vermesine yol açan oturum/uygunluk sözleşmesi sorunlarını gerçek API ve SQL üzerinde düzeltmek.

Değişen dosyalar: Frontend merkezi API istemcisine eşzamanlı-safe tek refresh ve tek retry davranışı, auth context'e stale oturum kapatma bildirimi, rezervasyon kartına açıklayıcı oturum hatası eklendi. JWT `401/403` yanıtları RFC 7807 yapıldı; availability servisi geçmiş slotları filtreliyor. Frontend ve integration regresyon testleri ile API sözleşmesi, kullanım kılavuzu, sürüm notları ve denetim raporu güncellendi.

Kararlar/varsayımlar: `401` sonrası retry yalnız korumalı uçlarda, en fazla bir kez ve state-changing rezervasyon için aynı `Idempotency-Key` korunarak yapılıyor. Login/register `401` yanıtları refresh tetiklemiyor. Geçmiş saatlerin rezervasyonda reddedilmesi yerine arama sonucunda hiç sunulmaması tercih edildi. Migration veya şema değişikliği yok.

Çalıştırılan doğrulamalar ve sonuçları: Güncel tokenla gerçek API rezervasyonu `201` verdi. Geçersiz token `401 application/problem+json`, açıklayıcı başlık ve correlation ID döndürdü; aynı cookie oturumu refresh `200`, aynı anahtarla create `201`, cleanup cancel `200` ve final `CancelledByCustomer`. Backend build 0/0, unit 4/4, integration 15/15; gerçek SQL şema testi kalıcı `Encrypt=True` ortam TLS hatası nedeniyle başarısız. Frontend lint/type-check, Vitest 16/16 ve production build geçti. Tarayıcıda geçmiş saat boş sonuç, gelecekteki saat beş kart ve sıfır warning/error doğrulandı.

Açık riskler: Kalıcı User Secrets bağlantısındaki `Encrypt=True` TLS sorunu bu turda tekrarlandı; çalışan API yalnız loopback için süreç kapsamlı, kalıcı olmayan `Encrypt=False` override ile başlatıldı. Bir denetim komutu yerel JWT anahtarını terminal çıktısına açtığı için anahtar hemen döndürüldü; eski access tokenlar bilerek geçersiz oldu. Kullanıcı yeniden giriş yapmalıdır.

Sonraki adım: Uygulamayı yeniden girişle kullanıp rezervasyon oluşturmak; ayrı ortam çalışmasında SQL Server/Schannel TLS sorununu çözerek kalıcı `Encrypt=True` readiness ve gerçek şema testini tekrar geçirmek.

## 2026-08-16 — Rezervasyon iptal düğmesi kalıcılık düzeltmesi

Amaç: `REQ-CAN-001`, `REQ-AUD-001` ve `US-CUS-003` kapsamında iptal endpointi `200` döndürmesine rağmen rezervasyonun yeniden yüklemede tekrar onaylı görünmesine yol açan veri kalıcılığı hatasını düzeltmek.

Değişen dosyalar: `ReservationService.QueryMine` tracking/no-tracking kaynakları ayrıldı; cancel akışına mevcut başarılı refund koruması eklendi. `ReservationsPage` başarılı iptal sonucunu React Query önbelleğine anında yazıyor. Integration testi kalıcı status/timestamp, tek refund ve tekrarlı iptali doğrulayacak şekilde genişletildi; sürüm notu ve denetim raporu güncellendi.

Kararlar/varsayımlar: İptal edilen iş kaydı silinmiyor. Önceki bug nedeniyle status `Confirmed` kalmış fakat refund oluşmuş kayıtlar için kullanıcı düğmeye yeniden bastığında ikinci fake refund üretilmeden status onarılıyor. Yeni migration veya destructive veri işlemi yok.

Çalıştırılan doğrulamalar ve sonuçları: Hata gerçek SQL'de `CreateStatus=Confirmed`, `CancelResponse=CancelledByCustomer`, yeniden listede `ReloadedStatus=Confirmed` olarak yeniden üretildi. Düzeltme sonrası health `200`, register `201`, create `201`, cancel `200`, farklı anahtarla tekrar cancel `200`; yeniden liste `CancelledByCustomer` ve `canCancel=false`. SQL logunda `UPDATE [Reservations] SET [CancelledAtUtc], [Status], [UpdatedAtUtc]` görüldü. Backend build 0/0, unit 4/4, integration 15/15; frontend lint/type-check, Vitest 16/16 ve production build geçti.

Açık riskler: Kalıcı `Encrypt=True` yerel TLS ortam sorunu önceki kayıttaki gibi açıktır; çalışan API loopback süreç override'ı ile devam ediyor. Kullanıcının daha önce başarısız görünen iptali refund üretmişse güncel kodla düğmeye bir kez daha basması statusu güvenle onarır.

Sonraki adım: Kullanıcı `Rezervasyonlarım` sayfasını yenileyip ilgili rezervasyonda iptal düğmesine yeniden basabilir; durum **Sen iptal ettin** olarak kalıcı görünmelidir.

## 2026-08-16 — İşletme sayfasının kapsamdan kaldırılması

Amaç: Kullanıcı kararı doğrultusunda üst menüdeki **İşletmeler için** girişini ve iptal edilen `/isletme` frontend sayfasını kaldırmak.

Değişen dosyalar: `AppLayout` üst menüsü, frontend rota ağacı, ana sayfadaki işletme CTA/kartı, `PortalPage` audience tipi ve tek kart yerleşim stili güncellendi. Frontend regresyon testi, kullanım kılavuzu, sürüm notu ve çalışma günlüğü güncellendi.

Kararlar/varsayımlar: Yalnız public işletme tanıtım sayfası/rotası kaldırıldı; backend tenant/Business domain tabloları ve yönetim rotası korunuyor. `/isletme` artık standart 404 ekranına düşüyor. Migration veya veri değişikliği yok.

Çalıştırılan doğrulamalar ve sonuçları: Release backend build 0/0; unit 4/4, integration 15/15 ve SQL şema testi ortam değişkeni verilmediği için 1 kontrollü skip. Frontend lint/type-check, Vitest 17/17 ve production build başarılı. Tarayıcıda üst menü yalnız **Saha bul / Yönetim / Giriş yap** gösterdi; ana sayfada işletme CTA/kartı yok, `/isletme` 404 ve konsol temiz.

Açık riskler: Backend işletme domaini gelecekte yönetici/tenant gereksinimleri için korunuyor; ürün bütünüyle işletme rolünü de iptal edecekse ayrı kapsam ve veri/yetki kararı gerekir.

Sonraki adım: Public müşteri ve rezervasyon akışlarıyla devam etmek.

## 2026-08-16 — Yönetim sayfasının kapsamdan kaldırılması

Amaç: Kullanıcı kararı doğrultusunda üst menüdeki **Yönetim** girişini ve `/yonetim` frontend sayfasını kaldırmak.

Değişen dosyalar: `AppLayout` menüsü ve frontend rota ağacı güncellendi; artık kullanılmayan `PortalPage.tsx` ile portal stilleri kaldırıldı. Frontend regresyon testi, kullanım kılavuzu, sürüm notu ve çalışma günlüğü güncellendi.

Kararlar/varsayımlar: Yalnız public yönetim sayfası kaldırıldı; backend `SystemAdmin` rolü, authorization policy ve yönetim veri temeli korunuyor. `/yonetim` artık standart 404 ekranına düşüyor. Migration veya veri değişikliği yok.

Çalıştırılan doğrulamalar ve sonuçları: Backend unit 4/4, integration 15/15; SQL şema testi environment variable verilmediği için 1 kontrollü skip. Frontend lint/type-check, Vitest 18/18 ve production build başarılı. Build 167 modül, JS 441,23 kB (gzip 135,78), CSS 19,49 kB (gzip 4,88). Tarayıcı üst menüsü yalnız **Saha bul / Giriş yap** gösterdi; `/yonetim` 404 ve konsol temiz.

Açık riskler: Backend yönetici rolü/yetkileri güvenlik ve gelecek operasyon gereksinimleri için korunuyor; bunların da ürün kapsamından çıkarılması ayrı yetki/veri kararı gerektirir.

Sonraki adım: Müşteri kayıt, saha arama ve rezervasyon akışlarıyla devam etmek.
