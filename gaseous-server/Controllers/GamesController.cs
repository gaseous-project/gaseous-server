using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using gaseous_server.Classes.Metadata;
using gaseous_tools;
using IGDB.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using static gaseous_server.Classes.Metadata.AgeRatings;

namespace gaseous_server.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(List<Game>), StatusCodes.Status200OK)]
        public ActionResult Game(string name = "", string platform = "", string genre = "", bool sortdescending = false)
        {
            string whereClause = "";
            string havingClause = "";
            Dictionary<string, object> whereParams = new Dictionary<string, object>();

            List<string> whereClauses = new List<string>();
            List<string> havingClauses = new List<string>();

            string tempVal = "";

            if (name.Length > 0)
            {
                tempVal = "`name` LIKE @name";
                whereParams.Add("@name", "%" + name + "%");
                havingClauses.Add(tempVal);
            }

            if (platform.Length > 0)
            {
                tempVal = "games_roms.platformid IN (";
                string[] platformClauseItems = platform.Split(",");
                for (int i = 0; i < platformClauseItems.Length; i++)
                {
                    if (i > 0)
                    {
                        tempVal += ", ";
                    }
                    string platformLabel = "@platform" + i;
                    tempVal += platformLabel;
                    whereParams.Add(platformLabel, platformClauseItems[i]);
                }
                tempVal += ")";
                whereClauses.Add(tempVal);
            }

            if (genre.Length > 0)
            {
                tempVal = "(";
                string[] genreClauseItems = genre.Split(",");
                for (int i = 0; i < genreClauseItems.Length; i++)
                {
                    if (i > 0)
                    {
                        tempVal += " AND ";
                    }
                    string genreLabel = "@genre" + i;
                    tempVal += "JSON_CONTAINS(game.genres, " + genreLabel + ", '$')";
                    whereParams.Add(genreLabel, genreClauseItems[i]);
                }
                tempVal += ")";
                whereClauses.Add(tempVal);
            }

            // build where clause
            if (whereClauses.Count > 0)
            {
                whereClause = "WHERE ";
                for (int i = 0; i < whereClauses.Count; i++)
                {
                    if (i > 0)
                    {
                        whereClause += " AND ";
                    }
                    whereClause += whereClauses[i];
                }
            }

            // build having clause
            if (havingClauses.Count > 0)
            {
                havingClause = "HAVING ";
                for (int i = 0; i < havingClauses.Count; i++)
                {
                    if (i > 0)
                    {
                        havingClause += " AND ";
                    }
                    havingClause += havingClauses[i];
                }
            }

            // order by clause
            string orderByClause = "ORDER BY `name` ASC";
            if (sortdescending == true)
            {
                orderByClause = "ORDER BY `name` DESC";
            }

            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT DISTINCT games_roms.gameid AS ROMGameId, game.ageratings, game.aggregatedrating, game.aggregatedratingcount, game.alternativenames, game.artworks, game.bundles, game.category, game.collection, game.cover, game.dlcs, game.expansions, game.externalgames, game.firstreleasedate, game.`follows`, game.franchise, game.franchises, game.gameengines, game.gamemodes, game.genres, game.hypes, game.involvedcompanies, game.keywords, game.multiplayermodes, (CASE WHEN games_roms.gameid = 0 THEN games_roms.`name` ELSE game.`name` END) AS `name`, game.parentgame, game.platforms, game.playerperspectives, game.rating, game.ratingcount, game.releasedates, game.screenshots, game.similargames, game.slug, game.standaloneexpansions, game.`status`, game.storyline, game.summary, game.tags, game.themes, game.totalrating, game.totalratingcount, game.versionparent, game.versiontitle, game.videos, game.websites FROM gaseous.games_roms LEFT JOIN game ON game.id = games_roms.gameid " + whereClause + " " + havingClause + " " + orderByClause;

            List<IGDB.Models.Game> RetVal = new List<IGDB.Models.Game>();

            DataTable dbResponse = db.ExecuteCMD(sql, whereParams);
            foreach (DataRow dr in dbResponse.Rows)
            {
                if ((long)dr["ROMGameId"] == 0)
                {
                    // unknown game
                }
                else
                {
                    // known game
                    RetVal.Add(Classes.Metadata.Games.GetGame((long)dr["ROMGameId"], false, false));
                }
            }

            return Ok(RetVal);
        }

        [HttpGet]
        [Route("{GameId}")]
        [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult Game(long GameId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                if (gameObject != null)
                {
                    return Ok(gameObject);
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/agerating")]
        [ProducesResponseType(typeof(List<AgeRatings.GameAgeRating>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameAgeClassification(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                if (gameObject.AgeRatings != null)
                {
                    List<AgeRatings.GameAgeRating> ageRatings = new List<AgeRatings.GameAgeRating>();
                    foreach (long ageRatingId in gameObject.AgeRatings.Ids)
                    {
                        ageRatings.Add(AgeRatings.GetConsolidatedAgeRating(ageRatingId));
                    }
                    return Ok(ageRatings);
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/agerating/{RatingId}/image")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameAgeClassification(long GameId, long RatingId)
        {
            try
            {
                GameAgeRating gameAgeRating = GetConsolidatedAgeRating(RatingId);

                string fileExtension = "";
                string fileType = "";
                switch (gameAgeRating.RatingBoard)
                {
                    case AgeRatingCategory.ESRB:
                        fileExtension = "svg";
                        fileType = "image/svg+xml";
                        break;
                    case AgeRatingCategory.PEGI:
                        fileExtension = "jpg";
                        fileType = "image/jpg";
                        break;
                    case AgeRatingCategory.ACB:
                        fileExtension = "png";
                        fileType = "image/png";
                        break;
                }

                string resourceName = "gaseous_server.Assets.Ratings." + gameAgeRating.RatingBoard.ToString() + "." + gameAgeRating.RatingTitle.ToString() + "." + fileExtension;

                var assembly = Assembly.GetExecutingAssembly();
                string[] resources = assembly.GetManifestResourceNames();
                if (resources.Contains(resourceName))
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        byte[] filedata = new byte[stream.Length];
                        stream.Read(filedata, 0, filedata.Length);

                        string filename = gameAgeRating.RatingBoard.ToString() + "-" + gameAgeRating.RatingTitle.ToString() + "." + fileExtension;
                        string contentType = fileType;

                        var cd = new System.Net.Mime.ContentDisposition
                        {
                            FileName = filename,
                            Inline = true,
                        };

                        Response.Headers.Add("Content-Disposition", cd.ToString());

                        return File(filedata, contentType);
                    }
                }
                return NotFound();
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/artwork")]
        [ProducesResponseType(typeof(List<Artwork>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameArtwork(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                List<Artwork> artworks = new List<Artwork>();
                if (gameObject.Artworks != null)
                {
                    foreach (long ArtworkId in gameObject.Artworks.Ids)
                    {
                        Artwork GameArtwork = Artworks.GetArtwork(ArtworkId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject));
                        artworks.Add(GameArtwork);
                    }
                }

                return Ok(artworks);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/artwork/{ArtworkId}")]
        [ProducesResponseType(typeof(Artwork), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameArtwork(long GameId, long ArtworkId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                try
                {
                    IGDB.Models.Artwork artworkObject = Artworks.GetArtwork(ArtworkId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject));
                    if (artworkObject != null)
                    {
                        return Ok(artworkObject);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                catch
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/artwork/{ArtworkId}/image")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameCoverImage(long GameId, long ArtworkId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                try
                {
                    IGDB.Models.Artwork artworkObject = Artworks.GetArtwork(ArtworkId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject));
                    if (artworkObject != null) {
                        string coverFilePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), "Artwork", artworkObject.ImageId + ".png");
                        if (System.IO.File.Exists(coverFilePath))
                        {
                            string filename = artworkObject.ImageId + ".png";
                            string filepath = coverFilePath;
                            byte[] filedata = System.IO.File.ReadAllBytes(filepath);
                            string contentType = "image/png";

                            var cd = new System.Net.Mime.ContentDisposition
                            {
                                FileName = filename,
                                Inline = true,
                            };

                            Response.Headers.Add("Content-Disposition", cd.ToString());

                            return File(filedata, contentType);
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                catch
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/cover")]
        [ProducesResponseType(typeof(Cover), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameCover(long GameId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);
                if (gameObject != null)
                {
                    IGDB.Models.Cover coverObject = Covers.GetCover(gameObject.Cover.Id, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject));
                    if (coverObject != null)
                    {
                        return Ok(coverObject);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/cover/image")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameCoverImage(long GameId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                string coverFilePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), "Cover.png");
                if (System.IO.File.Exists(coverFilePath)) {
                    string filename = "Cover.png";
                    string filepath = coverFilePath;
                    byte[] filedata = System.IO.File.ReadAllBytes(filepath);
                    string contentType = "image/png";

                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = filename,
                        Inline = true,
                    };

                    Response.Headers.Add("Content-Disposition", cd.ToString());

                    return File(filedata, contentType);
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/roms")]
        [ProducesResponseType(typeof(List<Classes.Roms.GameRomItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameRom(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                List<Classes.Roms.GameRomItem> roms = Classes.Roms.GetRoms(GameId);

                return Ok(roms);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/roms/{RomId}")]
        [ProducesResponseType(typeof(Classes.Roms.GameRomItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameRom(long GameId, long RomId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);
                if (rom.GameId == GameId)
                {
                    return Ok(rom);
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPatch]
        [Route("{GameId}/roms/{RomId}")]
        [ProducesResponseType(typeof(Classes.Roms.GameRomItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameRomRename(long GameId, long RomId, long NewPlatformId, long NewGameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);
                if (rom.GameId == GameId)
                {
                    rom = Classes.Roms.UpdateRom(RomId, NewPlatformId, NewGameId);
                    return Ok(rom);
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpDelete]
        [Route("{GameId}/roms/{RomId}")]
        [ProducesResponseType(typeof(Classes.Roms.GameRomItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameRomDelete(long GameId, long RomId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);
                if (rom.GameId == GameId)
                {
                    Classes.Roms.DeleteRom(RomId);
                    return Ok(rom);
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/roms/{RomId}/file")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameRomFile(long GameId, long RomId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);
                if (rom.GameId != GameId)
                {
                    return NotFound();
                }

                string romFilePath = rom.Path;
                if (System.IO.File.Exists(romFilePath))
                {
                    string filename = Path.GetFileName(romFilePath);
                    string filepath = romFilePath;
                    byte[] filedata = System.IO.File.ReadAllBytes(filepath);
                    string contentType = "application/octet-stream";

                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = filename,
                        Inline = false,
                    };

                    Response.Headers.Add("Content-Disposition", cd.ToString());

                    return File(filedata, contentType);
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/screenshots")]
        [ProducesResponseType(typeof(List<Screenshot>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameScreenshot(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                List<Screenshot> screenshots = new List<Screenshot>();
                if (gameObject.Screenshots != null)
                {
                    foreach (long ScreenshotId in gameObject.Screenshots.Ids)
                    {
                        Screenshot GameScreenshot = Screenshots.GetScreenshot(ScreenshotId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject));
                        screenshots.Add(GameScreenshot);
                    }
                }

                return Ok(screenshots);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/screenshots/{ScreenshotId}")]
        [ProducesResponseType(typeof(Screenshot), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameScreenshot(long GameId, long ScreenshotId)
        {
            try
            { 
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);
                if (gameObject != null) { 
                    IGDB.Models.Screenshot screenshotObject = Screenshots.GetScreenshot(ScreenshotId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject));
                    if (screenshotObject != null)
                    {
                        return Ok(screenshotObject);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/screenshots/{ScreenshotId}/image")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameScreenshotImage(long GameId, long ScreenshotId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                IGDB.Models.Screenshot screenshotObject = Screenshots.GetScreenshot(ScreenshotId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject));

                string coverFilePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), "Screenshots", screenshotObject.ImageId + ".png");
                if (System.IO.File.Exists(coverFilePath))
                {
                    string filename = screenshotObject.ImageId + ".png";
                    string filepath = coverFilePath;
                    byte[] filedata = System.IO.File.ReadAllBytes(filepath);
                    string contentType = "image/png";

                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = filename,
                        Inline = true,
                    };

                    Response.Headers.Add("Content-Disposition", cd.ToString());

                    return File(filedata, contentType);
                }
                else
                {
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("{GameId}/videos")]
        [ProducesResponseType(typeof(List<GameVideo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameVideo(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false);

                List<GameVideo> videos = new List<GameVideo>();
                if (gameObject.Videos != null)
                {
                    foreach (long VideoId in gameObject.Videos.Ids)
                    {
                        GameVideo gameVideo = GamesVideos.GetGame_Videos(VideoId);
                        videos.Add(gameVideo);
                    }
                }

                return Ok(videos);
            }
            catch
            {
                return NotFound();
            }
        }
    }
}
