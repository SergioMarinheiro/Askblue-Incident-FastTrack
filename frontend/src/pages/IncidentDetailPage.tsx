import { useRef, useState, type ChangeEvent, type FormEvent, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, ArrowLeft, Download, FileText, Loader2, MessageSquare, Paperclip, Plus, Trash2, XCircle } from 'lucide-react'
import { useAuth } from '@/auth/useAuth'
import { ActionDialog } from '@/components/ui/ActionDialog'
import { Card, CardHeader } from '@/components/ui/Card'
import { buttonPrimaryClass, buttonSecondaryClass, inputClass } from '@/components/ui/form'
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/States'
import { ApiError } from '@/lib/api'
import { formatDateTime, parseUtc, parseWallClock } from '@/lib/dates'
import {
  addComment,
  deleteAttachment,
  downloadAttachment,
  incidentKeys,
  uploadAttachment,
  useDuplicateFlags,
  useIncident,
  useIncidentAttachments,
  useIncidentComments,
} from '@/features/incidents/api'
import { IncidentActions } from '@/features/incidents/IncidentActions'
import { incidentPlace, incidentRef, incidentTitle, PRIORITY_LABELS, TYPE_LABELS } from '@/features/incidents/labels'
import { incidentPermissions } from '@/features/incidents/permissions'
import { ProcessTimeline } from '@/features/incidents/ProcessTimeline'
import { StatusBadge } from '@/features/incidents/StatusBadge'
import { buildTimeline } from '@/features/incidents/timeline'
import { DuplicateReviewStatus, IncidentStatus, type Attachment, type Incident } from '@/features/incidents/types'

const MAX_FILE_BYTES = 20 * 1024 * 1024 // same as the API

const money = new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'EUR' })

export function IncidentDetailPage() {
  const id = Number(useParams().id)
  const valid = Number.isInteger(id) && id > 0
  const { data: incident, isPending, isError, error, refetch } = useIncident(valid ? id : 0)

  if (!valid) return <NotFoundCard message="This incident link is not valid." />
  if (isPending) return <LoadingState label="Loading incident…" />
  if (isError) {
    if (error instanceof ApiError && (error.status === 404 || error.status === 403)) {
      return <NotFoundCard message={error.message} />
    }
    return (
      <Card>
        <ErrorState message={error.message} onRetry={() => refetch()} />
      </Card>
    )
  }

  return <IncidentDetail incident={incident} />
}

