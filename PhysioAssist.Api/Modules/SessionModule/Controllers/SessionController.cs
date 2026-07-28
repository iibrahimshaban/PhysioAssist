using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.PatientModule.DTOs;
using PhysioAssist.Api.Modules.SessionModule.Contracts;
using PhysioAssist.Api.Modules.SessionModule.Services;

namespace PhysioAssist.Api.Modules.SessionModule.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController(ISessionService sessionService, IScheduleSlotQueryService _scheduleSlotQueryService) : ControllerBase
    {
        private readonly ISessionService _sessionService = sessionService;

        [HttpPost("start")]
        [HasPermission(Permissions.DailySessions)]
        public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request, CancellationToken cancellationToken)
        {
            var doctorId = Guid.Parse(User.GetUserId()!);

            var result = await _sessionService.StartOrResumeSessionAsync(doctorId, request, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("{id}")]
        [HasPermission(Permissions.ReadSession)]
        public async Task<IActionResult> GetSessionById(Guid id)
        {
            var result = await _sessionService.GetSessionByIdAsync(id);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("{id}/details")]
        [HasPermission(Permissions.ReadSession)]
        public async Task<IActionResult> GetSessionDetails(Guid id)
        {
            var result = await _sessionService.GetSessionDetailsAsync(id);

            return result.IsSuccess
                ? Ok(result.Value)
                : result.ToProblem();
        }

        [HttpPost("{id}/transcription/audio")]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.WriteSession)]
        public async Task<IActionResult> CreateAudioTranscription(
            Guid id,
            [FromForm] CreateAudioTranscriptionRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sessionService.CreateAudioTranscriptionAsync(
                id,
                request,
                cancellationToken
            );

            return result.IsSuccess
                ? Ok(result.Value)
                : result.ToProblem();
        }

        [HttpPost("{id:guid}/attachments")]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.WriteSession)]
        public async Task<IActionResult> UploadAttachments(
            Guid id,
            [FromForm] UploadSessionAttachmentRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sessionService.UploadAttachmentsAsync(
                id,
                request,
                cancellationToken);

            return result.IsSuccess
                ? NoContent()
                : result.ToProblem();
        }

        [HttpPut("{id:guid}/complete")]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.WriteSession)]
        public async Task<IActionResult> CompleteSession(Guid id, [FromForm] CompleteSessionRequest request, CancellationToken cancellationToken)
        {
            var result = await _sessionService.CompleteSessionAsync(id, request, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [HttpPut("{id:guid}/draft")]
        [Consumes("multipart/form-data")]
        [HasPermission(Permissions.WriteSession)]
        public async Task<IActionResult> SaveDraft(
            Guid id,
            [FromForm] SaveSessionDraftRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sessionService.SaveSessionDraftAsync(id, request, cancellationToken);

            return result.IsSuccess
                ? NoContent()
                : result.ToProblem();
        }

        [HttpDelete("attachments/{attachmentId:guid}")]
        [HasPermission(Permissions.WriteSession)]
        public async Task<IActionResult> DeleteAttachment(Guid attachmentId, CancellationToken cancellationToken)
        {
            var result = await _sessionService.DeleteAttachmentAsync(attachmentId, cancellationToken);

            return result.IsSuccess
                ? NoContent()
                : result.ToProblem();
        }

        [HttpPut("slots/{slotId:guid}/no-show")]
        [HasPermission(Permissions.DailySessions)]
        public async Task<IActionResult> MarkNoShow(Guid slotId, [FromBody] MarkNoShowRequest request, CancellationToken cancellationToken)
        {
            var result = await _scheduleSlotQueryService.MarkNoShowAsync(slotId, request.CountsAsUsed, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }
    }
}