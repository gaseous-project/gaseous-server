using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using gaseous_server.Classes;
using IGDB.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class FilterController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        //[ResponseCache(CacheProfileName = "5Minute")]
        public Dictionary<string, object> Filter()
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            Dictionary<string, object> FilterSet = new Dictionary<string, object>();

            // platforms
            List<FilterPlatform> platforms = new List<FilterPlatform>();
            //string sql = "SELECT Platform.Id, Platform.Abbreviation, Platform.AlternativeName, Platform.`Name`, Platform.PlatformLogo, (SELECT COUNT(Games_Roms.Id) AS RomCount FROM Games_Roms WHERE Games_Roms.PlatformId = Platform.Id) AS RomCount FROM Platform HAVING RomCount > 0 ORDER BY `Name`";
            string sql = "SELECT Platform.Id, Platform.Abbreviation, Platform.AlternativeName, Platform.`Name`, Platform.PlatformLogo, (SELECT COUNT(Games_Roms.Id) AS RomCount FROM Games_Roms WHERE Games_Roms.PlatformId = Platform.Id) AS RomCount, (SELECT COUNT(*) AS GameCount FROM (SELECT DISTINCT Games_Roms.GameId AS ROMGameId, Games_Roms.PlatformId FROM Games_Roms LEFT JOIN Game ON Game.Id = Games_Roms.GameId) Game WHERE Game.PlatformId = Platform.Id) AS GameCount FROM Platform HAVING RomCount > 0 ORDER BY `Name`";
            DataTable dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                FilterPlatform platformItem = new FilterPlatform(Classes.Metadata.Platforms.GetPlatform((long)dr["id"]));
                platformItem.RomCount = (int)(long)dr["RomCount"];
                platformItem.GameCount = (int)(long)dr["GameCount"];
                platforms.Add(platformItem);

            }
            FilterSet.Add("platforms", platforms);

            // genres
            List<Genre> genres = new List<Genre>();
            sql = "SELECT DISTINCT Relation_Game_Genres.GenresId AS id, Genre.`Name` FROM Relation_Game_Genres JOIN Genre ON Relation_Game_Genres.GenresId = Genre.Id WHERE Relation_Game_Genres.GameId IN (SELECT DISTINCT GameId FROM Games_Roms) ORDER BY `Name`;";
            dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                genres.Add(Classes.Metadata.Genres.GetGenres((long)dr["id"]));
            }
            FilterSet.Add("genres", genres);

            // game modes
            List<GameMode> gameModes = new List<GameMode>();
            sql = "SELECT DISTINCT Relation_Game_GameModes.GameModesId AS id, GameMode.`Name` FROM Relation_Game_GameModes JOIN GameMode ON Relation_Game_GameModes.GameModesId = GameMode.Id WHERE Relation_Game_GameModes.GameId IN (SELECT DISTINCT GameId FROM Games_Roms) ORDER BY `Name`;";
            dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                gameModes.Add(Classes.Metadata.GameModes.GetGame_Modes((long)dr["id"]));
            }
            FilterSet.Add("gamemodes", gameModes);

            // player perspectives
            List<PlayerPerspective> playerPerspectives = new List<PlayerPerspective>();
            sql = "SELECT DISTINCT Relation_Game_PlayerPerspectives.PlayerPerspectivesId AS id, PlayerPerspective.`Name` FROM Relation_Game_PlayerPerspectives JOIN PlayerPerspective ON Relation_Game_PlayerPerspectives.PlayerPerspectivesId = PlayerPerspective.Id WHERE Relation_Game_PlayerPerspectives.GameId IN (SELECT DISTINCT GameId FROM Games_Roms) ORDER BY `Name`;";
            dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                playerPerspectives.Add(Classes.Metadata.PlayerPerspectives.GetGame_PlayerPerspectives((long)dr["id"]));
            }
            FilterSet.Add("playerperspectives", playerPerspectives);

            // themes
            List<Theme> themes = new List<Theme>();
            sql = "SELECT DISTINCT Relation_Game_Themes.ThemesId AS id, Theme.`Name` FROM Relation_Game_Themes JOIN Theme ON Relation_Game_Themes.ThemesId = Theme.Id WHERE Relation_Game_Themes.GameId IN (SELECT DISTINCT GameId FROM Games_Roms) ORDER BY `Name`;";
            dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                themes.Add(Classes.Metadata.Themes.GetGame_Themes((long)dr["id"]));
            }
            FilterSet.Add("themes", themes);

            return FilterSet;
        }

        public class FilterPlatform : IGDB.Models.Platform
        {
            public FilterPlatform(Platform platform)
            {
                var properties = platform.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.GetGetMethod() != null)
                    {
                        this.GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(platform));
                    }
                }
            }

            public int RomCount { get; set; }
            public int GameCount { get; set; }
        }
    }
}