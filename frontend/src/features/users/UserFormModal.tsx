import { useState, type FormEvent } from 'react'
import { AlertCircle, Loader2 } from 'lucide-react'
import { useAuth } from '@/auth/useAuth'
import { ROLE_LABELS, type Role } from '@/auth/roles'
import { controlClass, FormField } from '@/components/ui/FormField'
import { Modal } from '@/components/ui/Modal'
import { buttonPrimaryClass, inputClass } from '@/components/ui/form'
import { ApiError } from '@/lib/api'
import { useCompanies } from '@/features/companies/api'
import { createUser, updateUser, type User } from './api'
import { PasswordInput } from './PasswordInput'
import { assignableRoles, userPermissions } from './permissions'

type Values = { firstName: string; lastName: string; email: string; role: Role | ''; password: string; companyId: string }
type Errors = Partial<Record<keyof Values, string>>

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

/** Create a user (no `user`) or edit one. */
export function UserFormModal({ user, onClose, onSaved }: { user?: User; onClose: () => void; onSaved: () => Promise<void> }) {
  const { user: me } = useAuth()
  const isEdit = user !== undefined
  const isSuperAdmin = me?.role === 'SuperAdmin'
  const roleLocked = isEdit && !userPermissions(me, user).changeRole
  // The SuperAdmin role can only be chosen when creating; it can't be given to an existing user.
  const roles = isEdit ? assignableRoles(me).filter((r) => r !== 'SuperAdmin') : assignableRoles(me)

  const [values, setValues] = useState<Values>({
    firstName: user?.firstName ?? '',
    lastName: user?.lastName ?? '',
    email: user?.email ?? '',
    role: user?.role ?? '',
    password: '',
    companyId: '',
  })
  const [errors, setErrors] = useState<Errors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  // A SuperAdmin creating a non-SuperAdmin must say which company the user belongs to.
  const needsCompany = !isEdit && isSuperAdmin && values.role !== 'SuperAdmin'
  const companies = useCompanies(needsCompany)

  function set<K extends keyof Values>(key: K, value: Values[K]) {
    setValues((v) => ({ ...v, [key]: value }))
    if (errors[key]) setErrors((e) => ({ ...e, [key]: undefined }))
  }

  function validate(): Errors {
    const e: Errors = {}
    if (!values.firstName.trim()) e.firstName = 'Required.'
    else if (values.firstName.trim().length > 100) e.firstName = 'At most 100 characters.'
    if (!values.lastName.trim()) e.lastName = 'Required.'
    else if (values.lastName.trim().length > 100) e.lastName = 'At most 100 characters.'
    if (!values.email.trim()) e.email = 'Required.'
    else if (!EMAIL.test(values.email.trim())) e.email = 'Enter a valid email address.'
    if (!values.role) e.role = 'Choose a role.'
    if (!isEdit && !values.password) e.password = 'Required.'
    if (needsCompany && !values.companyId) e.companyId = 'Choose a company.'
    return e
  }

  async function handleSubmit(ev: FormEvent<HTMLFormElement>) {
    ev.preventDefault()
    const found = validate()
    if (Object.values(found).some(Boolean)) {
      setErrors(found)
      return
    }
    setBusy(true)
    setFormError(null)
    const input = {
      firstName: values.firstName.trim(),
      lastName: values.lastName.trim(),
      email: values.email.trim(),
      role: values.role as Role,
    }
    try {
      if (isEdit) {
        await updateUser(user.userId, input)
      } else {
        await createUser({ ...input, password: values.password, ...(needsCompany ? { companyId: Number(values.companyId) } : {}) })
      }
      await onSaved()
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) setErrors({ email: err.message })
      setFormError(err instanceof Error ? err.message : 'Something went wrong. Please try again.')
      setBusy(false)
    }
  }

  return (
    <Modal
      title={isEdit ? `Edit ${user.fullName}` : 'New user'}
      onClose={onClose}
      locked={busy}
      footer={
        <>
          <button type="button" onClick={onClose} disabled={busy} className="px-3 py-2.5 text-sm font-semibold text-muted hover:text-ink">
            Cancel
          </button>
          <button type="submit" form="user-form" disabled={busy} className={buttonPrimaryClass}>
            {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
            {isEdit ? 'Save changes' : 'Create user'}
          </button>
        </>
      }
    >
      <form id="user-form" noValidate onSubmit={handleSubmit} className="grid gap-4 sm:grid-cols-2">
        {formError && (
          <div role="alert" className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2.5 text-sm text-red-700 sm:col-span-2">
            <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden />
            <span>{formError}</span>
          </div>
        )}

        <FormField id="firstName" label="First name" required error={errors.firstName}>
          <input id="firstName" autoFocus value={values.firstName} onChange={(e) => set('firstName', e.target.value)} disabled={busy} className={controlClass(inputClass, errors.firstName)} />
        </FormField>
        <FormField id="lastName" label="Last name" required error={errors.lastName}>
          <input id="lastName" value={values.lastName} onChange={(e) => set('lastName', e.target.value)} disabled={busy} className={controlClass(inputClass, errors.lastName)} />
        </FormField>
        <FormField id="email" label="Email" required error={errors.email} hint="Used to sign in." className="sm:col-span-2">
          <input id="email" type="email" autoComplete="off" value={values.email} onChange={(e) => set('email', e.target.value)} disabled={busy} className={controlClass(inputClass, errors.email)} />
        </FormField>

        <FormField
          id="role"
          label="Role"
          required
          error={errors.role}
          hint={roleLocked ? (user?.userId === me?.userId ? "You can't change your own role." : "The SuperAdmin role can't be changed.") : isEdit ? 'A new role applies from the user’s next sign-in.' : undefined}
          className={needsCompany ? undefined : 'sm:col-span-2'}
        >
          <select id="role" value={values.role} onChange={(e) => set('role', e.target.value as Role)} disabled={busy || roleLocked} className={controlClass(inputClass, errors.role)}>
            <option value="">Select a role…</option>
            {(roleLocked && user ? [user.role] : roles).map((r) => (
              <option key={r} value={r}>
                {ROLE_LABELS[r]}
              </option>
            ))}
          </select>
        </FormField>

        {needsCompany && (
          <FormField id="companyId" label="Company" required error={errors.companyId ?? (companies.isError ? companies.error.message : undefined)}>
            <select id="companyId" value={values.companyId} onChange={(e) => set('companyId', e.target.value)} disabled={busy || companies.isPending} className={controlClass(inputClass, errors.companyId)}>
              <option value="">{companies.isPending ? 'Loading…' : 'Select a company…'}</option>
              {companies.data?.map((c) => (
                <option key={c.companyId} value={c.companyId}>
                  {c.name}
                </option>
              ))}
            </select>
          </FormField>
        )}

        {!isEdit && (
          <FormField id="password" label="Initial password" required error={errors.password} hint="Share it with the user securely." className="sm:col-span-2">
            <PasswordInput id="password" value={values.password} onChange={(v) => set('password', v)} disabled={busy} error={errors.password} />
          </FormField>
        )}
      </form>
    </Modal>
  )
}
