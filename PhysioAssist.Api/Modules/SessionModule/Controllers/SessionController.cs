using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.SessionModule.Contracts;
using PhysioAssist.Api.Modules.SessionModule.Services;

namespace PhysioAssist.Api.Modules.SessionModule.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController(ISessionService sessionService) : ControllerBase
    {
        private readonly ISessionService _sessionService = sessionService;

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            var result = await _sessionService.CreateSessionAsync(request);

            if (!result.IsSuccess)
                return result.ToProblem();

            return CreatedAtAction(
                nameof(GetSessionById),
                new { id = result.Value.Id },
                result.Value
            );
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSessionById(Guid id)
        {
            var result = await _sessionService.GetSessionByIdAsync(id);

            return result.IsSuccess
                ? Ok(result.Value)
                : result.ToProblem();
        }
        [HttpPut("{id}/start")]
        public async Task<IActionResult> StartSession(Guid id)
        {
            var result = await _sessionService.StartSessionAsync(id);

            return result.IsSuccess
                ? NoContent()
                : result.ToProblem();
        }
        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetSessionDetails(Guid id)
        {
            var result = await _sessionService.GetSessionDetailsAsync(id);

            return result.IsSuccess
                ? Ok(result.Value)
                : result.ToProblem();
        }


        [HttpPost("{id}/transcription/audio")]
        [Consumes("multipart/form-data")]
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
        public async Task<IActionResult> CompleteSession(
    Guid id,
    [FromForm] CompleteSessionRequest request,
    CancellationToken cancellationToken)
        {
            var result = await _sessionService.CompleteSessionAsync(
                id,
                request,
                cancellationToken);

            return result.IsSuccess
                ? NoContent()
                : result.ToProblem();
        }




        [HttpPut("{id:guid}/draft")]
        [Consumes("multipart/form-data")]
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
        public async Task<IActionResult> DeleteAttachment(
    Guid attachmentId,
    CancellationToken cancellationToken)
        {
            var result = await _sessionService.DeleteAttachmentAsync(attachmentId, cancellationToken);

            return result.IsSuccess
                ? NoContent()
                : result.ToProblem();
        }
    }
}
