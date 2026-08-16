# Halı Saha Rezervasyon Sistemi — Dokümantasyon Paketi

Bu klasör; ASP.NET Core/C#, React ve Microsoft SQL Server ile geliştirilecek halı saha rezervasyon sisteminin geliştirme öncesi bağlam paketidir. İnsan veya AI geliştirici, işe başlamadan önce önce `PROJECT.md`, ardından `AGENTS.md` dosyasını okumalıdır.

## Teknoloji kararı

- Backend: ASP.NET Core Web API, C#, Entity Framework Core
- Frontend: React + TypeScript
- Veritabanı: Microsoft SQL Server
- Mimari: Modüler monolit + Clean Architecture ilkeleri
- Kimlik: ASP.NET Core Identity, JWT access token + güvenli refresh token
- API: REST, `/api/v1` sürüm öneki, OpenAPI/Swagger
- Tarih/saat: Veritabanında UTC; arayüzde tesisin saat dilimi

Kesin framework sürümleri proje oluşturulurken destek durumu kontrol edilerek sabitlenir. Sürüm değişikliği bir ADR ile kaydedilir.

## Dosya haritası

| Dosya | Amaç |
|---|---|
| `PROJECT.md` | Projenin tek ana doğruluk kaynağı ve ürün sözleşmesi |
| `AGENTS.md` | AI geliştirici çalışma kuralları ve durdurma koşulları |
| `01_ANALIZ_VE_GEREKSINIMLER.md` | Problem, paydaş, kapsam ve fonksiyonel gereksinimler |
| `02_KULLANICI_HIKAYELERI.md` | Kullanıcı hikâyeleri ve Given/When/Then kabul kriterleri |
| `03_IS_AKISLARI_VE_DURUMLAR.md` | Rezervasyon/ödeme akışları ve durum makineleri |
| `04_TEKNIK_MIMARI.md` | Sistem bileşenleri, katmanlar ve kalite kararları |
| `05_VERI_MODELI_VE_ALAN_SOZLUGU.md` | Kavramsal veri modeli, alan tipleri ve iş kuralları |
| `06_API_SOZLESMESI.md` | Uç noktalar, istek/yanıt ve hata sözleşmesi |
| `07_TEST_STRATEJISI.md` | Test piramidi, kritik senaryolar, öncelik ve çıkış kriteri |
| `08_KULLANIM_KILAVUZU.md` | Kurulum ve rol bazlı ekran kullanımı |
| `09_AI_CONTEXT_VE_KURALLAR.md` | Planlayıcı–uygulayıcı–denetleyici agent düzeni |
| `10_PROMPT_KUTUPHANESI.md` | Tekrarlanabilir geliştirme ve denetim promptları |
| `11_AI_CALISMA_GUNLUGU.md` | AI kararları, insan düzeltmeleri ve kanıt günlüğü |
| `12_AI_DENETIM_RAPORU.md` | Gereksinim/kod/test halüsinasyon denetimi şablonu |
| `13_RISK_GUVENLIK_UYUM.md` | Kişisel veri, ödeme, log, izin ve risk listesi |
| `14_YONETICI_OZETI.md` | Ne yapılacak, riskler ve başarı ölçüleri |
| `15_ROADMAP_VE_GOREVLER.md` | Fazlar, bağımlılıklar ve Definition of Done |
| `16_DEMO_SENARYOSU.md` | 5 dakikalık mutlu yol ve hata senaryosu |
| `17_SURUM_NOTLARI.md` | Sürümlere göre eklenen/değişen/düzeltilenler |

## Öncelik sırası

1. Kimlik ve rol yönetimi
2. Tesis/saha/saat planı yönetimi
3. Uygunluk sorgusu ve çakışmasız rezervasyon
4. Ödeme ve idempotency
5. İptal/iade
6. Bildirimler
7. Raporlama
8. İsteğe bağlı AI öneri ve SSS özellikleri

## Ana kalite kapısı

Bir iş “bitti” sayılmaz; gereksinim, kod, migration, test, API sözleşmesi ve ilgili doküman birlikte güncellenmedikçe pull request kapatılamaz. Kritik çakışma, çift ödeme, yetki aşımı veya kişisel veri sızıntısı riski varken sürüm yayınlanmaz.
