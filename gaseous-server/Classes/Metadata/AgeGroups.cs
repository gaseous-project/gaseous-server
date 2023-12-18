using System;
using System.Reflection;
using System.Text.Json.Serialization;
using IGDB;
using IGDB.Models;
using Microsoft.CodeAnalysis.Classification;

namespace gaseous_server.Classes.Metadata
{
    public class AgeGroups
    {
        public AgeGroups()
        {

        }

        public static AgeGroup? GetAgeGroup(Game? game)
        {
            if (game == null)
            {
                return null;
            }
            else
            {
                Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
                cacheStatus = Storage.GetCacheStatus("AgeGroup", (long)game.Id);

                AgeGroup? RetVal = new AgeGroup();

                switch (cacheStatus)
                {
                    case Storage.CacheStatus.NotPresent:
                        RetVal = _GetAgeGroup(game);
                        Storage.NewCacheValue(RetVal, false);
                        break;

                    case Storage.CacheStatus.Expired:
                        RetVal = _GetAgeGroup(game);
                        Storage.NewCacheValue(RetVal, true);
                        break;

                    case Storage.CacheStatus.Current:
                        RetVal = Storage.GetCacheValue<AgeGroup>(RetVal, "Id", game.Id);
                        break;

                    default:
                        throw new Exception("How did you get here?");
                }
                
                return RetVal;
            }
        }

        public static AgeGroup? _GetAgeGroup(Game game)
        {
            // compile the maximum age group for the given game
            if (game != null)
            {
                if (game.AgeRatings != null)
                {
                    if (game.AgeRatings.Ids != null)
                    {
                        // collect ratings values from metadata
                        List<AgeRating> ageRatings = new List<AgeRating>();
                        foreach (long ratingId in game.AgeRatings.Ids)
                        {
                            AgeRating? rating = AgeRatings.GetAgeRatings(ratingId);
                            if (rating != null)
                            {
                                ageRatings.Add(rating);
                            }
                        }

                        // compile the ratings values into the ratings groups
                        AgeRestrictionGroupings highestAgeGroup = AgeRestrictionGroupings.Unclassified;
                        foreach (AgeRating ageRating in ageRatings)
                        {
                            foreach (KeyValuePair<AgeRestrictionGroupings, AgeGroupItem> ageGroupItem in AgeGroupingsFlat)
                            {
                                
                                PropertyInfo[] groupProps = typeof(AgeGroupItem).GetProperties();
                                foreach (PropertyInfo property in groupProps)
                                {
                                    if (RatingsBoards.Contains(property.Name))
                                    {
                                        List<AgeRatingTitle> ratingBoard = (List<AgeRatingTitle>)property.GetValue(ageGroupItem.Value);
                                        foreach (AgeRatingTitle ratingTitle in ratingBoard)
                                        {
                                            if (ageRating.Rating == ratingTitle)
                                            {
                                                if (highestAgeGroup < ageGroupItem.Key)
                                                {
                                                    highestAgeGroup = ageGroupItem.Key;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        
                        // return the compiled ratings group
                        AgeGroup ageGroup = new AgeGroup();
                        ageGroup.Id = game.Id;
                        ageGroup.GameId = game.Id;
                        if (highestAgeGroup == 0)
                        {
                            ageGroup.AgeGroupId = null;
                        }
                        else
                        {
                            ageGroup.AgeGroupId = highestAgeGroup;
                        }
                        
                        return ageGroup;
                    }
                    else
                    {
                        AgeGroup ageGroup = new AgeGroup();
                        ageGroup.Id = game.Id;
                        ageGroup.GameId = game.Id;
                        ageGroup.AgeGroupId = null;
                        
                        return ageGroup;
                    }
                }
                else
                {
                    AgeGroup ageGroup = new AgeGroup();
                    ageGroup.Id = game.Id;
                    ageGroup.GameId = game.Id;
                    ageGroup.AgeGroupId = null;
                    
                    return ageGroup;
                }
            }
            
            return null;
        }

        public class AgeGroup
        {
            public long? Id { get; set; }
            public long? GameId { get; set; }
            public AgeRestrictionGroupings? AgeGroupId { get; set; }
        }

        public static Dictionary<AgeRestrictionGroupings, List<AgeGroupItem>> AgeGroupings
        {
            get
            {
                return new Dictionary<AgeRestrictionGroupings, List<AgeGroupItem>>{
                    { 
                        AgeRestrictionGroupings.Adult, new List<AgeGroupItem>{ Adult_Item, Mature_Item, Teen_Item, Child_Item } 
                    },
                    {
                        AgeRestrictionGroupings.Mature, new List<AgeGroupItem>{ Mature_Item, Teen_Item, Child_Item }
                    },
                    {
                        AgeRestrictionGroupings.Teen, new List<AgeGroupItem>{ Teen_Item, Child_Item }
                    },
                    { 
                        AgeRestrictionGroupings.Child, new List<AgeGroupItem>{ Child_Item }
                    }
                };
            }
        }

        public static Dictionary<AgeRestrictionGroupings, AgeGroupItem> AgeGroupingsFlat
        {
            get
            {
                return new Dictionary<AgeRestrictionGroupings, AgeGroupItem>{
                    {
                        AgeRestrictionGroupings.Adult, Adult_Item
                    },
                    {
                        AgeRestrictionGroupings.Mature, Mature_Item
                    },
                    {
                        AgeRestrictionGroupings.Teen, Teen_Item
                    },
                    {
                        AgeRestrictionGroupings.Child, Child_Item
                    }
                };
            }
        }

        public enum AgeRestrictionGroupings
        {
            Adult = 4,
            Mature = 3,
            Teen = 2,
            Child = 1,
            Unclassified = 0
        }

        public static List<string> RatingsBoards
        {
            get
            {
                List<string> boards = new List<string>{
                    "ACB", "CERO", "CLASS_IND", "ESRB", "GRAC", "PEGI", "USK"
                };

                return boards;
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
    }
}