using System.Linq.Expressions;
using ApiIncidentFastTrack.Data;
using ApiIncidentFastTrack.DTOs.Attachments;
using ApiIncidentFastTrack.Enums;
using ApiIncidentFastTrack.Exceptions;
using ApiIncidentFastTrack.Interfaces;
using ApiIncidentFastTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiIncidentFastTrack.Services;

// Rules (decided 2026-11-23):
// - Who: anyone who can see the incident (IncidentVisibility): its Staff creator, and every
//   Manager/Admin of the company (SuperAdmin: all). Staff may delete any file on their incident.
//   Anything else is 404, like the incident itself.
// - Upload and delete only while New, AwaitingApproval or SentBack (409 otherwise).
//   List and download in any status.
// - Any file type, at most 20 MB. Uploads and deletes are recorded in IncidentHistory
//   (field "Attachment", the file name as new value on upload, as old value on delete).
// IncidentAttachment has no tenant filter (child entity): the incident is checked first.
public class IncidentAttachmentService : IIncidentAttachmentService
{
    private const string HistoryFieldName = "Attachment";
    private const string FallbackContentType = "application/octet-stream";

    // Column lengths in IncidentAttachmentConfiguration.
    private const int MaxFileNameLength = 260;
    private const int MaxContentTypeLength = 100;

    private static readonly IncidentStatus[] StatusesThatAllowChanges =
        [IncidentStatus.New, IncidentStatus.AwaitingApproval, IncidentStatus.SentBack];

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<IncidentAttachmentService> _logger;

    public IncidentAttachmentService(AppDbContext db, ICurrentUser currentUser, IFileStorage fileStorage, ILogger<IncidentAttachmentService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    // Oldest first, like comments.
    public async Task<IEnumerable<AttachmentResponseDto>> GetAttachmentsAsync(int incidentId, CancellationToken cancellationToken)
    {
        await GetVisibleIncidentStatusAsync(incidentId, cancellationToken);

        return await _db.IncidentAttachments
            .AsNoTracking()
            .Where(a => a.IncidentId == incidentId)
            .OrderBy(a => a.CreatedAt).ThenBy(a => a.Id)
            .Select(ToDtoExpression)
            .ToListAsync(cancellationToken);
    }

    public async Task<AttachmentResponseDto> UploadAttachmentAsync(int incidentId, UploadAttachmentRequestDto request, CancellationToken cancellationToken)
    {
        var status = await GetVisibleIncidentStatusAsync(incidentId, cancellationToken);
        EnsureFilesMayChange(status, "added");

        var file = request.File;

        if (file.Length == 0)
        {
            throw new BusinessRuleException("The file is empty.");
        }

        if (file.Length > AttachmentStorageOptions.MaxFileSizeBytes)
        {
            throw new BusinessRuleException("Files can be at most 20 MB.");
        }

        var fileName = CleanFileName(file.FileName);

        // 1. The file first: if the database save below fails, the file is removed again,
        //    so a row never points at a missing file and no orphan file is left behind.
        string storageKey;
        await using (var content = file.OpenReadStream())
        {
            storageKey = await _fileStorage.SaveAsync(content, cancellationToken);
        }

        var attachment = new IncidentAttachment
        {
            IncidentId = incidentId,
            UploadedByUserId = _currentUser.UserId,
            FileName = fileName,
            StoragePath = storageKey,
            // As declared by the client. Only informative: downloads are always sent as a
            // generic binary file, so this value can't make the browser run anything.
            ContentType = CleanContentType(file.ContentType)
        };

        _db.IncidentAttachments.Add(attachment);
        AddHistory(incidentId, oldValue: null, newValue: fileName);

        // 2. Then the row and its history, in one save.
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await DeleteStoredFileQuietlyAsync(storageKey);
            throw;
        }

        return await _db.IncidentAttachments
            .AsNoTracking()
            .Where(a => a.Id == attachment.Id)
            .Select(ToDtoExpression)
            .SingleAsync(cancellationToken);
    }

    public async Task<AttachmentDownloadDto> DownloadAttachmentAsync(int incidentId, int attachmentId, CancellationToken cancellationToken)
    {
        await GetVisibleIncidentStatusAsync(incidentId, cancellationToken);

        var attachment = await _db.IncidentAttachments
            .AsNoTracking()
            .Where(a => a.Id == attachmentId && a.IncidentId == incidentId)
            .Select(a => new { a.FileName, a.StoragePath })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Attachment {attachmentId} was not found.");

        var content = await _fileStorage.OpenReadAsync(attachment.StoragePath, cancellationToken);

        if (content is null)
        {
            // The row exists but the file is gone (removed from the folder by hand, lost backup...).
            _logger.LogWarning("Stored file {StorageKey} for attachment {AttachmentId} is missing.", attachment.StoragePath, attachmentId);
            throw new NotFoundException("This file is no longer available.");
        }

        return new AttachmentDownloadDto { Content = content, FileName = attachment.FileName };
    }

