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
        /// Translates a localisation key with pluralisation support based on a numeric count.
        /// </summary>
        /// <param name="baseKey">The base localisation key without the .one/.other suffix.</param>
        /// <param name="count">The numeric count used to decide plural form.</param>
        /// <param name="args">Optional formatting arguments applied via string.Format.</param>
        /// <returns>The translated pluralised string, or a fallback if not found.</returns>
        /// <remarks>
        /// Uses the locale's PluralRule expression (boolean over n) to select between baseKey.one and baseKey.other.
        /// If PluralRule evaluation returns false then singular (one) is chosen; if true, plural (other) is chosen.
        /// Falls back gracefully through: requested variant, alternate variant, baseKey, then returns the resolved variant key name.
        /// </remarks>
        public static string TranslatePlural(string baseKey, long count, string[]? args = null)
        {
            // ensure locale loaded
            if (!_loadedLocales.ContainsKey(_currentLocale))
            {
                GetLanguageFile(_currentLocale);
            }

            if (!_loadedLocales.TryGetValue(_currentLocale, out var localeFile) || localeFile?.Strings == null)
            {
                return baseKey; // nothing loaded
            }

            string? resolvedKey = null;

            // Advanced plural rules: evaluate categories in priority order
            if (localeFile.PluralRules != null && localeFile.PluralRules.Count > 0)
            {
                // Define evaluation order; user may supply subset.
                string[] order = new[] { "zero", "one", "few", "many", "other" };
                var matchedCategory = order.FirstOrDefault(cat => localeFile.PluralRules.ContainsKey(cat) && EvaluatePluralRule(localeFile.PluralRules[cat], count));
                if (matchedCategory != null)
                {
                    resolvedKey = baseKey + "." + matchedCategory;
                }
            }

            // Legacy binary rule fallback if no category matched or no advanced rules
            if (resolvedKey == null)
            {
                string rule = localeFile.PluralRule ?? "n != 1"; // default behaviour
                bool isPlural = EvaluatePluralRule(rule, count);
                resolvedKey = isPlural ? baseKey + ".other" : baseKey + ".one";
            }

            // Attempt retrieval; build fallback chain of alternative categories
            List<string> fallbackKeys = new List<string>();
            fallbackKeys.Add(resolvedKey);

            // Add other plural forms for fallback (excluding already added)
            string[] allCats = { "zero", "one", "few", "many", "other" };
            foreach (var cat in allCats)
            {
                string k = baseKey + "." + cat;
                if (!fallbackKeys.Contains(k)) fallbackKeys.Add(k);
            }
            // baseKey itself
            fallbackKeys.Add(baseKey);

            string? value = null;
            foreach (var k in fallbackKeys)
            {
                if (localeFile.Strings.TryGetValue(k, out value))
                {
                    resolvedKey = k;
                    break;
                }
            }

            if (value == null)
            {
                return resolvedKey ?? baseKey; // final fallback: category key name or baseKey
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(value, args);
                }
                catch (FormatException)
                {
                    return value; // return unformatted
                }
            }

            return value;
        }

        /// <summary>
        /// Evaluates a simple plural rule expression against the given count.
        /// </summary>
        /// <param name="expression">Boolean expression referencing 'n'. Example: "n != 1".</param>
        /// <param name="n">The numeric count.</param>
        /// <returns>True if expression evaluates to true; otherwise false. Invalid expressions default to (n != 1).</returns>
        private static bool EvaluatePluralRule(string expression, long n)
        {
            // Very small, safe evaluator supporting: n, integers, parentheses, ==, !=, <, <=, >, >=, &&, ||
            // Tokenize
            try
            {
                var tokens = TokenizeExpression(expression);
                int index = 0;
                bool result = ParseOr(tokens, ref index, n);
                return result;
            }
            catch
            {
                // fallback rule
                return n != 1;
            }
        }

        private static List<string> TokenizeExpression(string expr)
        {
            var tokens = new List<string>();
            for (int i = 0; i < expr.Length;)
            {
                char c = expr[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (char.IsDigit(c))
                {
                    int start = i;
                    while (i < expr.Length && char.IsDigit(expr[i])) i++;
                    tokens.Add(expr.Substring(start, i - start));
                    continue;
                }
                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < expr.Length && char.IsLetter(expr[i])) i++;
                    tokens.Add(expr.Substring(start, i - start));
                    continue;
                }
                // operators / parentheses
                if (i + 1 < expr.Length)
                {
                    string two = expr.Substring(i, 2);
                    if (two == "==" || two == "!=" || two == "<=" || two == ">=" || two == "&&" || two == "||")
                    {
                        tokens.Add(two);
                        i += 2;
                        continue;
                    }
                }
                tokens.Add(c.ToString());
                i++;
            }
            return tokens;
        }

        // Recursive descent parser: or -> and -> comparison -> primary
        private static bool ParseOr(List<string> tokens, ref int index, long n)
        {
            bool left = ParseAnd(tokens, ref index, n);
            while (index < tokens.Count && tokens[index] == "||")
            {
                index++; // skip '||'
                bool right = ParseAnd(tokens, ref index, n);
                left = left || right;
            }
            return left;
        }

        private static bool ParseAnd(List<string> tokens, ref int index, long n)
        {
            bool left = ParseComparison(tokens, ref index, n);
            while (index < tokens.Count && tokens[index] == "&&")
            {
                index++; // skip '&&'
                bool right = ParseComparison(tokens, ref index, n);
                left = left && right;
            }
            return left;
        }

        private static bool ParseComparison(List<string> tokens, ref int index, long n)
        {
            long leftValue = ParseValue(tokens, ref index, n);
            if (index >= tokens.Count) return leftValue != 1; // if no operator, treat number as boolean (non 1 => plural)
            string op = tokens[index];
            if (!(op == "==" || op == "!=" || op == "<" || op == "<=" || op == ">" || op == ">="))
            {
                // not a comparison operator, treat leftValue
                return leftValue != 1;
            }
            index++; // consume operator
            long rightValue = ParseValue(tokens, ref index, n);
            return op switch
            {
                "==" => leftValue == rightValue,
                "!=" => leftValue != rightValue,
                "<" => leftValue < rightValue,
                "<=" => leftValue <= rightValue,
                ">" => leftValue > rightValue,
                ">=" => leftValue >= rightValue,
                _ => false
            };
        }

        private static long ParseValue(List<string> tokens, ref int index, long n)
        {
            if (index >= tokens.Count) return 0;
            string token = tokens[index];
            if (token == "(")
            {
                index++; // consume '('
                bool inner = ParseOr(tokens, ref index, n);
                if (index < tokens.Count && tokens[index] == ")") index++; // consume ')'
                return inner ? 1 : 0;
            }
            if (token.Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                index++;
                return n;
            }
            if (long.TryParse(token, out var value))
            {
                index++;
                return value;
            }
            // unknown token
            index++;
            return 0;
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
                LocaleFileModel? enFileLocale = null;
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
                    englishLocale = enLocale!; // enLocale must not be null here otherwise resource load failed earlier
                }
                if (englishLocale != null)
                {
                    _loadedLocales.TryAdd("en", englishLocale);
                }
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
                if (englishLocale != null)
                {
                    localeData = MergeLocaleFiles(englishLocale, localeData);
                }
                // if englishLocale unexpectedly null, skip merge (localeData already loaded)
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
                    LocaleFileModel? localeFile = Newtonsoft.Json.JsonConvert.DeserializeObject<LocaleFileModel>(json);
                    if (localeFile == null)
                    {
                        throw new InvalidDataException("Failed to deserialize locale resource JSON: " + resourceName);
                    }

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
            LocaleFileModel? localeFile = Newtonsoft.Json.JsonConvert.DeserializeObject<LocaleFileModel>(json);
            if (localeFile == null)
            {
                throw new InvalidDataException("Failed to deserialize locale file JSON: " + filePath);
            }

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