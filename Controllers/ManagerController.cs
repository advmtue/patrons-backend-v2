using System;
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

        public ManagerController(IManagerService managerService)
        {
            // Save refs
            _managerService = managerService;
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
                return Ok(new { Venues = await _managerService.GetVenues(managerId) });
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ManagerController] Failed to get manager venues [mId: {managerId}]");
                Console.WriteLine(e.Message);

                return BadRequest();
            }
        }


        [HttpGet("venue/{venueId}/area/{areaId}/activeservice/start")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> StartService([FromRoute] string venueId, [FromRoute] string areaId)
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                return Ok(new { Service = await _managerService.StartService(managerId, venueId, areaId) });
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ManagerController] Failed to start service [vId: {venueId}]");
                Console.WriteLine(e.Message);

                return BadRequest();
            }
        }

        [HttpGet("venue/{venueId}/area/{areaId}/activeservice/stop")]
        [Authorize(Policy = "fullAccess")]
        public async Task<IActionResult> StopService([FromRoute] string venueId, [FromRoute] string areaId)
        {
            string managerId = HttpContext.User.Identity.Name;

            try
            {
                await _managerService.StopService(managerId, venueId, areaId);

                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ManagerController] Failed to stop service [vId: {venueId}]");
                Console.WriteLine(e.Message);

                return BadRequest();
            }

        }
    }
}