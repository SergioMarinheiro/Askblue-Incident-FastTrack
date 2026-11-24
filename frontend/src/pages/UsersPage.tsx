import { useMemo, useState, type ReactNode } from 'react'
import { useSearchParams } from 'react-router'
import { useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, KeyRound, Pencil, Plus, Search, Trash2, X } from 'lucide-react'
import { useAuth } from '@/auth/useAuth'
import { ROLE_LABELS, ROLES, type Role } from '@/auth/roles'
import { ActionDialog } from '@/components/ui/ActionDialog'
import { Badge, type BadgeTone } from '@/components/ui/Badge'
import { Card, CardHeader } from '@/components/ui/Card'
import { PageHeader } from '@/components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '@/components/ui/States'
import { buttonPrimaryClass, inputClass } from '@/components/ui/form'
import { Avatar } from '@/components/layout/Avatar'
import { useCompanies } from '@/features/companies/api'
import { deleteUser, userKeys, useUsers, type User } from '@/features/users/api'
import { canCreateUsers, userPermissions } from '@/features/users/permissions'
import { ResetPasswordModal } from '@/features/users/ResetPasswordModal'
import { UserFormModal } from '@/features/users/UserFormModal'

const ROLE_TONE: Record<Role, BadgeTone> = { SuperAdmin: 'violet', Admin: 'blue', Manager: 'amber', Staff: 'gray' }

type Dialog = { kind: 'create' } | { kind: 'edit'; user: User } | { kind: 'reset'; user: User } | { kind: 'delete'; user: User }

