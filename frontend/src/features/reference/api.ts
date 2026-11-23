// Lookup lists used by forms: stores, their locations, and resolution types.
import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api'

export type Store = { storeId: number; companyId: number; name: string; address: string | null }
export type Location = { locationId: number; storeId: number; name: string }
export type ResolutionType = { resolutionTypeId: number; companyId: number; name: string; requiresPayment: boolean }

// Lookups change rarely; keep them fresh for 5 minutes.
const STALE = 5 * 60 * 1000

export function useStores() {
  return useQuery({
    queryKey: ['stores'],
    queryFn: ({ signal }) => apiFetch<Store[]>('/api/stores', { signal }),
    staleTime: STALE,
  })
}

export function useLocations(storeId: number | null) {
  return useQuery({
    queryKey: ['stores', storeId, 'locations'],
    queryFn: ({ signal }) => apiFetch<Location[]>(`/api/stores/${storeId}/locations`, { signal }),
    enabled: storeId !== null,
    staleTime: STALE,
  })
}

/** Prefix of every store/location query; invalidate it after any change. */
export const storeKeys = { all: ['stores'] as const }

// ---- Writes (Admin, SuperAdmin) ----

/** `companyId` only from a SuperAdmin. */
export function createStore(input: { name: string; address: string | null; companyId?: number }) {
  return apiFetch<Store>('/api/stores', { method: 'POST', body: input })
}

/** Soft delete: old incidents keep showing the store's name. Its locations go with it. */
export function deleteStore(storeId: number) {
  return apiFetch<void>(`/api/stores/${storeId}`, { method: 'DELETE' })
}

export function createLocation(storeId: number, name: string) {
  return apiFetch<Location>(`/api/stores/${storeId}/locations`, { method: 'POST', body: { name } })
}

export function deleteLocation(storeId: number, locationId: number) {
  return apiFetch<void>(`/api/stores/${storeId}/locations/${locationId}`, { method: 'DELETE' })
}

export function useResolutionTypes() {
  return useQuery({
    queryKey: ['resolution-types'],
    queryFn: ({ signal }) => apiFetch<ResolutionType[]>('/api/resolution-types', { signal }),
    staleTime: STALE,
  })
}
