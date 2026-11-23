import { useState, type ReactNode } from 'react'
import { AlertCircle, Loader2 } from 'lucide-react'
import { Modal } from './Modal'
import { buttonPrimaryClass, inputClass } from './form'
import { cn } from '@/lib/cn'

type Props = {
  title: string
  description?: ReactNode
  confirmLabel: string
  tone?: 'primary' | 'danger'
  /** When set, asks for a required text (reason, comment). */
  textField?: { label: string; placeholder?: string; maxLength: number }
  onConfirm: (text: string) => Promise<void>
  onClose: () => void
}

const dangerButton =
  'inline-flex items-center justify-center gap-2 rounded-lg bg-red-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-70'

/** Confirmation popup for an action; shows the API error inside if the action fails. */
export function ActionDialog({ title, description, confirmLabel, tone = 'primary', textField, onConfirm, onClose }: Props) {
  const [text, setText] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const missingText = textField !== undefined && text.trim() === ''

  async function confirm() {
    if (missingText) {
      setError(`${textField.label} is required.`)
      return
    }
    setBusy(true)
    setError(null)
    try {
      await onConfirm(text.trim())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong. Please try again.')
      setBusy(false)
    }
  }

  return (
    <Modal
      title={title}
      onClose={onClose}
      locked={busy}
      footer={
        <>
          <button type="button" onClick={onClose} disabled={busy} className="px-3 py-2.5 text-sm font-semibold text-muted hover:text-ink">
            Cancel
          </button>
          <button type="button" onClick={confirm} disabled={busy} className={tone === 'danger' ? dangerButton : buttonPrimaryClass}>
            {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
            {confirmLabel}
          </button>
        </>
      }
    >
      {description && <div className="text-sm text-muted">{description}</div>}

      {textField && (
        <div className={description ? 'mt-4' : undefined}>
          <label htmlFor="action-text" className="block text-sm font-semibold">
            {textField.label}
            <span className="ml-0.5 text-red-600" aria-hidden>
              *
            </span>
          </label>
          <textarea
            id="action-text"
            rows={4}
            autoFocus
            maxLength={textField.maxLength}
            value={text}
            onChange={(e) => setText(e.target.value)}
            placeholder={textField.placeholder}
            disabled={busy}
            className={cn(inputClass, 'mt-1.5 resize-y')}
          />
          <p className="mt-1 text-right text-xs text-muted">
            {text.length}/{textField.maxLength}
          </p>
        </div>
      )}

      {error && (
        <div role="alert" className="mt-4 flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2.5 text-sm text-red-700">
          <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden />
          <span>{error}</span>
        </div>
      )}
    </Modal>
  )
}
