using System.Data;
using System.Reflection.Metadata.Ecma335;
using gaseous_server.Classes.Metadata;
using IGDB.Models;

namespace gaseous_server.Classes
{
    public class Filters
    {
        public static Dictionary<string, object> Filter(Metadata.AgeRatings.AgeGroups.AgeRestrictionGroupings MaximumAgeRestriction, bool IncludeUnrated)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            Dictionary<string, object> FilterSet = new Dictionary<string, object>();

            // platforms
            List<FilterPlatform> platforms = new List<FilterPlatform>();
            
            string ageRestriction_Platform = "Game.AgeGroupId <= " + (int)MaximumAgeRestriction;
            string ageRestriction_Generic = "view_Games.AgeGroupId <= " + (int)MaximumAgeRestriction;
            if (IncludeUnrated == true)
            {
                ageRestriction_Platform += " OR Game.AgeGroupId IS NULL";
                ageRestriction_Generic += " OR view_Games.AgeGroupId IS NULL";
            }

            string sql = "SELECT DISTINCT Platform.Id, Platform.Abbreviation, Platform.AlternativeName, Platform.`Name`, Platform.PlatformLogo, (SELECT COUNT(*) AS GameCount FROM (SELECT DISTINCT Games_Roms.GameId AS ROMGameId, Games_Roms.PlatformId, view_Games.AgeGroupId FROM Games_Roms LEFT JOIN view_Games ON view_Games.Id = Games_Roms.GameId) Game WHERE Game.PlatformId = Platform.Id AND (" + ageRestriction_Platform + ")) AS GameCount FROM Platform LEFT JOIN Relation_Game_Platforms ON Relation_Game_Platforms.PlatformsId = Platform.Id LEFT JOIN view_Games ON view_Games.Id = Relation_Game_Platforms.GameId HAVING GameCount > 0 ORDER BY Platform.`Name`;";
            
            DataTable dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterPlatform platformItem = new FilterPlatform(Classes.Metadata.Platforms.GetPlatform((long)dr["id"]));
                platformItem.GameCount = (int)(long)dr["GameCount"];
                platforms.Add(platformItem);

            }
            FilterSet.Add("platforms", platforms);

