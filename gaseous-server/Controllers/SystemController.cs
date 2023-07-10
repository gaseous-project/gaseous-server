using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using gaseous_tools;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class SystemController : Controller
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Dictionary<string, object> GetSystemStatus()
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            Dictionary<string, object> ReturnValue = new Dictionary<string, object>();

            // disk size
            List<Dictionary<string, object>> Disks = new List<Dictionary<string, object>>();
            //Disks.Add(GetDisk(gaseous_tools.Config.ConfigurationPath));
            Disks.Add(GetDisk(gaseous_tools.Config.LibraryConfiguration.LibraryRootDirectory));
            ReturnValue.Add("Paths", Disks);

            // database size
            string sql = "SELECT table_schema, SUM(data_length + index_length) FROM information_schema.tables WHERE table_schema = '" + Config.DatabaseConfiguration.DatabaseName + "'";
            DataTable dbResponse = db.ExecuteCMD(sql);
            ReturnValue.Add("DatabaseSize", dbResponse.Rows[0][1]);

            return ReturnValue;
        }

        private Dictionary<string, object> GetDisk(string Path)
        {
            Dictionary<string, object> DiskValues = new Dictionary<string, object>();
            DiskValues.Add("LibraryPath", Path);
            DiskValues.Add("SpaceUsed", gaseous_tools.Common.DirSize(new DirectoryInfo(Path)));
            DiskValues.Add("SpaceAvailable", new DriveInfo(Path).AvailableFreeSpace);
            DiskValues.Add("TotalSpace", new DriveInfo(Path).TotalSize);

            return DiskValues;
        }
    }
}