    public async Task DeleteAttachmentAsync(int incidentId, int attachmentId, CancellationToken cancellationToken)
    {
        var status = await GetVisibleIncidentStatusAsync(incidentId, cancellationToken);
        EnsureFilesMayChange(status, "deleted");

        var attachment = await _db.IncidentAttachments
            .SingleOrDefaultAsync(a => a.Id == attachmentId && a.IncidentId == incidentId, cancellationToken)
            ?? throw new NotFoundException($"Attachment {attachmentId} was not found.");

        _db.IncidentAttachments.Remove(attachment);
        AddHistory(incidentId, oldValue: attachment.FileName, newValue: null);

        // 1. The row first, so nobody can reach the file any more.
        await _db.SaveChangesAsync(cancellationToken);

        // 2. Then the file. If that fails it is only logged: a file with no row is unreachable, so harmless.
        await DeleteStoredFileQuietlyAsync(attachment.StoragePath);
    }

    // 404 when the caller can't see the incident (another company, or another Staff member's).
    private async Task<IncidentStatus> GetVisibleIncidentStatusAsync(int incidentId, CancellationToken cancellationToken)
    {
        return await _db.Incidents
            .VisibleTo(_currentUser)
            .Where(i => i.Id == incidentId)
            .Select(i => (IncidentStatus?)i.Status)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Incident {incidentId} was not found.");
    }

    private static void EnsureFilesMayChange(IncidentStatus status, string action)
    {
        if (!StatusesThatAllowChanges.Contains(status))
        {
            throw new ConflictException(
                $"Files can only be {action} while the incident is New, AwaitingApproval or SentBack. Its status is {status}.");
        }
    }

    // Who (the caller) and when (CreatedAt, set by AppDbContext) are filled in automatically.
    private void AddHistory(int incidentId, string? oldValue, string? newValue)
    {
        _db.IncidentHistory.Add(new IncidentHistory
        {
            IncidentId = incidentId,
            ChangedByUserId = _currentUser.UserId,
            ChangeSetId = Guid.NewGuid(),
            FieldName = HistoryFieldName,
            OldValue = oldValue,
            NewValue = newValue
        });
    }

    private async Task DeleteStoredFileQuietlyAsync(string storageKey)
    {
        try
        {
            // Not the request's token: the database change is already made (or failed), so finish the cleanup.
            await _fileStorage.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete stored file {StorageKey}.", storageKey);
        }
    }

    // The uploaded name is only kept for display and as the download name; it is never used on disk.
    // Some browsers send a full path ("C:\Users\...\photo.jpg"): keep only the last part.
    public static string CleanFileName(string? uploadedName)
    {
        var name = (uploadedName ?? string.Empty).Replace('\\', '/');
        name = name[(name.LastIndexOf('/') + 1)..];
        name = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();

        if (name.Length == 0)
        {
            return "file";
        }

        if (name.Length > MaxFileNameLength)
        {
            // Shorten the name but keep a normal-looking extension, so the download still opens with the right app.
            var extension = Path.GetExtension(name);
            if (extension.Length > 20)
            {
                extension = string.Empty;
            }

            name = name[..(MaxFileNameLength - extension.Length)] + extension;
        }

        return name;
    }

    public static string CleanContentType(string? contentType)
    {
        var value = contentType?.Trim();

        return string.IsNullOrEmpty(value) || value.Length > MaxContentTypeLength
            ? FallbackContentType
            : value;
    }

    // A deleted uploader still shows their name: soft delete keeps the row (by design).
    private static readonly Expression<Func<IncidentAttachment, AttachmentResponseDto>> ToDtoExpression = a => new AttachmentResponseDto
    {
        IncidentAttachmentId = a.Id,
        IncidentId = a.IncidentId,
        FileName = a.FileName,
        ContentType = a.ContentType,
        UploadedByUserId = a.UploadedByUserId,
        UploadedByUserName = a.UploadedByUser.FirstName + " " + a.UploadedByUser.LastName,
        UploadedAt = a.CreatedAt
    };
}
