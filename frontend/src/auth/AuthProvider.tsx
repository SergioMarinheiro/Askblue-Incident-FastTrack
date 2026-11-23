import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { configureApi, ApiError } from '@/lib/api'
import { login as loginRequest, type LoginRequest } from './authApi'
import { AuthContext, type AuthContextValue } from './AuthContext'
import { isRole } from './roles'
import { clearSession, loadSession, saveSession, type Session } from './session'

// setTimeout can't wait longer than ~24.8 days.
const MAX_TIMEOUT_MS = 2_147_483_647

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [session, setSession] = useState<Session | null>(loadSession)

  // The API client reads the token through this ref, so it always sees the latest value.
  const sessionRef = useRef(session)
  sessionRef.current = session

  const logout = useCallback(() => {
    clearSession()
    setSession(null)
    queryClient.clear() // drop cached data from the previous user
  }, [queryClient])

  // Done during render (not in an effect): child effects run before parent effects,
  // so a page's first request would otherwise go out without the token.
  const logoutRef = useRef(logout)
  logoutRef.current = logout
  configureApi({
    getToken: () => sessionRef.current?.token ?? null,
    onUnauthorized: () => logoutRef.current(),
  })

  // Log out automatically when the token expires.
  useEffect(() => {
    if (!session) return
    const msLeft = new Date(session.expiresAt).getTime() - Date.now()
    if (msLeft <= 0) {
      logout()
      return
    }
    const timer = setTimeout(logout, Math.min(msLeft, MAX_TIMEOUT_MS))
    return () => clearTimeout(timer)
  }, [session, logout])

  const login = useCallback(async (request: LoginRequest) => {
    const response = await loginRequest(request)
    if (!isRole(response.role)) {
      throw new ApiError(0, `Unknown role "${response.role}" returned by the server.`)
    }
    if (typeof response.userId !== 'number') {
      throw new ApiError(0, 'The server is running an older version (no user id in the login response). Restart the API and try again.')
    }
    const next: Session = {
      token: response.token,
      userId: response.userId,
      name: response.name,
      role: response.role,
      expiresAt: response.expiresAt,
    }
    saveSession(next)
    setSession(next)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session ? { userId: session.userId, name: session.name, role: session.role } : null,
      login,
      logout,
    }),
    [session, login, logout],
  )

  return <AuthContext value={value}>{children}</AuthContext>
}
