import { useEffect } from 'react'
import { Link, NavLink } from 'react-router'
import { X } from 'lucide-react'
import { USER_LIST_ROLES } from '@/features/users/permissions'
import { cn } from '@/lib/cn'
import { useAuth } from '@/auth/useAuth'
import { ADMIN_OR_ABOVE, hasRole, SUPERADMIN_ONLY, type Role } from '@/auth/roles'
import askblueLogoWhite from '@/assets/askblue-logo-negativo-rgb.png'
import { Logo } from './Logo'

type NavItem = {
  to: string
  label: string
  end?: boolean
  /** Roles that see this item. Leave out for "everyone". */
  roles?: readonly Role[]
}

type NavSection = { title: string; items: NavItem[] }

const sections: NavSection[] = [
  {
    title: 'Workspace',
    items: [
      { to: '/overview', label: 'Overview' },
      { to: '/incidents', label: 'Incidents', end: true },
    ],
  },
  {
    title: 'Administration',
    items: [
      { to: '/users', label: 'Users', roles: USER_LIST_ROLES },
      { to: '/stores', label: 'Stores', roles: ADMIN_OR_ABOVE },
      { to: '/resolution-types', label: 'Resolution types', roles: ADMIN_OR_ABOVE },
      { to: '/companies', label: 'Companies', roles: SUPERADMIN_ONLY },
    ],
  },
]

/**
 * Large screens: a fixed column on the left.
 * Small screens: hidden; slides in from the left when `mobileOpen` (opened from the top bar's menu button).
 */
export function Sidebar({ mobileOpen, onClose }: { mobileOpen: boolean; onClose: () => void }) {
  // While the mobile menu is open: Escape closes it and the page behind doesn't scroll.
  useEffect(() => {
    if (!mobileOpen) return
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    document.addEventListener('keydown', onKey)
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      document.removeEventListener('keydown', onKey)
      document.body.style.overflow = previousOverflow
    }
  }, [mobileOpen, onClose])

  return (
    <>
      {/* Desktop */}
      <aside className="sticky top-0 hidden h-screen w-64 shrink-0 flex-col bg-brand-600 px-4 py-6 lg:flex">
        <SidebarContent />
      </aside>

      {/* Mobile drawer */}
      <div className={cn('fixed inset-0 z-40 lg:hidden', !mobileOpen && 'pointer-events-none')} aria-hidden={!mobileOpen}>
        <div
          className={cn('absolute inset-0 bg-navy-950/50 transition-opacity', mobileOpen ? 'opacity-100' : 'opacity-0')}
          onClick={onClose}
        />
        <aside
          id="mobile-menu"
          className={cn(
            'absolute inset-y-0 left-0 flex w-72 max-w-[85vw] flex-col bg-brand-600 px-4 py-6 shadow-xl transition-transform',
            mobileOpen ? 'translate-x-0' : '-translate-x-full',
          )}
          inert={!mobileOpen}
        >
          <button
            type="button"
            onClick={onClose}
            aria-label="Close menu"
            className="absolute top-5 right-3 rounded-md p-1.5 text-white/70 hover:bg-white/10 hover:text-white"
          >
            <X className="size-5" aria-hidden />
          </button>
          <SidebarContent />
        </aside>
      </div>
    </>
  )
}

function SidebarContent() {
  const { user } = useAuth()

  const visibleSections = sections
    .map((s) => ({ ...s, items: s.items.filter((i) => !i.roles || hasRole(user?.role, i.roles)) }))
    .filter((s) => s.items.length > 0)

  return (
    <>
      {/* The product name doubles as a "home" link, as is usual for the name or logo at the top of an app. */}
      <Link to="/overview" className="block rounded-lg px-2 py-1 hover:bg-white/10">
        <Logo />
      </Link>

      {/* The main action, shown as a button so it stands out from the menu: white on the blue sidebar. */}
      <NavLink
        to="/incidents/new"
        className={({ isActive }) =>
          cn(
            'mt-8 flex w-full items-center justify-center gap-2 self-stretch rounded-lg bg-white px-3 py-2.5 text-center text-sm font-semibold text-brand-600 shadow-sm transition-colors',
            isActive ? 'ring-2 ring-white/50 ring-offset-2 ring-offset-brand-600' : 'hover:bg-brand-50',
          )
        }
      >
        New incident
      </NavLink>

      <nav className="mt-8 flex-1 space-y-8 overflow-y-auto">
        {visibleSections.map((section) => (
          <div key={section.title}>
            <p className="px-3 text-xs font-semibold tracking-wider text-white/60 uppercase">{section.title}</p>
            <ul className="mt-3 space-y-1">
              {section.items.map(({ to, label, end }) => (
                <li key={to}>
                  <NavLink
                    to={to}
                    end={end}
                    className={({ isActive }) =>
                      cn(
                        'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm transition-colors',
                        isActive
                          ? 'bg-white/15 font-semibold text-white'
                          : 'text-white/80 hover:bg-white/10 hover:text-white',
                      )
                    }
                  >
                    {label}
                  </NavLink>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </nav>

      {/* The menu above takes all free space (flex-1), which pushes the logo to the bottom. */}
      <img src={askblueLogoWhite} alt="AskBlue" className="mx-auto mt-6 w-40 shrink-0" />
    </>
  )
}
