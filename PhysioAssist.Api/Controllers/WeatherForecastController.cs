using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Shared.Authorization;
using PhysioAssist.Api.Shared.Consts;
using PhysioAssist.Api.Shared.Dtos.Transcription;
using PhysioAssist.Api.Shared.Interfaces;
using PhysioAssist.Api.Shared.SystemPrompts;

namespace PhysioAssist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController(IAudioTranscriptionService transcriptionService) : ControllerBase
{
    private readonly IAudioTranscriptionService _transcriptionService = transcriptionService;

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


}
