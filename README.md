# Halı Saha Rezervasyon Sistemi (SahaGo)

Bu proje, kullanıcıların İstanbul'daki halı sahaları tarih, saat ve ilçe bilgisine göre arayabildiği; uygun bir zaman dilimini rezerve edip rezervasyonlarını takip edebildiği web tabanlı bir uygulamadır.

Uygulama iki ayrı istemciden oluşur: ASP.NET Core Web API iş kuralları, kimlik doğrulama ve SQL Server erişimini yürütür; React arayüzü ise kullanıcıların kayıt, giriş, saha arama ve rezervasyon işlemlerini yapmasını sağlar.

> Bu çalışma eğitim/proje teslimi amacıyla hazırlanmıştır. Ödeme akışı gerçek para transferi yapmaz; `FakeSandbox` ödeme sağlayıcısı kullanılır. SMS ve e-posta gönderilmez.

## İçindekiler

- [Özellikler](#özellikler)
- [Teknolojiler](#teknolojiler)
- [Mimari ve proje yapısı](#mimari-ve-proje-yapısı)
- [Gereksinimler](#gereksinimler)
- [Sıfırdan kurulum](#sıfırdan-kurulum)
- [SQL Server ve güvenli yapılandırma](#sql-server-ve-güvenli-yapılandırma)
- [Migration ve örnek veriler](#migration-ve-örnek-veriler)
- [Uygulamayı çalıştırma](#uygulamayı-çalıştırma)
- [Hızlı kullanım senaryosu](#hızlı-kullanım-senaryosu)
- [API özeti](#api-özeti)
- [Test ve kalite komutları](#test-ve-kalite-komutları)
- [Güvenlik ve veri bütünlüğü](#güvenlik-ve-veri-bütünlüğü)
- [Sorun giderme](#sorun-giderme)

## Özellikler

### Kullanıcı özellikleri

- E-posta veya Türkiye telefon numarası ile kayıt olma
- JWT tabanlı giriş, oturum yenileme ve güvenli çıkış
- İlçe, tarih, başlangıç saati ve süreye göre müsait saha arama
- İstanbul'un 39 ilçesi için geliştirme örnek saha kataloğu
- Uygun saatten rezervasyon oluşturma
- Kendi rezervasyonlarını görüntüleme
- Başlamamış rezervasyonları iptal etme
- Sistem sağlık durumunu arayüzden görme

### API ve iş kuralı özellikleri

- RFC 7807 Problem Details biçiminde hata yanıtları
- İstek/yanıt izlenebilirliği için `X-Correlation-Id`
- Kimlik doğrulama, müsaitlik ve rezervasyon uçlarında hız sınırlama
- OpenAPI dokümanı ve liveness/readiness sağlık uçları
- Geliştirme ortamında idempotent örnek katalog seed işlemi

### Mevcut yönetim altyapısı

Veri modeli ve API yetkilendirme altyapısı `Customer`, `BusinessOwner`, `BusinessStaff` ve `SystemAdmin` rollerini; işletme üyeliği ve işletme kapsamlı erişim kontrolünü içerir. Teslimdeki web arayüzü müşteri rezervasyon akışına odaklanır; işletme/yönetim için ayrı bir web paneli bulunmaz.

## Teknolojiler

| Katman | Kullanılan teknoloji |
| --- | --- |
| Backend | ASP.NET Core Web API, C# / .NET 10 |
| Veritabanı erişimi | Entity Framework Core 10 (Code First) |
| Veritabanı | Microsoft SQL Server |
| Kimlik doğrulama | ASP.NET Core Identity, JWT, refresh token |
| Frontend | React 19, TypeScript, Vite 8 |
| İstemci veri/form yönetimi | TanStack Query, React Hook Form, Zod |
| Test | xUnit, Vitest, React Testing Library |
| Kod kalitesi | ESLint, TypeScript strict mode |

Bu projede Next.js, Prisma, Tailwind CSS, shadcn/ui veya Auth.js kullanılmaz.

## Mimari ve proje yapısı

Proje modüler monolit yaklaşımını kullanır. React doğrudan veritabanına erişmez; tüm işlemler API üzerinden gerçekleştirilir.

```text
halisaha/
├── src/
│   ├── Halisaha.Api/              # HTTP API, controller, middleware, health check
│   ├── Halisaha.Application/      # Uygulama sözleşmeleri ve iş kuralları sınırı
│   ├── Halisaha.Domain/           # Domain varlıkları ve enumlar
│   └── Halisaha.Infrastructure/   # EF Core, Identity, SQL Server, seed, ödeme adaptörü
├── tests/
│   ├── Halisaha.UnitTests/        # Birim ve mimari testler
│   └── Halisaha.IntegrationTests/ # API, SQL şema ve rezervasyon entegrasyon testleri
├── frontend/                      # React + TypeScript + Vite kullanıcı arayüzü
├── docs/adr/                      # Mimari karar kayıtları
├── scripts/                       # Yerel SQL ve doğrulama yardımcı betikleri
├── Halisaha.sln                   # .NET çözümü
├── dotnet-tools.json              # Yerel dotnet-ef aracı
└── global.json                    # Kullanılan .NET SDK sürümü
```

## Gereksinimler

- .NET SDK `10.0.400` (proje `global.json` ile bu sürümü hedefler)
- Node.js `>= 24.19.0 < 25`
- npm
- Microsoft SQL Server (SQL Server Authentication ile erişilebilen bir örnek)
- SQL Server Management Studio (SSMS) — zorunlu değildir, veritabanını incelemek için önerilir

## Sıfırdan kurulum

1. ZIP dosyasını bir klasöre çıkarın ve PowerShell'i proje kökünde açın.

2. SQL Server üzerinde boş bir `Halisahadb` veritabanı oluşturun veya migration komutunun oluşturabilmesi için hesabınıza veritabanı oluşturma yetkisi verin.

3. Aşağıdaki bağlantı ve JWT ayarlarını .NET User Secrets'a ekleyin. Gerçek bağlantı bilgilerinizi yalnızca kendi bilgisayarınız için kullanın.

4. .NET paketlerini ve yerel EF aracını yükleyin:

```powershell
dotnet restore .\Halisaha.sln
dotnet tool restore
```

5. Migration'ları SQL Server'a uygulayın:

```powershell
dotnet ef database update --project .\src\Halisaha.Infrastructure\Halisaha.Infrastructure.csproj --startup-project .\src\Halisaha.Api\Halisaha.Api.csproj
```

6. Frontend bağımlılıklarını kurun:

```powershell
Set-Location .\frontend
npm ci
Set-Location ..
```

7. API ve frontend'i iki ayrı PowerShell penceresinde çalıştırın. Komutlar [Uygulamayı çalıştırma](#uygulamayı-çalıştırma) bölümündedir.

## SQL Server ve güvenli yapılandırma

Bu proje Microsoft SQL Server kullanır. Bağlantı metni kaynak koda veya frontend `.env` dosyasına yazılmaz; API projesinin .NET User Secrets deposunda tutulur.

> `DATABASE_URL="sqlserver://..."` bu projenin kullandığı bir ayar değildir. Bu biçim Prisma projelerine aittir. Burada doğru yapılandırma anahtarı `ConnectionStrings:Halisaha`dır.

### 1. SQL Server hesabını hazırlama

Kendi SQL Server örneğinizde SQL Server Authentication ile bağlanabilen bir kullanıcı oluşturun ve bu kullanıcıya `Halisahadb` üzerinde migration çalıştırabilecek şema yetkileri verin. Gerçek kullanıcı adı ve şifrenizi bu README'ye, Git'e veya ZIP teslimine eklemeyin.

Örnek veritabanı adı:

```text
Halisahadb
```

### 2. Connection string'i User Secrets'a ekleme

Proje kökünde aşağıdaki komutu, `KENDI_*` alanlarını kendi SQL Server bilgilerinizle değiştirerek çalıştırın:

```powershell
dotnet user-secrets set "ConnectionStrings:Halisaha" "Server=KENDI_SUNUCUNUZ;Database=Halisahadb;User Id=KENDI_KULLANICINIZ;Password=KENDI_SIFRENIZ;Encrypt=True;TrustServerCertificate=True" --project .\src\Halisaha.Api\Halisaha.Api.csproj
```

Örnek bağlantı söz dizimi:

```text
Server=KENDI_SUNUCUNUZ;Database=Halisahadb;User Id=KENDI_KULLANICINIZ;Password=KENDI_SIFRENIZ;Encrypt=True;TrustServerCertificate=True
```

`TrustServerCertificate=True` yerel geliştirme ortamında SQL Server sertifikasını doğrulamak için kullanılır. Üretim ortamında geçerli bir sertifika ve daha dar yetkili bir veritabanı hesabı tercih edilmelidir.

### 3. JWT imzalama anahtarını ekleme

API başlatılırken en az 32 baytlık `Authentication:Jwt:SigningKey` beklenir. Aşağıdaki PowerShell komutları güvenli rastgele bir anahtar üretip User Secrets'a kaydeder:

```powershell
$jwtBytes = [byte[]]::new(64)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($jwtBytes)
$jwtKey = [Convert]::ToBase64String($jwtBytes)
dotnet user-secrets set "Authentication:Jwt:SigningKey" $jwtKey --project .\src\Halisaha.Api\Halisaha.Api.csproj
Remove-Variable jwtBytes, jwtKey
```

### Frontend environment değişkeni

Frontend'de yalnızca aşağıdaki değişken bulunur:

```env
VITE_API_BASE_URL=
```

- Boş bırakıldığında Vite geliştirme sunucusu `/api` ve `/health` isteklerini yerel API'ye (`http://127.0.0.1:5198`) yönlendirir.
- API başka bir adreste çalışıyorsa `frontend/.env.example` dosyasını `frontend/.env` adıyla kopyalayın ve `VITE_API_BASE_URL` değerini örneğin `http://127.0.0.1:5198` olarak ayarlayın.

Windows PowerShell ile kopyalama:

```powershell
Copy-Item .\frontend\.env.example .\frontend\.env
```

`VITE_` ile başlayan değerler tarayıcıya gönderildiği için frontend `.env` dosyasına parola, connection string veya JWT anahtarı yazmayın.

## Migration ve örnek veriler

Veritabanı Entity Framework Core Code First migration'ları ile oluşturulur. Projede bulunan migration'lar:

- `20260816075928_InitialFoundation`
- `20260816090501_CreateCoreSchemaAndIdentity`

Ana tablolar; kullanıcı/rol ve oturum varlıkları (`Users`, `Roles`, `RefreshTokens`), işletme ve saha kataloğu (`Businesses`, `Facilities`, `Fields`, `Amenities`), takvim/fiyat (`WeeklyHours`, `SpecialHours`, `FieldBlocks`, `PriceRules`) ve rezervasyon/ödeme kayıtlarını (`Reservations`, `ReservationSlots`, `Payments`, `Refunds`) içerir.

API `Development` ortamında başlatıldığında `DevelopmentData:SeedSampleCatalog=true` ayarıyla örnek katalog verisi idempotent olarak yüklenir. Bu işlem İstanbul'un 39 ilçesine saha verileri ekler; sabit kullanıcı veya yönetici hesabı oluşturmaz. Test için `/kayit` sayfasından yeni bir kullanıcı oluşturun.

Veritabanı yedeği (`.bak`), SQL dump (`.sql`) veya Prisma migration'ı bu teslimde bulunmaz. Şema EF Core migration'ları, örnek katalog ise geliştirme seed işlemi ile yeniden oluşturulur.

## Uygulamayı çalıştırma

### API

Proje kökünde:

```powershell
dotnet run --project .\src\Halisaha.Api\Halisaha.Api.csproj
```

Geliştirme API adresi:

```text
http://127.0.0.1:5198
```

Kontrol uçları:

```text
http://127.0.0.1:5198/health/live
http://127.0.0.1:5198/health/ready
http://127.0.0.1:5198/openapi/v1.json
```

OpenAPI dokümanı yalnızca `Development` ortamında sunulur.

### Frontend

İkinci PowerShell penceresinde:

```powershell
Set-Location .\frontend
npm run dev
```

Vite varsayılan olarak aşağıdaki adreste çalışır:

```text
http://localhost:5173
```

Tarayıcıda bu adresi açın. Arayüz, API hazır olduğunda üst bölümde sistem durumunu gösterir.

## Hızlı kullanım senaryosu

1. API'nin `/health/ready` adresinin başarılı yanıt verdiğini doğrulayın.
2. `http://localhost:5173/kayit` adresinden e-posta veya telefon numarasıyla hesap oluşturun.
3. `/giris` adresinden giriş yapın.
4. `/saha-ara` sayfasında ilçe, tarih, başlangıç saati ve süre seçin.
5. Listelenen uygun bir saat için **Bu saati rezerve et** düğmesine basın.
6. `/rezervasyonlarim` ekranından oluşturulan rezervasyonu görüntüleyin.
7. Maç başlamadıysa aynı ekrandaki iptal işlemiyle rezervasyonu iptal edin.

## API özeti

| Alan | Uçlar |
| --- | --- |
| Kimlik doğrulama | `POST /api/v1/auth/register`, `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout`, `GET /api/v1/auth/me` |
| Müsaitlik | `GET /api/v1/availability` |
| Rezervasyon | `POST /api/v1/reservations`, `GET /api/v1/reservations`, `GET /api/v1/reservations/{id}`, `POST /api/v1/reservations/{id}/cancel` |
| Sağlık | `GET /health/live`, `GET /health/ready` |

Rezervasyon oluşturma ve iptal istekleri `Idempotency-Key` başlığını kullanır. Frontend bu anahtarı otomatik üretir; API'yi doğrudan kullanırken 8–100 karakter uzunluğunda bir anahtar gönderilmelidir.

## Test ve kalite komutları

### Backend

```powershell
dotnet build .\Halisaha.sln
dotnet test .\Halisaha.sln
```

### Frontend

```powershell
Set-Location .\frontend
npm run lint
npm run type-check
npm test
npm run build
```

## Güvenlik ve veri bütünlüğü

- Parolalar ASP.NET Core Identity tarafından hashlenir; düz metin parola saklanmaz.
- Access token kısa ömürlüdür; refresh token HttpOnly ve SameSite cookie ile yenilenir.
- API, kullanıcı kimliğini istemci isteğinden değil doğrulanmış oturumdan alır.
- Kullanıcı e-postası ve telefon numarası için SQL Server seviyesinde tekillik indeksleri vardır.
- Rezervasyon fiyatı ve işletme/saha bilgisi sunucu tarafında hesaplanır; istemciden gelen fiyata güvenilmez.
- Aynı saha ve saat için çifte rezervasyon; serializable transaction, uygunluk denetimi ve `ReservationSlots(FieldId, SlotStartUtc)` benzersiz indeksi ile engellenir.
- Rezervasyon oluşturma/iptal işlemleri idempotency kaydıyla tekrar eden isteklerden korunur.
- Kalıcı tarih-saat değerleri UTC tutulur; tesis saat dilimi ayrıca saklanır.
- Hata yanıtları correlation ID içerir. Parola, token ve connection string loglanmamalıdır.

## Sorun giderme

### API başlatılamıyor veya `ConnectionStrings:Halisaha is required` hatası alınıyor

`ConnectionStrings:Halisaha` değerinin API projesine User Secrets ile eklendiğini kontrol edin. Bu değer `frontend/.env` dosyasına değil, şu projeye eklenmelidir:

```text
src/Halisaha.Api/Halisaha.Api.csproj
```

### JWT imza anahtarı hatası alınıyor

`Authentication:Jwt:SigningKey` User Secrets değerinin en az 32 bayt olduğunu doğrulayın ve yukarıdaki rastgele anahtar üretme komutunu yeniden çalıştırın.

### Frontend “API bağlantısı bekleniyor” gösteriyor

Önce API'yi çalıştırın ve aşağıdaki adresin yanıt verdiğini kontrol edin:

```text
http://127.0.0.1:5198/health/ready
```

API farklı portta çalışıyorsa `frontend/.env` içindeki `VITE_API_BASE_URL` değerini bu adrese göre değiştirin veya `frontend/vite.config.ts` içindeki geliştirme proxy hedefini güncelleyin.

### Migration SQL Server'a bağlanamıyor

- SQL Server servisinin çalıştığını ve TCP/IP erişiminin açık olduğunu kontrol edin.
- Sunucu/örnek adını ve SQL Server Authentication bilgilerini doğrulayın.
- Kullanıcının `Halisahadb` üzerinde tablo, indeks ve migration oluşturma yetkisi olduğundan emin olun.
- Yerel sertifika sorunlarında `Encrypt=True;TrustServerCertificate=True` bağlantı metnini kullanın; `Encrypt=False` değerini kaynak koda eklemeyin.

### Node.js sürümü uyumsuz

Frontend, Node.js `24.19.0` veya daha yeni bir 24.x sürümü bekler. `node --version` ile sürümü kontrol edin; ardından `frontend` klasöründe `npm ci` komutunu yeniden çalıştırın.

## Teslim notu

Bu proje eğitim amaçlı hazırlanmıştır. Yukarıdaki adımlarla başka bir bilgisayarda SQL Server bağlantısı, migration ve örnek saha verileri yeniden kurulabilir. Veritabanı bağlantı bilgileri ve JWT anahtarı her kullanıcının kendi ortamında .NET User Secrets ile tanımlanmalıdır; gerçek parola, token veya connection string proje dosyalarına eklenmemelidir.
