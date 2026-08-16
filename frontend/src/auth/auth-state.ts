import { createContext, useContext } from 'react'
import { refreshSession, resetApiClientStateForTests, type AuthUser } from '../lib/api'

export type RegisterInput = { displayName: string; email?: string; phoneNumber?: string; password: string }
export type AuthContextValue = {
  user: AuthUser | null
  isBootstrapping: boolean
  login: (identifier: string, password: string) => Promise<void>
  register: (input: RegisterInput) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
let initialSessionPromise: ReturnType<typeof refreshSession> | null = null

export function restoreInitialSession() {
  initialSessionPromise ??= refreshSession()
  return initialSessionPromise
}

export function clearInitialSession() {
  initialSessionPromise = null
}

export function resetAuthClientStateForTests() {
  clearInitialSession()
  resetApiClientStateForTests()
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}
