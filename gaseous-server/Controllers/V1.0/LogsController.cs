using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("1.1")]
    [Authorize(Roles = "Admin")]
    public class LogsController : Controller
    {
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<Logging.LogItem> Logs(Logging.LogsViewModel model)
        {
            return Logging.GetLogs(model);
        }
    }
}