export function UsersPage() {
  const { user: me } = useAuth()
  const queryClient = useQueryClient()
  const isSuperAdmin = me?.role === 'SuperAdmin'
  const { data, isPending, isError, error, refetch } = useUsers()
  const companies = useCompanies(isSuperAdmin)
  const companyName = (id: number | null) =>
    id === null ? 'All companies' : (companies.data?.find((c) => c.companyId === id)?.name ?? `Company ${id}`)

  const [params, setParams] = useSearchParams()
  const search = params.get('q') ?? ''
  const roleFilter = (ROLES as readonly string[]).includes(params.get('role') ?? '') ? (params.get('role') as Role) : null

  const [dialog, setDialog] = useState<Dialog | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase()
    return (data ?? [])
      .filter((u) => (!roleFilter || u.role === roleFilter) && (!term || `${u.fullName} ${u.email}`.toLowerCase().includes(term)))
      .sort((a, b) => a.fullName.localeCompare(b.fullName))
  }, [data, search, roleFilter])

  function updateParam(key: string, value: string) {
    setParams(
      (prev) => {
        const next = new URLSearchParams(prev)
        if (value) next.set(key, value)
        else next.delete(key)
        return next
      },
      { replace: true },
    )
  }

  async function afterChange(message: string) {
    setDialog(null)
    setNotice(message)
    await queryClient.invalidateQueries({ queryKey: userKeys.all })
  }

  return (
    <>
      <PageHeader
        title="Users"
        actions={
          canCreateUsers(me) && (
            <button type="button" onClick={() => setDialog({ kind: 'create' })} className={buttonPrimaryClass}>
              <Plus className="size-4" aria-hidden />
              New user
            </button>
          )
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

      <div className="mb-5 flex flex-col gap-3 sm:flex-row">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted" aria-hidden />
          <input
            type="search"
            value={search}
            onChange={(e) => updateParam('q', e.target.value)}
            placeholder="Search by name or email…"
            aria-label="Search users"
            className={`${inputClass} pl-9`}
          />
        </div>
        <select value={roleFilter ?? ''} onChange={(e) => updateParam('role', e.target.value)} aria-label="Filter by role" className={`${inputClass} sm:w-48`}>
          <option value="">All roles</option>
          {ROLES.map((r) => (
            <option key={r} value={r}>
              {ROLE_LABELS[r]}
            </option>
          ))}
        </select>
      </div>

      <Card>
        <CardHeader title={roleFilter ? ROLE_LABELS[roleFilter] : 'All users'} actions={data && `${filtered.length} ${filtered.length === 1 ? 'user' : 'users'}`} />
        {isPending ? (
          <LoadingState label="Loading users…" />
        ) : isError ? (
          <ErrorState message={error.message} onRetry={() => refetch()} />
        ) : filtered.length === 0 ? (
          <EmptyState title="No users found" description={search || roleFilter ? 'Try a different search or role.' : undefined} />
        ) : (
          <div className="overflow-x-auto px-3 pb-3">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead>
                <tr className="bg-surface text-xs font-semibold tracking-wide text-muted uppercase">
                  <th className="rounded-l-lg px-3 py-3">Name</th>
                  <th className="px-3 py-3">Email</th>
                  <th className="px-3 py-3">Role</th>
                  {isSuperAdmin && <th className="px-3 py-3">Company</th>}
                  <th className="rounded-r-lg px-3 py-3 text-right">
                    <span className="sr-only">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {filtered.map((u) => {
                  const p = userPermissions(me, u)
                  return (
                    <tr key={u.userId} className="hover:bg-surface/60">
                      <td className="px-3 py-3">
                        <div className="flex items-center gap-3">
                          <Avatar name={u.fullName} variant="light" />
                          <span className="font-medium">
                            {u.fullName}
                            {p.isSelf && <span className="ml-2 text-xs font-normal text-muted">(you)</span>}
                          </span>
                        </div>
                      </td>
                      <td className="px-3 py-3 text-ink/90">{u.email}</td>
                      <td className="px-3 py-3">
                        <Badge tone={ROLE_TONE[u.role]}>{ROLE_LABELS[u.role]}</Badge>
                      </td>
                      {isSuperAdmin && <td className="px-3 py-3 text-ink/90">{companyName(u.companyId)}</td>}
                      <td className="px-3 py-3">
                        <div className="flex justify-end gap-1">
                          {p.edit && (
                            <IconButton label={`Edit ${u.fullName}`} onClick={() => setDialog({ kind: 'edit', user: u })}>
                              <Pencil className="size-4" aria-hidden />
                            </IconButton>
                          )}
                          {p.resetPassword && (
                            <IconButton label={`Reset password for ${u.fullName}`} onClick={() => setDialog({ kind: 'reset', user: u })}>
                              <KeyRound className="size-4" aria-hidden />
                            </IconButton>
                          )}
                          {p.delete && (
                            <IconButton label={`Delete ${u.fullName}`} danger onClick={() => setDialog({ kind: 'delete', user: u })}>
                              <Trash2 className="size-4" aria-hidden />
                            </IconButton>
                          )}
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {dialog?.kind === 'create' && (
        <UserFormModal onClose={() => setDialog(null)} onSaved={() => afterChange('User created.')} />
      )}
      {dialog?.kind === 'edit' && (
        <UserFormModal user={dialog.user} onClose={() => setDialog(null)} onSaved={() => afterChange(`${dialog.user.fullName} was updated.`)} />
      )}
      {dialog?.kind === 'reset' && (
        <ResetPasswordModal
          user={dialog.user}
          onClose={() => setDialog(null)}
          onDone={() => void afterChange(`Password for ${dialog.user.fullName} was reset.`)}
        />
      )}
      {dialog?.kind === 'delete' && (
        <ActionDialog
          title={`Delete ${dialog.user.fullName}?`}
          description="They will no longer be able to sign in. Incidents and comments they created are kept."
          confirmLabel="Delete user"
          tone="danger"
          onClose={() => setDialog(null)}
          onConfirm={async () => {
            await deleteUser(dialog.user.userId)
            await afterChange(`${dialog.user.fullName} was deleted.`)
          }}
        />
      )}
    </>
  )
}

function IconButton({ label, danger, onClick, children }: { label: string; danger?: boolean; onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={label}
      title={label}
      className={
        danger
          ? 'rounded-md p-2 text-muted hover:bg-red-50 hover:text-red-600'
          : 'rounded-md p-2 text-muted hover:bg-brand-50 hover:text-brand-600'
      }
    >
      {children}
    </button>
  )
}
