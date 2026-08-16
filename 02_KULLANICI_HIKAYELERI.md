# Kullanıcı Hikâyeleri ve Kabul Kriterleri

## Müşteri

### `US-CUS-001` — Uygun saha arama

Given yayınlanmış bir tesis ve uygun çalışma aralığı, when müşteri yerel tarih/saat ve ilçe ile arar, then yalnız blokajsız ve aktif rezervasyonu olmayan slotlar tesis saat diliminde fiyatıyla gösterilir. Geçersiz/ambiguous yerel saat RFC 7807 hatası üretir.

### `US-CUS-002` — Rezervasyon oluşturma

Given uygun slot, when aynı kullanıcı aynı `Idempotency-Key` ile isteği tekrarlar, then aynı sonuç döner. Farklı anahtarlarla aynı slota 50 eşzamanlı istekte en fazla biri başarılı olur.

### `US-CUS-003` — Geçmiş ve iptal

Given müşterinin rezervasyonu, when detay veya uygun iptal istenir, then yalnız kendi kaydı görünür ve dondurulmuş politika uygulanır. Başka kullanıcının kaydı açığa çıkmaz.

## İşletme

### `US-BIZ-001` — Katalog yönetimi

Given onaylı işletme üyeliği, when tesis/saha/takvim/fiyat düzenlenir, then değişiklik yalnız aynı `BusinessId` kapsamında yapılır ve audit kaydı oluşur.

### `US-BIZ-002` — Operasyon

Given staff yetkisi, when manuel rezervasyon, check-in veya no-show işlemi yapılır, then yalnız izinli tesiste geçerli durum geçişi uygulanır.

## Yönetici

### `US-ADM-001` — İşletme onayı ve denetim

Given SystemAdmin, when işletme incelenir, then onay durumu ve maskelenmiş audit kayıtları görülebilir; secret/token/kart verisi gösterilmez.

## İzlenebilirlik testleri

- `T-AUTH-001..004`: login/refresh/logout/tenant negatif yolları.
- `T-RES-001..004`: uygunluk, idempotency, 50 paralel istek, expiry.
- `T-PAY-001..004`: webhook imzası, çift olay, geç capture, refund hatası.
- `T-SEC-001..003`: yetkisiz erişim, başka tenant, log/secret kontrolü.
