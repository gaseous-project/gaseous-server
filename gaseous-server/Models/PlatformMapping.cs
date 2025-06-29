﻿using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using gaseous_server.Controllers;
using HasheousClient.Models.Metadata.IGDB;
using Newtonsoft.Json;

namespace gaseous_server.Models
{
    public class PlatformMapping
    {
        /// <summary>
        /// Updates the platform map from the embedded platform map resource
        /// </summary>
        public static async Task ExtractPlatformMap(bool ResetToDefault = false)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("gaseous_server.Support.PlatformMap.json"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string rawJson = await reader.ReadToEndAsync();
                List<PlatformMapItem> platforms = new List<PlatformMapItem>();
                Newtonsoft.Json.JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
                {
                    MaxDepth = 64
                };
                platforms = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlatformMapItem>>(rawJson, jsonSerializerSettings);

                foreach (PlatformMapItem mapItem in platforms)
                {
                    // check if it exists first - only add if it doesn't exist
                    try
                    {
                        Logging.Log(Logging.LogType.Information, "Platform Map", "Checking if " + mapItem.IGDBName + " is in database.");
                        PlatformMapItem item = await GetPlatformMap(mapItem.IGDBId);
                        // exists
                        if (ResetToDefault == false)
                        {
                            await WriteAvailableEmulators(mapItem);
                            Logging.Log(Logging.LogType.Information, "Platform Map", "Skipping import of " + mapItem.IGDBName + " - already in database.");
                        }
                        else
                        {
                            await WritePlatformMap(mapItem, true, true, true);
                            Logging.Log(Logging.LogType.Information, "Platform Map", "Overwriting " + mapItem.IGDBName + " with default values.");
                        }
                    }
                    catch
                    {
                        Logging.Log(Logging.LogType.Information, "Platform Map", "Importing " + mapItem.IGDBName + " from predefined data.");
                        // doesn't exist - add it
                        await WritePlatformMap(mapItem, false, true, true);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the platform map from the provided file - existing items are overwritten
        /// </summary>
        /// <param name="ImportFile"></param>
        public static async Task ExtractPlatformMap(string ImportFile)
        {
            string rawJson = await File.ReadAllTextAsync(ImportFile);
            List<PlatformMapItem> platforms = new List<PlatformMapItem>();
            platforms = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlatformMapItem>>(rawJson);

            foreach (PlatformMapItem mapItem in platforms)
            {
                // insert dummy platform data - it'll be cleaned up on the first metadata refresh
                Platform platform = CreateDummyPlatform(mapItem);

                try
                {
                    PlatformMapItem item = await GetPlatformMap(mapItem.IGDBId);

                    // still here? we must have found the item we're looking for! overwrite it
                    Logging.Log(Logging.LogType.Information, "Platform Map", "Replacing " + mapItem.IGDBName + " from external JSON file.");
                    await WritePlatformMap(mapItem, true, true);
                }
                catch
                {
                    // we caught a not found error, insert a new record
                    Logging.Log(Logging.LogType.Information, "Platform Map", "Importing " + mapItem.IGDBName + " from external JSON file.");
                    await WritePlatformMap(mapItem, false, true);
                }
            }
        }

        private static Platform CreateDummyPlatform(PlatformMapItem mapItem)
        {
            Platform platform = new Platform
            {
                Id = mapItem.IGDBId,
                Name = mapItem.IGDBName,
                Slug = mapItem.IGDBSlug,
                AlternativeName = mapItem.AlternateNames.FirstOrDefault()
            };

            if (Storage.GetCacheStatus(HasheousClient.Models.MetadataSources.None, "Platform", mapItem.IGDBId) == Storage.CacheStatus.NotPresent)
            {
                _ = Task.Run(() => Storage.NewCacheValue(HasheousClient.Models.MetadataSources.None, platform));
            }

            if (Storage.GetCacheStatus(HasheousClient.Models.MetadataSources.IGDB, "Platform", mapItem.IGDBId) == Storage.CacheStatus.NotPresent)
            {
                _ = Task.Run(() => Storage.NewCacheValue(HasheousClient.Models.MetadataSources.IGDB, platform));
            }

            return platform;
        }

        // <summary>
        /// Gets the list of supported file extensions.
        /// </summary>
        public static List<string> SupportedFileExtensions { get; } = new List<string>();

        /// <summary>
        /// Gets the platform map from the database.
        /// </summary>
        public static List<PlatformMapItem> PlatformMap
        {
            get
            {
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "SELECT * FROM PlatformMap";
                DataTable data = db.ExecuteCMD(sql);

                List<PlatformMapItem> platformMaps = new List<PlatformMapItem>();

                List<string> _SupportedFileExtensions = new List<string>
                {
                    ".zip",
                    ".7z",
                    ".rar"
                };

                foreach (DataRow row in data.Rows)
                {
                    PlatformMapItem mapItem;

                    long mapId = (long)row["Id"];

                    mapItem = BuildPlatformMapItem(row).Result;
                    if (mapItem != null)
                    {
                        platformMaps.Add(mapItem);
                    }

                    // update known file types list
                    if (mapItem != null && mapItem.Extensions != null)
                    {
                        foreach (string ext in mapItem.Extensions.SupportedFileExtensions)
                        {
                            if (!_SupportedFileExtensions.Contains(ext.ToLower()))
                            {
                                _SupportedFileExtensions.Add(ext.ToLower());
                            }
                        }
                    }
                }

                SupportedFileExtensions.Clear();
                SupportedFileExtensions.AddRange(_SupportedFileExtensions);

                platformMaps.Sort((x, y) => x.IGDBName.CompareTo(y.IGDBName));

                return platformMaps;
            }
        }

        /// <summary>
        /// Gets the platform map item by its ID.
        /// </summary>
        /// <param name="Id">The ID of the platform map item.</param>
        /// <returns>The platform map item.</returns>
        public static async Task<PlatformMapItem> GetPlatformMap(long Id)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM PlatformMap WHERE Id = @Id";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("Id", Id);
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);

            if (data.Rows.Count > 0)
            {
                PlatformMapItem platformMap = await BuildPlatformMapItem(data.Rows[0]);

                return platformMap;
            }
            else
            {
                Exception exception = new Exception("Platform Map Id " + Id + " does not exist.");
                Logging.Log(Logging.LogType.Critical, "Platform Map", "Platform Map Id " + Id + " does not exist.", exception);
                throw exception;
            }
        }

        public static async Task WritePlatformMap(PlatformMapItem item, bool Update, bool AllowAvailableEmulatorOverwrite, bool overwriteBios = false)
        {
            CreateDummyPlatform(item);

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            if (Update == false)
            {
                // insert
                sql = "INSERT INTO PlatformMap (Id, RetroPieDirectoryName, WebEmulator_Type, WebEmulator_Core, AvailableWebEmulators) VALUES (@Id, @RetroPieDirectoryName, @WebEmulator_Type, @WebEmulator_Core, @AvailableWebEmulators);";
            }
            else
            {
                // update
                if (AllowAvailableEmulatorOverwrite == true)
                {
                    sql = "UPDATE PlatformMap SET RetroPieDirectoryName=@RetroPieDirectoryName, WebEmulator_Type=@WebEmulator_Type, WebEmulator_Core=@WebEmulator_Core, AvailableWebEmulators=@AvailableWebEmulators WHERE Id = @Id; ";
                }
                else
                {
                    sql = "UPDATE PlatformMap SET RetroPieDirectoryName=@RetroPieDirectoryName, WebEmulator_Type=@WebEmulator_Type, WebEmulator_Core=@WebEmulator_Core WHERE Id = @Id;";
                }
            }
            dbDict.Add("Id", item.IGDBId);
            dbDict.Add("RetroPieDirectoryName", item.RetroPieDirectoryName);
            if (item.WebEmulator != null)
            {
                dbDict.Add("WebEmulator_Type", item.WebEmulator.Type);
                dbDict.Add("WebEmulator_Core", item.WebEmulator.Core);
                dbDict.Add("AvailableWebEmulators", Newtonsoft.Json.JsonConvert.SerializeObject(item.WebEmulator.AvailableWebEmulators));
            }
            else
            {
                dbDict.Add("WebEmulator_Type", "");
                dbDict.Add("WebEmulator_Core", "");
                dbDict.Add("AvailableWebEmulators", "");
            }
            await db.ExecuteCMDAsync(sql, dbDict);

            // remove existing items so they can be re-inserted
            sql = "DELETE FROM PlatformMap_AlternateNames WHERE Id = @Id; DELETE FROM PlatformMap_Extensions WHERE Id = @Id; DELETE FROM PlatformMap_UniqueExtensions WHERE Id = @Id; DELETE FROM PlatformMap_Bios WHERE Id = @Id;";
            await db.ExecuteCMDAsync(sql, dbDict);

            // insert alternate names
            if (item.AlternateNames != null)
            {
                foreach (string alternateName in item.AlternateNames)
                {
                    if (alternateName != null)
                    {
                        sql = "INSERT INTO PlatformMap_AlternateNames (Id, Name) VALUES (@Id, @Name);";
                        dbDict.Clear();
                        dbDict.Add("Id", item.IGDBId);
                        dbDict.Add("Name", HttpUtility.HtmlDecode(alternateName));
                        await db.ExecuteCMDAsync(sql, dbDict);
                    }
                }
            }

            // insert extensions
            if (item.Extensions != null)
            {
                foreach (string extension in item.Extensions.SupportedFileExtensions)
                {
                    sql = "INSERT INTO PlatformMap_Extensions (Id, Extension) VALUES (@Id, @Extension);";
                    dbDict.Clear();
                    dbDict.Add("Id", item.IGDBId);
                    dbDict.Add("Extension", extension.Trim().ToUpper());
                    await db.ExecuteCMDAsync(sql, dbDict);
                }

                // delete duplicates
                sql = "DELETE FROM PlatformMap_UniqueExtensions; INSERT INTO PlatformMap_UniqueExtensions SELECT * FROM PlatformMap_Extensions WHERE Extension <> '.ZIP' AND Extension IN (SELECT Extension FROM PlatformMap_Extensions GROUP BY Extension HAVING COUNT(Extension) = 1);";
                await db.ExecuteCMDAsync(sql);
            }

            // insert bios
            if (item.Bios != null)
            {
                foreach (PlatformMapItem.EmulatorBiosItem biosItem in item.Bios)
                {
                    bool isEnabled = false;
                    if (overwriteBios == true)
                    {
                        isEnabled = true;
                    }
                    else
                    {
                        if (item.EnabledBIOSHashes == null)
                        {
                            item.EnabledBIOSHashes = new List<string>();
                        }
                        if (item.EnabledBIOSHashes.Contains(biosItem.hash))
                        {
                            isEnabled = true;
                        }
                    }

                    sql = "INSERT INTO PlatformMap_Bios (Id, Filename, Description, Hash, Enabled) VALUES (@Id, @Filename, @Description, @Hash, @Enabled);";
                    dbDict.Clear();
                    dbDict.Add("Id", item.IGDBId);
                    dbDict.Add("Filename", biosItem.filename);
                    dbDict.Add("Description", biosItem.description);
                    dbDict.Add("Hash", biosItem.hash);
                    dbDict.Add("Enabled", isEnabled);
                    await db.ExecuteCMDAsync(sql, dbDict);
                }
            }

            // clear cache
            Database.DatabaseMemoryCache.RemoveCacheObject("PlatformMap");
        }

        public static async Task WriteAvailableEmulators(PlatformMapItem item)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            sql = "UPDATE PlatformMap SET RetroPieDirectoryName=@RetroPieDirectoryName, WebEmulator_Type=@WebEmulator_Type, WebEmulator_Core=@WebEmulator_Core, AvailableWebEmulators=@AvailableWebEmulators WHERE Id = @Id; ";

            dbDict.Add("Id", item.IGDBId);
            dbDict.Add("RetroPieDirectoryName", item.RetroPieDirectoryName);
            if (item.WebEmulator != null)
            {
                dbDict.Add("WebEmulator_Type", item.WebEmulator.Type);
                dbDict.Add("WebEmulator_Core", item.WebEmulator.Core);
                dbDict.Add("AvailableWebEmulators", Newtonsoft.Json.JsonConvert.SerializeObject(item.WebEmulator.AvailableWebEmulators));
            }
            else
            {
                dbDict.Add("WebEmulator_Type", "");
                dbDict.Add("WebEmulator_Core", "");
                dbDict.Add("AvailableWebEmulators", "");
            }
            await db.ExecuteCMDAsync(sql, dbDict);
        }

