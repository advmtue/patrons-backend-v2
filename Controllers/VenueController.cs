using System;
using Microsoft.AspNetCore.Mvc;
using patrons_web_api.Services;
using System.Threading.Tasks;
using patrons_web_api.Database;

using patrons_web_api.Models.Transfer.Response;

namespace patrons_web_api.Controllers
{
    [Route("venue")]
    [ApiController]
    public class VenueController : ControllerBase
    {
        private VenueService _venueService;

        public VenueController(VenueService venueService)
        {
            // Save refs
            _venueService = venueService;
        }

        [HttpGet("{venueId}")]
        public async Task<IActionResult> getVenueById([FromRoute] string venueId)
        {
            try
            {
                return Ok(await _venueService.getVenueById(venueId));
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
    }
}