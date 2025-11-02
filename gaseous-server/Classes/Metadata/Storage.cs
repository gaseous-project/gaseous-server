using System;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using IGDB;
using IGDB.Models;
using Microsoft.Extensions.Caching.Memory;

namespace gaseous_server.Classes.Metadata
{
    public class Storage
    {
        /// <summary>
        /// Cache status of a record
        /// </summary>
        public enum CacheStatus
        {
            /// <summary>
            /// The record is not present in the database
            /// </summary>
            NotPresent,

            /// <summary>
            /// The record is present in the database and is current
            /// </summary>
            Current,

            /// <summary>
            /// The record is present in the database but is expired
            /// </summary>
            Expired
        }

        /// <summary>
        /// Get the cache status of a record in the database
        /// </summary>
        /// <param name="SourceType">
        /// The source of the metadata (IGDB, RAWG, etc.)
        /// </param>
        /// <param name="Endpoint">
        /// The endpoint of the metadata (games, companies, etc.)
        /// </param>
        /// <param name="Slug">
        /// The slug of the metadata record
        /// </param>
        /// <returns>
        /// The cache status of the record
        /// </returns>
        public static CacheStatus GetCacheStatus(FileSignature.MetadataSources SourceType, string Endpoint, string Slug)
        {
            return _GetCacheStatus(SourceType, Endpoint, "slug", Slug).Result;
        }

        /// <summary>
        /// Get the cache status of a record in the database
        /// </summary>
        /// <param name="SourceType">
        /// The source of the metadata (IGDB, RAWG, etc.)
        /// </param>
        /// <param name="Endpoint">
        /// The endpoint of the metadata (games, companies, etc.)
        /// </param>
        /// <param name="Slug">
        /// The slug of the metadata record
        /// </param>
        /// <returns>
        /// The cache status of the record
        /// </returns>
        public static async Task<CacheStatus> GetCacheStatusAsync(FileSignature.MetadataSources SourceType, string Endpoint, string Slug)
        {
            return await _GetCacheStatus(SourceType, Endpoint, "slug", Slug);
        }

        /// <summary>
        /// Get the cache status of a record in the database
        /// </summary>
        /// <param name="SourceType">
        /// The source of the metadata (IGDB, RAWG, etc.)
        /// </param>
        /// <param name="Endpoint">
        /// The endpoint of the metadata (games, companies, etc.)
        /// </param>
        /// <param name="Id">
        /// The ID of the metadata record
        /// </param>
        /// <returns>
        /// The cache status of the record
        /// </returns>
        public static CacheStatus GetCacheStatus(FileSignature.MetadataSources SourceType, string Endpoint, long Id)
        {
            return _GetCacheStatus(SourceType, Endpoint, "id", Id).Result;
        }

        /// <summary>
        /// Get the cache status of a record in the database
        /// </summary>
        /// <param name="SourceType">
        /// The source of the metadata (IGDB, RAWG, etc.)
        /// </param>
        /// <param name="Endpoint">
        /// The endpoint of the metadata (games, companies, etc.)
        /// </param>
        /// <param name="Id">
        /// The ID of the metadata record
        /// </param>
        /// <returns>
        /// The cache status of the record
        /// </returns>
        public static async Task<CacheStatus> GetCacheStatusAsync(FileSignature.MetadataSources SourceType, string Endpoint, long Id)
        {
            return await _GetCacheStatus(SourceType, Endpoint, "id", Id);
        }

        /// <summary>
        /// Get the cache status of a record in the database
        /// </summary>
        /// <param name="Row">
        /// The DataRow object to check
        /// </param>
        /// <returns>
        /// The cache status of the record
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when the DataRow object does not contain a "lastUpdated" column
        /// </exception>
        public static CacheStatus GetCacheStatus(DataRow Row)
        {
            if (Row.Table.Columns.Contains("lastUpdated"))
            {
                DateTime CacheExpiryTime = DateTime.UtcNow.AddHours(-168);
                if ((DateTime)Row["lastUpdated"] < CacheExpiryTime)
                {
                    return CacheStatus.Expired;
                }
                else
                {
                    return CacheStatus.Current;
                }
            }
            else
            {
                throw new Exception("No lastUpdated column!");
            }
        }

        private static async Task<CacheStatus> _GetCacheStatus(FileSignature.MetadataSources SourceType, string Endpoint, string SearchField, object SearchValue)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            string sql = "SELECT lastUpdated FROM `Metadata_" + Endpoint + "` WHERE SourceId = @SourceType AND " + SearchField + " = @" + SearchField;

            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("SourceType", SourceType);
            dbDict.Add("Endpoint", Endpoint);
            dbDict.Add(SearchField, SearchValue);