        static async Task<PlatformMapItem> BuildPlatformMapItem(DataRow row)
        {
            long IGDBId = (long)row["Id"];
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            string sql = "";

            // get platform data
            Platform? platform = null;
            if (await Storage.GetCacheStatusAsync(HasheousClient.Models.MetadataSources.None, "Platform", IGDBId) == Storage.CacheStatus.NotPresent)
            {
                //platform = Platforms.GetPlatform(IGDBId, false);
            }
            else
            {
                // platform = (Platform)Storage.GetCacheValue<Platform>(HasheousClient.Models.MetadataSources.None, new Platform(), "id", IGDBId);
                platform = await Platforms.GetPlatform(IGDBId, HasheousClient.Models.MetadataSources.None);
            }

            if (platform != null)
            {
                // get platform alternate names
                sql = "SELECT * FROM PlatformMap_AlternateNames WHERE Id = @Id ORDER BY Name";
                dbDict.Clear();
                dbDict.Add("Id", IGDBId);
                DataTable altTable = await db.ExecuteCMDAsync(sql, dbDict);

                List<string> alternateNames = new List<string>();
                foreach (DataRow altRow in altTable.Rows)
                {
                    string altVal = (string)altRow["Name"];
                    if (!alternateNames.Contains(altVal, StringComparer.OrdinalIgnoreCase))
                    {
                        alternateNames.Add(altVal);
                    }
                }
                if (platform.AlternativeName != null)
                {
                    if (!alternateNames.Contains(platform.AlternativeName, StringComparer.OrdinalIgnoreCase))
                    {
                        alternateNames.Add(platform.AlternativeName);
                    }
                }

                // get platform known extensions
                sql = "SELECT * FROM PlatformMap_Extensions WHERE Id = @Id ORDER BY Extension";
                dbDict.Clear();
                dbDict.Add("Id", IGDBId);
                DataTable extTable = await db.ExecuteCMDAsync(sql, dbDict);

                List<string> knownExtensions = new List<string>();
                foreach (DataRow extRow in extTable.Rows)
                {
                    string extVal = (string)extRow["Extension"];
                    if (!knownExtensions.Contains(extVal, StringComparer.OrdinalIgnoreCase))
                    {
                        knownExtensions.Add(extVal);
                    }
                }

                // get platform unique extensions
                sql = "SELECT * FROM PlatformMap_UniqueExtensions WHERE Id = @Id ORDER BY Extension";
                dbDict.Clear();
                dbDict.Add("Id", IGDBId);
                DataTable uextTable = await db.ExecuteCMDAsync(sql, dbDict);

                List<string> uniqueExtensions = new List<string>();
                foreach (DataRow uextRow in uextTable.Rows)
                {
                    string uextVal = (string)uextRow["Extension"];
                    if (!uniqueExtensions.Contains(uextVal, StringComparer.OrdinalIgnoreCase))
                    {
                        uniqueExtensions.Add(uextVal);
                    }
                }

                // get platform bios
                sql = "SELECT * FROM PlatformMap_Bios WHERE Id = @Id ORDER BY Filename";
                dbDict.Clear();
                dbDict.Add("Id", IGDBId);
                DataTable biosTable = await db.ExecuteCMDAsync(sql, dbDict);

                List<PlatformMapItem.EmulatorBiosItem> bioss = new List<PlatformMapItem.EmulatorBiosItem>();
                List<string> enabledBios = new List<string>();
                foreach (DataRow biosRow in biosTable.Rows)
                {
                    PlatformMapItem.EmulatorBiosItem bios = new PlatformMapItem.EmulatorBiosItem
                    {
                        filename = (string)Common.ReturnValueIfNull(biosRow["Filename"], ""),
                        description = (string)Common.ReturnValueIfNull(biosRow["Description"], ""),
                        hash = ((string)Common.ReturnValueIfNull(biosRow["Hash"], "")).ToLower()
                    };
                    bioss.Add(bios);

                    if ((bool)Common.ReturnValueIfNull(biosRow["Enabled"], true) == true)
                    {
                        enabledBios.Add(bios.hash);
                    }
                }

                // build item
                PlatformMapItem mapItem = new PlatformMapItem();
                mapItem.IGDBId = IGDBId;
                mapItem.IGDBName = platform.Name;
                mapItem.IGDBSlug = platform.Slug;
                mapItem.AlternateNames = alternateNames;
                mapItem.Extensions = new PlatformMapItem.FileExtensions
                {
                    SupportedFileExtensions = knownExtensions,
                    UniqueFileExtensions = uniqueExtensions
                };
                mapItem.RetroPieDirectoryName = (string)Common.ReturnValueIfNull(row["RetroPieDirectoryName"], "");
                mapItem.WebEmulator = new PlatformMapItem.WebEmulatorItem
                {
                    Type = (string)Common.ReturnValueIfNull(row["WebEmulator_Type"], ""),
                    Core = (string)Common.ReturnValueIfNull(row["WebEmulator_Core"], ""),
                    AvailableWebEmulators = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlatformMapItem.WebEmulatorItem.AvailableWebEmulatorItem>>((string)Common.ReturnValueIfNull(row["AvailableWebEmulators"], "[]"))
                };
                mapItem.Bios = bioss;
                mapItem.EnabledBIOSHashes = enabledBios;

                return mapItem;
            }

            return null;
        }

