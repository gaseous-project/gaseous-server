﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using gaseous_tools;
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
            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

            Dictionary<string, object> FilterSet = new Dictionary<string, object>();

            // platforms
            List<Platform> platforms = new List<Platform>();
            string sql = "SELECT Platform.Id, Platform.Abbreviation, Platform.AlternativeName, Platform.`Name`, Platform.PlatformLogo, (SELECT COUNT(Games_Roms.Id) AS RomCount FROM Games_Roms WHERE Games_Roms.PlatformId = Platform.Id) AS RomCount FROM Platform HAVING RomCount > 0 ORDER BY `Name`";
            DataTable dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                platforms.Add(Classes.Metadata.Platforms.GetPlatform((long)dr["id"]));
            }
            FilterSet.Add("platforms", platforms);

            // genres
            List<Genre> genres = new List<Genre>();
            sql = "SELECT DISTINCT t1.Id, t1.`Name` FROM Genre AS t1 JOIN (SELECT * FROM Game WHERE (SELECT COUNT(Id) FROM Games_Roms WHERE GameId = Game.Id) > 0) AS t2 ON JSON_CONTAINS(t2.Genres, CAST(t1.Id AS char), '$') ORDER BY t1.`Name`";
            dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                genres.Add(Classes.Metadata.Genres.GetGenres((long)dr["id"]));
            }
            FilterSet.Add("genres", genres);

            // game modes
            List<GameMode> gameModes = new List<GameMode>();
            sql = "SELECT DISTINCT t1.Id, t1.`Name` FROM GameMode AS t1 JOIN (SELECT * FROM Game WHERE (SELECT COUNT(Id) FROM Games_Roms WHERE GameId = Game.Id) > 0) AS t2 ON JSON_CONTAINS(t2.GameModes, CAST(t1.Id AS char), '$') ORDER BY t1.Id";
            dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                gameModes.Add(Classes.Metadata.GameModes.GetGame_Modes((long)dr["id"]));
            }
            FilterSet.Add("gamemodes", gameModes);

            // player perspectives
            List<PlayerPerspective> playerPerspectives = new List<PlayerPerspective>();
            sql = "SELECT DISTINCT t1.Id, t1.`Name` FROM PlayerPerspective AS t1 JOIN (SELECT * FROM Game WHERE (SELECT COUNT(Id) FROM Games_Roms WHERE GameId = Game.Id) > 0) AS t2 ON JSON_CONTAINS(t2.PlayerPerspectives, CAST(t1.Id AS char), '$') ORDER BY t1.`Name`";
            dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                playerPerspectives.Add(Classes.Metadata.PlayerPerspectives.GetGame_PlayerPerspectives((long)dr["id"]));
            }
            FilterSet.Add("playerperspectives", playerPerspectives);

            // themes
            List<Theme> themes = new List<Theme>();
            sql = "SELECT DISTINCT t1.Id, t1.`Name` FROM Theme AS t1 JOIN (SELECT * FROM Game WHERE (SELECT COUNT(Id) FROM Games_Roms WHERE GameId = Game.Id) > 0) AS t2 ON JSON_CONTAINS(t2.Themes, CAST(t1.Id AS char), '$') ORDER BY t1.`Name`";
            dbResponse = db.ExecuteCMD(sql);

            foreach (DataRow dr in dbResponse.Rows)
            {
                themes.Add(Classes.Metadata.Themes.GetGame_Themes((long)dr["id"]));
            }
            FilterSet.Add("themes", themes);

            return FilterSet;
        }
    }
}