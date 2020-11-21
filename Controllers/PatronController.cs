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
    }
}