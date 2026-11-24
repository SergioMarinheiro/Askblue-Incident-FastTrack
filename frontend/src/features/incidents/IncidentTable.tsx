import { Link, useNavigate } from 'react-router'
import { AlertTriangle, ChevronRight } from 'lucide-react'
import { cn } from '@/lib/cn'
import { formatDate } from '@/lib/dates'
import { incidentDisplayDate, incidentPlace, incidentRef, incidentTitle } from './labels'
import { StatusBadge } from './StatusBadge'
import type { Incident } from './types'

/**
 * Incidents (ID, incident, reported by, store, date, status). Used by the list and the overview.
 * From "md" up: a table where the ID links to the incident. On phones: a list of tappable rows.
 * `flush`: the table sits straight on the page instead of inside a card (no side padding,
 * a line under the header instead of a shaded header row).
 */
export function IncidentTable({ incidents, flush = false }: { incidents: Incident[]; flush?: boolean }) {
  const navigate = useNavigate()

  function openIncident(id: number, inNewTab: boolean) {
    const url = `/incidents/${id}`
    if (inNewTab) window.open(url, '_blank', 'noopener')
    else navigate(url)
  }

  return (
    <>
      <MobileList incidents={incidents} />

      <div className={cn('hidden md:block', !flush && 'px-3 pb-3')}>
        {/* Fixed layout: each column gets its share of the width, so the table always fills the card. */}
        <table className="w-full table-fixed text-left text-sm">
          <colgroup>
            <col className="w-[11%]" />
            <col className="w-[20%]" />
            <col className="w-[18%]" />
            <col className="w-[23%]" />
            <col className="w-[12%]" />
            <col className="w-[16%]" />
          </colgroup>
          <thead>
            <tr className={cn('text-xs font-semibold tracking-wide text-muted uppercase', flush ? 'border-b border-line' : 'bg-surface')}>
              <th className="rounded-l-lg px-3 py-3">ID</th>
              <th className="px-3 py-3">Incident</th>
              <th className="px-3 py-3">Reported by</th>
              <th className="px-3 py-3">Store</th>
              <th className="px-3 py-3">Date</th>
              <th className="rounded-r-lg px-3 py-3">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {incidents.map((incident) => (
              // The whole row opens the incident. A row can't be a real link, so it is made to act like one:
              // Ctrl/Cmd+click opens a new tab, and keyboard users can Tab to the row and press Enter.
              <tr
                key={incident.incidentId}
                tabIndex={0}
                onClick={(e) => openIncident(incident.incidentId, e.ctrlKey || e.metaKey)}
                onKeyDown={(e) => e.key === 'Enter' && openIncident(incident.incidentId, e.ctrlKey || e.metaKey)}
                className={cn(
                  'cursor-pointer focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-brand-500',
                  flush ? 'hover:bg-white' : 'hover:bg-surface/60',
                )}
              >
                <td className="px-3 py-3.5 font-semibold whitespace-nowrap text-brand-600">{incidentRef(incident.incidentId)}</td>
                <td className="px-3 py-3.5">
                  <div className="truncate font-medium" title={incidentTitle(incident)}>
                    {incidentTitle(incident)}
                  </div>
                  <div className="truncate text-xs text-muted">{incident.customerName}</div>
                </td>
                <td className="truncate px-3 py-3.5 text-ink/90" title={incident.createdByUserName}>
                  {incident.createdByUserName}
                </td>
                <td className="truncate px-3 py-3.5 text-ink/90" title={incidentPlace(incident) ?? undefined}>
                  {incidentPlace(incident) ?? <span className="text-muted">—</span>}
                </td>
                <td className="px-3 py-3.5 whitespace-nowrap text-ink/90">{formatDate(incidentDisplayDate(incident))}</td>
                <td className="px-3 py-3.5">
                  <StatusCell incident={incident} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}

function MobileList({ incidents }: { incidents: Incident[] }) {
  return (
    <ul className="divide-y divide-line border-t border-line md:hidden">
      {incidents.map((incident) => {
        const place = incidentPlace(incident)
        return (
          <li key={incident.incidentId}>
            <Link to={`/incidents/${incident.incidentId}`} className="flex items-center gap-3 px-4 py-3.5 active:bg-surface">
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-2">
                  <span className="text-sm font-semibold text-brand-600">{incidentRef(incident.incidentId)}</span>
                  <span className="text-xs text-muted">{formatDate(incidentDisplayDate(incident))}</span>
                </div>
                <p className="mt-0.5 truncate text-sm font-medium">{incidentTitle(incident)}</p>
                <p className="truncate text-xs text-muted">{[incident.customerName, place].filter(Boolean).join(' · ')}</p>
                <p className="truncate text-xs text-muted">Reported by {incident.createdByUserName}</p>
                <div className="mt-2">
                  <StatusCell incident={incident} />
                </div>
              </div>
              <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
            </Link>
          </li>
        )
      })}
    </ul>
  )
}

function StatusCell({ incident }: { incident: Incident }) {
  return (
    <div className="flex items-center gap-1.5">
      <StatusBadge status={incident.status} />
      {incident.hasPendingDuplicateFlag && (
        <span title="Possible duplicate — awaiting review" className="text-amber-600">
          <AlertTriangle className="size-4" aria-label="Possible duplicate" />
        </span>
      )}
    </div>
  )
}
