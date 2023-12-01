using System;
using System.Data;
using gaseous_server.Models;
using Microsoft.VisualStudio.Web.CodeGeneration;

namespace gaseous_server.Classes
{
	public class Maintenance
    {
        const int MaxFileAge = 30;

        public static void RunMaintenance()
        {
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

            Logging.Log(Logging.LogType.Information, "Maintenance", "Optimising database tables");
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SHOW TABLES;";
            DataTable tables = db.ExecuteCMD(sql);

            foreach (DataRow row in tables.Rows)
            {
                sql = "OPTIMIZE TABLE " + row[0].ToString();
                DataTable response = db.ExecuteCMD(sql);
                foreach (DataRow responseRow in response.Rows)
                {
                    string retVal = "";
                    for (int i = 0; i < responseRow.ItemArray.Length; i++)
                    {
                        retVal += responseRow.ItemArray[i] + "; ";
                    }
                    Logging.Log(Logging.LogType.Information, "Maintenance", "Optimise table " + row[0].ToString() + ": " + retVal);
                }
            }
        }
    }
}