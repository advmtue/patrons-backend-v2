using System.Threading;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

using patrons_web_api.Services;
using patrons_web_api.Models.Transfer.Response;

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
        private readonly ILogger<PatronController> _logger;

        public PatronController(IPatronService patronService, ILogger<PatronController> logger)
        {
            _patronService = patronService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("check-in/{venueId}/gaming/{areaId}")]
        public async Task<IActionResult> GamingCheckInRequest([FromRoute] string venueId, [FromRoute] string areaId, [FromBody] GamingCheckInRequest checkIn)
        {
            Console.WriteLine($"[VenueController] Gaming check-in. [v: {venueId}, a: {areaId}]");

            try
            {
                await _patronService.SubmitGamingCheckIn(venueId, areaId, checkIn);
                _logger.LogInformation("Gaming check-in. [vId: {venueId}, aId: {areaId}]", venueId, areaId);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Gaming check-in failed. [vId: {venueId}, aId: {areaId}]", venueId, areaId);

                return BadRequest(APIError.UnknownError());
            }
        }

        [AllowAnonymous]
        [HttpPost("check-in/{venueId}/dining/{areaId}")]
        public async Task<IActionResult> DiningCheckIn([FromRoute] string venueId, [FromRoute] string areaId, [FromBody] DiningCheckInRequest checkIn)
        {

            try
            {
                await _patronService.SubmitDiningCheckIn(venueId, areaId, checkIn);
                _logger.LogInformation("Dining check-in. [vId: {venueId}, aId: {areaId}, tN: {tableNumber}, count: {patronCount}", venueId, areaId, checkIn.TableNumber, checkIn.People.Count);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Dining check-in failed. [vId: {venueId}, aId: {areaId}]", venueId, areaId);

                return BadRequest(APIError.UnknownError());
            }

        }
    }
}