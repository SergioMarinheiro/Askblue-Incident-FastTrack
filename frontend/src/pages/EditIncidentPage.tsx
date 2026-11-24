import { Link, useParams } from 'react-router'
import { useAuth } from '@/auth/useAuth'
import { Card } from '@/components/ui/Card'
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/States'
import { useIncident } from '@/features/incidents/api'
import { IncidentEditor } from '@/features/incidents/IncidentEditor'
import { incidentPermissions } from '@/features/incidents/permissions'

export function EditIncidentPage() {
  const id = Number(useParams().id)
  const { user } = useAuth()
  const { data: incident, isPending, isError, error, refetch } = useIncident(Number.isInteger(id) && id > 0 ? id : 0)

  if (isPending) return <LoadingState label="Loading incident…" />
  if (isError) {
    return (
      <Card>
        <ErrorState message={error.message} onRetry={() => refetch()} />
      </Card>
    )
  }
  if (!incidentPermissions(user, incident).edit) {
    return (
      <Card>
        <EmptyState
          title="This incident can't be edited"
          description="Its current status or your role does not allow changes."
          action={
            <Link to={`/incidents/${id}`} className="text-sm font-semibold text-brand-600 hover:underline">
              Back to the incident
            </Link>
          }
        />
      </Card>
    )
  }
  // key: start from fresh values if another incident is opened in the same editor.
  return <IncidentEditor key={incident.incidentId} incident={incident} />
}
