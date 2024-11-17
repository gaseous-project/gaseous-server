using System;
using System.Buffers;
using System.Reflection;
using System.Text.Json.Serialization;
using HasheousClient.Models.Metadata.IGDB;
using Microsoft.CodeAnalysis.Classification;

namespace gaseous_server.Classes.Metadata
{
    public class AgeRatings
    {
        public const string fieldList = "fields category,checksum,content_descriptions,rating,rating_cover_url,synopsis;";

        public AgeRatings()
        {
        }

        public static AgeRating? GetAgeRating(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                AgeRating? RetVal = Metadata.GetMetadata<AgeRating>(SourceType, (long)Id, false);
                return RetVal;
            }
        }

        public static GameAgeRating GetConsolidatedAgeRating(HasheousClient.Models.MetadataModel.MetadataSources SourceType, long RatingId)
        {
            GameAgeRating gameAgeRating = new GameAgeRating();

            AgeRating ageRating = GetAgeRating(SourceType, RatingId);
            gameAgeRating.Id = (long)ageRating.Id;
            gameAgeRating.RatingBoard = (AgeRatingCategory)ageRating.Category;
            gameAgeRating.RatingTitle = (AgeRatingTitle)ageRating.Rating;

            List<string> descriptions = new List<string>();
            if (ageRating.ContentDescriptions != null)
            {
                foreach (long ContentId in ageRating.ContentDescriptions)
                {
                    AgeRatingContentDescription ageRatingContentDescription = AgeRatingContentDescriptions.GetAgeRatingContentDescriptions(SourceType, ContentId);
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
            foreach (KeyValuePair<AgeGroups.AgeRestrictionGroupings, AgeGroups.AgeGroupItem> ageGrouping in AgeGroups.AgeGroupingsFlat)
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

