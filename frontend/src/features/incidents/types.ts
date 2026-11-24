// Mirrors backend DTOs/Incidents/IncidentResponseDto.cs. Enums arrive as numbers.

export const IncidentStatus = {
  New: 1,
  AwaitingApproval: 2,
  AwaitingPayment: 3,
  AwaitingResolution: 4,
  Completed: 5,
  Rejected: 6,
  SentBack: 7,
  Canceled: 8,
  Duplicate: 9,
} as const
export type IncidentStatus = (typeof IncidentStatus)[keyof typeof IncidentStatus]

export const IncidentPriority = { Low: 1, Medium: 2, High: 3, Critical: 4 } as const
export type IncidentPriority = (typeof IncidentPriority)[keyof typeof IncidentPriority]

export const IncidentType = { CustomerInjury: 1, PropertyDamage: 2, WorkplaceAccident: 3 } as const
export type IncidentType = (typeof IncidentType)[keyof typeof IncidentType]

export type Incident = {
  incidentId: number
  companyId: number
  status: IncidentStatus
  title: string | null
  description: string | null
  priority: IncidentPriority | null
  incidentType: IncidentType | null
  /** Local wall-clock time entered by the user (use parseWallClock). */
  incidentDate: string | null
  storeId: number | null
  storeName: string | null
  locationId: number | null
  locationName: string | null
  resolutionTypeId: number | null
  resolutionTypeName: string | null
  customerName: string
  customerAddress: string | null
  customerEmail: string | null
  customerPhone: string | null
  estimatedCompensationAmount: number | null
  rejectionReason: string | null
  hasPendingDuplicateFlag: boolean
  createdByUserId: number
  /** For display ("Reported by"); compare users with createdByUserId. */
  createdByUserName: string
  /** UTC (use parseUtc) */
  createdAt: string
  updatedAt: string | null
  approvedByUserId: number | null
  approvedAt: string | null
  closedByUserId: number | null
  closedAt: string | null
}

// backend CreateIncidentRequestDto / UpdateIncidentRequestDto (same fields; create also takes companyId).
export type IncidentInput = {
  customerName: string
  title: string | null
  description: string | null
  priority: IncidentPriority | null
  incidentType: IncidentType | null
  /** Local wall-clock "YYYY-MM-DDTHH:mm:ss", no timezone. */
  incidentDate: string | null
  storeId: number | null
  locationId: number | null
  resolutionTypeId: number | null
  customerAddress: string | null
  customerEmail: string | null
  customerPhone: string | null
  estimatedCompensationAmount: number | null
}

// backend DTOs/Incidents/PossibleDuplicateDto.cs
export type PossibleDuplicate = {
  incidentId: number
  title: string | null
  status: IncidentStatus
  /** UTC */
  createdAt: string
  score: number
}

export const DuplicateReviewStatus = { Pending: 1, NotDuplicate: 2, ConfirmedDuplicate: 3 } as const
export type DuplicateReviewStatus = (typeof DuplicateReviewStatus)[keyof typeof DuplicateReviewStatus]

// backend DTOs/Incidents/IncidentDuplicateResponseDto.cs
export type DuplicateFlag = {
  incidentDuplicateId: number
  potentialDuplicateIncidentId: number
  potentialDuplicateTitle: string | null
  potentialDuplicateStatus: IncidentStatus
  detectionScore: number | null
  /** UTC */
  detectedAt: string
  reviewStatus: DuplicateReviewStatus
  reviewedByUserId: number | null
  reviewedAt: string | null
}

// backend DTOs/Incidents/IncidentHistoryResponseDto.cs. One changed field; rows saved together share changeSetId.
// Values are text: enums by name, dates ISO 8601, amounts with ".", store/location/resolution type as ids.
export type HistoryEntry = {
  historyId: number
  changeSetId: string
  fieldName: string
  oldValue: string | null
  newValue: string | null
  changedByUserId: number
  changedByUserName: string
  /** UTC */
  changedAt: string
}

// backend DTOs/Comments/CommentResponseDto.cs
export type Comment = {
  incidentCommentId: number
  incidentId: number
  userId: number
  userName: string
  content: string
  /** UTC */
  createdAt: string
}

// backend DTOs/Attachments/AttachmentResponseDto.cs
export type Attachment = {
  incidentAttachmentId: number
  incidentId: number
  fileName: string
  contentType: string
  uploadedByUserId: number
  uploadedByUserName: string
  /** UTC */
  uploadedAt: string
}
