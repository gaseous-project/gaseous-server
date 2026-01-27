
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using HasheousClient.Models;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Reflection;

namespace gaseous_server.Classes.Plugins.MetadataProviders
{
    /// <summary>
    /// Proxy provider for IGDB metadata via Hasheous.
    /// </summary>
    public class HasheousIGDBProxyProvider : IProxyProvider
    {
        /// <inheritdoc/>
        public string Name => throw new NotImplementedException();

        /// <inheritdoc/>
        public FileSignature.MetadataSources SourceType => FileSignature.MetadataSources.IGDB;

        /// <inheritdoc/>
        public gaseous_server.Classes.Plugins.MetadataProviders.Storage? Storage { get; set; }

        /// <inheritdoc/>
        public Dictionary<string, object>? Settings { get; set; }

        /// <inheritdoc/>
        public bool UsesInternet => true;

        /// <summary>
        /// The Hasheous client instance - only use via the hasheous property.
        /// </summary>
        private HasheousClient.Hasheous? _hasheous;
        /// <summary>
        /// Gets the Hasheous client instance, initializing it if necessary.
        /// </summary>
        private HasheousClient.Hasheous hasheous
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
        private readonly HTTPComms comms = new HTTPComms();

        /// <summary>
        /// Retrieves a single entity of type T using the Hasheous IGDB bundle cache.
        /// </summary>
        /// <typeparam name="T">The metadata model type to return.</typeparam>
        /// <param name="itemType">The IGDB item type name (e.g., "games").</param>
        /// <param name="id">The entity identifier.</param>
        /// <returns>The entity instance or null if not found.</returns>
        public async Task<T?> GetEntityAsync<T>(string itemType, long id) where T : class
        {
            if (typeof(T) == typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game))
            {
                return await GetGameBundleAsync<T>(id);
            }
            else
            {
                // only games are supported via bundles - reach out to Hasheous IGDB proxy for other types
                var response = await hasheous.GetMetadataProxyAsync<T>(MetadataSources.IGDB, id);

                return response;
            }
        }

        /// <summary>
        /// Searches for games.
        /// </summary>
        /// <param name="searchType">The type of search to perform.</param>
        /// <param name="platformId">The platform identifier to filter search results.</param>
        /// <param name="searchCandidates">The list of search candidate strings.</param>
        /// <returns>An array of games or null.</returns>
        public async Task<gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game[]?> SearchGamesAsync(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.SearchType searchType, long platformId, List<string> searchCandidates)
        {
            List<gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game> results = new List<gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game>();

            foreach (var candidate in searchCandidates)
            {
                var response = await hasheous.GetMetadataProxy_SearchGameAsync<gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game>(MetadataSources.IGDB, platformId.ToString(), candidate);
                if (response != null)
                {
                    foreach (var item in response)
                    {
                        // check if item is already in results by Id property
                        bool exists = results.Any(r =>
                        {
                            var idProp = r.GetType().GetProperty("Id") ?? r.GetType().GetProperty("ID") ?? r.GetType().GetProperty("id");
                            var itemIdProp = item.GetType().GetProperty("Id") ?? item.GetType().GetProperty("ID") ?? item.GetType().GetProperty("id");
                            if (idProp != null && itemIdProp != null)
                            {
                                var rId = idProp.GetValue(r);
                                var itemId = itemIdProp.GetValue(item);
                                return rId != null && itemId != null && rId.Equals(itemId);
                            }
                            return false;
                        });
                        if (!exists)
                        {
                            // hasheous IGDB proxy search is always a search type search, so if searchType is not "search", we need to filter results here
                            switch (searchType)
                            {
                                case gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.SearchType.where:
                                    // exact match only (on name)
                                    if (string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase))
                                    {
                                        results.Add(item);
                                    }
                                    break;
                                case gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.SearchType.wherefuzzy:
                                    // fuzzy match - contains
                                    if (item.Name != null && item.Name.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        results.Add(item);
                                    }
                                    break;
                                default:
                                    results.Add(item);
                                    break;
                            }
                        }
                    }
                }
            }

