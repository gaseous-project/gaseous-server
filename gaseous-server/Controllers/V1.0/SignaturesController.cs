using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using gaseous_server.Classes;
using gaseous_signature_parser.models.RomSignatureObject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using gaseous_server.Models;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace gaseous_server.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [ApiVersion("1.0", Deprecated = true)]
    [ApiVersion("1.1")]
    [Authorize]
    public class SignaturesController : Controller
    {
        /// <summary>
        /// Get the current signature counts from the database
        /// </summary>
        /// <returns>Number of sources, publishers, games, and rom signatures in the database</returns>
        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Models.Signatures_Status Status()
        {
            return new Models.Signatures_Status();
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<List<gaseous_server.Models.Signatures_Games>> GetSignature(string md5 = "", string sha1 = "", string sha256 = "", string crc32 = "")
        {
            HashObject hashes = new HashObject
            {
                md5hash = md5,
                sha1hash = sha1,
                sha256hash = sha256,
                crc32hash = crc32
            };

            SignatureManagement signatureManagement = new SignatureManagement();
            return await signatureManagement.GetSignature(hashes);
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<List<gaseous_server.Models.Signatures_Games>> GetByTosecName(string TosecName = "")
        {
            if (TosecName.Length > 0)
            {
                SignatureManagement signatureManagement = new SignatureManagement();
                return await signatureManagement.GetByTosecName(TosecName);
            }
            else
            {
                return null;
            }
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<List<Signatures_Sources>> GetSignatureSources()
        {
            SignatureManagement signatureManagement = new SignatureManagement();
            return await signatureManagement.GetSources();
        }

        [MapToApiVersion("1.0")]
        [MapToApiVersion("1.1")]
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteSignatureSource(int Id)
        {
            SignatureManagement signatureManagement = new SignatureManagement();
            await signatureManagement.DeleteSource(Id);

            return Ok();
        }
    }
}

