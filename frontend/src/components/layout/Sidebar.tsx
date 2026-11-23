import { NavLink } from 'react-router'
import { FilePlus2, LayoutDashboard, ListChecks, Users, type LucideIcon } from 'lucide-react'
import { USER_LIST_ROLES } from '@/features/users/permissions'
import { cn } from '@/lib/cn'
import { useAuth } from '@/auth/useAuth'
import { hasRole, ROLE_LABELS, type Role } from '@/auth/roles'
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
      { to: '/incidents/new', label: 'New incident', icon: FilePlus2 },
    ],
  },
  {
    title: 'Administration',
    items: [{ to: '/users', label: 'Users', icon: Users, roles: USER_LIST_ROLES }],
  },
]

export function Sidebar() {
  const { user } = useAuth()

  const visibleSections = sections
    .map((s) => ({ ...s, items: s.items.filter((i) => !i.roles || hasRole(user?.role, i.roles)) }))
    .filter((s) => s.items.length > 0)

  return (
    <aside className="sticky top-0 flex h-screen w-64 shrink-0 flex-col bg-navy-900 px-4 py-6">
      <div className="px-2">
        <Logo />
      </div>

      <nav className="mt-10 flex-1 space-y-8 overflow-y-auto">
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
    </aside>
  )
}
