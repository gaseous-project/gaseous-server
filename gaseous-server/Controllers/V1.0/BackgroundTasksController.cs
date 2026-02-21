using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Net;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    public class BackgroundTasksController : Controller
    {
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin,Gamer,Player")]
        public List<ProcessQueue.QueueProcessor.QueueItem> GetQueue()
        {
            return ProcessQueue.QueueProcessor.QueueItems;
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{TaskType}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin")]
        public ActionResult<ProcessQueue.QueueProcessor.QueueItem> ForceRun(ProcessQueue.QueueItemType TaskType, Boolean ForceRun)
        {
            foreach (ProcessQueue.QueueProcessor.QueueItem qi in ProcessQueue.QueueProcessor.QueueItems)
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

        /// <summary>
        /// Updates the status of a background task identified by its correlation ID. The updated status information is provided in the request body as a QueueItem object. The method checks if the request is coming from the local machine for security reasons, then searches for the background task with the specified correlation ID and updates its current state and progress accordingly. If the task is found and updated, it returns an Ok response; if not found, it returns a NotFound response; if the request is not from the local machine, it returns a Forbid response.
        /// </summary>
        /// <param name="CorrelationId">The correlation ID of the background task.</param>
        /// <param name="UpdatedStatus">The updated status information for the background task.</param>
        /// <returns>Returns Ok if the task was found and updated, NotFound otherwise, Forbid if the request is not from the local machine.</returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPut]
        [Route("{CorrelationId}/")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult UpdateStatus(string CorrelationId, [FromBody] Dictionary<string, string> UpdatedStatus)
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            if (remoteIp == null || !IPAddress.IsLoopback(remoteIp))
            {
                return Forbid();
            }

            foreach (ProcessQueue.QueueProcessor.QueueItem qi in ProcessQueue.QueueProcessor.QueueItems)
            {
                if (qi.CorrelationId == CorrelationId)
                {
                    qi.CurrentState = UpdatedStatus.ContainsKey("CurrentState") ? UpdatedStatus["CurrentState"].ToString() : qi.CurrentState;
                    qi.CurrentStateProgress = UpdatedStatus.ContainsKey("CurrentStateProgress") ? UpdatedStatus["CurrentStateProgress"].ToString() : qi.CurrentStateProgress;
                    return Ok();
                }
            }

            return NotFound();
        }

        /// <summary>
        /// Submits a subtask to a background task identified by its correlation ID.
        /// </summary>
        /// <param name="CorrelationId">The correlation ID of the background task.</param>
        /// <param name="SubTask">The subtask to add to the background task.</param>
        /// <returns>Returns Ok if the task was found and subtask added, NotFound otherwise.</returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Route("{CorrelationId}/SubTask/")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitSubTask(string CorrelationId, [FromBody] Dictionary<string, object> SubTask)
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            if (remoteIp == null || !IPAddress.IsLoopback(remoteIp))
            {
                return Forbid();
            }

            ProcessQueue.QueueItemSubTasks? subTaskType = SubTask.ContainsKey("TaskType") ? Enum.Parse<ProcessQueue.QueueItemSubTasks>(SubTask["TaskType"].ToString()) : null;
            if (subTaskType == null)
            {
                return BadRequest("SubTask must contain TaskType");
            }

            string taskName = SubTask.ContainsKey("TaskName") ? SubTask["TaskName"].ToString() : string.Empty;
            object? settings = SubTask.ContainsKey("Settings") ? SubTask["Settings"] : null;
            bool removeWhenCompleted = SubTask.ContainsKey("RemoveWhenCompleted") ? bool.Parse(SubTask["RemoveWhenCompleted"].ToString()) : false;
            string correlationId = SubTask.ContainsKey("CorrelationId") ? SubTask["CorrelationId"].ToString() : "";

            foreach (ProcessQueue.QueueProcessor.QueueItem qi in ProcessQueue.QueueProcessor.QueueItems)
            {
                if (qi.CorrelationId == CorrelationId)
                {
                    await qi.AddSubTask(subTaskType.Value, taskName, settings, removeWhenCompleted, correlationId);
                    return Ok();
                }
            }

            return NotFound();
        }

        /// <summary>
        /// Updates a subtask within a background task identified by its correlation ID.
        /// </summary>
        /// <param name="CorrelationId">The correlation ID of the background task.</param>
        /// <param name="SubTaskId">The correlation ID of the subtask to update.</param>
        /// <param name="UpdatedSubTask">The updated subtask information containing the new state and progress.</param>
        /// <returns>Returns Ok if the task and subtask were found and updated, NotFound otherwise.</returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPut]
        [Route("{CorrelationId}/SubTask/{SubTaskId}/")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSubTask(string CorrelationId, string SubTaskId, [FromBody] Dictionary<string, string> UpdatedSubTask)
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            if (remoteIp == null || !IPAddress.IsLoopback(remoteIp))
            {
                return Forbid();
            }

            foreach (ProcessQueue.QueueProcessor.QueueItem qi in ProcessQueue.QueueProcessor.QueueItems)
            {
                if (qi.CorrelationId == CorrelationId)
                {
                    if (qi.SubTasks != null)
                    {
                        ProcessQueue.QueueProcessor.QueueItem.SubTask? subTask = qi.SubTasks.FirstOrDefault(st => st.CorrelationId == SubTaskId);
                        if (subTask != null)
                        {
                            subTask.CurrentState = UpdatedSubTask.ContainsKey("CurrentState") ? UpdatedSubTask["CurrentState"] : subTask.CurrentState;
                            subTask.CurrentStateProgress = UpdatedSubTask.ContainsKey("CurrentStateProgress") ? UpdatedSubTask["CurrentStateProgress"] : subTask.CurrentStateProgress;
                            return Ok();
                        }
                    }
                }
            }

            return NotFound();
        }
    }
}