using System;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using patrons_web_api.Services;
using patrons_web_api.Database;

namespace patrons_web_api.Controllers
{
    public class ManagerResponse
    {
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("isPasswordReset")]
        public bool IsPasswordReset { get; set; }
    }

    public class DiningPatronUpdateRequest
    {
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
    }

    public class GamingPatronUpdateRequest
    {
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
    }

    public class ManagerLoginRequest
    {
        [Required]
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }

    public class UpdatePasswordRequest
    {
        [JsonPropertyName("newPassword")]
        public string NewPassword { get; set; }
    }

    [Route("manager")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        private IManagerService _managerService;
        private readonly ILogger _logger;

        public ManagerController(IManagerService managerService, ILogger<ManagerController> logger)
        {
            // Save refs
            _managerService = managerService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] ManagerLoginRequest login)
        {
            try
            {
                return Ok(await _managerService.Login(login, "unknown"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ManagerController] Failed to perform login. [un: {login.Username}]");
                Console.WriteLine(e.Message);

                return BadRequest();
            }
        }

        [HttpGet("@self")]
        [Authorize(Policy = "authenticated")]
        public async Task<IActionResult> GetSelf()
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                return Ok(await _managerService.GetSelf(managerId));
            }
            catch (Exception)
            {
                Console.WriteLine("faile");

                return BadRequest();
            }
        }

        [HttpPost("@updatepassword")]
        [Authorize(Policy = "registrationAccess")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest passwordInfo)
        {
            try
            {
                await _managerService.UpdatePassword(HttpContext.User.Identity.Name, passwordInfo.NewPassword);
                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ManagerController] Failed to update manager password [mId: {HttpContext.User.Identity.Name}]");
                Console.WriteLine(e.Message);

                return BadRequest();
            }
        }

        [HttpGet("@venues")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> GetVenues()
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                var venues = await _managerService.GetVenues(managerId);
                return Ok(new { Venues = venues });
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ManagerController] Failed to get manager venues [mId: {managerId}]");
                Console.WriteLine(e.Message);

                return BadRequest();
            }
        }


        [HttpGet("dining/venue/{venueId}/area/{areaId}/service/start")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> StartDiningService([FromRoute] string venueId, [FromRoute] string areaId)
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                return Ok(new { Service = await _managerService.StartDiningService(managerId, venueId, areaId) });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to start dining service [vId: {venueId}]", venueId);

                return BadRequest();
            }
        }

        [HttpGet("dining/venue/{venueId}/area/{areaId}/service/stop")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> StopDiningService([FromRoute] string venueId, [FromRoute] string areaId)
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                await _managerService.StopDiningService(managerId, venueId, areaId);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to stop dining service [vId: {venueId}]", venueId);

                return BadRequest();
            }
        }

        [HttpGet("gaming/venue/{venueId}/area/{areaId}/service/start")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> StartGamingService([FromRoute] string venueId, [FromRoute] string areaId)
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                return Ok(new { Service = await _managerService.StartGamingService(managerId, venueId, areaId) });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to start gaming service [vId: {venueId}]", venueId);

                return BadRequest();
            }
        }

        [HttpGet("gaming/venue/{venueId}/area/{areaId}/service/stop")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> StopGamingService([FromRoute] string venueId, [FromRoute] string areaId)
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                await _managerService.StopGamingService(managerId, venueId, areaId);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to stop gaming service [vId: {venueId}]", venueId);

                return BadRequest();
            }
        }

        [HttpDelete("dining/service/{serviceId}/table/{tableId}/check-in/{checkInId}/patron/{patronId}")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> DeleteDiningPatron(
            [FromRoute] string serviceId,
            [FromRoute] string tableId,
            [FromRoute] string checkInId,
            [FromRoute] string patronId
        )
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                await _managerService.DeleteDiningPatron(managerId, serviceId, tableId, checkInId, patronId);
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to delete patron.");

                return BadRequest();
            }
        }

        [HttpPatch("dining/service/{serviceId}/table/{tableId}/check-in/{checkInId}/patron/{patronId}")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> UpdateDiningPatron(
            [FromRoute] string serviceId,
            [FromRoute] string tableId,
            [FromRoute] string checkInId,
            [FromRoute] string patronId,
            [FromBody] DiningPatronUpdateRequest update
        )
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                await _managerService.UpdateDiningPatron(managerId, serviceId, tableId, checkInId, patronId, update);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to update dining patron");

                return BadRequest();
            }
        }

        [HttpGet("dining/service/{serviceId}/table/{tableId}/check-in/{checkInId}/move/{tableNumber}")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> MoveDiningGroup(
            [FromRoute] string serviceId,
            [FromRoute] string tableId,
            [FromRoute] string checkInId,
            [FromRoute] string tableNumber
        )
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                return Ok(new { TableId = await _managerService.MoveDiningGroup(managerId, serviceId, tableId, checkInId, tableNumber) });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to move dining check-in");

                return BadRequest();
            }
        }

        [HttpGet("dining/service/{serviceId}/table/{tableId}/move/{tableNumber}")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> MoveDiningTable(
            [FromRoute] string serviceId,
            [FromRoute] string tableId,
            [FromRoute] string tableNumber
        )
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                return Ok(new { TableId = await _managerService.MoveDiningTable(managerId, serviceId, tableId, tableNumber) });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to move dining table");

                return BadRequest();
            }
        }

        [HttpGet("dining/service/{serviceId}/table/{tableId}/close")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> CloseDiningTable([FromRoute] string serviceId, [FromRoute] string tableId)
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                await _managerService.CloseDiningTable(managerId, serviceId, tableId);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to close dining table");

                return BadRequest();
            }
        }

        [HttpDelete("gaming/service/{serviceId}/patron/{patronId}")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> DeleteGamingPatron([FromRoute] string serviceId, [FromRoute] string patronId)
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                await _managerService.DeleteGamingPatron(managerId, serviceId, patronId);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to delete gaming patron");

                return BadRequest();
            }
        }

        [HttpPatch("gaming/service/{serviceId}/patron/{patronId}")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> UpdateGamingPtron(
            [FromRoute] string serviceId,
            [FromRoute] string patronId,
            [FromBody] GamingPatronUpdateRequest update
        )
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                await _managerService.UpdateGamingPatron(managerId, serviceId, patronId, update);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to update gaming patron");

                return BadRequest();
            }
        }

        [HttpGet("gaming/service/{serviceId}/patron/{patronId}/checkout")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> CheckOutGamingPatron([FromRoute] string serviceId, [FromRoute] string patronId)
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                await _managerService.CheckOutGamingPatron(managerId, serviceId, patronId);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to checkout gaming patron");

                return BadRequest();
            }
        }
    }
}