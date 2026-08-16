import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { z } from 'zod'
import { useAuth } from '../auth/auth-state'
import { Icon } from '../components/Icon'
import { ApiError, createReservation, searchAvailableFields, type AvailableField, type Reservation } from '../lib/api'

const districts = [
  'Adalar', 'Arnavutköy', 'Ataşehir', 'Avcılar', 'Bağcılar', 'Bahçelievler', 'Bakırköy', 'Başakşehir', 'Bayrampaşa',
  'Beşiktaş', 'Beykoz', 'Beylikdüzü', 'Beyoğlu', 'Büyükçekmece', 'Çatalca', 'Çekmeköy', 'Esenler', 'Esenyurt',
  'Eyüpsultan', 'Fatih', 'Gaziosmanpaşa', 'Güngören', 'Kadıköy', 'Kağıthane', 'Kartal', 'Küçükçekmece', 'Maltepe',
  'Pendik', 'Sancaktepe', 'Sarıyer', 'Silivri', 'Sultanbeyli', 'Sultangazi', 'Şile', 'Şişli', 'Tuzla', 'Ümraniye',
  'Üsküdar', 'Zeytinburnu',
] as const

const searchSchema = z.object({
  district: z.string().trim().min(2, 'En az 2 karakter girin.'),
  date: z.string().min(1, 'Tarih seçin.'),
  time: z.string().regex(/^([01]\d|2[0-3]):(00|30)$/, 'Saat 30 dakikalık dilimlerden biri olmalıdır.'),
  duration: z.enum(['60', '90', '120']),
})
type SearchValues = z.infer<typeof searchSchema>

export function SearchPage() {
  const [submitted, setSubmitted] = useState<SearchValues | null>(null)
  const search = useMutation({
    mutationFn: (values: SearchValues) => searchAvailableFields({
      district: values.district.trim(),
      localDate: values.date,
      localTime: values.time,
      durationMinutes: Number(values.duration),
    }),
  })
  const { register, handleSubmit, formState: { errors } } = useForm<SearchValues>({
    resolver: zodResolver(searchSchema),
    defaultValues: { district: 'Kadıköy', date: todayInIstanbul(), time: '20:00', duration: '60' },
  })

  const submit = (values: SearchValues) => {
    setSubmitted(values)
    search.mutate(values)
  }

  return <section className="page-section section-shell">
    <div className="page-intro"><p className="eyebrow">Uygun saha arama</p><h1>Maçın nerede?</h1><p>İstanbul’un 39 ilçesinden konum, tarih ve saati seç; gerçek müsaitlik ve fiyat üzerinden rezervasyon yap.</p></div>
    <form className="search-form" noValidate onSubmit={(event) => { void handleSubmit(submit)(event) }}>
      <div className="field-group field-group--wide"><label htmlFor="district"><Icon name="location" /> İlçe</label><select aria-describedby={errors.district ? 'district-error' : undefined} id="district" {...register('district')}>{districts.map((district) => <option key={district} value={district}>{district}</option>)}</select>{errors.district && <span className="field-error" id="district-error">{errors.district.message}</span>}</div>
      <div className="field-group"><label htmlFor="date"><Icon name="calendar" /> Tarih</label><input id="date" min={todayInIstanbul()} type="date" {...register('date')} />{errors.date && <span className="field-error">{errors.date.message}</span>}</div>
      <div className="field-group"><label htmlFor="time"><Icon name="clock" /> Saat</label><input id="time" step="1800" type="time" {...register('time')} />{errors.time && <span className="field-error">{errors.time.message}</span>}</div>
      <div className="field-group"><label htmlFor="duration">Süre</label><select id="duration" {...register('duration')}><option value="60">60 dakika</option><option value="90">90 dakika</option><option value="120">120 dakika</option></select></div>
      <button className="button button--primary search-submit" disabled={search.isPending} type="submit">{search.isPending ? 'Sahalar kontrol ediliyor…' : 'Uygun sahaları ara'} <Icon name="arrow" /></button>
    </form>
    <AvailabilityResults error={search.error} fields={search.data} isPending={search.isPending} submitted={submitted} />
  </section>
}

