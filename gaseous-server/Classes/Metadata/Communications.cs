using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using Humanizer;
using IGDB;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RestEase;

namespace gaseous_server.Classes.Metadata
{
    /// <summary>
    /// Handles all metadata API communications
    /// </summary>
    public class Communications
    {
        static Communications()
        {
            var handler = new HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "deflate");
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        private static HttpClient client = new HttpClient();

        /// <summary>
        /// Configure metadata API communications
        /// </summary>
        public static HasheousClient.Models.MetadataModel.MetadataSources MetadataSource
        {
            get
            {
                return _MetadataSource;
            }
            set
            {
                _MetadataSource = value;

                switch (value)
                {
                    case HasheousClient.Models.MetadataModel.MetadataSources.IGDB:
                        // set rate limiter avoidance values
                        RateLimitAvoidanceWait = 1500;
                        RateLimitAvoidanceThreshold = 3;
                        RateLimitAvoidancePeriod = 1;

                        // set rate limiter recovery values
                        RateLimitRecoveryWaitTime = 10000;

                        break;
                    default:
                        // leave all values at default
                        break;
                }
            }
        }
        private static HasheousClient.Models.MetadataModel.MetadataSources _MetadataSource = HasheousClient.Models.MetadataModel.MetadataSources.None;

        // rate limit avoidance - what can we do to ensure that rate limiting is avoided?
        // these values affect all communications

        /// <summary>
        /// How long to wait to avoid hitting an API rate limiter
        /// </summary>
        private static int RateLimitAvoidanceWait = 2000;

        /// <summary>
        /// How many API calls in the period are allowed before we start introducing a wait
        /// </summary>
        private static int RateLimitAvoidanceThreshold = 80;

        /// <summary>
        /// A counter of API calls since the beginning of the period
        /// </summary>
        private static int RateLimitAvoidanceCallCount = 0;

        /// <summary>
        /// How large the period (in seconds) to measure API call counts against
        /// </summary>
        private static int RateLimitAvoidancePeriod = 60;

        /// <summary>
        /// The start of the rate limit avoidance period
        /// </summary>
        private static DateTime RateLimitAvoidanceStartTime = DateTime.UtcNow;

        /// <summary>
        /// Used to determine if we're already in rate limit avoidance mode - always query "InRateLimitAvoidanceMode"
        /// for up to date mode status.
        /// This bool is used to track status changes and should not be relied upon for current status.
        /// </summary>
        private static bool InRateLimitAvoidanceModeStatus = false;

        /// <summary>
        /// Determine if we're in rate limit avoidance mode.
        /// </summary>
        private static bool InRateLimitAvoidanceMode
        {
            get
            {
                if (RateLimitAvoidanceStartTime.AddSeconds(RateLimitAvoidancePeriod) <= DateTime.UtcNow)
                {
                    // avoidance period has expired - reset
                    RateLimitAvoidanceCallCount = 0;
                    RateLimitAvoidanceStartTime = DateTime.UtcNow;

                    return false;
                }
                else
                {
                    // we're in the avoidance period
                    if (RateLimitAvoidanceCallCount > RateLimitAvoidanceThreshold)
                    {
                        // the number of call counts indicates we should throttle things a bit
                        if (InRateLimitAvoidanceModeStatus == false)
                        {
                            Logging.Log(Logging.LogType.Information, "API Connection", "Entered rate limit avoidance period, API calls will be throttled by " + RateLimitAvoidanceWait + " milliseconds.");
                            InRateLimitAvoidanceModeStatus = true;
                        }
                        return true;
                    }
                    else
                    {
                        // still in full speed mode - no throttle required
                        if (InRateLimitAvoidanceModeStatus == true)
                        {
                            Logging.Log(Logging.LogType.Information, "API Connection", "Exited rate limit avoidance period, API call rate is returned to full speed.");
                            InRateLimitAvoidanceModeStatus = false;
                        }
                        return false;
                    }
                }
            }
        }

        // rate limit handling - how long to wait to allow the server to recover and try again
        // these values affect ALL communications if a 429 response code is received

        /// <summary>
        /// How long to wait (in milliseconds) if a 429 status code is received before trying again
        /// </summary>
        private static int RateLimitRecoveryWaitTime = 10000;

        /// <summary>
        /// The time when normal communications can attempt to be resumed
        /// </summary>
        private static DateTime RateLimitResumeTime = DateTime.UtcNow.AddMinutes(5 * -1);

