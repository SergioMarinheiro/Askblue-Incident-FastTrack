import type { BadgeTone } from '@/components/ui/Badge'
import { parseUtc, parseWallClock } from '@/lib/dates'
import { IncidentStatus, type Incident, type IncidentPriority, type IncidentType } from './types'

export const STATUS_INFO: Record<IncidentStatus, { label: string; tone: BadgeTone }> = {
  [IncidentStatus.New]: { label: 'New', tone: 'cyan' },
  [IncidentStatus.AwaitingApproval]: { label: 'Awaiting approval', tone: 'amber' },
  [IncidentStatus.AwaitingPayment]: { label: 'Awaiting payment', tone: 'violet' },
  [IncidentStatus.AwaitingResolution]: { label: 'Awaiting resolution', tone: 'blue' },
  [IncidentStatus.Completed]: { label: 'Completed', tone: 'green' },
  [IncidentStatus.Rejected]: { label: 'Rejected', tone: 'red' },
  [IncidentStatus.SentBack]: { label: 'Sent back', tone: 'orange' },
  [IncidentStatus.Canceled]: { label: 'Canceled', tone: 'red' },
  [IncidentStatus.Duplicate]: { label: 'Duplicate', tone: 'red' },
}

/** Statuses in the order they are shown in filters. */
export const STATUS_ORDER: IncidentStatus[] = [
  IncidentStatus.New,
  IncidentStatus.AwaitingApproval,
  IncidentStatus.SentBack,
  IncidentStatus.AwaitingPayment,
  IncidentStatus.AwaitingResolution,
  IncidentStatus.Completed,
  IncidentStatus.Rejected,
  IncidentStatus.Canceled,
  IncidentStatus.Duplicate,
]

export const TYPE_LABELS: Record<IncidentType, string> = {
  1: 'Customer injury',
  2: 'Property damage',
  3: 'Workplace accident',
}

export const PRIORITY_LABELS: Record<IncidentPriority, string> = {
  1: 'Low',
  2: 'Medium',
  3: 'High',
  4: 'Critical',
}

/** Display reference, e.g. IF-0042. Built from the id; the backend has no separate reference number. */
export function incidentRef(id: number) {
  return `IF-${String(id).padStart(4, '0')}`
}

/** Best short name for an incident: title, else its type, else a fallback. */
export function incidentTitle(incident: Incident) {
  return incident.title?.trim() || (incident.incidentType ? TYPE_LABELS[incident.incidentType] : null) || 'Untitled incident'
}

/** "Lisbon Store — Centre", or whichever part is known. */
export function incidentPlace(incident: Incident) {
  return [incident.storeName, incident.locationName].filter(Boolean).join(' — ') || null
}

/** When it happened if known, otherwise when it was recorded. */
export function incidentDisplayDate(incident: Incident): Date {
  return incident.incidentDate ? parseWallClock(incident.incidentDate) : parseUtc(incident.createdAt)
}
