import { useMemo } from 'react'
import { Link } from 'react-router'
import { ArrowRight } from 'lucide-react'
import { useAuth } from '@/auth/useAuth'
import { PageHeader } from '@/components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/States'
import { useIncidents } from '@/features/incidents/api'
import { IncidentTable } from '@/features/incidents/IncidentTable'
import { IncidentStatus } from '@/features/incidents/types'

const RECENT_COUNT = 5

function greeting(now = new Date()) {
  const h = now.getHours()
  return h < 12 ? 'Good morning' : h < 18 ? 'Good afternoon' : 'Good evening'
}

const STATS: { status: IncidentStatus; label: string }[] = [
  { status: IncidentStatus.AwaitingApproval, label: 'Awaiting approval' },
  { status: IncidentStatus.AwaitingPayment, label: 'Awaiting payment' },
  { status: IncidentStatus.AwaitingResolution, label: 'Awaiting resolution' },
  { status: IncidentStatus.Completed, label: 'Completed' },
]

export function OverviewPage() {
  const { user } = useAuth()
  const { data, isPending, isError, error, refetch } = useIncidents()
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
      />

      {/* Counts per status: one strip with lines between the numbers, no boxes. */}
      <div className="mb-12 grid grid-cols-2 border-y border-line lg:grid-cols-4 lg:divide-x lg:divide-line">
        {STATS.map((s) => (
          <Link key={s.status} to={`/incidents?status=${s.status}`} className="group px-4 py-5 text-center lg:px-6">
            <p className="text-sm text-muted group-hover:text-ink">{s.label}</p>
            <p className="mt-1 text-4xl font-semibold tabular-nums">
              {isPending ? <span className="text-muted/50">–</span> : counts.get(s.status) ?? 0}
            </p>
          </Link>
        ))}
      </div>

      {/* Recent incidents: a heading and the table straight on the page. */}
      <section>
        <div className="mb-2 flex items-center justify-between gap-4">
          <h2 className="text-lg font-semibold">Recent incidents</h2>
          <Link to="/incidents" className="inline-flex items-center gap-1 text-sm font-semibold text-brand-600 hover:underline">
            View all
            <ArrowRight className="size-4" aria-hidden />
          </Link>
        </div>
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
          <IncidentTable incidents={data.slice(0, RECENT_COUNT)} flush />
        )}
      </section>
    </>
  )
}
