
using System.Data;

namespace gaseous_server.Classes.Plugins.LogProviders
{
    /// <summary>
    /// Database-based log provider implementation.
    /// </summary>
    public class DatabaseProvider : ILogProvider
    {
        /// <inheritdoc/>
        public string Name => "Database Log Provider";

        /// <inheritdoc/>
        public bool SupportsLogFetch => true;

        /// <inheritdoc/>
        public Dictionary<string, object>? Settings { get; set; }

        /// <inheritdoc/>
        public async Task<bool> LogMessage(Logging.LogItem logItem)
        {
            return await LogMessage(logItem, null);
        }

        /// <inheritdoc/>
        public async Task<bool> LogMessage(Logging.LogItem logItem, Exception? exception)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO ServerLogs (EventTime, EventType, Process, Message, AdditionalData, Exception, CorrelationId, CallingProcess, CallingUser) VALUES (@EventTime, @EventType, @Process, @Message, @AdditionalData, @Exception, @correlationid, @callingprocess, @callinguser);";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "EventTime", logItem.EventTime },
                { "EventType", (int)(logItem.EventType ?? Logging.LogType.Information) },
                { "Process", logItem.Process },
                { "Message", logItem.Message },
                { "AdditionalData", Newtonsoft.Json.JsonConvert.SerializeObject(logItem.AdditionalData) },
                { "Exception", Common.ReturnValueIfNull(logItem.ExceptionValue, "").ToString() ?? "" },
                { "correlationid", logItem.CorrelationId ?? "" },
                { "callingprocess", logItem.CallingProcess ?? "" },
                { "callinguser", logItem.CallingUser ?? "" }
            };

            await db.ExecuteCMDAsync(sql, dbDict);

