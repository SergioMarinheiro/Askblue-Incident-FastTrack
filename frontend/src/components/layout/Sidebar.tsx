import { useEffect } from 'react'
import { NavLink } from 'react-router'
import { Building2, LayoutDashboard, ListChecks, Scale, Store, Users, X, type LucideIcon } from 'lucide-react'
import { USER_LIST_ROLES } from '@/features/users/permissions'
import { cn } from '@/lib/cn'
import { useAuth } from '@/auth/useAuth'
import { ADMIN_OR_ABOVE, hasRole, ROLE_LABELS, SUPERADMIN_ONLY, type Role } from '@/auth/roles'
import { Logo } from './Logo'
import { Avatar } from './Avatar'

type NavItem = {
  to: string
  label: string
  icon: LucideIcon
  end?: boolean
  /** Roles that see this item. Leave out for "everyone". */
  roles?: readonly Role[]
}

type NavSection = { title: string; items: NavItem[] }

const sections: NavSection[] = [
  {
    title: 'Workspace',
    items: [
      { to: '/overview', label: 'Overview', icon: LayoutDashboard },
      { to: '/incidents', label: 'Incidents', icon: ListChecks, end: true },
    ],
  },
  {
    title: 'Administration',
    items: [
      { to: '/users', label: 'Users', icon: Users, roles: USER_LIST_ROLES },
      { to: '/stores', label: 'Stores', icon: Store, roles: ADMIN_OR_ABOVE },
      { to: '/resolution-types', label: 'Resolution types', icon: Scale, roles: ADMIN_OR_ABOVE },
      { to: '/companies', label: 'Companies', icon: Building2, roles: SUPERADMIN_ONLY },
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
      <aside className="sticky top-0 hidden h-screen w-64 shrink-0 flex-col bg-navy-900 px-4 py-6 lg:flex">
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
            'absolute inset-y-0 left-0 flex w-72 max-w-[85vw] flex-col bg-navy-900 px-4 py-6 shadow-xl transition-transform',
            mobileOpen ? 'translate-x-0' : '-translate-x-full',
          )}
          inert={!mobileOpen}
        >
          <button
            type="button"
            onClick={onClose}
            aria-label="Close menu"
            className="absolute top-5 right-3 rounded-md p-1.5 text-white/70 hover:bg-navy-800 hover:text-white"
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
      <div className="px-2">
        <Logo />
      </div>

      {/* The main action, shown as a button so it stands out from the menu. */}
      <NavLink
        to="/incidents/new"
        className={({ isActive }) =>
          cn(
            'mt-8 flex w-full items-center justify-center gap-2 self-stretch rounded-lg px-3 py-2.5 text-center text-sm font-semibold text-white shadow-sm transition-colors',
            isActive ? 'bg-brand-700 ring-2 ring-white/30' : 'bg-brand-600 hover:bg-brand-500',
          )
        }
      >
        New incident
      </NavLink>

      <nav className="mt-8 flex-1 space-y-8 overflow-y-auto">
        {visibleSections.map((section) => (
          <div key={section.title}>
            <p className="px-3 text-xs font-semibold tracking-wider text-white/50 uppercase">{section.title}</p>
            <ul className="mt-3 space-y-1">
              {section.items.map(({ to, label, icon: Icon, end }) => (
                <li key={to}>
                  <NavLink
                    to={to}
                    end={end}
                    className={({ isActive }) =>
                      cn(
                        'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm transition-colors',
                        isActive
                          ? 'bg-navy-800 font-semibold text-white'
                          : 'text-white/75 hover:bg-navy-800/60 hover:text-white',
                      )
                    }
                  >
                    <Icon className="size-4" aria-hidden />
                    {label}
                  </NavLink>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </nav>

      {user && (
        <div className="flex items-center gap-3 border-t border-white/10 px-2 pt-5">
          <Avatar name={user.name} />
          <div className="min-w-0 leading-tight">
            <div className="truncate text-sm font-semibold text-white">{user.name}</div>
            <div className="truncate text-xs text-white/60">{ROLE_LABELS[user.role]}</div>
          </div>
        </div>
      )}
    </>
  )
}
