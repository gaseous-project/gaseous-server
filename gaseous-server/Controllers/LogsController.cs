using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gaseous_tools;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class LogsController : Controller
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<Logging.LogItem> Logs()
        {
            return Logging.GetLogs();
        }
    }
}