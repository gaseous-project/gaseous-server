using System;
using System.Data;
using System.Threading.Tasks;
using gaseous_server.Classes.Metadata;
using gaseous_server.Controllers;
using gaseous_server.Models;
using System.Linq;
using HasheousClient.Models;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes
{
	public class MetadataManagement : QueueItemStatus
	{
		public MetadataManagement()
		{

		}

		public MetadataManagement(object callingQueueItem)
		{
			this.CallingQueueItem = callingQueueItem;
		}

		private static bool Processing = false;

		public static FileSignature.MetadataSources[] BlockedMetadataSource = new FileSignature.MetadataSources[]
		{
			FileSignature.MetadataSources.RetroAchievements
		};

		public enum MetadataMapSupportDataTypes
		{
			UserManualLink
		}

		// static memory cache for database queries (snapshot cloning enabled so retrieved objects can be safely mutated)
		private static MemoryCache DatabaseMemoryCache = new MemoryCache(
			maxSize: 500,
			cloneOnSet: true,
			cloneOnGet: true,
			cloner: CloneForCache
		);

		/// <summary>
		/// Custom clone delegate used by the in-memory cache to snapshot supported object types.
		/// Ensures internal collections are copied so callers cannot mutate the cached instance.
		/// </summary>
		private static object CloneForCache(object source)
		{
			try
			{
				if (source is MetadataMap mm)
				{
					return new MetadataMap
					{
						Id = mm.Id,
						PlatformId = mm.PlatformId,
						SignatureGameName = mm.SignatureGameName,
						MetadataMapItems = mm.MetadataMapItems?.Select(i => new MetadataMap.MetadataMapItem
						{
							SourceType = i.SourceType,
							SourceId = i.SourceId,
							AutomaticMetadataSourceId = i.AutomaticMetadataSourceId,
							Preferred = i.Preferred,
							IsManual = i.IsManual
						}).ToList()
					};
				}
				else if (source is List<long> longList)
				{
					return new List<long>(longList); // shallow copy list of value types
				}
				// Return original for unsupported types (they'll either be immutable or not mutated downstream)
				return source;
			}
			catch
			{
				// Fail open: if cloning fails, return original reference (better than throwing inside cache path)
				return source;
			}
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
			MetadataMap? existingMetadataMap = GetMetadataMap(platformId, name).Result;
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
			sql = "INSERT INTO Metadata_Game (SourceId, Name, dateAdded, lastUpdated) VALUES (@sourceid, @name, @dateadded, @lastupdated); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
			dbDict = new Dictionary<string, object>()
			{
				{ "@sourceid", FileSignature.MetadataSources.None },
				{ "@name", name },
				{ "@dateadded", DateTime.UtcNow },
				{ "@lastupdated", DateTime.UtcNow }
			};
			dt = db.ExecuteCMD(sql, dbDict);

			long gameId = (long)dt.Rows[0][0];

			// add default metadata sources
			AddMetadataMapItem(metadataMapId, FileSignature.MetadataSources.None, gameId, true, false, gameId);

			// return the new metadata map
			MetadataMap? RetVal = GetMetadataMap(metadataMapId).Result;
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
		/// <param name="IsManual">
		/// Whether this metadata source was maunally configured by the user. Prevents automatic updates. Can be modified by the user.
		/// </param>
		/// <param name="AutomaticMetadataSourceId">
		/// The unique identifier for the data source as provided by the automatic metadata fetcher.
		/// <remarks>
		/// If the metadata source is preferred, all other metadata sources for the same metadata map will be set to not preferred.
		/// </remarks>
		public static void AddMetadataMapItem(long metadataMapId, FileSignature.MetadataSources sourceType, long sourceId, bool preferred, bool IsManual, long AutomaticMetadataSourceId)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@metadataMapId", metadataMapId },
				{ "@sourceType", sourceType },
				{ "@sourceId", sourceId },
				{ "@preferred", preferred },
				{ "@processedatimport", false },
				{ "@isManual", IsManual },
				{ "@automaticMetadataSourceId", AutomaticMetadataSourceId }
			};

			if (preferred == true)
			{
				// set all other items to not preferred
				sql = "UPDATE MetadataMapBridge SET Preferred = 0 WHERE ParentMapId = @metadataMapId;";
				db.ExecuteCMD(sql, dbDict);
			}

			// Use INSERT ... ON DUPLICATE KEY UPDATE so that a preferred flag change is reflected even if the row exists
			sql = "INSERT INTO MetadataMapBridge (ParentMapId, MetadataSourceType, MetadataSourceId, Preferred, ProcessedAtImport, IsManual, AutomaticMetadataSourceId) VALUES (@metadataMapId, @sourceType, @sourceId, @preferred, @processedatimport, @isManual, @automaticMetadataSourceId) ON DUPLICATE KEY UPDATE Preferred = VALUES(Preferred), IsManual = VALUES(IsManual), AutomaticMetadataSourceId = VALUES(AutomaticMetadataSourceId);";
			db.ExecuteCMD(sql, dbDict);

			// invalidate cache so subsequent GetMetadataMap sees the new/updated item
			DatabaseMemoryCache.RemoveCacheObject("MetadataMap_" + metadataMapId.ToString());
		}

		/// <summary>
		/// Updates a metadata map item in the database.
		/// </summary>
		/// <param name="metadataMapId">
		/// The ID of the metadata map to which the item belongs.
		/// </param>
		/// <param name="SourceType">
		/// The type of the metadata source.
		/// </param>
		/// <param name="sourceId">
		/// The ID of the metadata source.
		/// </param>
		/// <param name="preferred">
		/// Whether the metadata source is preferred.
		/// </param>
		/// <param name="IsManual">
		/// Whether this metadata source was maunally configured by the user. Prevents automatic updates. Can be modified by the user.
		/// </param>
		/// <param name="AutomaticMetadataSourceId">
		/// The unique identifier for the data source as provided by the automatic metadata fetcher.
		/// </param>
		/// /// <remarks>
		/// If the metadata source is preferred, all other metadata sources for the same metadata map will be set to not preferred.
		/// </remarks>
		public static void UpdateMetadataMapItem(long metadataMapId, FileSignature.MetadataSources SourceType, long? sourceId = null, bool? preferred = null, bool? IsManual = null, long? AutomaticMetadataSourceId = null)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@metadataMapId", metadataMapId },
				{ "@sourceType", SourceType },
				{ "@sourceId", sourceId ?? (object)DBNull.Value },
				{ "@preferred", preferred ?? (object)DBNull.Value },
				{ "@isManual", IsManual ?? (object)DBNull.Value },
				{ "@automaticMetadataSourceId", AutomaticMetadataSourceId ?? (object)DBNull.Value }
			};

			List<string> whereClauses = new List<string>();
			if (sourceId != null)
			{
				whereClauses.Add("MetadataSourceId = @sourceId");
			}
			if (IsManual != null)
			{
				whereClauses.Add("IsManual = @isManual");
			}
			if (AutomaticMetadataSourceId != null)
			{
				whereClauses.Add("AutomaticMetadataSourceId = @automaticMetadataSourceId");
			}

			string whereClause = string.Join(", ", whereClauses);

			if (preferred == true)
			{
				// set all other items to not preferred, and update this one to preferred
				sql = "UPDATE MetadataMapBridge SET Preferred = 0 WHERE ParentMapId = @metadataMapId; UPDATE MetadataMapBridge SET Preferred = @preferred WHERE ParentMapId = @metadataMapId AND MetadataSourceType = @sourceType;";
				db.ExecuteCMD(sql, dbDict);
				// ensure cache invalidated even if no other fields are changing
				DatabaseMemoryCache.RemoveCacheObject("MetadataMap_" + metadataMapId.ToString());
			}

			// only make changes to other fields if there is something to change
			if (whereClause == "")
			{
				return; // cache already cleared above if preferred was toggled
			}

			// update the metadata map item
			sql = $"UPDATE MetadataMapBridge SET {whereClause} WHERE ParentMapId = @metadataMapId AND MetadataSourceType = @sourceType;";
			db.ExecuteCMD(sql, dbDict);

			// clear the cache for this metadata map if present (covers non-preferred updates)
			DatabaseMemoryCache.RemoveCacheObject("MetadataMap_" + metadataMapId.ToString());

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
		private static async Task<MetadataMap?> GetMetadataMap(long platformId, string name)
		{
			// check the cache first
			long? cachedMetadataMap = (long?)DatabaseMemoryCache.GetCacheObject("MetadataMap_PlatformId_" + platformId.ToString() + "_Name_" + name.Trim());

			if (cachedMetadataMap != null)
			{
				return await GetMetadataMap((long)cachedMetadataMap);
			}

			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@platformId", platformId },
				{ "@name", name.Trim() }
			};
			DataTable dt = new DataTable();

			sql = "SELECT Id FROM MetadataMap WHERE PlatformId = @platformId AND SignatureGameName = @name;";
			dt = await db.ExecuteCMDAsync(sql, dbDict);

			if (dt.Rows.Count > 0)
			{
				// add to cache
				DatabaseMemoryCache.SetCacheObject("MetadataMap_PlatformId_" + platformId.ToString() + "_Name_" + name.Trim(), (long)dt.Rows[0]["Id"], 3600);

				return await GetMetadataMap((long)dt.Rows[0]["Id"]);
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
		public static async Task<MetadataMap?> GetMetadataMap(long metadataMapId)
		{
			// check the cache first
			MetadataMap? cachedMetadataMap = (MetadataMap?)DatabaseMemoryCache.GetCacheObject("MetadataMap_" + metadataMapId.ToString());
			if (cachedMetadataMap != null)
			{
				return cachedMetadataMap;
			}

			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@metadataMapId", metadataMapId }
			};

			string sql = "SELECT * FROM MetadataMap JOIN MetadataMapBridge ON MetadataMap.Id = MetadataMapBridge.ParentMapId WHERE Id = @metadataMapId;";
			DataTable dt = await db.ExecuteCMDAsync(sql, dbDict);

			if (dt.Rows.Count > 0)
			{
				MetadataMap metadataMap = new MetadataMap()
				{
					Id = (long)dt.Rows[0]["Id"],
					PlatformId = (long)dt.Rows[0]["PlatformId"],
					SignatureGameName = dt.Rows[0]["SignatureGameName"]?.ToString() ?? string.Empty,
					MetadataMapItems = new List<MetadataMap.MetadataMapItem>()
				};

				foreach (DataRow dr in dt.Rows)
				{
					MetadataMap.MetadataMapItem metadataMapItem = new MetadataMap.MetadataMapItem()
					{
						SourceType = (FileSignature.MetadataSources)dr["MetadataSourceType"],
						SourceId = (long)dr["MetadataSourceId"],
						Preferred = (bool)dr["Preferred"],
						AutomaticMetadataSourceId = dr["AutomaticMetadataSourceId"] == DBNull.Value ? null : (long?)dr["AutomaticMetadataSourceId"],
						IsManual = (bool)dr["IsManual"]
					};

					if (!BlockedMetadataSource.Contains(metadataMapItem.SourceType))
					{
						metadataMap.MetadataMapItems.Add(metadataMapItem);
					}
				}

				if (metadataMap.MetadataMapItems == null || metadataMap.MetadataMapItems.Count == 0)
				{
					throw new Exception("Metadata map has no metadata map items.");
				}

				// add to cache
				DatabaseMemoryCache.SetCacheObject("MetadataMap_" + metadataMapId.ToString(), metadataMap, 3600);

				return metadataMap;
			}

			return null;
		}

		/// <summary>
		/// Sets supplemental metadata for a metadata map (e.g., user manual link) and clears the related cache so future reads get updated values.
		/// </summary>
		/// <param name="metadataMapId">The unique identifier of the metadata map to update.</param>
		/// <param name="dataType">The type of support data being stored (currently only UserManualLink).</param>
		/// <param name="data">The value to store for the specified support data type.</param>
		/// <remarks>
		/// If the metadata map does not exist the method returns without making changes.
		/// Cache for the metadata map is cleared before and after the update to ensure consistency.
		/// </remarks>
		public static void SetMetadataSupportData(long metadataMapId, MetadataMapSupportDataTypes dataType, string data)
		{
			// verify the metadata map exists
			MetadataMap? metadataMap = GetMetadataMap(metadataMapId).Result;
			if (metadataMap == null)
			{
				return;
			}

			// clear the cache for this metadata map if present
			DatabaseMemoryCache.RemoveCacheObject("MetadataMap_" + metadataMapId.ToString());

			// update the metadata map
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

			// clear the cache for this metadata map if present
			DatabaseMemoryCache.RemoveCacheObject("MetadataMap_" + metadataMapId.ToString());
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
		public static long? GetMetadataMapIdFromSourceId(FileSignature.MetadataSources sourceType, long sourceId, bool preferred = true)
		{
			// check the cache first
			long? cachedMetadataMapId = (long?)DatabaseMemoryCache.GetCacheObject("MetadataMapId_" + sourceType.ToString() + "_" + sourceId.ToString() + "_" + preferred.ToString());
			if (cachedMetadataMapId != null)
			{
				return cachedMetadataMapId;
			}

			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@sourceType", sourceType },
				{ "@sourceId", sourceId },
				{ "@preferred", true }
			};
			DataTable dt = new DataTable();

			string preferredSql = preferred ? "AND Preferred = @preferred" : "";

			sql = "SELECT * FROM MetadataMapBridge WHERE MetadataSourceType = @sourceType AND MetadataSourceId = @sourceId " + preferredSql + ";";
			dt = db.ExecuteCMD(sql, dbDict);

			if (dt.Rows.Count > 0)
			{
				// add to cache
				DatabaseMemoryCache.SetCacheObject("MetadataMapId_" + sourceType.ToString() + "_" + sourceId.ToString() + "_" + preferred.ToString(), (long)dt.Rows[0]["ParentMapId"], 3600);

				return (long)dt.Rows[0]["ParentMapId"];
			}

			return null;
		}

		/// <summary>
		/// Get metadata map ids associated with the provided metadata map id
		/// </summary>
		/// <param name="metadataMapId"></param>
		/// <returns></returns>
		public static async Task<List<long>> GetAssociatedMetadataMapIds(long metadataMapId)
		{
			// check the cache first
			List<long>? cachedMetadataMap = (List<long>?)DatabaseMemoryCache.GetCacheObject("AssociatedMetadataMapIds_" + metadataMapId.ToString());
			if (cachedMetadataMap != null)
			{
				return cachedMetadataMap;
			}

			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@metadataMapId", metadataMapId }
			};

			sql = "SELECT `M1`.`Id` AS `Id` FROM `view_MetadataMap` `M` JOIN `view_MetadataMap` `M1` ON `M`.`MetadataSourceId` = `M1`.`MetadataSourceId` WHERE `M`.`Id` = @metadataMapId; ";
			DataTable dt = await db.ExecuteCMDAsync(sql, dbDict);

			List<long> metadataMapIds = new List<long>();
			foreach (DataRow dr in dt.Rows)
			{
				long associatedMetadataMapId = (long)dr["Id"];
				if (!metadataMapIds.Contains(associatedMetadataMapId))
				{
					metadataMapIds.Add(associatedMetadataMapId);
				}
			}

			// add to cache
			DatabaseMemoryCache.SetCacheObject("AssociatedMetadataMapIds_" + metadataMapId.ToString(), metadataMapIds, 3600);

			return metadataMapIds;
		}

		Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
		public async Task RefreshMetadata(bool forceRefresh = false)
		{
			// removed unused local variables sql/dt

			// disabling forceRefresh
			forceRefresh = false;

			// refresh platforms
			await RefreshPlatforms(forceRefresh);

			// refresh signatures
			await RefreshSignatures(forceRefresh);

			// update the rom counts
			UpdateRomCounts();

			// refresh games
			await RefreshGames(forceRefresh);
		}

		public async Task RefreshPlatforms(bool forceRefresh = false)
		{
			// update platform metadata
			string sql = "SELECT Id, `Name` FROM Metadata_Platform;";
			DataTable dt = await db.ExecuteCMDAsync(sql);

			int StatusCounter = 1;
			foreach (DataRow dr in dt.Rows)
			{
				SetStatus(StatusCounter, dt.Rows.Count, "Refreshing metadata for platform " + dr["name"]);

				try
				{
					Logging.Log(Logging.LogType.Information, "Metadata Refresh", "(" + StatusCounter + "/" + dt.Rows.Count + "): Refreshing metadata for platform " + dr["name"] + " (" + dr["id"] + ")");

					FileSignature.MetadataSources metadataSource = FileSignature.MetadataSources.None;

					// fetch the platform metadata
					Platform? platform = await Metadata.Platforms.GetPlatform((long)dr["id"], metadataSource);

					// fetch the platform metadata from Hasheous
					if ((long)dr["id"] != 0)
					{
						if (Config.MetadataConfiguration.SignatureSource == HasheousClient.Models.MetadataModel.SignatureSources.Hasheous)
						{

							await Communications.PopulateHasheousPlatformData((long)dr["id"]);
						}
					}
					else
					{
						// set the platform to unknown
						sql = "UPDATE Metadata_Platform SET Name = 'Unknown Platform', Slug = 'unknown', PlatformLogo = 0 WHERE Id = 0;";
						await db.ExecuteCMDAsync(sql);
					}

					// force platformLogo refresh
					if (platform != null && platform.PlatformLogo != null)
					{
						await Metadata.PlatformLogos.GetPlatformLogo(platform.PlatformLogo, metadataSource);
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

		public async Task RefreshSignatures(bool forceRefresh = false)
		{
			// update rom signatures - only valid if Haseheous is enabled
			if (Config.MetadataConfiguration.SignatureSource == MetadataModel.SignatureSources.Hasheous)
			{
				// get all ROMs in the database
				string sql = "SELECT * FROM view_Games_Roms WHERE DateUpdated < @LastUpdateThreshold;";
				// set @LastUpdateThreshold to a random date between 14 and 30 days in the past
				Dictionary<string, object> dbDict = new Dictionary<string, object>()
				{
					{ "LastUpdateThreshold", DateTime.UtcNow.AddDays(-new Random().Next(14, 30)) }
				};
				if (forceRefresh == true)
				{
					// set the LastUpdateThreshold to the current time
					dbDict["LastUpdateThreshold"] = DateTime.UtcNow; // force refresh all ROMs

					// clear hasheous cache
					string hasheousCachePath = Config.LibraryConfiguration.LibraryMetadataDirectory_Hasheous();
					// delete all *.json files in the hasheous cache directory
					if (Directory.Exists(hasheousCachePath))
					{
						foreach (string file in Directory.GetFiles(hasheousCachePath, "*.json"))
						{
							try
							{
								File.Delete(file);
							}
							catch (Exception ex)
							{
								Logging.Log(Logging.LogType.Warning, "Metadata Refresh", "Failed to delete file " + file + " from Hasheous cache directory", ex);
							}
						}
					}
				}
				DataTable dt = await db.ExecuteCMDAsync(sql, dbDict);

				int StatusCounter = 1;
				foreach (DataRow dr in dt.Rows)
				{
					SetStatus(StatusCounter, dt.Rows.Count, "Refreshing signature for ROM " + dr["Name"]);

					try
					{
						Logging.Log(Logging.LogType.Information, "Metadata Refresh", "(" + StatusCounter + "/" + dt.Rows.Count + "): Refreshing signature for ROM " + dr["Name"] + " (" + dr["Id"] + ")");

						// get the hash of the ROM from the datarow
						string? md5 = dr["MD5"] == DBNull.Value ? null : dr["MD5"].ToString();
						string? sha1 = dr["SHA1"] == DBNull.Value ? null : dr["SHA1"].ToString();
						string? sha256 = dr["SHA256"] == DBNull.Value ? null : dr["SHA256"].ToString();
						string? crc = dr["CRC"] == DBNull.Value ? null : dr["CRC"].ToString();
						HashObject hash = new HashObject();
						if (
							md5 != null && md5 != "" &&
							sha1 != null && sha1 != "" &&
							sha256 != null && sha256 != "" &&
							crc != null && crc != ""
						)
						{
							if (md5 != null)
							{
								hash.md5hash = md5;
							}
							if (sha1 != null)
							{
								hash.sha1hash = sha1;
							}
							if (sha256 != null)
							{
								hash.sha256hash = sha256;
							}
							if (crc != null)
							{
								hash.crc32hash = crc;
							}
						}
						else
						{
							Logging.Log(Logging.LogType.Information, "Metadata Refresh", "Missing one or more hashes for " + dr["Name"] + " - recalculating hashes");
							hash = new HashObject(dr["Path"].ToString());
						}

						// get the library for the ROM
						GameLibrary.LibraryItem library = await GameLibrary.GetLibrary((int)dr["LibraryId"]);

						// get the signature for the ROM
						FileInfo fi = new FileInfo(dr["Path"].ToString());
						FileSignature fileSignature = new FileSignature();
						gaseous_server.Models.Signatures_Games signature = await fileSignature.GetFileSignatureAsync(library, hash, fi, fi.FullName);

						// validate the signature - if it is invalid, skip the rest of the loop
						// validation rules: 1) signature must not be null, 2) signature must have a platform ID
						if (signature == null || signature.Flags.PlatformId == null)
						{
							Logging.Log(Logging.LogType.Information, "Metadata Refresh", "Signature for " + dr["Name"] + " is invalid - skipping metadata refresh");
							StatusCounter += 1;
							continue;
						}

						// update the signature in the database
						Platform? signaturePlatform = await Metadata.Platforms.GetPlatform((long)signature.Flags.PlatformId);
						if (signature.Flags.GameId == 0)
						{
							HasheousClient.Models.Metadata.IGDB.Game? discoveredGame = await ImportGame.SearchForGame(signature, signature.Flags.PlatformId, false);
							if (discoveredGame != null && discoveredGame.Id != null)
							{
								signature.MetadataSources.AddGame((long)discoveredGame.Id, discoveredGame.Name, FileSignature.MetadataSources.IGDB);
							}
						}
						await ImportGame.StoreGame(library, hash, signature, signaturePlatform, fi.FullName, (long)dr["Id"], false);
					}
					catch (Exception ex)
					{
						Logging.Log(Logging.LogType.Critical, "Metadata Refresh", "An error occurred while refreshing metadata for " + dr["Name"], ex);
					}

					StatusCounter += 1;
				}
				ClearStatus();
			}
		}

		public async Task RefreshGames(bool forceRefresh = false)
		{
			string sql = "";

			// update game metadata
			sql = "SELECT DISTINCT `Id`, `SignatureGameName` AS `Name` FROM gaseous.view_MetadataMap;";
			DataTable dt = await db.ExecuteCMDAsync(sql);

			int StatusCounter = 1;
			foreach (DataRow dr in dt.Rows)
			{
				SetStatus(StatusCounter, dt.Rows.Count, "Refreshing metadata for game " + dr["name"]);

				MetadataMap? metadataMap = await GetMetadataMap((long)dr["Id"]);
				if (metadataMap != null)
				{
					await _RefreshSpecificGame(metadataMap, forceRefresh);
				}

				StatusCounter += 1;
			}
			ClearStatus();
		}

		static List<Dictionary<string, object>> inProgressRefreshes = new List<Dictionary<string, object>>();
		public async Task RefreshSpecificGameAsync(long metadataMapId)
		{
			// get the metadata map for the game
			string sql = @"
			SELECT DISTINCT `Id`, SignatureGameName AS `Name` FROM gaseous.view_MetadataMap WHERE `Id` = @metadataMapId;
			";
			Dictionary<string, object> dbDict = new Dictionary<string, object>()
			{
				{ "@metadataMapId", metadataMapId }
			};
			DataTable dt = await db.ExecuteCMDAsync(sql, dbDict);

			foreach (DataRow dr in dt.Rows)
			{
				// check if the game is already in progress
				if (inProgressRefreshes.Any(x => x["Id"].ToString() == dr["Id"].ToString()))
				{
					Logging.Log(Logging.LogType.Information, "Metadata Refresh", "Skipping metadata refresh for game " + dr["Name"] + " (" + dr["Id"] + ") - already in progress");
					continue;
				}

				// add the game to the in progress list
				inProgressRefreshes.Add(new Dictionary<string, object>()
				{
					{ "Id", dr["Id"] },
					// { "GameIdType", dr["GameIdType"] },
					{ "Name", dr["Name"] }
				});

				// refresh the game metadata
				MetadataMap? metadataMap = await GetMetadataMap((long)dr["Id"]);
				if (metadataMap != null)
				{
					await _RefreshSpecificGame(metadataMap, false);
				}

				// remove the game from the in progress list
				inProgressRefreshes.RemoveAll(x => x["Id"].ToString() == dr["Id"].ToString());
			}
		}

		async Task _RefreshSpecificGame(MetadataMap metadataItem, bool forceRefresh)
		{
			if (metadataItem == null)
			{
				Logging.Log(Logging.LogType.Warning, "Metadata Refresh", "Metadata item is null - skipping refresh");
				return;
			}

			if (metadataItem.MetadataMapItems == null || metadataItem.MetadataMapItems.Count == 0)
			{
				Logging.Log(Logging.LogType.Warning, "Metadata Refresh", "Metadata item has no metadata map items - skipping refresh");
				return;
			}

			foreach (MetadataMap.MetadataMapItem item in metadataItem.MetadataMapItems)
			{
				// skip unsupported metadata sources
				List<FileSignature.MetadataSources> BlockedMetadataSource = new List<FileSignature.MetadataSources>
				{
					FileSignature.MetadataSources.None,
					FileSignature.MetadataSources.RetroAchievements
				};
				if (BlockedMetadataSource.Contains(item.SourceType))
				{
					continue;
				}

				Logging.Log(Logging.LogType.Information, "Metadata Refresh", "Refreshing metadata for game " + metadataItem.SignatureGameName + " (" + metadataItem.Id + ") using source " + item.SourceType.ToString() + " with source id " + item.SourceId);
				Models.Game? game = await Metadata.Games.GetGame(item.SourceType, item.SourceId, true, forceRefresh);

				// get supporting metadata
				if (game != null)
				{
					if (game.AgeRatings != null)
					{
						foreach (long ageRatingId in game.AgeRatings)
						{
							AgeRating? ageRating = await Metadata.AgeRatings.GetAgeRating(item.SourceType, ageRatingId);
							if (ageRating != null)
							{
								await Metadata.AgeRatingOrganizations.GetAgeRatingOrganization(item.SourceType, (long)ageRating.Organization);
								if (ageRating.RatingCategory != null)
								{
									await Metadata.AgeRatingCategorys.GetAgeRatingCategory(item.SourceType, (long)ageRating.RatingCategory);
								}
								if (ageRating.RatingContentDescriptions != null)
								{
									foreach (long ageRatingContentDescriptionId in ageRating.RatingContentDescriptions)
									{
										await Metadata.AgeRatingContentDescriptionsV2.GetAgeRatingContentDescriptionsV2(item.SourceType, ageRatingContentDescriptionId);
									}
								}
							}
						}
					}
					if (game.AlternativeNames != null)
					{
						foreach (long alternateNameId in game.AlternativeNames)
						{
							await Metadata.AlternativeNames.GetAlternativeNames(item.SourceType, alternateNameId);
						}
					}
					if (game.Artworks != null)
					{
						foreach (long artworkId in game.Artworks)
						{
							await Metadata.Artworks.GetArtwork(item.SourceType, artworkId);
							await ImageHandling.GameImage((long)game.MetadataMapId, item.SourceType, ImageHandling.MetadataImageType.artwork, artworkId, Communications.IGDBAPI_ImageSize.original);
						}
					}
					if (game.Cover != null)
					{
						await Metadata.Covers.GetCover(item.SourceType, (long?)game.Cover);
						await ImageHandling.GameImage((long)game.MetadataMapId, item.SourceType, ImageHandling.MetadataImageType.cover, game.Cover, Communications.IGDBAPI_ImageSize.original);
					}
					if (game.GameModes != null)
					{
						foreach (long gameModeId in game.GameModes)
						{
							await Metadata.GameModes.GetGame_Modes(item.SourceType, gameModeId);
						}
					}
					if (game.Genres != null)
					{
						foreach (long genreId in game.Genres)
						{
							await Metadata.Genres.GetGenres(item.SourceType, genreId);
						}
					}
					if (game.GameLocalizations != null)
					{
						foreach (long gameLocalizationId in game.GameLocalizations)
						{
							GameLocalization? gameLocalization = await Metadata.GameLocalizations.GetGame_Locatization(item.SourceType, gameLocalizationId);

							if (gameLocalization != null)
							{
								if (gameLocalization.Cover != null)
								{
									await Metadata.Covers.GetCover(item.SourceType, (long)gameLocalization.Cover);
								}

								if (gameLocalization.Region != null)
								{
									await Metadata.Regions.GetGame_Region(item.SourceType, (long)gameLocalization.Region);
								}
							}
						}
					}
					if (game.Videos != null)
					{
						foreach (long gameVideoId in game.Videos)
						{
							await Metadata.GamesVideos.GetGame_Videos(item.SourceType, gameVideoId);
						}
					}
					if (game.MultiplayerModes != null)
					{
						foreach (long multiplayerModeId in game.MultiplayerModes)
						{
							await Metadata.MultiplayerModes.GetGame_MultiplayerModes(item.SourceType, multiplayerModeId);
						}
					}
					if (game.PlayerPerspectives != null)
					{
						foreach (long playerPerspectiveId in game.PlayerPerspectives)
						{
							await Metadata.PlayerPerspectives.GetGame_PlayerPerspectives(item.SourceType, playerPerspectiveId);
						}
					}
					if (game.ReleaseDates != null)
					{
						foreach (long releaseDateId in game.ReleaseDates)
						{
							await Metadata.ReleaseDates.GetReleaseDates(item.SourceType, releaseDateId);
						}
					}
					if (game.Screenshots != null)
					{
						foreach (long screenshotId in game.Screenshots)
						{
							await Metadata.Screenshots.GetScreenshotAsync(item.SourceType, screenshotId);
							await ImageHandling.GameImage((long)game.MetadataMapId, item.SourceType, ImageHandling.MetadataImageType.screenshots, screenshotId, Communications.IGDBAPI_ImageSize.original);
						}
					}
					if (game.Themes != null)
					{
						foreach (long themeId in game.Themes)
						{
							await Metadata.Themes.GetGame_ThemesAsync(item.SourceType, themeId);
						}
					}
				}
			}
		}

		/// <summary>
		/// Updates the ROM counts for each metadata map in the database.
		/// </summary>
		public void UpdateRomCounts()
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

