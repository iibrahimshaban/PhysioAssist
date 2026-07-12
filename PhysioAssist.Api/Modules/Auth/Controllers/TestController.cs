using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using PhysioAssist.Api.Modules.SessionModule.Services;
using PhysioAssist.Api.Shared.Dtos.Transcription;
using PhysioAssist.Api.Shared.SystemPrompts;

namespace PhysioAssist.Api.Modules.Auth.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController(IAudioTranscriptionService transcriptionService,
    ISessionEmbeddingService sessionEmbeddingService,
    ISessionChunkSearchService searchService,
    ChatCompletionAgent chatCompletionAgent) : ControllerBase
{
    private readonly IAudioTranscriptionService _transcriptionService = transcriptionService;
    private readonly ISessionEmbeddingService _sessionEmbeddingService = sessionEmbeddingService;
    private readonly ISessionChunkSearchService _searchService = searchService;
    private readonly ChatCompletionAgent _agent = chatCompletionAgent;

    [HttpPost("voice-text/initial-report")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> TranscribeInitialReport(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return Problem(
                title: "Transcription.EmptyFile",
                detail: "Uploaded file is empty.",
                statusCode: StatusCodes.Status400BadRequest);

        await using var stream = file.OpenReadStream();

        var result = await _transcriptionService.TranscribeAsync(
            new TranscriptionRequest(stream, file.FileName, null, TranscriptionPrompts.GeminiInitialReportTranscription),
            cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        //for different transcription model from refinement model >> whisper and llama-3.3-70b-versatile
        //return Ok(new { raw = result.Value.RawText, refined = result.Value.RefinedText });
        return Ok(new { raw = result.Value.RawText });
    }

    [HttpPost("voice-text/session")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> TranscribeSession(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return Problem(
                title: "Transcription.EmptyFile",
                detail: "Uploaded file is empty.",
                statusCode: StatusCodes.Status400BadRequest);

        await using var stream = file.OpenReadStream();

        // In production this comes from the patient record
        // var sessionHint = $"{TranscriptionPrompts.GeminiSessionTranscription}\nPatient diagnosis: {patient.Diagnosis}";

        var result = await _transcriptionService.TranscribeAsync(
            new TranscriptionRequest(stream, file.FileName, null, TranscriptionPrompts.GeminiSessionTranscription),
            cancellationToken);

        if (result.IsFailure)
            return result.ToProblem();

        //for different transcription model from refinement model >> whisper and llama-3.3-70b-versatile
        //return Ok(new { raw = result.Value.RawText, refined = result.Value.RefinedText });
        return Ok(new { raw = result.Value.RawText });
    }

    //[HttpPost("generate/{sessionTranscriptionId:guid}")]
    //public async Task<IActionResult> GenerateEmbeddings(
    //    Guid sessionTranscriptionId,
    //    [FromBody] ChunkPreviewRequest request,
    //    CancellationToken ct)
    //{
    //    var result = await _sessionEmbeddingService.GenerateAndStoreEmbeddingAsync(
    //        sessionTranscriptionId, request.Text, ct);
    //    return result.IsSuccess
    //        ? Ok(new { Message = "Embeddings generated and stored." })
    //        : result.ToProblem(); // matches your existing Result -> ProblemDetails pattern
    //}

    [HttpGet("ask")]
    public async Task<IActionResult> Ask([FromQuery] string question, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(question))
            return BadRequest("Question is required.");

        var history = new ChatHistory();
        history.AddUserMessage(question);

        var responses = new List<string>();

        await foreach (var item in _agent.InvokeAsync(history, cancellationToken: ct))
        {
            var message = item.Message;
            if (!string.IsNullOrWhiteSpace(message.Content))
                responses.Add(message.Content);
        }

        return Ok(new
        {
            Question = question,
            Answer = string.Join("\n", responses)
        });
    }
}
