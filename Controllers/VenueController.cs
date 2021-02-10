using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

using Patrons.CheckIn.API.Database;
using Patrons.CheckIn.API.Models.Transfer.Response;
using Patrons.CheckIn.API.Services;

/// <summary>
/// Controller for handling various anonymous venue access requests.
///
/// Examples of requests include:
///     * Get venue by ID (BSON ID)
///     * Get venue by UrlName (eg: centra)
/// </summary>
namespace Patrons.CheckIn.API.Controllers
{
    // Allow anonymous on all routes
    [ApiController]
    [AllowAnonymous]
    [Route("venue")]
    public class VenueController : ControllerBase
    {
        private IVenueService _venueService;
        private readonly ILogger<VenueController> _logger;

        public VenueController(IVenueService venueService, ILogger<VenueController> logger)
        {
            _venueService = venueService;
            _logger = logger;
        }

        /// <summary>
        /// User requests public venue information for a given BSON venue id.
        /// </summary>
        /// <param name="venueId">Target venue Bson ID.</param>
        /// <returns>Public venue information, or an error.</returns>
        [HttpGet("{venueId}")]
        public async Task<IActionResult> GetVenueById([FromRoute] string venueId)
        {
            try
            {
                // Pull venue information and return it.
                return Ok(await _venueService.GetVenueById(venueId));
            }
            catch (VenueNotFoundException)
            {
                // Error: Venue not found.
                _logger.LogInformation("Venue not found. [vId: {venueId}]", venueId);

                return BadRequest(APIError.VenueNotFound());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to lookup venue. [vId: {venueId}]", venueId);

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// User requests public venue information for a given venue specified by it's urlName.
        /// </summary>
        /// <param name="venueUrl">Target venue urlName</param>
        /// <returns>Public venue information, or an error.</returns>
        [HttpGet("byUrl/{venueUrl}")]
        public async Task<IActionResult> GetVenueByURLName([FromRoute] string venueUrl)
        {
            try
            {
                return Ok(await _venueService.GetVenueByURLName(venueUrl));
            }
            catch (VenueNotFoundException)
            {
                // Error: Venue not found.
                _logger.LogInformation("Venue not found. [vUrl: {venueUrl}]", venueUrl);

                return BadRequest(APIError.VenueNotFound());
            }
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Failed to lookup venue. [vUrl: {venueUrl}]", venueUrl);

                return BadRequest(APIError.UnknownError());
            }
        }
    }
}
