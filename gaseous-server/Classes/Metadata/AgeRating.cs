using System;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using IGDB;
using IGDB.Models;
using Microsoft.CodeAnalysis.Classification;

namespace gaseous_server.Classes.Metadata
{
    public class AgeRatings
    {
        const string fieldList = "fields *;";

        public AgeRatings()
        {
        }

        public static AgeRating? GetAgeRatings(long? Id, bool forceRefresh = false)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<AgeRating> RetVal = _GetAgeRatings(SearchUsing.id, Id, forceRefresh);
                return RetVal.Result;
            }
        }

        private static async Task<AgeRating> _GetAgeRatings(SearchUsing searchUsing, object searchValue, bool forceRefresh = false)
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

            if (forceRefresh == true)
            {
                cacheStatus = Storage.CacheStatus.Expired; // force refresh
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

        private static async Task UpdateSubClasses(AgeRating ageRating)
        {
            GameAgeRating gameAgeRating = await GetConsolidatedAgeRating((long)ageRating.Id);
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

        public static async Task<GameAgeRating> GetConsolidatedAgeRating(long RatingId)
        {
            GameAgeRating gameAgeRating = new GameAgeRating();

            AgeRating ageRating = GetAgeRatings(RatingId);
            gameAgeRating.Id = (long)ageRating.Id;
            gameAgeRating.RatingBoard = AgeRatingOrganizations.GetAgeRatingOrganizations(ageRating.Organization.Id);
            gameAgeRating.RatingTitle = AgeRatingCategories.GetAgeRatingCategory(ageRating.RatingCategory.Id);

            List<AgeRatingContentDescriptionV2> descriptions = new List<AgeRatingContentDescriptionV2>();
            if (ageRating.RatingContentDescriptions != null)
            {
                foreach (long ContentId in ageRating.RatingContentDescriptions.Ids)
                {
                    try
                    {
                        AgeRatingContentDescriptionV2 ageRatingContentDescription = AgeRatingContentDescriptionsV2.GetAgeRatingContentDescriptions(ContentId);
                        descriptions.Add(ageRatingContentDescription);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            gameAgeRating.Descriptions = descriptions.ToArray();

            return gameAgeRating;
        }

        public class GameAgeRating
        {
            public long Id { get; set; }
            public AgeRatingOrganization RatingBoard { get; set; }
            public AgeRatingCategory RatingTitle { get; set; }
            public AgeRatingContentDescriptionV2[] Descriptions { get; set; }
        }

        public static async Task PopulateAgeMapAsync()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "DELETE FROM ClassificationMap;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            db.ExecuteNonQuery(sql);

            // loop all AgeRestrictionGroupings enums and store each item in a string
            foreach (AgeGroups.AgeRestrictionGroupings AgeRestrictionGroup in Enum.GetValues(typeof(AgeGroups.AgeRestrictionGroupings))) // example Adult, Teen, etc
            {
                if (AgeRestrictionGroup != AgeGroups.AgeRestrictionGroupings.Unclassified)
                {
                    int ageRestrictionGroupValue = (int)AgeRestrictionGroup;

                    // loop all AgeGroups in the AgeGroups.AgeGroupMap
                    foreach (var ratingBoard in AgeGroups.AgeGroupMap.AgeGroups[AgeRestrictionGroup].Ratings.Keys)
                    {
                        // collect ratings for this AgeRestrictionGroup
                        if (AgeGroups.AgeGroupMap.RatingBoards.ContainsKey(ratingBoard))
                        {
                            var ratingBoardItem = AgeGroups.AgeGroupMap.RatingBoards[ratingBoard];
                            long ratingBoardId = (long)ratingBoardItem.IGDBId;

                            // loop all ratings for this rating board
                            foreach (var rating in AgeGroups.AgeGroupMap.AgeGroups[AgeRestrictionGroup].Ratings[ratingBoard])
                            {
                                if (ratingBoardItem.Ratings.ContainsKey(rating))
                                {
                                    long ratingId = (long)ratingBoardItem.Ratings[rating].IGDBId;

                                    // insert into ClassificationMap
                                    sql = "INSERT INTO ClassificationMap (AgeGroupId, ClassificationBoardId, RatingId) VALUES (@ageGroupId, @classificationBoardId, @ratingId);";
                                    dbDict.Clear();
                                    dbDict.Add("@ageGroupId", ageRestrictionGroupValue);
                                    dbDict.Add("@classificationBoardId", ratingBoardId);
                                    dbDict.Add("@ratingId", ratingId);

                                    try
                                    {
                                        db.ExecuteNonQuery(sql, dbDict);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error inserting into ClassificationMap: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

