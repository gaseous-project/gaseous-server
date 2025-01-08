using System.Data;

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

            return _GetMetadata<T>(HasheousClient.Models.MetadataSources.IGDB, Id, ForceRefresh);
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

            return _GetMetadata<T>(SourceType, Id, ForceRefresh);
        }

        public static T? GetMetadata<T>(HasheousClient.Models.MetadataSources SourceType, string Slug, Boolean ForceRefresh = false) where T : class
        {
            return _GetMetadata<T>(SourceType, Slug, ForceRefresh);
        }

        private static T? _GetMetadata<T>(HasheousClient.Models.MetadataSources SourceType, object Id, Boolean ForceRefresh) where T : class
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
                cacheStatus = Storage.GetCacheStatus(SourceType, type, (long)Id);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus(SourceType, type, (string)Id);
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
                        metadata = Storage.GetCacheValue<T>(SourceType, metadata, "Id", (long)Id);
                    }
                    else
                    {
                        metadata = Storage.GetCacheValue<T>(SourceType, metadata, "Slug", (string)Id);
                    }
                    break;

                case Storage.CacheStatus.Expired:
                    try
                    {
                        if (idType == IdType.Long)
                        {
                            metadata = GetMetadataFromServer<T>(SourceType, (long)Id).Result;
                        }
                        else
                        {
                            metadata = GetMetadataFromServer<T>(SourceType, (string)Id).Result;
                        }
                        Storage.NewCacheValue<T>(SourceType, metadata, true);
                    }
                    catch (Exception e)
                    {
                        Logging.Log(Logging.LogType.Warning, "Fetch Metadata", "Failed to fetch metadata from source: " + SourceType + " for id: " + Id + " of type: " + type + ". Error: " + e.Message);
                        metadata = null;
                    }
                    break;

                case Storage.CacheStatus.NotPresent:
                    try
                    {
                        if (idType == IdType.Long)
                        {
                            metadata = GetMetadataFromServer<T>(SourceType, (long)Id).Result;
                        }
                        else
                        {
                            metadata = GetMetadataFromServer<T>(SourceType, (string)Id).Result;
                        }
                        Storage.NewCacheValue<T>(SourceType, metadata, false);
                    }
                    catch (Exception e)
                    {
                        Logging.Log(Logging.LogType.Warning, "Fetch Metadata", "Failed to fetch metadata from source: " + SourceType + " for id: " + Id + " of type: " + type + ". Error: " + e.Message);
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

            if (SourceType == HasheousClient.Models.MetadataSources.None)
            {
                // generate a dummy object
                var returnObject = (T)Activator.CreateInstance(typeof(T));
                returnObject.GetType().GetProperty("Id").SetValue(returnObject, Id);

                // if returnObject has a property called "name", query the metadatamap view for the name
                if (returnObject.GetType().GetProperty("Name") != null)
                {
                    Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                    string sql = "SELECT * FROM MetadataMap JOIN MetadataMapBridge ON MetadataMap.Id = MetadataMapBridge.ParentMapId WHERE MetadataSourceId = @id AND MetadataSourceType = 0;";
                    DataTable dataTable = db.ExecuteCMD(sql, new Dictionary<string, object>
                    {
                        { "@id", Id }
                    });
                    if (dataTable.Rows.Count > 0)
                    {
                        returnObject.GetType().GetProperty("Name").SetValue(returnObject, dataTable.Rows[0]["SignatureGameName"].ToString());
                    }
                }

                return returnObject;
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