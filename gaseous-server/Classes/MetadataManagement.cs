using System;
using System.Data;
using gaseous_tools;

namespace gaseous_server.Classes
{
	public class MetadataManagement
	{
		public static void RefreshMetadata(bool forceRefresh = false)
		{
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT id, `name` FROM game;";
			DataTable dt = db.ExecuteCMD(sql);

			foreach (DataRow dr in dt.Rows)
			{
				try
				{
					Logging.Log(Logging.LogType.Information, "Metadata Refresh", "Refreshing metadata for game " + dr["name"] + " (" + dr["id"] + ")");
					Metadata.Games.GetGame((long)dr["id"], true, forceRefresh);
				}
				catch (Exception ex)
				{
					Logging.Log(Logging.LogType.Critical, "Metadata Refresh", "An error occurred while refreshing metadata for " + dr["name"], ex);
				}
			}
        }
	}
}

