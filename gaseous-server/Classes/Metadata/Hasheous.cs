using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;

namespace gaseous_server.Classes.Metadata
{
    public static class Hasheous
    {
        /// <summary>
        /// The Hasheous client instance - only use via the hasheous property.
        /// </summary>
        private static HasheousClient.Hasheous? _hasheous;
        /// <summary>
        /// Gets the Hasheous client instance, initializing it if necessary.
        /// </summary>
        private static HasheousClient.Hasheous hasheous
        {
            get
            {
                if (_hasheous == null)
                {
                    _hasheous = new HasheousClient.Hasheous();

                    // Configure the Hasheous client
                    HasheousClient.WebApp.HttpHelper.BaseUri = Config.MetadataConfiguration.HasheousHost;

                    // Set the API key for Hasheous Proxy
                    if (HasheousClient.WebApp.HttpHelper.ClientKey == null || HasheousClient.WebApp.HttpHelper.ClientKey != Config.MetadataConfiguration.HasheousClientAPIKey)
                    {
                        HasheousClient.WebApp.HttpHelper.ClientKey = Config.MetadataConfiguration.HasheousClientAPIKey;
                    }
                }
                return _hasheous;
            }
        }

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

            // fetch all platforms
            if (hasheousPlatforms.Count == 0)
            {
                hasheousPlatforms = hasheous.GetPlatforms();
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
                        Uri logoUrl = new Uri(
                            new Uri(HasheousClient.WebApp.HttpHelper.BaseUri, UriKind.Absolute),
                            new Uri("/api/v1/images/" + hasheousPlatformAttribute.Value, UriKind.Relative));

                        // generate a platform logo object
                        platformLogo = new gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.PlatformLogo
                        {
                            AlphaChannel = false,
                            Animated = false,
                            ImageId = (string)hasheousPlatformAttribute.Value,
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