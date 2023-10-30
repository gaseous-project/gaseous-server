using System;
using System.Data;
using gaseous_server.Models;

namespace gaseous_server.Classes
{
	public class MetadataManagement
	{
		public static void RefreshMetadata(bool forceRefresh = false)
		{
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
			DataTable dt = new DataTable();
			
			// update platforms
			sql = "SELECT Id, `Name` FROM Platform;";
			dt = db.ExecuteCMD(sql);

			foreach (DataRow dr in dt.Rows)
			{
				try
				{
					Logging.Log(Logging.LogType.Information, "Metadata Refresh", "Refreshing metadata for platform " + dr["name"] + " (" + dr["id"] + ")");
					Metadata.Platforms.GetPlatform((long)dr["id"], true);
				}
				catch (Exception ex)
				{
					Logging.Log(Logging.LogType.Critical, "Metadata Refresh", "An error occurred while refreshing metadata for " + dr["name"], ex);
				}
			}

			// update games
			sql = "SELECT Id, `Name` FROM Game;";
			dt = db.ExecuteCMD(sql);

			foreach (DataRow dr in dt.Rows)
			{
				try
				{
					Logging.Log(Logging.LogType.Information, "Metadata Refresh", "Refreshing metadata for game " + dr["name"] + " (" + dr["id"] + ")");
					Metadata.Games.GetGame((long)dr["id"], true, true, forceRefresh);
				}
				catch (Exception ex)
				{
					Logging.Log(Logging.LogType.Critical, "Metadata Refresh", "An error occurred while refreshing metadata for " + dr["name"], ex);
				}
			}
        }
	}
}

