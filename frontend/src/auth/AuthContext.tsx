import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { loginUser, logoutUser, registerUser, setAccessToken, subscribeToAuthenticationExpired, type AuthUser } from '../lib/api'
import { AuthContext, clearInitialSession, restoreInitialSession, type AuthContextValue } from './auth-state'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isBootstrapping, setBootstrapping] = useState(true)

  useEffect(() => subscribeToAuthenticationExpired(() => {
    clearInitialSession()
    setUser(null)
  }), [])

  useEffect(() => {
    let active = true
    void restoreInitialSession()
      .then((session) => {
        if (!active) return
        setAccessToken(session.accessToken)
        setUser(session.user)
      })
      .catch(() => {
        if (!active) return
        setAccessToken(null)
        setUser(null)
      })
      .finally(() => { if (active) setBootstrapping(false) })
    return () => { active = false }
  }, [])

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isBootstrapping,
    login: async (identifier, password) => {
      const session = await loginUser({ identifier, password })
      setAccessToken(session.accessToken)
      setUser(session.user)
    },
    register: async (input) => {
      const session = await registerUser(input)
      setAccessToken(session.accessToken)
      setUser(session.user)
    },
    logout: async () => {
      await logoutUser()
      setAccessToken(null)
      setUser(null)
      clearInitialSession()
    },
  }), [isBootstrapping, user])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
