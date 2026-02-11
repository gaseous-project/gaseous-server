using System.Security.AccessControl;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public static class Hasheous
    {
        /// <summary>
        /// HTTP communications handler for making API requests.
        /// </summary>
        private static readonly HTTPComms comms = new HTTPComms();

        private static gaseous_server.Classes.Plugins.MetadataProviders.Storage storage = new gaseous_server.Classes.Plugins.MetadataProviders.Storage(FileSignature.MetadataSources.None);


        static List<HasheousClient.Models.DataObjectItem> hasheousPlatforms = new List<HasheousClient.Models.DataObjectItem>();
        public static async Task PopulateHasheousPlatformData(long Id)
        {
            // get the platform object from the cache
            Platform? platform = await Platforms.GetPlatform(Id);
            if (platform == null)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.populate_hasheous_platform_data", "populatehasheousplatformdata.platform_with_id_not_found_in_cache", null, new string[] { Id.ToString() });
                return;
            }

            HTTPComms comms = new HTTPComms();
            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(Config.MetadataConfiguration.HasheousAPIKey))
            {
                headers.Add("X-API-Key", Config.MetadataConfiguration.HasheousAPIKey);
            }
            headers.Add("X-Client-API-Key", Config.MetadataConfiguration.HasheousClientAPIKey);
            headers.Add("CacheControl", "no-cache");
            headers.Add("Pragma", "no-cache");

            // fetch all platforms
            if (hasheousPlatforms.Count == 0)
            {
                var response = await comms.SendRequestAsync<HasheousClient.Models.DataObjectsList>(HTTPComms.HttpMethod.GET, new Uri("https://hasheous.org/api/v1/Lookup/Platforms?PageSize=50&PageNumber=1"), headers);
                if (response != null && response.StatusCode == 200)
                {
                    if (response.Body != null)
                    {
                        hasheousPlatforms = new List<HasheousClient.Models.DataObjectItem>(response.Body.Objects);

                        if (response.Body.TotalPages > 1)
                        {
                            for (int i = 2; i <= response.Body.TotalPages; i++)
                            {
                                var pagedResponse = await comms.SendRequestAsync<HasheousClient.Models.DataObjectsList>(HTTPComms.HttpMethod.GET, new Uri($"https://hasheous.org/api/v1/Lookup/Platforms?PageSize=50&PageNumber={i}"), headers);
                                if (pagedResponse != null && pagedResponse.StatusCode == 200 && pagedResponse.Body != null)
                                {
                                    hasheousPlatforms.AddRange(pagedResponse.Body.Objects);
                                }
                            }
                        }
                    }
                }
            }

            // loop through the platforms and check if the metadata matches the IGDB platform id
            if (hasheousPlatforms == null || hasheousPlatforms.Count == 0)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.populate_hasheous_platform_data", "populatehasheousplatformdata.no_platforms_found_in_hasheous");
                return;
            }

            // search through hasheousPlatforms for a match where the metadata source is IGDB and the immutable id matches the platform id, or the metadata source is IGDB and the id matches the platform slug
            HasheousClient.Models.DataObjectItem? hasheousPlatform = hasheousPlatforms.FirstOrDefault(p =>
                p.Metadata != null &&
                p.Metadata.Any(m => m.Source == FileSignature.MetadataSources.IGDB.ToString() && (
                    (m.ImmutableId != null && m.ImmutableId.Length > 0 && long.TryParse(m.ImmutableId, out long objId) && objId == Id) ||
                    (m.Id != null && m.Id.Equals(platform.Slug, StringComparison.OrdinalIgnoreCase))
                ))
            );

            if (hasheousPlatform == null)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.populate_hasheous_platform_data", "populatehasheousplatformdata.no_matching_platform_found_for_id", null, new string[] { Id.ToString() });
                return;
            }

            // check the attributes for a logo
            gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.PlatformLogo? platformLogo = null;

            if (hasheousPlatform.Attributes != null && hasheousPlatform.Attributes.Count > 0)
            {
                foreach (var hasheousPlatformAttribute in hasheousPlatform.Attributes)
                {
                    if (
                        hasheousPlatformAttribute.attributeType == HasheousClient.Models.AttributeItem.AttributeType.ImageId &&
                        hasheousPlatformAttribute.attributeName == HasheousClient.Models.AttributeItem.AttributeName.Logo &&
                        hasheousPlatformAttribute.Value != null
                        )
                    {
                        // Convert JsonElement to string if necessary
                        string imageIdValue = hasheousPlatformAttribute.Value is System.Text.Json.JsonElement jsonElement
                            ? jsonElement.GetString() ?? hasheousPlatformAttribute.Value.ToString()
                            : hasheousPlatformAttribute.Value.ToString();

                        Uri logoUrl = new Uri(
                            new Uri(HasheousClient.WebApp.HttpHelper.BaseUri, UriKind.Absolute),
                            new Uri("/api/v1/images/" + imageIdValue, UriKind.Relative));

                        // generate a platform logo object
                        platformLogo = new gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.PlatformLogo
                        {
                            AlphaChannel = false,
                            Animated = false,
                            ImageId = imageIdValue,
                            Url = logoUrl.ToString()
                        };

                        // generate a long id from the value
                        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(platformLogo.ImageId);
                        long longId = BitConverter.ToInt64(bytes, 0);
                        platformLogo.Id = longId;

                        // store the platform logo object
                        await storage.StoreCacheValue<PlatformLogo>(platformLogo);

                        break;
                    }
                }
            }

            // update the platform object with the name and logo id
            if (platform != null)
            {
                platform.Name = hasheousPlatform.Name;
                if (platformLogo != null && platformLogo.Id.HasValue)
                {
                    platform.PlatformLogo = platformLogo.Id.Value;
                }
                await storage.StoreCacheValue<Platform>(platform);
            }
            Logging.LogKey(Logging.LogType.Information, "process.populate_hasheous_platform_data", "populatehasheousplatformdata.platform_data_populated_for_id", null, new string[] { Id.ToString() });
        }
    }
}