            return results.Distinct().ToArray();
        }

        /// <inheritdoc/>
        public async Task<byte[]?> GetImageAsync(long gameId, string url, ImageType imageType, ImageSize imageSize)
        {
            var entity = await GetEntityAsync<Game>("games", gameId);

            if (entity != null)
            {

            }

            return null;
        }

        private async Task<T?> GetGameBundleAsync<T>(long id) where T : class
        {
            // we can only handle bundles for games - check T type
            // abort if T is not Game
            string requestedTypeName = typeof(T).Name;
            if (typeof(T) != typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game))
            {
                return null;
            }

            // check if bundle exists
            string bundlePath = Config.LibraryConfiguration.LibraryMetadataDirectory_GameBundles(SourceType, id);
            bool forceDownloadBundle = false;
            if (Directory.Exists(bundlePath))
            {
                // open the game.json file
                string gameJsonPath = Path.Combine(bundlePath, "Game.json");
                if (!File.Exists(gameJsonPath))
                {
                    forceDownloadBundle = true;
                }
                else
                {
                    // load and deserialize the game.json file to Dictionary<string, object>
                    string gameJsonContent = await File.ReadAllTextAsync(gameJsonPath);
                    Dictionary<string, object>? gameDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(gameJsonContent);
                    if (gameDict != null)
                    {
                        // check the updated_at field - if older than 7 days, redownload
                        if (gameDict.ContainsKey("updated_at"))
                        {
                            if (!DateTime.TryParse(gameDict["updated_at"].ToString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime updatedAt))
                            {
                                // could not parse date, force download
                                forceDownloadBundle = true;
                            }
                            else if (DateTime.UtcNow.Subtract(updatedAt).TotalDays > 7)
                            {
                                // older than 7 days, force download
                                forceDownloadBundle = true;
                            }
                            else
                            {
                                // else, do not force download
                                forceDownloadBundle = false;
                            }
                        }
                        else
                        {
                            forceDownloadBundle = true;
                        }
                    }
                    else
                    {
                        forceDownloadBundle = true;
                    }
                }
            }
            else
            {
                forceDownloadBundle = true;
            }

            if (forceDownloadBundle)
            {
                // download the bundle via Hasheous Proxy
                string bundleUrl = $"{Config.MetadataConfiguration.HasheousHost}/api/v1/MetadataProxy/Bundles/IGDB/{id}.bundle";
                string downloadDirectory = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, Path.GetRandomFileName());
                string downloadedBundlePath = Path.Combine(downloadDirectory, $"{id}.bundle.zip");
                if (Directory.Exists(downloadDirectory) == true)
                {
                    Directory.Delete(downloadDirectory, true);
                }
                Directory.CreateDirectory(downloadDirectory);
                if (!Directory.Exists(bundlePath))
                {
                    Directory.CreateDirectory(bundlePath);
                }
                Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "X-Client-API-Key", Config.MetadataConfiguration.HasheousClientAPIKey }
                };
                var response = await comms.DownloadToFileAsync(bundleUrl, downloadedBundlePath, headers);
                if (response.StatusCode == 200)
                {
                    // extract the bundle
                    using (var zip = SharpCompress.Archives.Zip.ZipArchive.Open(downloadedBundlePath))
                    {
                        foreach (var entry in zip.Entries.Where(entry => !entry.IsDirectory))
                        {
                            await entry.WriteToDirectoryAsync(bundlePath, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }
                    }
                    // delete the temp file
                    try
                    {
                        File.Delete(downloadedBundlePath);
                    }
                    catch
                    {
                        // ignore - should get cleaned up later by the temp file cleaner anyway
                    }
                }
                else
                {
                    // failed to download bundle
                    return null;
                }
            }

            // bundle should now be present for loading - extract the requested entity from the Game.json file
            // open the game.json file and deserialize to Dictionary<string, object>
            string gameJsonFilePath = Path.Combine(bundlePath, "Game.json");
            if (!File.Exists(gameJsonFilePath))
            {
                return null;
            }
            string gameJson = await File.ReadAllTextAsync(gameJsonFilePath);
            Dictionary<string, object>? gameEntity = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(gameJson); ;

            // now build T from the deserialized data and return
            // create a new game instance - this is the root of the bundle, we'll then create sub-entities as needed and store them in the database
            if (gameEntity != null)
            {
                T? game = Activator.CreateInstance(typeof(T)) as T;
                foreach (var prop in typeof(T).GetProperties())
                {
                    // get the json property name from the JsonProperty attribute if present
                    var jsonPropAttr = prop.GetCustomAttributes(typeof(Newtonsoft.Json.JsonPropertyAttribute), true).FirstOrDefault() as Newtonsoft.Json.JsonPropertyAttribute;
                    string jsonPropName = prop.Name;
                    if (jsonPropAttr != null && !string.IsNullOrEmpty(jsonPropAttr.PropertyName))
                    {
                        jsonPropName = jsonPropAttr.PropertyName;
                    }

                    // set the property value if present in the deserialized data
                    if (gameEntity.ContainsKey(jsonPropName))
                    {
                        object? value = gameEntity[jsonPropName];
                        if (value != null)
                        {
                            // handle type conversion if necessary
                            // we need to map types from the dictionary:
                            // - if the object is a List<Dictionary<string, object>>, then we need to extract the keys and convert to List<long> for ID lists, the values themselves should be saved to the database under the type of object they represent
                            // - if the object is a class with an id property (e.g., Company, Genre, etc.), we need to extract the id and set that as long
                            if (prop.PropertyType == typeof(List<long>))
                            {
                                List<long> idList = new List<long>();

                                // convert value to the appropriate dictionary type using Activator
                                string serializedValue = Newtonsoft.Json.JsonConvert.SerializeObject(value);
                                Type elementType = GetPropertyMetadataType(typeof(T), jsonPropName) ?? typeof(object);
                                Type dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), elementType);
                                var deserializedDict = Activator.CreateInstance(dictType);
                                deserializedDict = Newtonsoft.Json.JsonConvert.DeserializeObject(serializedValue, dictType);

                                if (deserializedDict != null)
                                {
                                    var dictEnumerable = deserializedDict as System.Collections.IDictionary;
                                    if (dictEnumerable != null)
                                    {
                                        foreach (System.Collections.DictionaryEntry item in dictEnumerable)
                                        {
                                            var itemValue = item.Value;
                                            if (itemValue != null)
                                            {
                                                // use reflection to get the id property
                                                var idProp = itemValue.GetType().GetProperty("id") ?? itemValue.GetType().GetProperty("Id") ?? itemValue.GetType().GetProperty("ID");
                                                if (idProp != null)
                                                {
                                                    var idValue = idProp.GetValue(itemValue);
                                                    if (idValue != null)
                                                    {
                                                        idList.Add(Convert.ToInt64(idValue));

                                                        if (Storage != null)
                                                        {
                                                            // save the item to storage using runtime type
                                                            await StoreCacheValueWithRuntimeType(elementType, itemValue);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                prop.SetValue(game, idList);
                            }
                            else if (prop.PropertyType == typeof(long) && value is Newtonsoft.Json.Linq.JObject jObject && jObject["id"] != null)
                            {
                                // store the referenced object to cache if we can resolve its type
                                Type? elementType = GetPropertyMetadataType(typeof(T), jsonPropName);
                                if (Storage != null && elementType != null)
                                {
                                    try
                                    {
                                        var typedObj = jObject.ToObject(elementType);
                                        if (typedObj != null)
                                        {
                                            await StoreCacheValueWithRuntimeType(elementType, typedObj);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error caching {jsonPropName}: {ex.Message}");
                                    }
                                }

                                long idValue = jObject["id"]!.ToObject<long>();
                                prop.SetValue(game, idValue);
                            }
                            else if (prop.PropertyType == typeof(DateTimeOffset?))
                            {
                                // need to convert from DateTime or string to DateTimeOffset
                                DateTimeOffset dtoValue;
                                if (value is DateTime dt)
                                {
                                    dtoValue = new DateTimeOffset(dt);
                                }
                                else
                                {
                                    dtoValue = DateTimeOffset.Parse(value.ToString() ?? string.Empty);
                                }
                                prop.SetValue(game, dtoValue);
                            }
                            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                            {
                                // convert to int
                                try
                                {
                                    int intValue = Convert.ToInt32(value);
                                    prop.SetValue(game, intValue);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error converting int property {jsonPropName}: {ex.Message}");
                                }
                            }
                            else if (prop.PropertyType == typeof(uint) || prop.PropertyType == typeof(uint?))
                            {
                                // convert to uint
                                try
                                {
                                    uint uintValue = Convert.ToUInt32(value);
                                    prop.SetValue(game, uintValue);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error converting int property {jsonPropName}: {ex.Message}");
                                }
                            }
                            else if (prop.PropertyType == typeof(long) || prop.PropertyType == typeof(long?))
                            {
                                // convert to long
                                try
                                {
                                    long longValue = Convert.ToInt64(value);
                                    prop.SetValue(game, longValue);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error converting long property {jsonPropName}: {ex.Message}");
                                }
                            }
                            else if (prop.PropertyType == typeof(ulong) || prop.PropertyType == typeof(ulong?))
                            {
                                // convert to ulong
                                try
                                {
                                    ulong ulongValue = Convert.ToUInt64(value);
                                    prop.SetValue(game, ulongValue);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error converting long property {jsonPropName}: {ex.Message}");
                                }
                            }
                            else if (prop.PropertyType == typeof(double) || prop.PropertyType == typeof(double?))
                            {
                                // convert to double
                                try
                                {
                                    double doubleValue = Convert.ToDouble(value);
                                    prop.SetValue(game, doubleValue);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error converting long property {jsonPropName}: {ex.Message}");
                                }
                            }
                            else if (prop.PropertyType == typeof(float) || prop.PropertyType == typeof(float?))
                            {
                                // convert to float
                                try
                                {
                                    float floatValue = Convert.ToSingle(value);
                                    prop.SetValue(game, floatValue);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error converting long property {jsonPropName}: {ex.Message}");
                                }
                            }
                            else if (prop.PropertyType.IsEnum)
                            {
                                try
                                {
                                    object? enumValue = Enum.Parse(prop.PropertyType, value.ToString() ?? string.Empty);
                                    prop.SetValue(game, enumValue);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error converting enum property {jsonPropName}: {ex.Message}");
                                }
                            }
                            else
                            {
                                // direct conversion
                                try
                                {
                                    object? convertedValue = Convert.ChangeType(value, prop.PropertyType);
                                    prop.SetValue(game, convertedValue);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error converting property {jsonPropName}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                return game;
            }
            else { return null; }
        }

        /// <summary>
        /// Maps a property name to its corresponding metadata type for deserialization.
        /// </summary>
        /// <param name="entityType">The type of the entity (e.g., Game)</param>
        /// <param name="propertyName">The name of the property (e.g., "artworks")</param>
        /// <returns>The Type to use for deserializing the property, or null if no mapping exists</returns>
        private Type? GetPropertyMetadataType(Type entityType, string propertyName)
        {
            // Dictionary mapping entity types to their property mappings
            var typeMap = new Dictionary<Type, Dictionary<string, Type>>
            {
                {
                    typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game),
                    new Dictionary<string, Type>
                    {
                        { "age_ratings", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.AgeRating) },
                        { "alternative_names", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.AlternativeName) },
                        { "artworks", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Artwork) },
                        { "cover", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Cover) },
                        { "expanded_games", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game) },
                        { "external_games", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.ExternalGame) },
                        { "franchise", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Franchise) },
                        { "franchies", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Franchise) },
                        { "game_localizations", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.GameLocalization) },
                        { "game_modes", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.GameMode) },
                        { "genres", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Genre) },
                        { "involved_companies", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.InvolvedCompany) },
                        { "multiplayer_modes", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.MultiplayerMode) },
                        { "platforms", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Platform) },
                        { "player_perspectives", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.PlayerPerspective) },
                        { "ports", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game) },
                        { "release_dates", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.ReleaseDate) },
                        { "remasters", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game) },
                        { "screenshots", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Screenshot) },
                        { "similar_games", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game) },
                        { "themes", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Theme) },
                        { "videos", typeof(gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.GameVideo) }
                    }
                }
                // Add additional entity type mappings here
            };

            // Try to get the property map for the entity type
            if (typeMap.TryGetValue(entityType, out Dictionary<string, Type>? propertyMap))
            {
                // Try to get the mapped type for the property
                if (propertyMap.TryGetValue(propertyName, out Type? mappedType))
                {
                    return mappedType;
                }
            }

            return null;
        }

        /// <summary>
        /// Invokes the Storage.StoreCacheValue generic method using a runtime type.
        /// </summary>
        /// <param name="modelType">The runtime type to use for the generic parameter.</param>
        /// <param name="objectToCache">The object instance to cache.</param>
        private async Task StoreCacheValueWithRuntimeType(Type modelType, object objectToCache)
        {
            if (Storage == null)
            {
                return;
            }

            MethodInfo? method = typeof(gaseous_server.Classes.Plugins.MetadataProviders.Storage)
                .GetMethod("StoreCacheValue", BindingFlags.Public | BindingFlags.Instance);

            if (method == null || !method.IsGenericMethodDefinition)
            {
                return;
            }

            MethodInfo generic = method.MakeGenericMethod(modelType);
            var task = generic.Invoke(Storage, new object[] { objectToCache }) as Task;
            if (task != null)
            {
                await task.ConfigureAwait(false);
            }
        }
    }
}