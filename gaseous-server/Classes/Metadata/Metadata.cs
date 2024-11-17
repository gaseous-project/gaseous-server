namespace gaseous_server.Classes.Metadata
{
    public class Metadata
    {
        #region Exception Handling
        public class InvalidMetadataId : Exception
        {
            public InvalidMetadataId(long Id) : base("Invalid Metadata id: " + Id + " from source: " + Communications.MetadataSource + " (default)")
            {
            }

            public InvalidMetadataId(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long Id) : base("Invalid Metadata id: " + Id + " from source: " + SourceType)
            {
            }

            public InvalidMetadataId(string Id) : base("Invalid Metadata id: " + Id + " from source: " + Communications.MetadataSource + " (default)")
            {
            }

            public InvalidMetadataId(string SourceType, string Id) : base("Invalid Metadata id: " + Id + " from source: " + SourceType)
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

            return _GetMetadata<T>(Communications.MetadataSource, Id, ForceRefresh);
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
        public static T? GetMetadata<T>(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long Id, Boolean ForceRefresh = false) where T : class
        {
            if (Id < 0)
            {
                throw new InvalidMetadataId(SourceType, Id);
            }

            return _GetMetadata<T>(SourceType, Id, ForceRefresh);
        }

        private static T? _GetMetadata<T>(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long Id, Boolean ForceRefresh) where T : class
        {
            // get T type as string
            string type = typeof(T).Name;

            // check cached metadata status
            // if metadata is not cached or expired, get it from the source. Otherwise, return the cached metadata
            Storage.CacheStatus? cacheStatus = Storage.GetCacheStatus(SourceType, type, Id);

            // if ForceRefresh is true, set cache status to expired if it is current
            if (ForceRefresh == true)
            {
                if (cacheStatus == Storage.CacheStatus.Current)
                {
                    cacheStatus = Storage.CacheStatus.Expired;
                }
            }

            T? metadata = (T)Activator.CreateInstance(typeof(T));

            switch (cacheStatus)
            {
                case Storage.CacheStatus.Current:
                    metadata = Storage.GetCacheValue<T>(SourceType, metadata, "Id", Id);
                    break;

                case Storage.CacheStatus.Expired:
                    metadata = GetMetadataFromServer<T>(SourceType, Id).Result;
                    Storage.NewCacheValue<T>(SourceType, metadata, true);
                    break;

                case Storage.CacheStatus.NotPresent:
                    metadata = GetMetadataFromServer<T>(SourceType, Id).Result;
                    Storage.NewCacheValue<T>(SourceType, metadata, false);
                    break;
            }

            return metadata;
        }

        private static async Task<T> GetMetadataFromServer<T>(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long Id) where T : class
        {
            // get T type as string
            string type = typeof(T).Name;

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

        #endregion
    }
}