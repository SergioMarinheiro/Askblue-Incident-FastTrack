import { parseUtc } from '@/lib/dates'
import { STATUS_INFO } from './labels'
import { IncidentStatus as S, type Incident, type IncidentStatus } from './types'

export type StepState = 'done' | 'current' | 'warning' | 'failed' | 'upcoming'

export type TimelineStep = {
  label: string
  state: StepState
  date?: Date
}

const d = (value: string | null) => (value ? parseUtc(value) : undefined)

/** Step names are the status names, so the timeline reads the same as the status badge. */
const name = (status: IncidentStatus) => STATUS_INFO[status].label

/**
 * Builds the "Incident status" steps: the statuses the incident goes through.
 * Happy path: New → Awaiting approval → Awaiting payment / Awaiting resolution → Completed.
 * Sent back, Rejected, Duplicate and Canceled appear under their own names.
 *
 * `requiresPayment`: from the incident's resolution type, to name the step after approval.
 * Undefined when unknown (no resolution type chosen yet, or the list hasn't loaded).
 */
export function buildTimeline(incident: Incident, requiresPayment?: boolean): TimelineStep[] {
  const status = incident.status

  const afterApprovalName =
    status === S.AwaitingPayment || (status !== S.AwaitingResolution && requiresPayment === true)
      ? name(S.AwaitingPayment)
      : status === S.AwaitingResolution || requiresPayment === false
        ? name(S.AwaitingResolution)
        : `${name(S.AwaitingPayment)} or resolution`

  const created: TimelineStep = { label: name(S.New), state: 'done', date: d(incident.createdAt) }
  const approvalDone: TimelineStep = {
    label: name(S.AwaitingApproval),
    state: 'done',
    date: d(incident.approvedAt),
  }
  const upcoming = (label: string): TimelineStep => ({ label, state: 'upcoming' })

  switch (status) {
    case S.New:
      return [
        { label: name(S.New), state: 'current', date: d(incident.createdAt) },
        upcoming(name(S.AwaitingApproval)),
        upcoming(afterApprovalName),
        upcoming(name(S.Completed)),
      ]
    case S.AwaitingApproval:
      return [
        created,
        { label: name(S.AwaitingApproval), state: 'current' },
        upcoming(afterApprovalName),
        upcoming(name(S.Completed)),
      ]
    case S.SentBack:
      return [
        created,
        { label: name(S.SentBack), state: 'warning' },
        upcoming(name(S.AwaitingApproval)),
        upcoming(afterApprovalName),
        upcoming(name(S.Completed)),
      ]
    case S.AwaitingPayment:
    case S.AwaitingResolution:
      return [
        created,
        approvalDone,
        { label: name(status), state: 'current' },
        upcoming(name(S.Completed)),
      ]
    case S.Completed:
      return [
        created,
        approvalDone,
        { label: afterApprovalName, state: 'done' },
        { label: name(S.Completed), state: 'done', date: d(incident.closedAt) },
      ]
    case S.Rejected:
      return [created, { label: name(S.AwaitingApproval), state: 'done' }, { label: name(S.Rejected), state: 'failed', date: d(incident.closedAt) }]
    case S.Duplicate:
      return [created, { label: name(S.AwaitingApproval), state: 'done' }, { label: name(S.Duplicate), state: 'failed', date: d(incident.closedAt) }]
    case S.Canceled:
      // Drafts are deleted, not canceled, so a canceled incident was at least submitted.
      return [
        created,
        incident.approvedAt ? approvalDone : { label: name(S.AwaitingApproval), state: 'done' },
        ...(incident.approvedAt ? [{ label: afterApprovalName, state: 'done' } as TimelineStep] : []),
        { label: name(S.Canceled), state: 'failed', date: d(incident.closedAt) },
      ]
    default:
      return [created]
  }
}
