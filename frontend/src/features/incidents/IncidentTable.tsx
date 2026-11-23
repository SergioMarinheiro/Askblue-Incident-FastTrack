import { Link } from 'react-router'
import { AlertTriangle } from 'lucide-react'
import { formatDate } from '@/lib/dates'
import { incidentDisplayDate, incidentPlace, incidentRef, incidentTitle } from './labels'
import { StatusBadge } from './StatusBadge'
import type { Incident } from './types'

/** Table of incidents (ID, incident, store, date, status, view). Used by the list and the overview. */
export function IncidentTable({ incidents }: { incidents: Incident[] }) {
  return (
    <div className="overflow-x-auto px-3 pb-3">
      <table className="w-full min-w-[720px] text-left text-sm">
        <thead>
          <tr className="bg-surface text-xs font-semibold tracking-wide text-muted uppercase">
            <th className="rounded-l-lg px-3 py-3">ID</th>
            <th className="px-3 py-3">Incident</th>
            <th className="px-3 py-3">Store</th>
            <th className="px-3 py-3">Date</th>
            <th className="px-3 py-3">Status</th>
            <th className="rounded-r-lg px-3 py-3 text-right">
              <span className="sr-only">Action</span>
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-line">
          {incidents.map((incident) => {
            const ref = incidentRef(incident.incidentId)
            const to = `/incidents/${incident.incidentId}`
            return (
              <tr key={incident.incidentId} className="hover:bg-surface/60">
                <td className="px-3 py-3.5 font-semibold whitespace-nowrap">
                  <Link to={to} className="text-brand-600 hover:underline">
                    {ref}
                  </Link>
                </td>
                <td className="px-3 py-3.5">
                  <div className="font-medium">{incidentTitle(incident)}</div>
                  <div className="text-xs text-muted">{incident.customerName}</div>
                </td>
                <td className="px-3 py-3.5 text-ink/90">{incidentPlace(incident) ?? <span className="text-muted">—</span>}</td>
                <td className="px-3 py-3.5 whitespace-nowrap text-ink/90">{formatDate(incidentDisplayDate(incident))}</td>
                <td className="px-3 py-3.5">
                  <div className="flex items-center gap-1.5">
                    <StatusBadge status={incident.status} />
                    {incident.hasPendingDuplicateFlag && (
                      <span title="Possible duplicate — awaiting review" className="text-amber-600">
                        <AlertTriangle className="size-4" aria-label="Possible duplicate" />
                      </span>
                    )}
                  </div>
                </td>
                <td className="px-3 py-3.5 text-right">
                  <Link to={to} className="font-semibold text-brand-600 hover:underline" aria-label={`View ${ref}`}>
                    View
                  </Link>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
