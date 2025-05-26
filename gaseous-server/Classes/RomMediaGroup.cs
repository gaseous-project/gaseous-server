using System;
using System.Data;
using gaseous_signature_parser.models.RomSignatureObject;
using Microsoft.VisualBasic;
using gaseous_server.Classes.Metadata;
using System.IO.Compression;
using SharpCompress.Archives;
using SharpCompress.Common;
using gaseous_server.Models;
using HasheousClient.Models.Metadata.IGDB;
using System.Threading.Tasks;

namespace gaseous_server.Classes
{
    public class RomMediaGroup
    {
        public class InvalidMediaGroupId : Exception
        {
            public InvalidMediaGroupId(long Id) : base("Unable to find media group by id " + Id)
            { }
        }

        public static async Task<GameRomMediaGroupItem> CreateMediaGroup(long GameId, long PlatformId, List<long> RomIds)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO RomMediaGroup (Status, PlatformId, GameId) VALUES (@status, @platformid, @gameid); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("status", GameRomMediaGroupItem.GroupBuildStatus.WaitingForBuild);
            dbDict.Add("gameid", GameId);
            dbDict.Add("platformid", PlatformId);
            DataTable mgInsert = await db.ExecuteCMDAsync(sql, dbDict);
            long mgId = (long)mgInsert.Rows[0][0];
            foreach (long RomId in RomIds)
            {
                try
                {
                    Roms.GameRomItem gameRomItem = await Roms.GetRom(RomId);
                    if (gameRomItem.PlatformId == PlatformId)
                    {
                        sql = "INSERT INTO RomMediaGroup_Members (GroupId, RomId) VALUES (@groupid, @romid);";
                        dbDict.Clear();
                        dbDict.Add("groupid", mgId);
                        dbDict.Add("romid", RomId);
                        await db.ExecuteCMDAsync(sql, dbDict);
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

            await StartMediaGroupBuild(mgId);

            return await GetMediaGroupAsync(mgId);
        }

        public static async Task<GameRomMediaGroupItem> GetMediaGroupAsync(long Id, string userid = "")
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT DISTINCT RomMediaGroup.*, GameState.RomId AS GameStateRomId FROM gaseous.RomMediaGroup LEFT JOIN GameState ON RomMediaGroup.Id = GameState.RomId AND GameState.IsMediaGroup = 1 AND GameState.UserId = @userid WHERE RomMediaGroup.Id=@id;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "id", Id },
                { "userid", userid }
            };

            DataTable dataTable = await db.ExecuteCMDAsync(sql, dbDict);

            if (dataTable.Rows.Count == 0)
            {
                throw new InvalidMediaGroupId(Id);
            }
            else
            {
                GameRomMediaGroupItem mediaGroupItem = await BuildMediaGroupFromRowAsync(dataTable.Rows[0]);
                return mediaGroupItem;
            }
        }

        public static async Task<List<GameRomMediaGroupItem>> GetMediaGroupsFromGameId(long GameId, string userid = "", long? PlatformId = null)
        {
            string PlatformWhereClause = "";
            if (PlatformId != null)
            {
                PlatformWhereClause = " AND RomMediaGroup.PlatformId=@platformid";
            }

            string UserFields = "";
            string UserJoin = "";
            if (userid.Length > 0)
            {
                UserFields = ", User_RecentPlayedRoms.RomId AS MostRecentRomId, User_RecentPlayedRoms.IsMediaGroup AS MostRecentRomIsMediaGroup, User_GameFavouriteRoms.RomId AS FavouriteRomId, User_GameFavouriteRoms.IsMediaGroup AS FavouriteRomIsMediaGroup";
                UserJoin = @"
					LEFT JOIN
				User_RecentPlayedRoms ON User_RecentPlayedRoms.UserId = @userid
					AND User_RecentPlayedRoms.GameId = RomMediaGroup.GameId
					AND User_RecentPlayedRoms.PlatformId = RomMediaGroup.PlatformId
                    AND User_RecentPlayedRoms.RomId = RomMediaGroup.Id
					AND User_RecentPlayedRoms.IsMediaGroup = 1
					LEFT JOIN
				User_GameFavouriteRoms ON User_GameFavouriteRoms.UserId = @userid
					AND User_GameFavouriteRoms.GameId = RomMediaGroup.GameId
					AND User_GameFavouriteRoms.PlatformId = RomMediaGroup.PlatformId
                    AND User_GameFavouriteRoms.RomId = RomMediaGroup.Id
					AND User_GameFavouriteRoms.IsMediaGroup = 1
				";
            }

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT DISTINCT RomMediaGroup.*, GameState.RomId AS GameStateRomId" + UserFields + " FROM gaseous.RomMediaGroup LEFT JOIN GameState ON RomMediaGroup.Id = GameState.RomId AND GameState.IsMediaGroup = 1 AND GameState.UserId = @userid " + UserJoin + " WHERE RomMediaGroup.GameId=@gameid" + PlatformWhereClause + ";";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "gameid", GameId },
                { "userid", userid },
                { "platformid", PlatformId }
            };