            return true;
        }

        /// <inheritdoc/>
        public async Task<Logging.LogItem> GetLogMessageById(string id)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM ServerLogs WHERE Id = @Id LIMIT 1;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "Id", id }
            };
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);
            if (data.Rows.Count == 0)
            {
                return null;
            }
            return BuildLogItem(data.Rows[0]);
        }

        /// <inheritdoc/>
        public async Task<List<Logging.LogItem>> GetLogMessages(Logging.LogsViewModel model)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            if (model.StartIndex.HasValue)
            {
                dbDict.Add("StartIndex", model.StartIndex.Value);
            }
            dbDict.Add("PageNumber", (model.PageNumber - 1) * model.PageSize);
            dbDict.Add("PageSize", model.PageSize);
            string sql = "";

            List<string> whereClauses = new List<string>();

            // handle status criteria
            if (model.Status != null)
            {
                if (model.Status.Count > 0)
                {
                    List<string> statusWhere = new List<string>();
                    for (int i = 0; i < model.Status.Count; i++)
                    {
                        string valueName = "@eventtype" + i;
                        statusWhere.Add(valueName);
                        dbDict.Add(valueName, (int)model.Status[i]);
                    }

                    whereClauses.Add("EventType IN (" + string.Join(",", statusWhere) + ")");
                }
            }

            // handle start date criteria
            if (model.StartDateTime != null)
            {
                dbDict.Add("startdate", model.StartDateTime);
                whereClauses.Add("EventTime >= @startdate");
            }

            // handle end date criteria
            if (model.EndDateTime != null)
            {
                dbDict.Add("enddate", model.EndDateTime);
                whereClauses.Add("EventTime <= @enddate");
            }

            // handle search text criteria
            if (model.SearchText != null)
            {
                if (model.SearchText.Length > 0)
                {
                    dbDict.Add("messageSearch", model.SearchText);
                    whereClauses.Add("MATCH(Message) AGAINST (@messageSearch)");
                }
            }

            if (model.CorrelationId != null)
            {
                if (model.CorrelationId.Length > 0)
                {
                    dbDict.Add("correlationId", model.CorrelationId);
                    whereClauses.Add("CorrelationId = @correlationId");
                }
            }

            if (model.CallingProcess != null)
            {
                if (model.CallingProcess.Length > 0)
                {
                    dbDict.Add("callingProcess", model.CallingProcess);
                    whereClauses.Add("CallingProcess = @callingProcess");
                }
            }

            if (model.CallingUser != null)
            {
                if (model.CallingUser.Length > 0)
                {
                    dbDict.Add("callingUser", model.CallingUser);
                    whereClauses.Add("CallingUser = @callingUser");
                }
            }

            // compile WHERE clause
            string whereClause = "";
            if (whereClauses.Count > 0)
            {
                whereClause = "(" + String.Join(" AND ", whereClauses) + ")";
            }

            // execute query
            if (model.StartIndex == null)
            {
                if (whereClause.Length > 0)
                {
                    whereClause = "WHERE " + whereClause;
                }

                sql = "SELECT ServerLogs.Id, ServerLogs.EventTime, ServerLogs.EventType, ServerLogs.`Process`, ServerLogs.Message, ServerLogs.AdditionalData, ServerLogs.Exception, ServerLogs.CorrelationId, ServerLogs.CallingProcess, Users.Email FROM ServerLogs LEFT JOIN Users ON ServerLogs.CallingUser = Users.Id " + whereClause + " ORDER BY ServerLogs.Id DESC LIMIT @PageSize OFFSET @PageNumber;";
            }
            else
            {
                if (whereClause.Length > 0)
                {
                    whereClause = "AND " + whereClause;
                }

                sql = "SELECT ServerLogs.Id, ServerLogs.EventTime, ServerLogs.EventType, ServerLogs.`Process`, ServerLogs.Message, ServerLogs.AdditionalData, ServerLogs.Exception, ServerLogs.CorrelationId, ServerLogs.CallingProcess, Users.Email FROM ServerLogs LEFT JOIN Users ON ServerLogs.CallingUser = Users.Id  WHERE ServerLogs.Id < @StartIndex " + whereClause + " ORDER BY ServerLogs.Id DESC LIMIT @PageSize OFFSET @PageNumber;";
            }
            DataTable dataTable = await db.ExecuteCMDAsync(sql, dbDict);

            List<Logging.LogItem> logs = new List<Logging.LogItem>();
            foreach (DataRow row in dataTable.Rows)
            {
                logs.Add(BuildLogItem(row));
            }

            return logs;
        }

        /// <inheritdoc/>
        public async Task<bool> RunMaintenance()
        {
            // Purge logs older than the configured retention period
            // delete old logs
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            Logging.LogKey(Logging.LogType.Information, "process.maintenance", "maintenance.removing_old_logs", null, new string[] { Config.LoggingConfiguration.LogRetention.ToString() });
            long deletedCount = 1;
            long deletedEventCount = 0;
            long maxLoops = 10000;
            string sql = "DELETE FROM ServerLogs WHERE EventTime < @EventRetentionDate LIMIT 1000; SELECT ROW_COUNT() AS Count;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "EventRetentionDate", DateTime.UtcNow.AddDays(Config.LoggingConfiguration.LogRetention * -1) }
            };
            while (deletedCount > 0)
            {
                DataTable deletedCountTable = await db.ExecuteCMDAsync(sql, dbDict);
                deletedCount = (long)deletedCountTable.Rows[0][0];
                deletedEventCount += deletedCount;

                Logging.LogKey(Logging.LogType.Information, "process.maintenance", "maintenance.deleted_log_entries", null, new string[] { deletedCount.ToString() });

                // check if we've hit the limit
                maxLoops -= 1;
                if (maxLoops <= 0)
                {
                    Logging.LogKey(Logging.LogType.Warning, "process.maintenance", "maintenance.hit_maximum_number_of_loops_deleting_logs_stopping");
                    break;
                }
            }
            Logging.LogKey(Logging.LogType.Information, "process.maintenance", "maintenance.deleted_total_log_entries", null, new string[] { deletedEventCount.ToString() });

            return true;
        }

        private Logging.LogItem BuildLogItem(DataRow row)
        {
            Logging.LogItem log = new Logging.LogItem
            {
                Id = (long)row["Id"],
                EventTime = (DateTime)row["EventTime"],
                EventType = (Logging.LogType)row["EventType"],
                Process = (string)row["Process"],
                Message = (string)row["Message"],
                AdditionalData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>((string)Common.ReturnValueIfNull(row["AdditionalData"], "{}")) ?? new Dictionary<string, object>(),
                ExceptionValue = (string)row["Exception"],
                CorrelationId = (string)Common.ReturnValueIfNull(row["CorrelationId"], ""),
                CallingProcess = (string)Common.ReturnValueIfNull(row["CallingProcess"], ""),
                CallingUser = (string)Common.ReturnValueIfNull(row["Email"], "")
            };

            return log;
        }
    }
}