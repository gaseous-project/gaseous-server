using System.Collections.Concurrent;
using gaseous_server.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.IdentityModel.Tokens;

namespace gaseous_server.Classes
{
    /// <summary>
    /// Provides localisation utilities for sanitising locale codes, loading locale files and merging overlay locale data.
    /// </summary>
    public static class Localisation
    {
        /// <summary>
        /// The default locale code used by the server.
        /// </summary>
        public const string DefaultLocale = "en-AU";

        private static string _currentLocale = DefaultLocale;

        private static ConcurrentDictionary<string, LocaleFileModel> _loadedLocales = new ConcurrentDictionary<string, LocaleFileModel>();

        /// <summary>
        /// Gets or sets the current locale code used by the server.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when setting an invalid locale code.</exception>
        /// <exception cref="FileNotFoundException">Thrown when setting a locale code for which the locale file cannot be found.</exception>
        /// <remarks>
        /// When setting the locale, the locale code is sanitised and validated. If the locale file cannot be found, a FileNotFoundException is thrown.
        /// This value is only updated at server start up and is not intended to be changed at runtime.
        /// </remarks>
        public static string CurrentLocale
        {
            get
            {
                return _currentLocale;
            }
            set
            {
                // check if the locale is valid
                string sanitisedLocale = SanitiseLocale(value);
                if (string.IsNullOrEmpty(sanitisedLocale))
                {
                    throw new ArgumentException("Invalid locale", nameof(value));
                }

                // check if the locale file exists
                try
                {
                    LocaleFileModel localeFile = GetLanguageFile(sanitisedLocale);
                }
                catch (FileNotFoundException)
                {
                    throw new FileNotFoundException("Locale file not found", sanitisedLocale);
                }

                _currentLocale = sanitisedLocale;
            }
        }

        /// <summary>
        /// Translates a localisation key using the current locale, optionally formatting the result with arguments.
        /// </summary>
        /// <param name="key">The localisation string key to look up.</param>
        /// <param name="args">Optional formatting arguments applied via string.Format if provided.</param>
        /// <returns>The translated (and formatted) string, or the original key if no translation is found.</returns>
        public static string Translate(string key, string[]? args = null)
        {
            // check if the current locale is loaded
            if (!_loadedLocales.ContainsKey(_currentLocale))
            {
                GetLanguageFile(_currentLocale);
            }

            if (_loadedLocales.TryGetValue(_currentLocale, out var localeFile) && localeFile?.Strings != null && localeFile.Strings.TryGetValue(key, out var value))
            {
                if (args != null && args.Length > 0)
                {
                    return string.Format(value, args);
                }
                return value;
            }

            // return the key if not found
            return key;
        }

