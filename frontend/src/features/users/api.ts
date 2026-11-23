import { useQuery } from '@tanstack/react-query'
import type { Role } from '@/auth/roles'
import { apiFetch } from '@/lib/api'

// The API sends and expects roles as numbers (backend UserRole enum).
export const ROLE_TO_NUMBER: Record<Role, number> = { SuperAdmin: 1, Admin: 2, Manager: 3, Staff: 4 }
export const NUMBER_TO_ROLE: Record<number, Role> = { 1: 'SuperAdmin', 2: 'Admin', 3: 'Manager', 4: 'Staff' }

type UserDto = {
  userId: number
  companyId: number | null
  firstName: string
  lastName: string
  email: string
  role: number
}

export type User = Omit<UserDto, 'role'> & { role: Role; fullName: string }

export type Company = { companyId: number; name: string }

const toUser = (u: UserDto): User => ({
  ...u,
  role: NUMBER_TO_ROLE[u.role] ?? 'Staff',
  fullName: `${u.firstName} ${u.lastName}`.trim(),
})

export const userKeys = {
  all: ['users'] as const,
}

/** Manager/Admin: own company. SuperAdmin: everyone. */
export function useUsers() {
  return useQuery({
    queryKey: userKeys.all,
    queryFn: async ({ signal }) => (await apiFetch<UserDto[]>('/api/users', { signal })).map(toUser),
  })
}

/** SuperAdmin only. */
export function useCompanies(enabled: boolean) {
  return useQuery({
    queryKey: ['companies'],
    queryFn: ({ signal }) => apiFetch<Company[]>('/api/companies', { signal }),
    enabled,
    staleTime: 5 * 60 * 1000,
  })
}

export type UserInput = { firstName: string; lastName: string; email: string; role: Role }

export async function createUser(input: UserInput & { password: string; companyId?: number }) {
  return toUser(await apiFetch<UserDto>('/api/users', { method: 'POST', body: { ...input, role: ROLE_TO_NUMBER[input.role] } }))
}

export async function updateUser(userId: number, input: UserInput) {
  return toUser(await apiFetch<UserDto>(`/api/users/${userId}`, { method: 'PUT', body: { ...input, role: ROLE_TO_NUMBER[input.role] } }))
}

export function resetUserPassword(userId: number, newPassword: string) {
  return apiFetch<void>(`/api/users/${userId}/password`, { method: 'PUT', body: { newPassword } })
}

export function deleteUser(userId: number) {
  return apiFetch<void>(`/api/users/${userId}`, { method: 'DELETE' })
}
