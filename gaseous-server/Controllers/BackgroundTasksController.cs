using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class BackgroundTasksController : Controller
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<ProcessQueue.QueueItem> GetQueue()
        {
            return ProcessQueue.QueueItems;
        }

        [HttpGet]
        [Route("{TaskType}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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