            DataTable dt = await db.ExecuteCMDAsync(sql, dbDict);
            if (dt.Rows.Count == 0)
            {
                // no data stored for this item, or lastUpdated
                return CacheStatus.NotPresent;
            }
            else
            {
                // check if endpoint is non-expiring
                List<string> NonExpiringEndpoints = new List<string>
                {
                    "Artwork", "ClearLogo", "CompanyLogo", "Cover", "Genre", "GameType", "GameVideo", "Genre", "Keyword", "Language", "MultiplayerMode", "PlatformLogo", "PlayerPerspective", "Region", "Theme"
                };
                if (NonExpiringEndpoints.Contains(Endpoint))
                {
                    return CacheStatus.Current;
                }

                // check last updated time
                DateTime CacheExpiryTime = DateTime.UtcNow.AddHours(-168);
                if ((DateTime)dt.Rows[0]["lastUpdated"] < CacheExpiryTime)
                {
                    return CacheStatus.Expired;
                }
                else
                {
                    return CacheStatus.Current;
                }
            }
        }

        /// <summary>
        /// Add a new record to the cache
        /// </summary>
        /// <param name="SourceType">
        /// The source of the metadata (IGDB, RAWG, etc.)
        /// </param>
        /// <param name="ObjectToCache">
        /// The object to cache
        /// </param>
        /// <param name="UpdateRecord">
        /// Whether to update the record if it already exists
        /// </param>
        public static async Task NewCacheValue<T>(FileSignature.MetadataSources SourceType, T ObjectToCache, bool UpdateRecord = false)
        {
            // get the object type name
            string ObjectTypeName = ObjectToCache.GetType().Name;

            // build dictionary
            Dictionary<string, object?> objectDict = new Dictionary<string, object?>
            {
                { "SourceId", SourceType },
                { "dateAdded", DateTime.UtcNow },
                { "lastUpdated", DateTime.UtcNow }
            };
            foreach (PropertyInfo property in ObjectToCache.GetType().GetProperties())
            {
                if (property.GetCustomAttribute<Models.NoDatabaseAttribute>() == null)
                {
                    object? propertyValue = property.GetValue(ObjectToCache);
                    if (propertyValue != null)
                    {
                        objectDict.Add(property.Name, propertyValue);
                    }
                }
            }

            // generate sql
            string fieldList = "";
            string valueList = "";
            string updateFieldValueList = "";
            foreach (KeyValuePair<string, object?> key in objectDict)
            {
                if (fieldList.Length > 0)
                {
                    fieldList = fieldList + ", ";
                    valueList = valueList + ", ";
                }
                fieldList = fieldList + key.Key;
                valueList = valueList + "@" + key.Key;
                if ((key.Key != "id") && (key.Key != "dateAdded") && (key.Key != "SourceId"))
                {
                    if (updateFieldValueList.Length > 0)
                    {
                        updateFieldValueList = updateFieldValueList + ", ";
                    }
                    updateFieldValueList += key.Key + " = @" + key.Key;
                }

                // check property type
                Type objectType = ObjectToCache.GetType();
                if (objectType != null)
                {
                    PropertyInfo objectProperty = objectType.GetProperty(key.Key);
                    if (objectProperty != null)
                    {
                        string compareName = objectProperty.PropertyType.Name.ToLower().Split("`")[0];
                        var objectValue = objectProperty.GetValue(ObjectToCache);
                        if (objectValue != null)
                        {
                            string newObjectValue;
                            Dictionary<string, object> newDict;
                            switch (compareName)
                            {
                                case "identityorvalue":
                                    newObjectValue = Newtonsoft.Json.JsonConvert.SerializeObject(objectValue);
                                    newDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(newObjectValue);
                                    objectDict[key.Key] = newDict["Id"];
                                    break;
                                case "identitiesorvalues":
                                    newObjectValue = Newtonsoft.Json.JsonConvert.SerializeObject(objectValue);
                                    newDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(newObjectValue);
                                    newObjectValue = Newtonsoft.Json.JsonConvert.SerializeObject(newDict["Ids"]);
                                    objectDict[key.Key] = newObjectValue;

                                    await StoreRelations(SourceType, ObjectTypeName, key.Key, (long)objectDict["Id"], newObjectValue);
                                    break;
                                case "list":
                                    newObjectValue = Newtonsoft.Json.JsonConvert.SerializeObject(objectValue);
                                    objectDict[key.Key] = newObjectValue;

                                    await StoreRelations(SourceType, ObjectTypeName, key.Key, (long)objectDict["Id"], newObjectValue);

                                    break;
                                case "int32[]":
                                    newObjectValue = Newtonsoft.Json.JsonConvert.SerializeObject(objectValue);
                                    objectDict[key.Key] = newObjectValue;
                                    break;
                            }
                        }
                    }
                }
            }

            string sql = "";
            if (UpdateRecord == false)
            {
                sql = "INSERT INTO `Metadata_" + ObjectTypeName + "` (" + fieldList + ") VALUES (" + valueList + ");";
            }
            else
            {
                sql = "UPDATE `Metadata_" + ObjectTypeName + "` SET " + updateFieldValueList + " WHERE Id = @Id AND SourceId = @SourceId;";
            }

            // execute sql
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            await db.ExecuteCMDAsync(sql, objectDict);
        }

