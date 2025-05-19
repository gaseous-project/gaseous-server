using System.Data;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using gaseous_server.Classes.Metadata;
using IGDB.Models;

namespace gaseous_server.Classes
{
    public class Filters
    {
        public enum FilterType
        {
            Platforms,
            Genres,
            GameModes,
            PlayerPerspectives,
            Themes,
            AgeGroupings
        }

        public static async Task<List<FilterItem>> GetFilter(FilterType filterType, Metadata.AgeGroups.AgeRestrictionGroupings MaximumAgeRestriction, bool IncludeUnrated)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = string.Empty;

            List<FilterItem> returnList = new List<FilterItem>();

            // age restriction clauses
            string ageRestriction_Platform = "AgeGroup.AgeGroupId <= " + (int)MaximumAgeRestriction;
            string ageRestriction_Generic = "view_Games.AgeGroupId <= " + (int)MaximumAgeRestriction;
            if (IncludeUnrated == true)
            {
                ageRestriction_Platform += " OR AgeGroup.AgeGroupId IS NULL";
                ageRestriction_Generic += " OR view_Games.AgeGroupId IS NULL";
            }

            switch (filterType)
            {
                case FilterType.Platforms:
                    sql = "SELECT Platform.Id, Platform.`Name`, COUNT(Game.Id) AS GameCount FROM (SELECT DISTINCT Game.Id, view_Games_Roms.PlatformId, COUNT(view_Games_Roms.Id) AS RomCount FROM Game LEFT JOIN AgeGroup ON Game.Id = AgeGroup.GameId LEFT JOIN view_Games_Roms ON Game.Id = view_Games_Roms.GameId WHERE (" + ageRestriction_Platform + ") GROUP BY Game.Id , view_Games_Roms.PlatformId HAVING RomCount > 0) Game JOIN Platform ON Game.PlatformId = Platform.Id AND Platform.SourceId = 0 GROUP BY Platform.`Name`;";

                    DataTable dbResponse = await db.ExecuteCMDAsync(sql, new Database.DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 300));

                    foreach (DataRow dr in dbResponse.Rows)
                    {
                        FilterItem item = new FilterItem(dr);
                        returnList.Add(item);
                    }

                    return returnList;

                case FilterType.AgeGroupings:
                    sql = "SELECT Game.AgeGroupId, COUNT(Game.Id) AS GameCount FROM (SELECT DISTINCT Game.Id, AgeGroup.AgeGroupId, COUNT(view_Games_Roms.Id) AS RomCount FROM Game LEFT JOIN AgeGroup ON Game.Id = AgeGroup.GameId LEFT JOIN view_Games_Roms ON Game.Id = view_Games_Roms.GameId WHERE (" + ageRestriction_Platform + ") GROUP BY Game.Id HAVING RomCount > 0) Game GROUP BY Game.AgeGroupId ORDER BY Game.AgeGroupId DESC";
                    dbResponse = await db.ExecuteCMDAsync(sql, new Database.DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 300));

                    foreach (DataRow dr in dbResponse.Rows)
                    {
                        FilterItem filterAgeGrouping = new FilterItem();
                        if (dr["AgeGroupId"] == DBNull.Value)
                        {
                            filterAgeGrouping.Id = (int)(long)AgeGroups.AgeRestrictionGroupings.Unclassified;
                            filterAgeGrouping.Name = AgeGroups.AgeRestrictionGroupings.Unclassified.ToString();
                        }
                        else
                        {
                            int ageGroupLong = (int)dr["AgeGroupId"];
                            AgeGroups.AgeRestrictionGroupings ageGroup = (AgeGroups.AgeRestrictionGroupings)ageGroupLong;
                            filterAgeGrouping.Id = ageGroupLong;
                            filterAgeGrouping.Name = ageGroup.ToString();
                        }
                        filterAgeGrouping.GameCount = (int)(long)dr["GameCount"];
                        returnList.Add(filterAgeGrouping);
                    }

                    return returnList;

                case FilterType.Genres:
                    List<FilterItem> genres = await GenerateFilterSet(db, "Genre", ageRestriction_Platform);
                    return genres;

                case FilterType.GameModes:
                    List<FilterItem> gameModes = await GenerateFilterSet(db, "GameMode", ageRestriction_Platform);
                    return gameModes;

