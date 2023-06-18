using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using gaseous_tools;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]/[action]")]
    public class SignaturesController : ControllerBase
    {
        /// <summary>
        /// Get the current signature counts from the database
        /// </summary>
        /// <returns>Number of sources, publishers, games, and rom signatures in the database</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Models.Signatures_Status Status()
        {
            return new Models.Signatures_Status();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<Models.Signatures_Games> GetSignature(string md5 = "", string sha1 = "")
        {
            if (md5.Length > 0)
            {
                return _GetSignature("signatures_roms.md5 = @searchstring", md5);
            } else
            {
                return _GetSignature("signatures_roms.sha1 = @searchstring", sha1);
            }
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<Models.Signatures_Games> GetByTosecName(string TosecName = "")
        {
            if (TosecName.Length > 0)
            {
                return _GetSignature("signatures_roms.name = @searchstring", TosecName);
            } else
            {
                return null;
            }
        }

        private List<Models.Signatures_Games> _GetSignature(string sqlWhere, string searchString)
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT \n    view_signatures_games.*,\n    signatures_roms.id AS romid,\n    signatures_roms.name AS romname,\n    signatures_roms.size,\n    signatures_roms.crc,\n    signatures_roms.md5,\n    signatures_roms.sha1,\n    signatures_roms.developmentstatus,\n    signatures_roms.flags,\n    signatures_roms.romtype,\n    signatures_roms.romtypemedia,\n    signatures_roms.medialabel,\n    signatures_roms.metadatasource\nFROM\n    signatures_roms\n        INNER JOIN\n    view_signatures_games ON signatures_roms.gameid = view_signatures_games.id WHERE " + sqlWhere;
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("searchString", searchString);

            DataTable sigDb = db.ExecuteCMD(sql, dbDict);

            List<Models.Signatures_Games> GamesList = new List<Models.Signatures_Games>();

            foreach (DataRow sigDbRow in sigDb.Rows)
            {
                Models.Signatures_Games gameItem = new Models.Signatures_Games
                {
                    Game = new Models.Signatures_Games.GameItem
                    {
                        Id = (Int32)sigDbRow["id"],
                        Name = (string)sigDbRow["name"],
                        Description = (string)sigDbRow["description"],
                        Year = (string)sigDbRow["year"],
                        Publisher = (string)sigDbRow["publisher"],
                        Demo = (Models.Signatures_Games.GameItem.DemoTypes)(int)sigDbRow["demo"],
                        System = (string)sigDbRow["platform"],
                        SystemVariant = (string)sigDbRow["systemvariant"],
                        Video = (string)sigDbRow["video"],
                        Country = (string)sigDbRow["country"],
                        Language = (string)sigDbRow["language"],
                        Copyright = (string)sigDbRow["copyright"]
                    },
                    Rom = new Models.Signatures_Games.RomItem
                    {
                        Id = (Int32)sigDbRow["romid"],
                        Name = (string)sigDbRow["romname"],
                        Size = (Int64)sigDbRow["size"],
                        Crc = (string)sigDbRow["crc"],
                        Md5 = (string)sigDbRow["md5"],
                        Sha1 = (string)sigDbRow["sha1"],
                        DevelopmentStatus = (string)sigDbRow["developmentstatus"],
                        flags = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>((string)sigDbRow["flags"]),
                        RomType = (Models.Signatures_Games.RomItem.RomTypes)(int)sigDbRow["romtype"],
                        RomTypeMedia = (string)sigDbRow["romtypemedia"],
                        MediaLabel = (string)sigDbRow["medialabel"],
                        SignatureSource = (Models.Signatures_Games.RomItem.SignatureSourceType)(Int32)sigDbRow["metadatasource"]
                    }
                };
                GamesList.Add(gameItem);
            }
            return GamesList;
        }
    }
}

