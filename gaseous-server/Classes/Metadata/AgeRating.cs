using System;
using System.Reflection;
using System.Text.Json.Serialization;
using IGDB;
using IGDB.Models;
using Microsoft.CodeAnalysis.Classification;

namespace gaseous_server.Classes.Metadata
{
	public class AgeRatings
    {
        const string fieldList = "fields category,checksum,content_descriptions,rating,rating_cover_url,synopsis;";

        public AgeRatings()
        {
        }

        public static AgeRating? GetAgeRatings(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<AgeRating> RetVal = _GetAgeRatings(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static AgeRating GetAgeRatings(string Slug)
        {
            Task<AgeRating> RetVal = _GetAgeRatings(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<AgeRating> _GetAgeRatings(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("AgeRating", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("AgeRating", (string)searchValue);
            }

            // set up where clause
            string WhereClause = "";
            switch (searchUsing)
            {
                case SearchUsing.id:
                    WhereClause = "where id = " + searchValue;
                    break;
                case SearchUsing.slug:
                    WhereClause = "where slug = " + searchValue;
                    break;
                default:
                    throw new Exception("Invalid search type");
            }

            AgeRating returnValue = new AgeRating();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue);
                    UpdateSubClasses(returnValue);
                    break;  
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause);
                        Storage.NewCacheValue(returnValue, true);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<AgeRating>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<AgeRating>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private static void UpdateSubClasses(AgeRating ageRating)
        {
            if (ageRating.ContentDescriptions != null)
            {
                foreach (long AgeRatingContentDescriptionId in ageRating.ContentDescriptions.Ids)
                {
                    AgeRatingContentDescription ageRatingContentDescription = AgeRatingContentDescriptions.GetAgeRatingContentDescriptions(AgeRatingContentDescriptionId);
                }
            }
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<AgeRating> GetObjectFromServer(string WhereClause)
        {
            // get AgeRatings metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<AgeRating>(IGDBClient.Endpoints.AgeRating, fieldList, WhereClause);
            var result = results.First();

            return result;
        }

        public static GameAgeRating GetConsolidatedAgeRating(long RatingId)
        {
            GameAgeRating gameAgeRating = new GameAgeRating();

            AgeRating ageRating = GetAgeRatings(RatingId);
            gameAgeRating.Id = (long)ageRating.Id;
            gameAgeRating.RatingBoard = (AgeRatingCategory)ageRating.Category;
            gameAgeRating.RatingTitle = (AgeRatingTitle)ageRating.Rating;

            List<string> descriptions = new List<string>();
            if (ageRating.ContentDescriptions != null)
            {
                foreach (long ContentId in ageRating.ContentDescriptions.Ids)
                {
                    AgeRatingContentDescription ageRatingContentDescription = AgeRatingContentDescriptions.GetAgeRatingContentDescriptions(ContentId);
                    descriptions.Add(ageRatingContentDescription.Description);
                }
            }
            gameAgeRating.Descriptions = descriptions.ToArray();

            return gameAgeRating;
        }

        public class GameAgeRating
        {
            public long Id { get; set; }
            public AgeRatingCategory RatingBoard { get; set; }
            public AgeRatingTitle RatingTitle { get; set; }
            public string[] Descriptions { get; set; }
        }

        public static void PopulateAgeMap()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM ClassificationMap;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            db.ExecuteNonQuery(sql);

            // loop all age groups
            foreach(KeyValuePair<AgeGroups.AgeRestrictionGroupings, AgeGroups.AgeGroupItem> ageGrouping in AgeGroups.AgeGroupingsFlat)
            {
                AgeGroups.AgeGroupItem ageGroupItem = ageGrouping.Value;
                var properties = ageGroupItem.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.GetGetMethod() != null)
                    {
                        List<string> AgeRatingCategories = new List<string>(Enum.GetNames(typeof(AgeRatingCategory)));
                        if (AgeRatingCategories.Contains(prop.Name))
                        {
                            AgeRatingCategory ageRatingCategory = (AgeRatingCategory)Enum.Parse(typeof(AgeRatingCategory), prop.Name);
                            List<AgeRatingTitle> ageRatingTitles = (List<AgeRatingTitle>)prop.GetValue(ageGroupItem);
                            
                            foreach (AgeRatingTitle ageRatingTitle in ageRatingTitles)
                            {
                                dbDict.Clear();
                                dbDict.Add("AgeGroupId", ageGrouping.Key);
                                dbDict.Add("ClassificationBoardId", ageRatingCategory);
                                dbDict.Add("RatingId", ageRatingTitle);

                                sql = "INSERT INTO ClassificationMap (AgeGroupId, ClassificationBoardId, RatingId) VALUES (@AgeGroupId, @ClassificationBoardId, @RatingId);";
                                db.ExecuteCMD(sql, dbDict);
                            }
                        }
                    }
                }
            }
        }
	}
}

