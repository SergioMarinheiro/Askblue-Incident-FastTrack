import { useEffect, useRef, type ReactNode } from 'react'

type Props = {
  title: string
  onClose: () => void
  /** Prevents closing (Escape / backdrop) while something is in progress. */
  locked?: boolean
  children: ReactNode
  footer?: ReactNode
}

export function Modal({ title, onClose, locked, children, footer }: Props) {
  const dialogRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    dialogRef.current?.focus()
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !locked) onClose()
    }
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [onClose, locked])

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-4">
      <div className="absolute inset-0 bg-navy-950/50" onClick={() => !locked && onClose()} aria-hidden />
      {/* Never taller than the screen: the title and buttons stay put and the middle scrolls. */}
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
        tabIndex={-1}
        className="relative flex max-h-[calc(100dvh-1.5rem)] w-full max-w-lg flex-col rounded-xl bg-white shadow-xl outline-none sm:max-h-[calc(100dvh-2rem)]"
      >
        <h2 id="modal-title" className="shrink-0 px-5 pt-5 text-lg font-semibold sm:px-6 sm:pt-6">
          {title}
        </h2>
        <div className="min-h-0 flex-1 overflow-y-auto px-5 pt-3 pb-5 sm:px-6 sm:pb-6">{children}</div>
        {footer && <div className="flex shrink-0 flex-wrap justify-end gap-3 border-t border-line px-5 py-4 sm:px-6">{footer}</div>}
      </div>
    </div>
  )
}
