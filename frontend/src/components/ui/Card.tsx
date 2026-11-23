import type { ReactNode } from 'react'
import { cn } from '@/lib/cn'

export function Card({ children, className }: { children: ReactNode; className?: string }) {
  return <section className={cn('rounded-xl border border-line bg-white shadow-sm', className)}>{children}</section>
}

export function CardHeader({ title, actions }: { title: ReactNode; actions?: ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-4 px-6 pt-5 pb-4">
      <h2 className="text-lg font-semibold">{title}</h2>
      {actions && <div className="text-sm text-muted">{actions}</div>}
    </div>
  )
}
