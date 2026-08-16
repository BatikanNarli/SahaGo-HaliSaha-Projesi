import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { appRoutes } from './app/routes'
import { AuthProvider } from './auth/AuthContext'

function renderRoute(route: string) {
  const router = createMemoryRouter(appRoutes, { initialEntries: [route] })
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(<QueryClientProvider client={queryClient}><AuthProvider><RouterProvider router={router} /></AuthProvider></QueryClientProvider>)
}

const availableFieldFixture = {
  fieldId: '4cd031b0-04e5-46b9-b99a-97315c31f021',
  facilityId: 'e0972adf-fcc4-433c-9890-3381cb06084d',
  businessName: 'İstanbul Spor Tesisleri',
  facilityName: 'Kadıköy Arena',
  fieldName: 'Arena Kapalı',
  district: 'Kadıköy',
  addressLine: 'Örnek Mahallesi, Kadıköy',
  surfaceType: 'Sentetik çim',
  capacity: 12,
  isIndoor: true,
  price: 1750,
  currency: 'TRY',
  startsAtUtc: '2026-08-20T17:00:00Z',
  endsAtUtc: '2026-08-20T18:00:00Z',
  localStart: '20:00',
  localEnd: '21:00',
  amenities: ['Duş', 'Otopark'],
}

const reservationFixture = {
  id: '60cc6322-0e56-4bd4-bc69-f5664db183f9',
  status: 'Confirmed',
  fieldId: availableFieldFixture.fieldId,
  businessName: availableFieldFixture.businessName,
  facilityName: availableFieldFixture.facilityName,
  fieldName: availableFieldFixture.fieldName,
  district: availableFieldFixture.district,
  addressLine: availableFieldFixture.addressLine,
  startsAtUtc: availableFieldFixture.startsAtUtc,
  endsAtUtc: availableFieldFixture.endsAtUtc,
  localDate: '2026-08-20',
  localStart: '20:00',
  localEnd: '21:00',
  total: 1750,
  currency: 'TRY',
  canCancel: true,
  createdAtUtc: '2026-08-16T10:00:00Z',
  cancelledAtUtc: null,
}

