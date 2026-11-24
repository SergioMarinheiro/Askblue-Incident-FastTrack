import { useState, type FormEvent } from 'react'
import { AlertCircle, Loader2 } from 'lucide-react'
import { useAuth } from '@/auth/useAuth'
import { controlClass, FormField } from '@/components/ui/FormField'
import { Modal } from '@/components/ui/Modal'
import { buttonPrimaryClass, inputClass } from '@/components/ui/form'
import { createResolutionType, updateResolutionType, type ResolutionType } from '@/features/reference/api'
import { useCompanies } from '@/features/companies/api'
import { cn } from '@/lib/cn'

type Errors = { name?: string; companyId?: string }

/** Create a resolution type (no `type`) or edit one. */
export function ResolutionTypeModal({
  type,
  onClose,
  onSaved,
}: {
  type?: ResolutionType
  onClose: () => void
  onSaved: (saved: ResolutionType) => Promise<void>
}) {
  const { user } = useAuth()
  const isEdit = type !== undefined
  // A SuperAdmin has no company; when creating they pick one. The company never changes on edit.
  const needsCompany = !isEdit && user?.role === 'SuperAdmin'
  const companies = useCompanies(needsCompany)

  const [name, setName] = useState(type?.name ?? '')
  const [requiresPayment, setRequiresPayment] = useState(type?.requiresPayment ?? false)
  const [companyId, setCompanyId] = useState('')
  const [errors, setErrors] = useState<Errors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const paymentChanged = isEdit && requiresPayment !== type.requiresPayment

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const found: Errors = {}
    if (!name.trim()) found.name = 'Required.'
    else if (name.trim().length > 100) found.name = 'At most 100 characters.'
    if (needsCompany && !companyId) found.companyId = 'Choose a company.'
    setErrors(found)
    if (Object.values(found).some(Boolean)) return

    setBusy(true)
    setFormError(null)
    try {
      const input = { name: name.trim(), requiresPayment }
      const saved = isEdit
        ? await updateResolutionType(type.resolutionTypeId, input)
        : await createResolutionType({ ...input, ...(needsCompany ? { companyId: Number(companyId) } : {}) })
      await onSaved(saved)
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Could not save the resolution type.')
      setBusy(false)
    }
  }

  return (
    <Modal
      title={isEdit ? `Edit "${type.name}"` : 'New resolution type'}
      onClose={onClose}
      locked={busy}
      footer={
        <>
          <button type="button" onClick={onClose} disabled={busy} className="px-3 py-2.5 text-sm font-semibold text-muted hover:text-ink">
            Cancel
          </button>
          <button type="submit" form="resolution-form" disabled={busy} className={buttonPrimaryClass}>
            {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
            {isEdit ? 'Save changes' : 'Create'}
          </button>
        </>
      }
    >
      <form id="resolution-form" noValidate onSubmit={handleSubmit} className="space-y-4">
        {formError && (
          <div role="alert" className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2.5 text-sm text-red-700">
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden />
            <span>{formError}</span>
          </div>
        )}

        {needsCompany && (
          <FormField id="rtCompany" label="Company" required error={errors.companyId ?? (companies.isError ? companies.error.message : undefined)}>
            <select
              id="rtCompany"
              value={companyId}
              onChange={(e) => setCompanyId(e.target.value)}
              disabled={busy || companies.isPending}
              className={controlClass(inputClass, errors.companyId)}
            >
              <option value="">{companies.isPending ? 'Loading…' : 'Select a company…'}</option>
              {companies.data?.map((c) => (
                <option key={c.companyId} value={c.companyId}>
                  {c.name}
                </option>
              ))}
            </select>
          </FormField>
        )}

        <FormField id="rtName" label="Name" required error={errors.name}>
          <input
            id="rtName"
            autoFocus
            maxLength={100}
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Compensation, HR, Legal, Insurance"
            disabled={busy}
            className={controlClass(inputClass, errors.name)}
          />
        </FormField>

        <fieldset>
          <legend className="text-sm font-semibold">After approval, the incident goes to</legend>
          <div className="mt-2 grid gap-2 sm:grid-cols-2">
            <PaymentOption
              checked={requiresPayment}
              onSelect={() => setRequiresPayment(true)}
              disabled={busy}
              title="Awaiting payment"
              description="Money is paid out, e.g. compensation to the customer."
            />
            <PaymentOption
              checked={!requiresPayment}
              onSelect={() => setRequiresPayment(false)}
              disabled={busy}
              title="Awaiting resolution"
              description="Handled another way, e.g. HR, Legal or Insurance."
            />
          </div>
        </fieldset>

        {paymentChanged && (
          <p className="rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-900">
            Only incidents approved from now on follow the new setting. Incidents already approved keep their current status.
          </p>
        )}
      </form>
    </Modal>
  )
}

function PaymentOption({
  checked,
  onSelect,
  disabled,
  title,
  description,
}: {
  checked: boolean
  onSelect: () => void
  disabled: boolean
  title: string
  description: string
}) {
  return (
    <label
      className={cn(
        'flex cursor-pointer gap-3 rounded-lg border px-3 py-3 transition-colors',
        checked ? 'border-brand-600 bg-brand-50' : 'border-line hover:bg-surface',
        disabled && 'cursor-not-allowed opacity-70',
      )}
    >
      <input type="radio" name="requiresPayment" checked={checked} onChange={onSelect} disabled={disabled} className="mt-0.5 accent-brand-600" />
      <span>
        <span className="block text-sm font-semibold">{title}</span>
        <span className="block text-xs text-muted">{description}</span>
      </span>
    </label>
  )
}
