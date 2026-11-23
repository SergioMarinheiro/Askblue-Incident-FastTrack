import { Link } from 'react-router'
import { AlertTriangle, ChevronRight } from 'lucide-react'
import { formatDate } from '@/lib/dates'
import { incidentDisplayDate, incidentPlace, incidentRef, incidentTitle } from './labels'
import { StatusBadge } from './StatusBadge'
import type { Incident } from './types'

/**
 * Incidents (ID, incident, store, date, status). Used by the list and the overview.
 * From "md" up: a table where the ID links to the incident. On phones: a list of tappable rows.
 */
export function IncidentTable({ incidents }: { incidents: Incident[] }) {
  return (
    <>
      <MobileList incidents={incidents} />

      <div className="hidden px-3 pb-3 md:block">
        {/* Fixed layout: each column gets its share of the width, so the table always fills the card. */}
        <table className="w-full table-fixed text-left text-sm">
          <colgroup>
            <col className="w-[14%]" />
            <col className="w-[30%]" />
            <col className="w-[24%]" />
            <col className="w-[14%]" />
            <col className="w-[18%]" />
          </colgroup>
          <thead>
            <tr className="bg-surface text-xs font-semibold tracking-wide text-muted uppercase">
              <th className="rounded-l-lg px-3 py-3">ID</th>
              <th className="px-3 py-3">Incident</th>
              <th className="px-3 py-3">Store</th>
              <th className="px-3 py-3">Date</th>
              <th className="rounded-r-lg px-3 py-3">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {incidents.map((incident) => (
              <tr key={incident.incidentId} className="hover:bg-surface/60">
                <td className="px-3 py-3.5 font-semibold whitespace-nowrap">
                  <Link to={`/incidents/${incident.incidentId}`} className="text-brand-600 hover:underline">
                    {incidentRef(incident.incidentId)}
                  </Link>
                </td>
                <td className="px-3 py-3.5">
                  <div className="truncate font-medium" title={incidentTitle(incident)}>
                    {incidentTitle(incident)}
                  </div>
                  <div className="truncate text-xs text-muted">{incident.customerName}</div>
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
