import { Link, useMatches } from 'react-router'
import { UserMenu } from './UserMenu'

type CrumbHandle = { crumb?: string | ((params: Record<string, string | undefined>) => string) }

export function Topbar() {
  const crumbs = useMatches().flatMap((m) => {
    const crumb = (m.handle as CrumbHandle | undefined)?.crumb
    const label = typeof crumb === 'function' ? crumb(m.params) : crumb
    return label ? [{ label, path: m.pathname }] : []
  })

  return (
    <header className="flex h-16 items-center justify-between border-b border-line bg-white px-8">
      <nav aria-label="Breadcrumb" className="text-sm text-muted">
        {crumbs.map((c, i) => {
          const isFirst = i === 0
          const isLast = i === crumbs.length - 1
          return (
            <span key={c.path + c.label}>
              {!isFirst && <span className="mx-2 text-muted/60">/</span>}
              {isFirst || isLast ? (
                <span className={isLast ? 'text-ink' : undefined}>{c.label}</span>
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
