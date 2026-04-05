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
            string ageRestriction_Platform = "g.AgeGroupId <= " + (int)MaximumAgeRestriction;
            if (IncludeUnrated == true)
            {
                ageRestriction_Platform += " OR g.AgeGroupId IS NULL";
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
            string ageRestriction_Platform = "g.AgeGroupId <= " + (int)MaximumAgeRestriction;
            if (IncludeUnrated == true)
            {
                ageRestriction_Platform += " OR g.AgeGroupId IS NULL";
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
                    g.AgeGroupId
                FROM Metadata_Game g
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
            DataTable dbResponse = await GetGenericFilterItemFromTemp(db, Name);
            Dictionary<string, FilterItem> filterDict = new Dictionary<string, FilterItem>(StringComparer.Ordinal);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterItem filterItem = new FilterItem(dr);
                if (filterItem?.Name != null)
                {
                    if (filterDict.TryGetValue(filterItem.Name, out FilterItem? existingItem))
                    {
                        // Merge with existing item
                        if (existingItem?.Ids != null && filterItem.Ids != null)
                        {
                            foreach (var id in filterItem.Ids)
                            {
                                if (!existingItem.Ids.ContainsKey(id.Key))
                                {
                                    existingItem.Ids[id.Key] = id.Value;
                                    existingItem.GameCount += filterItem.GameCount;
                                }
                            }
                        }
                    }
                    else
                    {
                        filterDict[filterItem.Name] = filterItem;
                    }
                }
            }

            return new List<FilterItem>(filterDict.Values);
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
            DataTable dbResponse = await GetGenericFilterItem(db, Name, AgeRestriction);
            Dictionary<string, FilterItem> filterDict = new Dictionary<string, FilterItem>(StringComparer.Ordinal);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterItem filterItem = new FilterItem(dr);
                if (filterItem?.Name != null)
                {
                    if (filterDict.TryGetValue(filterItem.Name, out FilterItem? existingItem))
                    {
                        // Merge with existing item
                        if (existingItem?.Ids != null && filterItem.Ids != null)
                        {
                            foreach (var id in filterItem.Ids)
                            {
                                if (!existingItem.Ids.ContainsKey(id.Key))
                                {
                                    existingItem.Ids[id.Key] = id.Value;
                                    existingItem.GameCount += filterItem.GameCount;
                                }
                            }
                        }
                    }
                    else
                    {
                        filterDict[filterItem.Name] = filterItem;
                    }
                }
            }

            return new List<FilterItem>(filterDict.Values);
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
                this.Name = string.Empty;
            }

            public FilterItem(DataRow dr)
            {
                this.Name = string.Empty;

                if (dr.Table.Columns.Contains("GameIdType"))
                {
                    int gameIdTypeIndex = dr.Table.Columns.IndexOf("GameIdType");
                    int idIndex = dr.Table.Columns.IndexOf("Id");

                    object? gameIdTypeValue = dr[gameIdTypeIndex];
                    if (gameIdTypeValue != null && gameIdTypeValue != DBNull.Value)
                    {
                        if (int.TryParse(gameIdTypeValue.ToString(), out int sourceIdValue))
                        {
                            HasheousClient.Models.MetadataSources SourceId = (HasheousClient.Models.MetadataSources)sourceIdValue;
                            this.Ids = new Dictionary<HasheousClient.Models.MetadataSources, long>(1)
                            {
                                { SourceId, (long)dr[idIndex] }
                            };
                        }
                    }
                }
                else
                {
                    this.Id = (long)dr["Id"];
                }

                object? nameValue = dr["Name"];
                this.Name = nameValue?.ToString() ?? string.Empty;
                this.GameCount = (int)(long)dr["GameCount"];
            }

            public long? Id { get; set; }

            public Dictionary<HasheousClient.Models.MetadataSources, long>? Ids { get; set; }

            public string Name { get; set; }

            public int GameCount { get; set; }
        }
    }
}