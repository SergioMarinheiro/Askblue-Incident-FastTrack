import { isRole, type Role } from './roles'

export type Session = {
  token: string
  userId: number
  name: string
  role: Role
  /** ISO date-time (UTC) when the token stops being valid. */
  expiresAt: string
}

const STORAGE_KEY = 'ift.session'

export function isExpired(session: Session) {
  return new Date(session.expiresAt).getTime() <= Date.now()
}

/** Reads the saved session; returns null if missing, malformed or expired. */
export function loadSession(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const s = JSON.parse(raw) as Partial<Session>
    // Sessions saved by an older version (e.g. without userId) are dropped: the user logs in again.
    if (
      typeof s.token !== 'string' ||
      typeof s.userId !== 'number' ||
      typeof s.name !== 'string' ||
      typeof s.expiresAt !== 'string' ||
      !isRole(s.role)
    ) {
      clearSession()
      return null
    }
    const session = s as Session
    if (isExpired(session)) {
      clearSession()
      return null
    }
    return session
  } catch {
    return null
  }
}

export function saveSession(session: Session) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  } catch {
    // Storage blocked (e.g. private mode): the session still works until the page is reloaded.
  }
}

export function clearSession() {
  try {
    localStorage.removeItem(STORAGE_KEY)
  } catch {
    // ignore
  }
}
