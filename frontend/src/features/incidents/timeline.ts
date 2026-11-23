import { parseUtc } from '@/lib/dates'
import { IncidentStatus, type Incident } from './types'

export type StepState = 'done' | 'current' | 'warning' | 'failed' | 'upcoming'

export type TimelineStep = {
  label: string
  state: StepState
  /** Short line under the label, e.g. "Waiting for a manager". */
  note?: string
  date?: Date
}

const d = (value: string | null) => (value ? parseUtc(value) : undefined)

/**
 * Builds the "Process status" steps for an incident.
 * Happy path: Registered → Submitted → Approved → Completed.
 * Rejected / Canceled / Duplicate replace the remaining steps with a closing step.
 */
export function buildTimeline(incident: Incident): TimelineStep[] {
  const registered: TimelineStep = { label: 'Registered', state: 'done', date: d(incident.createdAt) }
  const approvedDone: TimelineStep = { label: 'Approved', state: 'done', date: d(incident.approvedAt) }

  switch (incident.status) {
    case IncidentStatus.New:
      return [
        registered,
        { label: 'Submitted', state: 'current', note: 'Draft — not submitted yet' },
        { label: 'Approved', state: 'upcoming' },
        { label: 'Completed', state: 'upcoming' },
      ]
    case IncidentStatus.SentBack:
      return [
        registered,
        { label: 'Sent back', state: 'warning', note: 'Changes requested — resubmit when fixed' },
        { label: 'Approved', state: 'upcoming' },
        { label: 'Completed', state: 'upcoming' },
      ]
    case IncidentStatus.AwaitingApproval:
      return [
        registered,
        { label: 'Submitted', state: 'done' },
        { label: 'Approval', state: 'current', note: 'Waiting for a manager' },
        { label: 'Completed', state: 'upcoming' },
      ]
    case IncidentStatus.AwaitingPayment:
    case IncidentStatus.AwaitingResolution:
      return [
        registered,
        { label: 'Submitted', state: 'done' },
        approvedDone,
        {
          label: 'Completed',
          state: 'current',
          note: incident.status === IncidentStatus.AwaitingPayment ? 'Awaiting payment' : 'Awaiting resolution',
        },
      ]
    case IncidentStatus.Completed:
      return [
        registered,
        { label: 'Submitted', state: 'done' },
        approvedDone,
        { label: 'Completed', state: 'done', date: d(incident.closedAt) },
      ]
    case IncidentStatus.Rejected:
      return [
        registered,
        { label: 'Submitted', state: 'done' },
        { label: 'Rejected', state: 'failed', date: d(incident.closedAt) },
      ]
    case IncidentStatus.Duplicate:
      return [
        registered,
        { label: 'Submitted', state: 'done' },
        { label: 'Closed as duplicate', state: 'failed', date: d(incident.closedAt) },
      ]
    case IncidentStatus.Canceled:
      return [
        registered,
        ...(incident.approvedAt ? [approvedDone] : []),
        { label: 'Canceled', state: 'failed', date: d(incident.closedAt) },
      ]
    default:
      return [registered]
  }
}