        public static void GetIGDBPlatformMapping(ref gaseous_server.Models.Signatures_Games Signature, string ImageExtension, bool SetSystemName)
        {
            if (Signature.Game != null)
            {
                Logging.Log(Logging.LogType.Information, "Platform Mapping", "Determining platform based on extension " + ImageExtension + " or \"" + Signature.Game.System + "\"");
            }

            bool PlatformFound = false;
            foreach (Models.PlatformMapping.PlatformMapItem PlatformMapping in Models.PlatformMapping.PlatformMap)
            {
                if (PlatformMapping.Extensions != null)
                {
                    if (PlatformMapping.Extensions.UniqueFileExtensions.Contains(ImageExtension, StringComparer.OrdinalIgnoreCase))
                    {
                        if (SetSystemName == true)
                        {
                            if (Signature.Game != null) { Signature.Game.System = PlatformMapping.IGDBName; }
                        }
                        Signature.MetadataSources.AddPlatform(PlatformMapping.IGDBId, PlatformMapping.IGDBName, HasheousClient.Models.MetadataSources.IGDB);

                        PlatformFound = true;

                        Logging.Log(Logging.LogType.Information, "Platform Mapping", "Platform id " + PlatformMapping.IGDBId + " determined from file extension");
                        break;
                    }
                }
            }

            if (PlatformFound == false)
            {
                foreach (Models.PlatformMapping.PlatformMapItem PlatformMapping in Models.PlatformMapping.PlatformMap)
                {
                    if (
                        PlatformMapping.IGDBName == Signature.Game.System ||
                        PlatformMapping.AlternateNames.Contains(Signature.Game.System, StringComparer.OrdinalIgnoreCase)
                        )
                    {
                        if (SetSystemName == true)
                        {
                            if (Signature.Game != null) { Signature.Game.System = PlatformMapping.IGDBName; }
                        }
                        Signature.MetadataSources.AddPlatform(PlatformMapping.IGDBId, PlatformMapping.IGDBName, HasheousClient.Models.MetadataSources.IGDB);

                        PlatformFound = true;

                        Logging.Log(Logging.LogType.Information, "Platform Mapping", "Platform id " + PlatformMapping.IGDBId + " determined from signature system to platform map");
                        break;
                    }
                }
            }

            if (PlatformFound == false)
            {
                Logging.Log(Logging.LogType.Information, "Platform Mapping", "Unable to determine platform");
            }
        }

