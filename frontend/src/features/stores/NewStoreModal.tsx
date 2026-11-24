import { useState, type FormEvent } from 'react'
import { AlertCircle, Loader2 } from 'lucide-react'
import { useAuth } from '@/auth/useAuth'
import { controlClass, FormField } from '@/components/ui/FormField'
import { Modal } from '@/components/ui/Modal'
import { buttonPrimaryClass, inputClass } from '@/components/ui/form'
import { createStore, type Store } from '@/features/reference/api'
import { useCompanies } from '@/features/companies/api'

type Errors = { name?: string; address?: string; companyId?: string }

export function NewStoreModal({ onClose, onCreated }: { onClose: () => void; onCreated: (store: Store) => Promise<void> }) {
  const { user } = useAuth()
  // A SuperAdmin has no company, so they must say which company the store belongs to.
  const needsCompany = user?.role === 'SuperAdmin'
  const companies = useCompanies(needsCompany)

  const [name, setName] = useState('')
  const [address, setAddress] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [errors, setErrors] = useState<Errors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const found: Errors = {}
    if (!name.trim()) found.name = 'Required.'
    else if (name.trim().length > 200) found.name = 'At most 200 characters.'
    if (address.trim().length > 500) found.address = 'At most 500 characters.'
    if (needsCompany && !companyId) found.companyId = 'Choose a company.'
    setErrors(found)
    if (Object.values(found).some(Boolean)) return

    setBusy(true)
    setFormError(null)
    try {
      const store = await createStore({
        name: name.trim(),
        address: address.trim() || null,
        ...(needsCompany ? { companyId: Number(companyId) } : {}),
      })
      await onCreated(store)
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Could not create the store.')
      setBusy(false)
    }
  }

  return (
    <Modal
      title="New store"
      onClose={onClose}
      locked={busy}
      footer={
        <>
          <button type="button" onClick={onClose} disabled={busy} className="px-3 py-2.5 text-sm font-semibold text-muted hover:text-ink">
            Cancel
          </button>
          <button type="submit" form="store-form" disabled={busy} className={buttonPrimaryClass}>
            {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
            Create store
          </button>
        </>
      }
    >
      <form id="store-form" noValidate onSubmit={handleSubmit} className="space-y-4">
        {formError && (
          <div role="alert" className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2.5 text-sm text-red-700">
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden />
            <span>{formError}</span>
          </div>
        )}
        {needsCompany && (
          <FormField id="companyId" label="Company" required error={errors.companyId ?? (companies.isError ? companies.error.message : undefined)}>
            <select
              id="companyId"
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
        <FormField id="storeName" label="Name" required error={errors.name}>
          <input
            id="storeName"
            autoFocus
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Lisbon — Centre"
            disabled={busy}
            className={controlClass(inputClass, errors.name)}
          />
        </FormField>
        <FormField id="storeAddress" label="Address" error={errors.address} hint="Optional.">
          <input id="storeAddress" value={address} onChange={(e) => setAddress(e.target.value)} disabled={busy} className={controlClass(inputClass, errors.address)} />
        </FormField>
      </form>
    </Modal>
  )
}
