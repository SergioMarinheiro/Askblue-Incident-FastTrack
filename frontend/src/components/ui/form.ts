// Shared Tailwind classes for form controls, so every input looks the same.
export const inputClass =
  'block w-full rounded-lg border border-line bg-white px-3 py-2.5 text-sm outline-none placeholder:text-muted/70 focus:border-brand-500 focus:ring-2 focus:ring-brand-100 disabled:cursor-not-allowed disabled:bg-surface disabled:text-muted'

export const buttonPrimaryClass =
  'inline-flex items-center justify-center gap-2 rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-70'

export const buttonSecondaryClass =
  'inline-flex items-center justify-center gap-2 rounded-lg border border-brand-600 bg-white px-4 py-2.5 text-sm font-semibold text-brand-600 transition-colors hover:bg-brand-50 disabled:cursor-not-allowed disabled:opacity-70'

export const buttonDangerClass =
  'inline-flex items-center justify-center gap-2 rounded-lg border border-red-300 bg-white px-4 py-2.5 text-sm font-semibold text-red-700 transition-colors hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-70'
