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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using static gaseous_server.Classes.Metadata.AgeRatings;
using Asp.Versioning;
using static gaseous_server.Models.PlatformMapping;
using HasheousClient.Models.Metadata.IGDB;
using HasheousClient;

namespace gaseous_server.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
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
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{MetadataMapId}")]
        [ProducesResponseType(typeof(gaseous_server.Models.Game), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "None")]
        public async Task<ActionResult> Game(long MetadataMapId)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).PreferredMetadataMapItem;
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId, true);

                // apply user specific localisation
                if (game.GameLocalizations != null && game.GameLocalizations.Count > 0)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        string? userLocale = user.UserPreferences?.Find(x => x.Setting == "User.Locale")?.Value;
                        if (userLocale != null)
                        {
                            // userLocale is in a serliazed format, so we need to deserialize it - but since it's the only thing, we can simply remove the quotes
                            userLocale = userLocale.Replace("\"", "");

                            GameLocalization? gameLocalization = null;
                            Region? gameRegion = null;
                            foreach (long locId in game.GameLocalizations)
                            {
                                GameLocalization? loc = await GameLocalizations.GetGame_Locatization(game.MetadataSource, locId);
                                if (loc != null)
                                {
                                    Region? region = await Regions.GetGame_Region(game.MetadataSource, loc.Region);
                                    if (region != null)
                                    {
                                        if (region.Identifier == userLocale)
                                        {
                                            gameLocalization = loc;
                                            gameRegion = region;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (gameLocalization != null && gameRegion != null)
                            {
                                if (gameLocalization.Name != null && gameLocalization.Name != "")
                                {
                                    game.Name = gameLocalization.Name;
                                }
                                if (gameLocalization.Cover != null && gameLocalization.Cover != 0)
                                {
                                    game.Cover = gameLocalization.Cover;
                                }
                            }
                        }
                    }
                }

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
        [Route("{MetadataMapId}/{MetadataSource}/alternativename")]
        [ProducesResponseType(typeof(List<AlternativeName>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameAlternativeNames(long MetadataMapId, FileSignature.MetadataSources MetadataSource)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);

                List<AlternativeName> altNames = new List<AlternativeName>();

                // add default game name
                AlternativeName defaultName = new AlternativeName();
                defaultName.Name = game.Name;
                defaultName.Comment = "Default";
                altNames.Add(defaultName);

                if (game.AlternativeNames != null)
                {
                    foreach (long altNameId in game.AlternativeNames)
                    {
                        AlternativeName altName = await AlternativeNames.GetAlternativeNames(game.MetadataSource, altNameId);

                        // make sure the name is not already in the list of alternative names
                        if (altNames.FirstOrDefault(x => x.Name == altName.Name) == null)
                        {
                            altNames.Add(altName);
                        }
                    }

                    // add localized names
                    if (game.GameLocalizations != null)
                    {
                        foreach (long locId in game.GameLocalizations)
                        {
                            GameLocalization loc = await GameLocalizations.GetGame_Locatization(game.MetadataSource, locId);
                            if (loc != null)
                            {
                                // make sure loc.Name is not already in the list of alternative names
                                if (altNames.FirstOrDefault(x => x.Name == loc.Name) == null)
                                {
                                    // get localisation region
                                    Region region = await Regions.GetGame_Region(game.MetadataSource, loc.Region);

                                    // add the localized name to the list of alternative names
                                    AlternativeName locAltName = new AlternativeName();
                                    locAltName.Name = loc.Name;
                                    locAltName.Comment = region.Name;
                                    altNames.Add(locAltName);
                                }
                            }
                        }
                    }
                }

                return Ok(altNames);
            }
            catch
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{MetadataMapId}/{MetadataSource}/agerating")]
        [ProducesResponseType(typeof(List<AgeRatings.GameAgeRating>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameAgeClassification(long MetadataMapId, FileSignature.MetadataSources MetadataSource)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);

                if (game.AgeRatings != null)
                {
                    List<AgeRatings.GameAgeRating> ageRatings = new List<AgeRatings.GameAgeRating>();
                    foreach (long ageRatingId in game.AgeRatings)
                    {
                        ageRatings.Add(await AgeRatings.GetConsolidatedAgeRating(game.MetadataSource, ageRatingId));
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
        [Route("{MetadataMapId}/{MetadataSource}/artwork")]
        [ProducesResponseType(typeof(List<Artwork>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameArtwork(long MetadataMapId, FileSignature.MetadataSources MetadataSource)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);

                List<Artwork> artworks = new List<Artwork>();
                if (game.Artworks != null)
                {
                    foreach (long ArtworkId in game.Artworks)
                    {
                        Artwork GameArtwork = await Artworks.GetArtwork(game.MetadataSource, ArtworkId);
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
        [Route("{MetadataMapId}/{MetadataSource}/artwork/{ArtworkId}")]
        [ProducesResponseType(typeof(Artwork), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameArtwork(long MetadataMapId, FileSignature.MetadataSources MetadataSource, long ArtworkId)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);

                try
                {
                    Artwork artworkObject = await Artworks.GetArtwork(game.MetadataSource, ArtworkId);
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
        [Route("{MetadataMapId}/{MetadataSource}/cover")]
        [ProducesResponseType(typeof(Cover), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameCover(long MetadataMapId, FileSignature.MetadataSources MetadataSource)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);
                if (game != null)
                {
                    Cover coverObject = await Covers.GetCover(game.MetadataSource, (long?)game.Cover);
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
        [Route("{MetadataMapId}/{MetadataSource}/{ImageType}/{ImageId}/image/{size}")]
        [Route("{MetadataMapId}/{MetadataSource}/{ImageType}/{ImageId}/image/{size}/{imagename}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameImage(long MetadataMapId, FileSignature.MetadataSources MetadataSource, ImageHandling.MetadataImageType imageType, long ImageId, Communications.IGDBAPI_ImageSize size, string imagename = "")
        {
            try
            {
                Dictionary<string, string>? imgData = await ImageHandling.GameImage(MetadataMapId, MetadataSource, imageType, ImageId, size, imagename);

                if (System.IO.File.Exists(imgData["imagePath"]))
                {
                    string filename = imgData["imageId"] + ".jpg";
                    string filepath = imgData["imagePath"];
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{MetadataMapId}/favourite")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameGetFavouriteAsync(long MetadataMapId)
        {
            try
            {
                MetadataMap? metadata = await MetadataManagement.GetMetadataMap(MetadataMapId);
                MetadataMap.MetadataMapItem? metadataMap = metadata?.PreferredMetadataMapItem;
                List<long> associatedMetadataMapIds = await MetadataManagement.GetAssociatedMetadataMapIds(MetadataMapId);

                if (metadataMap != null)
                {
                    var user = await _userManager.GetUserAsync(User);

                    if (user != null)
                    {
                        Favourites favourites = new Favourites();

                        foreach (long associatedMetadataMapId in associatedMetadataMapIds)
                        {
                            bool favourite = favourites.GetFavourite(user.Id, associatedMetadataMapId);
                            if (favourite)
                            {
                                return Ok(favourite);
                            }
                        }

                        return Ok(false);
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
        [Route("{MetadataMapId}/favourite")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameSetFavouriteAsync(long MetadataMapId, bool favourite)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).PreferredMetadataMapItem;

                if (metadataMap != null)
                {
                    var user = await _userManager.GetUserAsync(User);

                    if (user != null)
                    {
                        Favourites favourites = new Favourites();

                        // clear all favourite associated with this metadata id
                        if (!favourite)
                        {
                            List<long> associatedMetadataMapIds = await MetadataManagement.GetAssociatedMetadataMapIds(MetadataMapId);
                            foreach (long associatedMetadataMapId in associatedMetadataMapIds)
                            {
                                favourites.SetFavourite(user.Id, associatedMetadataMapId, favourite);
                            }
                        }

                        return Ok(favourites.SetFavourite(user.Id, MetadataMapId, favourite));
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
        [Route("{MetadataMapId}/{MetadataSource}/gamemode")]
        [ProducesResponseType(typeof(List<GameMode>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameMode(long MetadataMapId, FileSignature.MetadataSources MetadataSource)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);
                if (game != null)
                {
                    List<GameMode> gameModeObjects = new List<GameMode>();
                    if (game.GameModes != null)
                    {
                        foreach (long gameModeId in game.GameModes)
                        {
                            gameModeObjects.Add(await Classes.Metadata.GameModes.GetGame_Modes(game.MetadataSource, gameModeId));
                        }
                    }

                    return Ok(gameModeObjects);
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
        [Route("{MetadataMapId}/{MetadataSource}/genre")]
        [ProducesResponseType(typeof(List<Genre>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameGenre(long MetadataMapId, FileSignature.MetadataSources MetadataSource)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);
                if (game != null)
                {
                    List<Genre> genreObjects = new List<Genre>();
                    if (game.Genres != null)
                    {
                        foreach (long genreId in game.Genres)
                        {
                            genreObjects.Add(await Classes.Metadata.Genres.GetGenres(game.MetadataSource, genreId));
                        }
                    }

                    List<Genre> sortedGenreObjects = genreObjects.OrderBy(o => o.Name).ToList();

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
        [Route("{MetadataMapId}/{MetadataSource}/themes")]
        [ProducesResponseType(typeof(List<Theme>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameThemes(long MetadataMapId, FileSignature.MetadataSources MetadataSource)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);
                if (game != null)
                {
                    List<Theme> themeObjects = new List<Theme>();
                    if (game.Themes != null)
                    {
                        foreach (long themeId in game.Themes)
                        {
                            themeObjects.Add(await Classes.Metadata.Themes.GetGame_ThemesAsync(game.MetadataSource, themeId));
                        }
                    }

                    List<Theme> sortedThemeObjects = themeObjects.OrderBy(o => o.Name).ToList();

                    return Ok(sortedThemeObjects);
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
        [Route("{MetadataMapId}/{MetadataSource}/companies")]
        [ProducesResponseType(typeof(List<Dictionary<string, object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameInvolvedCompanies(long MetadataMapId, FileSignature.MetadataSources MetadataSource)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);
                if (game != null)
                {
                    List<Dictionary<string, object>> icObjects = new List<Dictionary<string, object>>();
                    if (game.InvolvedCompanies != null)
                    {
                        foreach (long icId in game.InvolvedCompanies)
                        {
                            try
                            {
                                InvolvedCompany involvedCompany = await Classes.Metadata.InvolvedCompanies.GetInvolvedCompanies(icId);
                                Company company = await Classes.Metadata.Companies.GetCompanies(game.MetadataSource, (long?)involvedCompany.Company);
                                company.Developed = null;
                                company.Published = null;

                                Dictionary<string, object> companyData = new Dictionary<string, object>();
                                companyData.Add("involvement", involvedCompany);
                                companyData.Add("company", company);

                                icObjects.Add(companyData);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error retrieving involved company with ID {icId}: {ex.Message}");
                            }
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
        [Route("{MetadataMapId}/{MetadataSource}/companies/{CompanyId}")]
        [ProducesResponseType(typeof(Dictionary<string, object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameInvolvedCompanies(long MetadataMapId, FileSignature.MetadataSources MetadataSource, long CompanyId)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);
                if (game != null)
                {
                    List<Dictionary<string, object>> icObjects = new List<Dictionary<string, object>>();
                    if (game.InvolvedCompanies != null)
                    {
                        InvolvedCompany involvedCompany = await Classes.Metadata.InvolvedCompanies.GetInvolvedCompanies(CompanyId);
                        Company company = await Classes.Metadata.Companies.GetCompanies(game.MetadataSource, (long?)involvedCompany.Company);
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
        [Route("{MetadataMapId}/{MetadataSource}/companies/{CompanyId}/image")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameCompanyImage(long MetadataMapId, FileSignature.MetadataSources MetadataSource, long CompanyId)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);

                InvolvedCompany involvedCompany = await Classes.Metadata.InvolvedCompanies.GetInvolvedCompanies(CompanyId);
                Company company = await Classes.Metadata.Companies.GetCompanies(game.MetadataSource, (long?)involvedCompany.Company);

                string coverFilePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Company(company), "Logo_Medium.png");
                if (System.IO.File.Exists(coverFilePath))
                {
                    string filename = "Logo.png";
                    string filepath = coverFilePath;
                    byte[] filedata = await System.IO.File.ReadAllBytesAsync(filepath);
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
        [Route("{MetadataMapId}/emulatorconfiguration/{PlatformId}")]
        [Authorize]
        [ProducesResponseType(typeof(UserEmulatorConfiguration), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetGameEmulator(long MetadataMapId, long PlatformId)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).PreferredMetadataMapItem;
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);

                if (game != null)
                {
                    Platform platformObject = await Classes.Metadata.Platforms.GetPlatform(PlatformId);

                    if (platformObject != null)
                    {
                        var user = await _userManager.GetUserAsync(User);

                        if (user != null)
                        {
                            PlatformMapping platformMapping = new PlatformMapping();
                            UserEmulatorConfiguration platformMappingObject = await platformMapping.GetUserEmulator(user.Id, MetadataMapId, PlatformId);

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
        [Route("{MetadataMapId}/emulatorconfiguration/{PlatformId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SetGameEmulator(long MetadataMapId, long PlatformId, UserEmulatorConfiguration configuration)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).PreferredMetadataMapItem;
                gaseous_server.Models.Game game = await (Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId));

                if (game != null)
                {
                    Platform platformObject = await Classes.Metadata.Platforms.GetPlatform(PlatformId);

                    if (platformObject != null)
                    {
                        var user = await _userManager.GetUserAsync(User);

                        if (user != null)
                        {
                            PlatformMapping platformMapping = new PlatformMapping();
                            await platformMapping.SetUserEmulator(user.Id, MetadataMapId, PlatformId, configuration);

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
        [Route("{MetadataMapId}/emulatorconfiguration/{PlatformId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteGameEmulator(long MetadataMapId, long PlatformId)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).PreferredMetadataMapItem;
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);

                if (game != null)
                {
                    Platform platformObject = await Classes.Metadata.Platforms.GetPlatform(PlatformId);

                    if (platformObject != null)
                    {
                        var user = await _userManager.GetUserAsync(User);

                        if (user != null)
                        {
                            PlatformMapping platformMapping = new PlatformMapping();
                            await platformMapping.DeleteUserEmulator(user.Id, MetadataMapId, PlatformId);

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
        [Route("{MetadataMapId}/metadata")]
        [ProducesResponseType(typeof(MetadataMap), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameMetadataSources(long MetadataMapId)
        {
            try
            {
                MetadataMap metadataMap = await Classes.MetadataManagement.GetMetadataMap(MetadataMapId);

                MetadataMap filteredMetadataMap = new MetadataMap();
                filteredMetadataMap.MetadataMapItems = metadataMap.MetadataMapItems;

                // further filter out metadataMapItems where sourceId = 0
                filteredMetadataMap.MetadataMapItems = filteredMetadataMap.MetadataMapItems.Where(x => x.SourceId != 0).ToList();

                metadataMap.MetadataMapItems = filteredMetadataMap.MetadataMapItems;

                return Ok(metadataMap);
            }
            catch
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPut]
        [Route("{MetadataMapId}/metadata")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(MetadataMap), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameMetadataSources(long MetadataMapId, List<MetadataMap.MetadataMapItem> metadataMapItems)
        {
            try
            {
                MetadataMap existingMetadataMap = await Classes.MetadataManagement.GetMetadataMap(MetadataMapId);

                if (existingMetadataMap != null)
                {
                    foreach (MetadataMap.MetadataMapItem metadataMapItem in metadataMapItems)
                    {
                        if (metadataMapItem.SourceType != FileSignature.MetadataSources.None)
                        {
                            // check if existingMetadataMap.MetadataMapItems contains metadataMapItem.SourceType
                            MetadataMap.MetadataMapItem existingMetadataMapItem = existingMetadataMap.MetadataMapItems.FirstOrDefault(x => x.SourceType == metadataMapItem.SourceType);

                            if (existingMetadataMapItem != null)
                            {
                                MetadataManagement.UpdateMetadataMapItem(MetadataMapId, metadataMapItem.SourceType, (long)metadataMapItem.SourceId, metadataMapItem.Preferred, metadataMapItem.IsManual);
                            }
                            else
                            {
                                MetadataManagement.AddMetadataMapItem(MetadataMapId, metadataMapItem.SourceType, (long)metadataMapItem.SourceId, metadataMapItem.Preferred, metadataMapItem.IsManual, (long)metadataMapItem.SourceId);
                            }
                        }
                        else
                        {
                            MetadataMap.MetadataMapItem existingMetadataMapItem = existingMetadataMap.MetadataMapItems.FirstOrDefault(x => x.SourceType == FileSignature.MetadataSources.None);
                            MetadataManagement.UpdateMetadataMapItem(MetadataMapId, existingMetadataMapItem.SourceType, (long)existingMetadataMapItem.SourceId, metadataMapItem.Preferred);
                        }
                    }

                    return Ok(await Classes.MetadataManagement.GetMetadataMap(MetadataMapId));
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
        [Route("{MetadataMapId}/{MetadataSource}/platforms")]
        [ProducesResponseType(typeof(List<Games.AvailablePlatformItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GamePlatforms(long MetadataMapId, FileSignature.MetadataSources MetadataSource)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                    return Ok(await Games.GetAvailablePlatformsAsync(user.Id, metadataMap.SourceType, (long)metadataMap.SourceId));
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
        [Route("{MetadataMapId}/{MetadataSource}/releasedates")]
        [ProducesResponseType(typeof(List<ReleaseDate>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameReleaseDates(long MetadataMapId, FileSignature.MetadataSources MetadataSource)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);
                if (game != null)
                {
                    List<ReleaseDate> rdObjects = new List<ReleaseDate>();
                    if (game.ReleaseDates != null)
                    {
                        foreach (long icId in game.ReleaseDates)
                        {
                            ReleaseDate releaseDate = await Classes.Metadata.ReleaseDates.GetReleaseDates(game.MetadataSource, icId);

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
        [Route("{MetadataMapId}/roms")]
        [ProducesResponseType(typeof(Classes.Roms.GameRomObject), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ResponseCache(CacheProfileName = "5Minute")]
        public async Task<ActionResult> GameRomAsync(long MetadataMapId, int pageNumber = 0, int pageSize = 0, long PlatformId = -1, string NameSearch = "")
        {
            var user = await _userManager.GetUserAsync(User);

            try
            {
                return Ok(await Classes.Roms.GetRomsAsync(MetadataMapId, PlatformId, NameSearch, pageNumber, pageSize, user.Id));
            }
            catch
            {
                return NotFound();
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{MetadataMapId}/roms/{RomId}")]
        [ProducesResponseType(typeof(Classes.Roms.GameRomItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ResponseCache(CacheProfileName = "5Minute")]
        public async Task<ActionResult> GameRom(long MetadataMapId, long RomId)
        {
            try
            {
                Classes.Roms.GameRomItem rom = await Classes.Roms.GetRom(RomId);
                if (rom.MetadataMapId == MetadataMapId)
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
        [HttpDelete]
        [Authorize(Roles = "Admin,Gamer")]
        [Route("{MetadataMapId}/roms/{RomId}")]
        [ProducesResponseType(typeof(Classes.Roms.GameRomItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomDelete(long MetadataMapId, long RomId)
        {
            try
            {
                Classes.Roms.GameRomItem rom = await Classes.Roms.GetRom(RomId);
                if (rom.MetadataMapId == MetadataMapId)
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
        [Route("{MetadataMapId}/roms/{RomId}/{PlatformId}/favourite")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomFavourite(long MetadataMapId, long RomId, long PlatformId, bool IsMediaGroup, bool favourite)
        {
            try
            {
                ApplicationUser? user = await _userManager.GetUserAsync(User);

                if (IsMediaGroup == false)
                {
                    Classes.Roms.GameRomItem rom = await Classes.Roms.GetRom(RomId);
                    if (rom.MetadataMapId == MetadataMapId)
                    {
                        if (favourite == true)
                        {
                            await Classes.Metadata.Games.GameSetFavouriteRom(user.Id, MetadataMapId, PlatformId, RomId, IsMediaGroup);
                        }
                        else
                        {
                            await Classes.Metadata.Games.GameClearFavouriteRom(user.Id, MetadataMapId, PlatformId);
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
                    Classes.RomMediaGroup.GameRomMediaGroupItem rom = await Classes.RomMediaGroup.GetMediaGroupAsync(RomId, user.Id);
                    if (rom.GameId == MetadataMapId)
                    {
                        if (favourite == true)
                        {
                            await Classes.Metadata.Games.GameSetFavouriteRom(user.Id, MetadataMapId, PlatformId, RomId, IsMediaGroup);
                        }
                        else
                        {
                            await Classes.Metadata.Games.GameClearFavouriteRom(user.Id, MetadataMapId, PlatformId);
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
        [Route("{MetadataMapId}/roms/{RomId}/file")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomFile(long MetadataMapId, long RomId)
        {
            try
            {
                Classes.Roms.GameRomItem rom = await Classes.Roms.GetRom(RomId);
                if (rom.GameId != MetadataMapId)
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
        [Route("{MetadataMapId}/roms/{RomId}/{FileName}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomFile(long MetadataMapId, long RomId, string FileName)
        {
            try
            {
                Classes.Roms.GameRomItem rom = await Classes.Roms.GetRom(RomId);
                if (rom == null)
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
        [Route("{MetadataMapId}/romgroup/{RomGroupId}")]
        [ProducesResponseType(typeof(Classes.RomMediaGroup.GameRomMediaGroupItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ResponseCache(CacheProfileName = "5Minute")]
        public async Task<ActionResult> GameRomGroupAsync(long MetadataMapId, long RomGroupId)
        {
            var user = await _userManager.GetUserAsync(User);

            try
            {
                Classes.RomMediaGroup.GameRomMediaGroupItem rom = await Classes.RomMediaGroup.GetMediaGroupAsync(RomGroupId, user.Id);
                if (rom.GameId == MetadataMapId)
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
        [Route("{MetadataMapId}/romgroup")]
        [ProducesResponseType(typeof(List<RomMediaGroup.GameRomMediaGroupItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetGameRomGroupAsync(long MetadataMapId, long? PlatformId = null)
        {
            var user = await _userManager.GetUserAsync(User);

            try
            {
                try
                {
                    return Ok(await RomMediaGroup.GetMediaGroupsFromGameId(MetadataMapId, user.Id, PlatformId));
                }
                catch (Exception ex)
                {
                    Logging.LogKey(Logging.LogType.Critical, "process.rom_group", "timered_event.error_occurred", null, null, ex);
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
        [Route("{MetadataMapId}/romgroup")]
        [ProducesResponseType(typeof(Classes.RomMediaGroup.GameRomMediaGroupItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> NewGameRomGroup(long MetadataMapId, [FromBody] List<long> RomIds)
        {
            try
            {
                try
                {
                    MetadataMap? metadataMap = await Classes.MetadataManagement.GetMetadataMap(MetadataMapId);
                    if (metadataMap == null)
                    {
                        return NotFound();
                    }
                    Classes.RomMediaGroup.GameRomMediaGroupItem rom = await Classes.RomMediaGroup.CreateMediaGroup(MetadataMapId, metadataMap.PlatformId, RomIds);
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
        [Route("{MetadataMapId}/romgroup/{RomId}")]
        [ProducesResponseType(typeof(Classes.RomMediaGroup.GameRomMediaGroupItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomGroupMembersAsync(long MetadataMapId, long RomGroupId, [FromBody] List<long> RomIds)
        {
            var user = await _userManager.GetUserAsync(User);

            try
            {
                Classes.RomMediaGroup.GameRomMediaGroupItem rom = await Classes.RomMediaGroup.GetMediaGroupAsync(RomGroupId, user.Id);
                if (rom.GameId == MetadataMapId)
                {
                    rom = await Classes.RomMediaGroup.EditMediaGroupAsync(RomGroupId, RomIds);
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
        [Route("{MetadataMapId}/romgroup/{RomGroupId}")]
        [ProducesResponseType(typeof(Classes.RomMediaGroup.GameRomMediaGroupItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomGroupDelete(long MetadataMapId, long RomGroupId)
        {
            try
            {
                Classes.RomMediaGroup.GameRomMediaGroupItem rom = await Classes.RomMediaGroup.GetMediaGroupAsync(RomGroupId);
                if (rom != null)
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
        [Route("{MetadataMapId}/romgroup/{RomGroupId}/file")]
        [Route("{MetadataMapId}/romgroup/{RomGroupId}/{filename}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameRomGroupFile(long MetadataMapId, long RomGroupId, string filename = "")
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).PreferredMetadataMapItem;
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);

                Classes.RomMediaGroup.GameRomMediaGroupItem rom = await Classes.RomMediaGroup.GetMediaGroupAsync(RomGroupId);
                if (rom.GameId != MetadataMapId)
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
                        returnFileName = game.Name + ".zip";
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
        [ProducesResponseType(typeof(List<gaseous_server.Models.Game>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GameSearch(long RomId = 0, string SearchString = "")
        {
            try
            {
                if (RomId > 0)
                {
                    Classes.Roms.GameRomItem romItem = await Classes.Roms.GetRom(RomId);
                    HashObject hash = new HashObject(romItem.Path);
                    FileSignature fileSignature = new FileSignature();
                    gaseous_server.Models.Signatures_Games romSig = await fileSignature.GetFileSignatureAsync(romItem.Library, hash, new FileInfo(romItem.Path), romItem.Path);
                    List<gaseous_server.Models.Game> searchResults = await Classes.ImportGame.SearchForGame_GetAll(romSig.Game.Name, romSig.Flags.PlatformId);

                    return Ok(searchResults);
                }
                else
                {
                    if (SearchString.Length > 0)
                    {
                        List<gaseous_server.Models.Game> searchResults = await Classes.ImportGame.SearchForGame_GetAll(SearchString, 0);

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
        [Route("{MetadataMapId}/{MetadataSource}/screenshots")]
        [ProducesResponseType(typeof(List<Screenshot>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameScreenshot(long MetadataMapId, FileSignature.MetadataSources MetadataSource)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);

                List<Screenshot> screenshots = new List<Screenshot>();
                if (game.Screenshots != null)
                {
                    foreach (long ScreenshotId in game.Screenshots)
                    {
                        Screenshot GameScreenshot = await Screenshots.GetScreenshotAsync(game.MetadataSource, ScreenshotId);
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
        [Route("{MetadataMapId}/{MetadataSource}/screenshots/{ScreenshotId}")]
        [ProducesResponseType(typeof(Screenshot), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameScreenshot(long MetadataMapId, FileSignature.MetadataSources MetadataSource, long ScreenshotId)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);
                if (game != null)
                {
                    Screenshot screenshotObject = await Screenshots.GetScreenshotAsync(game.MetadataSource, ScreenshotId);
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

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{MetadataMapId}/{MetadataSource}/videos")]
        [ProducesResponseType(typeof(List<GameVideo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(CacheProfileName = "7Days")]
        public async Task<ActionResult> GameVideo(long MetadataMapId, FileSignature.MetadataSources MetadataSource)
        {
            try
            {
                MetadataMap.MetadataMapItem metadataMap = (await Classes.MetadataManagement.GetMetadataMap(MetadataMapId)).MetadataMapItems.FirstOrDefault(x => x.SourceType == MetadataSource);
                gaseous_server.Models.Game game = await Classes.Metadata.Games.GetGame(metadataMap.SourceType, metadataMap.SourceId);

                List<GameVideo> videos = new List<GameVideo>();
                if (game.Videos != null)
                {
                    foreach (long VideoId in game.Videos)
                    {
                        GameVideo gameVideo = await GamesVideos.GetGame_Videos(game.MetadataSource, VideoId);
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
