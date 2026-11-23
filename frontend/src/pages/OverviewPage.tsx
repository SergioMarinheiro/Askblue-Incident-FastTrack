import { useMemo, type ReactNode } from 'react'
import { Link } from 'react-router'
import { ArrowRight, ClipboardCheck, PenLine, Plus } from 'lucide-react'
import { useAuth } from '@/auth/useAuth'
import { hasRole } from '@/auth/roles'
import { Card, CardHeader } from '@/components/ui/Card'
import { PageHeader } from '@/components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/States'
import { buttonPrimaryClass } from '@/components/ui/form'
import { useIncidents } from '@/features/incidents/api'
import { IncidentTable } from '@/features/incidents/IncidentTable'
import { IncidentStatus, type Incident } from '@/features/incidents/types'
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

  const todo = useMemo(() => buildTodo(data ?? [], user?.userId, isReviewer), [data, user?.userId, isReviewer])

  return (
    <>
      <PageHeader
        title={`${greeting()}${firstName ? `, ${firstName}` : ''}!`}
        subtitle={isReviewer ? "Here is a summary of your organisation's incidents." : 'Here is a summary of your incidents.'}
        actions={
          <Link to="/incidents/new" className={buttonPrimaryClass}>
            <Plus className="size-4" aria-hidden />
            New incident
          </Link>
        }
      />

      {/* Things waiting for this user */}
      {todo.length > 0 && (
        <div className="mb-6 grid gap-3 md:grid-cols-2">
          {todo.map((t) => (
            <TodoBanner key={t.id} {...t} />
          ))}
        </div>
      )}

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
              {isPending ? <span className="text-muted/50">–</span> : String(counts.get(c.status) ?? 0).padStart(2, '0')}
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

type Todo = { id: string; count: number; title: string; description: string; to: string; icon: ReactNode }

function buildTodo(incidents: Incident[], userId: number | undefined, isReviewer: boolean): Todo[] {
  const todo: Todo[] = []
  const mine = incidents.filter((i) => i.createdByUserId === userId)

  // Reviewers: submitted by someone else and waiting for a decision.
  if (isReviewer) {
    const toApprove = incidents.filter((i) => i.status === IncidentStatus.AwaitingApproval && i.createdByUserId !== userId).length
    if (toApprove > 0)
      todo.push({
        id: 'approve',
        count: toApprove,
        title: toApprove === 1 ? '1 incident needs your approval' : `${toApprove} incidents need your approval`,
        description: 'Approve, reject or send them back.',
        to: `/incidents?status=${IncidentStatus.AwaitingApproval}`,
        icon: <ClipboardCheck className="size-5" />,
      })
  }

  // Everyone: their own incidents waiting for them.
  const sentBack = mine.filter((i) => i.status === IncidentStatus.SentBack).length
  if (sentBack > 0)
    todo.push({
      id: 'sentBack',
      count: sentBack,
      title: sentBack === 1 ? '1 incident was sent back to you' : `${sentBack} incidents were sent back to you`,
      description: 'Make the requested changes and submit again.',
      to: `/incidents?status=${IncidentStatus.SentBack}`,
      icon: <PenLine className="size-5" />,
    })

  const drafts = mine.filter((i) => i.status === IncidentStatus.New).length
  if (drafts > 0)
    todo.push({
      id: 'drafts',
      count: drafts,
      title: drafts === 1 ? 'You have 1 unsubmitted draft' : `You have ${drafts} unsubmitted drafts`,
      description: 'Finish and submit them so they can be approved.',
      to: `/incidents?status=${IncidentStatus.New}`,
      icon: <PenLine className="size-5" />,
    })

  return todo
}

function TodoBanner({ title, description, to, icon }: Todo) {
  return (
    <Link
      to={to}
      className="group flex items-center gap-4 rounded-xl border border-brand-100 bg-brand-50 px-4 py-3.5 transition hover:border-brand-500"
    >
      <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-white text-brand-600" aria-hidden>
        {icon}
      </span>
      <span className="min-w-0 flex-1">
        <span className="block font-semibold text-ink">{title}</span>
        <span className="block text-sm text-muted">{description}</span>
      </span>
      <ArrowRight className="size-4 shrink-0 text-brand-600 transition group-hover:translate-x-0.5" aria-hidden />
    </Link>
  )
}
