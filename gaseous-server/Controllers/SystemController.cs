using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class SystemController : Controller
    {
        [MapToApiVersion("1.0")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public SystemInfo GetSystemStatus()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            SystemInfo ReturnValue = new SystemInfo();

            // disk size
            List<SystemInfo.PathItem> Disks = new List<SystemInfo.PathItem>();
            foreach (GameLibrary.LibraryItem libraryItem in GameLibrary.GetLibraries)
            {
                Disks.Add(GetDisk(libraryItem.Path));
            }
            ReturnValue.Paths = Disks;

            // database size
            string sql = "SELECT table_schema, SUM(data_length + index_length) FROM information_schema.tables WHERE table_schema = '" + Config.DatabaseConfiguration.DatabaseName + "'";
            DataTable dbResponse = db.ExecuteCMD(sql);
            ReturnValue.DatabaseSize = (long)(System.Decimal)dbResponse.Rows[0][1];

            // platform statistics
            sql = "SELECT Platform.`name`, grc.Count, grs.Size FROM Platform INNER JOIN (SELECT Platform.`name` AS `Name`, SUM(grs.Size) AS Size FROM Platform JOIN Games_Roms AS grs ON (grs.PlatformId = Platform.Id) GROUP BY Platform.`name`) grs ON (grs.`Name` = Platform.`name`) INNER JOIN (SELECT Platform.`name` AS `Name`, COUNT(grc.Size) AS Count FROM Platform JOIN Games_Roms AS grc ON (grc.PlatformId = Platform.Id) GROUP BY Platform.`name`) grc ON (grc.`Name` = Platform.`name`) ORDER BY Platform.`name`;";
            dbResponse = db.ExecuteCMD(sql);
            ReturnValue.PlatformStatistics = new List<SystemInfo.PlatformStatisticsItem>();
            foreach (DataRow dr in dbResponse.Rows)
            {
                SystemInfo.PlatformStatisticsItem platformStatisticsItem = new SystemInfo.PlatformStatisticsItem
                {
                    Platform = (string)dr["name"],
                    RomCount = (long)dr["Count"],
                    TotalSize = (long)(System.Decimal)dr["Size"]
                };
                ReturnValue.PlatformStatistics.Add(platformStatisticsItem);
            }

            return ReturnValue;
        }

        [MapToApiVersion("1.0")]
        [HttpGet]
        [Route("Version")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Version GetSystemVersion() {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        [MapToApiVersion("1.0")]
        [HttpGet]
        [Route("VersionFile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public FileContentResult GetSystemVersionAsFile() {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string ver = "var AppVersion = \"" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "\";" + Environment.NewLine +
                "var DBSchemaVersion = \"" + db.GetDatabaseSchemaVersion() + "\";";
            byte[] bytes = Encoding.UTF8.GetBytes(ver);
            return File(bytes, "text/javascript");
        }

        private SystemInfo.PathItem GetDisk(string Path)
        {
            SystemInfo.PathItem pathItem = new SystemInfo.PathItem {
                LibraryPath = Path,
                SpaceUsed = Common.DirSize(new DirectoryInfo(Path)),
                SpaceAvailable = new DriveInfo(Path).AvailableFreeSpace,
                TotalSpace = new DriveInfo(Path).TotalSize
            };

            return pathItem;
        }

        public class SystemInfo
        {
            public Version ApplicationVersion { 
                get
                    {
                        return Assembly.GetExecutingAssembly().GetName().Version;
                    }
            }
            public List<PathItem>? Paths { get; set; }
            public long DatabaseSize { get; set; }
            public List<PlatformStatisticsItem>? PlatformStatistics { get; set; }

            public class PathItem
            {
                public string LibraryPath { get; set; }
                public long SpaceUsed { get; set; }
                public long SpaceAvailable { get; set; }
                public long TotalSpace { get; set; }
            }

            public class PlatformStatisticsItem
            {
                public string Platform { get; set; }
                public long RomCount { get; set; }
                public long TotalSize { get; set; }
            }
        }
    }
}