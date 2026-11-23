import { createContext } from 'react'
import type { Role } from './roles'
import type { LoginRequest } from './authApi'

export type CurrentUser = { userId: number; name: string; role: Role }

export type AuthContextValue = {
  user: CurrentUser | null
  login: (request: LoginRequest) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
