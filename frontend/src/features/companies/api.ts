// Companies (the tenants). Every endpoint is SuperAdmin only.
import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api'

export type Company = { companyId: number; name: string }

export const companyKeys = { all: ['companies'] as const }

/** Pass `enabled: false` for anyone but a SuperAdmin (the API answers 403). */
export function useCompanies(enabled: boolean) {
  return useQuery({
    queryKey: companyKeys.all,
    queryFn: ({ signal }) => apiFetch<Company[]>('/api/companies', { signal }),
    enabled,
    staleTime: 5 * 60 * 1000,
  })
}

export function createCompany(name: string) {
  return apiFetch<Company>('/api/companies', { method: 'POST', body: { name } })
}

/** Hard delete; the API refuses (409) unless the company has no users, stores, incidents or resolution types. */
export function deleteCompany(companyId: number) {
  return apiFetch<void>(`/api/companies/${companyId}`, { method: 'DELETE' })
}
