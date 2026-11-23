import type { ReactNode } from 'react'
import { AlertCircle, Inbox, Loader2 } from 'lucide-react'

export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className="flex items-center justify-center gap-2 px-6 py-14 text-sm text-muted" role="status">
      <Loader2 className="size-4 animate-spin" aria-hidden />
      {label}
    </div>
  )
}

export function EmptyState({ title, description, action }: { title: string; description?: string; action?: ReactNode }) {
  return (
    <div className="flex flex-col items-center px-6 py-14 text-center">
      <div className="flex size-11 items-center justify-center rounded-full bg-surface text-muted">
        <Inbox className="size-5" aria-hidden />
      </div>
      <p className="mt-3 font-semibold">{title}</p>
      {description && <p className="mt-1 text-sm text-muted">{description}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  )
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="flex flex-col items-center px-6 py-14 text-center" role="alert">
      <div className="flex size-11 items-center justify-center rounded-full bg-red-50 text-red-600">
        <AlertCircle className="size-5" aria-hidden />
      </div>
      <p className="mt-3 font-semibold">Could not load data</p>
      <p className="mt-1 text-sm text-muted">{message}</p>
      {onRetry && (
        <button type="button" onClick={onRetry} className="mt-4 text-sm font-semibold text-brand-600 hover:underline">
          Try again
        </button>
      )}
    </div>
  )
}
