using System;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using gaseous_server.Models;
using HasheousClient.Models.Metadata.IGDB;
using Microsoft.CodeAnalysis.Classification;

namespace gaseous_server.Classes.Metadata
{
    public class AgeGroups
    {
        public AgeGroups()
        {

        }

        public static async Task<AgeGroup?> GetAgeGroup(Models.Game? game)
        {
            if (game == null)
            {
                return null;
            }
            else
            {
                Storage.CacheStatus? cacheStatus = await Storage.GetCacheStatusAsync(HasheousClient.Models.MetadataSources.IGDB, "AgeGroup", (long)game.Id);

                AgeGroup? RetVal = new AgeGroup();

                switch (cacheStatus)
                {
                    case Storage.CacheStatus.NotPresent:
                        RetVal = await _GetAgeGroup(game);
                        await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.IGDB, RetVal, false);
                        break;

                    case Storage.CacheStatus.Expired:
                        RetVal = await _GetAgeGroup(game);
                        await Storage.NewCacheValue(HasheousClient.Models.MetadataSources.IGDB, RetVal, true);
                        break;

                    case Storage.CacheStatus.Current:
                        RetVal = await Storage.GetCacheValue<AgeGroup>(HasheousClient.Models.MetadataSources.IGDB, RetVal, "Id", game.Id);
                        break;

                    default:
                        throw new InvalidOperationException("Unexpected cache status encountered in GetAgeGroup.");
                }

                return RetVal;
            }
        }

        public static async Task<AgeGroup?> _GetAgeGroup(Models.Game game)
        {
            // compile the maximum age group for the given game
            if (game != null)
            {
                if (game.AgeRatings != null)
                {
                    if (game.AgeRatings != null)
                    {
                        // collect ratings values from metadata
                        List<AgeRating> ageRatings = new List<AgeRating>();
                        foreach (long ratingId in game.AgeRatings)
                        {
                            AgeRating? rating = await AgeRatings.GetAgeRating(game.MetadataSource, ratingId);
                            if (rating != null)
                            {
                                ageRatings.Add(rating);
                            }
                        }

                        // compile the ratings values into the ratings groups
                        AgeRestrictionGroupings highestAgeGroup = GetAgeGroupFromAgeRatings(ageRatings);

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

            var boardEntry = AgeGroupMap.RatingsBoards
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

        private static AgeGroupMapModel? _ageGroupMap;
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
                        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("gaseous_server.Metadata.AgeGroupMap.json");
                        if (stream != null)
                        {
                            using StreamReader reader = new StreamReader(stream);
                            string json = reader.ReadToEnd();
                            _ageGroupMap = Newtonsoft.Json.JsonConvert.DeserializeObject<AgeGroupMapModel>(json);
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
            public Dictionary<string, RatingsBoardModel> RatingsBoards { get; set; } = new Dictionary<string, RatingsBoardModel>();

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
            public class RatingsBoardModel
            {
                /// <summary>
                /// Gets or sets the IGDB identifier for the ratings board item.
                /// </summary>
                public long IGDBId { get; set; }
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
                    public long IGDBId { get; set; }
                    /// <summary>
                    /// Gets or sets the name of the rating item.
                    /// </summary>
                    public string Name { get; set; }
                    /// <summary>
                    /// Gets or sets the description of the rating item.
                    /// </summary>
                    public string Description { get; set; }
                    /// <summary>
                    /// Gets or sets the icon name associated with the rating item.
                    /// </summary>
                    public string IconName { get; set; }
                }
            }
        }
    }
}