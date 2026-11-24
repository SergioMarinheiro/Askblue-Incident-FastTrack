import { useState, type FormEvent } from 'react'
import { AlertCircle, Loader2 } from 'lucide-react'
import { FormField } from '@/components/ui/FormField'
import { Modal } from '@/components/ui/Modal'
import { buttonPrimaryClass } from '@/components/ui/form'
import { resetUserPassword, type User } from './api'
import { PasswordInput } from './PasswordInput'

export function ResetPasswordModal({ user, onClose, onDone }: { user: User; onClose: () => void; onDone: () => void }) {
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (!password) {
      setError('Enter a new password.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      await resetUserPassword(user.userId, password)
      onDone()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not reset the password.')
      setBusy(false)
    }
  }

  return (
    <Modal
      title={`Reset password for ${user.fullName}`}
      onClose={onClose}
      locked={busy}
      footer={
        <>
          <button type="button" onClick={onClose} disabled={busy} className="px-3 py-2.5 text-sm font-semibold text-muted hover:text-ink">
            Cancel
          </button>
          <button type="submit" form="reset-form" disabled={busy} className={buttonPrimaryClass}>
            {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
            Set new password
          </button>
        </>
      }
    >
      <form id="reset-form" noValidate onSubmit={handleSubmit} className="space-y-4">
        {error && (
          <div role="alert" className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2.5 text-sm text-red-700">
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden />
            <span>{error}</span>
          </div>
        )}
        <FormField id="newPassword" label="New password" required>
          <PasswordInput id="newPassword" value={password} onChange={setPassword} disabled={busy} />
        </FormField>
      </form>
    </Modal>
  )
}
