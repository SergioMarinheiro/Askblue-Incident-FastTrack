import { useMemo, useState } from 'react'
import { useSearchParams } from 'react-router'
import { useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, ChevronRight, Plus, Search, Store as StoreIcon, X } from 'lucide-react'
import { useAuth } from '@/auth/useAuth'
import { Card } from '@/components/ui/Card'
import { PageHeader } from '@/components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/States'
import { buttonPrimaryClass, inputClass } from '@/components/ui/form'
import { storeKeys, useStores, type Store } from '@/features/reference/api'
import { NewStoreModal } from '@/features/stores/NewStoreModal'
import { StoreLocationsPanel } from '@/features/stores/StoreLocationsPanel'
import { useCompanies } from '@/features/companies/api'
import { cn } from '@/lib/cn'

export function StoresPage() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const isSuperAdmin = user?.role === 'SuperAdmin'

  const stores = useStores()
  const companies = useCompanies(isSuperAdmin)
  const companyName = (id: number) => companies.data?.find((c) => c.companyId === id)?.name ?? `Company ${id}`

  // Search, company filter and selected store live in the URL, so a refresh keeps your place.
  const [params, setParams] = useSearchParams()
  const search = params.get('q') ?? ''
  const companyFilter = params.get('company') ?? ''
  const selectedId = Number(params.get('store')) || null

  const [creating, setCreating] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)

  function updateParams(changes: Record<string, string | null>) {
    setParams(
      (prev) => {
        const next = new URLSearchParams(prev)
        for (const [k, v] of Object.entries(changes)) {
          if (v) next.set(k, v)
          else next.delete(k)
        }
        return next
      },
      { replace: true },
    )
  }

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase()
    return (stores.data ?? [])
      .filter((s) => !companyFilter || String(s.companyId) === companyFilter)
      .filter((s) => !term || `${s.name} ${s.address ?? ''}`.toLowerCase().includes(term))
      .sort((a, b) => a.name.localeCompare(b.name))
  }, [stores.data, search, companyFilter])

  const selected = stores.data?.find((s) => s.storeId === selectedId) ?? null

  async function handleCreated(store: Store) {
    setCreating(false)
    await queryClient.invalidateQueries({ queryKey: storeKeys.all })
    updateParams({ store: String(store.storeId), q: null })
    setNotice(`Store "${store.name}" created. Add its locations on the right.`)
  }

  async function handleDeleted(store: Store) {
    updateParams({ store: null })
    await queryClient.invalidateQueries({ queryKey: storeKeys.all })
    setNotice(`Store "${store.name}" was deleted.`)
  }

  return (
    <>
      <PageHeader
        title="Stores"
        actions={
          <button type="button" onClick={() => setCreating(true)} className={buttonPrimaryClass}>
            <Plus className="size-4" aria-hidden />
            New store
          </button>
        }
      />

      {notice && (
        <div role="status" className="mb-5 flex items-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
          <CheckCircle2 className="size-4 shrink-0" aria-hidden />
          <span className="flex-1">{notice}</span>
          <button type="button" onClick={() => setNotice(null)} aria-label="Dismiss" className="rounded p-0.5 hover:bg-emerald-100">
            <X className="size-4" aria-hidden />
          </button>
        </div>
      )}

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
        {/* Left: store list */}
        <Card>
          <div className="space-y-3 border-b border-line p-4">
            <div className="relative">
              <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted" aria-hidden />
              <input
                type="search"
                value={search}
                onChange={(e) => updateParams({ q: e.target.value })}
                placeholder="Search stores…"
                aria-label="Search stores"
                className={`${inputClass} pl-9`}
              />
            </div>
            {isSuperAdmin && (
              <select
                value={companyFilter}
                onChange={(e) => updateParams({ company: e.target.value })}
                aria-label="Filter by company"
                className={inputClass}
              >
                <option value="">All companies</option>
                {companies.data?.map((c) => (
                  <option key={c.companyId} value={c.companyId}>
                    {c.name}
                  </option>
                ))}
              </select>
            )}
          </div>

          {stores.isPending ? (
            <LoadingState label="Loading stores…" />
          ) : stores.isError ? (
            <ErrorState message={stores.error.message} onRetry={() => stores.refetch()} />
          ) : filtered.length === 0 ? (
            <EmptyState
              title={stores.data.length === 0 ? 'No stores yet' : 'No stores match'}
              description={stores.data.length === 0 ? 'Create the first store to start reporting incidents.' : 'Try a different search.'}
            />
          ) : (
            <ul className="max-h-[60vh] divide-y divide-line overflow-y-auto p-2">
              {filtered.map((s) => {
                const active = s.storeId === selectedId
                return (
                  <li key={s.storeId}>
                    <button
                      type="button"
                      onClick={() => updateParams({ store: String(s.storeId) })}
                      aria-current={active ? 'true' : undefined}
                      className={cn(
                        'flex w-full items-center gap-3 rounded-lg px-3 py-3 text-left transition-colors',
                        active ? 'bg-brand-50' : 'hover:bg-surface',
                      )}
                    >
                      <StoreIcon className={cn('size-4 shrink-0', active ? 'text-brand-600' : 'text-muted')} aria-hidden />
                      <span className="min-w-0 flex-1">
                        <span className={cn('block truncate text-sm font-semibold', active && 'text-brand-700')}>{s.name}</span>
                        <span className="block truncate text-xs text-muted">
                          {s.address || 'No address'}
                          {isSuperAdmin && <> · {companyName(s.companyId)}</>}
                        </span>
                      </span>
                      <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
                    </button>
                  </li>
                )
              })}
            </ul>
          )}
        </Card>

        {/* Right: selected store */}
        {selected ? (
          <StoreLocationsPanel
            key={selected.storeId}
            store={selected}
            companyName={isSuperAdmin ? companyName(selected.companyId) : undefined}
            onStoreDeleted={handleDeleted}
          />
        ) : (
          <Card>
            <EmptyState title="Select a store" description="Choose a store on the left to see and manage its locations." />
          </Card>
        )}
      </div>

      {creating && <NewStoreModal onClose={() => setCreating(false)} onCreated={handleCreated} />}
    </>
  )
}
