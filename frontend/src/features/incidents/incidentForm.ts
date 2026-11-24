import type { Incident, IncidentInput, IncidentPriority, IncidentType } from './types'

/** Raw form values: everything is a string, as the inputs give it. */
export type IncidentFormValues = {
  title: string
  incidentType: string
  priority: string
  date: string // YYYY-MM-DD
  time: string // HH:mm
  storeId: string
  locationId: string
  resolutionTypeId: string
  description: string
  customerName: string
  customerEmail: string
  customerPhone: string
  customerAddress: string
  compensation: string
}

export type FormErrors = Partial<Record<keyof IncidentFormValues, string>>

const pad = (n: number) => String(n).padStart(2, '0')

export function emptyIncidentForm(now = new Date()): IncidentFormValues {
  return {
    title: '',
    incidentType: '',
    priority: '',
    date: `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`,
    time: `${pad(now.getHours())}:${pad(now.getMinutes())}`,
    storeId: '',
    locationId: '',
    resolutionTypeId: '',
    description: '',
    customerName: '',
    customerEmail: '',
    customerPhone: '',
    customerAddress: '',
    compensation: '',
  }
}

/** Form values from an existing incident (for editing). */
export function incidentToForm(incident: Incident): IncidentFormValues {
  // incidentDate is a wall-clock "YYYY-MM-DDTHH:mm:ss" string.
  const [date = '', timeFull = ''] = incident.incidentDate?.split('T') ?? []
  const s = (v: string | null | undefined) => v ?? ''
  const n = (v: number | null) => (v === null ? '' : String(v))
  return {
    title: s(incident.title),
    incidentType: n(incident.incidentType),
    priority: n(incident.priority),
    date,
    time: timeFull.slice(0, 5),
    storeId: n(incident.storeId),
    locationId: n(incident.locationId),
    resolutionTypeId: n(incident.resolutionTypeId),
    description: s(incident.description),
    customerName: incident.customerName,
    customerEmail: s(incident.customerEmail),
    customerPhone: s(incident.customerPhone),
    customerAddress: s(incident.customerAddress),
    compensation: incident.estimatedCompensationAmount === null ? '' : incident.estimatedCompensationAmount.toFixed(2),
  }
}

// Same limits as the backend DTO.
const MAX_LENGTH: Partial<Record<keyof IncidentFormValues, number>> = {
  title: 200,
  description: 4000,
  customerName: 200,
  customerEmail: 256,
  customerPhone: 50,
  customerAddress: 500,
}

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

/** "1.234,50" / "1234.5" -> 1234.5; empty -> null; invalid -> NaN */
function parseAmount(value: string): number | null {
  const v = value.trim().replace(/\s/g, '')
  if (!v) return null
  // Accept a comma as the decimal separator.
  const normalised = v.includes(',') ? v.replace(/\./g, '').replace(',', '.') : v
  return /^\d+(\.\d{1,2})?$/.test(normalised) ? Number(normalised) : Number.NaN
}

/**
 * Draft: only the customer name is required (as in the API).
 * Submit: every field except compensation is required (IncidentService.EnsureRequiredForSubmit).
 */
export function validateIncidentForm(values: IncidentFormValues, mode: 'draft' | 'submit'): FormErrors {
  const errors: FormErrors = {}
  const required: (keyof IncidentFormValues)[] =
    mode === 'draft'
      ? ['customerName']
      : [
          'title',
          'incidentType',
          'priority',
          'date',
          'time',
          'storeId',
          'locationId',
          'resolutionTypeId',
          'description',
          'customerName',
          'customerEmail',
          'customerPhone',
          'customerAddress',
        ]

  for (const key of required) {
    if (!values[key].trim()) errors[key] = 'Required.'
  }

  for (const [key, max] of Object.entries(MAX_LENGTH) as [keyof IncidentFormValues, number][]) {
    if (values[key].trim().length > max) errors[key] = `At most ${max} characters.`
  }

  if (values.customerEmail.trim() && !EMAIL.test(values.customerEmail.trim())) {
    errors.customerEmail = 'Enter a valid email address.'
  }

  // A date without a time (or the other way round) can't be saved.
  if (values.date && !values.time) errors.time ??= 'Enter the time too.'
  if (values.time && !values.date) errors.date ??= 'Enter the date too.'
  if (values.date && values.time && new Date(`${values.date}T${values.time}`) > new Date()) {
    errors.date = 'The incident cannot be in the future.'
  }

  const amount = parseAmount(values.compensation)
  if (Number.isNaN(amount)) errors.compensation = 'Enter an amount like 150 or 150.50.'

  return errors
}

const textOrNull = (v: string) => v.trim() || null
const idOrNull = (v: string) => (v ? Number(v) : null)

export function toIncidentInput(values: IncidentFormValues): IncidentInput {
  return {
    customerName: values.customerName.trim(),
    title: textOrNull(values.title),
    description: textOrNull(values.description),
    priority: values.priority ? (Number(values.priority) as IncidentPriority) : null,
    incidentType: values.incidentType ? (Number(values.incidentType) as IncidentType) : null,
    incidentDate: values.date && values.time ? `${values.date}T${values.time}:00` : null,
    storeId: idOrNull(values.storeId),
    locationId: idOrNull(values.locationId),
    resolutionTypeId: idOrNull(values.resolutionTypeId),
    customerAddress: textOrNull(values.customerAddress),
    customerEmail: textOrNull(values.customerEmail),
    customerPhone: textOrNull(values.customerPhone),
    estimatedCompensationAmount: parseAmount(values.compensation),
  }
}

/** Maps API field names (e.g. "CustomerEmail", "$.customerEmail") to form fields. */
export function mapApiFieldErrors(fieldErrors: Record<string, string[]>): FormErrors {
  const map: Record<string, keyof IncidentFormValues> = {
    title: 'title',
    description: 'description',
    priority: 'priority',
    incidenttype: 'incidentType',
    incidentdate: 'date',
    storeid: 'storeId',
    locationid: 'locationId',
    resolutiontypeid: 'resolutionTypeId',
    customername: 'customerName',
    customeraddress: 'customerAddress',
    customeremail: 'customerEmail',
    customerphone: 'customerPhone',
    estimatedcompensationamount: 'compensation',
  }
  const errors: FormErrors = {}
  for (const [key, messages] of Object.entries(fieldErrors)) {
    const field = map[key.replace(/^\$\./, '').toLowerCase()]
    if (field && messages[0]) errors[field] = messages[0]
  }
  return errors
}
