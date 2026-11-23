import { useQuery } from '@tanstack/react-query'
import { ApiError, apiDownload, apiFetch } from '@/lib/api'
import type { Attachment, Comment, DuplicateFlag, Incident, IncidentInput, PossibleDuplicate } from './types'

export const incidentKeys = {
  all: ['incidents'] as const,
  list: () => [...incidentKeys.all, 'list'] as const,
  detail: (id: number) => [...incidentKeys.all, 'detail', id] as const,
  comments: (id: number) => [...incidentKeys.all, 'detail', id, 'comments'] as const,
  attachments: (id: number) => [...incidentKeys.all, 'detail', id, 'attachments'] as const,
}

/** Incidents the current user can see (Staff: own; Manager/Admin: company; SuperAdmin: all), newest first. */
export function useIncidents() {
  return useQuery({
    queryKey: incidentKeys.list(),
    queryFn: ({ signal }) => apiFetch<Incident[]>('/api/incidents', { signal }),
  })
}

export function useIncident(id: number) {
  return useQuery({
    queryKey: incidentKeys.detail(id),
    queryFn: ({ signal }) => apiFetch<Incident>(`/api/incidents/${id}`, { signal }),
    enabled: id > 0,
  })
}

export function useIncidentComments(id: number) {
  return useQuery({
    queryKey: incidentKeys.comments(id),
    queryFn: ({ signal }) => apiFetch<Comment[]>(`/api/incidents/${id}/comments`, { signal }),
  })
}

export function useIncidentAttachments(id: number) {
  return useQuery({
    queryKey: incidentKeys.attachments(id),
    queryFn: ({ signal }) => apiFetch<Attachment[]>(`/api/incidents/${id}/attachments`, { signal }),
  })
}

// ---- Writes ----

/** Creates a draft (status New). `companyId` is only sent by a SuperAdmin. */
export function createIncident(input: IncidentInput & { companyId?: number }) {
  return apiFetch<Incident>('/api/incidents', { method: 'POST', body: input })
}

export function updateIncident(id: number, input: IncidentInput) {
  return apiFetch<Incident>(`/api/incidents/${id}`, { method: 'PUT', body: input })
}

export type SubmitResult = { submitted: true; incident: Incident } | { submitted: false; duplicates: PossibleDuplicate[] }

/** New/SentBack -> AwaitingApproval. The API answers 409 with possible duplicates unless `proceed` is true. */
export async function submitIncident(id: number, proceed: boolean): Promise<SubmitResult> {
  try {
    const incident = await apiFetch<Incident>(`/api/incidents/${id}/submit`, {
      method: 'POST',
      body: { proceedDespiteDuplicates: proceed },
    })
    return { submitted: true, incident }
  } catch (err) {
    if (err instanceof ApiError && err.status === 409 && Array.isArray(err.data)) {
      return { submitted: false, duplicates: err.data as PossibleDuplicate[] }
    }
    throw err
  }
}

/** Hard-deletes a draft; only allowed when the duplicate check finds matches. */
export function deleteDraftIncident(id: number) {
  return apiFetch<void>(`/api/incidents/${id}`, { method: 'DELETE' })
}

export function uploadAttachment(incidentId: number, file: File) {
  const form = new FormData()
  form.append('file', file)
  return apiFetch<Attachment>(`/api/incidents/${incidentId}/attachments`, { method: 'POST', body: form })
}

export function deleteAttachment(incidentId: number, attachmentId: number) {
  return apiFetch<void>(`/api/incidents/${incidentId}/attachments/${attachmentId}`, { method: 'DELETE' })
}

export function addComment(incidentId: number, content: string) {
  return apiFetch<Comment>(`/api/incidents/${incidentId}/comments`, { method: 'POST', body: { content } })
}

// ---- Status changes (each returns the updated incident) ----

const post = (id: number, action: string, body?: unknown) =>
  apiFetch<Incident>(`/api/incidents/${id}/${action}`, { method: 'POST', body })

export const incidentActions = {
  approve: (id: number) => post(id, 'approve'),
  reject: (id: number, reason: string) => post(id, 'reject', { reason }),
  sendBack: (id: number, comment: string) => post(id, 'send-back', { comment }),
  complete: (id: number) => post(id, 'complete'),
  cancel: (id: number) => post(id, 'cancel'),
  dismissDuplicates: (id: number) => post(id, 'duplicates/dismiss'),
  confirmDuplicate: (id: number, comment: string) => post(id, 'duplicates/confirm', { comment }),
}

export function useDuplicateFlags(id: number, enabled: boolean) {
  return useQuery({
    queryKey: [...incidentKeys.detail(id), 'duplicates'],
    queryFn: ({ signal }) => apiFetch<DuplicateFlag[]>(`/api/incidents/${id}/duplicates`, { signal }),
    enabled,
  })
}

export function downloadAttachment(attachment: Attachment) {
  return apiDownload(
    `/api/incidents/${attachment.incidentId}/attachments/${attachment.incidentAttachmentId}`,
    attachment.fileName,
  )
}
