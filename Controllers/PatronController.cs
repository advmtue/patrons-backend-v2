using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using patrons_web_api.Services;
using patrons_web_api.Models.Transfer.Response;

/// <summary>
/// Controller used to handle patron check-in requests for venue areas.
/// 
/// Examples of such requests include:
///     * Patron group check-in to dining areas
///     * Patron single check-in to gaming areas
/// </summary>
namespace patrons_web_api.Controllers
{
    /// <summary>
    /// Dining patron for patron check-in.
    /// </summary>
    public class DiningPatron
    {
        /// <summary>
        /// Patron first name.
        /// </summary>
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// Patron phone number.
        /// </summary>
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
    }

    /// <summary>
    /// Dining group check-in request containing one or more patrons.
    /// </summary>
    public class DiningCheckInRequest
    {
        /// <summary>
        /// Dining patrons present in the check-in.
        /// </summary>
        [Required]
        [JsonPropertyName("people")]
        public List<DiningPatron> People { get; set; }

        /// <summary>
        /// Table number of the check-in.
        /// </summary>
        [Required]
        [JsonPropertyName("tableNumber")]
        public string TableNumber { get; set; }
    }

    /// <summary>
    /// Gaming check-in request containing a single patron's information.
    /// </summary>
    public class GamingCheckInRequest
    {
        /// <summary>
        /// Patron first name.
        /// </summary>
        [Required]
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// Patron family/last name.
        /// </summary>
        [Required]
        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        /// <summary>
        /// Patron phone number.
        /// </summary>
        [Required]
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
    }

    /// <summary>
    /// Controller handling /patron routes.
    /// </summary>
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

        /// <summary>
        /// Patron requests to check-in to a given venue + gaming area.
        /// </summary>
        /// <param name="venueId">Target venue ID.</param>
        /// <param name="areaId">Target area ID of target venue.</param>
        /// <param name="checkIn">Check-in information</param>
        /// <returns>Empty OK status on success, or an error.</returns>
        [AllowAnonymous]
        [HttpPost("check-in/{venueId}/gaming/{areaId}")]
        public async Task<IActionResult> GamingCheckInRequest([FromRoute] string venueId, [FromRoute] string areaId, [FromBody] GamingCheckInRequest checkIn)
        {
            try
            {
                // Check-in the patron to specified gaming area.
                await _patronService.SubmitGamingCheckIn(venueId, areaId, checkIn);

                // Log the check-in.
                _logger.LogInformation("Gaming check-in. [vId: {venueId}, aId: {areaId}]", venueId, areaId);

                // Return an empty OK status.
                return Ok();
            }
            // TODO Various not found exceptions.
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Gaming check-in failed. [vId: {venueId}, aId: {areaId}]", venueId, areaId);

                return BadRequest(APIError.UnknownError());
            }
        }

        /// <summary>
        /// Patron requests to check-in a group to a given venue + dining area.
        /// </summary>
        /// <param name="venueId">Target venue ID.</param>
        /// <param name="areaId">Target area ID, of given venue.</param>
        /// <param name="checkIn">Group check-in information.</param>
        /// <returns>Empty OK status on success, or an error.</returns>
        [AllowAnonymous]
        [HttpPost("check-in/{venueId}/dining/{areaId}")]
        public async Task<IActionResult> DiningCheckIn([FromRoute] string venueId, [FromRoute] string areaId, [FromBody] DiningCheckInRequest checkIn)
        {
            try
            {
                // Check-in the group to the dining area.
                await _patronService.SubmitDiningCheckIn(venueId, areaId, checkIn);

                // Log the check-in.
                _logger.LogInformation("Dining check-in. [vId: {venueId}, aId: {areaId}, tN: {tableNumber}, count: {patronCount}]", venueId, areaId, checkIn.TableNumber, checkIn.People.Count);

                // Return empty OK status.
                return Ok();
            }
            // TODO Various not found exceptions
            catch (Exception e)
            {
                // Error: Unknown error.
                _logger.LogError(e, "Dining check-in failed. [vId: {venueId}, aId: {areaId}, tN: {tableNumber}, count: {patronCount}]", venueId, areaId, checkIn.TableNumber, checkIn.People.Count);

                return BadRequest(APIError.UnknownError());
            }

        }
    }
}
