// What the current user may do with an incident. Mirrors the backend rules
// (IncidentService, IncidentAttachmentService, AuthPolicies) so the UI only shows
// buttons that will work. The API still enforces everything.
import type { CurrentUser } from '@/auth/AuthContext'
import { hasRole } from '@/auth/roles'
import { IncidentStatus as S, type Incident, type IncidentStatus } from './types'

const MANAGER_OR_ABOVE = ['Manager', 'Admin', 'SuperAdmin'] as const

// Statuses that can be canceled. Not New: a draft is deleted by its creator instead.
const CANCELABLE: IncidentStatus[] = [S.AwaitingApproval, S.SentBack, S.AwaitingPayment, S.AwaitingResolution]
const FILES_MAY_CHANGE: IncidentStatus[] = [S.New, S.AwaitingApproval, S.SentBack]

export type IncidentPermissions = {
  isCreator: boolean
  edit: boolean
  submit: boolean
  /** Hard delete of a draft (status New) by its creator. Drafts are deleted, never canceled. */
  deleteDraft: boolean
  changeFiles: boolean
  approve: boolean
  /** Why approve is not possible right now, if a reviewer would otherwise expect it. */
  approveBlockedReason: string | null
  reject: boolean
  sendBack: boolean
  reviewDuplicates: boolean
  complete: boolean
  cancel: boolean
}

export function incidentPermissions(user: CurrentUser | null, incident: Incident): IncidentPermissions {
  const role = user?.role
  const status = incident.status
  const isCreator = user !== null && incident.createdByUserId === user.userId
  const isReviewer = hasRole(role, MANAGER_OR_ABOVE)
  const awaitingApproval = status === S.AwaitingApproval

  // IncidentService.EnsureMayEdit
  const edit =
    role === 'Admin' || role === 'SuperAdmin'
      ? true
      : role === 'Manager'
        ? status === S.New || status === S.AwaitingApproval || status === S.SentBack
        : role === 'Staff'
          ? status === S.New || status === S.SentBack
          : false

  const canApproveBase = isReviewer && awaitingApproval
  let approveBlockedReason: string | null = null
  if (canApproveBase && isCreator) approveBlockedReason = "You can't approve an incident you created."
  else if (canApproveBase && incident.hasPendingDuplicateFlag) approveBlockedReason = 'Review the possible duplicates first.'

  return {
    isCreator,
    edit,
    submit: isCreator && (status === S.New || status === S.SentBack),
    deleteDraft: isCreator && status === S.New,
    changeFiles: FILES_MAY_CHANGE.includes(status),
    approve: canApproveBase && approveBlockedReason === null,
    approveBlockedReason,
    reject: isReviewer && awaitingApproval,
    sendBack: isReviewer && awaitingApproval,
    reviewDuplicates: isReviewer && awaitingApproval && incident.hasPendingDuplicateFlag,
    complete: status === S.AwaitingPayment || status === S.AwaitingResolution,
    cancel: CANCELABLE.includes(status),
  }
}
