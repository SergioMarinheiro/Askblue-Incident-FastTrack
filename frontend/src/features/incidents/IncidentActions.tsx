import { useState, type ReactNode } from 'react'
import { Link, useNavigate } from 'react-router'
import { useQueryClient } from '@tanstack/react-query'
import { AlertCircle, Ban, Check, CheckCheck, CopyX, Loader2, Pencil, Send, Undo2, X } from 'lucide-react'
import { ActionDialog } from '@/components/ui/ActionDialog'
import { Card, CardHeader } from '@/components/ui/Card'
import { cn } from '@/lib/cn'
import { deleteDraftIncident, incidentActions, incidentKeys, submitIncident } from './api'
import { DuplicatesModal } from './DuplicatesModal'
import { incidentRef } from './labels'
import type { IncidentPermissions } from './permissions'
import type { Incident, PossibleDuplicate } from './types'

type DialogKey = 'approve' | 'reject' | 'sendBack' | 'complete' | 'cancel' | 'dismiss' | 'confirmDuplicate'

type DialogConfig = {
  title: string
  description: ReactNode
  confirmLabel: string
  tone?: 'primary' | 'danger'
  textField?: { label: string; placeholder?: string; maxLength: number }
  run: (text: string) => Promise<Incident>
}

const actionButton =
  'flex w-full items-center gap-2 rounded-lg border px-3 py-2.5 text-left text-sm font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-60'
const tones = {
  primary: 'border-brand-600 bg-brand-600 text-white hover:bg-brand-700',
  neutral: 'border-line bg-white text-ink hover:bg-surface',
  danger: 'border-red-200 bg-white text-red-700 hover:bg-red-50',
}

