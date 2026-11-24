import { createContext } from 'react'
import type { Role } from './roles'
import type { LoginRequest } from './authApi'

export type CurrentUser = { userId: number; name: string; role: Role }

export type AuthContextValue = {
  user: CurrentUser | null
  /** `mayReturnToPreviousPage`: false when a different account signs in than the one before. */
  login: (request: LoginRequest) => Promise<{ mayReturnToPreviousPage: boolean }>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
