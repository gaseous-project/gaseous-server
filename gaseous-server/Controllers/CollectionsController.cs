using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class CollectionsController : Controller
    {
        /// <summary>
        /// Gets all ROM collections
        /// </summary>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<Classes.Collections.CollectionItem> GetCollections()
        {
            return Classes.Collections.GetCollections();
        }

        /// <summary>
        /// Gets a specific ROM collection
        /// </summary>
        /// <param name="CollectionId"></param>
        /// <param name="Build">Set to true to begin the collection build process</param>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [HttpGet]
        [Route("{CollectionId}")]
        [ProducesResponseType(typeof(Classes.Collections.CollectionItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GetCollection(long CollectionId, bool Build = false)
        {
            try
            {
                if (Build == true)
                {
                    Classes.Collections.StartCollectionItemBuild(CollectionId);
                }

                return Ok(Classes.Collections.GetCollection(CollectionId));
            }
            catch
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
        [HttpGet]
        [Route("{CollectionId}/Roms")]
        [ProducesResponseType(typeof(List<Classes.Collections.CollectionContents.CollectionPlatformItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GetCollectionRoms(long CollectionId)
        {
            try
            {
                Classes.Collections.CollectionItem collectionItem = Classes.Collections.GetCollection(CollectionId);
                return Ok(Classes.Collections.GetCollectionContent(collectionItem));
            }
            catch
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
        [HttpPost]
        [Route("Preview")]
        [ProducesResponseType(typeof(List<Classes.Collections.CollectionContents.CollectionPlatformItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GetCollectionRomsPreview(Classes.Collections.CollectionItem Item)
        {
            try
            {
                return Ok(Classes.Collections.GetCollectionContent(Item));
            }
            catch (Exception ex)
            {
               return NotFound(ex);
            }
        }

        /// <summary>
        /// Gets ROM collection in zip format
        /// </summary>
        /// <param name="CollectionId"></param>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [HttpGet]
        [Route("{CollectionId}/Roms/Zip")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GetCollectionRomsZip(long CollectionId)
        {
            try
            {
                Classes.Collections.CollectionItem collectionItem = Classes.Collections.GetCollection(CollectionId);

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

        /// <summary>
        /// Creates a new ROM collection
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [HttpPost]
        [ProducesResponseType(typeof(Classes.Collections.CollectionItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult NewCollection(Classes.Collections.CollectionItem Item)
        {
            try
            {
                return Ok(Classes.Collections.NewCollection(Item));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        /// <summary>
        /// Edits an existing collection
        /// </summary>
        /// <param name="CollectionId"></param>
        /// <param name="Item"></param>
        /// <returns></returns>
        [MapToApiVersion("1.0")]
        [HttpPatch]
        [Route("{CollectionId}")]
        [ProducesResponseType(typeof(Classes.Collections.CollectionItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult EditCollection(long CollectionId, Classes.Collections.CollectionItem Item)
        {
            try
            {
                return Ok(Classes.Collections.EditCollection(CollectionId, Item, true));
            }
            catch
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
        [HttpPatch]
        [Route("{CollectionId}/AlwaysInclude")]
        [ProducesResponseType(typeof(Classes.Collections.CollectionItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult EditCollectionAlwaysInclude(long CollectionId, [FromQuery]bool Rebuild, [FromBody]Collections.CollectionItem.AlwaysIncludeItem Inclusion)
        {
            try
            {
                Collections.CollectionItem collectionItem = Classes.Collections.GetCollection(CollectionId);
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

                return Ok(Classes.Collections.EditCollection(CollectionId, collectionItem, Rebuild));
            }
            catch
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Deletes the specified ROM collection
        /// </summary>
        /// <param name="CollectionId"></param>
        [MapToApiVersion("1.0")]
        [HttpDelete]
        [Route("{CollectionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeleteCollection(long CollectionId)
        {
            try
            {
                Classes.Collections.DeleteCollection(CollectionId);
                return Ok();
            }
            catch
            {
                return NotFound();
            }
        }
    }
}