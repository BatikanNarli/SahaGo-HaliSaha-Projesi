# ADR-0005 — Rezervasyon politikası

Durum: Kısmen kabul edildi (16 Ağustos 2026).

- Kullanıcı İstanbul yerel saatinde tarih ve başlangıç saati seçer; başlangıçlar 30 dakikalık teknik slotlara hizalanır.
- MVP müşteri seçenekleri 60, 90 ve 120 dakikadır. Her rezervasyon SQL'de 30 dakikalık aktif `ReservationSlot` satırlarına ayrılır.
- Aynı saha/slot tahsisi serializable transaction ve filtered unique index ile korunur. SQL deadlock veya unique yarışını kaybeden istek `409` alır.
- Sağlayıcı seçilene kadar `IPaymentGateway` arkasındaki `FakeSandbox` adaptörü rezervasyonu anında `Confirmed` yapar; gerçek para hareketi oluşmaz.
- Müşteri yalnız kendi `PendingPayment` veya `Confirmed` rezervasyonunu başlangıçtan önce iptal edebilir. Fake capture için fake tam iade kaydı yazılır ve slotlar aynı transaction içinde serbest bırakılır.

Gerçek ödeme sağlayıcısı, hold süresi, gerçek para iade eşikleri ve işletme iptal politikası hâlâ ürün kararı bekler. Bu kararlar fake/sandbox davranışını production politikası olarak kullanma yetkisi vermez.
