using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

using patrons_web_api.Services;
using patrons_web_api.Database;
using patrons_web_api.Models.Transfer.Response;

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
    }
}