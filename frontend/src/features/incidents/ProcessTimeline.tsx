import { Check, X } from 'lucide-react'
import { cn } from '@/lib/cn'
import { formatDateTime } from '@/lib/dates'
import type { StepState, TimelineStep } from './timeline'

const dotClass: Record<StepState, string> = {
  done: 'bg-emerald-600 text-white',
  current: 'bg-brand-600 ring-4 ring-brand-100',
  warning: 'bg-orange-500 ring-4 ring-orange-100',
  failed: 'bg-red-600 text-white',
  upcoming: 'bg-slate-300',
}

export function ProcessTimeline({ steps }: { steps: TimelineStep[] }) {
  return (
    <ol className="relative">
      {steps.map((step, i) => {
        const isLast = i === steps.length - 1
        return (
          <li key={step.label} className="relative flex gap-3 pb-6 last:pb-0">
            {!isLast && <span className="absolute top-5 bottom-0 left-[9px] w-0.5 bg-line" aria-hidden />}
            <span
              className={cn('relative mt-0.5 flex size-5 shrink-0 items-center justify-center rounded-full', dotClass[step.state])}
              aria-hidden
            >
              {step.state === 'done' && <Check className="size-3" strokeWidth={3} />}
              {step.state === 'failed' && <X className="size-3" strokeWidth={3} />}
            </span>
            <div className="min-w-0">
              <p className={cn('text-sm font-semibold', step.state === 'upcoming' && 'text-muted')}>{step.label}</p>
              {step.date && <p className="text-xs text-muted">{formatDateTime(step.date)}</p>}
            </div>
          </li>
        )
      })}
    </ol>
  )
}
