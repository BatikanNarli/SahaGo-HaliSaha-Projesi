import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach, beforeEach, vi } from 'vitest'
import { resetAuthClientStateForTests } from '../auth/auth-state'

function defaultFetch(input: RequestInfo | URL) {
  const url = typeof input === 'string' ? input : input instanceof URL ? input.href : input.url
  if (url.endsWith('/api/v1/auth/refresh')) {
    return Promise.resolve(new Response(JSON.stringify({ title: 'Oturum yenilenemedi.' }), { status: 401, headers: { 'Content-Type': 'application/problem+json' } }))
  }
  return Promise.resolve(new Response('Healthy', { status: 200, headers: { 'X-Correlation-Id': 'test-correlation-id' } }))
}

vi.stubGlobal('fetch', vi.fn(defaultFetch))
beforeEach(() => {
  resetAuthClientStateForTests()
  vi.mocked(fetch).mockImplementation(defaultFetch)
})
afterEach(() => { cleanup(); vi.clearAllMocks() })
