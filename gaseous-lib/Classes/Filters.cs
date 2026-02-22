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
            string ageRestriction_Platform = "ag.AgeGroupId <= " + (int)MaximumAgeRestriction;
            string ageRestriction_Generic = "view_Games.AgeGroupId <= " + (int)MaximumAgeRestriction;
            if (IncludeUnrated == true)
            {
                ageRestriction_Platform += " OR ag.AgeGroupId IS NULL";
                ageRestriction_Generic += " OR view_Games.AgeGroupId IS NULL";
            }

            switch (filterType)
            {
                case FilterType.Platforms:
                    // Optimized query: use CTE with view_MetadataMap
                    sql = @"
                        WITH FilteredGames AS (
                            SELECT DISTINCT 
                                g.Id,
                                g.SourceId AS GameIdType
                            FROM Metadata_Game g
                            LEFT JOIN Metadata_AgeGroup ag ON g.Id = ag.GameId
                            WHERE (" + ageRestriction_Platform + @")
                        ),
                        GamesWithRoms AS (
                            SELECT DISTINCT
                                fg.Id,
                                gr.PlatformId
                            FROM FilteredGames fg
                            INNER JOIN view_MetadataMap vmm 
                                ON vmm.MetadataSourceId = fg.Id 
                                AND vmm.MetadataSourceType = fg.GameIdType
                            INNER JOIN Games_Roms gr ON gr.MetadataMapId = vmm.Id
                        )
                        SELECT 
                            p.Id, 
                            p.Name, 
                            COUNT(DISTINCT gwr.Id) AS GameCount
                        FROM GamesWithRoms gwr
                        INNER JOIN Metadata_Platform p 
                            ON gwr.PlatformId = p.Id 
                            AND p.SourceId = 0
                        GROUP BY p.Id, p.Name
                        ORDER BY p.Name";

                    DataTable dbResponse = await db.ExecuteCMDAsync(sql, new DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 300));
                    
                    foreach (DataRow dr in dbResponse.Rows)
                    {
                        FilterItem item = new FilterItem(dr);
                        returnList.Add(item);
                    }

                    return returnList;

                case FilterType.AgeGroupings:
                    // Optimized query: use CTE with view_MetadataMap
                    sql = @"
                        WITH FilteredGames AS (
                            SELECT DISTINCT 
                                g.Id,
                                g.SourceId AS GameIdType
                            FROM Metadata_Game g
                            LEFT JOIN Metadata_AgeGroup ag ON g.Id = ag.GameId
                            WHERE (" + ageRestriction_Platform + @")
                        ),
                        GamesWithRoms AS (
                            SELECT DISTINCT
                                fg.Id,
                                ag.AgeGroupId
                            FROM FilteredGames fg
                            INNER JOIN view_MetadataMap vmm 
                                ON vmm.MetadataSourceId = fg.Id 
                                AND vmm.MetadataSourceType = fg.GameIdType
                            INNER JOIN Games_Roms gr ON gr.MetadataMapId = vmm.Id
                            LEFT JOIN Metadata_AgeGroup ag ON fg.Id = ag.GameId
                        )
                        SELECT 
                            AgeGroupId, 
                            COUNT(DISTINCT Id) AS GameCount
                        FROM GamesWithRoms
                        GROUP BY AgeGroupId
                        ORDER BY AgeGroupId DESC";
                    dbResponse = await db.ExecuteCMDAsync(sql, new DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 300));
                    
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

            // Build age restriction clause once
            string ageRestriction_Platform = "ag.AgeGroupId <= " + (int)MaximumAgeRestriction;
            if (IncludeUnrated == true)
            {
                ageRestriction_Platform += " OR ag.AgeGroupId IS NULL";
            }

            // OPTIMIZED: Compute the filtered game set once using a base query
            // This replaces 6 independent expensive subqueries with 1 base query + 6 lightweight joins
            string baseFilteredGamesQuery = @"
                CREATE TEMPORARY TABLE IF NOT EXISTS temp_FilteredGamesWithRoms (
                    GameId BIGINT,
                    GameIdType INT,
                    PlatformId BIGINT,
                    AgeGroupId INT,
                    INDEX idx_gameid_type (GameId, GameIdType),
                    INDEX idx_platformid (PlatformId),
                    INDEX idx_agegroupid (AgeGroupId)
                ) ENGINE=MEMORY;
                
                TRUNCATE TABLE temp_FilteredGamesWithRoms;
                
                INSERT INTO temp_FilteredGamesWithRoms
                SELECT DISTINCT
                    g.Id AS GameId,
                    g.SourceId AS GameIdType,
                    gr.PlatformId,
                    ag.AgeGroupId
                FROM Metadata_Game g
                LEFT JOIN Metadata_AgeGroup ag ON g.Id = ag.GameId
                INNER JOIN view_MetadataMap vmm 
                    ON vmm.MetadataSourceId = g.Id 
                    AND vmm.MetadataSourceType = g.SourceId
                INNER JOIN Games_Roms gr ON gr.MetadataMapId = vmm.Id
                WHERE (" + ageRestriction_Platform + @")";

            await db.ExecuteCMDAsync(baseFilteredGamesQuery, new DatabaseMemoryCacheOptions(CacheEnabled: false));

            // Now run lightweight queries against the temp table
            
            // platforms
            List<FilterItem> platforms = new List<FilterItem>();
            string sql = @"
                SELECT 
                    p.Id, 
                    p.Name, 
                    COUNT(DISTINCT fg.GameId) AS GameCount
                FROM temp_FilteredGamesWithRoms fg
                INNER JOIN Metadata_Platform p 
                    ON fg.PlatformId = p.Id 
                    AND p.SourceId = 0
                GROUP BY p.Id, p.Name
                ORDER BY p.Name";

            DataTable dbResponse = await db.ExecuteCMDAsync(sql, new DatabaseMemoryCacheOptions(CacheEnabled: false));
            
            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterItem platformItem = new FilterItem(dr);
                platforms.Add(platformItem);
            }
            FilterSet.Add("platforms", platforms);

            // genres
            List<FilterItem> genres = await GenerateFilterSetFromTemp(db, "Genre");
            FilterSet.Add("genres", genres);

            // game modes
            List<FilterItem> gameModes = await GenerateFilterSetFromTemp(db, "GameMode");
            FilterSet.Add("gamemodes", gameModes);

            // player perspectives
            List<FilterItem> playerPerspectives = await GenerateFilterSetFromTemp(db, "PlayerPerspective");
            FilterSet.Add("playerperspectives", playerPerspectives);

            // themes
            List<FilterItem> themes = await GenerateFilterSetFromTemp(db, "Theme");
            FilterSet.Add("themes", themes);

            // age groups
            List<FilterItem> agegroupings = new List<FilterItem>();
            sql = @"
                SELECT 
                    AgeGroupId, 
                    COUNT(DISTINCT GameId) AS GameCount
                FROM temp_FilteredGamesWithRoms
                GROUP BY AgeGroupId
                ORDER BY AgeGroupId DESC";
            dbResponse = await db.ExecuteCMDAsync(sql, new DatabaseMemoryCacheOptions(CacheEnabled: false));
            
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

            // Clean up temp table
            await db.ExecuteCMDAsync("DROP TEMPORARY TABLE IF EXISTS temp_FilteredGamesWithRoms", new DatabaseMemoryCacheOptions(CacheEnabled: false));

            return FilterSet;
        }

        private static async Task<List<FilterItem>> GenerateFilterSetFromTemp(Database db, string Name)
        {
            List<FilterItem> filter = new List<FilterItem>();
            DataTable dbResponse = await GetGenericFilterItemFromTemp(db, Name);

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
                                filterObject.Ids = new Dictionary<HasheousClient.Models.MetadataSources, long>();
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

        private static async Task<DataTable> GetGenericFilterItemFromTemp(Database db, string Name)
        {
            // Lightweight query against the pre-computed temp table
            string sql = @"
                SELECT 
                    fg.GameIdType, 
                    item.Id, 
                    item.Name, 
                    COUNT(DISTINCT fg.GameId) AS GameCount
                FROM temp_FilteredGamesWithRoms fg
                INNER JOIN Relation_Game_<ITEMNAME>s rel
                    ON fg.GameId = rel.GameId 
                    AND fg.GameIdType = rel.GameSourceId
                INNER JOIN Metadata_<ITEMNAME> item 
                    ON rel.<ITEMNAME>sId = item.Id
                GROUP BY fg.GameIdType, item.Id, item.Name
                ORDER BY item.Name";
            sql = sql.Replace("<ITEMNAME>", Name);
            DataTable dbResponse = await db.ExecuteCMDAsync(sql, new DatabaseMemoryCacheOptions(CacheEnabled: false));
            
            return dbResponse;
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
                                filterObject.Ids = new Dictionary<HasheousClient.Models.MetadataSources, long>();
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
            // Optimized query: use CTE with view_MetadataMap
            string sql = @"
                WITH FilteredGames AS (
                    SELECT DISTINCT 
                        g.Id,
                        g.SourceId AS GameIdType
                    FROM Metadata_Game g
                    LEFT JOIN Metadata_AgeGroup ag ON g.Id = ag.GameId
                    WHERE (" + AgeRestriction + @")
                ),
                GamesWithRoms AS (
                    SELECT DISTINCT
                        fg.Id,
                        fg.GameIdType
                    FROM FilteredGames fg
                    INNER JOIN view_MetadataMap vmm 
                        ON vmm.MetadataSourceId = fg.Id 
                        AND vmm.MetadataSourceType = fg.GameIdType
                    INNER JOIN Games_Roms gr ON gr.MetadataMapId = vmm.Id
                )
                SELECT 
                    gwr.GameIdType, 
                    item.Id, 
                    item.Name, 
                    COUNT(DISTINCT gwr.Id) AS GameCount
                FROM GamesWithRoms gwr
                INNER JOIN Relation_Game_<ITEMNAME>s rel
                    ON gwr.Id = rel.GameId 
                    AND gwr.GameIdType = rel.GameSourceId
                INNER JOIN Metadata_<ITEMNAME> item 
                    ON rel.<ITEMNAME>sId = item.Id
                GROUP BY gwr.GameIdType, item.Id, item.Name
                ORDER BY item.Name";
            sql = sql.Replace("<ITEMNAME>", Name);
            DataTable dbResponse = await db.ExecuteCMDAsync(sql, new DatabaseMemoryCacheOptions(CacheEnabled: true, ExpirationSeconds: 300));
            
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
                    HasheousClient.Models.MetadataSources SourceId = (HasheousClient.Models.MetadataSources)Enum.Parse(typeof(HasheousClient.Models.MetadataSources), dr["GameIdType"].ToString());

                    if (this.Ids == null)
                    {
                        this.Ids = new Dictionary<HasheousClient.Models.MetadataSources, long>();
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

            public Dictionary<HasheousClient.Models.MetadataSources, long>? Ids { get; set; }

            public string Name { get; set; }

            public int GameCount { get; set; }
        }
    }
}