        // rate limit retry - how many times to retry before aborting
        private int RetryAttempts = 0;
        private int RetryAttemptsMax = 3;

        /// <summary>
        /// Request data from the metadata API
        /// </summary>
        /// <typeparam name="T">Type of object to return</typeparam>
        /// <param name="Endpoint">API endpoint segment to use</param>
        /// <param name="Fields">Fields to request from the API</param>
        /// <param name="Query">Selection criteria for data to request</param>
        /// <returns></returns>
        public async Task<T[]?> APIComm<T>(string Endpoint, string Fields, string Query)
        {
            switch (_MetadataSource)
            {
                case HasheousClient.Models.MetadataModel.MetadataSources.None:
                    return null;
                case HasheousClient.Models.MetadataModel.MetadataSources.IGDB:
                    return await IGDBAPI<T>(Endpoint, Fields, Query);
                default:
                    return null;
            }
        }

        private async Task<T[]> IGDBAPI<T>(string Endpoint, string Fields, string Query)
        {
            Logging.Log(Logging.LogType.Debug, "API Connection", "Accessing API for endpoint: " + Endpoint);

            if (RateLimitResumeTime > DateTime.UtcNow)
            {
                Logging.Log(Logging.LogType.Information, "API Connection", "IGDB rate limit hit. Pausing API communications until " + RateLimitResumeTime.ToString() + ". Attempt " + RetryAttempts + " of " + RetryAttemptsMax + " retries.");
                Thread.Sleep(RateLimitRecoveryWaitTime);
            }

            try
            {   
                if (InRateLimitAvoidanceMode == true)
                {
                    // sleep for a moment to help avoid hitting the rate limiter
                    Thread.Sleep(RateLimitAvoidanceWait);
                }

                // perform the actual API call
                var results = await igdb.QueryAsync<T>(Endpoint, query: Fields + " " + Query + ";");

                // increment rate limiter avoidance call count
                RateLimitAvoidanceCallCount += 1;
                
                return results;
            }
            catch (ApiException apiEx)
            {
                switch (apiEx.StatusCode)
                {
                    case HttpStatusCode.TooManyRequests:
                        if (RetryAttempts >= RetryAttemptsMax)
                        {
                            Logging.Log(Logging.LogType.Warning, "API Connection", "IGDB rate limiter attempts expired. Aborting.", apiEx);
                            throw;
                        }
                        else
                        {
                            Logging.Log(Logging.LogType.Information, "API Connection", "IGDB API rate limit hit while accessing endpoint " + Endpoint, apiEx);
                            
                            RetryAttempts += 1;

                            return await IGDBAPI<T>(Endpoint, Fields, Query);
                        }
                    
                    case HttpStatusCode.Unauthorized:
                        Logging.Log(Logging.LogType.Information, "API Connection", "IGDB API unauthorised error while accessing endpoint " + Endpoint + ". Waiting " + RateLimitAvoidanceWait + " milliseconds and resetting IGDB client.", apiEx);
                        
                        Thread.Sleep(RateLimitAvoidanceWait);

                        igdb = new IGDBClient(
                            // Found in Twitch Developer portal for your app
                            Config.IGDB.ClientId,
                            Config.IGDB.Secret
                        );

                        RetryAttempts += 1;

                        return await IGDBAPI<T>(Endpoint, Fields, Query);

                    default:
                        Logging.Log(Logging.LogType.Warning, "API Connection", "Exception when accessing endpoint " + Endpoint, apiEx);
                        throw;
                }
            }
            catch(Exception ex)
            {
                Logging.Log(Logging.LogType.Warning, "API Connection", "Exception when accessing endpoint " + Endpoint, ex);
                throw;
            }
        }

        /// <summary>
        /// Download from the specified uri
        /// </summary>
        /// <param name="uri">The uri to download from</param>
        /// <param name="DestinationFile">The file name and path the download should be stored as</param>
        public Task<bool?> DownloadFile(Uri uri, string DestinationFile)
        {
            var result = _DownloadFile(uri, DestinationFile);
            
            return result;
        }

