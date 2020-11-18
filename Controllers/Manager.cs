using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;


namespace patrons_web_api.Controllers
{
    [Route("manager")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        [HttpGet("test")]
        public async Task<IActionResult> GetTest()
        {
            return Ok("hello world");
        }
    }
}