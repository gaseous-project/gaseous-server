using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize(Roles = "Admin,Gamer,Player")]
    public class BackgroundTasksController : Controller
    {
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<ProcessQueue.QueueItem> GetQueue()
        {
            return ProcessQueue.QueueItems;
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{TaskType}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin")]
        public ActionResult<ProcessQueue.QueueItem> ForceRun(ProcessQueue.QueueItemType TaskType, Boolean ForceRun)
        {
            foreach (ProcessQueue.QueueItem qi in ProcessQueue.QueueItems)
            {
                if (qi.AllowManualStart == true)
                {
                    if (TaskType == qi.ItemType)
                    {
                        if (ForceRun == true)
                        {
                            qi.ForceExecute();
                        }
                        return qi;
                    }
                }
            }

            return NotFound();
        }
    }
}