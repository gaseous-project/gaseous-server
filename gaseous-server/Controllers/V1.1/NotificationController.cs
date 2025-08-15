using Asp.Versioning;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize(Roles = "Admin,Gamer,Player")]
    public class NotificationController : Controller
    {
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotifications()
        {
            Dictionary<string, object> notifications = new Dictionary<string, object>();

            // get import queue status
            Dictionary<Models.ImportStateItem.ImportState, object> importQueueStatus = new Dictionary<Models.ImportStateItem.ImportState, object>();
            foreach (var item in ImportGame.ImportStates)
            {
                switch (item.State)
                {
                    case Models.ImportStateItem.ImportState.Pending:
                    case Models.ImportStateItem.ImportState.Queued:
                        if (importQueueStatus.ContainsKey(Models.ImportStateItem.ImportState.Pending))
                        {
                            importQueueStatus[Models.ImportStateItem.ImportState.Pending] = (int)importQueueStatus[Models.ImportStateItem.ImportState.Pending] + 1;
                        }
                        else
                        {
                            importQueueStatus.Add(Models.ImportStateItem.ImportState.Pending, 1);
                        }
                        break;

                    case Models.ImportStateItem.ImportState.Processing:
                    case Models.ImportStateItem.ImportState.Completed:
                        Dictionary<string, object> processingItem = new Dictionary<string, object>
                        {
                            { "sessionid", item.SessionId },
                            { "state", item.State.ToString() },
                            { "filename", Path.GetFileName(item.FileName) },
                            { "type", item.Type },
                            { "created", item.Created },
                            { "lastupdated", item.LastUpdated },
                            { "expiration", item.LastUpdated.AddMinutes(70) },
                            { "method", item.Method.ToString() }
                        };

                        if (!importQueueStatus.ContainsKey(item.State))
                        {
                            importQueueStatus.Add(item.State, new List<Dictionary<string, object>>());
                        }

                        ((List<Dictionary<string, object>>)importQueueStatus[item.State]).Add(processingItem);

                        break;
                }
            }
            if (importQueueStatus.Count > 0)
            {
                notifications.Add("importQueue", importQueueStatus);
            }

            // get database upgrade status
            Dictionary<string, Dictionary<string, string>> upgradeStatus = new Dictionary<string, Dictionary<string, string>>();
            foreach (var item in ProcessQueue.QueueItems)
            {
                if (item.ItemType == ProcessQueue.QueueItemType.BackgroundDatabaseUpgrade)
                {
                    if (item.SubTasks.Count > 0)
                    {
                        foreach (var subTask in item.SubTasks)
                        {
                            upgradeStatus.Add(subTask.TaskType.ToString(), new Dictionary<string, string>
                            {
                                { "state", subTask.State.ToString() },
                                { "progressText", subTask.CurrentState.ToString() },
                                { "progress", subTask.CurrentStateProgress.ToString() }
                            });
                        }
                    }
                }
            }
            if (upgradeStatus.Count > 0)
            {
                notifications.Add("databaseUpgrade", upgradeStatus);
            }

            return Ok(notifications);
        }
    }
}