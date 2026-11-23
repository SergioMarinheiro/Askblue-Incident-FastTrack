// The incident form, used both to register a new incident and to edit an existing one.
import { useMemo, useRef, useState, type ChangeEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { useQueryClient } from '@tanstack/react-query'
import { AlertCircle, CheckCircle2, FileText, Loader2, Plus, X } from 'lucide-react'
import { useAuth } from '@/auth/useAuth'
import { Card, CardHeader } from '@/components/ui/Card'
import { controlClass, FormField } from '@/components/ui/FormField'
import { PageHeader } from '@/components/ui/PageHeader'
import { buttonPrimaryClass, buttonSecondaryClass, inputClass } from '@/components/ui/form'
import { useLocations, useResolutionTypes, useStores } from '@/features/reference/api'
import { ApiError } from '@/lib/api'
import { createIncident, deleteDraftIncident, incidentKeys, submitIncident, updateIncident, uploadAttachment } from './api'
import { DuplicatesModal } from './DuplicatesModal'
import {
  emptyIncidentForm,
  incidentToForm,
  mapApiFieldErrors,
  toIncidentInput,
  validateIncidentForm,
  type FormErrors,
  type IncidentFormValues,
} from './incidentForm'
import { incidentRef, PRIORITY_LABELS, TYPE_LABELS } from './labels'
import { incidentPermissions } from './permissions'
import type { Incident, PossibleDuplicate } from './types'

const MAX_FILE_BYTES = 20 * 1024 * 1024 // same as the API

type PendingFile = { key: string; file: File; uploaded: boolean }
type Busy = null | 'draft' | 'submit' | 'proceed' | 'delete'

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

/** Without `incident`: register a new one. With `incident`: edit it. */
export function IncidentEditor({ incident }: { incident?: Incident }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const isSuperAdmin = user?.role === 'SuperAdmin'
  const isEdit = incident !== undefined
  // A new incident is always the user's own draft; an existing one depends on creator and status.
  const canSubmit = !incident || incidentPermissions(user, incident).submit
  const canDeleteDraft = !incident || incidentPermissions(user, incident).deleteDraft

  const [values, setValues] = useState<IncidentFormValues>(() => (incident ? incidentToForm(incident) : emptyIncidentForm()))
  const [errors, setErrors] = useState<FormErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [files, setFiles] = useState<PendingFile[]>([])
  const [fileError, setFileError] = useState<string | null>(null)
  const [busy, setBusy] = useState<Busy>(null)
  // Once the draft exists, later saves update it instead of creating another one.
  const [draftId, setDraftId] = useState<number | null>(incident?.incidentId ?? null)
  const [duplicates, setDuplicates] = useState<PossibleDuplicate[] | null>(null)

  const fileInput = useRef<HTMLInputElement>(null)
  const topRef = useRef<HTMLDivElement>(null)

  const stores = useStores()
  const storeId = values.storeId ? Number(values.storeId) : null
  const locations = useLocations(storeId)
  const resolutionTypes = useResolutionTypes()

  const selectedStore = stores.data?.find((s) => s.storeId === storeId)
  // Resolution types belong to a company; only offer the ones of the chosen store's company.
  const resolutionOptions = useMemo(
    () => (resolutionTypes.data ?? []).filter((r) => !selectedStore || r.companyId === selectedStore.companyId),
    [resolutionTypes.data, selectedStore],
  )

  function set<K extends keyof IncidentFormValues>(key: K, value: string) {
    setValues((v) => {
      const next = { ...v, [key]: value }
      if (key === 'storeId') {
        next.locationId = '' // locations belong to a store
        next.resolutionTypeId = isSuperAdmin ? '' : next.resolutionTypeId
      }
      return next
    })
    if (errors[key]) setErrors((e) => ({ ...e, [key]: undefined }))
  }

  const bind = (key: keyof IncidentFormValues) => ({
    id: key,
    value: values[key],
    onChange: (e: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => set(key, e.target.value),
    'aria-invalid': errors[key] ? true : undefined,
    'aria-describedby': errors[key] ? `${key}-error` : undefined,
    disabled: busy !== null,
  })

  function addFiles(e: ChangeEvent<HTMLInputElement>) {
    const picked = Array.from(e.target.files ?? [])
    e.target.value = '' // allow picking the same file again after removing it
    const tooBig = picked.filter((f) => f.size > MAX_FILE_BYTES)
    const empty = picked.filter((f) => f.size === 0)
    const ok = picked.filter((f) => f.size > 0 && f.size <= MAX_FILE_BYTES)
    setFileError(
      tooBig.length ? `Too large (max 20 MB): ${tooBig.map((f) => f.name).join(', ')}` : empty.length ? `Empty file: ${empty.map((f) => f.name).join(', ')}` : null,
    )
    setFiles((cur) => [...cur, ...ok.map((file) => ({ key: crypto.randomUUID(), file, uploaded: false }))])
  }

  function showErrors(next: FormErrors, message: string) {
    setErrors(next)
    setFormError(message)
    topRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  /** Creates or updates the draft, then uploads files not yet uploaded. Returns the incident id. */
  async function saveDraft(): Promise<number> {
    const input = toIncidentInput(values)
    let id = draftId
    if (id === null) {
      const created = await createIncident(isSuperAdmin && selectedStore ? { ...input, companyId: selectedStore.companyId } : input)
      id = created.incidentId
      setDraftId(id)
    } else {
      await updateIncident(id, input)
    }

    for (const pending of files) {
      if (pending.uploaded) continue
      try {
        await uploadAttachment(id, pending.file)
      } catch (err) {
        const reason = err instanceof Error ? err.message : 'Upload failed.'
        throw new ApiError(0, `The incident was saved, but "${pending.file.name}" could not be uploaded: ${reason}`)
      }
      setFiles((cur) => cur.map((f) => (f.key === pending.key ? { ...f, uploaded: true } : f)))
    }
    return id
  }

  async function finishSubmit(id: number, proceed: boolean) {
    const result = await submitIncident(id, proceed)
    await queryClient.invalidateQueries({ queryKey: incidentKeys.all })
    if (result.submitted) {
      navigate(`/incidents/${id}/submitted`, { replace: true })
    } else {
      setDuplicates(result.duplicates)
    }
  }

  function handleError(err: unknown) {
    if (err instanceof ApiError) {
      const fieldErrors = mapApiFieldErrors(err.fieldErrors)
      showErrors(fieldErrors, err.message)
    } else {
      showErrors({}, 'Something went wrong. Please try again.')
    }
  }

  async function run(mode: 'draft' | 'submit') {
    if (mode === 'submit' && !canSubmit) mode = 'draft'
    const found = validateIncidentForm(values, mode)
    if (Object.values(found).some(Boolean)) {
      showErrors(found, mode === 'submit' ? 'Fill in the highlighted fields before submitting.' : 'Check the highlighted fields.')
      return
    }
    setErrors({})
    setFormError(null)
    setBusy(mode)
    try {
      const id = await saveDraft()
      if (mode === 'draft') {
        await queryClient.invalidateQueries({ queryKey: incidentKeys.all })
        navigate(`/incidents/${id}`, { replace: true })
      } else {
        await finishSubmit(id, false)
      }
    } catch (err) {
      handleError(err)
    } finally {
      setBusy(null)
    }
  }

  async function proceedAnyway() {
    if (draftId === null) return
    setBusy('proceed')
    try {
      await finishSubmit(draftId, true)
      setDuplicates(null)
    } catch (err) {
      setDuplicates(null)
      handleError(err)
    } finally {
      setBusy(null)
    }
  }

  async function deleteDraft() {
    if (draftId === null) return
    setBusy('delete')
    try {
      await deleteDraftIncident(draftId)
      await queryClient.invalidateQueries({ queryKey: incidentKeys.all })
      // The deleted incident's cached detail must not be refetched (it would 404).
      queryClient.removeQueries({ queryKey: incidentKeys.detail(draftId) })
      navigate('/incidents', { replace: true })
    } catch (err) {
      setDuplicates(null)
      handleError(err)
    } finally {
      setBusy(null)
    }
  }

  const locked = busy !== null

  return (
    <div ref={topRef} className="scroll-mt-8">
      <PageHeader
        title={incident ? `Edit ${incidentRef(incident.incidentId)}` : 'Register new incident'}
        subtitle={
          canSubmit
            ? 'Fill in the details to start the process. Fields marked * are needed to submit.'
            : 'Change the details and save. Changes are recorded in the incident history.'
        }
      />

      {formError && (
        <div role="alert" className="mb-6 flex items-start gap-2 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden />
          <span>{formError}</span>
        </div>
      )}
      {draftId !== null && !isEdit && (
        <div className="mb-6 flex items-start gap-2 rounded-xl border border-brand-100 bg-brand-50 px-4 py-3 text-sm text-brand-700">
          <CheckCircle2 className="mt-0.5 size-4 shrink-0" aria-hidden />
          <span>
            Saved as draft <strong>{incidentRef(draftId)}</strong>. Further saves update this draft.
          </span>
        </div>
      )}

      <form
        noValidate
        onSubmit={(e) => {
          e.preventDefault()
          void run('submit')
        }}
        className="space-y-6"
      >
        {/* Incident */}
        <Card>
          <CardHeader title="Incident information" />
          <div className="grid gap-5 px-6 pb-6 sm:grid-cols-2">
            <FormField id="title" label="Title" required={canSubmit} error={errors.title} className="sm:col-span-2">
              <input type="text" placeholder="e.g. Customer slipped near the checkouts" className={controlClass(inputClass, errors.title)} {...bind('title')} />
            </FormField>

            <FormField id="incidentType" label="Incident type" required={canSubmit} error={errors.incidentType}>
              <select className={controlClass(inputClass, errors.incidentType)} {...bind('incidentType')}>
                <option value="">Select a type…</option>
                {Object.entries(TYPE_LABELS).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </FormField>

            <FormField id="priority" label="Priority" required={canSubmit} error={errors.priority}>
              <select className={controlClass(inputClass, errors.priority)} {...bind('priority')}>
                <option value="">Select a priority…</option>
                {Object.entries(PRIORITY_LABELS).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </FormField>

            <FormField id="date" label="Date" required={canSubmit} error={errors.date}>
              <input type="date" className={controlClass(inputClass, errors.date)} {...bind('date')} />
            </FormField>

            <FormField id="time" label="Time" required={canSubmit} error={errors.time}>
              <input type="time" className={controlClass(inputClass, errors.time)} {...bind('time')} />
            </FormField>

            <FormField
              id="storeId"
              label="Store"
              required={canSubmit}
              error={errors.storeId ?? (stores.isError ? stores.error.message : undefined)}
            >
              <select className={controlClass(inputClass, errors.storeId)} {...bind('storeId')} disabled={locked || stores.isPending}>
                <option value="">{stores.isPending ? 'Loading stores…' : 'Select a store…'}</option>
                {stores.data?.map((s) => (
                  <option key={s.storeId} value={s.storeId}>
                    {s.name}
                  </option>
                ))}
                {/* An incident's store may have been removed since; keep showing it. */}
                {incident?.storeId != null && stores.data && !stores.data.some((s) => s.storeId === incident.storeId) && (
                  <option value={incident.storeId}>{incident.storeName ?? 'Removed store'} (no longer available)</option>
                )}
              </select>
            </FormField>

            <FormField
              id="locationId"
              label="Location in store"
              required={canSubmit}
              error={errors.locationId ?? (locations.isError ? locations.error.message : undefined)}
              hint={!storeId ? 'Choose a store first.' : locations.data?.length === 0 ? 'This store has no locations yet.' : undefined}
            >
              <select
                className={controlClass(inputClass, errors.locationId)}
                {...bind('locationId')}
                disabled={locked || !storeId || locations.isPending}
              >
                <option value="">{storeId && locations.isPending ? 'Loading locations…' : 'Select a location…'}</option>
                {locations.data?.map((l) => (
                  <option key={l.locationId} value={l.locationId}>
                    {l.name}
                  </option>
                ))}
              </select>
            </FormField>

            <FormField
              id="resolutionTypeId"
              label="Resolution type"
              required={canSubmit}
              error={errors.resolutionTypeId ?? (resolutionTypes.isError ? resolutionTypes.error.message : undefined)}
              hint={isSuperAdmin && !storeId ? 'Choose a store first.' : undefined}
              className="sm:col-span-2"
            >
              <select
                className={controlClass(inputClass, errors.resolutionTypeId)}
                {...bind('resolutionTypeId')}
                disabled={locked || resolutionTypes.isPending || (isSuperAdmin && !storeId)}
              >
                <option value="">{resolutionTypes.isPending ? 'Loading…' : 'Select a resolution type…'}</option>
                {resolutionOptions.map((r) => (
                  <option key={r.resolutionTypeId} value={r.resolutionTypeId}>
                    {r.name}
                    {r.requiresPayment ? ' (requires payment)' : ''}
                  </option>
                ))}
              </select>
            </FormField>

            <FormField
              id="description"
              label="Description"
              required={canSubmit}
              error={errors.description}
              hint={`${values.description.length}/4000`}
              className="sm:col-span-2"
            >
              <textarea
                rows={4}
                placeholder="What happened, and what was done straight away?"
                className={controlClass(`${inputClass} resize-y`, errors.description)}
                {...bind('description')}
              />
            </FormField>
          </div>
        </Card>

        {/* Customer */}
        <Card>
          <CardHeader title="Customer" />
          <div className="grid gap-5 px-6 pb-6 sm:grid-cols-2">
            <FormField id="customerName" label="Name" required error={errors.customerName}>
              <input type="text" autoComplete="off" className={controlClass(inputClass, errors.customerName)} {...bind('customerName')} />
            </FormField>
            <FormField id="customerEmail" label="Email" required={canSubmit} error={errors.customerEmail}>
              <input type="email" autoComplete="off" className={controlClass(inputClass, errors.customerEmail)} {...bind('customerEmail')} />
            </FormField>
            <FormField id="customerPhone" label="Phone" required={canSubmit} error={errors.customerPhone}>
              <input type="tel" autoComplete="off" className={controlClass(inputClass, errors.customerPhone)} {...bind('customerPhone')} />
            </FormField>
            <FormField id="compensation" label="Estimated compensation (€)" error={errors.compensation} hint="Optional.">
              <input type="text" inputMode="decimal" placeholder="0.00" className={controlClass(inputClass, errors.compensation)} {...bind('compensation')} />
            </FormField>
            <FormField id="customerAddress" label="Address" required={canSubmit} error={errors.customerAddress} className="sm:col-span-2">
              <input type="text" autoComplete="off" className={controlClass(inputClass, errors.customerAddress)} {...bind('customerAddress')} />
            </FormField>
          </div>
        </Card>

        {/* Attachments */}
        <Card>
          <CardHeader title="Attachments" />
          <div className="px-6 pb-6">
            {files.length > 0 && (
              <ul className="mb-4 divide-y divide-line rounded-lg border border-line">
                {files.map((f) => (
                  <li key={f.key} className="flex items-center gap-3 px-3 py-2.5">
                    <FileText className="size-4 shrink-0 text-muted" aria-hidden />
                    <span className="min-w-0 flex-1 truncate text-sm" title={f.file.name}>
                      {f.file.name}
                    </span>
                    <span className="text-xs text-muted">{formatBytes(f.file.size)}</span>
                    {f.uploaded ? (
                      <span className="text-xs font-semibold text-emerald-700">Uploaded</span>
                    ) : (
                      <button
                        type="button"
                        onClick={() => setFiles((cur) => cur.filter((x) => x.key !== f.key))}
                        disabled={locked}
                        aria-label={`Remove ${f.file.name}`}
                        className="rounded p-1 text-muted hover:bg-surface hover:text-ink"
                      >
                        <X className="size-4" aria-hidden />
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            )}
            <div className="flex flex-wrap items-center gap-3">
              <input ref={fileInput} type="file" multiple className="hidden" onChange={addFiles} />
              <button type="button" onClick={() => fileInput.current?.click()} disabled={locked} className={buttonSecondaryClass}>
                <Plus className="size-4" aria-hidden />
                Attach files
              </button>
              <span className="text-xs text-muted">Optional · photos, PDFs or other documents · max 20 MB each</span>
            </div>
            {fileError && <p className="mt-2 text-xs text-red-600">{fileError}</p>}
          </div>
        </Card>

        {/* Actions */}
        <div className="flex flex-wrap items-center justify-end gap-3">
          <Link
            to={incident ? `/incidents/${incident.incidentId}` : '/incidents'}
            className="px-3 py-2.5 text-sm font-semibold text-muted hover:text-ink"
          >
            Cancel
          </Link>
          <button
            type="button"
            onClick={() => void run('draft')}
            disabled={locked}
            className={canSubmit ? buttonSecondaryClass : buttonPrimaryClass}
          >
            {busy === 'draft' && <Loader2 className="size-4 animate-spin" aria-hidden />}
            {!isEdit ? 'Save as draft' : canSubmit ? 'Save without submitting' : 'Save changes'}
          </button>
          {canSubmit && (
            <button type="submit" disabled={locked} className={buttonPrimaryClass}>
              {busy === 'submit' && <Loader2 className="size-4 animate-spin" aria-hidden />}
              {busy === 'submit' ? 'Submitting…' : isEdit ? 'Save and submit' : 'Submit incident'}
            </button>
          )}
        </div>
      </form>

      {duplicates && draftId !== null && (
        <DuplicatesModal
          duplicates={duplicates}
          incidentId={draftId}
          canDelete={canDeleteDraft}
          closeLabel="Back to the form"
          busy={busy === 'proceed' || busy === 'delete' ? busy : null}
          onClose={() => setDuplicates(null)}
          onProceed={proceedAnyway}
          onDelete={deleteDraft}
        />
      )}
    </div>
  )
}
