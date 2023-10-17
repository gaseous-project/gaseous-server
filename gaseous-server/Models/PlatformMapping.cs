using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Web;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using gaseous_server.Controllers;
using gaseous_tools;
using IGDB.Models;
using Newtonsoft.Json;

namespace gaseous_server.Models
{
	public class PlatformMapping
	{
        private static Dictionary<string, PlatformMapItem> PlatformMapCache = new Dictionary<string, PlatformMapItem>();

        /// <summary>
        /// Updates the platform map from the embedded platform map resource
        /// </summary>
        public static void ExtractPlatformMap(bool ResetToDefault = false)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("gaseous_server.Support.PlatformMap.json"))
            using (StreamReader reader = new StreamReader(stream))
            {
                string rawJson = reader.ReadToEnd();
                List<PlatformMapItem> platforms = new List<PlatformMapItem>();
                Newtonsoft.Json.JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings{
                    MaxDepth = 64
                };
                platforms = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlatformMapItem>>(rawJson, jsonSerializerSettings);

                foreach (PlatformMapItem mapItem in platforms)
                {
                    // check if it exists first - only add if it doesn't exist
                    try
                    {
                        Logging.Log(Logging.LogType.Information, "Platform Map", "Checking if " + mapItem.IGDBName + " is in database.");
                        PlatformMapItem item = GetPlatformMap(mapItem.IGDBId);
                        // exists
                        if (ResetToDefault == false)
                        {
                            Logging.Log(Logging.LogType.Information, "Platform Map", "Skipping import of " + mapItem.IGDBName + " - already in database.");
                        }
                        else
                        {
                            WritePlatformMap(mapItem, true, true);
                            Logging.Log(Logging.LogType.Information, "Platform Map", "Overwriting " + mapItem.IGDBName + " with default values.");
                        }
                    }
                    catch
                    {
                        Logging.Log(Logging.LogType.Information, "Platform Map", "Importing " + mapItem.IGDBName + " from predefined data.");
                        // doesn't exist - add it
                        WritePlatformMap(mapItem, false, true);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the platform map from the provided file - existing items are overwritten
        /// </summary>
        /// <param name="ImportFile"></param>
        public static void ExtractPlatformMap(string ImportFile)
        {
            string rawJson = File.ReadAllText(ImportFile);
            List<PlatformMapItem> platforms = new List<PlatformMapItem>();
            platforms = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlatformMapItem>>(rawJson);

            foreach (PlatformMapItem mapItem in platforms)
            {
                // get the IGDB platform data
                Platform platform = Platforms.GetPlatform(mapItem.IGDBId);

                try
                {
                    PlatformMapItem item = GetPlatformMap(mapItem.IGDBId);

                    // still here? we must have found the item we're looking for! overwrite it
                    Logging.Log(Logging.LogType.Information, "Platform Map", "Replacing " + mapItem.IGDBName + " from external JSON file.");
                    WritePlatformMap(mapItem, true, true);
                }
                catch
                {
                    // we caught a not found error, insert a new record
                    Logging.Log(Logging.LogType.Information, "Platform Map", "Importing " + mapItem.IGDBName + " from external JSON file.");
                    WritePlatformMap(mapItem, false, true);
                }
            }
        }
        
        public static List<PlatformMapItem> PlatformMap
        {
            get
            {
                Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "SELECT * FROM PlatformMap";
                DataTable data = db.ExecuteCMD(sql);

                List<PlatformMapItem> platformMaps = new List<PlatformMapItem>();
                foreach (DataRow row in data.Rows)
                {
                    long mapId = (long)row["Id"];
                    if (PlatformMapCache.ContainsKey(mapId.ToString()))
                    {
                        platformMaps.Add(PlatformMapCache[mapId.ToString()]);
                    }
                    else
                    {
                        platformMaps.Add(BuildPlatformMapItem(row));
                    }
                }

                platformMaps.Sort((x, y) => x.IGDBName.CompareTo(y.IGDBName));

                return platformMaps;
            }
        }

        public static PlatformMapItem GetPlatformMap(long Id)
        {
            if (PlatformMapCache.ContainsKey(Id.ToString()))
            {
                return PlatformMapCache[Id.ToString()];
            }
            else
            {
                Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "SELECT * FROM PlatformMap WHERE Id = @Id";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("Id", Id);
                DataTable data = db.ExecuteCMD(sql, dbDict);

                if (data.Rows.Count > 0)
                {
                    PlatformMapItem platformMap = BuildPlatformMapItem(data.Rows[0]);

                    return platformMap;
                }
                else
                {
                    Exception exception = new Exception("Platform Map Id " + Id + " does not exist.");
                    Logging.Log(Logging.LogType.Critical, "Platform Map", "Platform Map Id " + Id + " does not exist.", exception);
                    throw exception;
                }
            }
        }

        public static void WritePlatformMap(PlatformMapItem item, bool Update, bool AllowAvailableEmulatorOverwrite)
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            if (Update == false)
            {
                // insert
                sql = "INSERT INTO PlatformMap (Id, RetroPieDirectoryName, WebEmulator_Type, WebEmulator_Core, AvailableWebEmulators) VALUES (@Id, @RetroPieDirectoryName, @WebEmulator_Type, @WebEmulator_Core, @AvailableWebEmulators)";
            }
            else
            {
                // update
                if (AllowAvailableEmulatorOverwrite == true)
                {
                    sql = "UPDATE PlatformMap SET RetroPieDirectoryName=@RetroPieDirectoryName, WebEmulator_Type=@WebEmulator_Type, WebEmulator_Core=@WebEmulator_Core, AvailableWebEmulators=@AvailableWebEmulators WHERE Id = @Id";
                }
                else
                {
                    sql = "UPDATE PlatformMap SET RetroPieDirectoryName=@RetroPieDirectoryName, WebEmulator_Type=@WebEmulator_Type, WebEmulator_Core=@WebEmulator_Core WHERE Id = @Id";
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
            db.ExecuteCMD(sql, dbDict);

            // remove existing items so they can be re-inserted
            sql = "DELETE FROM PlatformMap_AlternateNames WHERE Id = @Id; DELETE FROM PlatformMap_Extensions WHERE Id = @Id; DELETE FROM PlatformMap_UniqueExtensions WHERE Id = @Id; DELETE FROM PlatformMap_Bios WHERE Id = @Id;";
            db.ExecuteCMD(sql, dbDict);

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
                        db.ExecuteCMD(sql, dbDict);
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
                    db.ExecuteCMD(sql, dbDict);
                }

                // delete duplicates
                sql = "DELETE FROM PlatformMap_UniqueExtensions; INSERT INTO PlatformMap_UniqueExtensions SELECT * FROM PlatformMap_Extensions WHERE Extension <> '.ZIP' AND Extension IN (SELECT Extension FROM PlatformMap_Extensions GROUP BY Extension HAVING COUNT(Extension) = 1);";
                db.ExecuteCMD(sql);
            }

            // insert bios
            if (item.Bios != null)
            {
                foreach (PlatformMapItem.EmulatorBiosItem biosItem in item.Bios)
                {
                    sql = "INSERT INTO PlatformMap_Bios (Id, Filename, Description, Hash) VALUES (@Id, @Filename, @Description, @Hash);";
                    dbDict.Clear();
                    dbDict.Add("Id", item.IGDBId);
                    dbDict.Add("Filename", biosItem.filename);
                    dbDict.Add("Description", biosItem.description);
                    dbDict.Add("Hash", biosItem.hash);
                    db.ExecuteCMD(sql, dbDict);
                }
            }

            if (PlatformMapCache.ContainsKey(item.IGDBId.ToString()))
            {
                PlatformMapCache.Remove(item.IGDBId.ToString());
            }
        }

        static PlatformMapItem BuildPlatformMapItem(DataRow row)
        {
            long IGDBId = (long)row["Id"];
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            string sql = "";

            // get platform data
            IGDB.Models.Platform platform = Platforms.GetPlatform(IGDBId);

            // get platform alternate names
            sql = "SELECT * FROM PlatformMap_AlternateNames WHERE Id = @Id ORDER BY Name";
            dbDict.Clear();
            dbDict.Add("Id", IGDBId);
            DataTable altTable = db.ExecuteCMD(sql, dbDict);

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
            DataTable extTable = db.ExecuteCMD(sql, dbDict);

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
            DataTable uextTable = db.ExecuteCMD(sql, dbDict);

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
            DataTable biosTable = db.ExecuteCMD(sql, dbDict);

            List<PlatformMapItem.EmulatorBiosItem> bioss = new List<PlatformMapItem.EmulatorBiosItem>();
            foreach (DataRow biosRow in biosTable.Rows)
            {
                PlatformMapItem.EmulatorBiosItem bios = new PlatformMapItem.EmulatorBiosItem
                {
                    filename = (string)Common.ReturnValueIfNull(biosRow["Filename"], ""),
                    description = (string)Common.ReturnValueIfNull(biosRow["Description"], ""),
                    hash = ((string)Common.ReturnValueIfNull(biosRow["Hash"], "")).ToLower()
                };
                bioss.Add(bios);
            }

            // build item
            PlatformMapItem mapItem = new PlatformMapItem();
            mapItem.IGDBId = IGDBId;
            mapItem.IGDBName = platform.Name;
            mapItem.IGDBSlug = platform.Slug;
            mapItem.AlternateNames = alternateNames;
            mapItem.Extensions = new PlatformMapItem.FileExtensions{
                SupportedFileExtensions = knownExtensions,
                UniqueFileExtensions = uniqueExtensions
            };
            mapItem.RetroPieDirectoryName = (string)Common.ReturnValueIfNull(row["RetroPieDirectoryName"], "");
            mapItem.WebEmulator = new PlatformMapItem.WebEmulatorItem{
                Type = (string)Common.ReturnValueIfNull(row["WebEmulator_Type"], ""),
                Core = (string)Common.ReturnValueIfNull(row["WebEmulator_Core"], ""),
                AvailableWebEmulators = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlatformMapItem.WebEmulatorItem.AvailableWebEmulatorItem>>((string)Common.ReturnValueIfNull(row["AvailableWebEmulators"], "[]"))
            };
            mapItem.Bios = bioss;
            
            if (PlatformMapCache.ContainsKey(IGDBId.ToString()))
            {
                PlatformMapCache[IGDBId.ToString()] = mapItem;
            }
            else
            {
                PlatformMapCache.Add(IGDBId.ToString(), mapItem);
            }

            return mapItem;
        }

        public static void GetIGDBPlatformMapping(ref Models.Signatures_Games Signature, FileInfo RomFileInfo, bool SetSystemName)
        {
            bool PlatformFound = false;
            foreach (Models.PlatformMapping.PlatformMapItem PlatformMapping in Models.PlatformMapping.PlatformMap)
            {
                if (PlatformMapping.Extensions != null)
                {
                    if (PlatformMapping.Extensions.UniqueFileExtensions.Contains(RomFileInfo.Extension, StringComparer.OrdinalIgnoreCase))
                    {
                        if (SetSystemName == true)
                        {
                            if (Signature.Game != null) { Signature.Game.System = PlatformMapping.IGDBName; }
                        }
                        Signature.Flags.IGDBPlatformId = PlatformMapping.IGDBId;
                        Signature.Flags.IGDBPlatformName = PlatformMapping.IGDBName;

                        PlatformFound = true;
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
                        Signature.Flags.IGDBPlatformId = PlatformMapping.IGDBId;
                        Signature.Flags.IGDBPlatformName = PlatformMapping.IGDBName;

                        PlatformFound = true;
                        break;
                    }
                }
            }
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
        }
	}
}

