using PhysioAssist.Api.Modules.SessionModule.Contracts;
using PhysioAssist.Api.Modules.SessionModule.Entities;
using PhysioAssist.Api.Modules.SessionModule.Errors;
using PhysioAssist.Api.Shared.Dtos.Transcription;

namespace PhysioAssist.Api.Modules.SessionModule.Services;

public class SessionService(
    ApplicationDbContext _context,
    IAudioTranscriptionService _audioTranscriptionService,
    ISessionEmbeddingService _sessionEmbeddingService ,
    IMediaStorageService _mediaStorageService,
    IScheduleSlotQueryService _scheduleSlotQueryService,
    IInitialReportQueryService _initialReportQueryService
    ) : ISessionService
{

    public async Task<Result<SessionResponse>> StartOrResumeSessionAsync(
        Guid doctorId, StartSessionRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Sessions
            .FirstOrDefaultAsync(s => s.ScheduleSlotId == request.ScheduleSlotId, cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == SessionStatus.Scheduled)
            {
                existing.Status = SessionStatus.InProgress;
                await _context.SaveChangesAsync(cancellationToken);
            }
            return Result.Success(ToResponse(existing));
        }

        var treatmentPlan = await ResolveTreatmentPlanAsync(request.PatientId, request.ScheduleSlotId, cancellationToken);

        var session = new Session
        {
            PatientId = request.PatientId,
            DoctorId = doctorId,
            ScheduleSlotId = request.ScheduleSlotId,
            Status = SessionStatus.InProgress,
            TreatmentPlan = treatmentPlan
        };

        await _context.Sessions.AddAsync(session, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(ToResponse(session));
    }
    public async Task<Result<SessionResponse>> GetSessionByIdAsync(Guid id)
    {
        var session = await _context.Sessions.FindAsync(id);

        if (session is null)
            return Result.Failure<SessionResponse>(SessionErrors.SessionNotFound);

        return Result.Success(ToResponse(session));
    }
    public async Task<Result<SessionDetailsResponse>> GetSessionDetailsAsync(Guid id)
    {
        var session = await _context.Sessions
            .Where(s => s.Id == id)
            .Select(s => new SessionDetailsResponse
            {
                Id = s.Id,
                PatientId = s.PatientId.ToString(),
                PatientName = _context.Patients
                    .Where(p => p.Id == s.PatientId)
                    .Select(p => p.FullName)
                    .FirstOrDefault() ?? "Unknown Patient",

                SlotStart = _context.ScheduleSlots
                     .Where(slot => slot.Id == s.ScheduleSlotId)
                     .Select(slot => (DateTime?)slot.SlotStart.DateTime) // Access .DateTime here
                     .FirstOrDefault(),

                                SlotEnd = _context.ScheduleSlots
                     .Where(slot => slot.Id == s.ScheduleSlotId)
                     .Select(slot => (DateTime?)slot.SlotEnd.DateTime)   // Access .DateTime here
                     .FirstOrDefault(),

                DurationInMinutes = _context.ScheduleSlots
                    .Where(slot => slot.Id == s.ScheduleSlotId)
                    .Select(slot => EF.Functions.DateDiffMinute(slot.SlotStart, slot.SlotEnd))
                    .FirstOrDefault(),

                Status = s.Status,

                EditedTranscript = s.Transcription == null
                    ? null
                    : s.Transcription.EditedTranscript,

                Attachments = s.Attachments
                    .Select(a => new SessionAttachmentResponse
                    {
                        Id = a.Id,
                        FileUrl = a.FileUrl,
                        FileName = a.FileName,
                        FileType = a.FileType
                    })
                    .ToList(),
                AudioFileUrl = s.Transcription == null
                ? null
                : s.Transcription.AudioFileUrl,
                TreatmentPlan = s.TreatmentPlan
            })
            .FirstOrDefaultAsync();

        if (session is null)
            return Result.Failure<SessionDetailsResponse>(SessionErrors.SessionNotFound);

        return Result.Success(session);
    }
    public async Task<Result<string>> CreateAudioTranscriptionAsync(
      Guid sessionId,
      CreateAudioTranscriptionRequest request,
      CancellationToken cancellationToken = default)
    {
        if (request.AudioFile is null || request.AudioFile.Length == 0)
            return Result.Failure<string>(SessionErrors.EmptyAudioFile);

        var session = await _context.Sessions
            .FindAsync([sessionId], cancellationToken);

        if (session is null)
            return Result.Failure<string>(SessionErrors.SessionNotFound);

        await using var audioStream = request.AudioFile.OpenReadStream();

        var transcriptionRequest = new TranscriptionRequest(
            AudioStream: audioStream,
            FileName: request.AudioFile.FileName,
            LanguageHint: request.LanguageHint,
            Prompt: request.Prompt
        );

        var transcriptionResult =
            await _audioTranscriptionService.TranscribeAsync(
                transcriptionRequest,
                cancellationToken
            );

        if (transcriptionResult.IsFailure)
            return Result.Failure<string>(transcriptionResult.Error);

        string audioUrl;

        try
        {
            audioUrl = await _mediaStorageService.UploadAudioAsync(
                request.AudioFile,
                "session-audio",
                $"{sessionId}/{Guid.CreateVersion7()}"
            );
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(
                new Error(
                    "Session.AudioUploadFailed",
                    $"Failed to upload the session audio. {ex.Message}",
                    StatusCodes.Status500InternalServerError
                )
            );
        }

        var transcription = await _context.SessionTranscriptions
            .FirstOrDefaultAsync(
                t => t.SessionId == sessionId,
                cancellationToken
            );

        if (transcription is null)
        {
            transcription = new SessionTranscription
            {
                SessionId = sessionId
            };

            await _context.SessionTranscriptions.AddAsync(
                transcription,
                cancellationToken
            );
        }

        transcription.RawTranscript =
            transcriptionResult.Value.RawText;

        transcription.EditedTranscript =
            transcriptionResult.Value.RefinedText;

        transcription.AudioFileUrl = audioUrl;

        transcription.Language =
            transcriptionResult.Value.DetectedLanguage;

        transcription.DurationSeconds =
            (int)(transcriptionResult.Value.DurationSeconds ?? 0);

        transcription.Status =
            TranscriptionStatus.Completed;

        await _context.SaveChangesAsync(cancellationToken);

        var textForEmbedding =
            string.IsNullOrWhiteSpace(transcription.EditedTranscript)
                ? transcription.RawTranscript
                : transcription.EditedTranscript;

        var embeddingResult =
            await _sessionEmbeddingService.GenerateAndStoreEmbeddingAsync(
                transcription.Id,
                textForEmbedding,
                cancellationToken
            );

        if (embeddingResult.IsFailure)
        {
            Console.WriteLine(
                embeddingResult.Error.Description
            );
        }

        return Result.Success(textForEmbedding);
    }
    public async Task<Result> UploadAttachmentsAsync(
      Guid sessionId,
      UploadSessionAttachmentRequest request,
      CancellationToken cancellationToken = default)
    {
        if (request.Files.Count == 0)
            return Result.Failure(SessionErrors.EmptyAttachmentFile);

        var session = await _context.Sessions
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        if (session is null)
            return Result.Failure(SessionErrors.SessionNotFound);

        foreach (var file in request.Files)
        {
            var fileUrl = await _mediaStorageService.UploadImageAsync(
                file,
                "session-attachments",
                $"{sessionId}/{Guid.CreateVersion7()}");

            await _context.SessionAttachments.AddAsync(new SessionAttachment
            {
                SessionId = sessionId,
                FileUrl = fileUrl,
                FileName = file.FileName,
                FileType = file.ContentType
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CompleteSessionAsync(
        Guid sessionId,
        CompleteSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .Include(s => s.Transcription)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
            return Result.Failure(SessionErrors.SessionNotFound);

        if (request.TreatmentPlanUpdated is not null)
            session.TreatmentPlan = request.TreatmentPlanUpdated;

        if (session.Transcription is null)
        {
            session.Transcription = new SessionTranscription
            {
                SessionId = sessionId,
                RawTranscript = request.EditedTranscript,
                EditedTranscript = request.EditedTranscript,
                AudioFileUrl = string.Empty,
                Status = TranscriptionStatus.Completed
            };
        }
        else
        {
            session.Transcription.EditedTranscript = request.EditedTranscript;
            session.Transcription.Status = TranscriptionStatus.Completed;
        }

        foreach (var file in request.Files)
        {
            if (file.Length == 0)
                continue;

            var fileUrl = await _mediaStorageService.UploadImageAsync(
                file, "session-attachments", $"{sessionId}/{Guid.CreateVersion7()}");

            await _context.SessionAttachments.AddAsync(new SessionAttachment
            {
                SessionId = sessionId,
                FileUrl = fileUrl,
                FileName = file.FileName,
                FileType = file.ContentType
            }, cancellationToken);
        }

        session.Status = SessionStatus.Completed;

        await _context.SaveChangesAsync(cancellationToken);

        if (session.ScheduleSlotId.HasValue)
        {
            var slotResult = await _scheduleSlotQueryService.MarkCompletedAsync(
                session.ScheduleSlotId.Value, cancellationToken);

            if (slotResult.IsFailure)
                return Result.Failure(slotResult.Error);
        }

        return Result.Success();
    }


    public async Task<Result> SaveSessionDraftAsync(
        Guid sessionId,
        SaveSessionDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .Include(s => s.Transcription)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
            return Result.Failure(SessionErrors.SessionNotFound);

        if (request.TreatmentPlanUpdated is not null)
            session.TreatmentPlan = request.TreatmentPlanUpdated;

        if (session.Transcription is null)
        {
            session.Transcription = new SessionTranscription
            {
                SessionId = sessionId,
                RawTranscript = request.EditedTranscript,
                EditedTranscript = request.EditedTranscript,
                AudioFileUrl = string.Empty,
                Status = TranscriptionStatus.Completed
            };
        }
        else
        {
            session.Transcription.EditedTranscript = request.EditedTranscript;
            session.Transcription.Status = TranscriptionStatus.Completed;
        }

        foreach (var file in request.Files)
        {
            if (file.Length == 0)
                continue;

            var fileUrl = await _mediaStorageService.UploadImageAsync(
                file,
                "session-attachments",
                $"{sessionId}/{Guid.CreateVersion7()}");

            await _context.SessionAttachments.AddAsync(new SessionAttachment
            {
                SessionId = sessionId,
                FileUrl = fileUrl,
                FileName = file.FileName,
                FileType = file.ContentType
            }, cancellationToken);
        }

        session.Status = SessionStatus.InProgress;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }


    public async Task<Result> DeleteAttachmentAsync(
    Guid attachmentId,
    CancellationToken cancellationToken = default)
    {
        var attachment = await _context.SessionAttachments
            .FirstOrDefaultAsync(x => x.Id == attachmentId, cancellationToken);

        if (attachment is null)
            return Result.Failure(SessionErrors.AttachmentNotFound);

        await _mediaStorageService.DeleteImageByUrlAsync(attachment.FileUrl);

        _context.SessionAttachments.Remove(attachment);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static SessionResponse ToResponse(Session session) => new()
    {
        Id = session.Id,
        PatientId = session.PatientId,
        DoctorId = session.DoctorId,
        ScheduleSlotId = session.ScheduleSlotId,
        Summary = session.SummaryText,
        Status = session.Status
    };
    private async Task<string?> ResolveTreatmentPlanAsync(Guid patientId, Guid scheduleSlotId, CancellationToken cancellationToken)
    {
        var priorSlotsResult = await _scheduleSlotQueryService.GetPriorSlotIdsInPackageAsync(scheduleSlotId, cancellationToken);

        if (priorSlotsResult.IsSuccess && priorSlotsResult.Value.Count > 0)
        {
            var priorSlotIds = priorSlotsResult.Value; 

            var candidates = await _context.Sessions
                .Where(s => s.ScheduleSlotId.HasValue
                            && priorSlotIds.Contains(s.ScheduleSlotId.Value)
                            && s.TreatmentPlan != null)
                .Select(s => new { s.ScheduleSlotId, s.TreatmentPlan })
                .ToListAsync(cancellationToken);


            var mostRecent = candidates
                .OrderBy(c => priorSlotIds.ToList().IndexOf(c.ScheduleSlotId!.Value))
                .FirstOrDefault();

            if (mostRecent is not null)
                return mostRecent.TreatmentPlan;
        }

        var initialReportResult = await _initialReportQueryService.GetTreatmentPlanTextAsync(patientId, cancellationToken);
        return initialReportResult.IsSuccess ? initialReportResult.Value : null;
    }

}