using System.IO.Pipes;
using System.Linq;
using System.Net;
using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using patrons_web_api.Services;
using patrons_web_api.Database;
using patrons_web_api.Models.Transfer.Request;

namespace patrons_web_api.Controllers
{
    [Route("manager")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        private ManagerService _managerService;

        public ManagerController(ManagerService managerService)
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
    }
}