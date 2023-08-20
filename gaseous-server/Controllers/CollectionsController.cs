using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CollectionsController : Controller
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<Classes.Collections.CollectionItem> GetCollections()
        {
            return Classes.Collections.GetCollections();
        }

        [HttpGet]
        [Route("{CollectionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Classes.Collections.CollectionItem GetCollections(long CollectionId)
        {
            return Classes.Collections.GetCollection(CollectionId);
        }

        [HttpGet]
        [Route("{CollectionId}/Roms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Classes.Collections.CollectionItem GetCollectionRoms(long CollectionId)
        {
            return Classes.Collections.GetCollectionContent(CollectionId);
        }
    }
}