        /// <summary>
        /// Get a record from the cache
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to return
        /// </typeparam>
        /// <param name="SourceType">
        /// The source of the metadata (IGDB, RAWG, etc.)
        /// </param>
        /// <param name="EndpointType">
        /// The type of the endpoint (games, companies, etc.)
        /// </param>
        /// <param name="SearchField">
        /// The field to search for the record by
        /// </param>
        /// <param name="SearchValue">
        /// The value to search for
        /// </param>
        /// <returns>
        /// The object from the cache
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when no record is found that matches the search criteria
        /// </exception>
        public static async Task<T> GetCacheValue<T>(FileSignature.MetadataSources SourceType, T? EndpointType, string SearchField, object SearchValue)
        {
            string Endpoint = EndpointType.GetType().Name;

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            string sql = "SELECT * FROM `Metadata_" + Endpoint + "` WHERE SourceId = @SourceType AND " + SearchField + " = @" + SearchField;

            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("SourceType", SourceType);
            dbDict.Add("Endpoint", Endpoint);
            dbDict.Add(SearchField, SearchValue);

            DataTable dt = await db.ExecuteCMDAsync(sql, dbDict, new DatabaseMemoryCacheOptions(true, (int)TimeSpan.FromHours(8).Ticks));
            if (dt.Rows.Count == 0)
            {
                // no data stored for this item
                throw new Exception("No record found that matches endpoint " + Endpoint + " with search value " + SearchValue);
            }
            else
            {
                DataRow dataRow = dt.Rows[0];
                object returnObject = BuildCacheObject<T>(EndpointType, dataRow);

                return (T)returnObject;
            }
        }

