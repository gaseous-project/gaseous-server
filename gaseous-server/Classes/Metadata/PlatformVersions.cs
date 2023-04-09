using System;
using System.Data;
using gaseous_tools;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
	public class PlatformVersions
	{
		public PlatformVersions()
		{
		}

        public static PlatformVersion UnknownPlatformVersion
        {
            get
            {
                PlatformVersion unkownPlatformVersion = new PlatformVersion
                {
                    Id = 0,
                    Checksum = "",
                    Companies = new IdentitiesOrValues<PlatformVersionCompany>(),
                    Connectivity = "",
                    CPU = "",
                    Graphics = "",
                    MainManufacturer = new IdentityOrValue<PlatformVersionCompany>(0),
                    Media = "",
                    Memory = "",
                    Name = "Unknown",
                    OS = "",
                    Output = "",
                    PlatformLogo = new IdentityOrValue<PlatformLogo>(0),
                    PlatformVersionReleaseDates = new IdentitiesOrValues<PlatformVersionReleaseDate>(),
                    Resolutions = "",
                    Slug = "Unknown",
                    Sound = "",
                    Storage = "",
                    Summary = "",
                    Url = ""
                };

                return unkownPlatformVersion;
            }
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static PlatformVersion GetPlatformVersion(long Id, Platform ParentPlatform)
        {
            if (Id == 0)
            {
                return UnknownPlatformVersion;
            }
            else
            {
                Task<PlatformVersion> RetVal = _GetPlatformVersion(SearchUsing.id, Id, ParentPlatform);
                return RetVal.Result;
            }
        }

        public static PlatformVersion GetPlatformVersion(string Slug, Platform ParentPlatform)
        {
            Task<PlatformVersion> RetVal = _GetPlatformVersion(SearchUsing.slug, Slug, ParentPlatform);
            return RetVal.Result;
        }

        private static async Task<PlatformVersion> _GetPlatformVersion(SearchUsing searchUsing, object searchValue, Platform ParentPlatform)
        {
            // check database first
            PlatformVersion? platformVersion = DBGetPlatformVersion(searchUsing, searchValue);

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

            if (platformVersion == null)
            {
                // get platform version metadata
                var results = await igdb.QueryAsync<PlatformVersion>(IGDBClient.Endpoints.PlatformVersions, query: "fields checksum,companies,connectivity,cpu,graphics,main_manufacturer,media,memory,name,online,os,output,platform_logo,platform_version_release_dates,resolutions,slug,sound,storage,summary,url; " + WhereClause + ";");
                var result = results.First();

                DBInsertPlatformVersion(result, true);

                // get platform logo
                if (result.PlatformLogo != null)
                {
                    PlatformLogos.GetPlatformLogo((long)result.PlatformLogo.Id, Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Platform(ParentPlatform), result.Slug, "platform_logo.jpg"));
                }

                return result;
            }
            else
            {
                return platformVersion;
            }
        }

        private static PlatformVersion? DBGetPlatformVersion(SearchUsing searchUsing, object searchValue)
        {
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            switch (searchUsing)
            {
                case SearchUsing.id:
                    dbDict.Add("id", searchValue);

                    return _DBGetPlatformVersion("SELECT * FROM platforms_versions WHERE id = @id", dbDict);

                case SearchUsing.slug:
                    dbDict.Add("slug", searchValue);

                    return _DBGetPlatformVersion("SELECT * FROM platforms_versions WHERE slug = @slug", dbDict);

                default:
                    throw new Exception("Invalid Search Type");
            }
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static PlatformVersion? _DBGetPlatformVersion(string sql, Dictionary<string, object> searchParams)
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            DataTable dbResponse = db.ExecuteCMD(sql, searchParams);

            if (dbResponse.Rows.Count > 0)
            {
                return ConvertDataRowToPlatformVersion(dbResponse.Rows[0]);
            }
            else
            {
                return null;
            }
        }

        private static PlatformVersion ConvertDataRowToPlatformVersion(DataRow PlatformDR)
        {
            PlatformVersion returnPlatformVersion = new PlatformVersion
            {
                Id = (long)(UInt64)PlatformDR["id"],
                Checksum = (string?)PlatformDR["checksum"],
                Companies = Newtonsoft.Json.JsonConvert.DeserializeObject<IdentitiesOrValues<PlatformVersionCompany>>((string?)PlatformDR["companies"]),
                Connectivity = (string?)PlatformDR["connectivity"],
                CPU = (string?)PlatformDR["cpu"],
                Graphics = (string?)PlatformDR["graphics"],
                MainManufacturer = new IdentityOrValue<PlatformVersionCompany>((int?)PlatformDR["main_manufacturer"]),
                Media = (string?)PlatformDR["media"],
                Memory = (string?)PlatformDR["memory"],
                Name = (string?)PlatformDR["name"],
                OS = (string?)PlatformDR["os"],
                Output = (string?)PlatformDR["output"],
                PlatformLogo = new IdentityOrValue<PlatformLogo>((int?)PlatformDR["platform_logo"]),
                PlatformVersionReleaseDates = Newtonsoft.Json.JsonConvert.DeserializeObject<IdentitiesOrValues<PlatformVersionReleaseDate>>((string?)PlatformDR["platform_version_release_dates"]),
                Resolutions = (string?)PlatformDR["resolutions"],
                Slug = (string?)PlatformDR["slug"],
                Sound = (string?)PlatformDR["sound"],
                Storage = (string?)PlatformDR["storage"],
                Summary = (string?)PlatformDR["summary"],
                Url = (string?)PlatformDR["url"]
            };

            return returnPlatformVersion;
        }

        private static void DBInsertPlatformVersion(PlatformVersion PlatformVersionItem, bool Insert)
        {
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO platforms_versions (id, checksum, connectivity, cpu, graphics, main_manufacturer, media, memory, name, os, output, platform_logo, platform_version_release_dates, resolutions, slug, sound, storage, summary, url, dateAdded, lastUpdated) VALUES (@id, @checksum, @connectivity, @cpu, @graphics, @main_manufacturer, @media, @memory, @name, @os, @output, @platform_logo, @platform_version_release_dates, @resolutions, @slug, @sound, @storage, @summary, @url, @lastUpdated, @lastUpdated)";
            if (Insert == false)
            {
                sql = "UPDATE platforms_versions SET checksum=@checksum, connectivity=@connectivity, cpu=@cpu, graphics=@graphics, main_manufacturer=@main_manufacturer, media=@media, memory=@memory, name=@name, os=@os, output=@output, platform_logo=@platform_logo, platform_version_release_dates=@platform_version_release_dates, resolutions=@resolutions, slug=@slug, sound=@sound, storage=@storage, summary=@summary, url=@url, lastUpdated=@lastUpdated WHERE id=@id";
            }
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            string EmptyJson = "{\"Ids\": [], \"Values\": null}";
            dbDict.Add("id", PlatformVersionItem.Id);
            dbDict.Add("checksum", Common.ReturnValueIfNull(PlatformVersionItem.Checksum, ""));
            dbDict.Add("connectivity", Common.ReturnValueIfNull(PlatformVersionItem.Connectivity, ""));
            dbDict.Add("cpu", Common.ReturnValueIfNull(PlatformVersionItem.CPU, ""));
            dbDict.Add("graphics", Common.ReturnValueIfNull(PlatformVersionItem.Graphics, ""));
            if (PlatformVersionItem.MainManufacturer == null)
            {
                dbDict.Add("main_manufacturer", 0);
            }
            else
            {
                dbDict.Add("main_manufacturer", Common.ReturnValueIfNull(PlatformVersionItem.MainManufacturer.Id, 0));
            }
            dbDict.Add("media", Common.ReturnValueIfNull(PlatformVersionItem.Media, ""));
            dbDict.Add("memory", Common.ReturnValueIfNull(PlatformVersionItem.Memory, ""));
            dbDict.Add("name", Common.ReturnValueIfNull(PlatformVersionItem.Name, ""));
            dbDict.Add("os", Common.ReturnValueIfNull(PlatformVersionItem.OS, ""));
            dbDict.Add("output", Common.ReturnValueIfNull(PlatformVersionItem.Output, ""));
            if (PlatformVersionItem.PlatformLogo == null)
            {
                dbDict.Add("platform_logo", 0);
            }
            else
            {
                dbDict.Add("platform_logo", Common.ReturnValueIfNull(PlatformVersionItem.PlatformLogo.Id, 0));
            }
            if (PlatformVersionItem.PlatformVersionReleaseDates == null)
            {
                dbDict.Add("platform_version_release_dates", EmptyJson);
            }
            else
            {
                dbDict.Add("platform_version_release_dates", Newtonsoft.Json.JsonConvert.SerializeObject(PlatformVersionItem.PlatformVersionReleaseDates));
            }
            dbDict.Add("resolutions", Common.ReturnValueIfNull(PlatformVersionItem.Resolutions, ""));
            dbDict.Add("slug", Common.ReturnValueIfNull(PlatformVersionItem.Slug, ""));
            dbDict.Add("sound", Common.ReturnValueIfNull(PlatformVersionItem.Sound, ""));
            dbDict.Add("storage", Common.ReturnValueIfNull(PlatformVersionItem.Storage, ""));
            dbDict.Add("summary", Common.ReturnValueIfNull(PlatformVersionItem.Summary, ""));
            dbDict.Add("url", Common.ReturnValueIfNull(PlatformVersionItem.Url, ""));
            dbDict.Add("lastUpdated", DateTime.UtcNow);

            db.ExecuteCMD(sql, dbDict);
        }
    }
}

