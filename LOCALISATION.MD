# Localisation in gaseous-server

The gaseous-server provides an extensible localisation (internationalisation) system that supports base language files, overlay locale variants, server-only strings and pluralisation.

## Locale File Overview

Locale files are JSON documents located under `gaseous-server/Support/Localisation/`. Each file represents either a base language (e.g. `en.json`) or an overlay locale (e.g. `en-AU.json`). Overlay files can override or extend the base language.

Example base file (`en.json`):
```json
{
  "name": "English",
  "nativeName": "English",
  "code": "en",
  "pluralRule": "n != 1",
  "direction": "ltr",
  "type": "Base",
  "strings": {
    "gaseous_title": "Gaseous",
    "game.count.one": "{0} game",
    "game.count.other": "{0} games"
  },
  "serverstrings": {
    "startup.complete": "Startup complete"
  }
}
```

Example overlay file (`en-AU.json`):
```json
{
  "name": "English (Australia)",
  "nativeName": "English (Australia)",
  "code": "en-AU",
  "pluralRule": "n != 1",
  "direction": "ltr",
  "type": "Overlay",
  "strings": {
    "game.count.other": "{0} games" // Could override only changed entries
  },
  "serverstrings": {}
}
```

### Merging Strategy
1. Embedded resource locale (if present) is loaded.
2. File system locale (if present) is loaded.
3. These two are merged with the file system taking precedence.
4. Result is merged over the English base (`en`) so missing keys fall back gracefully.
5. Overlay locales (e.g. `en-AU`) can reference their parent language (`en`) implicitly via the merge.

### Caching
Loaded locales are cached in memory (`ConcurrentDictionary<string, LocaleFileModel>`) to avoid repeated disk or resource access.

## Locale Code Sanitisation
`Localisation.SanitiseLocale` ensures locale codes contain only letters, digits, hyphens or underscores and normalises them to `language-REGION` (lowercase language, uppercase region). Invalid input falls back to the default locale `en-AU`.

## Translation API
Use `Localisation.Translate(key)` to obtain a string for the current locale. With formatting parameters:
```csharp
Localisation.Translate("gaseous_title");
Localisation.Translate("game.count.other", new[]{ count.ToString() });
```

Plural-aware translation (after plural support update):
```csharp
Localisation.TranslatePlural("game.count", count, new[]{ count.ToString() });
```
This method resolves to either `game.count.one` or `game.count.other` based on the locale's plural rule expression.

## Pluralisation Model
The `pluralRule` field defines a boolean expression evaluated against the variable `n` (the absolute numeric count). If the expression evaluates to `true`, the plural form "other" is chosen; if `false`, the singular form "one" is chosen.

Typical English rule:
```
n != 1
```
Meaning: Use singular when `n == 1`; otherwise use plural.

### Supported Expression Syntax
The rule is a simple C#-like infix expression limited to:
- Comparison operators: `==`, `!=`, `<`, `<=`, `>`, `>=`
- Logical operators: `&&`, `||`
- Parentheses for grouping
- Integer literals
- Variable: `n`

If an expression cannot be parsed or evaluated safely, the fallback is:
- Singular when `n == 1`
- Otherwise plural

### Key Naming Convention for Plurals
Provide two keys under `strings` for pluralisable messages:
- `<base>.one`
- `<base>.other`

Example:
```json
"user.count.one": "{0} user",
"user.count.other": "{0} users"
```

If either variant is missing:
- Falls back to the existing variant.
- If none exist, returns the base key (`user.count`).

### Adding a New Locale
1. Create `xx.json` for the language (Base) with required metadata and strings.
2. (Optional) Create `xx-YY.json` (Overlay) for region-specific adjustments.
3. Ensure `pluralRule` matches the language's plural logic (simplified to two forms for now).
4. Restart the server (locale loaded at startup).

### Server Strings (`serverstrings`)
These are internal-only localisation values (ignored in API responses) intended for logs or diagnostics.

## Implementation Notes
- Evaluation of `pluralRule` is sandboxed: we implement a tiny parser/interpreter rather than dynamic compilation for safety.
- Future extension can add more plural categories (e.g. `few`, `many`) by expanding naming conventions and parser output.

## Example Usage
```csharp
int count = 5;
string message = Localisation.TranslatePlural("game.count", count, new[]{ count.ToString() });
// => "5 games"

count = 1;
message = Localisation.TranslatePlural("game.count", count, new[]{ count.ToString() });
// => "1 game"
```

## Fallback Behaviour Summary
1. Requested locale missing key -> fallback to English.
2. Plural variant missing -> fallback to other variant.
3. Base and variants missing -> return original key.

## Extending Plural Support
To support more than two plural forms (e.g., `zero`, `few`, `many`):
- Adopt CLDR plural classifications.
- Store rule set mapping from category to boolean expression.
- Lookup keys like `<base>.few`.

This is out of scope for current implementation but the design can evolve incrementally.

---
Generated documentation for plural rule and localisation architecture.
