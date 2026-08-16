export type SystemHealth = {
  correlationId: string | null
  status: 'ready'
}

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ?? ''
let accessToken: string | null = null

export type AuthUser = {
  id: string
  displayName: string
  email: string | null
  phoneNumber: string | null
  roles: string[]
}

export type AuthSession = {
  accessToken: string
  accessTokenExpiresAtUtc: string
  user: AuthUser
}

let refreshSessionPromise: Promise<AuthSession> | null = null
const authenticationExpiredListeners = new Set<() => void>()

export type AvailableField = {
  fieldId: string
  facilityId: string
  businessName: string
  facilityName: string
  fieldName: string
  district: string
  addressLine: string
  surfaceType: string
  capacity: number
  isIndoor: boolean
  price: number
  currency: string
  startsAtUtc: string
  endsAtUtc: string
  localStart: string
  localEnd: string
  amenities: string[]
}

export type Reservation = {
  id: string
  status: string
  fieldId: string
  businessName: string
  facilityName: string
  fieldName: string
  district: string
  addressLine: string
  startsAtUtc: string
  endsAtUtc: string
  localDate: string
  localStart: string
  localEnd: string
  total: number
  currency: string
  canCancel: boolean
  createdAtUtc: string
  cancelledAtUtc: string | null
}

type ProblemDetails = {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
  correlationId?: string
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problemMessage(status, problem))
    this.status = status
    this.problem = problem
  }
}

function problemMessage(status: number, problem: ProblemDetails) {
  const validationMessages = Object.values(problem.errors ?? {}).flat().filter(Boolean)
  if (validationMessages.length > 0) return validationMessages.join(' ')
  if (status >= 500) {
    const message = problem.detail ?? problem.title ?? 'Sunucuda beklenmeyen bir hata oluştu.'
    return problem.correlationId ? `${message} Takip kodu: ${problem.correlationId}` : message
  }
  if (problem.detail) return problem.detail
  if (problem.title) return problem.title

  if (status === 401) return 'Oturum süreniz doldu. Lütfen yeniden giriş yapın.'
  if (status === 409) return 'Bu saat artık uygun değil. Uygun sahaları yeniden arayın.'
  if (status === 429) return 'Çok fazla deneme yapıldı. Bir dakika bekleyip tekrar deneyin.'
  return 'İstek tamamlanamadı. Lütfen bilgilerinizi kontrol edip tekrar deneyin.'
}

export function setAccessToken(value: string | null) {
  accessToken = value
}

export function subscribeToAuthenticationExpired(listener: () => void) {
  authenticationExpiredListeners.add(listener)
  return () => { authenticationExpiredListeners.delete(listener) }
}

export function resetApiClientStateForTests() {
  accessToken = null
  refreshSessionPromise = null
  authenticationExpiredListeners.clear()
}

async function requestJson<T>(path: string, init: RequestInit = {}, retryUnauthorized = true): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body) headers.set('Content-Type', 'application/json')
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)
  const response = await fetch(`${apiBaseUrl}${path}`, { ...init, credentials: 'include', headers })
  if (response.status === 401 && retryUnauthorized && accessToken && !path.startsWith('/api/v1/auth/')) {
    try {
      await refreshAccessToken()
      return await requestJson<T>(path, init, false)
    } catch {
      expireAuthentication()
      throw new ApiError(401, { status: 401, title: 'Oturum süreniz doldu. Lütfen yeniden giriş yapın.' })
    }
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => ({})) as ProblemDetails
    throw new ApiError(response.status, problem)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}

async function refreshAccessToken(): Promise<AuthSession> {
  if (refreshSessionPromise) return await refreshSessionPromise

  const pending = requestJson<AuthSession>('/api/v1/auth/refresh', { method: 'POST' }, false)
    .then((session) => {
      setAccessToken(session.accessToken)
      return session
    })
    .finally(() => {
      if (refreshSessionPromise === pending) refreshSessionPromise = null
    })
  refreshSessionPromise = pending
  return await pending
}

function expireAuthentication() {
  setAccessToken(null)
  authenticationExpiredListeners.forEach((listener) => { listener() })
}

export async function getSystemHealth(signal?: AbortSignal): Promise<SystemHealth> {
  const response = await fetch(`${apiBaseUrl}/health/ready`, { headers: { Accept: 'text/plain' }, signal })
  if (!response.ok) throw new Error(`Health request failed with ${String(response.status)}`)
  return { correlationId: response.headers.get('X-Correlation-Id'), status: 'ready' }
}

export async function registerUser(input: {
  displayName: string
  email?: string
  phoneNumber?: string
  password: string
}): Promise<AuthSession> {
  return await requestJson<AuthSession>('/api/v1/auth/register', { method: 'POST', body: JSON.stringify(input) })
}

export async function loginUser(input: { identifier: string; password: string }): Promise<AuthSession> {
  return await requestJson<AuthSession>('/api/v1/auth/login', { method: 'POST', body: JSON.stringify(input) })
}

export async function refreshSession(): Promise<AuthSession> {
  return await refreshAccessToken()
}

export async function logoutUser(): Promise<void> {
  await requestJson<void>('/api/v1/auth/logout', { method: 'POST' })
}

export async function searchAvailableFields(input: {
  district: string
  localDate: string
  localTime: string
  durationMinutes: number
}): Promise<AvailableField[]> {
  const query = new URLSearchParams({
    district: input.district,
    localDate: input.localDate,
    localTime: input.localTime,
    durationMinutes: String(input.durationMinutes),
  })
  return await requestJson<AvailableField[]>(`/api/v1/availability?${query.toString()}`)
}

export async function createReservation(field: AvailableField, idempotencyKey: string): Promise<Reservation> {
  return await requestJson<Reservation>('/api/v1/reservations', {
    method: 'POST',
    headers: { 'Idempotency-Key': idempotencyKey },
    body: JSON.stringify({
      fieldId: field.fieldId,
      startsAtUtc: field.startsAtUtc,
      endsAtUtc: field.endsAtUtc,
    }),
  })
}

export async function getMyReservations(): Promise<Reservation[]> {
  return await requestJson<Reservation[]>('/api/v1/reservations?page=1&pageSize=100')
}

export async function cancelReservation(reservationId: string, idempotencyKey: string): Promise<Reservation> {
  return await requestJson<Reservation>(`/api/v1/reservations/${reservationId}/cancel`, {
    method: 'POST',
    headers: { 'Idempotency-Key': idempotencyKey },
  })
}
