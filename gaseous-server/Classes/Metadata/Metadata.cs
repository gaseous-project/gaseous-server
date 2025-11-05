using System.Data;
using System.Data.SqlTypes;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace gaseous_server.Classes.Metadata
{
    public class Metadata
    {
        #region Exception Handling
        public class InvalidMetadataId : Exception
        {
            public InvalidMetadataId(long Id) : base("Invalid Metadata id: " + Id + " from source: " + FileSignature.MetadataSources.IGDB + " (default)")
            {
            }

            public InvalidMetadataId(FileSignature.MetadataSources SourceType, long Id) : base("Invalid Metadata id: " + Id + " from source: " + SourceType)
            {
            }

            public InvalidMetadataId(string Id) : base("Invalid Metadata id: " + Id + " from source: " + FileSignature.MetadataSources.IGDB + " (default)")
            {
            }

            public InvalidMetadataId(FileSignature.MetadataSources SourceType, string Id) : base("Invalid Metadata id: " + Id + " from source: " + SourceType)
            {
            }
        }
        #endregion

        #region Get Metadata
        /// <summary>
        /// Get metadata from the default source
        /// </summary>
        /// <typeparam name="T">
        /// The type of metadata to get
        /// </typeparam>
        /// <param name="Id">
        /// The id of the metadata to get
        /// </param>
        /// <returns>
        /// The metadata object
        /// </returns>
        /// <exception cref="InvalidMetadataId">
        /// Thrown when the id is invalid
        /// </exception>
        public static T? GetMetadata<T>(long Id, Boolean ForceRefresh = false) where T : class
        {
            if (Id < 0)
            {
                throw new InvalidMetadataId(Id);
            }

            return _GetMetadataAsync<T>(FileSignature.MetadataSources.IGDB, Id, ForceRefresh).Result;
        }

        /// <summary>
        /// Get metadata from the specified source
        /// </summary>
        /// <typeparam name="T">
        /// The type of metadata to get
        /// </typeparam>
        /// <param name="SourceType">
        /// The source of the metadata
        /// </param>
        /// <param name="Id">
        /// The id of the metadata to get
        /// </param>
        /// <returns>
        /// The metadata object
        /// </returns>
        /// <exception cref="InvalidMetadataId">
        /// Thrown when the id is invalid
        /// </exception>
        public static T? GetMetadata<T>(FileSignature.MetadataSources SourceType, long Id, Boolean ForceRefresh = false) where T : class
        {
            if (Id < 0)
            {
                throw new InvalidMetadataId(SourceType, Id);
            }

            return _GetMetadataAsync<T>(SourceType, Id, ForceRefresh).Result;
        }

        public static T? GetMetadata<T>(FileSignature.MetadataSources SourceType, string Slug, Boolean ForceRefresh = false) where T : class
        {
            return _GetMetadataAsync<T>(SourceType, Slug, ForceRefresh).Result;
        }

        /// <summary>
        /// Get metadata from the default source
        /// </summary>
        /// <typeparam name="T">
        /// The type of metadata to get
        /// </typeparam>
        /// <param name="Id">
        /// The id of the metadata to get
        /// </param>
        /// <returns>
        /// The metadata object
        /// </returns>
        /// <exception cref="InvalidMetadataId">
        /// Thrown when the id is invalid
        /// </exception>
        public static async Task<T?> GetMetadataAsync<T>(long Id, Boolean ForceRefresh = false) where T : class
        {
            if (Id < 0)
            {
                throw new InvalidMetadataId(Id);
            }

            return await _GetMetadataAsync<T>(FileSignature.MetadataSources.IGDB, Id, ForceRefresh);
        }

        /// <summary>
        /// Get metadata from the specified source
        /// </summary>
        /// <typeparam name="T">
        /// The type of metadata to get
        /// </typeparam>
        /// <param name="SourceType">
        /// The source of the metadata
        /// </param>
        /// <param name="Id">
        /// The id of the metadata to get
        /// </param>
        /// <returns>
        /// The metadata object
        /// </returns>
        /// <exception cref="InvalidMetadataId">
        /// Thrown when the id is invalid
        /// </exception>
        public static async Task<T?> GetMetadataAsync<T>(FileSignature.MetadataSources SourceType, long Id, Boolean ForceRefresh = false) where T : class
        {
            if (Id < 0)
            {
                throw new InvalidMetadataId(SourceType, Id);
            }

            return await _GetMetadataAsync<T>(SourceType, Id, ForceRefresh);
        }

        public static async Task<T?> GetMetadataAsync<T>(FileSignature.MetadataSources SourceType, string Slug, Boolean ForceRefresh = false) where T : class
        {
            return await _GetMetadataAsync<T>(SourceType, Slug, ForceRefresh);
        }

        private static async Task<T?> _GetMetadataAsync<T>(FileSignature.MetadataSources SourceType, object Id, Boolean ForceRefresh) where T : class
        {
            // get T type as string
            string type = typeof(T).Name;

            // get type of Id as string
            IdType idType = Id.GetType() == typeof(long) ? IdType.Long : IdType.String;

            // check cached metadata status
            // if metadata is not cached or expired, get it from the source. Otherwise, return the cached metadata
            Storage.CacheStatus? cacheStatus = Storage.CacheStatus.NotPresent;
            if (idType == IdType.Long)
            {
                cacheStatus = await Storage.GetCacheStatusAsync(SourceType, type, (long)Id);
            }
            else
            {
                cacheStatus = await Storage.GetCacheStatusAsync(SourceType, type, (string)Id);
            }

            // if ForceRefresh is true, set cache status to expired if it is current
            if (ForceRefresh == true)
            {
                if (cacheStatus == Storage.CacheStatus.Current)
                {
                    cacheStatus = Storage.CacheStatus.Expired;
                }
            }

            // if the source is "none", cache status should be "current" or "not present"
            if (SourceType == FileSignature.MetadataSources.None)
            {
                if (cacheStatus == Storage.CacheStatus.Expired)
                {
                    cacheStatus = Storage.CacheStatus.Current;
                }
            }

            T? metadata = (T)Activator.CreateInstance(typeof(T));

            switch (cacheStatus)
            {
                case Storage.CacheStatus.Current:
                    if (idType == IdType.Long)
                    {
                        metadata = await Storage.GetCacheValue<T>(SourceType, metadata, "Id", (long)Id);
                    }
                    else
                    {
                        metadata = await Storage.GetCacheValue<T>(SourceType, metadata, "Slug", (string)Id);
                    }
                    break;

                case Storage.CacheStatus.Expired:
                    try
                    {
                        if (idType == IdType.Long)
                        {
                            metadata = await GetMetadataFromServer<T>(SourceType, (long)Id);
                        }
                        else
                        {
                            metadata = await GetMetadataFromServer<T>(SourceType, (string)Id);
                        }
                        await Storage.NewCacheValue<T>(SourceType, metadata, true);
                    }
                    catch (Exception e)
                    {
                        Logging.LogKey(Logging.LogType.Information, "process.fetch_metadata", "fetchmetadata.failed_fetch", null, new string[] { SourceType.ToString(), Id.ToString(), type, e.Message });
                        metadata = null;
                    }
                    break;

                case Storage.CacheStatus.NotPresent:
                    try
                    {
                        if (idType == IdType.Long)
                        {
                            metadata = await GetMetadataFromServer<T>(SourceType, (long)Id);
                        }
                        else
                        {
                            metadata = await GetMetadataFromServer<T>(SourceType, (string)Id);
                        }
                        await Storage.NewCacheValue<T>(SourceType, metadata, false);
                    }
                    catch (Exception e)
                    {
                        Logging.LogKey(Logging.LogType.Information, "process.fetch_metadata", "fetchmetadata.failed_fetch", null, new string[] { SourceType.ToString(), Id.ToString(), type, e.Message });
                        metadata = null;
                    }
                    break;
            }

            return metadata;
        }

        private enum IdType
        {
            Long,
            String
        }

        private static async Task<T> GetMetadataFromServer<T>(FileSignature.MetadataSources SourceType, long Id) where T : class
        {
            // get T type as string
            string type = typeof(T).Name;

            // get metadata from the server
            Communications comms = new Communications();

            switch (SourceType)
            {
                case FileSignature.MetadataSources.None:
                    // generate a dummy object
                    var returnObject = (T)Activator.CreateInstance(typeof(T));
                    returnObject.GetType().GetProperty("Id").SetValue(returnObject, Id);

                    // if returnObject has a property called "name", query the metadatamap view for the name
                    if (returnObject.GetType().GetProperty("Name") != null)
                    {
                        Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                        string sql = "SELECT * FROM MetadataMap JOIN MetadataMapBridge ON MetadataMap.Id = MetadataMapBridge.ParentMapId WHERE MetadataSourceId = @id AND MetadataSourceType = 0;";
                        DataTable dataTable = await db.ExecuteCMDAsync(sql, new Dictionary<string, object>
                    {
                        { "@id", Id }
                    });
                        if (dataTable.Rows.Count > 0)
                        {
                            returnObject.GetType().GetProperty("Name").SetValue(returnObject, dataTable.Rows[0]["SignatureGameName"].ToString());
                        }
                    }

                    return returnObject;

                case FileSignature.MetadataSources.TheGamesDb:
                    // TheGamesDb metadata is a bit more complex, so we need to handle it differently
                    // check if the genre metadata is present in the local cache
                    // if not, we need to fetch it from the server and cache it
                    // if the metadata is not present, we need to fetch it from the server and cache it
                    // if the metadata is expired, we need to fetch it from the server and cache it
                    // if the metadata is current, we can do nothing
                    // check if local cache has the metadata
                    string tgdbFile_Genre = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_TheGamesDB(), "Genres.json");
                    bool forceRefresh = false;
                    if (!File.Exists(tgdbFile_Genre))
                    {
                        forceRefresh = true;
                    }
                    else
                    {
                        FileInfo fileInfo = new FileInfo(tgdbFile_Genre);
                        // check if the file is older than 30 day
                        if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-30))
                        {
                            forceRefresh = true;
                        }
                        else
                        {
                            // check if the file is empty
                            if (fileInfo.Length == 0)
                            {
                                forceRefresh = true;
                            }
                        }
                    }
                    // check if the file is expired
                    Storage.CacheStatus genreStatus = await Storage.GetCacheStatusAsync(FileSignature.MetadataSources.TheGamesDb, "Genre", 1);
                    if (genreStatus == Storage.CacheStatus.Expired || genreStatus == Storage.CacheStatus.NotPresent || forceRefresh)
                    {
                        forceRefresh = true;
                    }

                    // if forceRefresh is true, we need to fetch the genres from the server
                    if (forceRefresh)
                    {
                        // connect to hasheous api and get TheGamesDb metadata, convert it to IGDB metadata, and cache it
                        var theGamesDbGenres = await comms.APIComm<HasheousClient.Models.Metadata.TheGamesDb.Genres>(SourceType, Communications.MetadataEndpoint.Genre, 1);
                        if (theGamesDbGenres != null)
                        {
                            HasheousClient.Models.Metadata.TheGamesDb.Genres genres = theGamesDbGenres[0];

                            foreach (string genre in genres.data.genres.Keys)
                            {
                                Storage.CacheStatus genreCacheStatus = await Storage.GetCacheStatusAsync(FileSignature.MetadataSources.TheGamesDb, "Genre", genres.data.genres[genre].id);
                                switch (genreCacheStatus)
                                {
                                    case Storage.CacheStatus.NotPresent:
                                        await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, new HasheousClient.Models.Metadata.IGDB.Genre
                                        {
                                            Id = genres.data.genres[genre].id,
                                            Name = genres.data.genres[genre].name
                                        }, false);
                                        break;

                                    case Storage.CacheStatus.Expired:
                                        await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, new HasheousClient.Models.Metadata.IGDB.Genre
                                        {
                                            Id = genres.data.genres[genre].id,
                                            Name = genres.data.genres[genre].name
                                        }, true);
                                        break;
                                }
                            }
                        }

                        // save the genres to a file
                        await File.WriteAllTextAsync(tgdbFile_Genre, Newtonsoft.Json.JsonConvert.SerializeObject(theGamesDbGenres, new JsonSerializerSettings
                        {
                            Formatting = Newtonsoft.Json.Formatting.Indented,
                            NullValueHandling = NullValueHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Ignore,
                            MaxDepth = 25
                        }));
                    }

                    switch (type)
                    {
                        case "Game":
                            // fetch the requested metadata from the server
                            string tgdbDirectory = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_TheGamesDB(), "Game", Id.ToString());
                            if (!Directory.Exists(tgdbDirectory))
                            {
                                Directory.CreateDirectory(tgdbDirectory);
                            }
                            string tgdbFile_Game = Path.Combine(tgdbDirectory, "Game.json");
                            string tgdbFile_AgeRating = Path.Combine(tgdbDirectory, "AgeRating.json");
                            string tgdbFile_Cover = Path.Combine(tgdbDirectory, "Cover.json");
                            string tgdbFile_Video = Path.Combine(tgdbDirectory, "Video.json");
                            string tgdbFile_Artwork = Path.Combine(tgdbDirectory, "Artwork.json");
                            string tgdbFile_ClearLogo = Path.Combine(tgdbDirectory, "ClearLogo.json");
                            string tgdbFile_Screenshot = Path.Combine(tgdbDirectory, "Screenshot.json");
                            if (!File.Exists(tgdbFile_Game))
                            {
                                forceRefresh = true;
                            }
                            else
                            {
                                FileInfo fileInfo = new FileInfo(tgdbFile_Game);
                                // check if the file is older than 30 day
                                if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-30))
                                {
                                    forceRefresh = true;
                                }
                                else
                                {
                                    // check if the file is empty
                                    if (fileInfo.Length == 0)
                                    {
                                        forceRefresh = true;
                                    }
                                }
                            }
                            // check if the file is expired
                            Storage.CacheStatus gameStatus = await Storage.GetCacheStatusAsync(FileSignature.MetadataSources.TheGamesDb, "Game", Id);
                            if (gameStatus == Storage.CacheStatus.Expired || gameStatus == Storage.CacheStatus.NotPresent || forceRefresh)
                            {
                                forceRefresh = true;
                            }

                            // create a new IGDB game objects
                            HasheousClient.Models.Metadata.TheGamesDb.Game? theGamesDbGame = null;
                            HasheousClient.Models.Metadata.IGDB.AgeRating? igdbAgeRating = null;
                            HasheousClient.Models.Metadata.IGDB.Cover? igdbCover = null;
                            HasheousClient.Models.Metadata.IGDB.GameVideo? igdbVideo = null;
                            List<HasheousClient.Models.Metadata.IGDB.Artwork> igdbArtwork = new List<HasheousClient.Models.Metadata.IGDB.Artwork>();
                            List<HasheousClient.Models.Metadata.IGDB.ClearLogo> igdbClearLogo = new List<HasheousClient.Models.Metadata.IGDB.ClearLogo>();
                            List<HasheousClient.Models.Metadata.IGDB.Screenshot> igdbScreenshot = new List<HasheousClient.Models.Metadata.IGDB.Screenshot>();

                            // if forceRefresh is true, we need to fetch the game from the server
                            if (forceRefresh)
                            {
                                // connect to hasheous api and get TheGamesDb metadata and convert it to IGDB metadata
                                var theGamesDbGameResult = await comms.APIComm<HasheousClient.Models.Metadata.TheGamesDb.GamesByGameID>(SourceType, Communications.MetadataEndpoint.Game, Id);

                                // create a new IGDB game object
                                theGamesDbGame = theGamesDbGameResult[0].data.games[0];
                                await File.WriteAllTextAsync(tgdbFile_Game, Newtonsoft.Json.JsonConvert.SerializeObject(theGamesDbGame, new JsonSerializerSettings
                                {
                                    Formatting = Newtonsoft.Json.Formatting.Indented,
                                    NullValueHandling = NullValueHandling.Ignore,
                                    DefaultValueHandling = DefaultValueHandling.Ignore,
                                    MaxDepth = 25
                                }));

                                // generate age rating object
                                long? igdbAgeRatingTitle = null;
                                if (File.Exists(tgdbFile_AgeRating))
                                {
                                    File.Delete(tgdbFile_AgeRating);
                                }
                                if (theGamesDbGame.rating != null && theGamesDbGame.rating != "")
                                {
                                    // search the age group map for the rating id
                                    string tgdbRatingName = theGamesDbGame.rating.Split(" - ")[0];
                                    if (AgeGroups.AgeGroupMap.RatingBoards["ESRB"].Ratings.ContainsKey(tgdbRatingName))
                                    {
                                        igdbAgeRatingTitle = AgeGroups.AgeGroupMap.RatingBoards["ESRB"].Ratings[tgdbRatingName].IGDBId;
                                    }

                                    if (igdbAgeRatingTitle != null)
                                    {
                                        igdbAgeRating = new HasheousClient.Models.Metadata.IGDB.AgeRating
                                        {
                                            Id = theGamesDbGame.id,
                                            Organization = 1, // IGDB Age Rating Organization ID for ESRB
                                            RatingCategory = (long)igdbAgeRatingTitle
                                        };

                                        // update cache
                                        Storage.CacheStatus ageRatingStatus = await Storage.GetCacheStatusAsync(FileSignature.MetadataSources.TheGamesDb, "AgeRating", (long)igdbAgeRating.Id);
                                        switch (ageRatingStatus)
                                        {
                                            case Storage.CacheStatus.NotPresent:
                                                await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, igdbAgeRating, false);
                                                break;

                                            case Storage.CacheStatus.Expired:
                                                await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, igdbAgeRating, true);
                                                break;
                                        }

                                        await File.WriteAllTextAsync(tgdbFile_AgeRating, Newtonsoft.Json.JsonConvert.SerializeObject(igdbAgeRating, new JsonSerializerSettings
                                        {
                                            Formatting = Newtonsoft.Json.Formatting.Indented,
                                            NullValueHandling = NullValueHandling.Ignore,
                                            DefaultValueHandling = DefaultValueHandling.Ignore,
                                            MaxDepth = 25
                                        }));
                                    }
                                }

                                // generate cover image object
                                if (File.Exists(tgdbFile_Cover))
                                {
                                    File.Delete(tgdbFile_Cover);
                                }
                                List<HasheousClient.Models.Metadata.TheGamesDb.GameImage>? imageDict = new List<HasheousClient.Models.Metadata.TheGamesDb.GameImage>();
                                if (
                                    theGamesDbGameResult[0].include != null &&
                                    theGamesDbGameResult[0].include.boxart != null &&
                                    theGamesDbGameResult[0].include.boxart.data != null &&
                                    theGamesDbGameResult[0].include.boxart.data.ContainsKey(theGamesDbGame.id.ToString()))
                                {
                                    imageDict = theGamesDbGameResult[0].include.boxart.data[theGamesDbGame.id.ToString()];
                                    foreach (HasheousClient.Models.Metadata.TheGamesDb.GameImage image in imageDict)
                                    {
                                        if (image.type == "boxart" && image.side == "front")
                                        {
                                            int width = 0;
                                            int height = 0;

                                            if (image.resolution == null || image.resolution == "")
                                            {
                                                image.resolution = "0x0";
                                            }

                                            width = int.TryParse(image.resolution.Split("x")[0].Trim(), out width) ? width : 0;
                                            height = int.TryParse(image.resolution.Split("x")[1].Trim(), out height) ? height : 0;

                                            igdbCover = new HasheousClient.Models.Metadata.IGDB.Cover
                                            {
                                                Id = image.id,
                                                ImageId = image.filename,
                                                Width = width,
                                                Height = height,
                                                Url = new Uri(theGamesDbGameResult[0].include.boxart.base_url.original + image.filename).ToString(),
                                                AlphaChannel = false,
                                                Animated = false,
                                                Game = theGamesDbGame.id
                                            };

                                            await File.WriteAllTextAsync(tgdbFile_Cover, Newtonsoft.Json.JsonConvert.SerializeObject(igdbCover, new JsonSerializerSettings
                                            {
                                                Formatting = Newtonsoft.Json.Formatting.Indented,
                                                NullValueHandling = NullValueHandling.Ignore,
                                                DefaultValueHandling = DefaultValueHandling.Ignore,
                                                MaxDepth = 25
                                            }));

                                            // update cache
                                            Storage.CacheStatus coverStatus = await Storage.GetCacheStatusAsync(FileSignature.MetadataSources.TheGamesDb, "Cover", (long)igdbCover.Id);
                                            switch (coverStatus)
                                            {
                                                case Storage.CacheStatus.NotPresent:
                                                    await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, igdbCover, false);
                                                    break;

                                                case Storage.CacheStatus.Expired:
                                                    await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, igdbCover, true);
                                                    break;
                                            }
                                            break;
                                        }
                                    }
                                }

                                // generate youtube video object
                                if (File.Exists(tgdbFile_Video))
                                {
                                    File.Delete(tgdbFile_Video);
                                }
                                if (theGamesDbGame.youtube != null && theGamesDbGame.youtube != "")
                                {
                                    igdbVideo = new HasheousClient.Models.Metadata.IGDB.GameVideo
                                    {
                                        Id = theGamesDbGame.id,
                                        Name = theGamesDbGame.game_title,
                                        VideoId = theGamesDbGame.youtube,
                                        Game = theGamesDbGame.id
                                    };

                                    await File.WriteAllTextAsync(tgdbFile_Video, Newtonsoft.Json.JsonConvert.SerializeObject(igdbVideo, new JsonSerializerSettings
                                    {
                                        Formatting = Newtonsoft.Json.Formatting.Indented,
                                        NullValueHandling = NullValueHandling.Ignore,
                                        DefaultValueHandling = DefaultValueHandling.Ignore,
                                        MaxDepth = 25
                                    }));

                                    // update cache
                                    Storage.CacheStatus videoStatus = await Storage.GetCacheStatusAsync(FileSignature.MetadataSources.TheGamesDb, "GameVideo", (long)igdbVideo.Id);
                                    switch (videoStatus)
                                    {
                                        case Storage.CacheStatus.NotPresent:
                                            await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, igdbVideo, false);
                                            break;

                                        case Storage.CacheStatus.Expired:
                                            await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, igdbVideo, true);
                                            break;
                                    }
                                }

                                // generate artwork object
                                if (File.Exists(tgdbFile_Artwork))
                                {
                                    File.Delete(tgdbFile_Artwork);
                                }
                                foreach (HasheousClient.Models.Metadata.TheGamesDb.GameImage image in imageDict)
                                {
                                    if (image.type == "fanart")
                                    {
                                        HasheousClient.Models.Metadata.IGDB.Artwork igdbArtworkItem = new HasheousClient.Models.Metadata.IGDB.Artwork
                                        {
                                            Id = image.id,
                                            ImageId = image.filename,
                                            Url = new Uri(theGamesDbGameResult[0].include.boxart.base_url.original + image.filename).ToString(),
                                            AlphaChannel = false,
                                            Animated = false,
                                            Game = theGamesDbGame.id
                                        };

                                        // update cache
                                        Storage.CacheStatus artworkStatus = await Storage.GetCacheStatusAsync(FileSignature.MetadataSources.TheGamesDb, "Artwork", (long)igdbArtworkItem.Id);
                                        switch (artworkStatus)
                                        {
                                            case Storage.CacheStatus.NotPresent:
                                                await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, igdbArtworkItem, false);
                                                break;

                                            case Storage.CacheStatus.Expired:
                                                await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, igdbArtworkItem, true);
                                                break;
                                        }

                                        igdbArtwork.Add(igdbArtworkItem);
                                    }
                                }
                                await File.WriteAllTextAsync(tgdbFile_Artwork, Newtonsoft.Json.JsonConvert.SerializeObject(igdbArtwork, new JsonSerializerSettings
                                {
                                    Formatting = Newtonsoft.Json.Formatting.Indented,
                                    NullValueHandling = NullValueHandling.Ignore,
                                    DefaultValueHandling = DefaultValueHandling.Ignore,
                                    MaxDepth = 25
                                }));

                                // generate clearlogo object
                                if (File.Exists(tgdbFile_ClearLogo))
                                {
                                    File.Delete(tgdbFile_ClearLogo);
                                }
                                foreach (HasheousClient.Models.Metadata.TheGamesDb.GameImage image in imageDict)
                                {
                                    if (image.type == "clearlogo")
                                    {
                                        HasheousClient.Models.Metadata.IGDB.ClearLogo igdbClearWorkItem = new HasheousClient.Models.Metadata.IGDB.ClearLogo
                                        {
                                            Id = image.id,
                                            ImageId = image.filename,
                                            Url = new Uri(theGamesDbGameResult[0].include.boxart.base_url.original + image.filename).ToString(),
                                            AlphaChannel = false,
                                            Animated = false,
                                            Game = theGamesDbGame.id
                                        };

                                        // update cache
                                        Storage.CacheStatus clearLogoStatus = await Storage.GetCacheStatusAsync(FileSignature.MetadataSources.TheGamesDb, "ClearLogo", (long)igdbClearWorkItem.Id);
                                        switch (clearLogoStatus)
                                        {
                                            case Storage.CacheStatus.NotPresent:
                                                await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, igdbClearWorkItem, false);
                                                break;

                                            case Storage.CacheStatus.Expired:
                                                await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, igdbClearWorkItem, true);
                                                break;
                                        }

                                        igdbClearLogo.Add(igdbClearWorkItem);
                                    }
                                }
                                await File.WriteAllTextAsync(tgdbFile_ClearLogo, Newtonsoft.Json.JsonConvert.SerializeObject(igdbClearLogo, new JsonSerializerSettings
                                {
                                    Formatting = Newtonsoft.Json.Formatting.Indented,
                                    NullValueHandling = NullValueHandling.Ignore,
                                    DefaultValueHandling = DefaultValueHandling.Ignore,
                                    MaxDepth = 25
                                }));

                                // generate screenshot object
                                if (File.Exists(tgdbFile_Screenshot))
                                {
                                    File.Delete(tgdbFile_Screenshot);
                                }
                                foreach (HasheousClient.Models.Metadata.TheGamesDb.GameImage image in imageDict)
                                {
                                    if (image.type == "screenshot")
                                    {
                                        HasheousClient.Models.Metadata.IGDB.Screenshot igdbScreenshotItem = new HasheousClient.Models.Metadata.IGDB.Screenshot
                                        {
                                            Id = image.id,
                                            ImageId = image.filename,
                                            Url = new Uri(theGamesDbGameResult[0].include.boxart.base_url.original + image.filename).ToString(),
                                            AlphaChannel = false,
                                            Animated = false,
                                            Game = theGamesDbGame.id
                                        };

                                        // update cache
                                        Storage.CacheStatus screenshotStatus = await Storage.GetCacheStatusAsync(FileSignature.MetadataSources.TheGamesDb, "Screenshot", (long)igdbScreenshotItem.Id);
                                        switch (screenshotStatus)
                                        {
                                            case Storage.CacheStatus.NotPresent:
                                                await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, igdbScreenshotItem, false);
                                                break;

                                            case Storage.CacheStatus.Expired:
                                                await Storage.NewCacheValue(FileSignature.MetadataSources.TheGamesDb, igdbScreenshotItem, true);
                                                break;
                                        }

                                        igdbScreenshot.Add(igdbScreenshotItem);
                                    }
                                }
                                await File.WriteAllTextAsync(tgdbFile_Screenshot, Newtonsoft.Json.JsonConvert.SerializeObject(igdbScreenshot, new JsonSerializerSettings
                                {
                                    Formatting = Newtonsoft.Json.Formatting.Indented,
                                    NullValueHandling = NullValueHandling.Ignore,
                                    DefaultValueHandling = DefaultValueHandling.Ignore,
                                    MaxDepth = 25
                                }));
                            }
                            else
                            {
                                // load the game from the local cache
                                if (File.Exists(tgdbFile_Game))
                                {
                                    string gameJson = await File.ReadAllTextAsync(tgdbFile_Game);
                                    theGamesDbGame = Newtonsoft.Json.JsonConvert.DeserializeObject<HasheousClient.Models.Metadata.TheGamesDb.Game>(gameJson);
                                }
                                else
                                {
                                    throw new InvalidMetadataId(SourceType, Id.ToString());
                                }

                                // load age rating from local cache
                                if (File.Exists(tgdbFile_AgeRating))
                                {
                                    string ageRatingJson = await File.ReadAllTextAsync(tgdbFile_AgeRating);
                                    igdbAgeRating = Newtonsoft.Json.JsonConvert.DeserializeObject<HasheousClient.Models.Metadata.IGDB.AgeRating>(ageRatingJson);
                                }
                                else
                                {
                                    igdbAgeRating = null;
                                }

                                // load cover from local cache
                                if (File.Exists(tgdbFile_Cover))
                                {
                                    string coverJson = await File.ReadAllTextAsync(tgdbFile_Cover);
                                    igdbCover = Newtonsoft.Json.JsonConvert.DeserializeObject<HasheousClient.Models.Metadata.IGDB.Cover>(coverJson);
                                }
                                else
                                {
                                    igdbCover = null;
                                }

                                // load video from local cache
                                if (File.Exists(tgdbFile_Video))
                                {
                                    string videoJson = await File.ReadAllTextAsync(tgdbFile_Video);
                                    igdbVideo = Newtonsoft.Json.JsonConvert.DeserializeObject<HasheousClient.Models.Metadata.IGDB.GameVideo>(videoJson);
                                }
                                else
                                {
                                    igdbVideo = null;
                                }

                                // load artwork from local cache
                                if (File.Exists(tgdbFile_Artwork))
                                {
                                    string artworkJson = await File.ReadAllTextAsync(tgdbFile_Artwork);
                                    igdbArtwork = Newtonsoft.Json.JsonConvert.DeserializeObject<List<HasheousClient.Models.Metadata.IGDB.Artwork>>(artworkJson) ?? new List<HasheousClient.Models.Metadata.IGDB.Artwork>();
                                }
                                else
                                {
                                    igdbArtwork = new List<HasheousClient.Models.Metadata.IGDB.Artwork>();
                                }

                                // load clear logo from local cache
                                if (File.Exists(tgdbFile_ClearLogo))
                                {
                                    string clearLogoJson = await File.ReadAllTextAsync(tgdbFile_ClearLogo);
                                    igdbClearLogo = Newtonsoft.Json.JsonConvert.DeserializeObject<List<HasheousClient.Models.Metadata.IGDB.ClearLogo>>(clearLogoJson) ?? new List<HasheousClient.Models.Metadata.IGDB.ClearLogo>();
                                }
                                else
                                {
                                    igdbClearLogo = new List<HasheousClient.Models.Metadata.IGDB.ClearLogo>();
                                }
                                // load screenshot from local cache
                                if (File.Exists(tgdbFile_Screenshot))
                                {
                                    string screenshotJson = await File.ReadAllTextAsync(tgdbFile_Screenshot);
                                    igdbScreenshot = Newtonsoft.Json.JsonConvert.DeserializeObject<List<HasheousClient.Models.Metadata.IGDB.Screenshot>>(screenshotJson) ?? new List<HasheousClient.Models.Metadata.IGDB.Screenshot>();
                                }
                                else
                                {
                                    igdbScreenshot = new List<HasheousClient.Models.Metadata.IGDB.Screenshot>();
                                }
                            }

                            // generate IGDB game object
                            var igdbGame = new gaseous_server.Models.Game
                            {
                                Id = theGamesDbGame.id,
                                Name = theGamesDbGame.game_title,
                                Summary = theGamesDbGame.overview,
                                Slug = string.Join("-", theGamesDbGame.game_title.Trim().ToLower().Replace(" ", "-").Split(Common.GetInvalidFileNameChars())) + "-" + theGamesDbGame.id,
                                Genres = theGamesDbGame.genres?.Select(g => (long)g).ToList() ?? new List<long>()
                            };

                            if (theGamesDbGame.release_date != null && theGamesDbGame.release_date != "")
                            {
                                igdbGame.FirstReleaseDate = DateTime.TryParse(theGamesDbGame.release_date, out DateTime releaseDate) ? releaseDate : DateTime.MinValue;
                            }

                            if (igdbAgeRating != null)
                            {
                                igdbGame.AgeRatings = new List<long> { (long)igdbAgeRating.Id };
                            }

                            if (igdbCover != null)
                            {
                                igdbGame.Cover = (long)igdbCover.Id;
                            }

                            if (igdbClearLogo != null)
                            {
                                igdbGame.ClearLogo = new Dictionary<FileSignature.MetadataSources, List<long>>();
                                foreach (HasheousClient.Models.Metadata.IGDB.ClearLogo clearLogo in igdbClearLogo)
                                {
                                    if (!igdbGame.ClearLogo.ContainsKey(FileSignature.MetadataSources.TheGamesDb))
                                    {
                                        igdbGame.ClearLogo.Add(FileSignature.MetadataSources.TheGamesDb, new List<long> { (long)clearLogo.Id });
                                    }
                                    else
                                    {
                                        igdbGame.ClearLogo[FileSignature.MetadataSources.TheGamesDb].Add((long)clearLogo.Id);
                                    }
                                }
                            }

                            if (igdbVideo != null)
                            {
                                igdbGame.Videos = new List<long> { (long)igdbVideo.Id };
                            }

                            if (igdbArtwork != null)
                            {
                                igdbGame.Artworks = new List<long>();
                                foreach (HasheousClient.Models.Metadata.IGDB.Artwork artwork in igdbArtwork)
                                {
                                    if (!igdbGame.Artworks.Contains((long)artwork.Id))
                                    {
                                        igdbGame.Artworks.Add((long)artwork.Id);
                                    }
                                }
                            }

                            if (igdbScreenshot != null)
                            {
                                igdbGame.Screenshots = new List<long>();
                                foreach (HasheousClient.Models.Metadata.IGDB.Screenshot screenshot in igdbScreenshot)
                                {
                                    if (!igdbGame.Screenshots.Contains((long)screenshot.Id))
                                    {
                                        igdbGame.Screenshots.Add((long)screenshot.Id);
                                    }
                                }
                            }

                            return igdbGame as T;

                        default:
                            Storage.CacheStatus objectStatus = await Storage.GetCacheStatusAsync(FileSignature.MetadataSources.TheGamesDb, type, Id);
                            if (
                                objectStatus == Storage.CacheStatus.Current ||
                                objectStatus == Storage.CacheStatus.Expired
                                )
                            {
                                return await Storage.GetCacheValue<T>(FileSignature.MetadataSources.TheGamesDb, (T)Activator.CreateInstance(typeof(T)), "Id", Id);
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                    }

                case FileSignature.MetadataSources.IGDB:
                    var results = await comms.APIComm<T>(SourceType, (Communications.MetadataEndpoint)Enum.Parse(typeof(Communications.MetadataEndpoint), type, true), Id);

                    // check for errors
                    if (results == null)
                    {
                        throw new InvalidMetadataId(SourceType, Id);
                    }

                    return results.FirstOrDefault<T>();

                default:
                    // unsupported source type - fail silently
                    return null;
            }
        }

        private static async Task<T> GetMetadataFromServer<T>(FileSignature.MetadataSources SourceType, string Id) where T : class
        {
            // get T type as string
            string type = typeof(T).Name;

            if (SourceType == FileSignature.MetadataSources.None)
            {
                // generate a dummy object
                return (T)Activator.CreateInstance(typeof(T));
            }
            else
            {
                // get metadata from the server
                Communications comms = new Communications();
                var results = await comms.APIComm<T>(SourceType, (Communications.MetadataEndpoint)Enum.Parse(typeof(Communications.MetadataEndpoint), type, true), Id);

                // check for errors
                if (results == null)
                {
                    throw new InvalidMetadataId(SourceType, Id);
                }

                return results.FirstOrDefault<T>();
            }
        }

        #endregion
    }
}