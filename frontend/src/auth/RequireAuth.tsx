import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { useAuth } from './useAuth'

/** Shows its children only to logged-in users; everyone else is sent to the login page. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const location = useLocation()

  if (!user) {
    // Remember where the user was going, so login can send them back there.
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  }
  return children
}
