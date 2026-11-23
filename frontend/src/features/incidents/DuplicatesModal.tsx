import { Loader2 } from 'lucide-react'
import { Modal } from '@/components/ui/Modal'
import { buttonDangerClass, buttonPrimaryClass } from '@/components/ui/form'
import { formatDate, parseUtc } from '@/lib/dates'
import { incidentRef } from './labels'
import { StatusBadge } from './StatusBadge'
import type { PossibleDuplicate } from './types'

type Props = {
  duplicates: PossibleDuplicate[]
  incidentId: number
  busy: null | 'proceed' | 'delete'
  /** Deleting is only allowed for a draft (status New) created by the current user. */
  canDelete: boolean
  closeLabel?: string
  onClose: () => void
  onProceed: () => void
  onDelete: () => void
}

/** Shown when submitting finds open incidents that look the same. */
export function DuplicatesModal({ duplicates, incidentId, busy, canDelete, closeLabel = 'Back', onClose, onProceed, onDelete }: Props) {
  const locked = busy !== null
  return (
    <Modal
      title="This may already be reported"
      onClose={onClose}
      locked={locked}
      footer={
        <>
          <button type="button" onClick={onClose} disabled={locked} className="px-3 py-2.5 text-sm font-semibold text-muted hover:text-ink">
            {closeLabel}
          </button>
          {canDelete && (
            <button type="button" onClick={onDelete} disabled={locked} className={buttonDangerClass}>
              {busy === 'delete' && <Loader2 className="size-4 animate-spin" aria-hidden />}
              Delete my draft
            </button>
          )}
          <button type="button" onClick={onProceed} disabled={locked} className={buttonPrimaryClass}>
            {busy === 'proceed' && <Loader2 className="size-4 animate-spin" aria-hidden />}
            Submit anyway
          </button>
        </>
      }
    >
      <p className="text-sm text-muted">
        We found {duplicates.length === 1 ? 'an open incident' : `${duplicates.length} open incidents`} that look similar to{' '}
        <strong className="text-ink">{incidentRef(incidentId)}</strong>. Check them before submitting.
      </p>
      <ul className="mt-4 max-h-72 divide-y divide-line overflow-y-auto rounded-lg border border-line">
        {duplicates.map((d) => (
          <li key={d.incidentId} className="flex items-center gap-3 px-3 py-2.5">
            <div className="min-w-0 flex-1">
              <a href={`/incidents/${d.incidentId}`} target="_blank" rel="noreferrer" className="text-sm font-semibold text-brand-600 hover:underline">
                {incidentRef(d.incidentId)}
              </a>
              <p className="truncate text-sm">{d.title || 'Untitled incident'}</p>
              <p className="text-xs text-muted">Recorded {formatDate(parseUtc(d.createdAt))}</p>
            </div>
            <StatusBadge status={d.status} />
          </li>
        ))}
      </ul>
      <p className="mt-4 text-xs text-muted">
        <strong>Submit anyway</strong> sends it for approval; a manager will review the possible duplicate.
        {canDelete && (
          <>
            {' '}
            <strong>Delete my draft</strong> removes it if it is the same incident.
          </>
        )}
      </p>
    </Modal>
  )
}