        private async Task<bool?> _DownloadFile(Uri uri, string DestinationFile)
        {
            string DestinationDirectory = new FileInfo(DestinationFile).Directory.FullName;
            if (!Directory.Exists(DestinationDirectory))
            {
                Directory.CreateDirectory(DestinationDirectory);
            }

            Logging.Log(Logging.LogType.Information, "Communications", "Downloading from " + uri.ToString() + " to " + DestinationFile);

            try
            {
                using (HttpResponseMessage response = client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).Result)
                {
                    response.EnsureSuccessStatusCode();

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(DestinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var totalRead = 0L;
                        var totalReads = 0L;
                        var buffer = new byte[8192];
                        var isMoreToRead = true;

                        do
                        {
                            var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                isMoreToRead = false;
                            }
                            else
                            {
                                await fileStream.WriteAsync(buffer, 0, read);

                                totalRead += read;
                                totalReads += 1;

                                if (totalReads % 2000 == 0)
                                {
                                    Console.WriteLine(string.Format("total bytes downloaded so far: {0:n0}", totalRead));
                                }
                            }
                        }
                        while (isMoreToRead);
                    }
                }

                return true;
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    if (File.Exists(DestinationFile))
                    {
                        FileInfo fi = new FileInfo(DestinationFile);
                        if (fi.Length == 0)
                        {
                            File.Delete(DestinationFile);
                        }
                    }
                }

