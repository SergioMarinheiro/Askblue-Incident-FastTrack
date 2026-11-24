import { apiFetch } from '@/lib/api'

export type LoginRequest = { email: string; password: string }

export type LoginResponse = {
  token: string
  /** Missing on API versions before the userId was added to the login response. */
  userId?: number
  name: string
  role: string
  expiresAt: string
}

export function login(request: LoginRequest) {
  return apiFetch<LoginResponse>('/api/auth/login', { method: 'POST', body: request, auth: false })
}
