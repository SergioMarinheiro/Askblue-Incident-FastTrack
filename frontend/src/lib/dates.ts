/*
 * The API sends two kinds of date-times, both without a timezone marker:
 * - Audit times (createdAt, approvedAt, ...) are stored in UTC  -> parseUtc, shown in the viewer's local time.
 * - The incident date is what the user typed (local wall-clock) -> parseWallClock, shown as-is.
 */

const HAS_OFFSET = /(Z|[+-]\d{2}:?\d{2})$/i

// .NET writes up to 7 fractional-second digits ("10:25:00.1234567"); not every browser parses
// more than 3, so they are cut to milliseconds first.
const trimFraction = (value: string) => value.replace(/(\.\d{3})\d+/, '$1')

export function parseUtc(value: string): Date {
  const v = trimFraction(value)
  return new Date(HAS_OFFSET.test(v) ? v : `${v}Z`)
}

export function parseWallClock(value: string): Date {
  // Without an offset, JS reads "YYYY-MM-DDTHH:mm:ss" as local time, which is what we want.
  return new Date(trimFraction(value).replace(HAS_OFFSET, ''))
}

const dateFormat = new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: '2-digit', year: 'numeric' })
const timeFormat = new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit' })

/** 12/11/2026 */
export function formatDate(date: Date) {
  return dateFormat.format(date)
}

/** 12/11/2026 · 10:25 */
export function formatDateTime(date: Date) {
  return `${dateFormat.format(date)} · ${timeFormat.format(date)}`
}
