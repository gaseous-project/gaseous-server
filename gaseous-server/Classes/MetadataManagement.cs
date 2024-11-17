using System;
using System.Data;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;

namespace gaseous_server.Classes
{
	public class MetadataManagement : QueueItemStatus
	{
		public void RefreshMetadata(bool forceRefresh = false)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			DataTable dt = new DataTable();

			// disabling forceRefresh
			forceRefresh = false;

			// update platforms
			sql = "SELECT Id, `Name` FROM Platform;";
			dt = db.ExecuteCMD(sql);

			int StatusCounter = 1;
			foreach (DataRow dr in dt.Rows)
			{
				SetStatus(StatusCounter, dt.Rows.Count, "Refreshing metadata for platform " + dr["name"]);

				try
				{
					Logging.Log(Logging.LogType.Information, "Metadata Refresh", "(" + StatusCounter + "/" + dt.Rows.Count + "): Refreshing metadata for platform " + dr["name"] + " (" + dr["id"] + ")");
					Metadata.Platforms.GetPlatform((long)dr["id"]);
				}
				catch (Exception ex)
				{
					Logging.Log(Logging.LogType.Critical, "Metadata Refresh", "An error occurred while refreshing metadata for " + dr["name"], ex);
				}

				StatusCounter += 1;
			}
			ClearStatus();

			// update games
			if (forceRefresh == true)
			{
				// when forced, only update games with ROMs for
				sql = "SELECT Id, `Name` FROM view_GamesWithRoms;";
			}
			else
			{
				// when run normally, update all games (since this will honour cache timeouts)
				sql = "SELECT Id, `Name` FROM Game;";
			}
			dt = db.ExecuteCMD(sql);

			StatusCounter = 1;
			foreach (DataRow dr in dt.Rows)
			{
				SetStatus(StatusCounter, dt.Rows.Count, "Refreshing metadata for game " + dr["name"]);

				try
				{
					Logging.Log(Logging.LogType.Information, "Metadata Refresh", "(" + StatusCounter + "/" + dt.Rows.Count + "): Refreshing metadata for game " + dr["name"] + " (" + dr["id"] + ")");
					Metadata.Games.GetGame(Communications.MetadataSource, (long)dr["id"]);
				}
				catch (Exception ex)
				{
					Logging.Log(Logging.LogType.Critical, "Metadata Refresh", "An error occurred while refreshing metadata for " + dr["name"], ex);
				}

				StatusCounter += 1;
			}
			ClearStatus();
		}
	}
}

