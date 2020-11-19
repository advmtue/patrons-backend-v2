using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

using patrons_web_api.Services;

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

        [HttpGet("test")]
        public async Task<IActionResult> GetTest()
        {
            return Ok(_managerService.getHelloWorldFromDatabase());
        }
    }
}