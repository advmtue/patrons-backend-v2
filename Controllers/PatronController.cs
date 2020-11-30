using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

using patrons_web_api.Services;

namespace patrons_web_api.Controllers
{
    public class DiningPatron
    {
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
    }

    public class DiningCheckInRequest
    {
        [Required]
        [JsonPropertyName("people")]
        public List<DiningPatron> People { get; set; }

        [Required]
        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; }
    }

    public class GamingCheckInRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
    }

    [Route("patron")]
    [ApiController]
    public class PatronController : ControllerBase
    {
        private IPatronService _patronService;

        public PatronController(IPatronService patronService)
        {
            // Save refs
            _patronService = patronService;
        }

        [AllowAnonymous]
        [HttpPost("check-in/{venueId}/gaming/{areaId}")]
        public async Task<IActionResult> GamingCheckInRequest([FromRoute] string venueId, [FromRoute] string areaId, [FromBody] GamingCheckInRequest checkIn)
        {
            Console.WriteLine($"[VenueController] Gaming check-in. [v: {venueId}, a: {areaId}]");

            try
            {
                await _patronService.SubmitGamingCheckIn(venueId, areaId, checkIn);
                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[PatronController] Gaming check-in failed.");
                Console.WriteLine(e.Message);

                return BadRequest();
            }
        }

        [AllowAnonymous]
        [HttpPost("check-in/{venueId}/dining/{areaId}")]
        public async Task<IActionResult> DiningCheckIn([FromRoute] string venueId, [FromRoute] string areaId, [FromBody] DiningCheckInRequest checkIn)
        {
            Console.WriteLine($"[PatronController] Dining check-in request. [v: {venueId}, a: {areaId}]");

            try
            {
                await _patronService.SubmitDiningCheckIn(venueId, areaId, checkIn);
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