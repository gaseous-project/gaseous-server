using System;
using System.Reflection;
using System.Text.Json.Serialization;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
	public class AgeRatings
    {
        const string fieldList = "fields category,checksum,content_descriptions,rating,rating_cover_url,synopsis;";

        public AgeRatings()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

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
            var results = await igdb.QueryAsync<AgeRating>(IGDBClient.Endpoints.AgeRating, query: fieldList + " " + WhereClause + ";");
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

        public class AgeGroups
        {
            public AgeGroups()
            {

            }

            public static Dictionary<string, List<AgeGroupItem>> AgeGroupings
            {
                get
                {
                    return new Dictionary<string, List<AgeGroupItem>>{
                        { 
                            "Adult", new List<AgeGroupItem>{ Adult_Item, Mature_Item, Teen_Item, Child_Item } 
                        },
                        {
                            "Mature", new List<AgeGroupItem>{ Mature_Item, Teen_Item, Child_Item }
                        },
                        {
                            "Teen", new List<AgeGroupItem>{ Teen_Item, Child_Item }
                        },
                        { 
                            "Child", new List<AgeGroupItem>{ Child_Item }
                        }
                    };
                }
            }

            public static Dictionary<string, AgeGroupItem> AgeGroupingsFlat
            {
                get
                {
                    return new Dictionary<string, AgeGroupItem>{
                        {
                            "Adult", Adult_Item
                        },
                        {
                            "Mature", Mature_Item
                        },
                        {
                            "Teen", Teen_Item
                        },
                        {
                            "Child", Child_Item
                        }
                    };
                }
            }

            public static List<ClassificationBoardItem> ClassificationBoards
            {
                get
                {
                    ClassificationBoardItem boardItem = new ClassificationBoardItem{ 
                        Board = AgeRatingCategory.ACB, 
                        Classifications = new List<AgeRatingTitle>{
                            AgeRatingTitle.ACB_G, AgeRatingTitle.ACB_M, AgeRatingTitle.ACB_MA15, AgeRatingTitle.ACB_R18, AgeRatingTitle.ACB_RC
                        }
                    };

                    return new List<ClassificationBoardItem>{
                        new ClassificationBoardItem{ 
                            Board = AgeRatingCategory.ACB, 
                            Classifications = new List<AgeRatingTitle>{
                                AgeRatingTitle.ACB_G,
                                AgeRatingTitle.ACB_M,
                                AgeRatingTitle.ACB_MA15,
                                AgeRatingTitle.ACB_R18,
                                AgeRatingTitle.ACB_RC
                            }
                        },
                        new ClassificationBoardItem{ 
                            Board = AgeRatingCategory.CERO, 
                            Classifications = new List<AgeRatingTitle>{
                                AgeRatingTitle.CERO_A,
                                AgeRatingTitle.CERO_B,
                                AgeRatingTitle.CERO_C,
                                AgeRatingTitle.CERO_D,
                                AgeRatingTitle.CERO_Z
                            }
                        },
                        new ClassificationBoardItem{ 
                            Board = AgeRatingCategory.CLASS_IND, 
                            Classifications = new List<AgeRatingTitle>{
                                AgeRatingTitle.CLASS_IND_L,
                                AgeRatingTitle.CLASS_IND_Ten,
                                AgeRatingTitle.CLASS_IND_Twelve,
                                AgeRatingTitle.CLASS_IND_Fourteen,
                                AgeRatingTitle.CLASS_IND_Sixteen,
                                AgeRatingTitle.CLASS_IND_Eighteen
                            }
                        }
                    };
                }
            }

            readonly static AgeGroupItem Adult_Item = new AgeGroupItem{
                ACB         = new List<AgeRatingTitle>{ AgeRatingTitle.ACB_R18, AgeRatingTitle.ACB_RC },
                CERO        = new List<AgeRatingTitle>{ AgeRatingTitle.CERO_Z },
                CLASS_IND   = new List<AgeRatingTitle>{ AgeRatingTitle.CLASS_IND_Eighteen },
                ESRB        = new List<AgeRatingTitle>{ AgeRatingTitle.RP, AgeRatingTitle.AO },
                GRAC        = new List<AgeRatingTitle>{ AgeRatingTitle.GRAC_Eighteen },
                PEGI        = new List<AgeRatingTitle>{ AgeRatingTitle.Eighteen},
                USK         = new List<AgeRatingTitle>{ AgeRatingTitle.USK_18}
            };

            readonly static AgeGroupItem Mature_Item = new AgeGroupItem{
                ACB         = new List<AgeRatingTitle>{ AgeRatingTitle.ACB_M, AgeRatingTitle.ACB_MA15 },
                CERO        = new List<AgeRatingTitle>{ AgeRatingTitle.CERO_C, AgeRatingTitle.CERO_D },
                CLASS_IND   = new List<AgeRatingTitle>{ AgeRatingTitle.CLASS_IND_Sixteen },
                ESRB        = new List<AgeRatingTitle>{ AgeRatingTitle.M },
                GRAC        = new List<AgeRatingTitle>{ AgeRatingTitle.GRAC_Fifteen },
                PEGI        = new List<AgeRatingTitle>{ AgeRatingTitle.Sixteen},
                USK         = new List<AgeRatingTitle>{ AgeRatingTitle.USK_16}
            };

            readonly static AgeGroupItem Teen_Item = new AgeGroupItem{
                ACB         = new List<AgeRatingTitle>{ AgeRatingTitle.ACB_PG },
                CERO        = new List<AgeRatingTitle>{ AgeRatingTitle.CERO_B },
                CLASS_IND   = new List<AgeRatingTitle>{ AgeRatingTitle.CLASS_IND_Twelve, AgeRatingTitle.CLASS_IND_Fourteen },
                ESRB        = new List<AgeRatingTitle>{ AgeRatingTitle.T },
                GRAC        = new List<AgeRatingTitle>{ AgeRatingTitle.GRAC_Twelve },
                PEGI        = new List<AgeRatingTitle>{ AgeRatingTitle.Twelve},
                USK         = new List<AgeRatingTitle>{ AgeRatingTitle.USK_12}
            };

            readonly static AgeGroupItem Child_Item = new AgeGroupItem{
                ACB         = new List<AgeRatingTitle>{ AgeRatingTitle.ACB_G },
                CERO        = new List<AgeRatingTitle>{ AgeRatingTitle.CERO_A },
                CLASS_IND   = new List<AgeRatingTitle>{ AgeRatingTitle.CLASS_IND_L, AgeRatingTitle.CLASS_IND_Ten },
                ESRB        = new List<AgeRatingTitle>{ AgeRatingTitle.E, AgeRatingTitle.E10 },
                GRAC        = new List<AgeRatingTitle>{ AgeRatingTitle.GRAC_All },
                PEGI        = new List<AgeRatingTitle>{ AgeRatingTitle.Three, AgeRatingTitle.Seven},
                USK         = new List<AgeRatingTitle>{ AgeRatingTitle.USK_0, AgeRatingTitle.USK_6}
            };

            public class AgeGroupItem
            {
                public List<IGDB.Models.AgeRatingTitle> ACB { get; set; }
                public List<IGDB.Models.AgeRatingTitle> CERO { get; set; }
                public List<IGDB.Models.AgeRatingTitle> CLASS_IND { get; set; }
                public List<IGDB.Models.AgeRatingTitle> ESRB { get; set; }
                public List<IGDB.Models.AgeRatingTitle> GRAC { get; set; }
                public List<IGDB.Models.AgeRatingTitle> PEGI { get; set; }
                public List<IGDB.Models.AgeRatingTitle> USK { get; set; }

                [JsonIgnore]
                [Newtonsoft.Json.JsonIgnore]
                public List<long> AgeGroupItemValues
                {
                    get
                    {
                        List<long> values = new List<long>();
                        {
                            foreach (AgeRatingTitle ageRatingTitle in ACB)
                            {
                                values.Add((long)ageRatingTitle);
                            }
                            foreach (AgeRatingTitle ageRatingTitle in CERO)
                            {
                                values.Add((long)ageRatingTitle);
                            }
                            foreach (AgeRatingTitle ageRatingTitle in CLASS_IND)
                            {
                                values.Add((long)ageRatingTitle);
                            }
                            foreach (AgeRatingTitle ageRatingTitle in ESRB)
                            {
                                values.Add((long)ageRatingTitle);
                            }
                            foreach (AgeRatingTitle ageRatingTitle in GRAC)
                            {
                                values.Add((long)ageRatingTitle);
                            }
                            foreach (AgeRatingTitle ageRatingTitle in PEGI)
                            {
                                values.Add((long)ageRatingTitle);
                            }
                            foreach (AgeRatingTitle ageRatingTitle in USK)
                            {
                                values.Add((long)ageRatingTitle);
                            }
                        }

                        return values;
                    }
                }
            }

            public class ClassificationBoardItem
            {
                public IGDB.Models.AgeRatingCategory Board { get; set; }
                public List<IGDB.Models.AgeRatingTitle> Classifications { get; set; }
            }
        }
	}
}

