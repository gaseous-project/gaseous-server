using System;
using System.Data;
using gaseous_server.Models;
using Microsoft.VisualStudio.Web.CodeGeneration;

namespace gaseous_server.Classes
{
	public class Maintenance : QueueItemStatus
    {
        const int MaxFileAge = 30;

        public void RunDailyMaintenance()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            // remove any entries from the library that have an invalid id
            string LibraryWhereClause = "";
            foreach (GameLibrary.LibraryItem library in GameLibrary.GetLibraries)
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
                db.ExecuteCMD(sqlLibraryWhereClause);
            }

            // delete old logs
            sql = "DELETE FROM ServerLogs WHERE EventTime < @EventRetentionDate;";
            dbDict.Add("EventRetentionDate", DateTime.UtcNow.AddDays(Config.LoggingConfiguration.LogRetention * -1));
            db.ExecuteCMD(sql, dbDict);

            // delete files and directories older than 7 days in PathsToClean
            List<string> PathsToClean = new List<string>();
            PathsToClean.Add(Config.LibraryConfiguration.LibraryUploadDirectory);
            PathsToClean.Add(Config.LibraryConfiguration.LibraryTempDirectory);

            foreach (string PathToClean in PathsToClean)
            {
                Logging.Log(Logging.LogType.Information, "Maintenance", "Removing files older than " + MaxFileAge + " days from " + PathToClean);

                // get content
                // files first
                foreach (string filePath in Directory.GetFiles(PathToClean))
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.LastWriteTimeUtc.AddDays(MaxFileAge) < DateTime.UtcNow)
                    {
                        Logging.Log(Logging.LogType.Warning, "Maintenance", "Deleting file " + filePath);
                        File.Delete(filePath);
                    }
                }

                // now directories
                foreach (string dirPath in Directory.GetDirectories(PathToClean))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(dirPath);
                    if (directoryInfo.LastWriteTimeUtc.AddDays(MaxFileAge) < DateTime.UtcNow)
                    {
                        Logging.Log(Logging.LogType.Warning, "Maintenance", "Deleting directory " + directoryInfo);
                        Directory.Delete(dirPath, true);
                    }
                }
            }
        }

        public void RunWeeklyMaintenance()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            Logging.Log(Logging.LogType.Information, "Maintenance", "Optimising database tables");
            sql = "SHOW FULL TABLES WHERE Table_Type = 'BASE TABLE';";
            DataTable tables = db.ExecuteCMD(sql);

            int StatusCounter = 1;
            foreach (DataRow row in tables.Rows)
            {
                SetStatus(StatusCounter, tables.Rows.Count, "Optimising table " + row[0].ToString());

                sql = "OPTIMIZE TABLE " + row[0].ToString();
                DataTable response = db.ExecuteCMD(sql);
                foreach (DataRow responseRow in response.Rows)
                {
                    string retVal = "";
                    for (int i = 0; i < responseRow.ItemArray.Length; i++)
                    {
                        retVal += responseRow.ItemArray[i] + "; ";
                    }
                    Logging.Log(Logging.LogType.Information, "Maintenance", "(" + StatusCounter + "/" + tables.Rows.Count + "): Optimise table " + row[0].ToString() + ": " + retVal);
                }

                StatusCounter += 1;
            }
            ClearStatus();
        }
    }
}