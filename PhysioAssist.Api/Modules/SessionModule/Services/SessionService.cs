using PhysioAssist.Api.Modules.SessionModule.Contracts;
using PhysioAssist.Api.Modules.SessionModule.Entities;
using PhysioAssist.Api.Modules.SessionModule.Errors;
using PhysioAssist.Api.Persistence;
using PhysioAssist.Api.Shared.Enums;
using PhysioAssist.Api.Shared.Dtos.Transcription;
using PhysioAssist.Api.Shared.Interfaces;

namespace PhysioAssist.Api.Modules.SessionModule.Services;

public class SessionService(
    ApplicationDbContext context,
    IAudioTranscriptionService audioTranscriptionService,
    ISessionEmbeddingService sessionEmbeddingService ,
    IMediaStorageService mediaStorageService
) : ISessionService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAudioTranscriptionService _audioTranscriptionService = audioTranscriptionService;
    private readonly ISessionEmbeddingService _sessionEmbeddingService = sessionEmbeddingService;
    private readonly IMediaStorageService _mediaStorageService = mediaStorageService;
    public async Task<Result<SessionResponse>> CreateSessionAsync(CreateSessionRequest request)
    {

        var session = new Session
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            ScheduleSlotId = request.ScheduleSlotId
        };

        await _context.Sessions.AddAsync(session);


        await _context.SaveChangesAsync();

        var response = new SessionResponse
        {
            Id = session.Id,
            PatientId = session.PatientId,
            DoctorId = session.DoctorId,
            ScheduleSlotId = session.ScheduleSlotId,
            Summary = session.Summary,
            Status = session.Status
        };

        return Result.Success(response);
    }
    public async Task<Result> StartSessionAsync(Guid id)
    {
        var session = await _context.Sessions.FindAsync(id);

        if (session is null)
            return Result.Failure(SessionErrors.SessionNotFound);

        if (session.Status != SessionStatus.Scheduled)
            return Result.Failure(SessionErrors.InvalidSessionStatus);

        session.Status = SessionStatus.InProgress;

        await _context.SaveChangesAsync();

        return Result.Success();
    }
    public async Task<Result<SessionResponse>> GetSessionByIdAsync(Guid id)
    {
        var session = await _context.Sessions.FindAsync(id);

        if (session is null)
            return Result.Failure<SessionResponse>(SessionErrors.SessionNotFound);

        var response = new SessionResponse
        {
            Id = session.Id,
            PatientId = session.PatientId,
            DoctorId = session.DoctorId,
            ScheduleSlotId = session.ScheduleSlotId,
            Summary = session.Summary,
            Status = session.Status
        };

        return Result.Success(response);
    }
    public async Task<Result<SessionDetailsResponse>> GetSessionDetailsAsync(Guid id)
    {
        var session = await _context.Sessions
            .Where(s => s.Id == id)
            .Select(s => new SessionDetailsResponse
            {
                Id = s.Id,

                PatientName = _context.Patients
                    .Where(p => p.Id == s.PatientId)
                    .Select(p => p.FullName)
                    .FirstOrDefault() ?? "Unknown Patient",

                SlotStart = _context.ScheduleSlots
                    .Where(slot => slot.Id == s.ScheduleSlotId)
                    .Select(slot => (DateTimeOffset?)slot.SlotStart)
                    .FirstOrDefault(),

                SlotEnd = _context.ScheduleSlots
                    .Where(slot => slot.Id == s.ScheduleSlotId)
                    .Select(slot => (DateTimeOffset?)slot.SlotEnd)
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
                    .ToList()
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

        var session = await _context.Sessions.FindAsync([sessionId], cancellationToken);

        if (session is null)
            return Result.Failure<string>(SessionErrors.SessionNotFound);

        await using var audioStream = request.AudioFile.OpenReadStream();

        var transcriptionRequest = new TranscriptionRequest(
            AudioStream: audioStream,
            FileName: request.AudioFile.FileName,
            LanguageHint: request.LanguageHint,
            Prompt: request.Prompt
        );

        var transcriptionResult = await _audioTranscriptionService.TranscribeAsync(
            transcriptionRequest,
            cancellationToken
        );

        if (transcriptionResult.IsFailure)
            return Result.Failure<string>(transcriptionResult.Error);

        var transcription = await _context.SessionTranscriptions
            .FirstOrDefaultAsync(t => t.SessionId == sessionId, cancellationToken);

        if (transcription is null)
        {
            transcription = new SessionTranscription
            {
                SessionId = sessionId
            };

            await _context.SessionTranscriptions.AddAsync(transcription, cancellationToken);
        }

        transcription.RawTranscript = transcriptionResult.Value.RawText;
        transcription.EditedTranscript = transcriptionResult.Value.RefinedText;
        transcription.AudioFileUrl = string.Empty;
        transcription.Language = transcriptionResult.Value.DetectedLanguage;
        transcription.DurationSeconds = (int)(transcriptionResult.Value.DurationSeconds ?? 0);
        transcription.Status = TranscriptionStatus.Completed;

        await _context.SaveChangesAsync(cancellationToken);

        var textForEmbedding = string.IsNullOrWhiteSpace(transcription.EditedTranscript)
            ? transcription.RawTranscript
            : transcription.EditedTranscript;

        var embeddingResult = await _sessionEmbeddingService.GenerateAndStoreEmbeddingAsync(
            transcription.Id,
            textForEmbedding,
            cancellationToken
        );

        if (embeddingResult.IsFailure)
        {
            Console.WriteLine(embeddingResult.Error.Description);
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

        session.Status = SessionStatus.Completed;

        // TODO: Generate summary later when summary service is ready.
        // session.Summary = generatedSummary;

        await _context.SaveChangesAsync(cancellationToken);

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

}