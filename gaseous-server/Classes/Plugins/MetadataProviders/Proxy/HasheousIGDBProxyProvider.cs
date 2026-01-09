
using SharpCompress.Archives;
using SharpCompress.Common;

namespace gaseous_server.Classes.Plugins.MetadataProviders
{
    public class HasheousIGDBProxyProvider : IProxyProvider
    {
        /// <inheritdoc/>
        public string Name => throw new NotImplementedException();

        /// <inheritdoc/>
        public FileSignature.MetadataSources SourceType => FileSignature.MetadataSources.IGDB;

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

        public Task<T?> GetEntityAsync<T>(string itemType, long id) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<T[]?> SearchEntitiesAsync<T>(string itemType, string query) where T : class
        {
            throw new NotImplementedException();
        }

        private async Task<T?> GetGameBundleAsync<T>(long id) where T : class
        {
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
                    Dictionary<string, object>? game = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(gameJsonContent);
                    if (game != null)
                    {
                        // check the updated_at field - if older than 7 days, redownload
                        if (game.ContainsKey("updated_at"))
                        {
                            if (!DateTime.TryParse(game["updated_at"].ToString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime updatedAt))
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
                string downloadDirectory = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, Path.GetTempPath());
                string downloadedBundlePath = Path.Combine(downloadDirectory, $"{id}.bundle.zip");
                if (Directory.Exists(downloadDirectory) == true)
                {
                    Directory.Delete(downloadDirectory, true);
                }
                Directory.CreateDirectory(downloadDirectory);
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
                        // ignore
                    }
                }
                else
                {
                    // failed to download bundle
                    return null;
                }
            }

            // bundle show now be present for loading

            throw new NotImplementedException();
        }
    }
}