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
        private const string PendingKey = "pending";
        private const string ProcessingKey = "processing";

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotifications()
        {
            Dictionary<string, object> notifications = new Dictionary<string, object>();

            // get import queue status
            Dictionary<string, object> importQueueStatus = new Dictionary<string, object>();
            foreach (var item in ImportGame.ImportStates)
            {
                switch (item.State)
                {
                    case Models.ImportStateItem.ImportState.Pending:
                        if (importQueueStatus.ContainsKey(PendingKey))
                        {
                            importQueueStatus[PendingKey] = (int)importQueueStatus[PendingKey] + 1;
                        }
                        else
                        {
                            importQueueStatus.Add(PendingKey, 1);
                        }
                        break;

                    case Models.ImportStateItem.ImportState.Processing:
                        Dictionary<string, object> processingItem = new Dictionary<string, object>
                        {
                            { "sessionid", item.SessionId },
                            { "state", item.State.ToString() },
                            { "filename", item.FileName },
                            { "type", item.Type }
                        };

                        if (item.ProcessData != null)
                        {

                        }

                        if (!importQueueStatus.ContainsKey(ProcessingKey))
                        {
                            importQueueStatus.Add(ProcessingKey, new List<Dictionary<string, object>>());
                        }

                        ((List<Dictionary<string, object>>)importQueueStatus[ProcessingKey]).Add(processingItem);

                        break;
                }
            }
            if (ImportGame.ImportStates.Count > 0)
            {
                importQueueStatus.Add("ImportQueue", importQueueStatus);
            }

            return Ok();
        }
    }
}