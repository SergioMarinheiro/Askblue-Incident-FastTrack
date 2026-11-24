// Turns raw history rows (backend field names, text values) into readable change sets.
import { formatDateTime, parseUtc, parseWallClock } from '@/lib/dates'
import { PRIORITY_LABELS, STATUS_INFO, TYPE_LABELS } from './labels'
import { IncidentPriority, IncidentStatus, IncidentType, type HistoryEntry } from './types'

export type NameLookup = {
  store: (id: number) => string | undefined
  location: (id: number) => string | undefined
  resolutionType: (id: number) => string | undefined
}

export type FieldChange = {
  key: number
  field: string
  label: string
  /** Long text (description, reason): shown as "changed" with a toggle for before/after. */
  long: boolean
  from: string | null
  to: string | null
}

export type ChangeSet = {
  id: string
  userName: string
  at: Date
  changes: FieldChange[]
}

const FIELD_LABELS: Record<string, string> = {
  Status: 'Status',
  Title: 'Title',
  Description: 'Description',
  Priority: 'Priority',
  IncidentType: 'Type',
  IncidentDate: 'Date and time',
  StoreId: 'Store',
  LocationId: 'Location',
  ResolutionTypeId: 'Resolution type',
  CustomerName: 'Customer name',
  CustomerAddress: 'Customer address',
  CustomerEmail: 'Customer email',
  CustomerPhone: 'Customer phone',
  EstimatedCompensationAmount: 'Estimated compensation',
  RejectionReason: 'Rejection reason',
}

const LONG_FIELDS = new Set(['Description', 'RejectionReason'])

// Status first, then the order fields appear on the form.
const FIELD_ORDER = Object.keys(FIELD_LABELS)

const money = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'EUR' })

// Enum names as the backend writes them -> our labels.
const byName = <T extends Record<string, number>>(e: T, labels: Record<number, string>) =>
  Object.fromEntries(Object.entries(e).map(([name, n]) => [name, labels[n]])) as Record<string, string>

const STATUS_BY_NAME = byName(
  IncidentStatus,
  Object.fromEntries(Object.entries(STATUS_INFO).map(([n, i]) => [n, i.label])),
)
const PRIORITY_BY_NAME = byName(IncidentPriority, PRIORITY_LABELS)
const TYPE_BY_NAME = byName(IncidentType, TYPE_LABELS)

function formatValue(field: string, raw: string | null, names: NameLookup): string | null {
  if (raw === null || raw === '') return null
  const id = Number(raw)
  switch (field) {
    case 'Status':
      return STATUS_BY_NAME[raw] ?? raw
    case 'Priority':
      return PRIORITY_BY_NAME[raw] ?? raw
    case 'IncidentType':
      return TYPE_BY_NAME[raw] ?? raw
    case 'IncidentDate':
      return formatDateTime(parseWallClock(raw))
    case 'EstimatedCompensationAmount':
      return Number.isNaN(Number(raw)) ? raw : money.format(Number(raw))
    case 'StoreId':
      return names.store(id) ?? `Store #${raw}`
    case 'LocationId':
      return names.location(id) ?? `Location #${raw}`
    case 'ResolutionTypeId':
      return names.resolutionType(id) ?? `Resolution type #${raw}`
    default:
      return raw
  }
}

/** Groups rows by change set, newest first; fields within a set in form order. */
export function buildChangeSets(rows: HistoryEntry[], names: NameLookup): ChangeSet[] {
  const sets = new Map<string, ChangeSet>()
  for (const r of rows) {
    let set = sets.get(r.changeSetId)
    if (!set) {
      set = { id: r.changeSetId, userName: r.changedByUserName, at: parseUtc(r.changedAt), changes: [] }
      sets.set(r.changeSetId, set)
    }
    set.changes.push({
      key: r.historyId,
      field: r.fieldName,
      label: FIELD_LABELS[r.fieldName] ?? r.fieldName,
      long: LONG_FIELDS.has(r.fieldName),
      from: formatValue(r.fieldName, r.oldValue, names),
      to: formatValue(r.fieldName, r.newValue, names),
    })
  }
  const order = (f: string) => {
    const i = FIELD_ORDER.indexOf(f)
    return i === -1 ? FIELD_ORDER.length : i
  }
  for (const s of sets.values()) s.changes.sort((a, b) => order(a.field) - order(b.field))
  return [...sets.values()].sort((a, b) => b.at.getTime() - a.at.getTime())
}

/** Store ids mentioned in the history (to load their locations for names). */
export function storeIdsInHistory(rows: HistoryEntry[]): number[] {
  const ids = new Set<number>()
  for (const r of rows) {
    if (r.fieldName !== 'StoreId') continue
    for (const v of [r.oldValue, r.newValue]) if (v && !Number.isNaN(Number(v))) ids.add(Number(v))
  }
  return [...ids]
}
