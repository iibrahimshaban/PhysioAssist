using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhysioAssist.Api.Modules.Scheduling.DTO.Guest;
using PhysioAssist.Api.Modules.Scheduling.Services.Interfaces;

namespace PhysioAssist.Api.Modules.Scheduling.Controllers
{
    /// <summary>
    /// Manages Guests — lightweight, first-visit records (name + phone only) used
    /// to book an appointment before a full Patient record exists. Guests are never
    /// automatically converted to Patients; that happens elsewhere, outside this module.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class GuestsController(IGuestService guestService) : ControllerBase
    {
        private readonly IGuestService _guestService = guestService;

        /// <summary>
        /// Creates a new Guest record from a full name and phone number.
        /// </summary>
        /// <response code="201">Guest created successfully.</response>
        /// <response code="400">Full name or phone number missing.</response>
        [HttpPost]
        [ProducesResponseType(typeof(GuestDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<GuestDto>> Create([FromBody] CreateGuestRequest request, CancellationToken cancellationToken)
        {
            var result = await _guestService.CreateAsync(request, cancellationToken);

            if (result.IsFailure)
                return result.ToProblem();

            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        /// <summary>
        /// Retrieves a single Guest by ID.
        /// </summary>
        /// <response code="200">Guest found and returned.</response>
        /// <response code="404">No guest exists with the given ID.</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(GuestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GuestDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _guestService.GetByIdAsync(id, cancellationToken);

            return result.IsFailure ? result.ToProblem() : Ok(result.Value);
        }

        /// <summary>
        /// Retrieves multiple Guests by ID in a single call — used by the schedule
        /// UI to resolve display names for guest appointments without one request
        /// per guest.
        /// </summary>
        /// <param name="ids">Guest IDs to look up, e.g. ?ids=guid1&amp;ids=guid2.</param>
        /// <response code="200">Returns the matching guests (missing IDs are simply omitted, not an error).</response>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<GuestDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<GuestDto>>> GetByIds([FromQuery] List<Guid> ids, CancellationToken cancellationToken)
        {
            var result = await _guestService.GetByIdsAsync(ids, cancellationToken);
            return Ok(result);
        }
    }
}
