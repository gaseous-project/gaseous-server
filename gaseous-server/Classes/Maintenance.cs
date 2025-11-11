using System;
using System.Data;
using System.Threading.Tasks;
using gaseous_server.Models;
using Microsoft.VisualStudio.Web.CodeGeneration;

namespace gaseous_server.Classes
{
    public class Maintenance : QueueItemStatus
    {
        const int MaxFileAge = 30;

        public async Task RunDailyMaintenance()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            // remove any entries from the library that have an invalid id
            Logging.LogKey(Logging.LogType.Information, "process.maintenance", "maintenance.removing_invalid_library_entries");
            string LibraryWhereClause = "";
            foreach (GameLibrary.LibraryItem library in await GameLibrary.GetLibraries())
            {
                if (LibraryWhereClause.Length > 0)
                {
                    LibraryWhereClause += ", ";
                }
                LibraryWhereClause += library.Id;
            }
            string sqlLibraryWhereClause = "";
            if (LibraryWhereClause.Length > 0)
            {
                sqlLibraryWhereClause = "DELETE FROM Games_Roms WHERE LibraryId NOT IN ( " + LibraryWhereClause + " );";
                await db.ExecuteCMDAsync(sqlLibraryWhereClause);
            }

            // update rom counts
            Logging.LogKey(Logging.LogType.Information, "process.maintenance", "maintenance.updating_rom_counts");
            MetadataManagement metadataGame = new MetadataManagement(this);
            metadataGame.UpdateRomCounts();

            // delete old logs
            Logging.LogKey(Logging.LogType.Information, "process.maintenance", "maintenance.removing_old_logs", null, new string[] { Config.LoggingConfiguration.LogRetention.ToString() });
            long deletedCount = 1;
            long deletedEventCount = 0;
            long maxLoops = 10000;
            sql = "DELETE FROM ServerLogs WHERE EventTime < @EventRetentionDate LIMIT 1000; SELECT ROW_COUNT() AS Count;";
            dbDict.Add("EventRetentionDate", DateTime.UtcNow.AddDays(Config.LoggingConfiguration.LogRetention * -1));
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

            // delete files and directories older than 7 days in PathsToClean
            List<string> PathsToClean = new List<string>();
            PathsToClean.Add(Config.LibraryConfiguration.LibraryUploadDirectory);
            PathsToClean.Add(Config.LibraryConfiguration.LibraryTempDirectory);

            foreach (string PathToClean in PathsToClean)
            {
                Logging.LogKey(Logging.LogType.Information, "process.maintenance", "maintenance.removing_files_older_than_days_from_path", null, new string[] { MaxFileAge.ToString(), PathToClean });

                // get content
                // files first
                foreach (string filePath in Directory.GetFiles(PathToClean))
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.LastWriteTimeUtc.AddDays(MaxFileAge) < DateTime.UtcNow)
                    {
                        Logging.LogKey(Logging.LogType.Warning, "process.maintenance", "maintenance.deleting_file", null, new string[] { filePath });
                        File.Delete(filePath);
                    }
                }

                // now directories
                foreach (string dirPath in Directory.GetDirectories(PathToClean))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(dirPath);
                    if (directoryInfo.LastWriteTimeUtc.AddDays(MaxFileAge) < DateTime.UtcNow)
                    {
                        Logging.LogKey(Logging.LogType.Warning, "process.maintenance", "maintenance.deleting_directory", null, new string[] { directoryInfo.ToString() });
                        Directory.Delete(dirPath, true);
                    }
                }
            }
        }

        public async Task RunWeeklyMaintenance()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            Logging.LogKey(Logging.LogType.Information, "process.maintenance", "maintenance.optimising_database_tables");
            sql = "SHOW FULL TABLES WHERE Table_Type = 'BASE TABLE';";
            DataTable tables = await db.ExecuteCMDAsync(sql);

            int StatusCounter = 1;
            foreach (DataRow row in tables.Rows)
            {
                SetStatus(StatusCounter, tables.Rows.Count, "Optimising table " + row[0].ToString());

                sql = "OPTIMIZE TABLE `" + row[0].ToString() + "`;";
                DataTable response = await db.ExecuteCMDAsync(sql, new Dictionary<string, object>(), 240);
                foreach (DataRow responseRow in response.Rows)
                {
                    string retVal = "";
                    for (int i = 0; i < responseRow.ItemArray.Length; i++)
                    {
                        retVal += responseRow.ItemArray[i] + "; ";
                    }
                    Logging.LogKey(Logging.LogType.Information, "process.maintenance", "maintenance.optimise_table_status", null, new string[] { StatusCounter.ToString(), tables.Rows.Count.ToString(), row[0].ToString(), retVal });
                }

                StatusCounter += 1;
            }
            ClearStatus();
        }
    }
}