                Logging.Log(Logging.LogType.Warning, "Download Images", "Error downloading file: ", ex);
            }

            return false;
        }

        public async Task<string> GetSpecificImageFromServer(string ImagePath, string ImageId, IGDBAPI_ImageSize size, List<IGDBAPI_ImageSize>? FallbackSizes = null)
        {
            string returnPath = "";

            // check for artificial sizes first
            switch (size)
            {
                case IGDBAPI_ImageSize.screenshot_small:
                case IGDBAPI_ImageSize.screenshot_thumb:
                    string BasePath = Path.Combine(ImagePath, size.ToString());
                    if (!Directory.Exists(BasePath))
                    {
                        Directory.CreateDirectory(BasePath);
                    }
                    returnPath = Path.Combine(BasePath, ImageId + ".jpg");
                    if (!File.Exists(returnPath))
                    {
                        // get original size image and resize
                        string originalSizePath = await GetSpecificImageFromServer(ImagePath, ImageId, IGDBAPI_ImageSize.original, null);

                        int width = 0;
                        int height = 0;
                        
                        switch (size)
                        {
                            case IGDBAPI_ImageSize.screenshot_small:
                                // 235x128
                                width = 235;
                                height = 128;
                                break;
                            
                            case IGDBAPI_ImageSize.screenshot_thumb:
                                // 165x90
                                width = 165;
                                height = 90;
                                break;

                        }

                        using (var image = new ImageMagick.MagickImage(originalSizePath))
                        {
                            image.Resize(width, height);
                            image.Strip();
                            image.Write(returnPath);
                        }
                    }

                    break;

                default:
                    // these sizes are IGDB native
                    if (RateLimitResumeTime > DateTime.UtcNow)
                    {
                        Logging.Log(Logging.LogType.Information, "API Connection", "IGDB rate limit hit. Pausing API communications until " + RateLimitResumeTime.ToString() + ". Attempt " + RetryAttempts + " of " + RetryAttemptsMax + " retries.");
                        Thread.Sleep(RateLimitRecoveryWaitTime);
                    }

                    if (InRateLimitAvoidanceMode == true)
                    {
                        // sleep for a moment to help avoid hitting the rate limiter
                        Thread.Sleep(RateLimitAvoidanceWait);
                    }

                    Communications comms = new Communications();
                    List<IGDBAPI_ImageSize> imageSizes = new List<IGDBAPI_ImageSize>
                    {
                        size
                    };

                    // get the image
                    try
                    {
                        returnPath = Path.Combine(ImagePath, size.ToString(), ImageId + ".jpg");

                        // fail early if the file is already downloaded
                        if (!File.Exists(returnPath))
                        {
                            await comms.IGDBAPI_GetImage(imageSizes, ImageId, ImagePath);
                        }
                        
                    }
                    catch (HttpRequestException ex)
                    {
                        if (ex.StatusCode == HttpStatusCode.NotFound)
                        {
                            Logging.Log(Logging.LogType.Information, "Image Download", "Image not found, trying a different size.");

                            if (FallbackSizes != null)
                            {
                                foreach (Communications.IGDBAPI_ImageSize imageSize in FallbackSizes)
                                {
                                    returnPath = await GetSpecificImageFromServer(ImagePath, ImageId, imageSize, null);
                                }
                            }
                        }
                    }

                    // increment rate limiter avoidance call count
                    RateLimitAvoidanceCallCount += 1;

                    break;
            }

            return returnPath;
        }

        public static T? GetSearchCache<T>(string SearchFields, string SearchString)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM SearchCache WHERE SearchFields = @searchfields AND SearchString = @searchstring;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "searchfields", SearchFields },
                { "searchstring", SearchString }
            };
            DataTable data = db.ExecuteCMD(sql, dbDict);
            if (data.Rows.Count > 0)
            {
                // cache hit
                string rawString = data.Rows[0]["Content"].ToString();
                T ReturnValue = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(rawString);
                if (ReturnValue != null)
                {
                    Logging.Log(Logging.LogType.Information, "Search Cache", "Found search result in cache. Search string: " + SearchString);
                    return ReturnValue;
                }
                else
                {
                    Logging.Log(Logging.LogType.Information, "Search Cache", "Search result not found in cache.");
                    return default;
                }
            }
            else
            {
                // cache miss
                Logging.Log(Logging.LogType.Information, "Search Cache", "Search result not found in cache.");
                return default;
            }
        }

        public static void SetSearchCache<T>(string SearchFields, string SearchString, T SearchResult)
        {
            Logging.Log(Logging.LogType.Information, "Search Cache", "Storing search results in cache. Search string: " + SearchString);

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "INSERT INTO SearchCache (SearchFields, SearchString, Content, LastSearch) VALUES (@searchfields, @searchstring, @content, @lastsearch);";
            Dictionary<string, object> dbDict = new Dictionary<string, object>
            {
                { "searchfields", SearchFields },
                { "searchstring", SearchString },
                { "content", Newtonsoft.Json.JsonConvert.SerializeObject(SearchResult) },
                { "lastsearch", DateTime.UtcNow }
            };
            db.ExecuteNonQuery(sql, dbDict);
        }

        /// <summary>
        /// See https://api-docs.igdb.com/?javascript#images for more information about the image url structure
        /// </summary>
        /// <param name="ImageId"></param>
        /// <param name="outputPath">The path to save the downloaded files to
        public async Task IGDBAPI_GetImage(List<IGDBAPI_ImageSize> ImageSizes, string ImageId, string OutputPath)
        {
            string urlTemplate = "https://images.igdb.com/igdb/image/upload/t_{size}/{hash}.jpg";
            
            foreach (IGDBAPI_ImageSize ImageSize in ImageSizes)
            {
                string url = urlTemplate.Replace("{size}", Common.GetDescription(ImageSize)).Replace("{hash}", ImageId);
                string newOutputPath = Path.Combine(OutputPath, Common.GetDescription(ImageSize));
                string OutputFile = ImageId + ".jpg";
                string fullPath = Path.Combine(newOutputPath, OutputFile);
                
                await _DownloadFile(new Uri(url), fullPath);
            }
        }

        public enum IGDBAPI_ImageSize
        {
            /// <summary>
            /// 90x128 Fit
            /// </summary>
            [Description("cover_small")]
            cover_small,

            /// <summary>
            /// 264x374 Fit
            /// </summary>
            [Description("cover_big")]
            cover_big,

            /// <summary>
            /// 165x90 Lfill, Centre gravity - resized by Gaseous and is not a real IGDB size
            /// </summary>
            [Description("screenshot_thumb")]
            screenshot_thumb,

            /// <summary>
            /// 235x128 Lfill, Centre gravity - resized by Gaseous and is not a real IGDB size
            /// </summary>
            [Description("screenshot_small")]
            screenshot_small,

            /// <summary>
            /// 589x320 Lfill, Centre gravity
            /// </summary>
            [Description("screenshot_med")]
            screenshot_med,

            /// <summary>
            /// 889x500 Lfill, Centre gravity
            /// </summary>
            [Description("screenshot_big")]
            screenshot_big,

            /// <summary>
            /// 1280x720 Lfill, Centre gravity
            /// </summary>
            [Description("screenshot_huge")]
            screenshot_huge,

            /// <summary>
            /// 284x160 Fit
            /// </summary>
            [Description("logo_med")]
            logo_med,

            /// <summary>
            /// 90x90 Thumb, Centre gravity
            /// </summary>
            [Description("thumb")]
            thumb,

            /// <summary>
            /// 35x35 Thumb, Centre gravity
            /// </summary>
            [Description("micro")]
            micro,

            /// <summary>
            /// 1280x720 Fit, Centre gravity
            /// </summary>
            [Description("720p")]
            r720p,

            /// <summary>
            /// 1920x1080 Fit, Centre gravity
            /// </summary>
            [Description("1080p")]
            r1080p,

            /// <summary>
            /// The originally uploaded image
            /// </summary>
            [Description("original")]
            original
        }
    }
}