        public async Task<PlatformMapItem> GetUserPlatformMap(string UserId, long PlatformId, long GameId)
        {
            // get the system enabled bios hashes
            Models.PlatformMapping.PlatformMapItem platformMapItem = await PlatformMapping.GetPlatformMap(PlatformId);

            // get the user enabled bios hashes
            PlatformMapping.UserEmulatorConfiguration userEmulatorConfiguration = await GetUserEmulator(UserId, GameId, PlatformId);
            if (userEmulatorConfiguration != null)
            {
                platformMapItem.WebEmulator.Type = userEmulatorConfiguration.EmulatorType;
                platformMapItem.WebEmulator.Core = userEmulatorConfiguration.Core;
                platformMapItem.EnabledBIOSHashes = userEmulatorConfiguration.EnableBIOSFiles;
            }

            return platformMapItem;
        }

        public async Task<UserEmulatorConfiguration> GetUserEmulator(string UserId, long GameId, long PlatformId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT Mapping FROM User_PlatformMap WHERE id = @UserId AND GameId = @GameId AND PlatformId = @PlatformId;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "UserId", UserId },
                { "GameId", GameId },
                { "PlatformId", PlatformId }
            };
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);

            if (data.Rows.Count > 0)
            {
                UserEmulatorConfiguration emulator = Newtonsoft.Json.JsonConvert.DeserializeObject<UserEmulatorConfiguration>((string)data.Rows[0]["Mapping"]);

                return emulator;
            }
            else
            {
                return null;
            }
        }

        public async Task SetUserEmulator(string UserId, long GameId, long PlatformId, UserEmulatorConfiguration Mapping)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO User_PlatformMap (id, GameId, PlatformId, Mapping) VALUES (@UserId, @GameId, @PlatformId, @Mapping) ON DUPLICATE KEY UPDATE Mapping = @Mapping;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "UserId", UserId },
                { "GameId", GameId },
                { "PlatformId", PlatformId },
                { "Mapping", Newtonsoft.Json.JsonConvert.SerializeObject(Mapping) }
            };
            await db.ExecuteCMDAsync(sql, dbDict);
        }

        public async Task DeleteUserEmulator(string UserId, long GameId, long PlatformId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM User_PlatformMap WHERE id = @UserId AND GameId = @GameId AND PlatformId = @PlatformId;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "UserId", UserId },
                { "GameId", GameId },
                { "PlatformId", PlatformId }
            };
            await db.ExecuteCMDAsync(sql, dbDict);
        }

        public class PlatformMapItem
        {
            public long IGDBId { get; set; }
            public string IGDBName { get; set; }
            public string IGDBSlug { get; set; }
            public List<string> AlternateNames { get; set; } = new List<string>();

            public FileExtensions Extensions { get; set; }
            public class FileExtensions
            {
                public List<string> SupportedFileExtensions { get; set; } = new List<string>();

                public List<string> UniqueFileExtensions { get; set; } = new List<string>();
            }

            public string RetroPieDirectoryName { get; set; }
            public WebEmulatorItem? WebEmulator { get; set; }

            public class WebEmulatorItem
            {
                public string Type { get; set; }
                public string Core { get; set; }

                public List<AvailableWebEmulatorItem> AvailableWebEmulators { get; set; } = new List<AvailableWebEmulatorItem>();

                public class AvailableWebEmulatorItem
                {
                    public string EmulatorType { get; set; }
                    public List<AvailableWebEmulatorCoreItem> AvailableWebEmulatorCores { get; set; } = new List<AvailableWebEmulatorCoreItem>();

                    public class AvailableWebEmulatorCoreItem
                    {
                        public string Core { get; set; }
                        public string? AlternateCoreName { get; set; } = "";
                        public bool Default { get; set; } = false;
                    }
                }
            }

            public List<EmulatorBiosItem> Bios { get; set; }

            public class EmulatorBiosItem
            {
                public string hash { get; set; }
                public string description { get; set; }
                public string filename { get; set; }
            }

            public List<string> EnabledBIOSHashes { get; set; }
        }

        public class UserEmulatorConfiguration
        {
            public string EmulatorType { get; set; }
            public string Core { get; set; }
            public List<string> EnableBIOSFiles { get; set; } = new List<string>();
        }
    }
}