        public static T BuildCacheObject<T>(T EndpointType, DataRow dataRow)
        {
            // copy the DataRow to EndpointType
            foreach (PropertyInfo property in EndpointType.GetType().GetProperties())
            {
                if (property.GetCustomAttribute<Models.NoDatabaseAttribute>() == null)
                {
                    // get the value from the DataRow with the same name as the property
                    if (dataRow.Table.Columns.Contains(property.Name) == true)
                    {
                        object? value = dataRow[property.Name];

                        if (value != null && value != DBNull.Value)
                        {
                            // check the property type - if it's a list or array, deserialize it. Otherwise, just set the value
                            Type objectType = EndpointType.GetType();
                            if (objectType != null)
                            {
                                // fullname = System.Nullable`1[[System.DateTimeOffset,
                                string propertyTypeName = property.PropertyType.FullName.Split(",")[0];
                                bool isNullable = false;
                                if (propertyTypeName.StartsWith("System.Nullable"))
                                {
                                    isNullable = true;
                                    propertyTypeName = propertyTypeName.Split("[[")[1];
                                }
                                propertyTypeName = propertyTypeName.Split("`")[0];

                                switch (propertyTypeName.ToLower())
                                {
                                    case "system.collections.generic.list":
                                        var listArray = Newtonsoft.Json.JsonConvert.DeserializeObject<List<long>>(value.ToString());
                                        property.SetValue(EndpointType, listArray);
                                        break;

                                    case "system.int32[]":
                                        var int32array = Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>(value.ToString());
                                        property.SetValue(EndpointType, int32array);
                                        break;

                                    case "system.datetimeoffset":
                                        property.SetValue(EndpointType, (DateTimeOffset)(DateTime?)value);
                                        break;

                                    default:
                                        // check if property is an enum
                                        if (property.PropertyType.IsEnum)
                                        {
                                            property.SetValue(EndpointType, Enum.Parse(property.PropertyType, value.ToString()));
                                        }
                                        else if (Common.IsNullableEnum(property.PropertyType))
                                        {
                                            property.SetValue(EndpointType, Enum.Parse(Nullable.GetUnderlyingType(property.PropertyType), value.ToString()));
                                        }
                                        else
                                        {
                                            property.SetValue(EndpointType, value);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            return EndpointType;
        }

        private static async Task StoreRelations(FileSignature.MetadataSources SourceType, string PrimaryTable, string SecondaryTable, long ObjectId, string Relations)
        {
            string TableName = "Relation_" + PrimaryTable + "_" + SecondaryTable;
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM information_schema.tables WHERE table_schema = '" + Config.DatabaseConfiguration.DatabaseName + "' AND table_name = '" + TableName + "';";
            DataTable data = await db.ExecuteCMDAsync(sql);
            if (data.Rows.Count == 0)
            {
                // table doesn't exist, create it
                sql = @"
                    CREATE TABLE 
                    `" + Config.DatabaseConfiguration.DatabaseName + "`.`" + TableName + @"` 
                    (`" + PrimaryTable + @"SourceId` INT NOT NULL, 
                    `" + PrimaryTable + @"Id` BIGINT NOT NULL, 
                    `" + SecondaryTable + @"Id` BIGINT NOT NULL, 
                    PRIMARY KEY (`" + PrimaryTable + "SourceId`, `" + PrimaryTable + "Id`, `" + SecondaryTable + "Id`), INDEX `idx_PrimaryColumn` (`" + PrimaryTable + "Id` ASC) VISIBLE);";
                await db.ExecuteCMDAsync(sql);
            }
            else
            {
                // clean existing records for this object
                sql = "DELETE FROM " + TableName + " WHERE `" + PrimaryTable + "Id` = @objectid";
                Dictionary<string, object> dbDict = new Dictionary<string, object>();
                dbDict.Add("objectid", ObjectId);
                await db.ExecuteCMDAsync(sql, dbDict);
            }

            // insert data
            long[] RelationValues = Newtonsoft.Json.JsonConvert.DeserializeObject<long[]>(Relations);
            foreach (long RelationValue in RelationValues)
            {
                sql = "INSERT IGNORE INTO " + TableName + " (`" + PrimaryTable + "SourceId`, `" + PrimaryTable + "Id`, `" + SecondaryTable + "Id`) VALUES (@sourceid, @objectid, @relationvalue);";
                Dictionary<string, object> dbDict = new Dictionary<string, object>
                {
                    { "sourceid", SourceType },
                    { "objectid", ObjectId },
                    { "relationvalue", RelationValue }
                };
                await db.ExecuteCMDAsync(sql, dbDict);
            }
        }

        public static async Task CreateRelationsTables<T>()
        {
            string PrimaryTable = typeof(T).Name;
            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                string SecondaryTable = property.Name;

                if (property.PropertyType.Name == "IdentitiesOrValues`1")
                {

                    string TableName = "Relation_" + PrimaryTable + "_" + SecondaryTable;
                    Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                    string sql = "SELECT * FROM information_schema.tables WHERE table_schema = '" + Config.DatabaseConfiguration.DatabaseName + "' AND table_name = '" + TableName + "';";
                    DataTable data = await db.ExecuteCMDAsync(sql);
                    if (data.Rows.Count == 0)
                    {
                        // table doesn't exist, create it
                        sql = @"
                            CREATE TABLE 
                            `" + Config.DatabaseConfiguration.DatabaseName + "`.`" + TableName + @"` 
                            (`" + PrimaryTable + @"SourceId` INT NOT NULL, 
                            `" + PrimaryTable + @"Id` BIGINT NOT NULL, 
                            `" + SecondaryTable + @"Id` BIGINT NOT NULL, 
                            PRIMARY KEY (`" + PrimaryTable + "SourceId`, `" + PrimaryTable + "Id`, `" + SecondaryTable + "Id`), INDEX `idx_PrimaryColumn` (`" + PrimaryTable + "Id` ASC) VISIBLE);";
                        await db.ExecuteCMDAsync(sql);
                    }
                }
            }
        }

        private class MemoryCacheObject
        {
            public object Object { get; set; }
            public DateTime CreationTime { get; } = DateTime.UtcNow;
            public DateTime ExpiryTime
            {
                get
                {
                    return CreationTime.AddMinutes(60);
                }
            }
        }
    }
}