            DataTable dataTable = await db.ExecuteCMDAsync(sql, dbDict);

            List<GameRomMediaGroupItem> mediaGroupItems = new List<GameRomMediaGroupItem>();

            foreach (DataRow row in dataTable.Rows)
            {
                mediaGroupItems.Add(await BuildMediaGroupFromRowAsync(row));
            }

            mediaGroupItems.Sort((x, y) => x.Platform.CompareTo(y.Platform));

            return mediaGroupItems;
        }

        public static async Task<GameRomMediaGroupItem> EditMediaGroupAsync(long Id, List<long> RomIds)
        {
            GameRomMediaGroupItem mg = await GetMediaGroupAsync(Id);

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();

            // delete roms from group
            sql = "DELETE FROM RomMediaGroup_Members WHERE GroupId=@groupid;";
            dbDict.Clear();
            dbDict.Add("groupid", Id);
            await db.ExecuteCMDAsync(sql, dbDict);

            // add roms to group
            foreach (long RomId in RomIds)
            {
                try
                {
                    Roms.GameRomItem gameRomItem = await Roms.GetRom(RomId);
                    if (gameRomItem.PlatformId == mg.PlatformId)
                    {
                        sql = "INSERT INTO RomMediaGroup_Members (GroupId, RomId) VALUES (@groupid, @romid);";
                        dbDict.Clear();
                        dbDict.Add("groupid", Id);
                        dbDict.Add("romid", RomId);
                        await db.ExecuteCMDAsync(sql, dbDict);
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
            await db.ExecuteCMDAsync(sql, dbDict);

            string MediaGroupZipPath = Path.Combine(Config.LibraryConfiguration.LibraryMediaGroupDirectory, Id + ".zip");
            if (File.Exists(MediaGroupZipPath))
            {
                File.Delete(MediaGroupZipPath);
            }

            await StartMediaGroupBuild(Id);

            // return to caller
            return await GetMediaGroupAsync(Id);
        }

        public static void DeleteMediaGroup(long Id)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM RomMediaGroup WHERE Id=@id; DELETE FROM GameState WHERE RomId=@id AND IsMediaGroup=1; DELETE FROM User_GameFavouriteRoms WHERE RomId = @id AND IsMediaGroup = 1; DELETE FROM User_RecentPlayedRoms WHERE RomId = @id AND IsMediaGroup = 1; UPDATE UserTimeTracking SET PlatformId = NULL, IsMediaGroup = NULL, RomId = NULL WHERE RomId=@id AND IsMediaGroup = 1;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", Id);
            db.ExecuteCMD(sql, dbDict);

            string MediaGroupZipPath = Path.Combine(Config.LibraryConfiguration.LibraryMediaGroupDirectory, Id + ".zip");
            if (File.Exists(MediaGroupZipPath))
            {
                File.Delete(MediaGroupZipPath);
            }
        }

        internal static async Task<GameRomMediaGroupItem> BuildMediaGroupFromRowAsync(DataRow row)
        {
            bool hasSaveStates = false;
            if (row.Table.Columns.Contains("GameStateRomId"))
            {
                if (row["GameStateRomId"] != DBNull.Value)
                {
                    hasSaveStates = true;
                }
            }

            GameRomMediaGroupItem mediaGroupItem = new GameRomMediaGroupItem
            {
                Id = (long)row["Id"],
                Status = (GameRomMediaGroupItem.GroupBuildStatus)row["Status"],
                PlatformId = (long)row["PlatformId"],
                GameId = (long)row["GameId"],
                RomIds = new List<long>(),
                Roms = new List<Roms.GameRomItem>(),
                HasSaveStates = hasSaveStates
            };

            mediaGroupItem.RomUserLastUsed = false;
            if (row.Table.Columns.Contains("MostRecentRomId"))
            {
                if (row["MostRecentRomId"] != DBNull.Value)
                {
                    mediaGroupItem.RomUserLastUsed = true;
                }
            }

            mediaGroupItem.RomUserFavourite = false;
            if (row.Table.Columns.Contains("FavouriteRomId"))
            {
                if (row["FavouriteRomId"] != DBNull.Value)
                {
                    mediaGroupItem.RomUserFavourite = true;
                }
            }

            // get members
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM RomMediaGroup_Members WHERE GroupId=@id;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", mediaGroupItem.Id);
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);
            foreach (DataRow dataRow in data.Rows)
            {
                mediaGroupItem.RomIds.Add((long)dataRow["RomId"]);
                try
                {
                    mediaGroupItem.Roms.Add(await Roms.GetRom((long)dataRow["RomId"]));
                }
                catch (Exception ex)
                {
                    Logging.Log(Logging.LogType.Warning, "Rom Group", "Unable to load ROM data", ex);
                }
            }

            return mediaGroupItem;
        }

        public static async Task StartMediaGroupBuild(long Id)
        {
            GameRomMediaGroupItem mediaGroupItem = await GetMediaGroupAsync(Id);

            if (mediaGroupItem.Status != GameRomMediaGroupItem.GroupBuildStatus.Building)
            {
                // set collection item to waitingforbuild
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "UPDATE RomMediaGroup SET Status=@bs WHERE Id=@id";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("id", Id);
                dbDict.Add("bs", GameRomMediaGroupItem.GroupBuildStatus.WaitingForBuild);
                await db.ExecuteCMDAsync(sql, dbDict);

                // start background task
                ProcessQueue.QueueItem queueItem = new ProcessQueue.QueueItem(ProcessQueue.QueueItemType.MediaGroupCompiler, 1, false, true);
                queueItem.Options = Id;
                queueItem.ForceExecute();
                ProcessQueue.QueueItems.Add(queueItem);
            }
        }

        public static async Task CompileMediaGroup(long Id)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            GameRomMediaGroupItem mediaGroupItem = await GetMediaGroupAsync(Id);
            if (mediaGroupItem.Status == GameRomMediaGroupItem.GroupBuildStatus.WaitingForBuild)
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(mediaGroupItem.GameId)).PreferredMetadataMapItem;
                Models.Game GameObject = await Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);
                Platform PlatformObject = await Platforms.GetPlatform(mediaGroupItem.PlatformId);
                PlatformMapping.PlatformMapItem platformMapItem = await PlatformMapping.GetPlatformMap(mediaGroupItem.PlatformId);

                Logging.Log(Logging.LogType.Information, "Media Group", "Beginning build of media group: " + GameObject.Name + " for platform " + PlatformObject.Name);

                // set starting
                string sql = "UPDATE RomMediaGroup SET Status=@bs WHERE Id=@id";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("id", mediaGroupItem.Id);
                dbDict.Add("bs", GameRomMediaGroupItem.GroupBuildStatus.Building);
                await db.ExecuteCMDAsync(sql, dbDict);

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
                        Roms.GameRomItem rom = await Roms.GetRom(RomId);
                        bool fileNameFound = false;
                        if (File.Exists(rom.Path))
                        {
                            string romExt = Path.GetExtension(rom.Path);
                            if (new string[] { ".zip", ".rar", ".7z" }.Contains(romExt))
                            {
                                Logging.Log(Logging.LogType.Information, "Media Group", "Decompressing ROM: " + rom.Name);

                                // is compressed
                                switch (romExt)
                                {
                                    case ".zip":
                                        try
                                        {
                                            using (var archive = SharpCompress.Archives.Zip.ZipArchive.Open(rom.Path))
                                            {
                                                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                                {
                                                    Logging.Log(Logging.LogType.Information, "Media Group", "Extracting file: " + entry.Key);
                                                    if (fileNameFound == false)
                                                    {
                                                        //check if extension is in valid extensions
                                                        if (platformMapItem.Extensions.SupportedFileExtensions.Contains(Path.GetExtension(entry.Key), StringComparer.InvariantCultureIgnoreCase))
                                                        {
                                                            // update rom file name
                                                            rom.Name = entry.Key;
                                                            fileNameFound = true;
                                                        }
                                                    }
                                                    entry.WriteToDirectory(ZipFileTempPath, new ExtractionOptions()
                                                    {
                                                        ExtractFullPath = true,
                                                        Overwrite = true
                                                    });
                                                }
                                            }
                                        }
                                        catch (Exception zipEx)
                                        {
                                            Logging.Log(Logging.LogType.Warning, "Media Group", "Unzip error", zipEx);
                                            throw;
                                        }
                                        break;

                                    case ".rar":
                                        try
                                        {
                                            using (var archive = SharpCompress.Archives.Rar.RarArchive.Open(rom.Path))
                                            {
                                                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                                {
                                                    Logging.Log(Logging.LogType.Information, "Media Group", "Extracting file: " + entry.Key);
                                                    if (fileNameFound == false)
                                                    {
                                                        //check if extension is in valid extensions
                                                        if (platformMapItem.Extensions.SupportedFileExtensions.Contains(Path.GetExtension(entry.Key), StringComparer.InvariantCultureIgnoreCase))
                                                        {
                                                            // update rom file name
                                                            rom.Name = entry.Key;
                                                            fileNameFound = true;
                                                        }
                                                    }
                                                    entry.WriteToDirectory(ZipFileTempPath, new ExtractionOptions()
                                                    {
                                                        ExtractFullPath = true,
                                                        Overwrite = true
                                                    });
                                                }
                                            }
                                        }
                                        catch (Exception zipEx)
                                        {
                                            Logging.Log(Logging.LogType.Warning, "Media Group", "Unrar error", zipEx);
                                            throw;
                                        }
                                        break;

                                    case ".7z":
                                        try
                                        {
                                            using (var archive = SharpCompress.Archives.SevenZip.SevenZipArchive.Open(rom.Path))
                                            {
                                                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                                {
                                                    Logging.Log(Logging.LogType.Information, "Media Group", "Extracting file: " + entry.Key);
                                                    if (fileNameFound == false)
                                                    {
                                                        //check if extension is in valid extensions
                                                        if (platformMapItem.Extensions.SupportedFileExtensions.Contains(Path.GetExtension(entry.Key), StringComparer.InvariantCultureIgnoreCase))
                                                        {
                                                            // update rom file name
                                                            rom.Name = entry.Key;
                                                            fileNameFound = true;
                                                        }
                                                    }
                                                    entry.WriteToDirectory(ZipFileTempPath, new ExtractionOptions()
                                                    {
                                                        ExtractFullPath = true,
                                                        Overwrite = true
                                                    });
                                                }
                                            }
                                        }
                                        catch (Exception zipEx)
                                        {
                                            Logging.Log(Logging.LogType.Warning, "Media Group", "7z error", zipEx);
                                            throw;
                                        }
                                        break;

                                }
                            }
                            else
                            {
                                // is uncompressed
                                Logging.Log(Logging.LogType.Information, "Media Group", "Copying ROM: " + rom.Name);
                                File.Copy(rom.Path, Path.Combine(ZipFileTempPath, Path.GetFileName(rom.Path)));
                            }

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

                    await File.WriteAllTextAsync(Path.Combine(ZipFileTempPath, GameObject.Name + ".m3u"), String.Join(Environment.NewLine, M3UFileContents));

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
                    await db.ExecuteCMDAsync(sql, dbDict);
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
                    await db.ExecuteCMDAsync(sql, dbDict);

                    Logging.Log(Logging.LogType.Critical, "Media Group", "Media Group building has failed", ex);
                }
            }
        }

        public class GameRomMediaGroupItem
        {
            public long Id { get; set; }
            public long GameId { get; set; }
            public long PlatformId { get; set; }
            public string Platform
            {
                get
                {
                    try
                    {
                        return Platforms.GetPlatform(PlatformId).Result.Name;
                    }
                    catch
                    {
                        return "Unknown";
                    }
                }
            }
            public List<long> RomIds { get; set; }
            public List<Roms.GameRomItem> Roms { get; set; }
            public bool HasSaveStates { get; set; } = false;
            public bool RomUserLastUsed { get; set; }
            public bool RomUserFavourite { get; set; }
            private GroupBuildStatus _Status { get; set; }
            public GroupBuildStatus Status
            {
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
            public long? Size
            {
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