import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/auth-state'
import { Icon } from '../components/Icon'
import { ApiError, cancelReservation, getMyReservations, type Reservation } from '../lib/api'

export function ReservationsPage() {
  const auth = useAuth()
  const queryClient = useQueryClient()
  const reservations = useQuery({
    queryKey: ['my-reservations', auth.user?.id],
    queryFn: getMyReservations,
    enabled: Boolean(auth.user) && !auth.isBootstrapping,
  })
  const cancel = useMutation({
    mutationFn: (reservationId: string) => cancelReservation(reservationId, crypto.randomUUID()),
    onSuccess: async (updated) => {
      queryClient.setQueryData<Reservation[]>(['my-reservations', auth.user?.id], (current) =>
        current?.map((reservation) => reservation.id === updated.id ? updated : reservation))
      await queryClient.invalidateQueries({ queryKey: ['my-reservations', auth.user?.id] })
    },
  })

  if (auth.isBootstrapping) {
    return <section className="page-section section-shell"><div className="results-state"><div className="results-icon"><Icon name="clock" /></div><h1>Rezervasyonların yükleniyor…</h1></div></section>
  }

  if (!auth.user) {
    return <section className="page-section section-shell"><div className="page-intro"><p className="eyebrow">Rezervasyonlarını takip et</p><h1>Maçların tek panelde.</h1><p>Yaklaşan ve geçmiş maçlarını görmek, uygun olanı iptal etmek için hesabına giriş yap.</p><Link className="button button--primary" to="/giris">Giriş yap <Icon name="arrow" /></Link></div></section>
  }

  const rows = reservations.data ?? []
  const upcomingCount = rows.filter((reservation) => reservation.canCancel).length
  const completedCount = rows.filter((reservation) => ['Completed', 'NoShow'].includes(reservation.status)).length
  const cancelledCount = rows.filter((reservation) => reservation.status.startsWith('Cancelled')).length

  return <section className="page-section section-shell reservations-page">
    <div className="page-intro"><p className="eyebrow">Rezervasyonlarını takip et</p><h1>Maç programın.</h1><p>{auth.user.displayName}, yaklaşan maçların ve rezervasyon geçmişin burada. Başlamamış rezervasyonları tek tıkla iptal edebilirsin.</p></div>
    <div aria-label="Rezervasyon özeti" className="reservation-stats">
      <div><span>Yaklaşan</span><strong>{upcomingCount}</strong></div>
      <div><span>Tamamlanan</span><strong>{completedCount}</strong></div>
      <div><span>İptal edilen</span><strong>{cancelledCount}</strong></div>
    </div>
    {reservations.isPending && <ReservationState title="Rezervasyonlar yükleniyor…" copy="SQL Server üzerindeki maç kayıtların getiriliyor." icon="clock" />}
    {reservations.error && <ReservationState title="Rezervasyonlar getirilemedi" copy={toMessage(reservations.error)} icon="shield" error />}
    {!reservations.isPending && !reservations.error && rows.length === 0 && <ReservationState title="Henüz rezervasyonun yok" copy="Uygun bir saha ve saat seçerek ilk maçını oluşturabilirsin." icon="calendar"><Link className="button button--primary" to="/saha-ara">Saha bul <Icon name="arrow" /></Link></ReservationState>}
    {rows.length > 0 && <div className="reservation-list">{rows.map((reservation) => <ReservationCard cancelError={cancel.variables === reservation.id ? cancel.error : null} cancelling={cancel.isPending && cancel.variables === reservation.id} key={reservation.id} onCancel={() => cancel.mutate(reservation.id)} reservation={reservation} />)}</div>}
  </section>
}

function ReservationCard({ reservation, cancelling, cancelError, onCancel }: { reservation: Reservation; cancelling: boolean; cancelError: Error | null; onCancel: () => void }) {
  return <article className="reservation-card">
    <div className="reservation-card__date"><strong>{formatDay(reservation.localDate)}</strong><span>{formatMonth(reservation.localDate)}</span></div>
    <div className="reservation-card__main">
      <div className="reservation-card__heading"><div><span>{reservation.businessName}</span><h2>{reservation.facilityName} · {reservation.fieldName}</h2></div><StatusBadge status={reservation.status} /></div>
      <div className="reservation-card__details"><span><Icon name="clock" /> {reservation.localStart}–{reservation.localEnd}</span><span><Icon name="location" /> {reservation.district}</span><strong>{formatPrice(reservation.total, reservation.currency)}</strong></div>
      <p>{reservation.addressLine}</p>
      {reservation.canCancel && <button className="button button--outline" disabled={cancelling} onClick={onCancel} type="button">{cancelling ? 'İptal ediliyor…' : 'Rezervasyonu iptal et'}</button>}
      {cancelError && <div aria-live="assertive" className="form-notice form-notice--error" role="alert">{toMessage(cancelError)}</div>}
    </div>
  </article>
}

function ReservationState({ title, copy, icon, error = false, children }: { title: string; copy: string; icon: 'calendar' | 'clock' | 'shield'; error?: boolean; children?: React.ReactNode }) {
  return <div className={`results-state ${error ? 'results-state--error' : ''}`} role={error ? 'alert' : undefined}><div className="results-icon"><Icon name={icon} /></div><h2>{title}</h2><p>{copy}</p>{children}</div>
}

function StatusBadge({ status }: { status: string }) {
  const labels: Record<string, string> = {
    Confirmed: 'Onaylandı', PendingPayment: 'Ödeme bekliyor', Completed: 'Tamamlandı', NoShow: 'Katılmadı', Expired: 'Süresi doldu',
    CancelledByCustomer: 'Sen iptal ettin', CancelledByBusiness: 'İşletme iptal etti',
  }
  return <span className={`reservation-status reservation-status--${status.toLowerCase()}`}>{labels[status] ?? status}</span>
}

function formatDay(date: string) { return new Intl.DateTimeFormat('tr-TR', { day: '2-digit', timeZone: 'Europe/Istanbul' }).format(new Date(`${date}T12:00:00+03:00`)) }
function formatMonth(date: string) { return new Intl.DateTimeFormat('tr-TR', { month: 'short', timeZone: 'Europe/Istanbul' }).format(new Date(`${date}T12:00:00+03:00`)) }
function formatPrice(price: number, currency: string) { return new Intl.NumberFormat('tr-TR', { style: 'currency', currency, maximumFractionDigits: 0 }).format(price) }
function toMessage(error: unknown) { return error instanceof ApiError ? error.message : 'API bağlantısı kurulamadı. Lütfen tekrar deneyin.' }
