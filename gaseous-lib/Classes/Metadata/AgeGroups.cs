using System;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using gaseous_server.Models;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using Microsoft.CodeAnalysis.Classification;
using gaseous_server.Classes.Plugins.MetadataProviders;
using System.Data;

namespace gaseous_server.Classes.Metadata
{
    public class AgeGroups
    {
        public AgeGroups()
        {

        }

        /// <summary>
        /// Gets the source-agnostic age group for a metadata map. Request paths only read the stored value;
        /// if none has been calculated yet it is derived once from locally stored metadata (never downloaded).
        /// </summary>
        /// <returns>The age group, or null when the map is unrated or nothing is stored locally.</returns>
        public static async Task<AgeRestrictionGroupings?> GetMapAgeGroupAsync(long metadataMapId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            DataTable dt = await db.ExecuteCMDAsync(
                "SELECT AgeGroupId FROM MetadataMapAgeGroup WHERE MetadataMapId = @id;",
                new Dictionary<string, object> { { "id", metadataMapId } },
                new DatabaseMemoryCacheOptions(CacheEnabled: false));
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0]["AgeGroupId"] == DBNull.Value ? null : (AgeRestrictionGroupings)Convert.ToInt32(dt.Rows[0]["AgeGroupId"]);
            }

            return await RefreshMapAgeGroupAsync(metadataMapId);
        }

        /// <summary>
        /// Recalculates and stores the age group of a metadata map as the highest age group across the age ratings
        /// of every source attached to it. Only locally stored metadata is used. Intended for the metadata refresh process.
        /// </summary>
        public static async Task<AgeRestrictionGroupings?> RefreshMapAgeGroupAsync(long metadataMapId)
        {
            MetadataMap? map = await MetadataManagement.GetMetadataMap(metadataMapId);
            if (map?.MetadataMapItems == null)
            {
                return null;
            }

            bool foundLocalGame = false;
            bool complete = true;
            List<AgeRating> ageRatings = new List<AgeRating>();
            foreach (MetadataMap.MetadataMapItem item in map.MetadataMapItems)
            {
                if (item.SourceId == null || item.SourceId <= 0)
                {
                    continue;
                }

                // sources without a configured provider can never supply local metadata
                if (!Metadata.MetadataProviders.Any(p => p.SourceType == item.SourceType))
                {
                    continue;
                }

                Storage storage = new Storage(item.SourceType);
                if (await storage.GetCacheStatusAsync("Game", (long)item.SourceId) == Storage.CacheStatus.NotPresent)
                {
                    complete = false;
                    continue;
                }

                Game? game = await storage.GetCacheValue<Game>(new Game(), "Id", (long)item.SourceId);
                if (game == null)
                {
                    complete = false;
                    continue;
                }
                foundLocalGame = true;

                if (game.AgeRatings == null)
                {
                    continue;
                }
                foreach (long ratingId in game.AgeRatings)
                {
                    if (ratingId <= 0)
                    {
                        continue;
                    }
                    if (await storage.GetCacheStatusAsync("AgeRating", ratingId) == Storage.CacheStatus.NotPresent)
                    {
                        complete = false;
                        continue;
                    }
                    AgeRating? rating = await storage.GetCacheValue<AgeRating>(new AgeRating(), "Id", ratingId);
                    if (rating != null)
                    {
                        ageRatings.Add(rating);
                    }
                    else
                    {
                        complete = false;
                    }
                }
            }

            // Missing source metadata must not be cached as a final, potentially lower classification.
            if (!foundLocalGame || !complete)
            {
                return null;
            }

            AgeRestrictionGroupings highest = GetAgeGroupFromAgeRatings(ageRatings);
            AgeRestrictionGroupings? result = highest == AgeRestrictionGroupings.Unclassified ? null : highest;

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            await db.ExecuteCMDAsync(
                @"INSERT INTO MetadataMapAgeGroup (MetadataMapId, AgeGroupId) VALUES (@id, @age)
                  ON DUPLICATE KEY UPDATE AgeGroupId = @age, lastUpdated = CURRENT_TIMESTAMP;",
                new Dictionary<string, object> { { "id", metadataMapId }, { "age", result == null ? DBNull.Value : (int)result } },
                new DatabaseMemoryCacheOptions(CacheEnabled: false));

            return result;
        }

        /// <summary>
        /// Determines the highest age restriction grouping from a list of age ratings.
        /// </summary>
        /// <param name="ageRatings">The list of age ratings to evaluate.</param>
        /// <returns>The highest <see cref="AgeRestrictionGroupings"/> found in the ratings.</returns>
        public static AgeRestrictionGroupings GetAgeGroupFromAgeRatings(List<AgeRating> ageRatings)
        {
            AgeRestrictionGroupings highestAgeGroup = AgeRestrictionGroupings.Unclassified;

            foreach (AgeRating ageRating in ageRatings)
            {
                var (ratingsBoardName, boardRatingName) = GetBoardAndRatingNames(ageRating);

                if (ratingsBoardName != null && boardRatingName != null)
                {
                    var group = GetMatchingAgeGroup(ratingsBoardName, boardRatingName);
                    if (group.HasValue && group.Value > highestAgeGroup)
                    {
                        highestAgeGroup = group.Value;
                    }
                }
            }

            return highestAgeGroup;
        }

        private static (string? ratingsBoardName, string? boardRatingName) GetBoardAndRatingNames(AgeRating ageRating)
        {
            long ratingBoard = ageRating.Organization;
            long ratingValue = ageRating.RatingCategory;

            var boardEntry = AgeGroupMap.RatingBoards
                .FirstOrDefault(b => b.Value.IGDBId == ratingBoard);

            if (!string.IsNullOrEmpty(boardEntry.Key))
            {
                string ratingsBoardName = boardEntry.Key;
                var ratingEntry = boardEntry.Value.Ratings
                    .FirstOrDefault(r => r.Value.IGDBId == ratingValue);

                if (!string.IsNullOrEmpty(ratingEntry.Key))
                {
                    string boardRatingName = ratingEntry.Key;
                    return (ratingsBoardName, boardRatingName);
                }
            }
            return (null, null);
        }

        private static AgeRestrictionGroupings? GetMatchingAgeGroup(string ratingsBoardName, string boardRatingName)
        {
            foreach (var ageGroup in AgeGroupMap.AgeGroups)
            {
                if (ageGroup.Value.Ratings.ContainsKey(ratingsBoardName) &&
                    ageGroup.Value.Ratings[ratingsBoardName].Contains(boardRatingName))
                {
                    return ageGroup.Key;
                }
            }
            return null;
        }

        public static Dictionary<string, object> GetAgeRatingAndGroupings()
        {
            // get age ratings dictionary
            Dictionary<long, string> ClassificationBoardsStrings = new Dictionary<long, string>();
            Dictionary<long, string> AgeRatingsStrings = new Dictionary<long, string>();
            foreach (var ratingBoardKey in AgeGroups.AgeGroupMap.RatingBoards.Keys)
            {
                ClassificationBoardsStrings.Add(
                    (long)AgeGroups.AgeGroupMap.RatingBoards[ratingBoardKey].IGDBId,
                    ratingBoardKey
                    );

                foreach (var ageRatingKey in AgeGroups.AgeGroupMap.RatingBoards[ratingBoardKey].Ratings.Keys)
                {
                    AgeRatingsStrings.Add(
                        (long)AgeGroups.AgeGroupMap.RatingBoards[ratingBoardKey].Ratings[ageRatingKey].IGDBId,
                        AgeGroups.AgeGroupMap.RatingBoards[ratingBoardKey].Ratings[ageRatingKey].IGDBName
                        );
                }
            }

            Dictionary<string, Dictionary<string, List<long>>> AgeGroupsStrings = new Dictionary<string, Dictionary<string, List<long>>>();
            foreach (var ageGroupKey in AgeGroups.AgeGroupMap.AgeGroups.Keys)
            {
                Dictionary<string, List<long>> ageGroupRatings = new Dictionary<string, List<long>>();
                foreach (var ratingBoardKey in AgeGroups.AgeGroupMap.AgeGroups[ageGroupKey].Ratings.Keys)
                {
                    List<long> ageRatingIds = new List<long>();
                    foreach (var ageRatingItem in AgeGroups.AgeGroupMap.AgeGroups[ageGroupKey].Ratings[ratingBoardKey])
                    {
                        if (AgeGroups.AgeGroupMap.RatingBoards.ContainsKey(ratingBoardKey))
                        {
                            if (AgeGroups.AgeGroupMap.RatingBoards[ratingBoardKey].Ratings.ContainsKey(ageRatingItem))
                            {
                                ageRatingIds.Add((long)AgeGroups.AgeGroupMap.RatingBoards[ratingBoardKey].Ratings[ageRatingItem].IGDBId);
                            }
                        }
                    }

                    if (ageRatingIds.Count > 0)
                    {
                        ageGroupRatings.Add(ratingBoardKey, ageRatingIds);
                    }
                }

                if (ageGroupRatings.Count > 0)
                {
                    AgeGroupsStrings.Add(ageGroupKey.ToString(), ageGroupRatings);
                }
            }

            // create the final dictionary
            Dictionary<string, object> ageRatingAndGroupings = new Dictionary<string, object>
            {
                { "ClassificationBoards", ClassificationBoardsStrings },
                { "AgeRatings", AgeRatingsStrings },
                { "AgeGroups", AgeGroupsStrings }
            };

            return ageRatingAndGroupings;
        }

        /// <summary>
        /// Represents an age group for a game, including its ID, associated game ID, and age restriction grouping.
        /// </summary>
        public class AgeGroup
        {
            /// <summary>
            /// Gets or sets the unique identifier for the age group.
            /// </summary>
            public long? Id { get; set; }
            /// <summary>
            /// Gets or sets the unique identifier for the associated game.
            /// </summary>
            public long? GameId { get; set; }
            /// <summary>
            /// Gets or sets the age restriction grouping for the age group.
            /// </summary>
            public AgeRestrictionGroupings? AgeGroupId { get; set; }
        }

        /// <summary>
        /// Represents the possible age restriction groupings for games.
        /// </summary>
        public enum AgeRestrictionGroupings
        {
            /// <summary>
            /// Represents games suitable only for adults.
            /// </summary>
            Adult = 4,
            /// <summary>
            /// Represents games suitable for mature audiences.
            /// </summary>
            Mature = 3,
            /// <summary>
            /// Represents games suitable for teenagers.
            /// </summary>
            Teen = 2,
            /// <summary>
            /// Represents games suitable for children.
            /// </summary>
            Child = 1,
            /// <summary>
            /// Represents games that are unclassified.
            /// </summary>
            Unclassified = 0
        }

        private static AgeGroupMapModel? _ageGroupMap { get; set; } = null;
        /// <summary>
        /// Gets the age group map model, loading from file or embedded resource if necessary.
        /// </summary>
        public static AgeGroupMapModel AgeGroupMap
        {
            get
            {
                // if _ageGroupMap is null:
                // - check if a file named "AgeGroupMap.json" exists at "~/Metadata/AgeGroupMap.json"
                // - if it exists, read the file and deserialize it into _ageGroupMap
                // - if it does not exist, read AgeGroupMap.json from the embedded resources and deserialize it into _ageGroupMap
                if (_ageGroupMap == null)
                {
                    string filePath = Path.Combine(Config.LibraryConfiguration.LibraryRootDirectory, "AgeGroupMap.json");
                    if (File.Exists(filePath))
                    {
                        string json = File.ReadAllText(filePath);
                        _ageGroupMap = Newtonsoft.Json.JsonConvert.DeserializeObject<AgeGroupMapModel>(json);
                    }
                    else
                    {
                        // using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("gaseous_server.wwwroot.images.Ratings.AgeGroupMap.json");
                        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("gaseous_lib.Support.AgeGroupMap.json");
                        if (stream != null)
                        {
                            using StreamReader reader = new StreamReader(stream);
                            string json = reader.ReadToEnd();
                            _ageGroupMap = Newtonsoft.Json.JsonConvert.DeserializeObject<AgeGroupMapModel>(json, new Newtonsoft.Json.JsonSerializerSettings
                            {
                                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
                                MaxDepth = 10
                            });
                        }
                        else
                        {
                            // Could not find AgeGroupMap.json in embedded resources; handle gracefully
                            _ageGroupMap = new AgeGroupMapModel();
                        }
                    }
                }

                return _ageGroupMap;
            }
        }

        /// <summary>
        /// Represents the mapping model for age groups, including group definitions and ratings boards.
        /// </summary>
        public class AgeGroupMapModel
        {
            /// <summary>
            /// Gets or sets the dictionary mapping age restriction groupings to their corresponding age group models.
            /// </summary>
            public Dictionary<AgeRestrictionGroupings, AgeGroupsModel> AgeGroups { get; set; } = new Dictionary<AgeRestrictionGroupings, AgeGroupsModel>();

            /// <summary>
            /// Gets or sets the dictionary of ratings boards, keyed by their names.
            /// </summary>
            public Dictionary<string, RatingBoardModel> RatingBoards { get; set; } = new Dictionary<string, RatingBoardModel>();

            /// <summary>
            /// Represents an age group model containing its ID and associated ratings.
            /// </summary>
            public class AgeGroupsModel
            {
                /// <summary>
                /// Gets or sets the unique identifier for the age group.
                /// </summary>
                public long Id { get; set; }

                /// <summary>
                /// Gets or sets the ratings associated with the age group, organized by ratings board.
                /// </summary>
                public Dictionary<string, List<string>> Ratings { get; set; } = new Dictionary<string, List<string>>();
            }

            /// <summary>
            /// Represents a ratings board model containing ratings boards and their associated items.
            /// </summary>
            public class RatingBoardModel
            {
                /// <summary>
                /// Gets or sets the IGDB identifier for the ratings board item.
                /// </summary>
                public long? IGDBId { get; set; }
                /// <summary>
                /// Gets or sets the name of the ratings board item.
                /// </summary>
                public string? Name { get; set; }
                /// <summary>
                /// Gets or sets the short name of the ratings board item.
                /// </summary>
                public string? ShortName { get; set; }
                /// <summary>
                /// Gets or sets the description of the ratings board item.
                /// </summary>
                public string? Description { get; set; }
                /// <summary>
                /// Gets or sets the website URL for the ratings board item.
                /// </summary>
                public string? Website { get; set; }
                /// <summary>
                /// Gets or sets the dictionary of ratings for this ratings board item, keyed by rating name.
                /// </summary>
                public Dictionary<string, RatingsItemModel> Ratings { get; set; } = new Dictionary<string, RatingsItemModel>();

                /// <summary>
                /// Represents a rating item within a ratings board, including its IGDB ID, name, description, and icon name.
                /// </summary>
                public class RatingsItemModel
                {
                    /// <summary>
                    /// Gets or sets the IGDB identifier for the rating item.
                    /// </summary>
                    public long? IGDBId { get; set; }
                    /// <summary>
                    /// Gets or sets the IGDB name for the rating item.
                    /// </summary>
                    public string? IGDBName { get; set; }
                    /// <summary>
                    /// Gets or sets the name of the rating item.
                    /// </summary>
                    public string? Name { get; set; }
                    /// <summary>
                    /// Gets or sets the description of the rating item.
                    /// </summary>
                    public string? Description { get; set; }
                    /// <summary>
                    /// Gets or sets the icon name associated with the rating item.
                    /// </summary>
                    public string? IconName { get; set; }
                }
            }
        }
    }
}