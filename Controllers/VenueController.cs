using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using patrons_web_api.Services;
using System.Threading.Tasks;
using patrons_web_api.Database;

using patrons_web_api.Models.Transfer.Response;

namespace patrons_web_api.Controllers
{
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

        [HttpGet("{venueId}")]
        public async Task<IActionResult> GetVenueById([FromRoute] string venueId)
        {
            try
            {
                return Ok(await _venueService.GetVenueById(venueId));
            }
            catch (VenueNotFoundException)
            {
                _logger.LogInformation("Venue not found. [vId: {venueId}]", venueId);

                return BadRequest(APIError.VenueNotFound());

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to lookup venue. [vId: {venueId}]", venueId);

                return BadRequest(APIError.UnknownError());
            }
        }

        [HttpGet("byUrl/{venueUrl}")]
        public async Task<IActionResult> GetVenueByURLName([FromRoute] string venueUrl)
        {
            try
            {
                return Ok(await _venueService.GetVenueByURLName(venueUrl));
            }
            catch (VenueNotFoundException)
            {
                _logger.LogInformation("Venue not found. [vUrl: {venueUrl}]", venueUrl);

                return BadRequest(APIError.VenueNotFound());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to lookup venue. [vUrl: {venueUrl}]", venueUrl);

                return BadRequest(APIError.UnknownError());
            }
        }
    }
}