using System;
using System.Data;
using gaseous_signature_parser.models.RomSignatureObject;
using Microsoft.VisualBasic;
using IGDB.Models;
using gaseous_server.Classes.Metadata;
using System.IO.Compression;

namespace gaseous_server.Classes
{
	public class RomMediaGroup
    {
        public class InvalidMediaGroupId : Exception
        { 
            public InvalidMediaGroupId(long Id) : base("Unable to find media group by id " + Id)
            {}
        }

        public static GameRomMediaGroupItem CreateMediaGroup(long GameId, long PlatformId, List<long> RomIds)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO RomMediaGroup (Status, PlatformId, GameId) VALUES (@status, @platformid, @gameid); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("status", GameRomMediaGroupItem.GroupBuildStatus.WaitingForBuild);
            dbDict.Add("gameid", GameId);
            dbDict.Add("platformid", PlatformId);
            DataTable mgInsert = db.ExecuteCMD(sql, dbDict);
            long mgId = (long)mgInsert.Rows[0][0];
            foreach (long RomId in RomIds)
            {
                try
                {
                    Roms.GameRomItem gameRomItem = Roms.GetRom(RomId);
                    if (gameRomItem.PlatformId == PlatformId)
                    {
                        sql = "INSERT INTO RomMediaGroup_Members (GroupId, RomId) VALUES (@groupid, @romid);";
                        dbDict.Clear();
                        dbDict.Add("groupid", mgId);
                        dbDict.Add("romid", RomId);
                        db.ExecuteCMD(sql, dbDict);
                    }
                    else
                    {
                        Logging.Log(Logging.LogType.Warning, "Media Group", "Unable to add ROM id " + RomId + " to group. ROM platform is different from group platform.");
                    }
                }
                catch (Roms.InvalidRomId irid)
                {
                    Logging.Log(Logging.LogType.Warning, "Media Group", "Unable to add ROM id " + RomId + " to group. ROM doesn't exist", irid);
                }
            }

            StartMediaGroupBuild(mgId);

            return GetMediaGroup(mgId);
        }

        public static GameRomMediaGroupItem GetMediaGroup(long Id)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT DISTINCT RomMediaGroup.*, GameState.RomId AS GameStateRomId FROM gaseous.RomMediaGroup LEFT JOIN GameState ON RomMediaGroup.Id = GameState.RomId AND GameState.IsMediaGroup = 1 WHERE RomMediaGroup.Id=@id;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", Id);

            DataTable dataTable = db.ExecuteCMD(sql, dbDict);

