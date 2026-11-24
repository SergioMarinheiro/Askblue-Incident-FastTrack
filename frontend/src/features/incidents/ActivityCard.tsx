import { useMemo, useState, type ReactNode } from 'react'
import { useQueries } from '@tanstack/react-query'
import { ArrowRight, FilePlus2, History, RefreshCw } from 'lucide-react'
import { Card, CardHeader } from '@/components/ui/Card'
import { ExpandableText } from '@/components/ui/ExpandableText'
import { ErrorState, LoadingState } from '@/components/ui/States'
import { apiFetch } from '@/lib/api'
import { formatDateTime, parseUtc } from '@/lib/dates'
import { useResolutionTypes, useStores, type Location } from '@/features/reference/api'
import { useIncidentHistory } from './api'
import { buildChangeSets, storeIdsInHistory, type ChangeSet, type FieldChange, type NameLookup } from './history'
import type { Incident } from './types'

const INITIAL_SETS = 5

/** "Activity": every recorded change to the incident, newest first, ending with its registration. */
export function ActivityCard({ incident }: { incident: Incident }) {
  const history = useIncidentHistory(incident.incidentId)
  const stores = useStores()
  const resolutionTypes = useResolutionTypes()
  const [showAll, setShowAll] = useState(false)

  // Locations of the current store and of every store in the history, so ids turn into names.
  const storeIds = useMemo(() => {
    const ids = new Set(storeIdsInHistory(history.data ?? []))
    if (incident.storeId !== null) ids.add(incident.storeId)
    return [...ids]
  }, [history.data, incident.storeId])

  const locationQueries = useQueries({
    queries: storeIds.map((storeId) => ({
      queryKey: ['stores', storeId, 'locations'],
      queryFn: ({ signal }: { signal: AbortSignal }) => apiFetch<Location[]>(`/api/stores/${storeId}/locations`, { signal }),
      staleTime: 5 * 60 * 1000,
      retry: false, // a deleted store answers 404; fall back to "Location #id"
    })),
  })
  // Deleted stores/locations aren't in the lists; the incident still knows the names of its current ones.
  const allLocations = locationQueries.flatMap((q) => q.data ?? [])
  const names: NameLookup = {
    store: (id) => stores.data?.find((s) => s.storeId === id)?.name ?? (id === incident.storeId ? (incident.storeName ?? undefined) : undefined),
    location: (id) =>
      allLocations.find((l) => l.locationId === id)?.name ?? (id === incident.locationId ? (incident.locationName ?? undefined) : undefined),
    resolutionType: (id) =>
      resolutionTypes.data?.find((r) => r.resolutionTypeId === id)?.name ??
      (id === incident.resolutionTypeId ? (incident.resolutionTypeName ?? undefined) : undefined),
  }
  // Cheap (a few dozen rows), so it is simply recomputed on each render.
  const sets = buildChangeSets(history.data ?? [], names)

  const visible = showAll ? sets : sets.slice(0, INITIAL_SETS)
  const hidden = sets.length - visible.length

  return (
    <Card>
      <CardHeader title="Activity" />
      {history.isPending ? (
        <LoadingState label="Loading activity…" />
      ) : history.isError ? (
        <ErrorState message={history.error.message} onRetry={() => history.refetch()} />
      ) : (
        <div className="px-6 pb-6">
          <ol className="relative space-y-5 before:absolute before:top-2 before:bottom-2 before:left-[15px] before:w-px before:bg-line">
            {visible.map((set) => (
              <ChangeSetItem key={set.id} set={set} />
            ))}

            {hidden > 0 ? (
              <li className="relative pl-11">
                <button type="button" onClick={() => setShowAll(true)} className="text-sm font-semibold text-brand-600 hover:underline">
                  Show {hidden} older {hidden === 1 ? 'change' : 'changes'}
                </button>
              </li>
            ) : (
              <li className="relative flex gap-3">
                <Dot>
                  <FilePlus2 className="size-3.5" aria-hidden />
                </Dot>
                <div className="pt-1.5 text-sm">
                  <span className="font-semibold">Incident registered</span>
                  <span className="text-muted"> · {formatDateTime(parseUtc(incident.createdAt))}</span>
                </div>
              </li>
            )}
          </ol>
        </div>
      )}
    </Card>
  )
}

function ChangeSetItem({ set }: { set: ChangeSet }) {
  const statusOnly = set.changes.length === 1 && set.changes[0].field === 'Status'
  return (
    <li className="relative flex gap-3">
      <Dot>{statusOnly ? <RefreshCw className="size-3.5" aria-hidden /> : <History className="size-3.5" aria-hidden />}</Dot>
      <div className="min-w-0 flex-1 pt-1.5">
        <p className="text-sm">
          <span className="font-semibold">{set.userName}</span>
          <span className="text-muted">
            {' '}
            {statusOnly ? 'changed the status' : `made ${set.changes.length === 1 ? 'a change' : `${set.changes.length} changes`}`} ·{' '}
            {formatDateTime(set.at)}
          </span>
        </p>
        <ul className="mt-2 space-y-1.5">
          {set.changes.map((c) => (
            <ChangeLine key={c.key} change={c} />
          ))}
        </ul>
      </div>
    </li>
  )
}

function ChangeLine({ change }: { change: FieldChange }) {
  const [open, setOpen] = useState(false)

  if (change.long) {
    return (
      <li className="text-sm">
        <span className="text-muted">{change.label}:</span>{' '}
        {change.to === null ? 'cleared' : change.from === null ? 'added' : 'changed'}{' '}
        <button type="button" onClick={() => setOpen((o) => !o)} className="font-semibold text-brand-600 hover:underline">
          {open ? 'Hide' : 'Show'}
        </button>
        {open && (
          <div className="mt-2 grid gap-2 sm:grid-cols-2">
            <TextBox title="Before" text={change.from} />
            <TextBox title="After" text={change.to} />
          </div>
        )}
      </li>
    )
  }

  return (
    <li className="flex flex-wrap items-center gap-x-1.5 text-sm">
      <span className="text-muted">{change.label}:</span>
      <Value text={change.from} muted />
      <ArrowRight className="size-3.5 text-muted" aria-label="to" />
      <Value text={change.to} />
    </li>
  )
}

function Value({ text, muted }: { text: string | null; muted?: boolean }) {
  if (text === null) return <span className="text-muted italic">empty</span>
  return <span className={`min-w-0 wrap-anywhere ${muted ? 'text-muted line-through decoration-muted/40' : 'font-medium'}`}>{text}</span>
}

function TextBox({ title, text }: { title: string; text: string | null }) {
  return (
    // min-w-0: the box may shrink below its longest word, so ExpandableText can wrap it.
    <div className="min-w-0 rounded-lg bg-surface px-3 py-2">
      <p className="text-xs font-semibold tracking-wide text-muted uppercase">{title}</p>
      {text === null ? (
        <p className="mt-1 text-sm text-muted italic">empty</p>
      ) : (
        <ExpandableText text={text} className="mt-1 text-sm" />
      )}
    </div>
  )
}

function Dot({ children }: { children: ReactNode }) {
  return (
    <span className="relative z-10 flex size-8 shrink-0 items-center justify-center rounded-full border border-line bg-white text-muted">
      {children}
    </span>
  )
}
