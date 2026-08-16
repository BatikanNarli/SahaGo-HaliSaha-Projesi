# Kullanım Kılavuzu

## Müşteri rezervasyonu

1. `/kayit` veya `/giris` üzerinden müşteri hesabıyla oturum açın.
2. `/saha-ara` ekranında İstanbul ilçesini, gelecekteki tarihi, 30 dakikaya hizalı saati ve 60/90/120 dakika süreyi seçin.
3. Uygun saha kartındaki **Bu saati rezerve et** düğmesine basın. Fiyat ve uygunluk API tarafından tekrar doğrulanır; fake/sandbox onayında kart üzerinde başarı mesajı görünür.
4. Üst menüden **Rezervasyonlarım** sayfasına gidin. Yaklaşan, tamamlanan ve iptal edilen maç özetleri ile saha/tarih/saat/fiyat bilgileri burada gösterilir.
5. Maç başlamadıysa **Rezervasyonu iptal et** düğmesi görünür. İptal sonrasında durum **Sen iptal ettin** olur ve saha slotu yeniden uygunluğa açılır.

Gerçek para alınmaz; geliştirme ortamı `FakeSandbox` ödeme/iade adaptörünü kullanır. Başlamış, tamamlanmış veya başka kullanıcıya ait rezervasyon iptal edilemez.

Access tokenın süresi rezervasyon sırasında dolarsa uygulama HttpOnly refresh cookie ile oturumu arka planda bir kez yeniler ve rezervasyon isteğini aynı tekrar-güvenli anahtarla sürdürür. Refresh oturumu da bitmişse genel form hatası yerine **Oturum süreniz doldu. Lütfen yeniden giriş yapın.** mesajı ve giriş bağlantısı gösterilir.

## Yerel geliştirme

Gereksinimler: .NET 10 SDK, Node.js 24 LTS ve SQL Server 2022. Bağlantı dizesi repoya yazılmaz; API projesinde `.NET User Secrets` ile `ConnectionStrings:Halisaha` anahtarına eklenir. En az 32 byte rastgele JWT anahtarı da aynı depoda `Authentication:Jwt:SigningKey` olarak tutulur; örnek veya gerçek anahtar repoya yazılmaz.

Yerel SQL Server yalnız loopback TCP (`127.0.0.1`/`::1`, port `1433`) dinleyecek şekilde `scripts/configure-local-sql.ps1` ile yönetici olarak yapılandırılır. Windows Firewall’da dış erişim kuralı açılmaz. CU veya SQL ağ protokolü değişikliğinden sonra bilgisayar yeniden başlatılır.

Backend için restore/build/test, frontend için install/lint/type-check/test çalıştırılır. API başladıktan sonra `/health/live`, SQL hazır olduğunda `/health/ready`, geliştirme ortamında `/openapi/v1.json` kontrol edilir.

## Frontend

`src/Halisaha.Api` için normal `dotnet run` profili API'yi `http://127.0.0.1:5198` üzerinde başlatır. `frontend` klasöründe `npm install` ve ardından `npm run dev` çalıştırılır; geliştirme sunucusu `/health` ve `/api` isteklerini aynı API adresine yönlendirir. Ayrı bir API adresi gerektiğinde secret içermeyen `VITE_API_BASE_URL` değeri, `frontend/.env.example` örnek alınarak yerel ortamda tanımlanabilir.

Kalite kapıları `npm run lint`, `npm run type-check`, `npm test`, `npm run build` ve `npm audit --audit-level=high` komutlarıdır.

Mevcut rotalar:

- `/`: müşteri odaklı ana sayfa ve canlı API readiness göstergesi.
- `/saha-ara`: gerçek `/api/v1/availability` verisiyle loading/empty/error/success durumlarını ve uygun saha kartlarını gösterir.
- `/rezervasyonlarim`: oturumdaki müşterinin yaklaşan/geçmiş/iptal edilmiş rezervasyonlarını ve uygun iptal eylemini gösterir.
- `/giris`: gerçek API üzerinden e-posta/telefon ile giriş, oturum yenileme ve çıkış.
- `/kayit`: e-posta ve/veya telefon ile Customer hesabı oluşturma. Türkiye telefonu için kullanıcı `+90` yazmaz; sabit ülke kodunun yanına `555 111 22 33` biçiminde numarasını girer.
- Bilinmeyen rotalar: erişilebilir 404 ekranı.

Arayüz masaüstü ve mobil kırılımlarda çalışır. Public işletme ve yönetim sayfaları ürün kapsamından kaldırılmıştır; ayrıntılı ödeme ekranları ilgili roadmap fazlarında backend API'leriyle birlikte tamamlanacaktır.

SQL şemasını doğrulayan integration test yalnız yerel bağlantı environment variable'ı sağlandığında çalışır: `HALISAHA_SQL_TEST_CONNECTION`. Bu değer komut geçmişine veya repoya kaydedilmemelidir. Test 29 tabloyu (`__EFMigrationsHistory` dahil), dört rolü, iki migration kaydını ve aktif rezervasyon slotu filtered unique indexini kontrol eder.

Development ortamında `DevelopmentData:SeedSampleCatalog=true` ayarı, gerçek müşteri/PII içermeyen deterministik örnek veriyi idempotent olarak ekler: İstanbul'un 39 ilçesinin her birinde en az üç yayınlanmış saha, özellikler, haftalık saatler ve fiyat kuralları bulunur. Production `appsettings.json` bu ayarı içermez. Örnek Kadıköy sonucu için gelecekteki tarih/saat seçip `Uygun sahaları ara` düğmesi kullanılır; geçmiş saatler sonuçlarda gösterilmez.
