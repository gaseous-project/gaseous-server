using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using gaseous_server.Classes;
using gaseous_server.Models;

namespace gaseous_server.Controllers.v1_1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.1")]
    [ApiController]
    public class LocalisationController : ControllerBase
    {
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

            // create the language path
            string langPath = Path.Combine(Config.LocalisationPath, locale + ".json");

            // check if the file exists
            if (!System.IO.File.Exists(langPath))
            {
                // try to extract from resources
                try
                {
                    langPath = await ExtractLocaleFromResourceAsync(locale);
                }
                catch (FileNotFoundException)
                {
                    return NotFound("Locale file not found: " + locale);
                }
            }

            // read the file asynchronously
            string langJson = await System.IO.File.ReadAllTextAsync(langPath);
            LocaleFileModel langFile = Newtonsoft.Json.JsonConvert.DeserializeObject<LocaleFileModel>(langJson)!;

            if (langFile.Type == LocaleFileModel.LocaleFileType.Overlay)
            {
                // load the base language file
                string baseLangPath = Path.Combine(Config.LocalisationPath, langFile.ParentLanguage + ".json");

                // check if the file exists
                if (!System.IO.File.Exists(baseLangPath))
                {
                    // try to extract from resources
                    try
                    {
                        baseLangPath = await ExtractLocaleFromResourceAsync(langFile.ParentLanguage);
                    }
                    catch (FileNotFoundException)
                    {
                        return NotFound("Locale file not found: " + locale);
                    }
                }

                if (System.IO.File.Exists(baseLangPath))
                {
                    string baseLangJson = await System.IO.File.ReadAllTextAsync(baseLangPath);
                    LocaleFileModel baseLangFile = Newtonsoft.Json.JsonConvert.DeserializeObject<LocaleFileModel>(baseLangJson)!;

                    // merge base strings with overlay strings
                    if (baseLangFile.Strings != null)
                    {
                        foreach (var kvp in baseLangFile.Strings)
                        {
                            // only add the base string if it doesn't exist in the overlay
                            if (langFile.Strings == null || !langFile.Strings.ContainsKey(kvp.Key))
                            {
                                langFile.Strings ??= new Dictionary<string, string>();
                                langFile.Strings[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
            }

            return Ok(langFile);
        }
        
        /// <summary>
        /// Extracts the locale file from embedded resources to the localisation directory if it exists.
        /// </summary>
        /// <param name="locale">The language or locale such as 'en' or 'en-US'. Note: this is assumed to have been sanitised upstream.</param>
        /// <returns>The full path to the extracted locale file</returns>
        private static async Task<string> ExtractLocaleFromResourceAsync(string locale)
        {
            // extract the language file from app resources if it exists there
            string langPath = Path.Combine(Config.LocalisationPath, locale + ".json");
            string resourceName = "gaseous_server.Support.Localisation." + locale + ".json";
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        string resourceContent = await reader.ReadToEndAsync();
                        await System.IO.File.WriteAllTextAsync(langPath, resourceContent);
                    }
                } else
                {
                    throw new FileNotFoundException("Locale resource not found: " + resourceName);
                }
            }

            return langPath;
        }
    }
}