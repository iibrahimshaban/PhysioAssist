using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.SessionModule.Services;
using PhysioAssist.Api.Shared.Authorization;
using PhysioAssist.Api.Shared.Consts;
using PhysioAssist.Api.Shared.Dtos.Transcription;
using PhysioAssist.Api.Shared.Interfaces;
using PhysioAssist.Api.Shared.SystemPrompts;

namespace PhysioAssist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController(IAudioTranscriptionService transcriptionService, ISessionEmbeddingService sessionEmbeddingService) : ControllerBase
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];
    private readonly IAudioTranscriptionService _transcriptionService = transcriptionService;
    private readonly ISessionEmbeddingService _sessionEmbeddingService = sessionEmbeddingService;

    [HttpGet(Name = "GetWeatherForecast")]
    [HasPermission(Permissions.GetUsers)]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
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
    [HttpPost("generate/{sessionTranscriptionId:guid}")]
    public async Task<IActionResult> GenerateEmbeddings(
        Guid sessionTranscriptionId,
        [FromBody] ChunkPreviewRequest request,
        CancellationToken ct)
    {
        var result = await _sessionEmbeddingService.GenerateAndStoreEmbeddingAsync(
            sessionTranscriptionId, request.Text, ct);

        return result.IsSuccess
            ? Ok(new { Message = "Embeddings generated and stored." })
            : result.ToProblem(); // matches your existing Result -> ProblemDetails pattern
    }

    public record ChunkPreviewRequest(string Text);

}
