using System.Data;
using System.Data.SqlTypes;
using System.Threading.Tasks;

namespace gaseous_server.Classes.Metadata
{
    public class Metadata
    {
        #region Exception Handling
        public class InvalidMetadataId : Exception
        {
            public InvalidMetadataId(long Id) : base("Invalid Metadata id: " + Id + " from source: " + HasheousClient.Models.MetadataSources.IGDB + " (default)")
            {
            }

            public InvalidMetadataId(HasheousClient.Models.MetadataSources SourceType, long Id) : base("Invalid Metadata id: " + Id + " from source: " + SourceType)
            {
            }

            public InvalidMetadataId(string Id) : base("Invalid Metadata id: " + Id + " from source: " + HasheousClient.Models.MetadataSources.IGDB + " (default)")
            {
            }

            public InvalidMetadataId(HasheousClient.Models.MetadataSources SourceType, string Id) : base("Invalid Metadata id: " + Id + " from source: " + SourceType)
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

            return _GetMetadataAsync<T>(HasheousClient.Models.MetadataSources.IGDB, Id, ForceRefresh).Result;
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
        public static T? GetMetadata<T>(HasheousClient.Models.MetadataSources SourceType, long Id, Boolean ForceRefresh = false) where T : class
        {
            if (Id < 0)
            {
                throw new InvalidMetadataId(SourceType, Id);
            }

            return _GetMetadataAsync<T>(SourceType, Id, ForceRefresh).Result;
        }

        public static T? GetMetadata<T>(HasheousClient.Models.MetadataSources SourceType, string Slug, Boolean ForceRefresh = false) where T : class
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

            return await _GetMetadataAsync<T>(HasheousClient.Models.MetadataSources.IGDB, Id, ForceRefresh);
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
        public static async Task<T?> GetMetadataAsync<T>(HasheousClient.Models.MetadataSources SourceType, long Id, Boolean ForceRefresh = false) where T : class
        {
            if (Id < 0)
            {
                throw new InvalidMetadataId(SourceType, Id);
            }

            return await _GetMetadataAsync<T>(SourceType, Id, ForceRefresh);
        }

        public static async Task<T?> GetMetadataAsync<T>(HasheousClient.Models.MetadataSources SourceType, string Slug, Boolean ForceRefresh = false) where T : class
        {
            return await _GetMetadataAsync<T>(SourceType, Slug, ForceRefresh);
        }

