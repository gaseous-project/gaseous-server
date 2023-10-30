using System;
using System.Data;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using IGDB.Models;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace gaseous_server
{
	public static class GameLibrary
	{
        // exceptions
        public class PathExists : Exception
        { 
            public PathExists(string path) : base("The library path " + path + " already exists.")
            {}
        }

        public class PathNotFound : Exception
        {
            public PathNotFound(string path) : base("The path " + path + " does not exist.")
            {}
        }

        public class LibraryNotFound : Exception
        {
            public LibraryNotFound(int LibraryId) : base("Library id " + LibraryId + " does not exist.")
            {}
        }

        public class CannotDeleteDefaultLibrary : Exception
        {
            public CannotDeleteDefaultLibrary() : base("Unable to delete the default library.")
            {}
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

                return library;
            }
        }

        public static List<LibraryItem> GetLibraries
        {
            get
            {
                List<LibraryItem> libraryItems = new List<LibraryItem>();
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "SELECT * FROM GameLibraries";
                DataTable data = db.ExecuteCMD(sql);
                foreach (DataRow row in data.Rows)
                {
                    LibraryItem library = new LibraryItem((int)row["Id"], (string)row["Name"], (string)row["Path"], (long)row["DefaultPlatform"], Convert.ToBoolean((int)row["DefaultLibrary"]));
                    libraryItems.Add(library);
                }

                return libraryItems;
            }
        }

        public static LibraryItem AddLibrary(string Name, string Path, long DefaultPlatformId)
        {
            string PathName = Common.NormalizePath(Path);

            // check path isn't already in place
            foreach (LibraryItem item in GetLibraries)
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
            DataTable data = db.ExecuteCMD(sql, dbDict);
            
            int newLibraryId = (int)(long)data.Rows[0][0];

            return GetLibrary(newLibraryId);
        }

        public static void DeleteLibrary(int LibraryId)
        {
            if (GetLibrary(LibraryId).IsDefaultLibrary == false)
            {
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql = "DELETE FROM Games_Roms WHERE LibraryId=@id; DELETE FROM GameLibraries WHERE Id=@id;";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("id", LibraryId);
                db.ExecuteCMD(sql, dbDict);
            }
            else
            {
                throw new CannotDeleteDefaultLibrary();
            }
        }

        public static LibraryItem GetLibrary(int LibraryId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM GameLibraries WHERE Id=@id";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", LibraryId);
            DataTable data = db.ExecuteCMD(sql, dbDict);
            if (data.Rows.Count > 0)
            {
                DataRow row = data.Rows[0];
                LibraryItem library = new LibraryItem((int)row["Id"], (string)row["Name"], (string)row["Path"], (long)row["DefaultPlatform"], Convert.ToBoolean((int)row["DefaultLibrary"]));
                return library;
            }
            else
            {
                throw new LibraryNotFound(LibraryId);
            }
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
                        Platform platform = Platforms.GetPlatform(_DefaultPlatformId);
                        return platform.Name;
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            public bool IsDefaultLibrary => _IsDefaultLibrary;
        }
    }
}