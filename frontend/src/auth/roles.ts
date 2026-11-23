// Mirrors the backend UserRole enum (sent as text in the login response).
export const ROLES = ['SuperAdmin', 'Admin', 'Manager', 'Staff'] as const
export type Role = (typeof ROLES)[number]

export function isRole(value: unknown): value is Role {
  return typeof value === 'string' && (ROLES as readonly string[]).includes(value)
}

export const ROLE_LABELS: Record<Role, string> = {
  SuperAdmin: 'Super admin',
  Admin: 'Admin',
  Manager: 'Manager',
  Staff: 'Staff',
}

/** Same groups as the backend AuthPolicies. */
export const MANAGER_OR_ABOVE = ['Manager', 'Admin', 'SuperAdmin'] as const satisfies readonly Role[]
export const ADMIN_OR_ABOVE = ['Admin', 'SuperAdmin'] as const satisfies readonly Role[]

/** True when the role is one of the allowed roles. Use it to show or hide buttons and menu items. */
export function hasRole(role: Role | undefined, allowed: readonly Role[]) {
  return role !== undefined && allowed.includes(role)
}