function IncidentDetail({ incident }: { incident: Incident }) {
  const { user } = useAuth()
  const perms = incidentPermissions(user, incident)
  const place = incidentPlace(incident)
  const subtitle = [incident.incidentType ? TYPE_LABELS[incident.incidentType] : null, place].filter(Boolean).join(' · ')

  return (
    <>
      {/* Header */}
      <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="flex flex-wrap items-center gap-3">
            <h1 className="text-3xl font-bold">{incidentRef(incident.incidentId)}</h1>
            <StatusBadge status={incident.status} />
          </div>
          <p className="mt-2 text-muted">
            <span className="font-medium text-ink">{incidentTitle(incident)}</span>
            {subtitle && <> · {subtitle}</>}
          </p>
        </div>
        <BackLink />
      </div>

      {/* Banners */}
      {incident.status === IncidentStatus.Rejected && incident.rejectionReason && (
        <Banner tone="red" icon={<XCircle className="size-5" />} title="Rejected">
          {incident.rejectionReason}
        </Banner>
      )}
      {incident.status === IncidentStatus.SentBack && (
        <Banner tone="amber" icon={<AlertTriangle className="size-5" />} title="Sent back for changes">
          {perms.submit
            ? 'A manager asked for changes — see the comments. Edit the details, then submit again.'
            : 'A manager asked the reporter for changes — see the comments.'}
        </Banner>
      )}
      {incident.hasPendingDuplicateFlag && (
        <Banner tone="amber" icon={<AlertTriangle className="size-5" />} title="Possible duplicate">
          This incident looks similar to another one and is waiting for a manager to review it.
          {perms.reviewDuplicates && <DuplicateFlagList incidentId={incident.incidentId} />}
        </Banner>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <DetailsCard incident={incident} />
          <CommentsCard incidentId={incident.incidentId} />
        </div>
        <div className="space-y-6">
          <IncidentActions incident={incident} perms={perms} />
          <Card>
            <CardHeader title="Process status" />
            <div className="px-6 pb-6">
              <ProcessTimeline steps={buildTimeline(incident)} />
            </div>
          </Card>
          <AttachmentsCard incidentId={incident.incidentId} canChange={perms.changeFiles} />
        </div>
      </div>
    </>
  )
}

function DetailsCard({ incident }: { incident: Incident }) {
  const contact = [incident.customerEmail, incident.customerPhone].filter(Boolean).join(' · ')
  return (
    <Card>
      <CardHeader title="Incident details" />
      <div className="px-6 pb-6">
        <dl className="grid gap-x-8 gap-y-5 sm:grid-cols-2">
          <Field label="Type">{incident.incidentType ? TYPE_LABELS[incident.incidentType] : null}</Field>
          <Field label="Store">{incidentPlace(incident)}</Field>
          <Field label="Date and time">
            {incident.incidentDate ? formatDateTime(parseWallClock(incident.incidentDate)) : null}
          </Field>
          <Field label="Priority">{incident.priority ? PRIORITY_LABELS[incident.priority] : null}</Field>
          <Field label="Resolution type">{incident.resolutionTypeName}</Field>
          <Field label="Estimated compensation">
            {incident.estimatedCompensationAmount != null ? money.format(incident.estimatedCompensationAmount) : null}
          </Field>
        </dl>

        <h3 className="mt-7 border-t border-line pt-5 text-sm font-semibold">Customer</h3>
        <dl className="mt-4 grid gap-x-8 gap-y-5 sm:grid-cols-2">
          <Field label="Name">{incident.customerName}</Field>
          <Field label="Contact">{contact || null}</Field>
          <Field label="Address" wide>
            {incident.customerAddress}
          </Field>
        </dl>

        <div className="mt-7 border-t border-line pt-5">
          <Field label="Description" wide>
            {incident.description ? <p className="whitespace-pre-line">{incident.description}</p> : null}
          </Field>
        </div>

        <p className="mt-6 text-xs text-muted">
          Recorded {formatDateTime(parseUtc(incident.createdAt))}
          {incident.updatedAt && <> · Last updated {formatDateTime(parseUtc(incident.updatedAt))}</>}
        </p>
      </div>
    </Card>
  )
}

function DuplicateFlagList({ incidentId }: { incidentId: number }) {
  const { data } = useDuplicateFlags(incidentId, true)
  const pending = data?.filter((f) => f.reviewStatus === DuplicateReviewStatus.Pending) ?? []
  if (pending.length === 0) return null
  return (
    <span className="mt-2 block">
      Compare with:{' '}
      {pending.map((f, i) => (
        <span key={f.incidentDuplicateId}>
          {i > 0 && ', '}
          <a href={`/incidents/${f.potentialDuplicateIncidentId}`} target="_blank" rel="noreferrer" className="font-semibold underline">
            {incidentRef(f.potentialDuplicateIncidentId)}
          </a>
          {f.potentialDuplicateTitle && <> ({f.potentialDuplicateTitle})</>}
        </span>
      ))}
    </span>
  )
}

function CommentsCard({ incidentId }: { incidentId: number }) {
  const { data, isPending, isError, error, refetch } = useIncidentComments(incidentId)
  return (
    <Card>
      <CardHeader title="Comments" actions={data && `${data.length}`} />
      {isPending ? (
        <LoadingState label="Loading comments…" />
      ) : isError ? (
        <ErrorState message={error.message} onRetry={() => refetch()} />
      ) : data.length === 0 ? (
        <div className="flex items-center gap-2 px-6 pb-4 text-sm text-muted">
          <MessageSquare className="size-4" aria-hidden />
          No comments yet.
        </div>
      ) : (
        <ul className="space-y-4 px-6 pb-4">
          {data.map((c) => (
            <li key={c.incidentCommentId} className="rounded-lg bg-surface px-4 py-3">
              <div className="flex flex-wrap items-baseline justify-between gap-2">
                <span className="text-sm font-semibold">{c.userName}</span>
                <span className="text-xs text-muted">{formatDateTime(parseUtc(c.createdAt))}</span>
              </div>
              <p className="mt-1 text-sm whitespace-pre-line">{c.content}</p>
            </li>
          ))}
        </ul>
      )}
      <CommentForm incidentId={incidentId} />
    </Card>
  )
}

function CommentForm({ incidentId }: { incidentId: number }) {
  const queryClient = useQueryClient()
  const [text, setText] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (!text.trim()) return
    setBusy(true)
    setError(null)
    try {
      await addComment(incidentId, text.trim())
      setText('')
      await queryClient.invalidateQueries({ queryKey: incidentKeys.comments(incidentId) })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not add the comment.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="border-t border-line px-6 py-4">
      <label htmlFor="new-comment" className="sr-only">
        Add a comment
      </label>
      <textarea
        id="new-comment"
        rows={3}
        maxLength={4000}
        value={text}
        onChange={(e) => setText(e.target.value)}
        placeholder="Write a comment…"
        disabled={busy}
        className={`${inputClass} resize-y`}
      />
      {error && <p className="mt-1 text-xs text-red-600">{error}</p>}
      <div className="mt-2 flex justify-end">
        <button type="submit" disabled={busy || !text.trim()} className={buttonPrimaryClass}>
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          Add comment
        </button>
      </div>
    </form>
  )
}

function AttachmentsCard({ incidentId, canChange }: { incidentId: number; canChange: boolean }) {
  const queryClient = useQueryClient()
  const { data, isPending, isError, error, refetch } = useIncidentAttachments(incidentId)
  const fileInput = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState<string | null>(null)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [toDelete, setToDelete] = useState<Attachment | null>(null)

  const refresh = () => queryClient.invalidateQueries({ queryKey: incidentKeys.attachments(incidentId) })

  async function handleFiles(e: ChangeEvent<HTMLInputElement>) {
    const files = Array.from(e.target.files ?? [])
    e.target.value = ''
    setUploadError(null)
    for (const file of files) {
      if (file.size === 0 || file.size > MAX_FILE_BYTES) {
        setUploadError(`"${file.name}" was skipped: files must be between 1 byte and 20 MB.`)
        continue
      }
      setUploading(file.name)
      try {
        await uploadAttachment(incidentId, file)
      } catch (err) {
        setUploadError(`"${file.name}" could not be uploaded: ${err instanceof Error ? err.message : 'unknown error'}`)
      }
    }
    setUploading(null)
    await refresh()
  }

  return (
    <Card>
      <CardHeader title="Attachments" actions={data && `${data.length}`} />
      {isPending ? (
        <LoadingState label="Loading attachments…" />
      ) : isError ? (
        <ErrorState message={error.message} onRetry={() => refetch()} />
      ) : data.length === 0 ? (
        <div className="flex items-center gap-2 px-6 pb-4 text-sm text-muted">
          <Paperclip className="size-4" aria-hidden />
          No files attached.
        </div>
      ) : (
        <ul className="divide-y divide-line px-6 pb-2">
          {data.map((a) => (
            <AttachmentRow key={a.incidentAttachmentId} attachment={a} onDelete={canChange ? () => setToDelete(a) : undefined} />
          ))}
        </ul>
      )}

      {canChange && (
        <div className="border-t border-line px-6 py-4">
          <input ref={fileInput} type="file" multiple className="hidden" onChange={handleFiles} />
          <button type="button" onClick={() => fileInput.current?.click()} disabled={uploading !== null} className={`${buttonSecondaryClass} w-full`}>
            {uploading ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <Plus className="size-4" aria-hidden />}
            {uploading ? `Uploading ${uploading}…` : 'Add files'}
          </button>
          {uploadError && <p className="mt-2 text-xs text-red-600">{uploadError}</p>}
        </div>
      )}

      {toDelete && (
        <ActionDialog
          title="Delete this file?"
          description={
            <>
              <strong className="text-ink">{toDelete.fileName}</strong> will be removed from the incident.
            </>
          }
          confirmLabel="Delete file"
          tone="danger"
          onClose={() => setToDelete(null)}
          onConfirm={async () => {
            await deleteAttachment(incidentId, toDelete.incidentAttachmentId)
            setToDelete(null)
            await refresh()
          }}
        />
      )}
    </Card>
  )
}

function AttachmentRow({ attachment, onDelete }: { attachment: Attachment; onDelete?: () => void }) {
  const [busy, setBusy] = useState(false)
  const [failed, setFailed] = useState<string | null>(null)

  async function handleDownload() {
    setBusy(true)
    setFailed(null)
    try {
      await downloadAttachment(attachment)
    } catch (err) {
      setFailed(err instanceof Error ? err.message : 'Download failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <li className="flex items-start gap-3 py-3">
      <FileText className="mt-0.5 size-5 shrink-0 text-muted" aria-hidden />
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium" title={attachment.fileName}>
          {attachment.fileName}
        </p>
        <p className="text-xs text-muted">
          {attachment.uploadedByUserName} · {formatDateTime(parseUtc(attachment.uploadedAt))}
        </p>
        {failed && <p className="mt-1 text-xs text-red-600">{failed}</p>}
      </div>
      <button
        type="button"
        onClick={handleDownload}
        disabled={busy}
        aria-label={`Download ${attachment.fileName}`}
        className="rounded-md p-1.5 text-brand-600 hover:bg-brand-50 disabled:opacity-60"
      >
        {busy ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <Download className="size-4" aria-hidden />}
      </button>
      {onDelete && (
        <button
          type="button"
          onClick={onDelete}
          aria-label={`Delete ${attachment.fileName}`}
          className="rounded-md p-1.5 text-muted hover:bg-red-50 hover:text-red-600"
        >
          <Trash2 className="size-4" aria-hidden />
        </button>
      )}
    </li>
  )
}

function Field({ label, children, wide }: { label: string; children: ReactNode; wide?: boolean }) {
  return (
    <div className={wide ? 'sm:col-span-2' : undefined}>
      <dt className="text-xs font-semibold tracking-wide text-muted uppercase">{label}</dt>
      <dd className="mt-1 text-sm">{children ?? <span className="text-muted">—</span>}</dd>
    </div>
  )
}

function Banner({ tone, icon, title, children }: { tone: 'red' | 'amber'; icon: ReactNode; title: string; children: ReactNode }) {
  const styles = tone === 'red' ? 'border-red-200 bg-red-50 text-red-800' : 'border-amber-200 bg-amber-50 text-amber-900'
  return (
    <div className={`mb-6 flex gap-3 rounded-xl border px-4 py-3 ${styles}`} role="note">
      <span className="mt-0.5 shrink-0" aria-hidden>
        {icon}
      </span>
      <div>
        <p className="font-semibold">{title}</p>
        <p className="mt-0.5 text-sm whitespace-pre-line">{children}</p>
      </div>
    </div>
  )
}

function BackLink() {
  return (
    <Link to="/incidents" className="inline-flex items-center gap-1 text-sm font-semibold text-brand-600 hover:underline">
      <ArrowLeft className="size-4" aria-hidden />
      Back to list
    </Link>
  )
}

function NotFoundCard({ message }: { message: string }) {
  return (
    <>
      <div className="mb-6 flex justify-end">
        <BackLink />
      </div>
      <Card>
        <EmptyState title="Incident not available" description={message} />
      </Card>
    </>
  )
}
