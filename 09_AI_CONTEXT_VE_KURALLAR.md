# AI Context ve Kurallar

Her dilim Planlayıcı → Uygulayıcı → Denetleyici sırasındadır. Plan `REQ-*`, `US-*` ve `T-*` kayıtlarını belirtir; veri/API/güvenlik etkisini ve rollback yaklaşımını yazar.

Uygulayıcı yalnız onaylı küçük dikey dilimi değiştirir; test, migration, OpenAPI ve dokümanı birlikte günceller. Denetleyici tenant sınırı, veri sızıntısı, yarış durumu, idempotency, saat dilimi, secret ve gerçek test çıktısını bağımsız kontrol eder.

Critical/High bulguda dilim kabul edilmez. En fazla iki revizyon turundan sonra açık bulgu insan kararına çıkarılır.
