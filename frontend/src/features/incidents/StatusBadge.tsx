import { Badge } from '@/components/ui/Badge'
import { STATUS_INFO } from './labels'
import type { IncidentStatus } from './types'

export function StatusBadge({ status }: { status: IncidentStatus }) {
  const info = STATUS_INFO[status] ?? { label: `Unknown (${status})`, tone: 'gray' as const }
  return <Badge tone={info.tone}>{info.label}</Badge>
}
