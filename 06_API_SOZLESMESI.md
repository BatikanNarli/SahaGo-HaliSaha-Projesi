# API Sözleşmesi

Kök `/api/v1`, JSON `camelCase`, UTF-8 ve UTC ISO-8601’dir. Hatalar RFC 7807 ve `correlationId` içerir. Her yanıt `X-Correlation-Id` taşır.

## Planlanan kaynaklar

- `/auth`: register, login, refresh, logout, forgot/reset password.
- `/facilities`, `/fields`, `/amenities`: katalog ve tenant kapsamlı yönetim.
- `/availability`: tarih/saat/konum/özellik sorgusu.
- `/reservations`: create/list/detail/cancel/check-in/complete/no-show.
- `/payments/sessions`, `/payments/webhooks/{provider}` ve `/refunds`.
- `/reports` ve `/admin/businesses`.
- `/health/live`, `/health/ready`.

State-changing rezervasyon isteği `Idempotency-Key` gerektirir. Listeleme `page`, `pageSize`, `sort`; maksimum `pageSize=100`. OpenAPI istemci üretiminin tek kaynağıdır.

## Uygulanan kimlik API'si

| Method | Endpoint | Davranış |
|---|---|---|
| `POST` | `/api/v1/auth/register` | E-posta ve/veya E.164 telefon ile Customer hesabı oluşturur. |
| `POST` | `/api/v1/auth/login` | E-posta/telefon ve parola ile kısa ömürlü access token üretir. |
| `POST` | `/api/v1/auth/refresh` | HttpOnly cookie'yi tek kullanımlık rotation ile yeniler. |
| `POST` | `/api/v1/auth/logout` | Mevcut refresh tokenı iptal eder ve cookie'yi siler. |
| `GET` | `/api/v1/auth/me` | Bearer access token içindeki kullanıcı/rol kimliğini döndürür. |

Register isteği `displayName`, nullable `email`, nullable E.164 `phoneNumber` ve `password`; login isteği `identifier` ve `password` taşır. Türkiye arayüzü ülke kodunu sabit gösterir, kullanıcının girdiği yerel mobil numarayı API çağrısından önce E.164 biçimine dönüştürür. Public register rol seçimi kabul etmez ve yalnız `Customer` atar. Başarılı register/login/refresh yanıtı `accessToken`, `accessTokenExpiresAtUtc` ve `user` döndürür; ham refresh token JSON'a girmez. Refresh cookie `HttpOnly`, `SameSite=Strict` ve `/api/v1/auth` path kapsamındadır; production ortamında `Secure` zorunludur.

Kimlik uçları IP kapsamlı dakikada 10 istek rate limitine tabidir. Login hesabın varlığını açığa çıkarmayan genel `401`; duplicate register `409`; doğrulama `400`; limit aşımı açıklayıcı `429` RFC 7807 döndürür. Parola sıfırlama uçları henüz planlanan kapsamda, uygulanmış değildir.

## Uygulanan uygunluk API'si

`GET /api/v1/availability` anonim erişime açıktır ve `district`, `localDate` (`yyyy-MM-dd`), `localTime` (`HH:mm`) ile `durationMinutes` (`60|90|120`) query parametrelerini alır. Yalnız `Approved` işletmenin `Published` tesis/sahaları değerlendirilir. Tesisin haftalık/özel çalışma saati, aktif fiyat kuralı, çakışan `FieldBlock` ve aktif `ReservationSlot` kontrollerinden geçen sonuçlar en fazla 100 kayıtla döner.

Başarılı sonuç; saha/tesis/işletme adları, ilçe/adres, zemin, kapasite, açık-kapalı bilgisi, seçilen yerel aralık, UTC aralık, TRY fiyatı ve özellik adlarını içerir. Geçmiş başlangıç saatleri rezervasyon kartı olarak sunulmaz. Geçersiz süre/tarih/saat ile invalid veya ambiguous yerel saat `400` RFC 7807 ve alan bazlı `errors` döndürür. Uç IP kapsamlı dakikada 60 istekle sınırlıdır.

## Uygulanan rezervasyon API'si

Tüm rezervasyon uçları Bearer token ve `Customer` rolü gerektirir. Oluşturma ve iptal isteklerinde 8–100 karakterlik `Idempotency-Key` zorunludur.

| Method | Endpoint | Davranış |
|---|---|---|
| `POST` | `/api/v1/reservations` | `fieldId`, `startsAtUtc`, `endsAtUtc` ile rezervasyonu oluşturur; ilk sonuç `201`, aynı anahtar/istek tekrarı `200` döner. |
| `GET` | `/api/v1/reservations?page=1&pageSize=20` | Yalnız oturumdaki müşterinin rezervasyonlarını en fazla 100 kayıtlık sayfalarla döndürür. |
| `GET` | `/api/v1/reservations/{reservationId}` | Yalnız müşterinin kendi rezervasyon detayını döndürür; başka kullanıcı kaydı `404` olarak gizlenir. |
| `POST` | `/api/v1/reservations/{reservationId}/cancel` | Başlangıçtan önce uygun müşteri rezervasyonunu iptal eder, aktif slotları kapatır ve fake iade kaydı oluşturur. |

Oluşturma sırasında istemcinin gönderdiği fiyat/kullanıcı/işletme bilgisi kabul edilmez. API yayın, çalışma saati, blokaj ve fiyatı yeniden SQL'den doğrular; fiyat/politika snapshot'ı, `FakeSandbox` payment, 30 dakikalık slotlar, idempotency kaydı ve audit kaydı tek serializable transaction içinde yazılır. Slot yarışı, deadlock ve daha önce alınmış saat `409` Problem Details üretir.

Geçersiz veya süresi dolmuş Bearer token `401 application/problem+json`, açıklayıcı `title` ve `correlationId` döndürür. Frontend, HttpOnly refresh cookie ile access tokenı yalnız bir kez yeniler ve aynı state-changing isteği aynı `Idempotency-Key` ile tekrarlar; yenileme başarısızsa bellekteki oturumu kapatıp yeniden giriş ister.
