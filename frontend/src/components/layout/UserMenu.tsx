import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { LogOut } from 'lucide-react'
import { useAuth } from '@/auth/useAuth'
import { ROLE_LABELS } from '@/auth/roles'
import { Avatar } from './Avatar'

export function UserMenu() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  // Go to the login page ourselves, without a "return to" page: whoever signs in next
  // starts at the overview instead of the page the previous user was on.
  function handleLogout() {
    navigate('/login', { replace: true })
    logout()
  }

  // Close when clicking outside or pressing Escape.
  useEffect(() => {
    if (!open) return
    const onClick = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false)
    }
    document.addEventListener('mousedown', onClick)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onClick)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  if (!user) return null

  return (
    <div ref={ref} className="relative flex items-center gap-3">
      {/* Name and role next to the avatar; hidden on phones, where the top bar is narrow. */}
      <div className="hidden min-w-0 text-right leading-tight sm:block">
        <div className="truncate text-sm font-semibold">{user.name}</div>
        <div className="truncate text-xs text-muted">{ROLE_LABELS[user.role]}</div>
      </div>
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label="Open user menu"
        className="group cursor-pointer rounded-full focus:ring-2 focus:ring-brand-100 focus:outline-none"
      >
        <Avatar name={user.name} variant="light" />
      </button>

      {open && (
        <div role="menu" className="absolute top-full right-0 z-20 mt-2 w-max overflow-hidden rounded-xl border border-line bg-white shadow-lg">
          <button
            type="button"
            role="menuitem"
            onClick={handleLogout}
            className="flex w-full items-center gap-2 px-4 py-2.5 text-left text-sm text-ink hover:bg-surface"
          >
            <LogOut className="size-4" aria-hidden />
            Log out
          </button>
        </div>
      )}
    </div>
  )
}
