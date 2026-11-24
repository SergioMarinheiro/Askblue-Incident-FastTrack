import { useMemo, useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { useQueryClient } from '@tanstack/react-query'
import { AlertCircle, CheckCircle2, Loader2, Plus, Search, Trash2, X } from 'lucide-react'
import { ActionDialog } from '@/components/ui/ActionDialog'
import { Card, CardHeader } from '@/components/ui/Card'
import { controlClass, FormField } from '@/components/ui/FormField'
import { Modal } from '@/components/ui/Modal'
import { PageHeader } from '@/components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/States'
import { buttonPrimaryClass, inputClass } from '@/components/ui/form'
import { companyKeys, createCompany, deleteCompany, useCompanies, type Company } from '@/features/companies/api'
import { useIncidents } from '@/features/incidents/api'
import { useResolutionTypes, useStores } from '@/features/reference/api'
import { useUsers } from '@/features/users/api'

type Counts = { users: number; stores: number; resolutionTypes: number; incidents: number }

export function CompaniesPage() {
  const queryClient = useQueryClient()
  const companies = useCompanies(true)

  // A SuperAdmin sees every company's data, so the counts can be worked out here.
  const users = useUsers()
  const stores = useStores()
  const resolutionTypes = useResolutionTypes()
  const incidents = useIncidents()
  const countsReady = users.isSuccess && stores.isSuccess && resolutionTypes.isSuccess && incidents.isSuccess

  const counts = useMemo(() => {
    const map = new Map<number, Counts>()
    const get = (id: number) => {
      let c = map.get(id)
      if (!c) map.set(id, (c = { users: 0, stores: 0, resolutionTypes: 0, incidents: 0 }))
      return c
    }
    for (const u of users.data ?? []) if (u.companyId !== null) get(u.companyId).users++
    for (const s of stores.data ?? []) get(s.companyId).stores++
    for (const r of resolutionTypes.data ?? []) get(r.companyId).resolutionTypes++
    for (const i of incidents.data ?? []) get(i.companyId).incidents++
    return map
  }, [users.data, stores.data, resolutionTypes.data, incidents.data])

  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const [toDelete, setToDelete] = useState<Company | null>(null)
  const [created, setCreated] = useState<Company | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const rows = useMemo(() => {
    const term = search.trim().toLowerCase()
    return (companies.data ?? []).filter((c) => !term || c.name.toLowerCase().includes(term)).sort((a, b) => a.name.localeCompare(b.name))
  }, [companies.data, search])

  async function refresh() {
    await queryClient.invalidateQueries({ queryKey: companyKeys.all })
  }

  return (
    <>
      <PageHeader
        title="Companies"
        actions={
          <button type="button" onClick={() => setCreating(true)} className={buttonPrimaryClass}>
            <Plus className="size-4" aria-hidden />
            New company
          </button>
        }
      />

      {created && <NextStepsBanner company={created} onDismiss={() => setCreated(null)} />}

      {notice && (
        <div role="status" className="mb-5 flex items-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
          <CheckCircle2 className="size-4 shrink-0" aria-hidden />
          <span className="flex-1">{notice}</span>
          <button type="button" onClick={() => setNotice(null)} aria-label="Dismiss" className="rounded p-0.5 hover:bg-emerald-100">
            <X className="size-4" aria-hidden />
          </button>
        </div>
      )}

      <div className="relative mb-5 sm:w-80">
        <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted" aria-hidden />
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search companies…"
          aria-label="Search companies"
          className={`${inputClass} pl-9`}
        />
      </div>

      <Card>
        <CardHeader title={companies.data ? `${rows.length} ${rows.length === 1 ? 'company' : 'companies'}` : 'Companies'} />
        {companies.isPending ? (
          <LoadingState label="Loading companies…" />
        ) : companies.isError ? (
          <ErrorState message={companies.error.message} onRetry={() => companies.refetch()} />
        ) : rows.length === 0 ? (
          <EmptyState title={search ? 'No companies match' : 'No companies yet'} />
        ) : (
          <ul className="divide-y divide-line border-t border-line">
            {rows.map((c) => {
              const n = counts.get(c.companyId) ?? { users: 0, stores: 0, resolutionTypes: 0, incidents: 0 }
              const hasData = n.users + n.stores + n.resolutionTypes + n.incidents > 0
              return (
                <li key={c.companyId} className="flex flex-wrap items-center gap-x-4 gap-y-2 px-6 py-4">
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-semibold">{c.name}</p>
                    <p className="text-xs text-muted">
                      {countsReady ? (
                        <>
                          {plural(n.users, 'user')} · {plural(n.stores, 'store')} · {plural(n.resolutionTypes, 'resolution type')} ·{' '}
                          {plural(n.incidents, 'incident')}
                        </>
                      ) : (
                        'Counting…'
                      )}
                    </p>
                  </div>
                  <button
                    type="button"
                    onClick={() => setToDelete(c)}
                    disabled={!countsReady || hasData}
                    title={hasData ? 'Only a company without any data can be deleted.' : 'Delete company'}
                    aria-label={`Delete ${c.name}`}
                    className="rounded-md p-2 text-muted hover:bg-red-50 hover:text-red-600 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:bg-transparent disabled:hover:text-muted"
                  >
                    <Trash2 className="size-4" aria-hidden />
                  </button>
                </li>
              )
            })}
          </ul>
        )}
      </Card>

      {creating && (
        <NewCompanyModal
          onClose={() => setCreating(false)}
          onCreated={async (company) => {
            setCreating(false)
            setNotice(null)
            setCreated(company)
            await refresh()
          }}
        />
      )}

      {toDelete && (
        <ActionDialog
          title={`Delete "${toDelete.name}"?`}
          description="The company is removed for good. This only works while it has no data at all."
          confirmLabel="Delete company"
          tone="danger"
          onClose={() => setToDelete(null)}
          onConfirm={async () => {
            await deleteCompany(toDelete.companyId)
            setNotice(`"${toDelete.name}" was deleted.`)
            setCreated((c) => (c?.companyId === toDelete.companyId ? null : c))
            setToDelete(null)
            await refresh()
          }}
        />
      )}
    </>
  )
}

const plural = (n: number, word: string) => `${n} ${word}${n === 1 ? '' : 's'}`

/** After creating a company: what it needs before its people can report incidents. */
function NextStepsBanner({ company, onDismiss }: { company: Company; onDismiss: () => void }) {
  return (
    <div role="status" className="mb-5 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900">
      <div className="flex items-start gap-2">
        <CheckCircle2 className="mt-0.5 size-4 shrink-0" aria-hidden />
        <div className="flex-1">
          <p className="font-semibold">"{company.name}" created. To get it ready:</p>
          <ol className="mt-1.5 list-decimal space-y-0.5 pl-5">
            <li>
              <Link to="/users" className="font-semibold underline">
                Create its first Admin user
              </Link>{' '}
              (choose this company in the form).
            </li>
            <li>
              Add its{' '}
              <Link to="/stores" className="font-semibold underline">
                stores and locations
              </Link>{' '}
              and{' '}
              <Link to="/resolution-types" className="font-semibold underline">
                resolution types
              </Link>
              , or let its Admin do it.
            </li>
          </ol>
        </div>
        <button type="button" onClick={onDismiss} aria-label="Dismiss" className="rounded p-0.5 hover:bg-emerald-100">
          <X className="size-4" aria-hidden />
        </button>
      </div>
    </div>
  )
}

function NewCompanyModal({ onClose, onCreated }: { onClose: () => void; onCreated: (company: Company) => Promise<void> }) {
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const trimmed = name.trim()
    if (!trimmed) return setError('Required.')
    if (trimmed.length > 200) return setError('At most 200 characters.')
    setBusy(true)
    setFormError(null)
    try {
      await onCreated(await createCompany(trimmed))
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Could not create the company.')
      setBusy(false)
    }
  }

  return (
    <Modal
      title="New company"
      onClose={onClose}
      locked={busy}
      footer={
        <>
          <button type="button" onClick={onClose} disabled={busy} className="px-3 py-2.5 text-sm font-semibold text-muted hover:text-ink">
            Cancel
          </button>
          <button type="submit" form="company-form" disabled={busy} className={buttonPrimaryClass}>
            {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
            Create company
          </button>
        </>
      }
    >
      <form id="company-form" noValidate onSubmit={handleSubmit} className="space-y-4">
        {formError && (
          <div role="alert" className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2.5 text-sm text-red-700">
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden />
            <span>{formError}</span>
          </div>
        )}
        <FormField id="companyName" label="Name" required error={error ?? undefined} hint="The name can't be changed later.">
          <input
            id="companyName"
            autoFocus
            maxLength={200}
            value={name}
            onChange={(e) => {
              setName(e.target.value)
              setError(null)
            }}
            disabled={busy}
            className={controlClass(inputClass, error ?? undefined)}
          />
        </FormField>
      </form>
    </Modal>
  )
}
