# Analiz ve Gereksinimler

Bu belge MVP gereksinim kimliklerini tanımlar. Ayrıntılı kabul kriterleri `02_KULLANICI_HIKAYELERI.md` içindedir.

## Fonksiyonel gereksinimler

- `REQ-AUTH-001`: Kullanıcı e-posta veya telefonla kayıt olup güvenli biçimde oturum açabilmelidir.
- `REQ-AUTH-002`: Access token kısa ömürlü, refresh token döndürülebilir ve iptal edilebilir olmalıdır.
- `REQ-AUTH-003`: Customer, BusinessOwner, BusinessStaff ve SystemAdmin yetkileri server-side uygulanmalıdır.
- `REQ-TENANT-001`: İşletme kullanıcıları yalnız üyesi oldukları işletmenin verisine erişebilmelidir.
- `REQ-CAT-001`: Yetkili işletme tesis, saha, özellik, görsel ve yayın durumunu yönetebilmelidir.
- `REQ-SCH-001`: Haftalık çalışma saati, özel gün ve blokajlar tesis saat diliminde tanımlanmalıdır.
- `REQ-PRICE-001`: Fiyat sunucuda hesaplanmalı ve rezervasyonda snapshot olarak saklanmalıdır.
- `REQ-AVL-001`: Arama; konum, tarih/saat ve özellik filtreleriyle yalnız uygun slotları döndürmelidir.
- `REQ-RES-001`: Aynı `FieldId + SlotStartUtc` aktif slotu eşzamanlı isteklerde en fazla bir rezervasyona verilmelidir.
- `REQ-RES-002`: Rezervasyon oluşturma kullanıcı kapsamlı `Idempotency-Key` ile tekrar güvenli olmalıdır.
- `REQ-PAY-001`: Ödeme sağlayıcı arkasında çalışmalı; kart verisi uygulamaya girmemelidir.
- `REQ-PAY-002`: Webhook imzası doğrulanmalı ve olay kimliği tekilleştirilmelidir.
- `REQ-CAN-001`: İptal ve iade ayrı durum makineleri olmalı; iade hatası rezervasyon kaydını silmemelidir.
- `REQ-NOT-001`: Bildirimler outbox üzerinden, sağlayıcıdan bağımsız ve tekrar güvenli gönderilmelidir.
- `REQ-REP-001`: Yetkili roller doluluk, gelir ve iptal özetlerini tenant kapsamlı görebilmelidir.
- `REQ-AUD-001`: Kritik durum değişiklikleri secret/PII içermeyen audit kayıtları üretmelidir.

## Kalite gereksinimleri

- `REQ-NFR-001`: Kalıcı zamanlar UTC, para `decimal(18,2)` ve para birimi ISO 4217 olmalıdır.
- `REQ-NFR-002`: Normal yükte uygunluk p95 ≤ 800 ms, rezervasyon p95 ≤ 1.5 sn hedeflenir.
- `REQ-NFR-003`: Her API yanıtı correlation id taşımalı, hatalar RFC 7807 olmalıdır.
- `REQ-NFR-004`: Liste uçlarında `pageSize` en fazla 100 olmalıdır.
- `REQ-NFR-005`: Build, test, migration, OpenAPI uyumu ve güvenlik kontrolleri CI kapısıdır.

## Açık ürün kararları

Slot/süre, hold süresi, iptal-iade oranları, komisyon, işletme onayı ve production sağlayıcıları onaylanana kadar yapılandırma/arayüz arkasında kalır. Bu kararlar ilgili faz başlamadan kapanmalıdır.
