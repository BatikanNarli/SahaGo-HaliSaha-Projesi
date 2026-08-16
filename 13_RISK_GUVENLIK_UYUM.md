# Risk, Güvenlik ve Uyum

| Risk | Kontrol |
|---|---|
| Tenant/IDOR veri sızıntısı | Server-side BusinessId scope ve negatif integration test |
| Çift rezervasyon | Tek transaction, SQL unique index, concurrency testi |
| Çift ödeme/webhook | İmza, event id unique ve idempotent handler |
| Kart verisi sızıntısı | Redirect/tokenize sağlayıcı; PAN/CVV alınmaz |
| Secret sızıntısı | User Secrets, CI secret store, secret scan |
| PII loglama | Allowlist yapılandırılmış log ve maskeleme |
| DST/zaman hatası | UTC persistence, tesis timezone ve edge testleri |
| İade kaydı kaybı | Ayrı refund state machine ve audit |
| Zararlı dosya | Tür/boyut doğrulama, yeniden adlandırma, izole storage |
| Supply-chain | Lockfile, bağımlılık ve güvenlik taraması |

Gerçek müşteri verisi, gerçek ödeme, SMS/e-posta veya production değişikliği ayrı insan onayı olmadan yapılmaz.
