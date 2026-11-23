// Mirrors backend UserController + UserService rules so only working buttons are shown.
import type { CurrentUser } from '@/auth/AuthContext'
import { hasRole, type Role } from '@/auth/roles'
import type { User } from './api'

export const USER_ADMIN_ROLES = ['Admin', 'SuperAdmin'] as const
export const USER_LIST_ROLES = ['Manager', 'Admin', 'SuperAdmin'] as const

export function canCreateUsers(me: CurrentUser | null) {
  return hasRole(me?.role, USER_ADMIN_ROLES)
}

/** Roles the caller may give a new user. Only a SuperAdmin may create SuperAdmins. */
export function assignableRoles(me: CurrentUser | null): Role[] {
  return me?.role === 'SuperAdmin' ? ['Staff', 'Manager', 'Admin', 'SuperAdmin'] : ['Staff', 'Manager', 'Admin']
}

export function userPermissions(me: CurrentUser | null, target: User) {
  const isSelf = me?.userId === target.userId
  const isAdmin = hasRole(me?.role, USER_ADMIN_ROLES)
  return {
    isSelf,
    edit: isAdmin,
    /** Nobody changes their own role; the SuperAdmin role can't be given or taken away. */
    changeRole: isAdmin && !isSelf && target.role !== 'SuperAdmin',
    resetPassword: isAdmin && (target.role !== 'SuperAdmin' || me?.role === 'SuperAdmin'),
    delete: !isSelf && (isAdmin || (me?.role === 'Manager' && target.role === 'Staff')),
  }
}