/** The buttons a user may use on this incident, based on role, creator and status. */
export function IncidentActions({ incident, perms }: { incident: Incident; perms: IncidentPermissions }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const id = incident.incidentId
  const ref = incidentRef(id)

  const [dialog, setDialog] = useState<DialogKey | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [duplicates, setDuplicates] = useState<PossibleDuplicate[] | null>(null)
  const [dupBusy, setDupBusy] = useState<null | 'proceed' | 'delete'>(null)

  async function applyUpdate(updated: Incident) {
    queryClient.setQueryData(incidentKeys.detail(id), updated)
    // Lists, comments (send back / confirm duplicate add one) and duplicate flags may have changed.
    await queryClient.invalidateQueries({ queryKey: incidentKeys.all })
  }

  const dialogs: Record<DialogKey, DialogConfig> = {
    approve: {
      title: `Approve ${ref}?`,
      description: 'The incident moves on to payment or resolution, depending on its resolution type.',
      confirmLabel: 'Approve',
      run: () => incidentActions.approve(id),
    },
    reject: {
      title: `Reject ${ref}?`,
      description: 'Rejecting closes the incident for good. The reporter will see your reason.',
      confirmLabel: 'Reject incident',
      tone: 'danger',
      textField: { label: 'Reason', placeholder: 'Why is this incident rejected?', maxLength: 1000 },
      run: (text) => incidentActions.reject(id, text),
    },
    sendBack: {
      title: `Send ${ref} back?`,
      description: 'The reporter can make changes and submit it again. Your message is added as a comment.',
      confirmLabel: 'Send back',
      textField: { label: 'What needs to change', placeholder: 'e.g. Add the customer’s phone number and a photo.', maxLength: 4000 },
      run: (text) => incidentActions.sendBack(id, text),
    },
    complete: {
      title: `Mark ${ref} as completed?`,
      description: 'Use this once the payment or resolution is done. The incident is then closed.',
      confirmLabel: 'Mark as completed',
      run: () => incidentActions.complete(id),
    },
    cancel: {
      title: `Cancel ${ref}?`,
      description: 'Canceling closes the incident. This cannot be undone.',
      confirmLabel: 'Cancel incident',
      tone: 'danger',
      run: () => incidentActions.cancel(id),
    },
    dismiss: {
      title: 'Not a duplicate?',
      description: 'The possible-duplicate warning is cleared and the incident can be approved as normal.',
      confirmLabel: 'Not a duplicate',
      run: () => incidentActions.dismissDuplicates(id),
    },
    confirmDuplicate: {
      title: `Close ${ref} as a duplicate?`,
      description: 'The incident is closed as a duplicate of an existing one. Your explanation is added as a comment.',
      confirmLabel: 'Close as duplicate',
      tone: 'danger',
      textField: { label: 'Explanation', placeholder: 'e.g. Same fall as IF-0012.', maxLength: 4000 },
      run: (text) => incidentActions.confirmDuplicate(id, text),
    },
  }

  async function submit(proceed: boolean) {
    const result = await submitIncident(id, proceed)
    await queryClient.invalidateQueries({ queryKey: incidentKeys.all })
    if (result.submitted) navigate(`/incidents/${id}/submitted`)
    else setDuplicates(result.duplicates)
  }

  async function handleSubmit() {
    setSubmitting(true)
    setSubmitError(null)
    try {
      await submit(false)
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Could not submit.')
    } finally {
      setSubmitting(false)
    }
  }

  async function handleProceed() {
    setDupBusy('proceed')
    try {
      await submit(true)
      setDuplicates(null)
    } catch (err) {
      setDuplicates(null)
      setSubmitError(err instanceof Error ? err.message : 'Could not submit.')
    } finally {
      setDupBusy(null)
    }
  }

  async function handleDelete() {
    setDupBusy('delete')
    try {
      await deleteDraftIncident(id)
      queryClient.removeQueries({ queryKey: incidentKeys.detail(id) })
      await queryClient.invalidateQueries({ queryKey: incidentKeys.list() })
      navigate('/incidents', { replace: true })
    } catch (err) {
      setDuplicates(null)
      setSubmitError(err instanceof Error ? err.message : 'Could not delete the draft.')
      setDupBusy(null)
    }
  }

  const buttons: ReactNode[] = []
  if (perms.submit)
    buttons.push(
      <button key="submit" type="button" onClick={handleSubmit} disabled={submitting} className={cn(actionButton, tones.primary)}>
        {submitting ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <Send className="size-4" aria-hidden />}
        Submit for approval
      </button>,
    )
  if (perms.approve || perms.approveBlockedReason)
    buttons.push(
      <div key="approve">
        <button
          type="button"
          onClick={() => setDialog('approve')}
          disabled={!perms.approve}
          className={cn(actionButton, tones.primary)}
        >
          <Check className="size-4" aria-hidden />
          Approve
        </button>
        {perms.approveBlockedReason && <p className="mt-1 text-xs text-muted">{perms.approveBlockedReason}</p>}
      </div>,
    )
  if (perms.reviewDuplicates) {
    buttons.push(
      <button key="dismiss" type="button" onClick={() => setDialog('dismiss')} className={cn(actionButton, tones.neutral)}>
        <CheckCheck className="size-4" aria-hidden />
        Not a duplicate
      </button>,
      <button key="confirmDup" type="button" onClick={() => setDialog('confirmDuplicate')} className={cn(actionButton, tones.danger)}>
        <CopyX className="size-4" aria-hidden />
        Close as duplicate
      </button>,
    )
  }
  if (perms.sendBack)
    buttons.push(
      <button key="sendBack" type="button" onClick={() => setDialog('sendBack')} className={cn(actionButton, tones.neutral)}>
        <Undo2 className="size-4" aria-hidden />
        Send back for changes
      </button>,
    )
  if (perms.complete)
    buttons.push(
      <button key="complete" type="button" onClick={() => setDialog('complete')} className={cn(actionButton, tones.primary)}>
        <CheckCheck className="size-4" aria-hidden />
        Mark as completed
      </button>,
    )
  if (perms.edit)
    buttons.push(
      <Link key="edit" to={`/incidents/${id}/edit`} className={cn(actionButton, tones.neutral)}>
        <Pencil className="size-4" aria-hidden />
        Edit details
      </Link>,
    )
  if (perms.reject)
    buttons.push(
      <button key="reject" type="button" onClick={() => setDialog('reject')} className={cn(actionButton, tones.danger)}>
        <X className="size-4" aria-hidden />
        Reject
      </button>,
    )
  if (perms.cancel)
    buttons.push(
      <button key="cancel" type="button" onClick={() => setDialog('cancel')} className={cn(actionButton, tones.danger)}>
        <Ban className="size-4" aria-hidden />
        Cancel incident
      </button>,
    )

  if (buttons.length === 0) return null

  const active = dialog ? dialogs[dialog] : null

  return (
    <Card>
      <CardHeader title="Actions" />
      <div className="space-y-2 px-6 pb-6">
        {submitError && (
          <div role="alert" className="mb-2 flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2.5 text-sm text-red-700">
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden />
            <span>{submitError}</span>
          </div>
        )}
        {buttons}
      </div>

      {active && (
        <ActionDialog
          key={dialog}
          title={active.title}
          description={active.description}
          confirmLabel={active.confirmLabel}
          tone={active.tone}
          textField={active.textField}
          onClose={() => setDialog(null)}
          onConfirm={async (text) => {
            const updated = await active.run(text)
            setDialog(null)
            await applyUpdate(updated)
          }}
        />
      )}

      {duplicates && (
        <DuplicatesModal
          duplicates={duplicates}
          incidentId={id}
          busy={dupBusy}
          canDelete={perms.deleteDraft}
          onClose={() => setDuplicates(null)}
          onProceed={handleProceed}
          onDelete={handleDelete}
        />
      )}
    </Card>
  )
}