describe('frontend routes', () => {
  it('renders the customer-first landing page', async () => {
    renderRoute('/')
    expect(screen.getByRole('heading', { name: /sahan hazır/i })).toBeInTheDocument()
    expect(screen.getAllByRole('link', { name: /^saha bul/i }).some((link) => link.getAttribute('href') === '/saha-ara')).toBe(true)
    expect(screen.queryByRole('link', { name: /işletme|sahanı sahago|yönetim/i })).not.toBeInTheDocument()
    expect(await screen.findByText('Sistem hazır')).toBeInTheDocument()
  })

  it('does not expose the cancelled business page route', () => {
    renderRoute('/isletme')
    expect(screen.getByRole('heading', { name: /çizgilerin dışında/i })).toBeInTheDocument()
  })

  it('does not expose the cancelled management page route', () => {
    renderRoute('/yonetim')
    expect(screen.getByRole('heading', { name: /çizgilerin dışında/i })).toBeInTheDocument()
  })

  it('shows a clear degraded state when the API health check fails', async () => {
    vi.mocked(fetch).mockImplementation((input) => requestUrl(input).endsWith('/health/ready')
      ? Promise.resolve(new Response('Unhealthy', { status: 503 }))
      : Promise.resolve(new Response(JSON.stringify({ title: 'Oturum yenilenemedi.' }), { status: 401, headers: { 'Content-Type': 'application/problem+json' } })))
    renderRoute('/')
    expect(await screen.findByText('API bağlantısı bekleniyor')).toBeInTheDocument()
  })

  it('validates criteria and renders fields returned by the availability API', async () => {
    vi.mocked(fetch).mockImplementation((input) => requestUrl(input).includes('/api/v1/availability?')
      ? Promise.resolve(new Response(JSON.stringify([availableFieldFixture]), { status: 200, headers: { 'Content-Type': 'application/json' } }))
      : Promise.resolve(new Response(JSON.stringify({ title: 'Oturum yenilenemedi.' }), { status: 401, headers: { 'Content-Type': 'application/problem+json' } })))
    renderRoute('/saha-ara')
    fireEvent.change(screen.getByLabelText(/^İlçe$/i), { target: { value: '' } })
    fireEvent.click(screen.getByRole('button', { name: /uygun sahaları ara/i }))
    expect(await screen.findByText('En az 2 karakter girin.')).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText(/^İlçe$/i), { target: { value: 'Kadıköy' } })
    fireEvent.change(screen.getByLabelText(/^Tarih$/i), { target: { value: '2026-08-20' } })
    fireEvent.click(screen.getByRole('button', { name: /uygun sahaları ara/i }))
    expect(await screen.findByRole('heading', { name: /1 uygun saha/i })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /Kadıköy Arena · Arena Kapalı/i })).toBeInTheDocument()
    expect(screen.getByText(/₺1\.750/)).toBeInTheDocument()
    expect(vi.mocked(fetch).mock.calls.some(([input]) => requestUrl(input).includes('district=Kad%C4%B1k%C3%B6y'))).toBe(true)
  })

  it('shows loading while the availability API is working', async () => {
    vi.mocked(fetch).mockImplementation((input) => requestUrl(input).includes('/api/v1/availability?')
      ? new Promise<Response>(() => undefined)
      : Promise.resolve(new Response(JSON.stringify({ title: 'Oturum yenilenemedi.' }), { status: 401, headers: { 'Content-Type': 'application/problem+json' } })))
    renderRoute('/saha-ara')
    fireEvent.click(screen.getByRole('button', { name: /uygun sahaları ara/i }))
    expect(await screen.findByRole('heading', { name: /sahalar kontrol ediliyor/i })).toBeInTheDocument()
  })

  it('shows an empty result without inventing field data', async () => {
    vi.mocked(fetch).mockImplementation((input) => requestUrl(input).includes('/api/v1/availability?')
      ? Promise.resolve(new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json' } }))
      : Promise.resolve(new Response(JSON.stringify({ title: 'Oturum yenilenemedi.' }), { status: 401, headers: { 'Content-Type': 'application/problem+json' } })))
    renderRoute('/saha-ara')
    fireEvent.click(screen.getByRole('button', { name: /uygun sahaları ara/i }))
    expect(await screen.findByRole('heading', { name: /uygun saha bulunamadı/i })).toBeInTheDocument()
  })

  it('shows the API problem when availability search fails', async () => {
    vi.mocked(fetch).mockImplementation((input) => requestUrl(input).includes('/api/v1/availability?')
      ? Promise.resolve(new Response(JSON.stringify({ title: 'Arama servisi geçici olarak kullanılamıyor.' }), { status: 500, headers: { 'Content-Type': 'application/problem+json' } }))
      : Promise.resolve(new Response(JSON.stringify({ title: 'Oturum yenilenemedi.' }), { status: 401, headers: { 'Content-Type': 'application/problem+json' } })))
    renderRoute('/saha-ara')
    fireEvent.click(screen.getByRole('button', { name: /uygun sahaları ara/i }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Arama servisi geçici olarak kullanılamıyor.')
  })

  it('creates an authenticated reservation from an available field', async () => {
    let reservationHeaders: Headers | undefined
    vi.mocked(fetch).mockImplementation((input, init) => {
      const url = requestUrl(input)
      if (url.endsWith('/api/v1/auth/refresh')) return Promise.resolve(jsonResponse(authSessionFixture))
      if (url.includes('/api/v1/availability?')) return Promise.resolve(jsonResponse([availableFieldFixture]))
      if (url.endsWith('/api/v1/reservations') && init?.method === 'POST') {
        reservationHeaders = new Headers(init.headers)
        return Promise.resolve(jsonResponse(reservationFixture, 201))
      }
      return Promise.resolve(jsonResponse({ title: 'Beklenmeyen istek.' }, 500))
    })

    renderRoute('/saha-ara')
    fireEvent.change(screen.getByLabelText(/^Tarih$/i), { target: { value: '2026-08-20' } })
    fireEvent.click(screen.getByRole('button', { name: /uygun sahaları ara/i }))
    fireEvent.click(await screen.findByRole('button', { name: /bu saati rezerve et/i }))

    expect(await screen.findByText('Rezervasyon onaylandı')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Takip et' })).toHaveAttribute('href', '/rezervasyonlarim')
    expect(reservationHeaders?.get('Authorization')).toBe('Bearer access-token')
    expect(reservationHeaders?.get('Idempotency-Key')).toBeTruthy()
  })

  it('refreshes an expired access token and safely retries the reservation once', async () => {
    let refreshCount = 0
    const reservationHeaders: Headers[] = []
    vi.mocked(fetch).mockImplementation((input, init) => {
      const url = requestUrl(input)
      if (url.endsWith('/api/v1/auth/refresh')) {
        refreshCount += 1
        return Promise.resolve(jsonResponse({
          ...authSessionFixture,
          accessToken: refreshCount === 1 ? 'expired-access-token' : 'renewed-access-token',
        }))
      }
      if (url.includes('/api/v1/availability?')) return Promise.resolve(jsonResponse([availableFieldFixture]))
      if (url.endsWith('/api/v1/reservations') && init?.method === 'POST') {
        reservationHeaders.push(new Headers(init.headers))
        return Promise.resolve(reservationHeaders.length === 1
          ? new Response(null, { status: 401 })
          : jsonResponse(reservationFixture, 201))
      }
      return Promise.resolve(jsonResponse({ title: 'Beklenmeyen istek.' }, 500))
    })

    renderRoute('/saha-ara')
    fireEvent.change(screen.getByLabelText(/^Tarih$/i), { target: { value: '2026-08-20' } })
    fireEvent.click(screen.getByRole('button', { name: /uygun sahaları ara/i }))
    fireEvent.click(await screen.findByRole('button', { name: /bu saati rezerve et/i }))

    expect(await screen.findByText('Rezervasyon onaylandı')).toBeInTheDocument()
    expect(refreshCount).toBe(2)
    expect(reservationHeaders).toHaveLength(2)
    expect(reservationHeaders[0].get('Authorization')).toBe('Bearer expired-access-token')
    expect(reservationHeaders[1].get('Authorization')).toBe('Bearer renewed-access-token')
    expect(reservationHeaders[1].get('Idempotency-Key')).toBe(reservationHeaders[0].get('Idempotency-Key'))
  })

  it('ends the stale session with a clear message when token refresh fails', async () => {
    let refreshCount = 0
    vi.mocked(fetch).mockImplementation((input, init) => {
      const url = requestUrl(input)
      if (url.endsWith('/api/v1/auth/refresh')) {
        refreshCount += 1
        return Promise.resolve(refreshCount === 1
          ? jsonResponse({ ...authSessionFixture, accessToken: 'expired-access-token' })
          : new Response(null, { status: 401 }))
      }
      if (url.includes('/api/v1/availability?')) return Promise.resolve(jsonResponse([availableFieldFixture]))
      if (url.endsWith('/api/v1/reservations') && init?.method === 'POST') return Promise.resolve(new Response(null, { status: 401 }))
      return Promise.resolve(jsonResponse({ title: 'Beklenmeyen istek.' }, 500))
    })

    renderRoute('/saha-ara')
    fireEvent.change(screen.getByLabelText(/^Tarih$/i), { target: { value: '2026-08-20' } })
    fireEvent.click(screen.getByRole('button', { name: /uygun sahaları ara/i }))
    fireEvent.click(await screen.findByRole('button', { name: /bu saati rezerve et/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Oturum süreniz doldu. Lütfen yeniden giriş yapın.')
    expect(screen.getByRole('link', { name: /giriş yap ve rezervasyon yap/i })).toBeInTheDocument()
  })

  it('lists and cancels the signed-in customer reservation', async () => {
    let cancelled = false
    vi.mocked(fetch).mockImplementation((input, init) => {
      const url = requestUrl(input)
      if (url.endsWith('/api/v1/auth/refresh')) return Promise.resolve(jsonResponse(authSessionFixture))
      if (url.includes('/api/v1/reservations?page=')) return Promise.resolve(jsonResponse([
        cancelled ? { ...reservationFixture, status: 'CancelledByCustomer', canCancel: false, cancelledAtUtc: '2026-08-16T11:00:00Z' } : reservationFixture,
      ]))
      if (url.endsWith(`/api/v1/reservations/${reservationFixture.id}/cancel`) && init?.method === 'POST') {
        cancelled = true
        return Promise.resolve(jsonResponse({ ...reservationFixture, status: 'CancelledByCustomer', canCancel: false, cancelledAtUtc: '2026-08-16T11:00:00Z' }))
      }
      return Promise.resolve(jsonResponse({ title: 'Beklenmeyen istek.' }, 500))
    })

    renderRoute('/rezervasyonlarim')
    expect(await screen.findByRole('heading', { name: /Kadıköy Arena · Arena Kapalı/i })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /rezervasyonu iptal et/i }))
    expect(await screen.findByText('Sen iptal ettin')).toBeInTheDocument()
  })

  it('logs in through the identity API and renders the returned customer', async () => {
    vi.mocked(fetch).mockImplementation((input) => requestUrl(input).endsWith('/api/v1/auth/login')
      ? Promise.resolve(new Response(JSON.stringify({ accessToken: 'access-token', accessTokenExpiresAtUtc: '2026-08-16T12:00:00Z', user: { id: '9cdf1858-b836-4a98-9e7e-2b0e9e791d0e', displayName: 'Test Oyuncu', email: 'oyuncu@example.com', phoneNumber: null, roles: ['Customer'] } }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
      : Promise.resolve(new Response(JSON.stringify({ title: 'Oturum yenilenemedi.' }), { status: 401, headers: { 'Content-Type': 'application/problem+json' } })))
    renderRoute('/giris')
    await screen.findByRole('heading', { name: 'Giriş yap' })
    fireEvent.change(screen.getByLabelText('E-posta veya telefon'), { target: { value: 'oyuncu@example.com' } })
    fireEvent.change(screen.getByLabelText('Parola'), { target: { value: 'guvenli-parola' } })
    fireEvent.click(screen.getByRole('button', { name: /^giriş yap/i }))
    await waitFor(() => expect(screen.getByRole('heading', { name: /hoş geldin, test oyuncu/i })).toBeInTheDocument())
    expect(screen.getByText('Customer')).toBeInTheDocument()
  })

  it('shows the API problem when login credentials are rejected', async () => {
    vi.mocked(fetch).mockImplementation((input) => requestUrl(input).endsWith('/api/v1/auth/login')
      ? Promise.resolve(new Response(JSON.stringify({ title: 'Kullanıcı bilgileri geçersiz.' }), { status: 401, headers: { 'Content-Type': 'application/problem+json' } }))
      : Promise.resolve(new Response(JSON.stringify({ title: 'Oturum yenilenemedi.' }), { status: 401, headers: { 'Content-Type': 'application/problem+json' } })))
    renderRoute('/giris')
    await screen.findByRole('heading', { name: 'Giriş yap' })
    fireEvent.change(screen.getByLabelText('E-posta veya telefon'), { target: { value: 'oyuncu@example.com' } })
    fireEvent.change(screen.getByLabelText('Parola'), { target: { value: 'Yanlis12345' } })
    fireEvent.click(screen.getByRole('button', { name: /^giriş yap/i }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Kullanıcı bilgileri geçersiz.')
  })

  it('validates customer registration before calling the API', async () => {
    renderRoute('/kayit')
    await screen.findByRole('heading', { name: /hesap oluştur/i })
    fireEvent.click(screen.getByRole('button', { name: /ücretsiz hesap oluştur/i }))
    expect(await screen.findByText(/ad soyad en az 2 karakter/i)).toBeInTheDocument()
    expect(screen.getByText(/e-posta veya telefon numarasından en az birini/i)).toBeInTheDocument()
  })

  it('registers a Turkish mobile number without asking the user to type the country code', async () => {
    let requestBody: Record<string, unknown> | null = null
    vi.mocked(fetch).mockImplementation((input, init) => {
      if (requestUrl(input).endsWith('/api/v1/auth/register')) {
        if (typeof init?.body !== 'string') throw new Error('Register request body must be JSON text.')
        requestBody = JSON.parse(init.body) as Record<string, unknown>
        return Promise.resolve(new Response(JSON.stringify({ accessToken: 'access-token', accessTokenExpiresAtUtc: '2026-08-16T12:00:00Z', user: { id: '9cdf1858-b836-4a98-9e7e-2b0e9e791d0e', displayName: 'Telefonlu Oyuncu', email: null, phoneNumber: '+905551112233', roles: ['Customer'] } }), { status: 201, headers: { 'Content-Type': 'application/json' } }))
      }
      return Promise.resolve(new Response(JSON.stringify({ title: 'Oturum yenilenemedi.' }), { status: 401, headers: { 'Content-Type': 'application/problem+json' } }))
    })

    renderRoute('/kayit')
    await screen.findByRole('heading', { name: /hesap oluştur/i })
    fireEvent.change(screen.getByLabelText('Ad soyad'), { target: { value: 'Telefonlu Oyuncu' } })
    fireEvent.change(screen.getByLabelText('Telefon'), { target: { value: '0555 111 22 33' } })
    fireEvent.change(screen.getByLabelText(/^Parola$/), { target: { value: 'Guvenli1234' } })
    fireEvent.change(screen.getByLabelText('Parola tekrar'), { target: { value: 'Guvenli1234' } })
    fireEvent.click(screen.getByRole('button', { name: /ücretsiz hesap oluştur/i }))

    expect(await screen.findByRole('heading', { name: /hoş geldin, telefonlu oyuncu/i })).toBeInTheDocument()
    expect(requestBody).toMatchObject({ phoneNumber: '+905551112233' })
  })

  it('explains rate limiting instead of showing a generic registration error', async () => {
    vi.mocked(fetch).mockImplementation((input) => requestUrl(input).endsWith('/api/v1/auth/register')
      ? Promise.resolve(new Response(null, { status: 429 }))
      : Promise.resolve(new Response(JSON.stringify({ title: 'Oturum yenilenemedi.' }), { status: 401, headers: { 'Content-Type': 'application/problem+json' } })))

    renderRoute('/kayit')
    await screen.findByRole('heading', { name: /hesap oluştur/i })
    fireEvent.change(screen.getByLabelText('Ad soyad'), { target: { value: 'Test Oyuncu' } })
    fireEvent.change(screen.getByLabelText('E-posta'), { target: { value: 'yeni@example.test' } })
    fireEvent.change(screen.getByLabelText(/^Parola$/), { target: { value: 'Guvenli1234' } })
    fireEvent.change(screen.getByLabelText('Parola tekrar'), { target: { value: 'Guvenli1234' } })
    fireEvent.click(screen.getByRole('button', { name: /ücretsiz hesap oluştur/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Çok fazla deneme yapıldı')
  })

  it('renders an accessible not-found route', () => {
    renderRoute('/olmayan-sayfa')
    expect(screen.getByRole('heading', { name: /çizgilerin dışında/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /ana sayfaya dön/i })).toHaveAttribute('href', '/')
  })
})

function requestUrl(input: RequestInfo | URL) {
  return typeof input === 'string' ? input : input instanceof URL ? input.href : input.url
}

const authSessionFixture = {
  accessToken: 'access-token',
  accessTokenExpiresAtUtc: '2026-08-16T12:00:00Z',
  user: { id: '9cdf1858-b836-4a98-9e7e-2b0e9e791d0e', displayName: 'Test Oyuncu', email: 'oyuncu@example.com', phoneNumber: null, roles: ['Customer'] },
}

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}
