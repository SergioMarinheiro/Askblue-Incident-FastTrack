import { Link, useMatches } from 'react-router'
import { Menu } from 'lucide-react'
import { cn } from '@/lib/cn'
import { UserMenu } from './UserMenu'

type CrumbHandle = { crumb?: string | ((params: Record<string, string | undefined>) => string) }

export function Topbar({ onOpenMenu, menuOpen }: { onOpenMenu: () => void; menuOpen: boolean }) {
  const crumbs = useMatches().flatMap((m) => {
    const crumb = (m.handle as CrumbHandle | undefined)?.crumb
    const label = typeof crumb === 'function' ? crumb(m.params) : crumb
    return label ? [{ label, path: m.pathname }] : []
  })

  return (
    // Sticky, so the menu button stays reachable while scrolling on a phone.
    <header className="sticky top-0 z-30 flex h-16 items-center gap-3 border-b border-line bg-white px-4 sm:px-6 lg:static lg:px-8">
      <button
        type="button"
        onClick={onOpenMenu}
        aria-label="Open menu"
        aria-controls="mobile-menu"
        aria-expanded={menuOpen}
        className="-ml-1 rounded-md p-2 text-ink hover:bg-surface lg:hidden"
      >
        <Menu className="size-5" aria-hidden />
      </button>

      <nav aria-label="Breadcrumb" className="min-w-0 flex-1 truncate text-sm text-muted">
        {crumbs.map((c, i) => {
          const isFirst = i === 0
          const isLast = i === crumbs.length - 1
          return (
            // On phones only the current page is shown; the full trail from "sm" up.
            <span key={c.path + c.label} className={cn(!isLast && 'hidden sm:inline')}>
              {!isFirst && <span className="mx-2 hidden text-muted/60 sm:inline">/</span>}
              {isFirst || isLast ? (
                <span className={isLast ? 'font-medium text-ink sm:font-normal' : undefined}>{c.label}</span>
              ) : (
                <Link to={c.path} className="hover:text-brand-600">
                  {c.label}
                </Link>
              )}
            </span>
          )
        })}
      </nav>
      <UserMenu />
    </header>
  )
}
