# İş Akışları ve Durumlar

## Rezervasyon

| Kaynak | Hedef | Tetikleyici |
|---|---|---|
| PendingPayment | Confirmed | Ödeme yakalandı veya tesiste ödeme onayı |
| PendingPayment | Expired | Hold süresi doldu |
| PendingPayment | CancelledByCustomer | Müşteri iptali |
| PendingPayment | CancelledByBusiness | İşletme iptali |
| Confirmed | Completed | Operasyon tamamladı |
| Confirmed | NoShow | Operasyon gelmedi işaretledi |
| Confirmed | CancelledByCustomer | Politika dahilinde müşteri iptali |
| Confirmed | CancelledByBusiness | İşletme iptali |

Tabloda olmayan geçiş reddedilir. Rezervasyon ve slot tahsisi aynı SQL transaction içindedir.

## Ödeme

`Created → Pending → Authorized → Captured | Failed | Cancelled`

Captured ödeme için iade yolu: `Captured → RefundPending → Refunded | RefundFailed`. Refund hatası rezervasyon iptal kaydını geri almaz.

## İşletme

`Draft → PendingApproval → Approved | Rejected → Suspended`. Yalnız Approved işletmenin yayınlanmış tesisleri aranabilir.
