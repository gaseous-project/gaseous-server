using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using gaseous_server.Classes;
using gaseous_server.Models;

namespace gaseous_server.Controllers.v1_1
{
    /// <summary>
    /// API controller providing localisation (language/locale) resources and handling overlay inheritance of locale files.
    /// </summary>
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.1")]
    [ApiController]
    public class LocalisationController : ControllerBase
    {
        /// <summary>
        /// Returns the server default language/locale configured.
        /// </summary>
        /// <returns>JSON object with serverLanguage property.</returns>
        [MapToApiVersion("1.1")]
        [HttpGet("server-language")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult GetServerLanguage()
        {
            return Ok(new { serverLanguage = Config.ServerLanguage });
        }

        /// <summary>
        /// Retrieves the locale file for the specified locale code.
        /// </summary>
        /// <param name="locale">The language or locale code (e.g., "en-US" or "fr"). Defaults to "en-US".</param>
        /// <returns>The locale file model containing localization data.</returns>
        /// <response code="200">Returns the locale file model.</response>
        /// <response code="400">If the locale parameter is invalid.</response>
        /// <response code="404">If the specified locale file is not found.</response>
        [MapToApiVersion("1.1")]
        [HttpGet]
        [ProducesResponseType(typeof(LocaleFileModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLanguage(string locale = "en-US")
        {
            // sanitize input to prevent directory traversal attacks
            // input should only ever be a locale code like "en-US" or "fr-FR", or language code like "en" or "fr"
            locale = locale.Replace("/", "").Replace("\\", "").Replace("..", "");
            if (string.IsNullOrWhiteSpace(locale))
            {
                return BadRequest("Locale cannot be empty");
            }

            // sanitise the input locale
            string sanitisedLocale = Localisation.SanitiseLocale(locale);
            if (string.IsNullOrEmpty(sanitisedLocale))
            {
                throw new ArgumentException("Invalid locale", nameof(locale));
            }

            LocaleFileModel localeFile = Localisation.GetLanguageFile(locale);
            if (localeFile == null)
            {
                return NotFound("Locale file not found");
            }

            return Ok(localeFile);
        }
    }
}