            // genres
            List<FilterGenre> genres = new List<FilterGenre>();
            dbResponse = GetGenericFilterItem(db, "Genre", ageRestriction_Generic);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterGenre genreItem = new FilterGenre(Classes.Metadata.Genres.GetGenres((long)dr["id"]));
                genreItem.GameCount = (int)(long)dr["GameCount"];
                genres.Add(genreItem);
            }
            FilterSet.Add("genres", genres);

            // game modes
            List<FilterGameMode> gameModes = new List<FilterGameMode>();
            dbResponse = GetGenericFilterItem(db, "GameMode", ageRestriction_Generic);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterGameMode gameModeItem = new FilterGameMode(Classes.Metadata.GameModes.GetGame_Modes((long)dr["id"]));
                gameModeItem.GameCount = (int)(long)dr["GameCount"];
                gameModes.Add(gameModeItem);
            }
            FilterSet.Add("gamemodes", gameModes);

            // player perspectives
            List<FilterPlayerPerspective> playerPerspectives = new List<FilterPlayerPerspective>();
            dbResponse = GetGenericFilterItem(db, "PlayerPerspective", ageRestriction_Generic);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterPlayerPerspective playerPerspectiveItem = new FilterPlayerPerspective(Classes.Metadata.PlayerPerspectives.GetGame_PlayerPerspectives((long)dr["id"]));
                playerPerspectiveItem.GameCount = (int)(long)dr["GameCount"];
                playerPerspectives.Add(playerPerspectiveItem);
            }
            FilterSet.Add("playerperspectives", playerPerspectives);

            // themes
            List<FilterTheme> themes = new List<FilterTheme>();
            dbResponse = GetGenericFilterItem(db, "Theme", ageRestriction_Generic);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterTheme themeItem = new FilterTheme(Classes.Metadata.Themes.GetGame_Themes((long)dr["id"]));
                themeItem.GameCount = (int)(long)dr["GameCount"];
                themes.Add(themeItem);
            }
            FilterSet.Add("themes", themes);

            // age groups
            List<FilterAgeGrouping> agegroupings = new List<FilterAgeGrouping>();
            sql = "SELECT view_Games.Id, view_Games.AgeGroupId, COUNT(view_Games.Id) AS GameCount FROM view_Games WHERE (" + ageRestriction_Generic + ") GROUP BY view_Games.AgeGroupId ORDER BY view_Games.AgeGroupId DESC;";
            dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterAgeGrouping filterAgeGrouping = new FilterAgeGrouping();
                if (dr["AgeGroupId"] == DBNull.Value)
                {
                    filterAgeGrouping.Id = (long)AgeRatings.AgeGroups.AgeRestrictionGroupings.Unclassified;
                    filterAgeGrouping.AgeGroup = AgeRatings.AgeGroups.AgeRestrictionGroupings.Unclassified;
                }
                else
                {
                    filterAgeGrouping.Id = (long)(AgeRatings.AgeGroups.AgeRestrictionGroupings)dr["AgeGroupId"];
                    filterAgeGrouping.AgeGroup = (AgeRatings.AgeGroups.AgeRestrictionGroupings)dr["AgeGroupId"];
                }
                filterAgeGrouping.GameCount = (int)(long)dr["GameCount"];
                agegroupings.Add(filterAgeGrouping);
            }
            FilterSet.Add("agegroupings", agegroupings);

            return FilterSet;
        }

        private static DataTable GetGenericFilterItem(Database db, string Name, string AgeRestriction_Generic)
        {
            string sql = "SELECT DISTINCT <ITEMNAME>.Id, <ITEMNAME>.`Name`, COUNT(view_Games.Id) AS GameCount FROM <ITEMNAME> LEFT JOIN Relation_Game_<ITEMNAME>s ON Relation_Game_<ITEMNAME>s.<ITEMNAME>sId = <ITEMNAME>.Id LEFT JOIN view_Games ON view_Games.Id = Relation_Game_<ITEMNAME>s.GameId WHERE (" + AgeRestriction_Generic + ") GROUP BY <ITEMNAME>.Id ORDER BY <ITEMNAME>.`Name`;";
            sql = sql.Replace("<ITEMNAME>", Name);
            DataTable dbResponse = db.ExecuteCMD(sql);

            return dbResponse;
        }

        public class FilterPlatform : IGDB.Models.Platform
        {
            public FilterPlatform(Platform obj)
            {
                var properties = obj.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.GetGetMethod() != null)
                    {
                        this.GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(obj));
                    }
                }
            }

            public int GameCount { get; set; }
        }

        public class FilterGenre : IGDB.Models.Genre
        {
            public FilterGenre(Genre obj)
            {
                var properties = obj.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.GetGetMethod() != null)
                    {
                        this.GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(obj));
                    }
                }
            }

            public int GameCount { get; set; }
        }

        public class FilterGameMode : IGDB.Models.GameMode
        {
            public FilterGameMode(GameMode obj)
            {
                var properties = obj.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.GetGetMethod() != null)
                    {
                        this.GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(obj));
                    }
                }
            }

            public int GameCount { get; set; }
        }

        public class FilterPlayerPerspective : IGDB.Models.PlayerPerspective
        {
            public FilterPlayerPerspective(PlayerPerspective obj)
            {
                var properties = obj.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.GetGetMethod() != null)
                    {
                        this.GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(obj));
                    }
                }
            }

            public int GameCount { get; set; }
        }

        public class FilterTheme : IGDB.Models.Theme
        {
            public FilterTheme(Theme obj)
            {
                var properties = obj.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.GetGetMethod() != null)
                    {
                        this.GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(obj));
                    }
                }
            }

            public int GameCount { get; set; }
        }

        public class FilterAgeGrouping
        {
            public long Id { get; set; }

            public AgeRatings.AgeGroups.AgeRestrictionGroupings AgeGroup { get ; set; }

            public string Name
            {
                get
                {
                    return this.AgeGroup.ToString();
                }
            }

            public int GameCount { get; set; }
        }
    }
}