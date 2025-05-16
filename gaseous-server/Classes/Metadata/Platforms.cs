using System;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class Platforms
    {
        public const string fieldList = "fields abbreviation,alternative_name,category,checksum,created_at,generation,name,platform_family,platform_logo,slug,summary,updated_at,url,versions,websites;";

        public Platforms()
        {

        }

        public static async Task<Platform?> GetPlatform(long Id, HasheousClient.Models.MetadataSources? SourceType = null)
        {
            HasheousClient.Models.MetadataSources Source = SourceType ?? Communications.MetadataSource;

            if ((Id == 0) || (Id == null))
            {
                Platform returnValue = new Platform();
                if (await Storage.GetCacheStatusAsync(Source, "Platform", 0) == Storage.CacheStatus.NotPresent)
                {
                    returnValue = new Platform
                    {
                        Id = 0,
                        Name = "Unknown Platform",
                        Slug = "unknown"
                    };
                    await Storage.NewCacheValue(Source, returnValue);

                    return returnValue;
                }
                else
                {
                    return await Storage.GetCacheValue<Platform>(Source, returnValue, "id", 0);
                }
            }
            else
            {
                Platform? RetVal = new Platform();
                RetVal = (Platform?)await Storage.GetCacheValue<Platform>(HasheousClient.Models.MetadataSources.None, RetVal, "Id", (long)Id);
                if (Source != HasheousClient.Models.MetadataSources.None)
                {
                    if (RetVal == null)
                    {
                        RetVal = await Metadata.GetMetadataAsync<Platform>(Source, (long)Id, false);
                    }
                }
                return RetVal;
            }
        }

        public static async Task<Platform> GetPlatform(string Slug)
        {
            // get platform id from slug - query Platform table
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string query = "SELECT Id FROM Platform WHERE slug = @slug AND SourceId = @sourceid;";
            DataTable result = await db.ExecuteCMDAsync(query, new Dictionary<string, object> { { "@slug", Slug }, { "@sourceid", HasheousClient.Models.MetadataSources.IGDB } });
            if (result.Rows.Count == 0)
            {
                throw new Metadata.InvalidMetadataId(Slug);
            }

            long Id = (long)result.Rows[0]["Id"];
            return await GetPlatform(Id);
        }
    }
}

