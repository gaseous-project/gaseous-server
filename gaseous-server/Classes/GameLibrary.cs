using System;
using System.Data;
using System.Threading.Tasks;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace gaseous_server
{
    public static class GameLibrary
    {
        // exceptions
        public class PathExists : Exception
        {
            public PathExists(string path) : base("The library path " + path + " already exists.")
            { }
        }

        public class PathNotFound : Exception
        {
            public PathNotFound(string path) : base("The path " + path + " does not exist.")
            { }
        }

        public class LibraryNotFound : Exception
        {
            public LibraryNotFound(int LibraryId) : base("Library id " + LibraryId + " does not exist.")
            { }
        }

        public class CannotDeleteDefaultLibrary : Exception
        {
            public CannotDeleteDefaultLibrary() : base("Unable to delete the default library.")
            { }
        }

        public class CannotDeleteLibraryWhileScanIsActive : Exception
        {
            public CannotDeleteLibraryWhileScanIsActive() : base("Unable to delete library while a library scan is active. Wait for all scans to complete and try again")
            { }
        }

        // code
        public static LibraryItem GetDefaultLibrary
        {
            get
            {
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "SELECT * FROM GameLibraries WHERE DefaultLibrary=1 LIMIT 1";
                DataTable data = db.ExecuteCMD(sql);
                DataRow row = data.Rows[0];
                LibraryItem library = new LibraryItem((int)row["Id"], (string)row["Name"], (string)row["Path"], (long)row["DefaultPlatform"], Convert.ToBoolean((int)row["DefaultLibrary"]));

                if (!Directory.Exists(library.Path) && !(File.Exists(library.Path) && new FileInfo(library.Path).Attributes.HasFlag(FileAttributes.ReparsePoint)))
                {
                    Directory.CreateDirectory(library.Path);
                }

                return library;
            }
        }

        // update default library path
        public static async Task UpdateDefaultLibraryPathAsync()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "UPDATE GameLibraries SET Path=@path WHERE DefaultLibrary=1;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "path", Path.Combine(Config.LibraryConfiguration.LibraryRootDirectory, "Library") }
            };
            await db.ExecuteCMDAsync(sql, dbDict);
        }

        public static async Task<List<LibraryItem>> GetLibraries(bool GetStorageInfo = false)
        {
            List<LibraryItem> libraryItems = new List<LibraryItem>();
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM GameLibraries ORDER BY `Name`;";
            DataTable data = await db.ExecuteCMDAsync(sql);
            foreach (DataRow row in data.Rows)
            {
                LibraryItem library = new LibraryItem((int)row["Id"], (string)row["Name"], (string)row["Path"], (long)row["DefaultPlatform"], Convert.ToBoolean((int)row["DefaultLibrary"]));
                if (GetStorageInfo == true)
                {
                    library.PathInfo = Controllers.SystemController.GetDisk(library.Path);
                }
                libraryItems.Add(library);

                if (library.IsDefaultLibrary == true)
                {
                    // check directory exists
                    if (!Directory.Exists(library.Path) && !(File.Exists(library.Path) && new FileInfo(library.Path).Attributes.HasFlag(FileAttributes.ReparsePoint)))
                    {
                        Directory.CreateDirectory(library.Path);
                    }
                }
            }

            return libraryItems;
        }

        public static async Task<LibraryItem> AddLibrary(string Name, string Path, long DefaultPlatformId)
        {
            string PathName = Common.NormalizePath(Path);

            // check path isn't already in place
            foreach (LibraryItem item in await GetLibraries())
            {
                if (Common.NormalizePath(PathName) == Common.NormalizePath(item.Path))
                {
                    // already existing path!
                    throw new PathExists(PathName);
                }
            }

            if (!System.IO.Path.Exists(PathName))
            {
                throw new PathNotFound(PathName);
            }

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO GameLibraries (Name, Path, DefaultPlatform, DefaultLibrary) VALUES (@name, @path, @defaultplatform, 0); SELECT CAST(LAST_INSERT_ID() AS SIGNED);";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("name", Name);
            dbDict.Add("path", PathName);
            dbDict.Add("defaultplatform", DefaultPlatformId);
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);

            int newLibraryId = (int)(long)data.Rows[0][0];

            Logging.Log(Logging.LogType.Information, "Library Management", "Created library " + Name + " at directory " + PathName);

            LibraryItem library = await GetLibrary(newLibraryId);

            return library;
        }

        public static async Task<LibraryItem> EditLibrary(int LibraryId, string Name, long DefaultPlatformId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "UPDATE GameLibraries SET Name=@name, DefaultPlatform=@defaultplatform WHERE Id=@id;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("name", Name);
            dbDict.Add("defaultplatform", DefaultPlatformId);
            dbDict.Add("id", LibraryId);
            await db.ExecuteCMDAsync(sql, dbDict);

            Logging.Log(Logging.LogType.Information, "Library Management", "Updated library " + Name);

            return await GetLibrary(LibraryId);
        }

        public static async Task DeleteLibrary(int LibraryId)
        {
            LibraryItem library = await GetLibrary(LibraryId);
            if (library.IsDefaultLibrary == false)
            {
                // check for active library scans
                foreach (ProcessQueue.QueueItem item in ProcessQueue.QueueItems)
                {
                    if (
                        (item.ItemType == ProcessQueue.QueueItemType.LibraryScan && item.ItemState == ProcessQueue.QueueItemState.Running) ||
                        (item.ItemType == ProcessQueue.QueueItemType.LibraryScanWorker && item.ItemState == ProcessQueue.QueueItemState.Running)
                    )
                    {
                        Logging.Log(Logging.LogType.Warning, "Library Management", "Unable to delete libraries while a library scan is running. Wait until the the library scan is completed and try again.");
                        throw new CannotDeleteLibraryWhileScanIsActive();
                    }
                }

                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "DELETE FROM Games_Roms WHERE LibraryId=@id; DELETE FROM GameLibraries WHERE Id=@id;";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("id", LibraryId);
                await db.ExecuteCMDAsync(sql, dbDict);

                Logging.Log(Logging.LogType.Information, "Library Management", "Deleted library " + library.Name + " at path " + library.Path);
            }
            else
            {
                Logging.Log(Logging.LogType.Warning, "Library Management", "Unable to delete the default library.");
                throw new CannotDeleteDefaultLibrary();
            }
        }

        public static async Task<LibraryItem> GetLibrary(int LibraryId, bool GetStorageInfo = false)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM GameLibraries WHERE Id=@id";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", LibraryId);
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);
            if (data.Rows.Count > 0)
            {
                DataRow row = data.Rows[0];
                LibraryItem library = new LibraryItem((int)row["Id"], (string)row["Name"], (string)row["Path"], (long)row["DefaultPlatform"], Convert.ToBoolean((int)row["DefaultLibrary"]));

                if (GetStorageInfo == true)
                {
                    library.PathInfo = Controllers.SystemController.GetDisk(library.Path);
                }

                return library;
            }
            else
            {
                throw new LibraryNotFound(LibraryId);
            }
        }

        public static async Task<LibraryItem> ScanLibrary(int LibraryId)
        {
            // add the library to scan to the queue
            LibraryItem library = await GetLibrary(LibraryId);

            // start the library scan if it's not already running
            foreach (ProcessQueue.QueueItem item in ProcessQueue.QueueItems)
            {
                if (item.ItemType == ProcessQueue.QueueItemType.LibraryScan && item.ItemState != ProcessQueue.QueueItemState.Running)
                {
                    item.AddSubTask(ProcessQueue.QueueItem.SubTask.TaskTypes.LibraryScanWorker, library.Name, library, true);
                    item.ForceExecute();
                }
            }

            return library;
        }

        public class LibraryItem
        {
            public LibraryItem(int Id, string Name, string Path, long DefaultPlatformId, bool IsDefaultLibrary)
            {
                _Id = Id;
                _Name = Name;
                _Path = Path;
                _DefaultPlatformId = DefaultPlatformId;
                _IsDefaultLibrary = IsDefaultLibrary;

                if (_IsDefaultLibrary)
                {
                    if (!Directory.Exists(Path) && !(File.Exists(Path) && new FileInfo(Path).Attributes.HasFlag(FileAttributes.ReparsePoint)))
                    {
                        Directory.CreateDirectory(Path);
                    }
                }
            }

            int _Id = 0;
            string _Name = "";
            string _Path = "";
            long _DefaultPlatformId = 0;
            bool _IsDefaultLibrary = false;

            public int Id => _Id;
            public string Name => _Name;
            public string Path => _Path;
            public long DefaultPlatformId => _DefaultPlatformId;
            public string? DefaultPlatformName
            {
                get
                {
                    if (_DefaultPlatformId != 0)
                    {
                        HasheousClient.Models.Metadata.IGDB.Platform platform = Platforms.GetPlatform(_DefaultPlatformId).Result;
                        return platform.Name;
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            public bool IsDefaultLibrary => _IsDefaultLibrary;

            public Controllers.SystemController.SystemInfo.PathItem? PathInfo { get; set; }
        }
    }
}