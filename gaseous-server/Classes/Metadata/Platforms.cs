using System;
using System.Data;
using System.Net;
using gaseous_tools;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
	public class Platforms
	{
		public Platforms()
		{
		}

        public static Platform UnknownPlatform
        {
            get
            {
                Platform unkownPlatform = new Platform
                {
                    Id = 0,
                    Abbreviation = "",
                    AlternativeName = "",
                    Category = PlatformCategory.Computer,
                    Checksum = "",
                    CreatedAt = DateTime.UtcNow,
                    Generation = 1,
                    Name = "Unknown",
                    PlatformFamily = new IdentityOrValue<PlatformFamily>(0),
                    PlatformLogo = new IdentityOrValue<PlatformLogo>(0),
                    Slug = "Unknown",
                    Summary = "",
                    UpdatedAt = DateTime.UtcNow,
                    Url = "",
                    Versions = new IdentitiesOrValues<PlatformVersion>(),
                    Websites = new IdentitiesOrValues<PlatformWebsite>()
                };

                return unkownPlatform;
            }
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static Platform GetPlatform(int Id)
		{
            if (Id == 0)
            {
                return UnknownPlatform;
            }
            else
            {
                Task<Platform> RetVal = _GetPlatform(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

		public static Platform GetPlatform(string Slug)
		{
			Task<Platform> RetVal = _GetPlatform(SearchUsing.slug, Slug);
			return RetVal.Result;
		}

		private static async Task<Platform> _GetPlatform(SearchUsing searchUsing, object searchValue)
		{
            // check database first
            Platform? platform = DBGetPlatform(searchUsing, searchValue);

            // set up where clause
            string WhereClause = "";
            switch (searchUsing)
            {
                case SearchUsing.id:
                    WhereClause = "where id = " + searchValue;
                    break;
                case SearchUsing.slug:
                    WhereClause = "where slug = " + searchValue;
                    break;
                default:
                    throw new Exception("Invalid search type");
            }

            if (platform == null)
            {
                // get platform metadata
                var results = await igdb.QueryAsync<Platform>(IGDBClient.Endpoints.Platforms, query: "fields abbreviation,alternative_name,category,checksum,created_at,generation,name,platform_family,platform_logo,slug,summary,updated_at,url,versions,websites; " + WhereClause + ";");
                var result = results.First();

                DBInsertPlatform(result, true);

                // get platform logo
                if (result.PlatformLogo != null)
                {
                    var logo_results = await igdb.QueryAsync<PlatformLogo>(IGDBClient.Endpoints.PlatformLogos, query: "fields alpha_channel,animated,checksum,height,image_id,url,width; where id = " + result.PlatformLogo.Id + ";");
                    var logo_result = logo_results.First();

                    using (var client = new HttpClient())
                    {
                        using (var s = client.GetStreamAsync("https:" + logo_result.Url))
                        {
                            using (var fs = new FileStream(Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(result), "platform_logo.jpg"), FileMode.OpenOrCreate))
                            {
                                s.Result.CopyTo(fs);
                            }
                        }
                    }
                }

                return result;
            }
            else
            {   
                return platform;
            }
        }

        private static Platform? DBGetPlatform(SearchUsing searchUsing, object searchValue)
        {
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            switch (searchUsing)
            {
                case SearchUsing.id:
                    dbDict.Add("id", searchValue);

                    return _DBGetPlatform("SELECT * FROM platforms WHERE id = @id", dbDict);

                case SearchUsing.slug:
                    dbDict.Add("slug", searchValue);

                    return _DBGetPlatform("SELECT * FROM platforms WHERE slug = @slug", dbDict);

                default:
                    throw new Exception("Invalid Search Type");
            }
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static Platform? _DBGetPlatform(string sql, Dictionary<string, object> searchParams)
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            DataTable dbResponse = db.ExecuteCMD(sql, searchParams);

            if (dbResponse.Rows.Count > 0)
            {
                return ConvertDataRowToPlatform(dbResponse.Rows[0]);
            }
            else
            {
                return null;
            }
        }

        private static Platform ConvertDataRowToPlatform(DataRow PlatformDR)
        {
            Platform returnPlatform = new Platform
            {
                Id = (long)(UInt64)PlatformDR["id"],
                Abbreviation = (string?)PlatformDR["abbreviation"],
                AlternativeName = (string?)PlatformDR["alternative_name"],
                Category = (PlatformCategory)PlatformDR["category"],
                Checksum = (string?)PlatformDR["checksum"],
                CreatedAt = (DateTime?)PlatformDR["created_at"],
                Generation = (int?)PlatformDR["generation"],
                Name = (string?)PlatformDR["name"],
                PlatformFamily = new IdentityOrValue<PlatformFamily>((int?)PlatformDR["platform_family"]),
                PlatformLogo = new IdentityOrValue<PlatformLogo>((int?)PlatformDR["platform_logo"]),
                Slug = (string?)PlatformDR["slug"],
                Summary = (string?)PlatformDR["summary"],
                UpdatedAt = (DateTime?)PlatformDR["updated_at"],
                Url = (string?)PlatformDR["url"],
                Versions = Newtonsoft.Json.JsonConvert.DeserializeObject<IdentitiesOrValues<PlatformVersion>>((string?)PlatformDR["versions"]),
                Websites = Newtonsoft.Json.JsonConvert.DeserializeObject<IdentitiesOrValues<PlatformWebsite>>((string?)PlatformDR["websites"])
            };

            return returnPlatform;
        }

        private static void DBInsertPlatform(Platform PlatformItem, bool Insert)
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO platforms (id, abbreviation, alternative_name, category, checksum, created_at, generation, name, platform_family, platform_logo, slug, summary, updated_at, url, versions, websites, dateAdded, lastUpdated) VALUES (@id, @abbreviation, @alternative_name, @category, @checksum, @created_at, @generation, @name, @platform_family, @platform_logo, @slug, @summary, @updated_at, @url, @versions, @websites, @lastUpdated, @lastUpdated)";
            if (Insert == false)
            {
                sql = "UPDATE platforms SET abbreviation=@abbreviation, alternative_name=@alternative_name, category=@category, checksum=@checksum, created_at=@created_at, generation=@generation, name=@name, platform_family=@platform_family, platform_logo=@platform_logo, slug=@slug, summary=@summary, updated_at=@updated_at, url=@url, versions=@versions, websites=@websites, lastUpdated=@lastUpdated WHERE id=@id";
            }
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("id", PlatformItem.Id);
            dbDict.Add("abbreviation", Common.ReturnValueIfNull(PlatformItem.Abbreviation, ""));
            dbDict.Add("alternative_name", Common.ReturnValueIfNull(PlatformItem.AlternativeName, ""));
            dbDict.Add("category", Common.ReturnValueIfNull(PlatformItem.Category, PlatformCategory.Computer));
            dbDict.Add("checksum", Common.ReturnValueIfNull(PlatformItem.Checksum, ""));
            dbDict.Add("created_at", Common.ReturnValueIfNull(PlatformItem.CreatedAt, DateTime.UtcNow));
            dbDict.Add("generation", Common.ReturnValueIfNull(PlatformItem.Generation, 1));
            dbDict.Add("name", Common.ReturnValueIfNull(PlatformItem.Name, ""));
            if (PlatformItem.PlatformFamily == null)
            {
                dbDict.Add("platform_family", 0);
            }
            else
            {
                dbDict.Add("platform_family", Common.ReturnValueIfNull(PlatformItem.PlatformFamily.Id, 0));
            }
            if (PlatformItem.PlatformLogo == null)
            {
                dbDict.Add("platform_logo", 0);
            }
            else
            {
                dbDict.Add("platform_logo", Common.ReturnValueIfNull(PlatformItem.PlatformLogo.Id, 0));
            }
            dbDict.Add("slug", Common.ReturnValueIfNull(PlatformItem.Slug, ""));
            dbDict.Add("summary", Common.ReturnValueIfNull(PlatformItem.Summary, ""));
            dbDict.Add("updated_at", Common.ReturnValueIfNull(PlatformItem.UpdatedAt, DateTime.UtcNow));
            dbDict.Add("url", Common.ReturnValueIfNull(PlatformItem.Url, ""));
            dbDict.Add("lastUpdated", DateTime.UtcNow);
            string EmptyJson = "{\"Ids\": [], \"Values\": null}";
            if (PlatformItem.Versions == null)
            {
                dbDict.Add("versions", EmptyJson);
            }
            else
            {
                dbDict.Add("versions", Newtonsoft.Json.JsonConvert.SerializeObject(PlatformItem.Versions));
            }
            if (PlatformItem.Websites == null)
            {
                dbDict.Add("websites", EmptyJson);
            }
            else
            {
                dbDict.Add("websites", Newtonsoft.Json.JsonConvert.SerializeObject(PlatformItem.Websites));
            }

            db.ExecuteCMD(sql, dbDict);
        }
    }
}

