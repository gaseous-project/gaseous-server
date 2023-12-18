using System.Data;
using System.Reflection.Metadata.Ecma335;
using gaseous_server.Classes.Metadata;
using IGDB.Models;

namespace gaseous_server.Classes
{
    public class Filters
    {
        public static Dictionary<string, List<FilterItem>> Filter(Metadata.AgeGroups.AgeRestrictionGroupings MaximumAgeRestriction, bool IncludeUnrated)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            Dictionary<string, List<FilterItem>> FilterSet = new Dictionary<string, List<FilterItem>>();

            // platforms
            List<FilterItem> platforms = new List<FilterItem>();
            
            string ageRestriction_Platform = "Game.AgeGroupId <= " + (int)MaximumAgeRestriction;
            string ageRestriction_Generic = "view_Games.AgeGroupId <= " + (int)MaximumAgeRestriction;
            if (IncludeUnrated == true)
            {
                ageRestriction_Platform += " OR Game.AgeGroupId IS NULL";
                ageRestriction_Generic += " OR view_Games.AgeGroupId IS NULL";
            }

            string sql = "SELECT Platform.Id, Platform.`Name`, COUNT(view_Games.Id) AS GameCount FROM view_Games JOIN Relation_Game_Platforms ON Relation_Game_Platforms.GameId = view_Games.Id AND (Relation_Game_Platforms.PlatformsId IN (SELECT DISTINCT PlatformId FROM Games_Roms WHERE Games_Roms.GameId = view_Games.Id)) JOIN Platform ON Platform.Id = Relation_Game_Platforms.PlatformsId WHERE (" + ageRestriction_Generic + ") GROUP BY Platform.`Name` ORDER BY Platform.`Name`;";
            
            DataTable dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterItem platformItem = new FilterItem(dr);
                platforms.Add(platformItem);

            }
            FilterSet.Add("platforms", platforms);

            // genres
            List<FilterItem> genres = new List<FilterItem>();
            dbResponse = GetGenericFilterItem(db, "Genre", ageRestriction_Generic);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterItem genreItem = new FilterItem(dr);
                genres.Add(genreItem);
            }
            FilterSet.Add("genres", genres);

            // game modes
            List<FilterItem> gameModes = new List<FilterItem>();
            dbResponse = GetGenericFilterItem(db, "GameMode", ageRestriction_Generic);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterItem gameModeItem = new FilterItem(dr);
                gameModes.Add(gameModeItem);
            }
            FilterSet.Add("gamemodes", gameModes);

            // player perspectives
            List<FilterItem> playerPerspectives = new List<FilterItem>();
            dbResponse = GetGenericFilterItem(db, "PlayerPerspective", ageRestriction_Generic);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterItem playerPerspectiveItem = new FilterItem(dr);
                playerPerspectives.Add(playerPerspectiveItem);
            }
            FilterSet.Add("playerperspectives", playerPerspectives);

            // themes
            List<FilterItem> themes = new List<FilterItem>();
            dbResponse = GetGenericFilterItem(db, "Theme", ageRestriction_Generic);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterItem themeItem = new FilterItem(dr);
                themes.Add(themeItem);
            }
            FilterSet.Add("themes", themes);

            // age groups
            List<FilterItem> agegroupings = new List<FilterItem>();
            sql = "SELECT view_Games.Id, view_Games.AgeGroupId, COUNT(view_Games.Id) AS GameCount FROM view_Games WHERE (" + ageRestriction_Generic + ") GROUP BY view_Games.AgeGroupId ORDER BY view_Games.AgeGroupId DESC;";
            dbResponse = db.ExecuteCMD(sql);

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

        private static DataTable GetGenericFilterItem(Database db, string Name, string AgeRestriction_Generic)
        {
            string sql = "SELECT DISTINCT <ITEMNAME>.Id, <ITEMNAME>.`Name`, COUNT(view_Games.Id) AS GameCount FROM <ITEMNAME> LEFT JOIN Relation_Game_<ITEMNAME>s ON Relation_Game_<ITEMNAME>s.<ITEMNAME>sId = <ITEMNAME>.Id LEFT JOIN view_Games ON view_Games.Id = Relation_Game_<ITEMNAME>s.GameId WHERE (" + AgeRestriction_Generic + ") GROUP BY <ITEMNAME>.Id HAVING GameCount > 0 ORDER BY <ITEMNAME>.`Name`;";
            sql = sql.Replace("<ITEMNAME>", Name);
            DataTable dbResponse = db.ExecuteCMD(sql);

            return dbResponse;
        }

        public class FilterItem
        {
            public FilterItem()
            {

            }

            public FilterItem(DataRow dr)
            {
                this.Id = (long)dr["Id"];
                this.Name = (string)dr["Name"];
                this.GameCount = (int)(long)dr["GameCount"];
            }

            public long Id { get; set; }

            public string Name { get; set; }

            public int GameCount { get; set; }
        }
    }
}