function AvailabilityResults({ error, fields, isPending, submitted }: { error: Error | null; fields: AvailableField[] | undefined; isPending: boolean; submitted: SearchValues | null }) {
  if (!submitted) return <div aria-live="polite" className="results-state"><div className="results-icon"><Icon name="location" /></div><h2>Aramaya hazırsın</h2><p>Örnek olarak Kadıköy ve bu akşam seçili; Pendik, Kartal veya başka bir İstanbul ilçesini seçebilirsin.</p></div>
  if (isPending) return <div aria-live="polite" className="results-state"><div className="results-icon"><Icon name="clock" /></div><h2>Sahalar kontrol ediliyor…</h2><p>Çalışma saatleri, bloklar ve aktif rezervasyonlar karşılaştırılıyor.</p></div>
  if (error) {
    const message = error instanceof ApiError ? error.message : 'API bağlantısı kurulamadı. Lütfen tekrar deneyin.'
    return <div aria-live="assertive" className="results-state results-state--error" role="alert"><div className="results-icon"><Icon name="shield" /></div><h2>Arama tamamlanamadı</h2><p>{message}</p></div>
  }
  if (!fields || fields.length === 0) return <div aria-live="polite" className="results-state"><div className="results-icon"><Icon name="calendar" /></div><h2>Bu aralıkta uygun saha bulunamadı</h2><p>{submitted.district} için saat veya süreyi değiştirerek yeniden deneyebilirsin.</p></div>

  return <div className="availability-section">
    <div className="results-summary"><div><p className="eyebrow">Canlı uygunluk</p><h2>{fields.length} uygun saha</h2></div><p>{submitted.district} · {submitted.date} · {submitted.time} · {submitted.duration} dakika</p></div>
    <div className="availability-grid">{fields.map((field) => <AvailabilityCard field={field} key={field.fieldId} />)}</div>
  </div>
}

function AvailabilityCard({ field }: { field: AvailableField }) {
  const auth = useAuth()
  const queryClient = useQueryClient()
  const reservation = useMutation({
    mutationFn: () => createReservation(field, crypto.randomUUID()),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['my-reservations', auth.user?.id] })
    },
  })

  return <article className="availability-card">
    <div className={`availability-card__visual ${field.isIndoor ? 'availability-card__visual--indoor' : ''}`}><span>{field.isIndoor ? 'Kapalı saha' : 'Açık saha'}</span><strong>{field.localStart}–{field.localEnd}</strong></div>
    <div className="availability-card__body">
      <div className="availability-card__heading"><div><span>{field.businessName}</span><h3>{field.facilityName} · {field.fieldName}</h3></div><strong>{formatPrice(field.price, field.currency)}</strong></div>
      <p><Icon name="location" /> {field.district} · {field.addressLine}</p>
      <div className="availability-card__facts"><span>{field.surfaceType}</span><span>{field.capacity} oyuncu</span></div>
      <div className="availability-card__amenities">{field.amenities.map((amenity) => <span key={amenity}>{amenity}</span>)}</div>
      <ReservationAction error={reservation.error} pending={reservation.isPending} reservation={reservation.data} signedIn={Boolean(auth.user)} submit={() => reservation.mutate()} />
    </div>
  </article>
}

function ReservationAction({ signedIn, pending, error, reservation, submit }: { signedIn: boolean; pending: boolean; error: Error | null; reservation: Reservation | undefined; submit: () => void }) {
  if (!signedIn) return <><Link className="button button--primary button--full availability-card__action" to="/giris">Giriş yap ve rezervasyon yap</Link>{error && <ReservationError error={error} />}</>
  if (reservation) return <div aria-live="polite" className="reservation-success"><Icon name="check" /><div><strong>Rezervasyon onaylandı</strong><span>{reservation.localDate} · {reservation.localStart}–{reservation.localEnd}</span></div><Link to="/rezervasyonlarim">Takip et</Link></div>
  return <><button className="button button--primary button--full availability-card__action" disabled={pending} onClick={submit} type="button">{pending ? 'Rezervasyon oluşturuluyor…' : 'Bu saati rezerve et'} <Icon name="arrow" /></button>{error && <ReservationError error={error} />}</>
}

function ReservationError({ error }: { error: Error }) {
  return <div aria-live="assertive" className="form-notice form-notice--error" role="alert">{error instanceof ApiError ? error.message : 'API bağlantısı kurulamadı. Lütfen tekrar deneyin.'}</div>
}

function formatPrice(price: number, currency: string) { return new Intl.NumberFormat('tr-TR', { style: 'currency', currency, maximumFractionDigits: 0 }).format(price) }
function todayInIstanbul() {
  const parts = new Intl.DateTimeFormat('en-CA', { timeZone: 'Europe/Istanbul', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date())
  const values = Object.fromEntries(parts.map((part) => [part.type, part.value]))
  return `${values.year}-${values.month}-${values.day}`
}
