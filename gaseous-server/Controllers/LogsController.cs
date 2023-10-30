using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class LogsController : Controller
    {
        [MapToApiVersion("1.0")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<Logging.LogItem> Logs(long? StartIndex, int PageNumber = 1, int PageSize = 100)
        {
            return Logging.GetLogs(StartIndex, PageNumber, PageSize);
        }
    }
}