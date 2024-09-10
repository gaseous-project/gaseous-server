using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Authentication;
using gaseous_server.Classes;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using IGDB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using static gaseous_server.Classes.Metadata.AgeRatings;
using Asp.Versioning;
using static gaseous_server.Models.PlatformMapping;

namespace gaseous_server.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiVersion("1.1")]
    [Authorize]
    [ApiController]
    public class GamesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public GamesController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [MapToApiVersion("1.0")]
        [HttpGet]
        [ProducesResponseType(typeof(List<Game>), StatusCodes.Status200OK)]
        public async Task<ActionResult> Game(
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

            return Ok(GetGames(name, platform, genre, gamemode, playerperspective, theme, minrating, maxrating, "Adult", true, true, sortdescending));
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
            string ratinggroup = "Adult",
            bool includenullrating = true,
            bool sortbynamethe = false,
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
                tempVal = "view_Games_Roms.PlatformId IN (";
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

            if (ratinggroup.Length > 0)
            {
                List<long> AgeClassificationsList = new List<long>();
                foreach (string ratingGroup in ratinggroup.Split(','))
                {
                    AgeGroups.AgeRestrictionGroupings ageRestriction = (AgeGroups.AgeRestrictionGroupings)Enum.Parse(typeof(AgeGroups.AgeRestrictionGroupings), ratingGroup);
                    if (AgeGroups.AgeGroupings.ContainsKey(ageRestriction))
                    {
                        List<AgeGroups.AgeGroupItem> ageGroups = AgeGroups.AgeGroupings[ageRestriction];
                        foreach (AgeGroups.AgeGroupItem ageGroup in ageGroups)
                        {
                            AgeClassificationsList.AddRange(ageGroup.AgeGroupItemValues);
                        }
                    }
                }

                if (AgeClassificationsList.Count > 0)
                {
                    tempVal = "(view_AgeRatings.Rating IN (";
                    for (int i = 0; i < AgeClassificationsList.Count; i++)
                    {
                        if (i > 0)
                        {
                            tempVal += ", ";
                        }
                        string themeLabel = "@Rating" + i;
                        tempVal += themeLabel;
                        whereParams.Add(themeLabel, AgeClassificationsList[i]);
                    }
                    tempVal += ")";

                    tempVal += " OR ";

                    if (includenullrating == true)
                    {
                        tempVal += "view_AgeRatings.Rating IS NULL";
                    }
                    else
                    {
                        tempVal += "view_AgeRatings.Rating IS NOT NULL";
                    }
                    tempVal += ")";

                    whereClauses.Add(tempVal);
                }
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
            string orderByField = "Name";
            if (sortbynamethe == true)
            {
                orderByField = "NameThe";
            }
            string orderByClause = "ORDER BY `" + orderByField + "` ASC";
            if (sortdescending == true)
            {
                orderByClause = "ORDER BY `" + orderByField + "` DESC";
            }

            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT DISTINCT view_Games_Roms.GameId AS ROMGameId, Game.*, case when Game.`Name` like 'The %' then CONCAT(trim(substr(Game.`Name` from 4)), ', The') else Game.`Name` end as NameThe FROM view_Games_Roms LEFT JOIN Game ON Game.Id = view_Games_Roms.GameId LEFT JOIN Relation_Game_Genres ON Game.Id = Relation_Game_Genres.GameId LEFT JOIN Relation_Game_GameModes ON Game.Id = Relation_Game_GameModes.GameId LEFT JOIN Relation_Game_PlayerPerspectives ON Game.Id = Relation_Game_PlayerPerspectives.GameId LEFT JOIN Relation_Game_Themes ON Game.Id = Relation_Game_Themes.GameId LEFT JOIN (SELECT Relation_Game_AgeRatings.GameId, AgeRating.* FROM Relation_Game_AgeRatings JOIN AgeRating ON Relation_Game_AgeRatings.AgeRatingsId = AgeRating.Id) view_AgeRatings ON Game.Id = view_AgeRatings.GameId " + whereClause + " " + havingClause + " " + orderByClause;

            List<IGDB.Models.Game> RetVal = new List<IGDB.Models.Game>();

            DataTable dbResponse = db.ExecuteCMD(sql, whereParams);
            foreach (DataRow dr in dbResponse.Rows)
            {
                //RetVal.Add(Classes.Metadata.Games.GetGame((long)dr["ROMGameId"], false, false));
                RetVal.Add(Classes.Metadata.Games.GetGame(dr));
            }

            return RetVal;
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}")]
        [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "5Minute")]
        public async Task<ActionResult> Game(long GameId)
        {
            try
            {
                Game game = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                if (game != null)
                {
                    return Ok(game);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/alternativename")]
        [ProducesResponseType(typeof(List<AlternativeName>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameAlternativeNames(long GameId)
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/agerating")]
        [ProducesResponseType(typeof(List<AgeRatings.GameAgeRating>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameAgeClassification(long GameId)
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/artwork")]
        [ProducesResponseType(typeof(List<Artwork>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameArtwork(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                List<Artwork> artworks = new List<Artwork>();
                if (gameObject.Artworks != null)
                {
                    foreach (long ArtworkId in gameObject.Artworks.Ids)
                    {
                        Artwork GameArtwork = Artworks.GetArtwork(ArtworkId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), false);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/artwork/{ArtworkId}")]
        [ProducesResponseType(typeof(Artwork), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameArtwork(long GameId, long ArtworkId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                try
                {
                    IGDB.Models.Artwork artworkObject = Artworks.GetArtwork(ArtworkId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), false);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/cover")]
        [ProducesResponseType(typeof(Cover), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameCover(long GameId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);
                if (gameObject != null)
                {
                    IGDB.Models.Cover coverObject = Covers.GetCover(gameObject.Cover.Id, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), false);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/{ImageType}/{ImageId}/image/{size}")]
        [Route("{GameId}/{ImageType}/{ImageId}/image/{size}/{imagename}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameImage(long GameId, MetadataImageType imageType, long ImageId, Communications.IGDBAPI_ImageSize size, string imagename = "")
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                string? imageId = null;
                string? imageTypePath = null;

                switch (imageType)
                {
                    case MetadataImageType.cover:
                        if (gameObject.Cover != null)
                        {
                            if (gameObject.Cover.Id != null)
                            {
                                IGDB.Models.Cover cover = Classes.Metadata.Covers.GetCover(gameObject.Cover.Id, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), false);
                                imageId = cover.ImageId;
                                imageTypePath = "Covers";
                            }
                        }
                        else
                        {
                            return NotFound();
                        }
                        break;

                    case MetadataImageType.screenshots:
                        if (gameObject.Screenshots != null)
                        {
                            if (gameObject.Screenshots.Ids.Contains(ImageId))
                            {
                                IGDB.Models.Screenshot imageObject = Screenshots.GetScreenshot(ImageId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), true);

                                imageId = imageObject.ImageId;
                                imageTypePath = "Screenshots";
                            }
                        }
                        else
                        {
                            return NotFound();
                        }
                        break;

                    case MetadataImageType.artwork:
                        if (gameObject.Artworks != null)
                        {
                            if (gameObject.Artworks.Ids.Contains(ImageId))
                            {
                                IGDB.Models.Artwork imageObject = Artworks.GetArtwork(ImageId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), true);

                                imageId = imageObject.ImageId;
                                imageTypePath = "Artwork";
                            }
                        }
                        else
                        {
                            return NotFound();
                        }
                        break;

                    default:
                        return NotFound();
                }

                if (imageId == null)
                {
                    return NotFound();
                }

                string basePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), imageTypePath);
                string imagePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), imageTypePath, size.ToString(), imageId + ".jpg");

                if (!System.IO.File.Exists(imagePath))
                {
                    Communications comms = new Communications();
                    Task<string> ImgFetch = comms.GetSpecificImageFromServer(Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), imageTypePath), imageId, size, new List<Communications.IGDBAPI_ImageSize> { Communications.IGDBAPI_ImageSize.cover_big, Communications.IGDBAPI_ImageSize.original });

                    imagePath = ImgFetch.Result;
                }

                if (!System.IO.File.Exists(imagePath))
                {
                    Communications comms = new Communications();
                    Task<string> ImgFetch = comms.GetSpecificImageFromServer(basePath, imageId, size, new List<Communications.IGDBAPI_ImageSize> { Communications.IGDBAPI_ImageSize.cover_big, Communications.IGDBAPI_ImageSize.original });

                    imagePath = ImgFetch.Result;
                }

                if (System.IO.File.Exists(imagePath))
                {
                    string filename = imageId + ".jpg";
                    string filepath = imagePath;
                    string contentType = "image/jpg";

                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = filename,
                        Inline = true,
                    };

                    Response.Headers.Add("Content-Disposition", cd.ToString());
                    Response.Headers.Add("Cache-Control", "public, max-age=604800");

                    byte[] filedata = null;
                    using (FileStream fs = System.IO.File.OpenRead(filepath))
                    {
                        using (BinaryReader binaryReader = new BinaryReader(fs))
                        {
                            filedata = binaryReader.ReadBytes((int)fs.Length);
                        }
                    }

                    return File(filedata, contentType);
                }

                return NotFound();
            }
            catch
            {
                return NotFound();
            }
        }

        public enum MetadataImageType
        {
            cover,
            screenshots,
            artwork
        }


        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/favourite")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameGetFavouriteAsync(long GameId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                if (gameObject != null)
                {
                    var user = await _userManager.GetUserAsync(User);

                    if (user != null)
                    {
                        Favourites favourites = new Favourites();
                        return Ok(favourites.GetFavourite(user.Id, GameId));
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Route("{GameId}/favourite")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameSetFavouriteAsync(long GameId, bool favourite)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                if (gameObject != null)
                {
                    var user = await _userManager.GetUserAsync(User);

                    if (user != null)
                    {
                        Favourites favourites = new Favourites();
                        return Ok(favourites.SetFavourite(user.Id, GameId, favourite));
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/genre")]
        [ProducesResponseType(typeof(List<Genre>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameGenre(long GameId)
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/companies")]
        [ProducesResponseType(typeof(List<Dictionary<string, object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameInvolvedCompanies(long GameId)
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/companies/{CompanyId}")]
        [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameInvolvedCompanies(long GameId, long CompanyId)
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/companies/{CompanyId}/image")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameCompanyImage(long GameId, long CompanyId)
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/emulatorconfiguration/{PlatformId}")]
        [Authorize]
        [ProducesResponseType(typeof(UserEmulatorConfiguration), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetGameEmulator(long GameId, long PlatformId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                if (gameObject != null)
                {
                    IGDB.Models.Platform platformObject = Classes.Metadata.Platforms.GetPlatform(PlatformId);

                    if (platformObject != null)
                    {
                        var user = await _userManager.GetUserAsync(User);

                        if (user != null)
                        {
                            PlatformMapping platformMapping = new PlatformMapping();
                            UserEmulatorConfiguration platformMappingObject = platformMapping.GetUserEmulator(user.Id, GameId, PlatformId);

                            if (platformMappingObject != null)
                            {
                                return Ok(platformMappingObject);
                            }
                        }
                    }
                }

                return NotFound();
            }
            catch
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Route("{GameId}/emulatorconfiguration/{PlatformId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SetGameEmulator(long GameId, long PlatformId, UserEmulatorConfiguration configuration)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                if (gameObject != null)
                {
                    IGDB.Models.Platform platformObject = Classes.Metadata.Platforms.GetPlatform(PlatformId);

                    if (platformObject != null)
                    {
                        var user = await _userManager.GetUserAsync(User);

                        if (user != null)
                        {
                            PlatformMapping platformMapping = new PlatformMapping();
                            platformMapping.SetUserEmulator(user.Id, GameId, PlatformId, configuration);

                            return Ok();
                        }
                    }
                }

                return NotFound();
            }
            catch
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpDelete]
        [Route("{GameId}/emulatorconfiguration/{PlatformId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteGameEmulator(long GameId, long PlatformId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                if (gameObject != null)
                {
                    IGDB.Models.Platform platformObject = Classes.Metadata.Platforms.GetPlatform(PlatformId);

                    if (platformObject != null)
                    {
                        var user = await _userManager.GetUserAsync(User);

                        if (user != null)
                        {
                            PlatformMapping platformMapping = new PlatformMapping();
                            platformMapping.DeleteUserEmulator(user.Id, GameId, PlatformId);

                            return Ok();
                        }
                    }
                }

                return NotFound();
            }
            catch
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/platforms")]
        [ProducesResponseType(typeof(List<Games.AvailablePlatformItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GamePlatforms(long GameId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    return Ok(Games.GetAvailablePlatforms(user.Id, GameId));
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/releasedates")]
        [ProducesResponseType(typeof(List<ReleaseDate>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameReleaseDates(long GameId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);
                if (gameObject != null)
                {
                    List<ReleaseDate> rdObjects = new List<ReleaseDate>();
                    if (gameObject.ReleaseDates != null)
                    {
                        foreach (long icId in gameObject.ReleaseDates.Ids)
                        {
                            ReleaseDate releaseDate = Classes.Metadata.ReleaseDates.GetReleaseDates(icId);

                            rdObjects.Add(releaseDate);
                        }
                    }

                    return Ok(rdObjects);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/roms")]
        [ProducesResponseType(typeof(Classes.Roms.GameRomObject), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ResponseCache(CacheProfileName = "5Minute")]
        public async Task<ActionResult> GameRomAsync(long GameId, int pageNumber = 0, int pageSize = 0, long PlatformId = -1, string NameSearch = "")
        {
            var user = await _userManager.GetUserAsync(User);

            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                return Ok(Classes.Roms.GetRoms(GameId, PlatformId, NameSearch, pageNumber, pageSize, user.Id));
            }
            catch
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/roms/{RomId}")]
        [ProducesResponseType(typeof(Classes.Roms.GameRomItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ResponseCache(CacheProfileName = "5Minute")]
        public async Task<ActionResult> GameRom(long GameId, long RomId)
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPatch]
        [Authorize(Roles = "Admin,Gamer")]
        [Route("{GameId}/roms/{RomId}")]
        [ProducesResponseType(typeof(Classes.Roms.GameRomItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomRename(long GameId, long RomId, long NewPlatformId, long NewGameId)
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpDelete]
        [Authorize(Roles = "Admin,Gamer")]
        [Route("{GameId}/roms/{RomId}")]
        [ProducesResponseType(typeof(Classes.Roms.GameRomItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomDelete(long GameId, long RomId)
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Route("{GameId}/roms/{RomId}/{PlatformId}/favourite")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomFavourite(long GameId, long RomId, long PlatformId, bool IsMediaGroup, bool favourite)
        {
            try
            {
                ApplicationUser? user = await _userManager.GetUserAsync(User);

                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                if (IsMediaGroup == false)
                {
                    Classes.Roms.GameRomItem rom = Classes.Roms.GetRom(RomId);
                    if (rom.GameId == GameId)
                    {
                        if (favourite == true)
                        {
                            Classes.Metadata.Games.GameSetFavouriteRom(user.Id, GameId, PlatformId, RomId, IsMediaGroup);
                        }
                        else
                        {
                            Classes.Metadata.Games.GameClearFavouriteRom(user.Id, GameId, PlatformId);
                        }
                        return Ok();
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                else
                {
                    Classes.RomMediaGroup.GameRomMediaGroupItem rom = Classes.RomMediaGroup.GetMediaGroup(RomId, user.Id);
                    if (rom.GameId == GameId)
                    {
                        if (favourite == true)
                        {
                            Classes.Metadata.Games.GameSetFavouriteRom(user.Id, GameId, PlatformId, RomId, IsMediaGroup);
                        }
                        else
                        {
                            Classes.Metadata.Games.GameClearFavouriteRom(user.Id, GameId, PlatformId);
                        }
                        return Ok();
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpHead]
        [Route("{GameId}/roms/{RomId}/file")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomFile(long GameId, long RomId)
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpHead]
        [Route("{GameId}/roms/{RomId}/{FileName}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomFile(long GameId, long RomId, string FileName)
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/romgroup/{RomGroupId}")]
        [ProducesResponseType(typeof(Classes.RomMediaGroup.GameRomMediaGroupItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ResponseCache(CacheProfileName = "5Minute")]
        public async Task<ActionResult> GameRomGroupAsync(long GameId, long RomGroupId)
        {
            var user = await _userManager.GetUserAsync(User);

            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                Classes.RomMediaGroup.GameRomMediaGroupItem rom = Classes.RomMediaGroup.GetMediaGroup(RomGroupId, user.Id);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/romgroup")]
        [ProducesResponseType(typeof(List<RomMediaGroup.GameRomMediaGroupItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetGameRomGroupAsync(long GameId, long? PlatformId = null)
        {
            var user = await _userManager.GetUserAsync(User);

            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                try
                {
                    return Ok(RomMediaGroup.GetMediaGroupsFromGameId(GameId, user.Id, PlatformId));
                }
                catch (Exception ex)
                {
                    Logging.Log(Logging.LogType.Critical, "Rom Group", "An error occurred", ex);
                    return NotFound();
                }
            }
            catch
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Authorize(Roles = "Admin,Gamer")]
        [Route("{GameId}/romgroup")]
        [ProducesResponseType(typeof(Classes.RomMediaGroup.GameRomMediaGroupItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> NewGameRomGroup(long GameId, long PlatformId, [FromBody] List<long> RomIds)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                try
                {
                    Classes.RomMediaGroup.GameRomMediaGroupItem rom = Classes.RomMediaGroup.CreateMediaGroup(GameId, PlatformId, RomIds);
                    return Ok(rom);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPatch]
        [Authorize(Roles = "Admin,Gamer")]
        [Route("{GameId}/romgroup/{RomId}")]
        [ProducesResponseType(typeof(Classes.RomMediaGroup.GameRomMediaGroupItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomGroupMembersAsync(long GameId, long RomGroupId, [FromBody] List<long> RomIds)
        {
            var user = await _userManager.GetUserAsync(User);

            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                Classes.RomMediaGroup.GameRomMediaGroupItem rom = Classes.RomMediaGroup.GetMediaGroup(RomGroupId, user.Id);
                if (rom.GameId == GameId)
                {
                    rom = Classes.RomMediaGroup.EditMediaGroup(RomGroupId, RomIds);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpDelete]
        [Authorize(Roles = "Admin,Gamer")]
        [Route("{GameId}/romgroup/{RomGroupId}")]
        [ProducesResponseType(typeof(Classes.RomMediaGroup.GameRomMediaGroupItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomGroupDelete(long GameId, long RomGroupId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                Classes.RomMediaGroup.GameRomMediaGroupItem rom = Classes.RomMediaGroup.GetMediaGroup(RomGroupId);
                if (rom.GameId == GameId)
                {
                    Classes.RomMediaGroup.DeleteMediaGroup(RomGroupId);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpHead]
        [Route("{GameId}/romgroup/{RomGroupId}/file")]
        [Route("{GameId}/romgroup/{RomGroupId}/{filename}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomGroupFile(long GameId, long RomGroupId, string filename = "")
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                Classes.RomMediaGroup.GameRomMediaGroupItem rom = Classes.RomMediaGroup.GetMediaGroup(RomGroupId);
                if (rom.GameId != GameId)
                {
                    return NotFound();
                }

                string romFilePath = Path.Combine(Config.LibraryConfiguration.LibraryMediaGroupDirectory, RomGroupId + ".zip");
                if (System.IO.File.Exists(romFilePath))
                {
                    FileStream content = new FileStream(romFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    string returnFileName = "";
                    if (filename == "")
                    {
                        returnFileName = gameObject.Name + ".zip";
                    }
                    else
                    {
                        returnFileName = filename;
                    }
                    FileStreamResult response = File(content, "application/octet-stream", returnFileName);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("search")]
        [ProducesResponseType(typeof(List<Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameSearch(long RomId = 0, string SearchString = "")
        {
            try
            {
                if (RomId > 0)
                {
                    Classes.Roms.GameRomItem romItem = Classes.Roms.GetRom(RomId);
                    Common.hashObject hash = new Common.hashObject(romItem.Path);
                    FileSignature fileSignature = new FileSignature();
                    gaseous_server.Models.Signatures_Games romSig = fileSignature.GetFileSignature(romItem.Library, hash, new FileInfo(romItem.Path), romItem.Path);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/screenshots")]
        [ProducesResponseType(typeof(List<Screenshot>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameScreenshot(long GameId)
        {
            try
            {
                Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

                List<Screenshot> screenshots = new List<Screenshot>();
                if (gameObject.Screenshots != null)
                {
                    foreach (long ScreenshotId in gameObject.Screenshots.Ids)
                    {
                        Screenshot GameScreenshot = Screenshots.GetScreenshot(ScreenshotId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), false);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/screenshots/{ScreenshotId}")]
        [ProducesResponseType(typeof(Screenshot), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameScreenshot(long GameId, long ScreenshotId)
        {
            try
            {
                IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);
                if (gameObject != null)
                {
                    IGDB.Models.Screenshot screenshotObject = Screenshots.GetScreenshot(ScreenshotId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), false);
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

        // [MapToApiVersion("1.0")]
        // [MapToApiVersion("1.1")]
        // [HttpGet]
        // [Route("{GameId}/screenshots/{ScreenshotId}/image/{size}")]
        // [Route("{GameId}/screenshots/{ScreenshotId}/image/{size}/{ImageName}")]
        // [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        // [ProducesResponseType(StatusCodes.Status404NotFound)]
        // public async Task<ActionResult> GameScreenshotImage(long GameId, long ScreenshotId, Communications.IGDBAPI_ImageSize Size, string ImageName)
        // {
        //     try
        //     {
        //         IGDB.Models.Game gameObject = Classes.Metadata.Games.GetGame(GameId, false, false, false);

        //         IGDB.Models.Screenshot screenshotObject = Screenshots.GetScreenshot(ScreenshotId, Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), true);

        //         string basePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Game(gameObject), "Screenshots");

        //         Communications comms = new Communications();
        //         Task<string> ImgFetch = comms.GetSpecificImageFromServer(basePath, screenshotObject.ImageId, Size, new List<Communications.IGDBAPI_ImageSize> { Communications.IGDBAPI_ImageSize.original });

        //         string coverFilePath = ImgFetch.Result;

        //         if (System.IO.File.Exists(coverFilePath))
        //         {
        //             string filename = screenshotObject.ImageId + ".jpg";
        //             string filepath = coverFilePath;
        //             byte[] filedata = System.IO.File.ReadAllBytes(filepath);
        //             string contentType = "image/jpg";

        //             var cd = new System.Net.Mime.ContentDisposition
        //             {
        //                 FileName = filename,
        //                 Inline = true,
        //             };

        //             Response.Headers.Add("Content-Disposition", cd.ToString());
        //             Response.Headers.Add("Cache-Control", "public, max-age=604800");

        //             return File(filedata, contentType);
        //         }
        //         else
        //         {
        //             return NotFound();
        //         }
        //     }
        //     catch
        //     {
        //         return NotFound();
        //     }
        // }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{GameId}/videos")]
        [ProducesResponseType(typeof(List<GameVideo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameVideo(long GameId)
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
