# Localisation in gaseous-server

The gaseous-server provides an extensible localisation (internationalisation) system that supports base language files, overlay locale variants, server-only strings and pluralisation (including multi-category forms: one, few, many, other).

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

Plural-aware translation (multi-category support enabled):
```csharp
Localisation.TranslatePlural("game.count", count, new[]{ count.ToString() });
```
This method resolves to `game.count.<category>` where `<category>` may be one, few, many, or other depending on configured rules (falling back to legacy one/other when advanced rules are absent).

## Pluralisation Model
Pluralisation can be configured in two ways:

1. Legacy binary form using `pluralRule` (boolean expression over `n`).
  - If expression evaluates to `true` -> use `other` variant.
  - If expression evaluates to `false` -> use `one` variant.
2. Advanced multi-category form using `pluralRules` (a JSON object mapping category to expression).
  - Categories currently supported: `one`, `few`, `many`, `other`.
  - Each expression is evaluated; the first category in priority order that returns `true` is selected.
  - Evaluation priority order: one, few, many, other.
  - If no category matches, legacy fallback logic is applied (or `other`).

Typical English binary rule:
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
Legacy (binary): Provide two keys under `strings` for pluralisable messages:
- `<base>.one`
- `<base>.other`

Advanced multi-category (optional additional keys):
- `<base>.few`
- `<base>.many`
- `<base>.other` (always recommended)

Example:
```json
"user.count.one": "{0} user",
"user.count.other": "{0} users"
```

Fallback order when resolving plural forms:
1. Matched category key (e.g. `<base>.few`).
2. Other category keys in order: one, few, many, other (excluding matched).
3. Base key `<base>` if present.
4. Return the resolved category key string (as a last resort) so caller can see missing translation.

### Adding a New Locale
1. Create `xx.json` for the language (Base) with required metadata and strings.
2. (Optional) Create `xx-YY.json` (Overlay) for region-specific adjustments.
3. For binary pluralisation add `pluralRule` (e.g. `"pluralRule": "n != 1"`).
4. For multi-category pluralisation add `pluralRules` object, e.g.:
```json
"pluralRules": {
  "one": "n == 1",
  "few": "n >= 2 && n <= 4",
  "many": "n >= 5",
  "other": "n == 0"
}
```
5. Provide matching key variants in `strings`.
6. Restart the server (locale loaded at startup).

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

## Extending Plural Support Further
The current advanced implementation can be extended by adding more categories (e.g. zero) simply by:
1. Adding expressions for the new category in `pluralRules`.
2. Adding `<base>.zero` key in the locale file.
3. Updating evaluation order in code if priority needed.

For full CLDR compliance you could map categories directly from CLDR rules into expressions and maintain their standard evaluation order.

---
Generated documentation for plural rule and localisation architecture.
