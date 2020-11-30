using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        public VenueController(IVenueService venueService)
        {
            _venueService = venueService;
        }

        [HttpGet("{venueId}")]
        public async Task<IActionResult> GetVenueById([FromRoute] string venueId)
        {
            try
            {
                return Ok(await _venueService.GetVenueById(venueId));
            }
            catch (VenueNotFoundException e)
            {
                Console.WriteLine($"[VenueController] Failed to lookup venue. [vId: {venueId}]");
                Console.WriteLine(e.Message);

                var error = APIError.VenueNotFound();

                return BadRequest(error);

            }
            catch (Exception e)
            {
                Console.WriteLine($"[VenueController] Failed to lookup venue. [e: {e.Message}]");

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
            catch (VenueNotFoundException e)
            {
                Console.WriteLine($"[VenueController] Failed to lookup venue. [vUrl: {venueUrl}]");
                Console.WriteLine(e.Message);

                var error = APIError.VenueNotFound();

                return BadRequest(error);

            }
            catch (Exception e)
            {
                Console.WriteLine($"[VenueController] Failed to lookup venue. [e: {e.Message}]");

                return BadRequest(APIError.UnknownError());
            }

        }
    }
}