using System.Data;
using gaseous_server.Models;
using gaseous_signature_parser.models.RomSignatureObject;

namespace gaseous_server.Classes
{
    public class SignatureManagement
    {
        public List<gaseous_server.Models.Signatures_Games> GetSignature(string md5 = "", string sha1 = "")
        {
            if (md5.Length > 0)
            {
                return _GetSignature("Signatures_Roms.md5 = @searchstring", md5);
            }
            else
            {
                return _GetSignature("Signatures_Roms.sha1 = @searchstring", sha1);
            }
        }

        public List<gaseous_server.Models.Signatures_Games> GetByTosecName(string TosecName = "")
        {
            if (TosecName.Length > 0)
            {
                return _GetSignature("Signatures_Roms.name = @searchstring", TosecName);
            }
            else
            {
                return null;
            }
        }

        private List<gaseous_server.Models.Signatures_Games> _GetSignature(string sqlWhere, string searchString)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT     view_Signatures_Games.*,    Signatures_Roms.Id AS romid,    Signatures_Roms.Name AS romname,    Signatures_Roms.Size,    Signatures_Roms.CRC,    Signatures_Roms.MD5,    Signatures_Roms.SHA1,    Signatures_Roms.DevelopmentStatus,    Signatures_Roms.Attributes,    Signatures_Roms.RomType,    Signatures_Roms.RomTypeMedia,    Signatures_Roms.MediaLabel,    Signatures_Roms.MetadataSource FROM    Signatures_Roms        INNER JOIN    view_Signatures_Games ON Signatures_Roms.GameId = view_Signatures_Games.Id WHERE " + sqlWhere;
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("searchString", searchString);

            DataTable sigDb = db.ExecuteCMD(sql, dbDict);

            List<gaseous_server.Models.Signatures_Games> GamesList = new List<gaseous_server.Models.Signatures_Games>();

            foreach (DataRow sigDbRow in sigDb.Rows)
            {
                gaseous_server.Models.Signatures_Games gameItem = new gaseous_server.Models.Signatures_Games
                {
                    Game = new gaseous_server.Models.Signatures_Games.GameItem
                    {
                        Id = (Int32)sigDbRow["Id"],
                        Name = (string)sigDbRow["Name"],
                        Description = (string)sigDbRow["Description"],
                        Year = (string)sigDbRow["Year"],
                        Publisher = (string)sigDbRow["Publisher"],
                        Demo = (gaseous_server.Models.Signatures_Games.GameItem.DemoTypes)(int)sigDbRow["Demo"],
                        System = (string)sigDbRow["Platform"],
                        SystemVariant = (string)sigDbRow["SystemVariant"],
                        Video = (string)sigDbRow["Video"],
                        Country = "",
                        Language = "",
                        Copyright = (string)sigDbRow["Copyright"]
                    },
                    Rom = new gaseous_server.Models.Signatures_Games.RomItem
                    {
                        Id = (Int32)sigDbRow["romid"],
                        Name = (string)sigDbRow["romname"],
                        Size = (Int64)sigDbRow["Size"],
                        Crc = (string)sigDbRow["CRC"],
                        Md5 = ((string)sigDbRow["MD5"]).ToLower(),
                        Sha1 = ((string)sigDbRow["SHA1"]).ToLower(),
                        DevelopmentStatus = (string)sigDbRow["DevelopmentStatus"],
                        RomType = (gaseous_server.Models.Signatures_Games.RomItem.RomTypes)(int)sigDbRow["RomType"],
                        RomTypeMedia = (string)sigDbRow["RomTypeMedia"],
                        MediaLabel = (string)sigDbRow["MediaLabel"],
                        SignatureSource = (gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType)(Int32)sigDbRow["MetadataSource"]
                    }
                };
                string attributeValues = (string)Common.ReturnValueIfNull(sigDbRow["Attributes"], "[]");
                Dictionary<string, object> attributesDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(attributeValues);
                if (attributesDict != null)
                {
                    gameItem.Rom.Attributes = [.. attributesDict];
                }
                else
                {
                    gameItem.Rom.Attributes = new List<KeyValuePair<string, object>>();
                }
                GamesList.Add(gameItem);
            }
            return GamesList;
        }

        public List<Signatures_Sources> GetSources()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM Signatures_Sources ORDER BY `SourceType`, `Name`;";
            DataTable sigDb = db.ExecuteCMD(sql);

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

        public void DeleteSource(int sourceId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM Signatures_Sources WHERE Id = @sourceId;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "sourceId", sourceId }
            };
            db.ExecuteCMD(sql, dbDict);
        }
    }
}