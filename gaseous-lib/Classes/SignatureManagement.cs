using System.Data;
using System.Threading.Tasks;
using gaseous_server.Models;
using gaseous_signature_parser.models.RomSignatureObject;
using HasheousClient.Models;
using static gaseous_server.Classes.Common;

namespace gaseous_server.Classes
{
    public class SignatureManagement
    {
        public async Task<List<gaseous_server.Models.Signatures_Games>> GetSignature(HashObject hashes)
        {
            // Check if any hashes are provided
            // Search in the order of SHA256, SHA1, MD5, CRC32
            // If none are provided, return an empty list
            if (hashes.sha256hash != null && hashes.sha256hash.Length > 0)
            {
                return await _GetSignature("Signatures_Roms.sha256 = @searchstring", hashes.sha256hash.ToLower());
            }
            else if (hashes.sha1hash != null && hashes.sha1hash.Length > 0)
            {
                return await _GetSignature("Signatures_Roms.sha1 = @searchstring", hashes.sha1hash.ToLower());
            }
            else if (hashes.md5hash != null && hashes.md5hash.Length > 0)
            {
                return await _GetSignature("Signatures_Roms.md5 = @searchstring", hashes.md5hash.ToLower());
            }
            else if (hashes.crc32hash != null && hashes.crc32hash.Length > 0)
            {
                return await _GetSignature("Signatures_Roms.crc = @searchstring", hashes.crc32hash.ToLower());
            }
            else
            {
                return new List<gaseous_server.Models.Signatures_Games>();
            }
        }

        public async Task<List<gaseous_server.Models.Signatures_Games>> GetByTosecName(string TosecName = "")
        {
            if (TosecName.Length > 0)
            {
                return await _GetSignature("Signatures_Roms.name = @searchstring", TosecName);
            }
            else
            {
                return null;
            }
        }

        private async Task<List<gaseous_server.Models.Signatures_Games>> _GetSignature(string sqlWhere, string searchString)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT     view_Signatures_Games.*,    Signatures_Roms.Id AS romid,    Signatures_Roms.Name AS romname,    Signatures_Roms.Size,    Signatures_Roms.CRC,    Signatures_Roms.MD5,    Signatures_Roms.SHA1,    Signatures_Roms.CRC,   Signatures_Roms.DevelopmentStatus,    Signatures_Roms.Attributes,    Signatures_Roms.RomType,    Signatures_Roms.RomTypeMedia,    Signatures_Roms.MediaLabel,    Signatures_Roms.MetadataSource FROM    Signatures_Roms        INNER JOIN    view_Signatures_Games ON Signatures_Roms.GameId = view_Signatures_Games.Id WHERE " + sqlWhere;
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("searchString", searchString);

            DataTable sigDb = await db.ExecuteCMDAsync(sql, dbDict);

            List<gaseous_server.Models.Signatures_Games> GamesList = new List<gaseous_server.Models.Signatures_Games>();