        /// <summary>
        /// Sanitises a locale string by removing invalid characters and enforcing a standard format.
        /// </summary>
        /// <param name="locale">The input locale string to sanitise. The locale can be either the language (example: en), or the language and region (example: en-AU)</param>
        /// <returns>The sanitised locale string.</returns>
        /// <remarks>
        /// This method removes any characters that are not letters, digits, hyphens, or underscores.
        /// It also ensures that the locale follows the standard format of "language-REGION" where the language is lowercase and the region is uppercase.
        /// </remarks>
        public static string SanitiseLocale(string locale)
        {
            // remove invalid characters
            var validChars = locale.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray();
            string cleanedLocale = new string(validChars);
            // split into parts
            var parts = cleanedLocale.Split(new char[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return DefaultLocale;
            }
            // format parts
            string language = parts[0].ToLowerInvariant();
            if (parts.Length == 1)
            {
                return language;
            }
            string region = parts[1].ToUpperInvariant();
            return $"{language}-{region}";
        }

        /// <summary>
        /// Loads and returns the locale file data for the specified locale code, merging overlay locales with their base locale if applicable.
        /// </summary>
        /// <param name="locale">Locale code to load (e.g., en or en-AU).</param>
        /// <returns>A LocaleFileModel representing the loaded localisation data.</returns>
        /// <exception cref="ArgumentException">Thrown when the provided locale is invalid.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the locale file (or required base overlay file) cannot be found.</exception>
        public static LocaleFileModel GetLanguageFile(string locale)
        {
            // check if locale is already loaded
            if (_loadedLocales.ContainsKey(locale))
            {
                return _loadedLocales[locale];
            }

            // load English as fallback
            // the strings from English should be used if any strings are missing from the requested locale, or the requested locale file cannot be found
            LocaleFileModel englishLocale;
            if (!_loadedLocales.ContainsKey("en"))
            {
                LocaleFileModel enLocale = LoadLocaleFromResources("en");
                LocaleFileModel enFileLocale = null;
                try
                {
                    enFileLocale = LoadLocaleFromFile("en");
                }
                catch (FileNotFoundException)
                {
                    // silent catch
                }
                if (enLocale != null && enFileLocale != null)
                {
                    englishLocale = MergeLocaleFiles(enLocale, enFileLocale);
                }
                else if (enFileLocale != null)
                {
                    englishLocale = enFileLocale;
                }
                else
                {
                    englishLocale = enLocale;
                }
                _loadedLocales.TryAdd("en", englishLocale);
            }
            else
            {
                englishLocale = _loadedLocales["en"];
            }

            // load the locale file from the embedded resources
            LocaleFileModel? resourceLocale = null;
            try
            {
                resourceLocale = LoadLocaleFromResources(locale);
            }
            catch (FileNotFoundException)
            {
                // silent catch
            }

            // load the locale file from disk
            LocaleFileModel? fileLocale = null;
            try
            {
                fileLocale = LoadLocaleFromFile(locale);
            }
            catch (FileNotFoundException)
            {
                // silent catch
            }

            LocaleFileModel? localeData = null;
            if (resourceLocale != null && fileLocale != null)
            {
                // merge the two, with fileLocale taking precedence
                localeData = MergeLocaleFiles(resourceLocale, fileLocale);
            }
            else if (fileLocale != null)
            {
                localeData = fileLocale;
            }
            else if (resourceLocale != null)
            {
                localeData = resourceLocale;
            }
            else
            {
                throw new FileNotFoundException("Locale file not found", locale);
            }

            // merge the localeData with English to ensure all strings are present
            if (localeData.Code != "en")
            {
                localeData = MergeLocaleFiles(englishLocale, localeData);
            }

            // check if locale is defined in loaded locales cache, and add it if not
            _loadedLocales.AddOrUpdate(locale, localeData, (key, oldValue) => localeData);

            return localeData;
        }

        private static LocaleFileModel LoadLocaleFromResources(string locale)
        {
            string resourceName = "gaseous_server.Support.Localisation." + locale + ".json";
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("Locale file not found in resources", locale);
                }

                using (var reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    LocaleFileModel localeFile = Newtonsoft.Json.JsonConvert.DeserializeObject<LocaleFileModel>(json);

                    if (localeFile.Type == LocaleFileModel.LocaleFileType.Overlay)
                    {
                        // load the base locale from resources
                        LocaleFileModel baseLocale = LoadLocaleFromResources(localeFile.ParentLanguage);
                        localeFile = MergeLocaleFiles(baseLocale, localeFile);
                    }

                    return localeFile;
                }
            }
        }

        private static LocaleFileModel LoadLocaleFromFile(string locale)
        {
            string filePath = Path.Combine(Config.LocalisationPath, locale + ".json");
            if (!System.IO.File.Exists(filePath))
            {
                throw new FileNotFoundException("Locale file not found", filePath);
            }

            string json = System.IO.File.ReadAllText(filePath);
            LocaleFileModel localeFile = Newtonsoft.Json.JsonConvert.DeserializeObject<LocaleFileModel>(json);

            if (localeFile.Type == LocaleFileModel.LocaleFileType.Overlay)
            {
                // load the base locale from file
                LocaleFileModel baseLocale = LoadLocaleFromFile(localeFile.ParentLanguage);
                localeFile = MergeLocaleFiles(baseLocale, localeFile);
            }

            return localeFile;
        }

        private static LocaleFileModel MergeLocaleFiles(LocaleFileModel baseLocale, LocaleFileModel overlayLocale)
        {
            LocaleFileModel mergedLocale = new LocaleFileModel
            {
                Name = overlayLocale.Name ?? baseLocale.Name,
                NativeName = overlayLocale.NativeName ?? baseLocale.NativeName,
                Code = overlayLocale.Code ?? baseLocale.Code,
                PluralRule = overlayLocale.PluralRule ?? baseLocale.PluralRule,
                Direction = overlayLocale.Direction ?? baseLocale.Direction,
                Strings = new Dictionary<string, string>(),
                ServerStrings = new Dictionary<string, string>()
            };

            // add base strings
            if (baseLocale.Strings != null)
            {
                mergedLocale.Strings ??= new Dictionary<string, string>();
                foreach (var kvp in baseLocale.Strings)
                {
                    mergedLocale.Strings[kvp.Key] = kvp.Value;
                }
            }

            // add base server strings
            if (baseLocale.ServerStrings != null)
            {
                foreach (var kvp in baseLocale.ServerStrings)
                {
                    mergedLocale.ServerStrings[kvp.Key] = kvp.Value;
                }
            }

            // overlay strings
            if (overlayLocale.Strings != null)
            {
                foreach (var kvp in overlayLocale.Strings)
                {
                    mergedLocale.Strings[kvp.Key] = kvp.Value;
                }
            }

            // overlay server strings
            if (overlayLocale.ServerStrings != null)
            {
                foreach (var kvp in overlayLocale.ServerStrings)
                {
                    mergedLocale.ServerStrings[kvp.Key] = kvp.Value;
                }
            }

            return mergedLocale;
        }
    }
}