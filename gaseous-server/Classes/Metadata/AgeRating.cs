using System;
using System.Buffers;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;
using Microsoft.CodeAnalysis.Classification;

namespace gaseous_server.Classes.Metadata
{
    public class AgeRatings
    {
        public AgeRatings()
        {
        }

        public static async Task<AgeRating?> GetAgeRating(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                AgeRating? RetVal = await Metadata.GetMetadataAsync<AgeRating>(SourceType, (long)Id, false);
                return RetVal;
            }
        }

        public static async Task<GameAgeRating> GetConsolidatedAgeRating(HasheousClient.Models.MetadataSources SourceType, long RatingId)
        {
            GameAgeRating gameAgeRating = new GameAgeRating();

            AgeRating ageRating = await GetAgeRating(SourceType, RatingId);
            gameAgeRating.Id = (long)ageRating.Id;
            gameAgeRating.RatingBoard = await AgeRatingOrganizations.GetAgeRatingOrganization(SourceType, ageRating.Organization);
            gameAgeRating.RatingTitle = await AgeRatingCategorys.GetAgeRatingCategory(SourceType, ageRating.RatingCategory);

            List<AgeRatingContentDescriptionV2> descriptions = new List<AgeRatingContentDescriptionV2>();
            if (ageRating.RatingContentDescriptions != null)
            {
                foreach (long ContentId in ageRating.RatingContentDescriptions)
                {
                    try
                    {
                        AgeRatingContentDescriptionV2 ageRatingContentDescription = await AgeRatingContentDescriptionsV2.GetAgeRatingContentDescriptionsV2(SourceType, ContentId);
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
                        if (AgeGroups.AgeGroupMap.RatingsBoards.ContainsKey(ratingBoard))
                        {
                            var ratingBoardItem = AgeGroups.AgeGroupMap.RatingsBoards[ratingBoard];
                            long ratingBoardId = ratingBoardItem.IGDBId;

                            // loop all ratings for this rating board
                            foreach (var rating in AgeGroups.AgeGroupMap.AgeGroups[AgeRestrictionGroup].Ratings[ratingBoard])
                            {
                                if (ratingBoardItem.Ratings.ContainsKey(rating))
                                {
                                    long ratingId = ratingBoardItem.Ratings[rating].IGDBId;

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

