using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

using patrons_web_api.Services;
using patrons_web_api.Models.Transfer.Request;
using patrons_web_api.Models.Transfer.Response;

namespace patrons_web_api.Controllers
{
    [Route("patron")]
    [ApiController]
    public class PatronController : ControllerBase
    {
        private PatronService _patronService;

        public PatronController(PatronService patronService)
        {
            // Save refs
            _patronService = patronService;
        }

        [HttpPost("check-in/{venueId}/gaming/{areaId}")]
        public async Task<IActionResult> GamingCheckInRequest([FromRoute] string venueId, [FromRoute] string areaId, [FromBody] GamingCheckInRequest checkIn)
        {
            Console.WriteLine($"[VenueController] Gaming check-in. [v: {venueId}, a: {areaId}]");

            try
            {
                await _patronService.GamingCheckIn(venueId, areaId, checkIn);
                return Ok();
            }
            catch
            {
                Console.WriteLine($"[PatronController] Gaming check-in failed.");

                return BadRequest();
            }
        }

        [HttpPost("check-in/{venueId}/dining/{areaId}")]
        public async Task<IActionResult> DiningCheckIn([FromRoute] string venueId, [FromRoute] string areaId, [FromBody] DiningCheckInRequest checkIn)
        {
            Console.WriteLine($"[PatronController] Dining check-in request. [v: {venueId}, a: {areaId}]");

            try
            {
                await _patronService.DiningCheckin(venueId, areaId, checkIn);
                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[PatronController] Dining check-in failed.");
                Console.WriteLine(e.Message);

                return BadRequest();
            }

        }
    }
}