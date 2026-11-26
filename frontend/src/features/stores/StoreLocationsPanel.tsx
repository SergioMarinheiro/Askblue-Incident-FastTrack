import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { Loader2, MapPin, Plus, Trash2 } from 'lucide-react'
import { ActionDialog } from '@/components/ui/ActionDialog'
import { Card } from '@/components/ui/Card'
import { ErrorState, LoadingState } from '@/components/ui/States'
import { buttonDangerClass, buttonPrimaryClass, inputClass } from '@/components/ui/form'
import {
  createLocation,
  deleteLocation,
  deleteStore,
  storeKeys,
  useLocations,
  type Location,
  type Store,
} from '@/features/reference/api'

type Props = {
  store: Store
  companyName?: string
  onStoreDeleted: (store: Store) => Promise<void>
}

/** Right-hand panel: one store's details and its locations. */
export function StoreLocationsPanel({ store, companyName, onStoreDeleted }: Props) {
  const queryClient = useQueryClient()
  const panelRef = useRef<HTMLDivElement>(null)

  // On small screens this panel sits below the store list: bring it into view when a store is picked.
  // (The page gives this component a new key per store, so this runs once per selection.)
  useEffect(() => {
    if (window.matchMedia('(max-width: 1023px)').matches) {
      panelRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
  }, [])
  const locations = useLocations(store.storeId)

  const [newName, setNewName] = useState('')
  const [adding, setAdding] = useState(false)
  const [addError, setAddError] = useState<string | null>(null)
  const [toDelete, setToDelete] = useState<Location | null>(null)
  const [deletingStore, setDeletingStore] = useState(false)

  const refresh = () => queryClient.invalidateQueries({ queryKey: storeKeys.all })

  async function handleAdd(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const name = newName.trim()
    if (!name) return
    if (name.length > 200) {
      setAddError('At most 200 characters.')
      return
    }
    setAdding(true)
    setAddError(null)
    try {
      await createLocation(store.storeId, name)
      setNewName('')
      await refresh()
    } catch (err) {
      setAddError(err instanceof Error ? err.message : 'Could not add the location.')
    } finally {
      setAdding(false)
    }
  }

  const sorted = [...(locations.data ?? [])].sort((a, b) => a.name.localeCompare(b.name))

  return (
    // scroll-mt: leaves room for the sticky top bar when scrolled into view.
    <div ref={panelRef} className="scroll-mt-20">
    <Card>
      {/* Store header */}
      <div className="flex flex-wrap items-start justify-between gap-4 border-b border-line px-6 py-5">
        <div className="min-w-0">
          <h2 className="text-lg font-semibold">{store.name}</h2>
          <p className="text-sm text-muted">{store.address || 'No address'}</p>
          {companyName && <p className="mt-0.5 text-xs text-muted">{companyName}</p>}
        </div>
        <button type="button" onClick={() => setDeletingStore(true)} className={buttonDangerClass}>
          <Trash2 className="size-4" aria-hidden />
          Delete store
        </button>
      </div>

      {/* Locations */}
      <div className="px-6 py-5">
        <h3 className="text-sm font-semibold">Locations in this store</h3>

        <form onSubmit={handleAdd} className="mt-4 flex gap-2">
          <label htmlFor="newLocation" className="sr-only">
            New location name
          </label>
          <input
            id="newLocation"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            placeholder="New location name"
            maxLength={200}
            disabled={adding}
            className={inputClass}
          />
          <button type="submit" disabled={adding || !newName.trim()} className={`${buttonPrimaryClass} shrink-0`}>
            {adding ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <Plus className="size-4" aria-hidden />}
            Add
          </button>
        </form>
        {addError && <p className="mt-1 text-xs text-red-600">{addError}</p>}

        <div className="mt-4">
          {locations.isPending ? (
            <LoadingState label="Loading locations…" />
          ) : locations.isError ? (
            <ErrorState message={locations.error.message} onRetry={() => locations.refetch()} />
          ) : sorted.length === 0 ? null : (
            <ul className="divide-y divide-line rounded-lg border border-line">
              {sorted.map((l) => (
                <li key={l.locationId} className="flex items-center gap-3 px-3 py-2.5">
                  <MapPin className="size-4 shrink-0 text-muted" aria-hidden />
                  <span className="min-w-0 flex-1 truncate text-sm">{l.name}</span>
                  <button
                    type="button"
                    onClick={() => setToDelete(l)}
                    aria-label={`Delete location ${l.name}`}
                    title="Delete location"
                    className="rounded-md p-1.5 text-muted hover:bg-red-50 hover:text-red-600"
                  >
                    <Trash2 className="size-4" aria-hidden />
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {toDelete && (
        <ActionDialog
          title={`Delete location "${toDelete.name}"?`}
          description="It can no longer be chosen for new incidents. Existing incidents keep showing its name."
          confirmLabel="Delete location"
          tone="danger"
          onClose={() => setToDelete(null)}
          onConfirm={async () => {
            await deleteLocation(store.storeId, toDelete.locationId)
            setToDelete(null)
            await refresh()
          }}
        />
      )}

      {deletingStore && (
        <ActionDialog
          title={`Delete store "${store.name}"?`}
          description={
            <>
              {sorted.length === 0
                ? 'The store'
                : `The store and its ${sorted.length === 1 ? 'location' : `${sorted.length} locations`}`}{' '}
              can no longer be chosen for new incidents. Existing incidents keep showing their names.
            </>
          }
          confirmLabel="Delete store"
          tone="danger"
          onClose={() => setDeletingStore(false)}
          onConfirm={async () => {
            await deleteStore(store.storeId)
            setDeletingStore(false)
            await onStoreDeleted(store)
          }}
        />
      )}
    </Card>
    </div>
  )
}