            if (dataTable.Rows.Count == 0)
            {
                throw new InvalidMediaGroupId(Id);
            }
            else
            {
                GameRomMediaGroupItem mediaGroupItem = BuildMediaGroupFromRow(dataTable.Rows[0]);
                return mediaGroupItem;
            }
        }

        public static List<GameRomMediaGroupItem> GetMediaGroupsFromGameId(long GameId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT DISTINCT RomMediaGroup.*, GameState.RomId AS GameStateRomId FROM gaseous.RomMediaGroup LEFT JOIN GameState ON RomMediaGroup.Id = GameState.RomId AND GameState.IsMediaGroup = 1 WHERE RomMediaGroup.GameId=@gameid;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("gameid", GameId);

            DataTable dataTable = db.ExecuteCMD(sql, dbDict);

            List<GameRomMediaGroupItem> mediaGroupItems = new List<GameRomMediaGroupItem>();

            foreach (DataRow row in dataTable.Rows)
            {
                mediaGroupItems.Add(BuildMediaGroupFromRow(row));
            }

            mediaGroupItems.Sort((x, y) => x.Platform.CompareTo(y.Platform));

            return mediaGroupItems;
        }

        public static GameRomMediaGroupItem EditMediaGroup(long Id, List<long> RomIds)
        {
            GameRomMediaGroupItem mg = GetMediaGroup(Id);

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            // delete roms from group
            sql = "DELETE FROM RomMediaGroup_Members WHERE GroupId=@groupid;";
            dbDict.Clear();
            dbDict.Add("groupid", Id);
            db.ExecuteCMD(sql, dbDict);

            // add roms to group
            foreach (long RomId in RomIds)
            {
                try
                {
                    Roms.GameRomItem gameRomItem = Roms.GetRom(RomId);
                    if (gameRomItem.PlatformId == mg.PlatformId)
                    {
                        sql = "INSERT INTO RomMediaGroup_Members (GroupId, RomId) VALUES (@groupid, @romid);";
                        dbDict.Clear();
                        dbDict.Add("groupid", Id);
                        dbDict.Add("romid", RomId);
                        db.ExecuteCMD(sql, dbDict);
                    }
                    else
                    {
                        Logging.Log(Logging.LogType.Warning, "Media Group", "Unable to add ROM id " + RomId + " to group. ROM platform is different from group platform.");
                    }
                }
                catch (Roms.InvalidRomId irid)
                {
                    Logging.Log(Logging.LogType.Warning, "Media Group", "Unable to add ROM id " + RomId + " to group. ROM doesn't exist", irid);
                }
            }

            // set group to rebuild
            sql = "UPDATE RomMediaGroup SET Status=1 WHERE GroupId=@groupid;";
            dbDict.Clear();
            dbDict.Add("groupid", Id);
            db.ExecuteCMD(sql, dbDict);

            string MediaGroupZipPath = Path.Combine(Config.LibraryConfiguration.LibraryMediaGroupDirectory, Id + ".zip");
            if (File.Exists(MediaGroupZipPath))
            {
                File.Delete(MediaGroupZipPath);
            }

            StartMediaGroupBuild(Id);

            // return to caller
            return GetMediaGroup(Id);
        }

        public static void DeleteMediaGroup(long Id)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM RomMediaGroup WHERE Id=@id; DELETE FROM GameState WHERE RomId=@id AND IsMediaGroup=1;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", Id);
            db.ExecuteCMD(sql, dbDict);

            string MediaGroupZipPath = Path.Combine(Config.LibraryConfiguration.LibraryMediaGroupDirectory, Id + ".zip");
            if (File.Exists(MediaGroupZipPath))
            {
                File.Delete(MediaGroupZipPath);
            }
        }

        internal static GameRomMediaGroupItem BuildMediaGroupFromRow(DataRow row)
        {
            bool hasSaveStates = false;
			if (row.Table.Columns.Contains("GameStateRomId"))
			{
				if (row["GameStateRomId"] != DBNull.Value)
				{
					hasSaveStates = true;
				}
			}

            GameRomMediaGroupItem mediaGroupItem = new GameRomMediaGroupItem();
            mediaGroupItem.Id = (long)row["Id"];
            mediaGroupItem.Status = (GameRomMediaGroupItem.GroupBuildStatus)row["Status"];
            mediaGroupItem.PlatformId = (long)row["PlatformId"];
            mediaGroupItem.GameId = (long)row["GameId"];
            mediaGroupItem.RomIds = new List<long>();
            mediaGroupItem.Roms = new List<Roms.GameRomItem>();
            mediaGroupItem.HasSaveStates = hasSaveStates;

            // get members
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM RomMediaGroup_Members WHERE GroupId=@id;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", mediaGroupItem.Id);
            DataTable data = db.ExecuteCMD(sql, dbDict);
            foreach (DataRow dataRow in data.Rows)
            {
                mediaGroupItem.RomIds.Add((long)dataRow["RomId"]);
                try
                {
                    mediaGroupItem.Roms.Add(Roms.GetRom((long)dataRow["RomId"]));
                }
                catch (Exception ex)
                {
                    Logging.Log(Logging.LogType.Warning, "Rom Group", "Unable to load ROM data", ex);
                }
            }

            // check for a web emulator and update the romItem
			foreach (Models.PlatformMapping.PlatformMapItem platformMapping in Models.PlatformMapping.PlatformMap)
			{
				if (platformMapping.IGDBId == mediaGroupItem.PlatformId)
				{
					if (platformMapping.WebEmulator != null)
					{
						mediaGroupItem.Emulator = platformMapping.WebEmulator;
					}
				}
			}

            return mediaGroupItem;
        }

        public static void StartMediaGroupBuild(long Id)
        {
            GameRomMediaGroupItem mediaGroupItem = GetMediaGroup(Id);

            if (mediaGroupItem.Status != GameRomMediaGroupItem.GroupBuildStatus.Building)
            {
                // set collection item to waitingforbuild
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "UPDATE RomMediaGroup SET Status=@bs WHERE Id=@id";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("id", Id);
                dbDict.Add("bs", GameRomMediaGroupItem.GroupBuildStatus.WaitingForBuild);
                db.ExecuteCMD(sql, dbDict);

                // start background task
                ProcessQueue.QueueItem queueItem = new ProcessQueue.QueueItem(ProcessQueue.QueueItemType.MediaGroupCompiler, 1, false, true);
                queueItem.Options = Id;
                queueItem.ForceExecute();
                ProcessQueue.QueueItems.Add(queueItem);
            }
        }

        public static void CompileMediaGroup(long Id)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            GameRomMediaGroupItem mediaGroupItem = GetMediaGroup(Id);
            if (mediaGroupItem.Status == GameRomMediaGroupItem.GroupBuildStatus.WaitingForBuild)
            {
                Game GameObject = Games.GetGame(mediaGroupItem.GameId, false, false, false);
                Platform PlatformObject = Platforms.GetPlatform(mediaGroupItem.PlatformId, false);

                Logging.Log(Logging.LogType.Information, "Media Group", "Beginning build of media group: " + GameObject.Name + " for platform " + PlatformObject.Name);

                // set starting
                string sql = "UPDATE RomMediaGroup SET Status=@bs WHERE Id=@id";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("id", mediaGroupItem.Id);
                dbDict.Add("bs", GameRomMediaGroupItem.GroupBuildStatus.Building);
                db.ExecuteCMD(sql, dbDict);

                string ZipFilePath = Path.Combine(Config.LibraryConfiguration.LibraryMediaGroupDirectory, mediaGroupItem.Id + ".zip");
                string ZipFileTempPath = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, mediaGroupItem.Id.ToString());

                try
                {
                    // clean up if needed
                    if (File.Exists(ZipFilePath))
                    {
                        Logging.Log(Logging.LogType.Warning, "Media Group", "Deleting existing build of media group: " + GameObject.Name + " for platform " + PlatformObject.Name);
                        File.Delete(ZipFilePath);
                    }

                    if (Directory.Exists(ZipFileTempPath))
                    {
                        Directory.Delete(ZipFileTempPath, true);
                    }

                    // gather media group files
                    Directory.CreateDirectory(ZipFileTempPath);
                    List<Roms.GameRomItem> romItems = new List<Roms.GameRomItem>();
                    List<string> M3UFileContents = new List<string>();
                    foreach (long RomId in mediaGroupItem.RomIds)
                    {
                        Roms.GameRomItem rom = Roms.GetRom(RomId);
                        if (File.Exists(rom.Path))
                        {
                            Logging.Log(Logging.LogType.Information, "Media Group", "Copying ROM: " + rom.Name);
                            File.Copy(rom.Path, Path.Combine(ZipFileTempPath, Path.GetFileName(rom.Path)));

                            romItems.Add(rom);
                        }
                    }

                    // build m3u
                    romItems.Sort((a, b) =>
                        {
                            if (a.MediaDetail != null)
                            {
                                if (a.MediaDetail.Number != null && a.MediaDetail.Side != null)
                                {
                                    var firstCompare = a.MediaDetail.Number.ToString().CompareTo(b.MediaDetail.Number.ToString());
                                    return firstCompare != 0 ? firstCompare : a.MediaDetail.Side.CompareTo(b.MediaDetail.Side);
                                }
                                else if (a.MediaDetail.Number != null && a.MediaDetail.Side == null)
                                {
                                    return a.MediaDetail.Number.ToString().CompareTo(b.MediaDetail.Number.ToString());
                                }
                                else if (a.MediaDetail.Number == null && a.MediaDetail.Side != null)
                                {
                                    return a.MediaDetail.Side.ToString().CompareTo(b.MediaDetail.Side.ToString());
                                }
                                else
                                {
                                    return a.Name.CompareTo(b.Name);
                                }
                            }
                            else
                            {
                                return a.Name.CompareTo(b.Name);
                            }
                        }
                    );
                    foreach (Roms.GameRomItem romItem in romItems)
                    {
                        string M3UFileContent = "";
                        M3UFileContent += romItem.Name;
                        if (romItem.MediaLabel.Length == 0)
                        {
                            if (romItem.RomTypeMedia.Length > 0)
                            {
                                M3UFileContent += "|" + romItem.RomTypeMedia;
                            }
                        }
                        else
                        {
                            M3UFileContent += "|" + romItem.MediaLabel;
                        }
                        M3UFileContents.Add(M3UFileContent);
                    }

                    File.WriteAllText(Path.Combine(ZipFileTempPath, GameObject.Name + ".m3u"), String.Join(Environment.NewLine, M3UFileContents));

                    // compress to zip
                    Logging.Log(Logging.LogType.Information, "Media Group", "Compressing media group");
                    if (!Directory.Exists(Config.LibraryConfiguration.LibraryMediaGroupDirectory))
                    {
                        Directory.CreateDirectory(Config.LibraryConfiguration.LibraryMediaGroupDirectory);
                    }
                    ZipFile.CreateFromDirectory(ZipFileTempPath, ZipFilePath, CompressionLevel.SmallestSize, false);

                    // clean up
                    if (Directory.Exists(ZipFileTempPath))
                    {
                        Logging.Log(Logging.LogType.Information, "Media Group", "Cleaning up");
                        Directory.Delete(ZipFileTempPath, true);
                    }

                    // set completed
                    dbDict["bs"] = GameRomMediaGroupItem.GroupBuildStatus.Completed;
                    db.ExecuteCMD(sql, dbDict);
                }
                catch (Exception ex)
                {
                    // clean up
                    if (Directory.Exists(ZipFileTempPath))
                    {
                        Directory.Delete(ZipFileTempPath, true);
                    }

                    if (File.Exists(ZipFilePath))
                    {
                        File.Delete(ZipFilePath);
                    }

                    // set failed
                    dbDict["bs"] = GameRomMediaGroupItem.GroupBuildStatus.Failed;
                    db.ExecuteCMD(sql, dbDict);

                    Logging.Log(Logging.LogType.Critical, "Media Group", "Media Group building has failed", ex);
                }
            }
        }

        public class GameRomMediaGroupItem
		{
			public long Id { get; set; }
			public long GameId { get; set; }
			public long PlatformId { get; set; }
            public string Platform {
                get
                {
                    try
                    {
                        return Platforms.GetPlatform(PlatformId, false).Name;
                    }
                    catch
                    {
                        return "Unknown";
                    }
                }
            }
            public Models.PlatformMapping.PlatformMapItem.WebEmulatorItem? Emulator { get; set; }
			public List<long> RomIds { get; set; }
            public List<Roms.GameRomItem> Roms { get; set; }
            public bool HasSaveStates { get; set; } = false;
			private GroupBuildStatus _Status { get; set; }
			public GroupBuildStatus Status {
				get
				{
					if (_Status == GroupBuildStatus.Completed)
					{
						if (File.Exists(MediaGroupZipPath))
						{
							return GroupBuildStatus.Completed;
						}
						else
						{
							return GroupBuildStatus.NoStatus;
						}
					}
					else
					{
						return _Status;
					}
				}
                set
                {
                    _Status = value;
                }
			}
			public long? Size {
				get
                {
                    if (Status == GroupBuildStatus.Completed)
                    {
                        if (File.Exists(MediaGroupZipPath))
                        {
                            FileInfo fi = new FileInfo(MediaGroupZipPath);
                            return fi.Length;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
			}
			internal string MediaGroupZipPath
			{
				get
				{
					return Path.Combine(Config.LibraryConfiguration.LibraryMediaGroupDirectory, Id + ".zip");
				}
			}
			public enum GroupBuildStatus
            {
                NoStatus = 0,
                WaitingForBuild = 1,
                Building = 2,
                Completed = 3,
                Failed = 4
            }
		}
    }
}