                case FilterType.PlayerPerspectives:
                    List<FilterItem> playerPerspectives = await GenerateFilterSet(db, "PlayerPerspective", ageRestriction_Platform);
                    return playerPerspectives;

                case FilterType.Themes:
                    List<FilterItem> themes = await GenerateFilterSet(db, "Theme", ageRestriction_Platform);
                    return themes;

                default:
                    // invalid filter type
                    returnList = new List<FilterItem>();
                    FilterItem invalidFilter = new FilterItem();
                    invalidFilter.Name = "Invalid Filter Type";
                    invalidFilter.GameCount = 0;
                    invalidFilter.Id = 0;
                    returnList.Add(invalidFilter);

                    return returnList;
            }
        }

        public static async Task<Dictionary<string, List<FilterItem>>> Filter(Metadata.AgeGroups.AgeRestrictionGroupings MaximumAgeRestriction, bool IncludeUnrated)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            Dictionary<string, List<FilterItem>> FilterSet = new Dictionary<string, List<FilterItem>>();

            // platforms
            List<FilterItem> platforms = new List<FilterItem>();

            string ageRestriction_Platform = "AgeGroup.AgeGroupId <= " + (int)MaximumAgeRestriction;
            string ageRestriction_Generic = "view_Games.AgeGroupId <= " + (int)MaximumAgeRestriction;
            if (IncludeUnrated == true)
            {
                ageRestriction_Platform += " OR AgeGroup.AgeGroupId IS NULL";
                ageRestriction_Generic += " OR view_Games.AgeGroupId IS NULL";
            }

            string sql = "SELECT Platform.Id, Platform.`Name`, COUNT(Game.Id) AS GameCount FROM (SELECT DISTINCT Game.Id, view_Games_Roms.PlatformId, COUNT(view_Games_Roms.Id) AS RomCount FROM Game LEFT JOIN AgeGroup ON Game.Id = AgeGroup.GameId LEFT JOIN view_Games_Roms ON Game.Id = view_Games_Roms.GameId WHERE (" + ageRestriction_Platform + ") GROUP BY Game.Id , view_Games_Roms.PlatformId HAVING RomCount > 0) Game JOIN Platform ON Game.PlatformId = Platform.Id AND Platform.SourceId = 0 GROUP BY Platform.`Name`;";

            DataTable dbResponse = await db.ExecuteCMDAsync(sql, new Database.DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 300));

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterItem platformItem = new FilterItem(dr);
                platforms.Add(platformItem);

            }
            FilterSet.Add("platforms", platforms);

            // genres
            List<FilterItem> genres = await GenerateFilterSet(db, "Genre", ageRestriction_Platform);
            FilterSet.Add("genres", genres);

            // game modes
            List<FilterItem> gameModes = await GenerateFilterSet(db, "GameMode", ageRestriction_Platform);
            FilterSet.Add("gamemodes", gameModes);

            // player perspectives
            List<FilterItem> playerPerspectives = await GenerateFilterSet(db, "PlayerPerspective", ageRestriction_Platform);
            FilterSet.Add("playerperspectives", playerPerspectives);

            // themes
            List<FilterItem> themes = await GenerateFilterSet(db, "Theme", ageRestriction_Platform);
            FilterSet.Add("themes", themes);

            // age groups
            List<FilterItem> agegroupings = new List<FilterItem>();
            sql = "SELECT Game.AgeGroupId, COUNT(Game.Id) AS GameCount FROM (SELECT DISTINCT Game.Id, AgeGroup.AgeGroupId, COUNT(view_Games_Roms.Id) AS RomCount FROM Game LEFT JOIN AgeGroup ON Game.Id = AgeGroup.GameId LEFT JOIN view_Games_Roms ON Game.Id = view_Games_Roms.GameId WHERE (" + ageRestriction_Platform + ") GROUP BY Game.Id HAVING RomCount > 0) Game GROUP BY Game.AgeGroupId ORDER BY Game.AgeGroupId DESC";
            dbResponse = await db.ExecuteCMDAsync(sql, new Database.DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 300));

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterItem filterAgeGrouping = new FilterItem();
                if (dr["AgeGroupId"] == DBNull.Value)
                {
                    filterAgeGrouping.Id = (int)(long)AgeGroups.AgeRestrictionGroupings.Unclassified;
                    filterAgeGrouping.Name = AgeGroups.AgeRestrictionGroupings.Unclassified.ToString();
                }
                else
                {
                    int ageGroupLong = (int)dr["AgeGroupId"];
                    AgeGroups.AgeRestrictionGroupings ageGroup = (AgeGroups.AgeRestrictionGroupings)ageGroupLong;
                    filterAgeGrouping.Id = ageGroupLong;
                    filterAgeGrouping.Name = ageGroup.ToString();
                }
                filterAgeGrouping.GameCount = (int)(long)dr["GameCount"];
                agegroupings.Add(filterAgeGrouping);
            }
            FilterSet.Add("agegroupings", agegroupings);

            return FilterSet;
        }

        private static async Task<List<FilterItem>> GenerateFilterSet(Database db, string Name, string AgeRestriction)
        {
            List<FilterItem> filter = new List<FilterItem>();
            DataTable dbResponse = await GetGenericFilterItem(db, Name, AgeRestriction);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterItem filterItem = new FilterItem(dr);
                if (filterItem != null)
                {
                    bool nameExists = false;
                    foreach (var filterObject in filter)
                    {
                        if (filterObject.Name == filterItem.Name)
                        {
                            // add the ids to the existing genre
                            if (filterObject.Ids == null)
                            {
                                filterObject.Ids = new Dictionary<HasheousClient.Hasheous.MetadataProvider, long>();
                            }

                            foreach (var id in filterItem.Ids)
                            {
                                if (filterObject.Ids.ContainsKey(id.Key) == false)
                                {
                                    filterObject.Ids.Add(id.Key, id.Value);
                                    filterObject.GameCount += filterItem.GameCount;
                                }
                            }

                            nameExists = true;
                        }
                    }

                    if (nameExists == false)
                    {
                        filter.Add(filterItem);
                    }
                }
            }

            return filter;
        }

        private static async Task<DataTable> GetGenericFilterItem(Database db, string Name, string AgeRestriction)
        {
            string sql = "SELECT Game.GameIdType, <ITEMNAME>.Id, <ITEMNAME>.`Name`, COUNT(Game.Id) AS GameCount FROM (SELECT DISTINCT view_Games_Roms.GameIdType, Game.Id, AgeGroup.AgeGroupId, COUNT(view_Games_Roms.Id) AS RomCount FROM Game LEFT JOIN AgeGroup ON Game.Id = AgeGroup.GameId LEFT JOIN view_Games_Roms ON Game.Id = view_Games_Roms.GameId WHERE (" + AgeRestriction + ") GROUP BY Game.Id HAVING RomCount > 0) Game JOIN Relation_Game_<ITEMNAME>s ON Game.Id = Relation_Game_<ITEMNAME>s.GameId AND Game.GameIdType = Relation_Game_<ITEMNAME>s.GameSourceId JOIN <ITEMNAME> ON Relation_Game_<ITEMNAME>s.<ITEMNAME>sId = <ITEMNAME>.Id GROUP BY GameIdType, <ITEMNAME>.`Name` ORDER BY <ITEMNAME>.`Name`;";
            sql = sql.Replace("<ITEMNAME>", Name);
            DataTable dbResponse = await db.ExecuteCMDAsync(sql, new Database.DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 300));

            return dbResponse;
        }

        public class FilterItem
        {
            public FilterItem()
            {

            }

            public FilterItem(DataRow dr)
            {
                if (dr.Table.Columns.Contains("GameIdType"))
                {
                    HasheousClient.Hasheous.MetadataProvider SourceId = (HasheousClient.Hasheous.MetadataProvider)Enum.Parse(typeof(HasheousClient.Hasheous.MetadataProvider), dr["GameIdType"].ToString());

                    if (this.Ids == null)
                    {
                        this.Ids = new Dictionary<HasheousClient.Hasheous.MetadataProvider, long>();
                    }

                    if (this.Ids.ContainsKey(SourceId) == false)
                    {
                        this.Ids.Add(SourceId, (long)dr["Id"]);
                    }
                }
                else
                {
                    this.Id = (long)dr["Id"];
                }

                this.Name = (string)dr["Name"];
                this.GameCount = (int)(long)dr["GameCount"];
            }

            public long? Id { get; set; }

            public Dictionary<HasheousClient.Hasheous.MetadataProvider, long>? Ids { get; set; }

            public string Name { get; set; }

            public int GameCount { get; set; }
        }
    }
}