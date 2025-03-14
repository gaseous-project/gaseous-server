using System;
using System.Data;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using HasheousClient.Models;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes
{
	public class MetadataManagement : QueueItemStatus
	{
		private static bool Processing = false;

		public static HasheousClient.Models.MetadataSources[] BlockedMetadataSource = new HasheousClient.Models.MetadataSources[]
		{
			HasheousClient.Models.MetadataSources.RetroAchievements
		};

		public enum MetadataMapSupportDataTypes
		{
			UserManualLink
		}

		/// <summary>
		/// Creates a new metadata map, if one with the same platformId and name does not already exist.
		/// </summary>
		/// <param name="platformId">
		/// The ID of the platform to which the metadata map belongs.
		/// </param>
		/// <param name="name">
		/// The name of the metadata map.
		/// </param>
		/// <returns>
		/// The ID of the new metadata map, or the ID of the existing metadata map if one already exists.
		/// </returns>
		public static MetadataMap? NewMetadataMap(long platformId, string name)
		{
			if (Processing == true)
			{
				// loop until processing = false
				while (Processing == true)
				{
					System.Threading.Thread.Sleep(500);
				}
			}
			Processing = true;

			// strip version number from name
			name = Common.StripVersionsFromFileName(name);

			// store the metadata map
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@platformId", platformId },
				{ "@name", name }
			};
			DataTable dt = new DataTable();

			// check if the metadata map already exists
			MetadataMap? existingMetadataMap = GetMetadataMap(platformId, name);
			if (existingMetadataMap != null)
			{
				Processing = false;
				return existingMetadataMap;
			}

			// create the metadata map
			sql = "INSERT INTO MetadataMap (PlatformId, SignatureGameName) VALUES (@platformId, @name); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
			dt = db.ExecuteCMD(sql, dbDict);

			long metadataMapId = (long)dt.Rows[0][0];

			// create dummy game metadata item and capture id
			sql = "INSERT INTO Game (SourceId, Name, dateAdded, lastUpdated) VALUES (@sourceid, @name, @dateadded, @lastupdated); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
			dbDict = new Dictionary<string, object>()
			{
				{ "@sourceid", HasheousClient.Models.MetadataSources.None },
				{ "@name", name },
				{ "@dateadded", DateTime.UtcNow },
				{ "@lastupdated", DateTime.UtcNow }
			};
			dt = db.ExecuteCMD(sql, dbDict);

			long gameId = (long)dt.Rows[0][0];

			// add default metadata sources
			AddMetadataMapItem(metadataMapId, HasheousClient.Models.MetadataSources.None, gameId, true);

			// return the new metadata map
			MetadataMap RetVal = GetMetadataMap(metadataMapId);
			Processing = false;
			return RetVal;
		}

		/// <summary>
		/// Adds a metadata map item to the database.
		/// </summary>
		/// <param name="metadataMapId">
		/// The ID of the metadata map to which the item belongs.
		/// </param>
		/// <param name="sourceType">
		/// The type of the metadata source.
		/// </param>
		/// <param name="sourceId">
		/// The ID of the metadata source.
		/// </param>
		/// <param name="preferred">
		/// Whether the metadata source is preferred.
		/// </param>
		/// <remarks>
		/// If the metadata source is preferred, all other metadata sources for the same metadata map will be set to not preferred.
		/// </remarks>
		public static void AddMetadataMapItem(long metadataMapId, HasheousClient.Models.MetadataSources sourceType, long sourceId, bool preferred)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@metadataMapId", metadataMapId },
				{ "@sourceType", sourceType },
				{ "@sourceId", sourceId },
				{ "@preferred", preferred },
				{ "@processedatimport", false }
			};

			if (preferred == true)
			{
				// set all other items to not preferred
				sql = "UPDATE MetadataMapBridge SET Preferred = 0 WHERE ParentMapId = @metadataMapId;";
				db.ExecuteCMD(sql, dbDict);
			}

			sql = "INSERT INTO MetadataMapBridge (ParentMapId, MetadataSourceType, MetadataSourceId, Preferred, ProcessedAtImport) VALUES (@metadataMapId, @sourceType, @sourceId, @preferred, @processedatimport);";
			db.ExecuteCMD(sql, dbDict);
		}

		public static void UpdateMetadataMapItem(long metadataMapId, HasheousClient.Models.MetadataSources SourceType, long sourceId, bool? preferred = null)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@metadataMapId", metadataMapId },
				{ "@sourceType", SourceType },
				{ "@sourceId", sourceId },
				{ "@preferred", preferred }
			};

			if (preferred == true)
			{
				// set all other items to not preferred
				sql = "UPDATE MetadataMapBridge SET Preferred = 0 WHERE ParentMapId = @metadataMapId; UPDATE MetadataMapBridge SET MetadataSourceId = @sourceId, Preferred = @preferred WHERE ParentMapId = @metadataMapId AND MetadataSourceType = @sourceType;";
				db.ExecuteCMD(sql, dbDict);
			}
			else
			{
				sql = "UPDATE MetadataMapBridge SET MetadataSourceId = @sourceId WHERE ParentMapId = @metadataMapId AND MetadataSourceType = @sourceType;";
				db.ExecuteCMD(sql, dbDict);
			}
		}

		/// <summary>
		/// Gets a metadata map from the database.
		/// </summary>
		/// <param name="platformId">
		/// The ID of the platform to which the metadata map belongs.
		/// </param>
		/// <param name="name">
		/// The name of the metadata map.
		/// </param>
		/// <returns>
		/// The metadata map, or null if it does not exist.
		/// </returns>
		/// <remarks>
		/// This method will return the first metadata map found with the given platformId and name.
		/// </remarks>
		private static MetadataMap? GetMetadataMap(long platformId, string name)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@platformId", platformId },
				{ "@name", name.Trim() }
			};
			DataTable dt = new DataTable();

			sql = "SELECT Id FROM MetadataMap WHERE PlatformId = @platformId AND SignatureGameName = @name;";
			dt = db.ExecuteCMD(sql, dbDict);

			if (dt.Rows.Count > 0)
			{
				return GetMetadataMap((long)dt.Rows[0]["Id"]);
			}

			return null;
		}

		/// <summary>
		/// Gets a metadata map from the database.
		/// </summary>
		/// <param name="metadataMapId">
		/// The ID of the metadata map.
		/// </param>
		/// <returns>
		/// The metadata map, or null if it does not exist.
		/// </returns>
		/// <remarks>
		/// This method will return the metadata map with the given ID.
		/// </remarks>
		public static MetadataMap? GetMetadataMap(long metadataMapId)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@metadataMapId", metadataMapId }
			};
			DataTable dt = new DataTable();

			sql = "SELECT * FROM MetadataMap WHERE Id = @metadataMapId;";
			dt = db.ExecuteCMD(sql, dbDict);

			if (dt.Rows.Count > 0)
			{
				MetadataMap metadataMap = new MetadataMap()
				{
					Id = (long)dt.Rows[0]["Id"],
					PlatformId = (long)dt.Rows[0]["PlatformId"],
					SignatureGameName = dt.Rows[0]["SignatureGameName"].ToString(),
					MetadataMapItems = new List<MetadataMap.MetadataMapItem>()
				};

				sql = "SELECT * FROM MetadataMapBridge WHERE ParentMapId = @metadataMapId;";
				dt = db.ExecuteCMD(sql, dbDict);

				foreach (DataRow dr in dt.Rows)
				{
					MetadataMap.MetadataMapItem metadataMapItem = new MetadataMap.MetadataMapItem()
					{
						SourceType = (HasheousClient.Models.MetadataSources)dr["MetadataSourceType"],
						SourceId = (long)dr["MetadataSourceId"],
						Preferred = (bool)dr["Preferred"]
					};

					if (!BlockedMetadataSource.Contains(metadataMapItem.SourceType))
					{
						metadataMap.MetadataMapItems.Add(metadataMapItem);
					}
				}

				return metadataMap;
			}

			return null;
		}

		public static void SetMetadataSupportData(long metadataMapId, MetadataMapSupportDataTypes dataType, string data)
		{
			// verify the metadata map exists
			MetadataMap? metadataMap = GetMetadataMap(metadataMapId);
			if (metadataMap == null)
			{
				return;
			}

			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@metadataMapId", metadataMapId },
				{ "@data", data }
			};

			switch (dataType)
			{
				case MetadataMapSupportDataTypes.UserManualLink:
					sql = "UPDATE MetadataMap SET UserManualLink = @data WHERE Id = @metadataMapId;";
					db.ExecuteCMD(sql, dbDict);
					break;
			}
		}

		/// <summary>
		/// Get the MetadataMapItem for the provided metadata source, and source id
		/// </summary>
		/// <param name="sourceType">
		/// The type of the metadata source.
		/// </param>
		/// <param name="sourceId">
		/// The ID of the metadata source.
		/// </param>
		/// <returns>
		/// The MetadataMapItem, or null if it does not exist.
		/// </returns>
		/// <remarks>
		/// This method will return the MetadataMapItem with the given sourceType and sourceId.
		/// </remarks>
		public static MetadataMap.MetadataMapItem? GetMetadataMapFromSourceId(HasheousClient.Models.MetadataSources sourceType, long sourceId)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@sourceType", sourceType },
				{ "@sourceId", sourceId }
			};
			DataTable dt = new DataTable();

			sql = "SELECT * FROM MetadataMapBridge WHERE MetadataSourceType = @sourceType AND MetadataSourceId = @sourceId;";
			dt = db.ExecuteCMD(sql, dbDict);

			if (dt.Rows.Count > 0)
			{
				MetadataMap.MetadataMapItem metadataMapItem = new MetadataMap.MetadataMapItem()
				{
					SourceType = (HasheousClient.Models.MetadataSources)dt.Rows[0]["MetadataSourceType"],
					SourceId = (long)dt.Rows[0]["MetadataSourceId"],
					Preferred = (bool)dt.Rows[0]["Preferred"]
				};

				return metadataMapItem;
			}

			return null;
		}

		/// <summary>
		/// Get the Id of the MetadataMap for the provided metadata source, and source id
		/// </summary>
		/// <param name="sourceType">
		/// The type of the metadata source.
		/// </param>
		/// <param name="sourceId">
		/// The ID of the metadata source.
		/// </param>
		/// <returns>
		/// The ID of the MetadataMap, or null if it does not exist.
		/// </returns>
		public static long? GetMetadataMapIdFromSourceId(HasheousClient.Models.MetadataSources sourceType, long sourceId)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@sourceType", sourceType },
				{ "@sourceId", sourceId }
			};
			DataTable dt = new DataTable();

			sql = "SELECT * FROM MetadataMapBridge WHERE MetadataSourceType = @sourceType AND MetadataSourceId = @sourceId;";
			dt = db.ExecuteCMD(sql, dbDict);

			if (dt.Rows.Count > 0)
			{
				return (long)dt.Rows[0]["ParentMapId"];
			}

			return null;
		}

		public void RefreshMetadata(bool forceRefresh = false)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			DataTable dt = new DataTable();

			// disabling forceRefresh
			forceRefresh = false;

			// update platform metadata
			sql = "SELECT Id, `Name` FROM Platform;";
			dt = db.ExecuteCMD(sql);

			int StatusCounter = 1;
			foreach (DataRow dr in dt.Rows)
			{
				SetStatus(StatusCounter, dt.Rows.Count, "Refreshing metadata for platform " + dr["name"]);

				try
				{
					Logging.Log(Logging.LogType.Information, "Metadata Refresh", "(" + StatusCounter + "/" + dt.Rows.Count + "): Refreshing metadata for platform " + dr["name"] + " (" + dr["id"] + ")");

					HasheousClient.Models.MetadataSources metadataSource = HasheousClient.Models.MetadataSources.None;

					// fetch the platform metadata
					Platform platform = Metadata.Platforms.GetPlatform((long)dr["id"], metadataSource);

					// fetch the platform metadata from Hasheous
					if ((long)dr["id"] != 0)
					{
						if (Config.MetadataConfiguration.SignatureSource == HasheousClient.Models.MetadataModel.SignatureSources.Hasheous)
						{

							Communications.PopulateHasheousPlatformData((long)dr["id"]);
						}
					}
					else
					{
						// set the platform to unknown
						sql = "UPDATE Platform SET Name = 'Unknown Platform', Slug = 'unknown', PlatformLogo = 0 WHERE Id = 0;";
						db.ExecuteCMD(sql);
					}

					// force platformLogo refresh
					if (platform.PlatformLogo != null)
					{
						Metadata.PlatformLogos.GetPlatformLogo(platform.PlatformLogo, metadataSource);
					}
				}
				catch (Exception ex)
				{
					Logging.Log(Logging.LogType.Critical, "Metadata Refresh", "An error occurred while refreshing metadata for " + dr["name"], ex);
				}

				StatusCounter += 1;
			}
			ClearStatus();

			// update rom signatures - only valid if Haseheous is enabled
			if (Config.MetadataConfiguration.SignatureSource == MetadataModel.SignatureSources.Hasheous)
			{
				// get all ROMs in the database
				sql = "SELECT * FROM view_Games_Roms WHERE DateUpdated < @LastUpdateThreshold;";
				// set @LastUpdateThreshold to a random date between 14 and 30 days in the past
				Dictionary<string, object> dbDict = new Dictionary<string, object>()
				{
					{ "@LastUpdateThreshold", DateTime.UtcNow.AddDays(-new Random().Next(14, 30)) }
				};
				dt = db.ExecuteCMD(sql, dbDict);

				StatusCounter = 1;
				foreach (DataRow dr in dt.Rows)
				{
					SetStatus(StatusCounter, dt.Rows.Count, "Refreshing signature for ROM " + dr["Name"]);

					try
					{
						Logging.Log(Logging.LogType.Information, "Metadata Refresh", "(" + StatusCounter + "/" + dt.Rows.Count + "): Refreshing signature for ROM " + dr["Name"] + " (" + dr["Id"] + ")");

						// get the hash of the ROM from the datarow
						string? md5 = dr["MD5"] == DBNull.Value ? null : dr["MD5"].ToString();
						string? sha1 = dr["SHA1"] == DBNull.Value ? null : dr["SHA1"].ToString();
						Common.hashObject hash = new Common.hashObject();
						if (md5 != null)
						{
							hash.md5hash = md5;
						}
						if (sha1 != null)
						{
							hash.sha1hash = sha1;
						}

						// get the library for the ROM
						GameLibrary.LibraryItem library = GameLibrary.GetLibrary((int)dr["LibraryId"]);

						// get the signature for the ROM
						FileInfo fi = new FileInfo(dr["Path"].ToString());
						FileSignature fileSignature = new FileSignature();
						gaseous_server.Models.Signatures_Games signature = fileSignature.GetFileSignature(library, hash, fi, fi.FullName);

						// validate the signature - if it is invalid, skip the rest of the loop
						// validation rules: 1) signature must not be null, 2) signature must have a platform ID
						if (signature == null || signature.Flags.PlatformId == null)
						{
							Logging.Log(Logging.LogType.Information, "Metadata Refresh", "Signature for " + dr["Name"] + " is invalid - skipping metadata refresh");
							StatusCounter += 1;
							continue;
						}

						// update the signature in the database
						Platform? signaturePlatform = Metadata.Platforms.GetPlatform((long)signature.Flags.PlatformId);
						if (signature.Flags.GameId == 0)
						{
							HasheousClient.Models.Metadata.IGDB.Game? discoveredGame = ImportGame.SearchForGame(signature, signature.Flags.PlatformId, false);
							if (discoveredGame != null && discoveredGame.Id != null)
							{
								signature.MetadataSources.AddGame((long)discoveredGame.Id, discoveredGame.Name, MetadataSources.IGDB);
							}
						}
						ImportGame.StoreGame(library, hash, signature, signaturePlatform, fi.FullName, (long)dr["Id"], false);
					}
					catch (Exception ex)
					{
						Logging.Log(Logging.LogType.Critical, "Metadata Refresh", "An error occurred while refreshing metadata for " + dr["Name"], ex);
					}

					StatusCounter += 1;
				}
				ClearStatus();
			}

			// update the rom counts
			UpdateRomCounts();

			// update game metadata
			if (forceRefresh == true)
			{
				// when forced, only update games with ROMs for
				sql = "SELECT Id, `Name` FROM view_GamesWithRoms;";
			}
			else
			{
				// when run normally, update all games (since this will honour cache timeouts)
				sql = "SELECT DISTINCT MetadataSourceId AS `Id`, MetadataSourceType AS `GameIdType`, SignatureGameName AS `Name` FROM gaseous.view_MetadataMap;";
			}
			dt = db.ExecuteCMD(sql);

			StatusCounter = 1;
			foreach (DataRow dr in dt.Rows)
			{
				SetStatus(StatusCounter, dt.Rows.Count, "Refreshing metadata for game " + dr["name"]);

				try
				{
					MetadataSources metadataSource;
					if (dr["GameIdType"] == DBNull.Value)
					{
						Logging.Log(Logging.LogType.Information, "Metadata Refresh", "(" + StatusCounter + "/" + dt.Rows.Count + "): Unable to refresh metadata for game " + dr["name"] + " (" + dr["id"] + ") - no source type specified");
					}
					else
					{
						metadataSource = (MetadataSources)Enum.Parse(typeof(MetadataSources), dr["GameIdType"].ToString());

						Logging.Log(Logging.LogType.Information, "Metadata Refresh", "(" + StatusCounter + "/" + dt.Rows.Count + "): Refreshing metadata for game " + dr["name"] + " (" + dr["id"] + ") using source " + metadataSource.ToString());
						HasheousClient.Models.Metadata.IGDB.Game game = Metadata.Games.GetGame(metadataSource, (long)dr["id"]);

						// get supporting metadata
						if (game != null)
						{
							if (game.AgeRatings != null)
							{
								foreach (long ageRatingId in game.AgeRatings)
								{
									AgeRating ageRating = Metadata.AgeRatings.GetAgeRating(metadataSource, ageRatingId);
									if (ageRating.ContentDescriptions != null)
									{
										foreach (long ageRatingContentDescriptionId in ageRating.ContentDescriptions)
										{
											Metadata.AgeRatingContentDescriptions.GetAgeRatingContentDescriptions(metadataSource, ageRatingContentDescriptionId);
										}
									}
								}
							}
							if (game.AlternativeNames != null)
							{
								foreach (long alternateNameId in game.AlternativeNames)
								{
									Metadata.AlternativeNames.GetAlternativeNames(metadataSource, alternateNameId);
								}
							}
							if (game.Artworks != null)
							{
								foreach (long artworkId in game.Artworks)
								{
									Metadata.Artworks.GetArtwork(metadataSource, artworkId);
								}
							}
							if (game.Cover != null)
							{
								Metadata.Covers.GetCover(metadataSource, (long?)game.Cover);
							}
							if (game.GameModes != null)
							{
								foreach (long gameModeId in game.GameModes)
								{
									Metadata.GameModes.GetGame_Modes(metadataSource, gameModeId);
								}
							}
							if (game.Genres != null)
							{
								foreach (long genreId in game.Genres)
								{
									Metadata.Genres.GetGenres(metadataSource, genreId);
								}
							}
							if (game.GameLocalizations != null)
							{
								foreach (long gameLocalizationId in game.GameLocalizations)
								{
									GameLocalization gameLocalization = Metadata.GameLocalizations.GetGame_Locatization(metadataSource, gameLocalizationId);

									if (gameLocalization != null)
									{
										if (gameLocalization.Cover != null)
										{
											Metadata.Covers.GetCover(metadataSource, (long)gameLocalization.Cover);
										}

										if (gameLocalization.Region != null)
										{
											Metadata.Regions.GetGame_Region(metadataSource, (long)gameLocalization.Region);
										}
									}
								}
							}
							if (game.Videos != null)
							{
								foreach (long gameVideoId in game.Videos)
								{
									Metadata.GamesVideos.GetGame_Videos(metadataSource, gameVideoId);
								}
							}
							if (game.MultiplayerModes != null)
							{
								foreach (long multiplayerModeId in game.MultiplayerModes)
								{
									Metadata.MultiplayerModes.GetGame_MultiplayerModes(metadataSource, multiplayerModeId);
								}
							}
							if (game.PlayerPerspectives != null)
							{
								foreach (long playerPerspectiveId in game.PlayerPerspectives)
								{
									Metadata.PlayerPerspectives.GetGame_PlayerPerspectives(metadataSource, playerPerspectiveId);
								}
							}
							if (game.ReleaseDates != null)
							{
								foreach (long releaseDateId in game.ReleaseDates)
								{
									Metadata.ReleaseDates.GetReleaseDates(metadataSource, releaseDateId);
								}
							}
							if (game.Screenshots != null)
							{
								foreach (long screenshotId in game.Screenshots)
								{
									Metadata.Screenshots.GetScreenshot(metadataSource, screenshotId);
								}
							}
							if (game.Themes != null)
							{
								foreach (long themeId in game.Themes)
								{
									Metadata.Themes.GetGame_Themes(metadataSource, themeId);
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					Logging.Log(Logging.LogType.Critical, "Metadata Refresh", "An error occurred while refreshing metadata for " + dr["name"], ex);
				}

				StatusCounter += 1;
			}
			ClearStatus();
		}

		/// <summary>
		/// Updates the ROM counts for each metadata map in the database.
		/// </summary>
		public static void UpdateRomCounts()
		{
			string sql = @"
UPDATE `MetadataMap`
        INNER JOIN
    (SELECT 
        `MetadataMap`.`Id`, COUNT(`Games_Roms`.`Id`) AS `RomCount`
    FROM
        `MetadataMap`
    LEFT JOIN `Games_Roms` ON `MetadataMap`.`Id` = `Games_Roms`.`MetadataMapId`
    GROUP BY `MetadataMap`.`Id`
    ORDER BY `MetadataMap`.`SignatureGameName`) AS `RomCounter` ON `MetadataMap`.`Id` = `RomCounter`.`Id` 
SET 
    `MetadataMap`.`RomCount` = `RomCounter`.`RomCount`;";
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			db.ExecuteNonQuery(sql);
		}
	}
}