        private static async Task<T?> _GetMetadataAsync<T>(HasheousClient.Models.MetadataSources SourceType, object Id, Boolean ForceRefresh) where T : class
        {
            // get T type as string
            string type = typeof(T).Name;

            // get type of Id as string
            IdType idType = Id.GetType() == typeof(long) ? IdType.Long : IdType.String;

            // check cached metadata status
            // if metadata is not cached or expired, get it from the source. Otherwise, return the cached metadata
            Storage.CacheStatus? cacheStatus;
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
            if (SourceType == HasheousClient.Models.MetadataSources.None)
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
                        Logging.Log(Logging.LogType.Information, "Fetch Metadata", "Failed to fetch metadata from source: " + SourceType + " for id: " + Id + " of type: " + type + ". Error: " + e.Message);
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
                        Logging.Log(Logging.LogType.Information, "Fetch Metadata", "Failed to fetch metadata from source: " + SourceType + " for id: " + Id + " of type: " + type + ". Error: " + e.Message);
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

        private static async Task<T> GetMetadataFromServer<T>(HasheousClient.Models.MetadataSources SourceType, long Id) where T : class
        {
            // get T type as string
            string type = typeof(T).Name;

            // get metadata from the server
            Communications comms = new Communications();

            switch (SourceType)
            {
                case HasheousClient.Models.MetadataSources.None:
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

                case HasheousClient.Models.MetadataSources.TheGamesDb:
                    switch (type)
                    {
                        case "Game":
                            // connect to hasheous api and get TheGamesDb metadata and convert it to IGDB metadata
                            var theGamesDbGameResult = await comms.APIComm<HasheousClient.Models.Metadata.TheGamesDb.GamesByGameID>(SourceType, Communications.MetadataEndpoint.Game, Id);

                            // get genres from TheGamesDb if we haven't before
                            Storage.CacheStatus genreStatus = await Storage.GetCacheStatusAsync(HasheousClient.Models.MetadataSources.TheGamesDb, "Genre", 1);
                            if (genreStatus == Storage.CacheStatus.NotPresent || genreStatus == Storage.CacheStatus.Expired)
                            {
                                var theGamesDbGenres = await comms.APIComm<HasheousClient.Models.Metadata.TheGamesDb.Genres>(SourceType, Communications.MetadataEndpoint.Genre, 1);
                                if (theGamesDbGenres != null)
                                {
                                    HasheousClient.Models.Metadata.TheGamesDb.Genres genres = theGamesDbGenres[0];

                                    foreach (string genre in genres.data.genres.Keys)
                                    {
                                        Storage.CacheStatus genreCacheStatus = await Storage.GetCacheStatusAsync(HasheousClient.Models.MetadataSources.TheGamesDb, "Genre", genres.data.genres[genre].id);
                                        switch (genreCacheStatus)
                                        {
                                            case Storage.CacheStatus.NotPresent:
                                                await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, new HasheousClient.Models.Metadata.IGDB.Genre
                                                {
                                                    Id = genres.data.genres[genre].id,
                                                    Name = genres.data.genres[genre].name
                                                }, false);
                                                break;

                                            case Storage.CacheStatus.Expired:
                                                await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, new HasheousClient.Models.Metadata.IGDB.Genre
                                                {
                                                    Id = genres.data.genres[genre].id,
                                                    Name = genres.data.genres[genre].name
                                                }, true);
                                                break;
                                        }
                                    }
                                }
                            }

                            // create a new IGDB game object
                            HasheousClient.Models.Metadata.TheGamesDb.Game theGamesDbGame = theGamesDbGameResult[0].data.games[0];

                            // generate age rating object
                            HasheousClient.Models.Metadata.IGDB.AgeRating? igdbAgeRating = null;
                            long? igdbAgeRatingTitle = null;
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
                                    Storage.CacheStatus ageRatingStatus = await Storage.GetCacheStatusAsync(HasheousClient.Models.MetadataSources.TheGamesDb, "AgeRating", (long)igdbAgeRating.Id);
                                    switch (ageRatingStatus)
                                    {
                                        case Storage.CacheStatus.NotPresent:
                                            await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, igdbAgeRating, false);
                                            break;

                                        case Storage.CacheStatus.Expired:
                                            await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, igdbAgeRating, true);
                                            break;
                                    }
                                }
                            }

                            // generate cover image object
                            HasheousClient.Models.Metadata.IGDB.Cover? igdbCover = null;
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

                                        // update cache
                                        Storage.CacheStatus coverStatus = await Storage.GetCacheStatusAsync(HasheousClient.Models.MetadataSources.TheGamesDb, "Cover", (long)igdbCover.Id);
                                        switch (coverStatus)
                                        {
                                            case Storage.CacheStatus.NotPresent:
                                                await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, igdbCover, false);
                                                break;

