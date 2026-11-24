import { useMemo } from 'react'
import { Link } from 'react-router'
import { ArrowRight } from 'lucide-react'
import { useAuth } from '@/auth/useAuth'
import { hasRole } from '@/auth/roles'
import { Card, CardHeader } from '@/components/ui/Card'
import { PageHeader } from '@/components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/States'
import { useIncidents } from '@/features/incidents/api'
import { IncidentTable } from '@/features/incidents/IncidentTable'
import { IncidentStatus } from '@/features/incidents/types'
import { cn } from '@/lib/cn'

const RECENT_COUNT = 5

function greeting(now = new Date()) {
  const h = now.getHours()
  return h < 12 ? 'Good morning' : h < 18 ? 'Good afternoon' : 'Good evening'
}

const STAT_CARDS: { status: IncidentStatus; label: string; hint: string; accent: string }[] = [
  { status: IncidentStatus.AwaitingApproval, label: 'Awaiting approval', hint: 'Submitted, waiting for a manager', accent: 'bg-amber-400' },
  { status: IncidentStatus.AwaitingPayment, label: 'Awaiting payment', hint: 'Approved, payment pending', accent: 'bg-violet-500' },
  { status: IncidentStatus.AwaitingResolution, label: 'Awaiting resolution', hint: 'Approved, being resolved', accent: 'bg-brand-600' },
  { status: IncidentStatus.Completed, label: 'Completed', hint: 'Resolved and closed', accent: 'bg-emerald-500' },
]

export function OverviewPage() {
  const { user } = useAuth()
  const { data, isPending, isError, error, refetch } = useIncidents()
  const isReviewer = hasRole(user?.role, ['Manager', 'Admin', 'SuperAdmin'])
  const firstName = user?.name.split(' ')[0] ?? ''

  const counts = useMemo(() => {
    const map = new Map<IncidentStatus, number>()
    for (const i of data ?? []) map.set(i.status, (map.get(i.status) ?? 0) + 1)
    return map
  }, [data])

  return (
    <>
      <PageHeader
        title={`${greeting()}${firstName ? `, ${firstName}` : ''}!`}
        subtitle={isReviewer ? "Here is a summary of your organisation's incidents." : 'Here is a summary of your incidents.'}
      />

      {/* Counts per status */}
      <div className="mb-8 grid grid-cols-2 gap-4 lg:grid-cols-4">
        {STAT_CARDS.map((c) => (
          <Link
            key={c.status}
            to={`/incidents?status=${c.status}`}
            className="group relative overflow-hidden rounded-xl border border-line bg-white p-5 shadow-sm transition hover:border-brand-100 hover:shadow-md"
          >
            <span className={cn('absolute inset-x-0 top-0 h-1', c.accent)} aria-hidden />
            <p className="text-sm font-semibold text-muted">{c.label}</p>
            <p className="mt-2 text-3xl font-bold tabular-nums">
              {isPending ? <span className="text-muted/50">–</span> : counts.get(c.status) ?? 0}
            </p>
            <p className="mt-1 text-xs text-muted">{c.hint}</p>
          </Link>
        ))}
      </div>

      {/* Recent incidents */}
      <Card>
        <CardHeader
          title="Recent incidents"
          actions={
            <Link to="/incidents" className="inline-flex items-center gap-1 font-semibold text-brand-600 hover:underline">
              View all
              <ArrowRight className="size-4" aria-hidden />
            </Link>
          }
        />
        {isPending ? (
          <LoadingState label="Loading incidents…" />
        ) : isError ? (
          <ErrorState message={error.message} onRetry={() => refetch()} />
        ) : data.length === 0 ? (
          <EmptyState
            title="No incidents yet"
            description="When an incident happens, register it here."
            action={
              <Link to="/incidents/new" className="text-sm font-semibold text-brand-600 hover:underline">
                Register an incident
              </Link>
            }
          />
        ) : (
          // The API returns incidents newest first.
          <IncidentTable incidents={data.slice(0, RECENT_COUNT)} />
        )}
      </Card>
    </>
  )
}
