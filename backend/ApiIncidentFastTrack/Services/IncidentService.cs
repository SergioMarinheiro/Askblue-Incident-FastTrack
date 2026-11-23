using System.Globalization;
using System.Linq.Expressions;
using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.DTOs.Incidents;
using ApiIncidentFastTrack.Enums;
using ApiIncidentFastTrack.Exceptions;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiIncidentFastTrack.Services;

// Visibility (every read goes through VisibleIncidents):
// - Company: the "Tenant" query filter on Incident (SuperAdmin: all companies).
// - Staff: only incidents they created. Anything not visible is reported as 404.
//
// Implemented: create, read (task 8), edit with history (task 9), status transitions and
// duplicate detection (task 10). Scoring lives in DuplicateScorer.
public class IncidentService : IIncidentService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ICompanyResolver _companyResolver;
    private readonly TimeProvider _timeProvider;

    public IncidentService(AppDbContext db, ICurrentUser currentUser, ICompanyResolver companyResolver, TimeProvider timeProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _companyResolver = companyResolver;
        _timeProvider = timeProvider;
    }

    // ---------- Reading ----------

    public async Task<IEnumerable<IncidentResponseDto>> GetAllIncidentsAsync(CancellationToken cancellationToken)
    {
        return await VisibleIncidents()
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .Select(ToDtoExpression)
            .ToListAsync(cancellationToken);
    }

    public async Task<IncidentResponseDto> GetIncidentByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await VisibleIncidents()
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(ToDtoExpression)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Incident {id} was not found.");
    }

    // Same visibility as the incident itself. IncidentHistory has no tenant filter
    // (child entity), so the incident is checked first through VisibleIncidents.
    public async Task<IEnumerable<IncidentHistoryResponseDto>> GetIncidentHistoryAsync(int id, CancellationToken cancellationToken)
    {
        await EnsureIncidentVisibleAsync(id, cancellationToken);

        return await _db.IncidentHistory
            .AsNoTracking()
            .Where(h => h.IncidentId == id)
            .OrderBy(h => h.CreatedAt).ThenBy(h => h.Id)
            .Select(h => new IncidentHistoryResponseDto
            {
                HistoryId = h.Id,
                ChangeSetId = h.ChangeSetId,
                FieldName = h.FieldName,
                OldValue = h.OldValue,
                NewValue = h.NewValue,
                ChangedByUserId = h.ChangedByUserId,
                ChangedByUserName = h.ChangedByUser.FirstName + " " + h.ChangedByUser.LastName,
                ChangedAt = h.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    // ---------- Draft editing ----------

    // Any role. Creates a draft (status New); only CustomerName is required.
    // No IncidentHistory rows on create (decided 2026-11-21): CreatedAt/CreatedByUserId record it,
    // and history starts with the first edit.
    public async Task<IncidentResponseDto> CreateIncidentAsync(CreateIncidentRequestDto request, CancellationToken cancellationToken)
    {
        var companyId = await _companyResolver.ResolveForCreateAsync(request.CompanyId, "an incident", cancellationToken);

        await ValidateReferencesAsync(
            companyId,
            request.StoreId,
            request.LocationId,
            request.ResolutionTypeId,
            checkStore: true,
            checkLocation: true,
            checkResolutionType: true,
            cancellationToken);

        var incident = new Incident
        {
            // Set by the server, never by the client
            CompanyId = companyId,
            CreatedByUserId = _currentUser.UserId,
            Status = IncidentStatus.New,

            CustomerName = request.CustomerName.Trim(),
            Title = Normalize(request.Title),
            Description = Normalize(request.Description),
            Priority = request.Priority,
            IncidentType = request.IncidentType,
            IncidentDate = request.IncidentDate,
            StoreId = request.StoreId,
            LocationId = request.LocationId,
            ResolutionTypeId = request.ResolutionTypeId,
            CustomerAddress = Normalize(request.CustomerAddress),
            CustomerEmail = Normalize(request.CustomerEmail),
            CustomerPhone = Normalize(request.CustomerPhone),
            EstimatedCompensationAmount = request.EstimatedCompensationAmount
        };

        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync(cancellationToken);

        // Read back through the same projection, so the response includes the store,
        // location and resolution type names exactly like GET does.
        return await GetIncidentByIdAsync(incident.Id, cancellationToken);
    }

    // Full replacement (PUT): every field is sent, null clears it. Never changes the status.
    // Each changed field becomes one IncidentHistory row; all rows of this save share a ChangeSetId
    // and are saved in the same SaveChanges as the incident (all or nothing).
    public async Task<IncidentResponseDto> UpdateIncidentAsync(int id, UpdateIncidentRequestDto request, CancellationToken cancellationToken)
    {
        var incident = await VisibleIncidents()
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Incident {id} was not found.");

        EnsureMayEdit(incident);

        // Only references that change are checked, so an incident whose store was deleted later
        // can still be edited. A new location needs an active store, so it re-checks the store too.
        var storeChanged = request.StoreId != incident.StoreId;
        var locationChanged = request.LocationId != incident.LocationId;
        var resolutionTypeChanged = request.ResolutionTypeId != incident.ResolutionTypeId;

        await ValidateReferencesAsync(
            incident.CompanyId,
            request.StoreId,
            request.LocationId,
            request.ResolutionTypeId,
            checkStore: storeChanged || locationChanged,
            checkLocation: storeChanged || locationChanged,
            checkResolutionType: resolutionTypeChanged,
            cancellationToken);

        var changeSetId = Guid.NewGuid();

        // Applies the new value and records a history row, but only if the value really changed.
        void Set<T>(string fieldName, T oldValue, T newValue, Action<T> apply)
        {
            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            {
                return;
            }

            apply(newValue);
            AddHistory(incident, changeSetId, fieldName, oldValue, newValue);
        }

        Set(nameof(Incident.CustomerName), incident.CustomerName, request.CustomerName.Trim(), v => incident.CustomerName = v);
        Set(nameof(Incident.Title), incident.Title, Normalize(request.Title), v => incident.Title = v);
        Set(nameof(Incident.Description), incident.Description, Normalize(request.Description), v => incident.Description = v);
        Set(nameof(Incident.Priority), incident.Priority, request.Priority, v => incident.Priority = v);
        Set(nameof(Incident.IncidentType), incident.IncidentType, request.IncidentType, v => incident.IncidentType = v);
        Set(nameof(Incident.IncidentDate), incident.IncidentDate, request.IncidentDate, v => incident.IncidentDate = v);
        Set(nameof(Incident.StoreId), incident.StoreId, request.StoreId, v => incident.StoreId = v);
        Set(nameof(Incident.LocationId), incident.LocationId, request.LocationId, v => incident.LocationId = v);
        Set(nameof(Incident.ResolutionTypeId), incident.ResolutionTypeId, request.ResolutionTypeId, v => incident.ResolutionTypeId = v);
        Set(nameof(Incident.CustomerAddress), incident.CustomerAddress, Normalize(request.CustomerAddress), v => incident.CustomerAddress = v);
        Set(nameof(Incident.CustomerEmail), incident.CustomerEmail, Normalize(request.CustomerEmail), v => incident.CustomerEmail = v);
        Set(nameof(Incident.CustomerPhone), incident.CustomerPhone, Normalize(request.CustomerPhone), v => incident.CustomerPhone = v);
        Set(nameof(Incident.EstimatedCompensationAmount), incident.EstimatedCompensationAmount, request.EstimatedCompensationAmount, v => incident.EstimatedCompensationAmount = v);

        // Nothing changed: no history rows, and EF leaves the row (and ModifiedAt) untouched.
        await _db.SaveChangesAsync(cancellationToken);

        return await GetIncidentByIdAsync(incident.Id, cancellationToken);
    }

    // The only way an incident is ever deleted (hard delete): the creator acknowledges that the
    // duplicate check found the same incident. Only while New (decided 2026-11-22), and only if the
    // check, run again now, still finds matches.
    public async Task DeleteDuplicateIncidentAsync(int id, CancellationToken cancellationToken)
    {
        var incident = await LoadForChangeAsync(id, cancellationToken);

        if (incident.CreatedByUserId != _currentUser.UserId)
        {
            throw new ForbiddenException("Only the person who created this incident can delete it.");
        }

        EnsureStatus(incident, "deleted", IncidentStatus.New);

        var matches = await FindPossibleDuplicatesAsync(incident, cancellationToken);

        if (matches.Count == 0)
        {
            throw new ConflictException(
                "An incident can only be deleted when the duplicate check finds possible duplicates. Cancel it instead.");
        }

        // Its own comments, history, attachments and flags are removed by the database (Cascade).
        // Flags on OTHER incidents that point at it are Restrict, so they are removed here explicitly:
        // the incident they point at no longer exists.
        var flagsPointingHere = await _db.IncidentDuplicates
            .Where(d => d.PotentialDuplicateIncidentId == incident.Id)
            .ToListAsync(cancellationToken);

        _db.IncidentDuplicates.RemoveRange(flagsPointingHere);
        _db.Incidents.Remove(incident);

        await _db.SaveChangesAsync(cancellationToken);
    }

    // ---------- Status transitions ----------
    // Each one: load through VisibleIncidents (404), check the caller and the current status
    // (403 / 409), change the status with a "Status" history row, save once (all or nothing).
    // Role gates (ManagerOrAbove for approve/reject/send-back) are on the controller.

    // The creator only, whatever their role: New/SentBack -> AwaitingApproval.
    // Runs the duplicate check first:
    // - No matches: submitted.
    // - Matches, ProceedDespiteDuplicates = false: nothing changes; the controller returns 409 with the list.
    // - Matches, ProceedDespiteDuplicates = true: submitted, with a Pending flag per match.
    public async Task<SubmitIncidentResultDto> SubmitIncidentAsync(int id, SubmitIncidentRequestDto request, CancellationToken cancellationToken)
    {
        var incident = await LoadForChangeAsync(id, cancellationToken);

        if (incident.CreatedByUserId != _currentUser.UserId)
        {
            throw new ForbiddenException("Only the person who created this incident can submit it.");
        }

        EnsureStatus(incident, "submitted", IncidentStatus.New, IncidentStatus.SentBack);
        EnsureRequiredForSubmit(incident);

        // The store, location or resolution type may have been deleted since they were chosen.
        await ValidateReferencesAsync(
            incident.CompanyId,
            incident.StoreId,
            incident.LocationId,
            incident.ResolutionTypeId,
            checkStore: true,
            checkLocation: true,
            checkResolutionType: true,
            cancellationToken);

        var matches = await FindPossibleDuplicatesAsync(incident, cancellationToken);

        if (matches.Count > 0 && !request.ProceedDespiteDuplicates)
        {
            return new SubmitIncidentResultDto
            {
                Submitted = false,
                PossibleDuplicates = matches
                    .Select(m => new PossibleDuplicateDto
                    {
                        IncidentId = m.Candidate.Id,
                        Title = m.Candidate.Title,
                        Status = m.Candidate.Status,
                        CreatedAt = m.Candidate.CreatedAt,
                        Score = m.Score
                    })
                    .ToList()
            };
        }

        foreach (var match in matches)
        {
            _db.IncidentDuplicates.Add(new IncidentDuplicate
            {
                IncidentId = incident.Id,
                PotentialDuplicateIncidentId = match.Candidate.Id,
                DetectionScore = match.Score,
                ReviewStatus = DuplicateReviewStatus.Pending
            });
        }

        ChangeStatus(incident, IncidentStatus.AwaitingApproval, Guid.NewGuid());
        await _db.SaveChangesAsync(cancellationToken);

        return new SubmitIncidentResultDto
        {
            Submitted = true,
            Incident = await GetIncidentByIdAsync(incident.Id, cancellationToken)
        };
    }

    // Manager or above: AwaitingApproval -> AwaitingPayment or AwaitingResolution.
    // The next status always follows from ResolutionType.RequiresPayment; the caller doesn't choose.
    public async Task<IncidentResponseDto> ApproveIncidentAsync(int id, CancellationToken cancellationToken)
    {
        var incident = await LoadForChangeAsync(id, cancellationToken);

        // Decided 2026-11-22: e.g. a Manager approving their own compensation.
        if (incident.CreatedByUserId == _currentUser.UserId)
        {
            throw new ForbiddenException("You can't approve an incident you created.");
        }

        EnsureStatus(incident, "approved", IncidentStatus.AwaitingApproval);

        // Decided 2026-11-22: the Manager must dismiss or confirm the flags first.
        var hasPendingFlag = await _db.IncidentDuplicates
            .AnyAsync(d => d.IncidentId == incident.Id && d.ReviewStatus == DuplicateReviewStatus.Pending, cancellationToken);

        if (hasPendingFlag)
        {
            throw new ConflictException("This incident has possible duplicates. Review the duplicate flags first.");
        }

        // Set at submit, but an Admin can edit any field afterwards, including clearing it.
        var requiresPayment = await _db.ResolutionTypes
            .Where(r => r.Id == incident.ResolutionTypeId)
            .Select(r => (bool?)r.RequiresPayment)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleException("Choose a resolution type before approving.");

        var next = requiresPayment ? IncidentStatus.AwaitingPayment : IncidentStatus.AwaitingResolution;

        ChangeStatus(incident, next, Guid.NewGuid());

        // Decided 2026-11-23: dedicated fields so lists and reports can filter on them.
        // The status change is still recorded in IncidentHistory by ChangeStatus.
        incident.ApprovedAt = _timeProvider.GetUtcNow().UtcDateTime;
        incident.ApprovedByUserId = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetIncidentByIdAsync(incident.Id, cancellationToken);
    }

    // Manager or above: AwaitingApproval -> Rejected (final). The reason is required (DTO).
    public async Task<IncidentResponseDto> RejectIncidentAsync(int id, RejectIncidentRequestDto request, CancellationToken cancellationToken)
    {
        var incident = await LoadForChangeAsync(id, cancellationToken);

        EnsureStatus(incident, "rejected", IncidentStatus.AwaitingApproval);

        var changeSetId = Guid.NewGuid();
        var reason = request.Reason.Trim();

        AddHistory(incident, changeSetId, nameof(Incident.RejectionReason), incident.RejectionReason, reason);
        incident.RejectionReason = reason;
        ChangeStatus(incident, IncidentStatus.Rejected, changeSetId);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetIncidentByIdAsync(incident.Id, cancellationToken);
    }

    // Manager or above: AwaitingApproval -> SentBack. What to fix is saved as a comment,
    // in the same save as the status change (decided 2026-11-22).
    public async Task<IncidentResponseDto> SendBackIncidentAsync(int id, SendBackIncidentRequestDto request, CancellationToken cancellationToken)
    {
        var incident = await LoadForChangeAsync(id, cancellationToken);

        EnsureStatus(incident, "sent back", IncidentStatus.AwaitingApproval);

        _db.IncidentComments.Add(new IncidentComment
        {
            IncidentId = incident.Id,
            UserId = _currentUser.UserId,
            Content = request.Comment.Trim()
        });

        ChangeStatus(incident, IncidentStatus.SentBack, Guid.NewGuid());
        await _db.SaveChangesAsync(cancellationToken);

        return await GetIncidentByIdAsync(incident.Id, cancellationToken);
    }

    // Anyone who can see the incident: AwaitingPayment/AwaitingResolution -> Completed.
    public async Task<IncidentResponseDto> CompleteIncidentAsync(int id, CancellationToken cancellationToken)
    {
        var incident = await LoadForChangeAsync(id, cancellationToken);

        EnsureStatus(incident, "completed", IncidentStatus.AwaitingPayment, IncidentStatus.AwaitingResolution);

        incident.ClosedAt = _timeProvider.GetUtcNow().UtcDateTime;
        incident.ClosedByUserId = _currentUser.UserId;
        ChangeStatus(incident, IncidentStatus.Completed, Guid.NewGuid());

        await _db.SaveChangesAsync(cancellationToken);

        return await GetIncidentByIdAsync(incident.Id, cancellationToken);
    }

    // Staff (own, guaranteed by VisibleIncidents), Manager, Admin, SuperAdmin: any open status -> Canceled.
    // Doesn't set ClosedAt: CLAUDE.md only asks that for Completed and Duplicate.
    public async Task<IncidentResponseDto> CancelIncidentAsync(int id, CancellationToken cancellationToken)
    {
        var incident = await LoadForChangeAsync(id, cancellationToken);

        EnsureStatus(incident, "canceled",
            IncidentStatus.New,
            IncidentStatus.AwaitingApproval,
            IncidentStatus.SentBack,
            IncidentStatus.AwaitingPayment,
            IncidentStatus.AwaitingResolution);

        ChangeStatus(incident, IncidentStatus.Canceled, Guid.NewGuid());
        await _db.SaveChangesAsync(cancellationToken);

        return await GetIncidentByIdAsync(incident.Id, cancellationToken);
    }

    // ---------- Duplicate flags ----------

    // Anyone who can see the incident: its flags with the matching incidents, best match first.
    public async Task<IEnumerable<IncidentDuplicateResponseDto>> GetDuplicateFlagsAsync(int id, CancellationToken cancellationToken)
    {
        await EnsureIncidentVisibleAsync(id, cancellationToken);

        return await _db.IncidentDuplicates
            .AsNoTracking()
            .Where(d => d.IncidentId == id)
            .OrderByDescending(d => d.DetectionScore)
            .Select(d => new IncidentDuplicateResponseDto
            {
                IncidentDuplicateId = d.Id,
                PotentialDuplicateIncidentId = d.PotentialDuplicateIncidentId,
                PotentialDuplicateTitle = d.PotentialDuplicateIncident.Title,
                PotentialDuplicateStatus = d.PotentialDuplicateIncident.Status,
                DetectionScore = d.DetectionScore,
                DetectedAt = d.CreatedAt,
                ReviewStatus = d.ReviewStatus,
                ReviewedByUserId = d.ReviewedByUserId,
                ReviewedAt = d.ReviewedAt
            })
            .ToListAsync(cancellationToken);
    }

    // Manager or above: "not a duplicate". Pending flags -> NotDuplicate; the incident stays
    // AwaitingApproval and can now be approved as normal.
    public async Task<IncidentResponseDto> DismissDuplicateFlagsAsync(int id, CancellationToken cancellationToken)
    {
        var incident = await LoadForChangeAsync(id, cancellationToken);

        EnsureStatus(incident, "reviewed for duplicates", IncidentStatus.AwaitingApproval);

        var pending = await LoadPendingFlagsAsync(incident, cancellationToken);
        MarkReviewed(pending, DuplicateReviewStatus.NotDuplicate);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetIncidentByIdAsync(incident.Id, cancellationToken);
    }

    // Manager or above: "yes, it's a duplicate". Pending flags -> ConfirmedDuplicate, status Duplicate
    // (final, closed). The explanation is required and saved as a comment (decided 2026-11-22).
    public async Task<IncidentResponseDto> ConfirmDuplicateAsync(int id, ConfirmDuplicateRequestDto request, CancellationToken cancellationToken)
    {
        var incident = await LoadForChangeAsync(id, cancellationToken);

        EnsureStatus(incident, "marked as a duplicate", IncidentStatus.AwaitingApproval);

        var pending = await LoadPendingFlagsAsync(incident, cancellationToken);
        MarkReviewed(pending, DuplicateReviewStatus.ConfirmedDuplicate);

        _db.IncidentComments.Add(new IncidentComment
        {
            IncidentId = incident.Id,
            UserId = _currentUser.UserId,
            Content = request.Comment.Trim()
        });

        incident.ClosedAt = _timeProvider.GetUtcNow().UtcDateTime;
        incident.ClosedByUserId = _currentUser.UserId;
        ChangeStatus(incident, IncidentStatus.Duplicate, Guid.NewGuid());

        await _db.SaveChangesAsync(cancellationToken);

        return await GetIncidentByIdAsync(incident.Id, cancellationToken);
    }

    // ---------- Helpers ----------

    // Incidents the caller may see. The tenant filter limits the company; Staff are further
    // limited to their own incidents. Every read and every write starts here.
    // Tracked, so writes can use it; reads add AsNoTracking.
    // The rule itself lives in IncidentVisibility, shared with IncidentCommentService.
    private IQueryable<Incident> VisibleIncidents() => _db.Incidents.VisibleTo(_currentUser);

    // Store, location and resolution type are optional on a draft, but whatever is sent must
    // exist, not be deleted, and belong to the incident's company.
    // The explicit CompanyId check matters for a SuperAdmin: the tenant filter lets them see
    // every company, so without it company A's incident could point at company B's store.
    // Errors are 422, not 404: the URL is the incident; these are invalid values in the body.
    // On update, the check flags skip references that did not change (see UpdateIncidentAsync).
    private async Task ValidateReferencesAsync(
        int companyId,
        int? storeId,
        int? locationId,
        int? resolutionTypeId,
        bool checkStore,
        bool checkLocation,
        bool checkResolutionType,
        CancellationToken cancellationToken)
    {
        if (checkStore && storeId is int sId)
        {
            var storeOk = await _db.Stores
                .AnyAsync(s => s.Id == sId && s.CompanyId == companyId && !s.IsDeleted, cancellationToken);

            if (!storeOk)
            {
                throw new BusinessRuleException($"Store {sId} does not exist.");
            }
        }

        if (checkLocation && locationId is int lId)
        {
            if (storeId is not int locationStoreId)
            {
                throw new BusinessRuleException("A location can only be chosen together with its store.");
            }

            // The store was already checked above (company, not deleted).
            var locationOk = await _db.Locations
                .AnyAsync(l => l.Id == lId && l.StoreId == locationStoreId && !l.IsDeleted, cancellationToken);

            if (!locationOk)
            {
                throw new BusinessRuleException($"Location {lId} does not exist in store {locationStoreId}.");
            }
        }

        if (checkResolutionType && resolutionTypeId is int rId)
        {
            var resolutionTypeOk = await _db.ResolutionTypes
                .AnyAsync(r => r.Id == rId && r.CompanyId == companyId, cancellationToken);

            if (!resolutionTypeOk)
            {
                throw new BusinessRuleException($"Resolution type {rId} does not exist.");
            }
        }
    }

    private static readonly IncidentStatus[] OpenStatuses =
    {
        IncidentStatus.New,
        IncidentStatus.AwaitingApproval,
        IncidentStatus.SentBack,
        IncidentStatus.AwaitingPayment,
        IncidentStatus.AwaitingResolution
    };

    // Open incidents of the same company that score at least DuplicateScorer.Threshold, best first.
    // - Deliberately NOT through VisibleIncidents: Staff must be warned about other people's incidents too.
    //   The company is filtered explicitly (the tenant filter doesn't restrict a SuperAdmin).
    // - Pairs already linked by a flag, in either direction and whatever its review status,
    //   are skipped: a pair is checked only once (decided 2026-11-22).
    // The candidates are compared in C# (decided 2026-11-22); only the compared fields are loaded.
    private async Task<List<(DuplicateCandidate Candidate, int Score)>> FindPossibleDuplicatesAsync(Incident incident, CancellationToken cancellationToken)
    {
        var alreadyLinked = await _db.IncidentDuplicates
            .Where(d => d.IncidentId == incident.Id || d.PotentialDuplicateIncidentId == incident.Id)
            .Select(d => d.IncidentId == incident.Id ? d.PotentialDuplicateIncidentId : d.IncidentId)
            .ToListAsync(cancellationToken);

        var candidates = await _db.Incidents
            .AsNoTracking()
            .Where(i => i.CompanyId == incident.CompanyId
                        && i.Id != incident.Id
                        && OpenStatuses.Contains(i.Status)
                        && !alreadyLinked.Contains(i.Id))
            .Select(i => new DuplicateCandidate(
                i.Id, i.Title, i.Description, i.Status, i.CreatedAt, i.IncidentType,
                i.StoreId, i.LocationId, i.IncidentDate, i.CustomerName, i.CustomerAddress, i.CreatedByUserId))
            .ToListAsync(cancellationToken);

        var self = new DuplicateCandidate(
            incident.Id, incident.Title, incident.Description, incident.Status, incident.CreatedAt, incident.IncidentType,
            incident.StoreId, incident.LocationId, incident.IncidentDate, incident.CustomerName, incident.CustomerAddress, incident.CreatedByUserId);

        return candidates
            .Select(c => (Candidate: c, Score: DuplicateScorer.Score(self, c)))
            .Where(m => m.Score >= DuplicateScorer.Threshold)
            .OrderByDescending(m => m.Score)
            .ToList();
    }

    // 409 when there is nothing to review: dismiss/confirm only make sense on a flagged incident.
    private async Task<List<IncidentDuplicate>> LoadPendingFlagsAsync(Incident incident, CancellationToken cancellationToken)
    {
        var pending = await _db.IncidentDuplicates
            .Where(d => d.IncidentId == incident.Id && d.ReviewStatus == DuplicateReviewStatus.Pending)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            throw new ConflictException("This incident has no duplicate flags waiting for review.");
        }

        return pending;
    }

    // Who reviewed and when are kept on each flag.
    private void MarkReviewed(List<IncidentDuplicate> flags, DuplicateReviewStatus result)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var flag in flags)
        {
            flag.ReviewStatus = result;
            flag.ReviewedByUserId = _currentUser.UserId;
            flag.ReviewedAt = now;
        }
    }

    // Tracked incident for a write, through the visibility rules. 404 if not visible.
    private async Task<Incident> LoadForChangeAsync(int id, CancellationToken cancellationToken)
    {
        return await VisibleIncidents()
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Incident {id} was not found.");
    }

    // 409: the action conflicts with the incident's current state.
    private static void EnsureStatus(Incident incident, string action, params IncidentStatus[] allowed)
    {
        if (!allowed.Contains(incident.Status))
        {
            throw new ConflictException($"This incident can't be {action} while its status is {incident.Status}.");
        }
    }

    // Every field except EstimatedCompensationAmount (CLAUDE.md). One 422 listing everything missing,
    // so the reporter can fix it all at once instead of one field per attempt.
    private static void EnsureRequiredForSubmit(Incident incident)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(incident.Title)) missing.Add(nameof(Incident.Title));
        if (string.IsNullOrWhiteSpace(incident.Description)) missing.Add(nameof(Incident.Description));
        if (incident.Priority is null) missing.Add(nameof(Incident.Priority));
        if (incident.IncidentType is null) missing.Add(nameof(Incident.IncidentType));
        if (incident.IncidentDate is null) missing.Add(nameof(Incident.IncidentDate));
        if (incident.StoreId is null) missing.Add(nameof(Incident.StoreId));
        if (incident.LocationId is null) missing.Add(nameof(Incident.LocationId));
        if (incident.ResolutionTypeId is null) missing.Add(nameof(Incident.ResolutionTypeId));
        if (string.IsNullOrWhiteSpace(incident.CustomerName)) missing.Add(nameof(Incident.CustomerName));
        if (string.IsNullOrWhiteSpace(incident.CustomerAddress)) missing.Add(nameof(Incident.CustomerAddress));
        if (string.IsNullOrWhiteSpace(incident.CustomerEmail)) missing.Add(nameof(Incident.CustomerEmail));
        if (string.IsNullOrWhiteSpace(incident.CustomerPhone)) missing.Add(nameof(Incident.CustomerPhone));

        if (missing.Count > 0)
        {
            throw new BusinessRuleException($"Fill in these fields before submitting: {string.Join(", ", missing)}.");
        }
    }

    // Every status change is a "Status" history row (decided 2026-11-22), like an edit.
    private void ChangeStatus(Incident incident, IncidentStatus newStatus, Guid changeSetId)
    {
        AddHistory(incident, changeSetId, nameof(Incident.Status), incident.Status, newStatus);
        incident.Status = newStatus;
    }

    // One history row: who (the caller) and when (CreatedAt, set by AppDbContext) are filled in automatically.
    private void AddHistory<T>(Incident incident, Guid changeSetId, string fieldName, T oldValue, T newValue)
    {
        _db.IncidentHistory.Add(new IncidentHistory
        {
            IncidentId = incident.Id,
            ChangedByUserId = _currentUser.UserId,
            ChangeSetId = changeSetId,
            FieldName = fieldName,
            OldValue = FormatHistoryValue(oldValue),
            NewValue = FormatHistoryValue(newValue)
        });
    }

    private async Task EnsureIncidentVisibleAsync(int id, CancellationToken cancellationToken)
    {
        if (!await VisibleIncidents().AnyAsync(i => i.Id == id, cancellationToken))
        {
            throw new NotFoundException($"Incident {id} was not found.");
        }
    }

    // Who may edit in which status (CLAUDE.md, "Editing"). Visibility (own incident for Staff,
    // own company) is already guaranteed by VisibleIncidents. Wrong status is 409: the request
    // conflicts with the incident's current state.
    private void EnsureMayEdit(Incident incident)
    {
        var allowed = _currentUser.Role switch
        {
            UserRole.Admin or UserRole.SuperAdmin => true,

            // Until approved, rejected or marked Duplicate. Not Canceled either (decided 2026-11-21).
            UserRole.Manager => incident.Status is IncidentStatus.New or IncidentStatus.AwaitingApproval or IncidentStatus.SentBack,

            UserRole.Staff => incident.Status is IncidentStatus.New or IncidentStatus.SentBack,

            _ => false
        };

        if (!allowed)
        {
            throw new ConflictException($"This incident can't be edited while its status is {incident.Status}.");
        }
    }

    // History columns are text. Culture-independent, so a pt-PT server doesn't write "1234,56".
    private static string? FormatHistoryValue<T>(T value) => value switch
    {
        null => null,
        string s => s,
        DateTime d => d.ToString("O", CultureInfo.InvariantCulture),
        Enum e => e.ToString(),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // One projection for every read, translated to SQL (joins for the names, EXISTS for the flag).
    // Soft-deleted stores and locations still show their names on old incidents, by design.
    private static readonly Expression<Func<Incident, IncidentResponseDto>> ToDtoExpression = i => new IncidentResponseDto
    {
        IncidentId = i.Id,
        CompanyId = i.CompanyId,
        Status = i.Status,
        Title = i.Title,
        Description = i.Description,
        Priority = i.Priority,
        IncidentType = i.IncidentType,
        IncidentDate = i.IncidentDate,
        StoreId = i.StoreId,
        StoreName = i.Store != null ? i.Store.Name : null,
        LocationId = i.LocationId,
        LocationName = i.Location != null ? i.Location.Name : null,
        ResolutionTypeId = i.ResolutionTypeId,
        ResolutionTypeName = i.ResolutionType != null ? i.ResolutionType.Name : null,
        CustomerName = i.CustomerName,
        CustomerAddress = i.CustomerAddress,
        CustomerEmail = i.CustomerEmail,
        CustomerPhone = i.CustomerPhone,
        EstimatedCompensationAmount = i.EstimatedCompensationAmount,
        RejectionReason = i.RejectionReason,
        HasPendingDuplicateFlag = i.DuplicateRelationships.Any(d => d.ReviewStatus == DuplicateReviewStatus.Pending),
        CreatedByUserId = i.CreatedByUserId,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.ModifiedAt,
        ApprovedByUserId = i.ApprovedByUserId,
        ApprovedAt = i.ApprovedAt,
        ClosedByUserId = i.ClosedByUserId,
        ClosedAt = i.ClosedAt
    };
}
