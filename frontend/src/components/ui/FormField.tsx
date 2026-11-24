import type { ReactNode } from 'react'
import { cn } from '@/lib/cn'

type Props = {
  id: string
  label: string
  required?: boolean
  error?: string
  hint?: ReactNode
  className?: string
  children: ReactNode
}

/** Label + control + error/hint, laid out the same way everywhere. */
export function FormField({ id, label, required, error, hint, className, children }: Props) {
  return (
    <div className={className}>
      <label htmlFor={id} className="block text-sm font-semibold">
        {label}
        {required && (
          <span className="ml-0.5 text-red-600" aria-hidden>
            *
          </span>
        )}
      </label>
      <div className="mt-1.5">{children}</div>
      {error ? (
        <p id={`${id}-error`} className="mt-1 text-xs text-red-600">
          {error}
        </p>
      ) : (
        hint && <p className="mt-1 text-xs text-muted">{hint}</p>
      )}
    </div>
  )
}

/** Input classes with a red border when there is an error. */
export function controlClass(base: string, error?: string) {
  return cn(base, error && 'border-red-400 focus:border-red-500 focus:ring-red-100')
}
