import { Link, useParams } from 'react-router'
import { Check } from 'lucide-react'
import { Card } from '@/components/ui/Card'
import { buttonPrimaryClass, buttonSecondaryClass } from '@/components/ui/form'
import { useIncident } from '@/features/incidents/api'
import { incidentRef } from '@/features/incidents/labels'
import { StatusBadge } from '@/features/incidents/StatusBadge'

export function IncidentSubmittedPage() {
  const id = Number(useParams().id)
  const { data: incident } = useIncident(Number.isInteger(id) && id > 0 ? id : 0)

  return (
    <div className="flex justify-center py-6">
      <Card className="w-full max-w-lg px-8 py-10 text-center">
        <div className="mx-auto flex size-16 items-center justify-center rounded-full bg-emerald-50 text-emerald-700">
          <Check className="size-8" strokeWidth={2.5} aria-hidden />
        </div>
        <h1 className="mt-5 text-2xl font-bold">Incident submitted!</h1>
        <p className="mt-2 text-sm text-muted">It has been sent for approval and you can follow its progress.</p>

        <div className="mt-7 flex items-center justify-between rounded-xl border border-line bg-surface px-5 py-4 text-left">
          <div>
            <p className="text-xs font-semibold tracking-wide text-muted uppercase">Incident reference</p>
            <p className="mt-1 text-xl font-bold">{incidentRef(id)}</p>
          </div>
          {incident && <StatusBadge status={incident.status} />}
        </div>

        <div className="mt-7 flex flex-col gap-3">
          <Link to={`/incidents/${id}`} className={buttonPrimaryClass}>
            Follow incident
          </Link>
          <Link to="/overview" className={buttonSecondaryClass}>
            Back to overview
          </Link>
        </div>
      </Card>
    </div>
  )
}