                                            case Storage.CacheStatus.Expired:
                                                await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, igdbCover, true);
                                                break;
                                        }
                                        break;
                                    }
                                }
                            }

                            // generate youtube video object
                            HasheousClient.Models.Metadata.IGDB.GameVideo? igdbVideo = null;
                            if (theGamesDbGame.youtube != null && theGamesDbGame.youtube != "")
                            {
                                igdbVideo = new HasheousClient.Models.Metadata.IGDB.GameVideo
                                {
                                    Id = theGamesDbGame.id,
                                    Name = theGamesDbGame.game_title,
                                    VideoId = theGamesDbGame.youtube,
                                    Game = theGamesDbGame.id
                                };

                                // update cache
                                Storage.CacheStatus videoStatus = await Storage.GetCacheStatusAsync(HasheousClient.Models.MetadataSources.TheGamesDb, "GameVideo", (long)igdbVideo.Id);
                                switch (videoStatus)
                                {
                                    case Storage.CacheStatus.NotPresent:
                                        await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, igdbVideo, false);
                                        break;

                                    case Storage.CacheStatus.Expired:
                                        await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, igdbVideo, true);
                                        break;
                                }
                            }

                            // generate artwork object
                            List<HasheousClient.Models.Metadata.IGDB.Artwork> igdbArtwork = new List<HasheousClient.Models.Metadata.IGDB.Artwork>();
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
                                    Storage.CacheStatus artworkStatus = await Storage.GetCacheStatusAsync(HasheousClient.Models.MetadataSources.TheGamesDb, "Artwork", (long)igdbArtworkItem.Id);
                                    switch (artworkStatus)
                                    {
                                        case Storage.CacheStatus.NotPresent:
                                            await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, igdbArtworkItem, false);
                                            break;

                                        case Storage.CacheStatus.Expired:
                                            await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, igdbArtworkItem, true);
                                            break;
                                    }

                                    igdbArtwork.Add(igdbArtworkItem);
                                }
                            }

                            // generate clearlogo object
                            List<HasheousClient.Models.Metadata.IGDB.ClearLogo> igdbClearLogo = new List<HasheousClient.Models.Metadata.IGDB.ClearLogo>();
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
                                    Storage.CacheStatus clearLogoStatus = await Storage.GetCacheStatusAsync(HasheousClient.Models.MetadataSources.TheGamesDb, "ClearLogo", (long)igdbClearWorkItem.Id);
                                    switch (clearLogoStatus)
                                    {
                                        case Storage.CacheStatus.NotPresent:
                                            await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, igdbClearWorkItem, false);
                                            break;

                                        case Storage.CacheStatus.Expired:
                                            await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, igdbClearWorkItem, true);
                                            break;
                                    }

                                    igdbClearLogo.Add(igdbClearWorkItem);
                                }
                            }

                            // generate screenshot object
                            List<HasheousClient.Models.Metadata.IGDB.Screenshot> igdbScreenshot = new List<HasheousClient.Models.Metadata.IGDB.Screenshot>();
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
                                    Storage.CacheStatus screenshotStatus = await Storage.GetCacheStatusAsync(HasheousClient.Models.MetadataSources.TheGamesDb, "Screenshot", (long)igdbScreenshotItem.Id);
                                    switch (screenshotStatus)
                                    {
                                        case Storage.CacheStatus.NotPresent:
                                            await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, igdbScreenshotItem, false);
                                            break;

                                        case Storage.CacheStatus.Expired:
                                            await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.TheGamesDb, igdbScreenshotItem, true);
                                            break;
                                    }

                                    igdbScreenshot.Add(igdbScreenshotItem);
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
                                igdbGame.ClearLogo = new Dictionary<HasheousClient.Models.MetadataSources, List<long>>();
                                foreach (HasheousClient.Models.Metadata.IGDB.ClearLogo clearLogo in igdbClearLogo)
                                {
                                    if (!igdbGame.ClearLogo.ContainsKey(HasheousClient.Models.MetadataSources.TheGamesDb))
                                    {
                                        igdbGame.ClearLogo.Add(HasheousClient.Models.MetadataSources.TheGamesDb, new List<long> { (long)clearLogo.Id });
                                    }
                                    else
                                    {
                                        igdbGame.ClearLogo[HasheousClient.Models.MetadataSources.TheGamesDb].Add((long)clearLogo.Id);
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
                            Storage.CacheStatus objectStatus = await Storage.GetCacheStatusAsync(HasheousClient.Models.MetadataSources.TheGamesDb, type, Id);
                            if (
                                objectStatus == Storage.CacheStatus.Current ||
                                objectStatus == Storage.CacheStatus.Expired
                                )
                            {
                                return await Storage.GetCacheValue<T>(HasheousClient.Models.MetadataSources.TheGamesDb, (T)Activator.CreateInstance(typeof(T)), "Id", Id);
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }
                    }

                default:
                    var results = await comms.APIComm<T>(SourceType, (Communications.MetadataEndpoint)Enum.Parse(typeof(Communications.MetadataEndpoint), type, true), Id);

                    // check for errors
                    if (results == null)
                    {
                        throw new InvalidMetadataId(SourceType, Id);
                    }

                    return results.FirstOrDefault<T>();
            }
        }

        private static async Task<T> GetMetadataFromServer<T>(HasheousClient.Models.MetadataSources SourceType, string Id) where T : class
        {
            // get T type as string
            string type = typeof(T).Name;

            if (SourceType == HasheousClient.Models.MetadataSources.None)
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