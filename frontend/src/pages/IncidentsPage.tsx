import { useMemo } from 'react'
import { Link, useSearchParams } from 'react-router'
import { Plus, Search } from 'lucide-react'
import { Card, CardHeader } from '@/components/ui/Card'
import { PageHeader } from '@/components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/States'
import { buttonPrimaryClass, inputClass } from '@/components/ui/form'
import { useIncidents } from '@/features/incidents/api'
import { IncidentTable } from '@/features/incidents/IncidentTable'
import { incidentRef, STATUS_INFO, STATUS_ORDER } from '@/features/incidents/labels'
import type { Incident, IncidentStatus } from '@/features/incidents/types'

function matchesSearch(incident: Incident, term: string) {
  if (!term) return true
  const haystack = [
    incidentRef(incident.incidentId),
    String(incident.incidentId),
    incident.title,
    incident.description,
    incident.customerName,
    incident.storeName,
    incident.locationName,
  ]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()
  return haystack.includes(term)
}

export function IncidentsPage() {
  // Filters live in the URL (?q=...&status=2) so they survive refresh and the back button.
  const [params, setParams] = useSearchParams()
  const search = params.get('q') ?? ''
  const statusParam = params.get('status')
  const status = statusParam && Number(statusParam) in STATUS_INFO ? (Number(statusParam) as IncidentStatus) : null

  const { data, isPending, isError, error, refetch } = useIncidents()

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase()
    return (data ?? []).filter((i) => (status === null || i.status === status) && matchesSearch(i, term))
  }, [data, search, status])

  function updateParam(key: string, value: string) {
    setParams(
      (prev) => {
        const next = new URLSearchParams(prev)
        if (value) next.set(key, value)
        else next.delete(key)
        return next
      },
      { replace: true },
    )
  }

  const hasFilters = search !== '' || status !== null
  const total = data?.length ?? 0

  return (
    <>
      <PageHeader
        title="Incidents"
        subtitle="Browse and follow up on submitted incidents."
        actions={
          <Link to="/incidents/new" className={buttonPrimaryClass}>
            <Plus className="size-4" aria-hidden />
            New incident
          </Link>
        }
      />

      <div className="mb-5 flex flex-col gap-3 sm:flex-row">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted" aria-hidden />
          <input
            type="search"
            value={search}
            onChange={(e) => updateParam('q', e.target.value)}
            placeholder="Search by ID, title, customer or store…"
            aria-label="Search incidents"
            className={`${inputClass} pl-9`}
          />
        </div>
        <select
          value={status ?? ''}
          onChange={(e) => updateParam('status', e.target.value)}
          aria-label="Filter by status"
          className={`${inputClass} sm:w-56`}
        >
          <option value="">All statuses</option>
          {STATUS_ORDER.map((s) => (
            <option key={s} value={s}>
              {STATUS_INFO[s].label}
            </option>
          ))}
        </select>
      </div>

      <Card>
        <CardHeader
          title={status === null ? 'All incidents' : STATUS_INFO[status].label}
          actions={data && `${filtered.length} ${filtered.length === 1 ? 'incident' : 'incidents'}`}
        />

        {isPending ? (
          <LoadingState label="Loading incidents…" />
        ) : isError ? (
          <ErrorState message={error.message} onRetry={() => refetch()} />
        ) : filtered.length === 0 ? (
          hasFilters ? (
            <EmptyState
              title="No incidents match your filters"
              description="Try a different search or status."
              action={
                <button type="button" onClick={() => setParams({}, { replace: true })} className="text-sm font-semibold text-brand-600 hover:underline">
                  Clear filters
                </button>
              }
            />
          ) : (
            <EmptyState
              title="No incidents yet"
              description="Incidents you register will appear here."
              action={
                <Link to="/incidents/new" className="text-sm font-semibold text-brand-600 hover:underline">
                  Register an incident
                </Link>
              }
            />
          )
        ) : (
          <IncidentTable incidents={filtered} />
        )}
      </Card>

      {data && (
        <p className="mt-4 text-sm text-muted">
          Showing {filtered.length} of {total} {total === 1 ? 'incident' : 'incidents'}
        </p>
      )}
    </>
  )
}
