using System;
using System.Collections;
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
        public ActionResult Game(
            string name = "",
            string platform = "",
            string genre = "",
            string gamemode = "",
            string playerperspective = "",
            string theme = "",
            int minrating = -1,
            int maxrating = -1,
            bool sortdescending = false)
            {

            return Ok(GetGames(name, platform, genre, gamemode, playerperspective, theme, minrating, maxrating, sortdescending));
        }

        public static List<Game> GetGames(
            string name = "",
            string platform = "",
            string genre = "",
            string gamemode = "",
            string playerperspective = "",
            string theme = "",
            int minrating = -1,
            int maxrating = -1,
            bool sortdescending = false)
        {
            string whereClause = "";
            string havingClause = "";
            Dictionary<string, object> whereParams = new Dictionary<string, object>();

            List<string> whereClauses = new List<string>();
            List<string> havingClauses = new List<string>();

            string tempVal = "";

            if (name.Length > 0)
            {
                tempVal = "`Name` LIKE @Name";
                whereParams.Add("@Name", "%" + name + "%");
                havingClauses.Add(tempVal);
            }

            if (minrating != -1)
            {
                string ratingTempMinVal = "totalRating >= @totalMinRating";
                whereParams.Add("@totalMinRating", minrating);
                havingClauses.Add(ratingTempMinVal);
            }

            if (maxrating != -1)
            {
                string ratingTempMaxVal = "totalRating <= @totalMaxRating";
                whereParams.Add("@totalMaxRating", maxrating);
                havingClauses.Add(ratingTempMaxVal);
            }

            if (platform.Length > 0)
            {
                tempVal = "Games_Roms.PlatformId IN (";
                string[] platformClauseItems = platform.Split(",");
                for (int i = 0; i < platformClauseItems.Length; i++)
                {
                    if (i > 0)
                    {
                        tempVal += ", ";
                    }
                    string platformLabel = "@Platform" + i;
                    tempVal += platformLabel;
                    whereParams.Add(platformLabel, platformClauseItems[i]);
                }
                tempVal += ")";
                whereClauses.Add(tempVal);
            }

            if (genre.Length > 0)
            {
                tempVal = "Relation_Game_Genres.GenresId IN (";
                string[] genreClauseItems = genre.Split(",");
                for (int i = 0; i < genreClauseItems.Length; i++)
                {
                    if (i > 0)
                    {
                        tempVal += " AND ";
                    }
                    string genreLabel = "@Genre" + i;
                    tempVal += genreLabel;
                    whereParams.Add(genreLabel, genreClauseItems[i]);
                }
                tempVal += ")";
                whereClauses.Add(tempVal);
            }

            if (gamemode.Length > 0)
            {
                tempVal = "Relation_Game_GameModes.GameModesId IN (";
                string[] gameModeClauseItems = gamemode.Split(",");
                for (int i = 0; i < gameModeClauseItems.Length; i++)
                {
                    if (i > 0)
                    {
                        tempVal += " AND ";
                    }
                    string gameModeLabel = "@GameMode" + i;
                    tempVal += gameModeLabel;
                    whereParams.Add(gameModeLabel, gameModeClauseItems[i]);
                }
                tempVal += ")";
                whereClauses.Add(tempVal);
            }

            if (playerperspective.Length > 0)
            {
                tempVal = "Relation_Game_PlayerPerspectives.PlayerPerspectivesId IN (";
                string[] playerPerspectiveClauseItems = playerperspective.Split(",");
                for (int i = 0; i < playerPerspectiveClauseItems.Length; i++)
                {
                    if (i > 0)
                    {
                        tempVal += " AND ";
                    }
                    string playerPerspectiveLabel = "@PlayerPerspective" + i;
                    tempVal += playerPerspectiveLabel;
                    whereParams.Add(playerPerspectiveLabel, playerPerspectiveClauseItems[i]);
                }
                tempVal += ")";
                whereClauses.Add(tempVal);
            }

            if (theme.Length > 0)
            {
                tempVal = "Relation_Game_Themes.ThemesId IN (";
                string[] themeClauseItems = theme.Split(",");
                for (int i = 0; i < themeClauseItems.Length; i++)
                {
                    if (i > 0)
                    {
                        tempVal += " AND ";
                    }
                    string themeLabel = "@Theme" + i;
                    tempVal += themeLabel;
                    whereParams.Add(themeLabel, themeClauseItems[i]);
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
            string orderByClause = "ORDER BY `Name` ASC";
            if (sortdescending == true)
            {
                orderByClause = "ORDER BY `Name` DESC";
            }

            Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT DISTINCT Games_Roms.GameId AS ROMGameId, Game.* FROM Games_Roms LEFT JOIN Game ON Game.Id = Games_Roms.GameId LEFT JOIN Relation_Game_Genres ON Game.Id = Relation_Game_Genres.GameId LEFT JOIN Relation_Game_GameModes ON Game.Id = Relation_Game_GameModes.GameId LEFT JOIN Relation_Game_PlayerPerspectives ON Game.Id = Relation_Game_PlayerPerspectives.GameId LEFT JOIN Relation_Game_Themes ON Game.Id = Relation_Game_Themes.GameId " + whereClause + " " + havingClause + " " + orderByClause;

            List<IGDB.Models.Game> RetVal = new List<IGDB.Models.Game>();

            DataTable dbResponse = db.ExecuteCMD(sql, whereParams);
            foreach (DataRow dr in dbResponse.Rows)
            {
                //RetVal.Add(Classes.Metadata.Games.GetGame((long)dr["ROMGameId"], false, false));
                RetVal.Add(Classes.Metadata.Games.GetGame(dr));
            }

            return RetVal;
        }

        [HttpGet]
        [Route("{GameId}")]
        [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "5Minute")]
        public ActionResult Game(long GameId, bool forceRefresh = false)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, forceRefresh, false, forceRefresh);

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
        [Route("{GameId}/alternativename")]
        [ProducesResponseType(typeof(List<AlternativeName>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public ActionResult GameAlternativeNames(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                if (gameObject.AlternativeNames != null)
                {
                    List<AlternativeName> altNames = new List<AlternativeName>();
                    foreach (long altNameId in gameObject.AlternativeNames.Ids)
                    {
                        altNames.Add(AlternativeNames.GetAlternativeNames(altNameId));
                    }
                    return Ok(altNames);
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
        [ResponseCache(CacheProfileName = "7Days")]
        public ActionResult GameAgeClassification(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

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
                        fileExtension = "svg";
                        fileType = "image/svg+xml";
                        break;
                    case AgeRatingCategory.ACB:
                        fileExtension = "svg";
                        fileType = "image/svg+xml";
                        break;
                    case AgeRatingCategory.CERO:
                        fileExtension = "svg";
                        fileType = "image/svg+xml";
                        break;
                    case AgeRatingCategory.USK:
                        fileExtension = "svg";
                        fileType = "image/svg+xml";
                        break;
                    case AgeRatingCategory.GRAC:
                        fileExtension = "svg";
                        fileType = "image/svg+xml";
                        break;
                    case AgeRatingCategory.CLASS_IND:
                        fileExtension = "svg";
                        fileType = "image/svg+xml";
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
                        Response.Headers.Add("Cache-Control", "public, max-age=604800");

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
        [ResponseCache(CacheProfileName = "7Days")]
        public ActionResult GameArtwork(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

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
        [ResponseCache(CacheProfileName = "7Days")]
        public ActionResult GameArtwork(long GameId, long ArtworkId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

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
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

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
                            Response.Headers.Add("Cache-Control", "public, max-age=604800");

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
        [ResponseCache(CacheProfileName = "7Days")]
        public ActionResult GameCover(long GameId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);
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
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

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
                    Response.Headers.Add("Cache-Control", "public, max-age=604800");

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
        [Route("{GameId}/genre")]
        [ProducesResponseType(typeof(List<Genre>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public ActionResult GameGenre(long GameId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);
                if (gameObject != null)
                {
                    List<IGDB.Models.Genre> genreObjects = new List<Genre>();
                    if (gameObject.Genres != null)
                    {
                        foreach (long genreId in gameObject.Genres.Ids)
                        {
                            genreObjects.Add(Classes.Metadata.Genres.GetGenres(genreId));
                        }
                    }

                    List<IGDB.Models.Genre> sortedGenreObjects = genreObjects.OrderBy(o => o.Name).ToList();

                    return Ok(sortedGenreObjects);
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
        [Route("{GameId}/companies")]
        [ProducesResponseType(typeof(List<Dictionary<string, object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public ActionResult GameInvolvedCompanies(long GameId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);
                if (gameObject != null)
                {
                    List<Dictionary<string, object>> icObjects = new List<Dictionary<string, object>>();
                    if (gameObject.InvolvedCompanies != null)
                    {
                        foreach (long icId in gameObject.InvolvedCompanies.Ids)
                        {
                            InvolvedCompany involvedCompany = Classes.Metadata.InvolvedCompanies.GetInvolvedCompanies(icId);
                            Company company = Classes.Metadata.Companies.GetCompanies(involvedCompany.Company.Id);
                            company.Developed = null;
                            company.Published = null;

                            Dictionary<string, object> companyData = new Dictionary<string, object>();
                            companyData.Add("involvement", involvedCompany);
                            companyData.Add("company", company);

                            icObjects.Add(companyData);
                        }
                    }

                    return Ok(icObjects);
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
        [Route("{GameId}/companies/{CompanyId}")]
        [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public ActionResult GameInvolvedCompanies(long GameId, long CompanyId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);
                if (gameObject != null)
                {
                    List<Dictionary<string, object>> icObjects = new List<Dictionary<string, object>>();
                    if (gameObject.InvolvedCompanies != null)
                    {
                        InvolvedCompany involvedCompany = Classes.Metadata.InvolvedCompanies.GetInvolvedCompanies(CompanyId);
                        Company company = Classes.Metadata.Companies.GetCompanies(involvedCompany.Company.Id);
                        company.Developed = null;
                        company.Published = null;

                        Dictionary<string, object> companyData = new Dictionary<string, object>();
                        companyData.Add("involvement", involvedCompany);
                        companyData.Add("company", company);

                        return Ok(companyData);
                    } else
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
        [Route("{GameId}/companies/{CompanyId}/image")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameCompanyImage(long GameId, long CompanyId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                InvolvedCompany involvedCompany = Classes.Metadata.InvolvedCompanies.GetInvolvedCompanies(CompanyId);
                Company company = Classes.Metadata.Companies.GetCompanies(involvedCompany.Company.Id);

                string coverFilePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Company(company), "Logo_Medium.png");
                if (System.IO.File.Exists(coverFilePath))
                {
                    string filename = "Logo.png";
                    string filepath = coverFilePath;
                    byte[] filedata = System.IO.File.ReadAllBytes(filepath);
                    string contentType = "image/png";

                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = filename,
                        Inline = true,
                    };

                    Response.Headers.Add("Content-Disposition", cd.ToString());
                    Response.Headers.Add("Cache-Control", "public, max-age=604800");

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
        //[ResponseCache(CacheProfileName = "5Minute")]
        public ActionResult GameRom(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

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
        //[ResponseCache(CacheProfileName = "5Minute")]
        public ActionResult GameRom(long GameId, long RomId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

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
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

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
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

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
        [HttpHead]
        [Route("{GameId}/roms/{RomId}/file")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameRomFile(long GameId, long RomId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);
                if (rom.GameId != GameId)
                {
                    return NotFound();
                }

                string romFilePath = rom.Path;
                if (System.IO.File.Exists(romFilePath))
                {
                    FileStream content = new FileStream(romFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    FileStreamResult response = File(content, "application/octet-stream", rom.Name);
                    return response;
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
        [HttpHead]
        [Route("{GameId}/roms/{RomId}/{FileName}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameRomFile(long GameId, long RomId, string FileName)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);
                if (rom.GameId != GameId || rom.Name != FileName)
                {
                    return NotFound();
                }

                string romFilePath = rom.Path;
                if (System.IO.File.Exists(romFilePath))
                {
                    FileStream content = new FileStream(romFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    FileStreamResult response = File(content, "application/octet-stream", rom.Name);
                    return response;
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
        [Route("search")]
        [ProducesResponseType(typeof(List<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GameSearch(long RomId = 0, string SearchString = "")
        {
            try
            {
                if (RomId > 0)
                {
                    Classes.Roms.GameRomItem romItem = Classes.Roms.GetRom(RomId);
                    Common.hashObject hash = new Common.hashObject(romItem.Path);
                    Models.Signatures_Games romSig = Classes.ImportGame.GetFileSignature(hash, new FileInfo(romItem.Path), romItem.Path);
                    List<Game> searchResults = Classes.ImportGame.SearchForGame_GetAll(romSig.Game.Name, romSig.Flags.IGDBPlatformId);

                    return Ok(searchResults);
                }
                else
                {
                    if (SearchString.Length > 0)
                    {
                        List<Game> searchResults = Classes.ImportGame.SearchForGame_GetAll(SearchString, 0);

                        return Ok(searchResults);
                    }
                    else
                    {
                        return NotFound();
                    }
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
        [ResponseCache(CacheProfileName = "7Days")]
        public ActionResult GameScreenshot(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

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
        [ResponseCache(CacheProfileName = "7Days")]
        public ActionResult GameScreenshot(long GameId, long ScreenshotId)
        {
            try
            { 
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);
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
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

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
                    Response.Headers.Add("Cache-Control", "public, max-age=604800");

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
        [ResponseCache(CacheProfileName = "7Days")]
        public ActionResult GameVideo(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

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