            foreach (DataRow sigDbRow in sigDb.Rows)
            {
                gaseous_server.Models.Signatures_Games gameItem = new gaseous_server.Models.Signatures_Games
                {
                    Game = new gaseous_server.Models.Signatures_Games.GameItem
                    {
                        Id = (long)(int)sigDbRow["Id"],
                        Name = (string)sigDbRow["Name"],
                        Description = (string)sigDbRow["Description"],
                        Year = (string)sigDbRow["Year"],
                        Publisher = (string)sigDbRow["Publisher"],
                        Demo = (gaseous_server.Models.Signatures_Games.GameItem.DemoTypes)(int)sigDbRow["Demo"],
                        System = (string)sigDbRow["Platform"],
                        SystemVariant = (string)sigDbRow["SystemVariant"],
                        Video = (string)sigDbRow["Video"],
                        Countries = new Dictionary<string, string>(await GetLookup(LookupTypes.Country, (long)(int)sigDbRow["Id"])),
                        Languages = new Dictionary<string, string>(await GetLookup(LookupTypes.Language, (long)(int)sigDbRow["Id"])),
                        Copyright = (string)sigDbRow["Copyright"]
                    },
                    Rom = new gaseous_server.Models.Signatures_Games.RomItem
                    {
                        Id = (long)(int)sigDbRow["romid"],
                        Name = (string)sigDbRow["romname"],
                        Size = (Int64)sigDbRow["Size"],
                        Crc = ((string)sigDbRow["CRC"]).ToLower(),
                        Md5 = ((string)sigDbRow["MD5"]).ToLower(),
                        Sha1 = ((string)sigDbRow["SHA1"]).ToLower(),
                        DevelopmentStatus = (string)sigDbRow["DevelopmentStatus"],
                        Attributes = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>((string)Common.ReturnValueIfNull(sigDbRow["Attributes"], "[]")),
                        RomType = (gaseous_server.Models.Signatures_Games.RomItem.RomTypes)(int)sigDbRow["RomType"],
                        RomTypeMedia = (string)sigDbRow["RomTypeMedia"],
                        MediaLabel = (string)sigDbRow["MediaLabel"],
                        SignatureSource = (gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType)(Int32)sigDbRow["MetadataSource"]
                    }
                };
                GamesList.Add(gameItem);
            }
            return GamesList;
        }

        public async Task<List<Signatures_Sources>> GetSources()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM Signatures_Sources ORDER BY `SourceType`, `Name`;";
            DataTable sigDb = await db.ExecuteCMDAsync(sql);

            List<Signatures_Sources> SourcesList = new List<Signatures_Sources>();

            foreach (DataRow sigDbRow in sigDb.Rows)
            {
                Signatures_Sources sourceItem = new Signatures_Sources
                {
                    Id = (int)sigDbRow["Id"],
                    Name = (string)sigDbRow["Name"],
                    Description = (string)sigDbRow["Description"],
                    URL = (string)sigDbRow["URL"],
                    Category = (string)sigDbRow["Category"],
                    Version = (string)sigDbRow["Version"],
                    Author = (string)sigDbRow["Author"],
                    Email = (string)sigDbRow["Email"],
                    Homepage = (string)sigDbRow["Homepage"],
                    SourceType = (gaseous_signature_parser.parser.SignatureParser)Enum.Parse(typeof(gaseous_signature_parser.parser.SignatureParser), sigDbRow["SourceType"].ToString()),
                    MD5 = (string)sigDbRow["SourceMD5"],
                    SHA1 = (string)sigDbRow["SourceSHA1"]
                };
                SourcesList.Add(sourceItem);
            }
            return SourcesList;
        }

        public async Task DeleteSource(int sourceId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM Signatures_Sources WHERE Id = @sourceId;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "sourceId", sourceId }
            };
            await db.ExecuteCMDAsync(sql, dbDict);
        }

        public async Task<Dictionary<string, string>> GetLookup(LookupTypes LookupType, long GameId)
        {
            string tableName = "";
            switch (LookupType)
            {
                case LookupTypes.Country:
                    tableName = "Countries";
                    break;

                case LookupTypes.Language:
                    tableName = "Languages";
                    break;

            }

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT " + LookupType.ToString() + ".Code, " + LookupType.ToString() + ".Value FROM Signatures_Games_" + tableName + " JOIN " + LookupType.ToString() + " ON Signatures_Games_" + tableName + "." + LookupType.ToString() + "Id = " + LookupType.ToString() + ".Id WHERE Signatures_Games_" + tableName + ".GameId = @id;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>{
                { "id", GameId }
            };
            DataTable data = await db.ExecuteCMDAsync(sql, dbDict);

            Dictionary<string, string> returnDict = new Dictionary<string, string>();
            foreach (DataRow row in data.Rows)
            {
                returnDict.Add((string)row["Code"], (string)row["Value"]);
            }

            return returnDict;
        }
    }
}