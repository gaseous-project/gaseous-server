using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Authentication;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize]
    public class CollectionsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public CollectionsController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Gets all ROM collections
        /// </summary>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetCollectionsAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                return Ok(Classes.Collections.GetCollections(user.Id));
            }

            return NotFound();
        }

        /// <summary>
        /// Gets a specific ROM collection
        /// </summary>
        /// <param name="CollectionId"></param>
        /// <param name="Build">Set to true to begin the collection build process</param>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{CollectionId}")]
        [ProducesResponseType(typeof(Classes.Collections.CollectionItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCollection(long CollectionId, bool Build = false)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                try
                {
                    if (Build == true)
                    {
                        Classes.Collections.StartCollectionItemBuild(CollectionId, user.Id);
                    }

                    return Ok(Classes.Collections.GetCollection(CollectionId, user.Id));
                }
                catch
                {
                    return NotFound();
                }
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Gets the contents of the specified ROM collection
        /// </summary>
        /// <param name="CollectionId"></param>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{CollectionId}/Roms")]
        [ProducesResponseType(typeof(List<Classes.Collections.CollectionContents.CollectionPlatformItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCollectionRoms(long CollectionId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                try
                {
                    Classes.Collections.CollectionItem collectionItem = Classes.Collections.GetCollection(CollectionId, user.Id);
                    return Ok(Classes.Collections.GetCollectionContent(collectionItem, user.Id));
                }
                catch
                {
                    return NotFound();
                }
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Gets a preview of the provided collection item
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Route("Preview")]
        [Authorize(Roles = "Admin,Gamer")]
        [ProducesResponseType(typeof(List<Classes.Collections.CollectionContents.CollectionPlatformItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCollectionRomsPreview(Classes.Collections.CollectionItem Item)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                try
                {
                    return Ok(Classes.Collections.GetCollectionContent(Item, user.Id));
                }
                catch (Exception ex)
                {
                    return NotFound(ex);
                }
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Gets ROM collection in zip format
        /// </summary>
        /// <param name="CollectionId"></param>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [Route("{CollectionId}/Roms/Zip")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCollectionRomsZip(long CollectionId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                try
                {
                    Classes.Collections.CollectionItem collectionItem = Classes.Collections.GetCollection(CollectionId, user.Id);

                    string ZipFilePath = Path.Combine(Config.LibraryConfiguration.LibraryCollectionsDirectory, CollectionId + ".zip");

                    if (System.IO.File.Exists(ZipFilePath))
                    {
                        var stream = new FileStream(ZipFilePath, FileMode.Open);
                        return File(stream, "application/zip", collectionItem.Name + ".zip");
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
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Creates a new ROM collection
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPost]
        [Authorize(Roles = "Admin,Gamer")]
        [ProducesResponseType(typeof(Classes.Collections.CollectionItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> NewCollectionAsync(Classes.Collections.CollectionItem Item)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                try
                {
                    return Ok(Classes.Collections.NewCollection(Item, user.Id));
                }
                catch (Exception ex)
                {
                    return BadRequest(ex);
                }
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Edits an existing collection
        /// </summary>
        /// <param name="CollectionId"></param>
        /// <param name="Item"></param>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPatch]
        [Route("{CollectionId}")]
        [Authorize(Roles = "Admin,Gamer")]
        [ProducesResponseType(typeof(Classes.Collections.CollectionItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> EditCollection(long CollectionId, Classes.Collections.CollectionItem Item)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                try
                {
                    return Ok(Classes.Collections.EditCollection(CollectionId, Item, user.Id, true));
                }
                catch
                {
                    return NotFound();
                }
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Edits an existing collection
        /// </summary>
        /// <param name="CollectionId"></param>
        /// <param name="Item"></param>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpPatch]
        [Authorize(Roles = "Admin,Gamer")]
        [Route("{CollectionId}/AlwaysInclude")]
        [ProducesResponseType(typeof(Classes.Collections.CollectionItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> EditCollectionAlwaysInclude(long CollectionId, [FromQuery] bool Rebuild, [FromBody] Collections.CollectionItem.AlwaysIncludeItem Inclusion)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                try
                {
                    Collections.CollectionItem collectionItem = Classes.Collections.GetCollection(CollectionId, user.Id);
                    bool ItemFound = false;
                    foreach (Collections.CollectionItem.AlwaysIncludeItem includeItem in collectionItem.AlwaysInclude)
                    {
                        if (includeItem.PlatformId == Inclusion.PlatformId && includeItem.GameId == Inclusion.GameId)
                        {
                            ItemFound = true;
                        }
                    }
                    if (ItemFound == false)
                    {
                        collectionItem.AlwaysInclude.Add(Inclusion);
                    }

                    return Ok(Classes.Collections.EditCollection(CollectionId, collectionItem, user.Id, Rebuild));
                }
                catch
                {
                    return NotFound();
                }
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Deletes the specified ROM collection
        /// </summary>
        /// <param name="CollectionId"></param>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpDelete]
        [Authorize(Roles = "Admin,Gamer")]
        [Route("{CollectionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteCollection(long CollectionId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                try
                {
                    Classes.Collections.DeleteCollection(CollectionId, user.Id);
                    return Ok();
                }
                catch
                {
                    return NotFound();
                }
            }
            else
            {
                return NotFound();
            }
        }
    }
}