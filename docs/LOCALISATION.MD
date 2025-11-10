# Localisation in gaseous-server

## Language & Locale Inventory

<!-- LOCALE_TABLE_START -->
| Code | English Name | Native Name | Type | Parent | Pluralisation | Notes |
|------|--------------|-------------|------|--------|---------------|-------|
| en-AU | English (Australia) | English (Australia) | Overlay | en | pluralRule + pluralRules | Regional spelling |
| en-CA | English (Canada) | English (Canada) | Overlay | en | pluralRule | Regional spelling |
| en-GB | English (United Kingdom) | English (United Kingdom) | Overlay | en | pluralRule | Regional spelling |
| en-HK | English (Hong Kong) | English (Hong Kong) | Overlay | en | pluralRule | Regional spelling |
| en-IE | English (Ireland) | English (Ireland) | Overlay | en | pluralRule | Regional spelling |
| en-IN | English (India) | English (India) | Overlay | en | pluralRule | Regional spelling |
| en-NZ | English (New Zealand) | English (New Zealand) | Overlay | en | pluralRule | Regional spelling |
| en-SG | English (Singapore) | English (Singapore) | Overlay | en | pluralRule | Regional spelling |
| en-US | English (American English) | English (American English) | Overlay | en | pluralRule | Acts as default American variant |
| en-ZA | English (South Africa) | English (South Africa) | Overlay | en | pluralRule | Regional spelling |
| en | English | English | Base | — | pluralRule + pluralRules | Provides full master string set & advanced multi-category rules |
| es-419 | Español (Latinoamérica) | Español (Latinoamérica) | Overlay | es | pluralRule |  |
| es-AR | Español (Argentina) | Español (Argentina) | Overlay | es | pluralRule |  |
| es-BO | Español (Bolivia) | Español (Bolivia) | Overlay | es | pluralRule |  |
| es-CL | Español (Chile) | Español (Chile) | Overlay | es | pluralRule |  |
| es-CO | Español (Colombia) | Español (Colombia) | Overlay | es | pluralRule |  |
| es-CR | Español (Costa Rica) | Español (Costa Rica) | Overlay | es | pluralRule |  |
| es-CU | Español (Cuba) | Español (Cuba) | Overlay | es | pluralRule |  |
| es-DO | Español (República Dominicana) | Español (República Dominicana) | Overlay | es | pluralRule |  |
| es-EC | Español (Ecuador) | Español (Ecuador) | Overlay | es | pluralRule |  |
| es-ES | Español (España) | Español (España) | Overlay | es | pluralRule |  |
| es-GT | Español (Guatemala) | Español (Guatemala) | Overlay | es | pluralRule |  |
| es-HN | Español (Honduras) | Español (Honduras) | Overlay | es | pluralRule |  |
| es-MX | Español (México) | Español (México) | Overlay | es | pluralRule |  |
| es-NI | Español (Nicaragua) | Español (Nicaragua) | Overlay | es | pluralRule |  |
| es-PA | Español (Panamá) | Español (Panamá) | Overlay | es | pluralRule |  |
| es-PE | Español (Perú) | Español (Perú) | Overlay | es | pluralRule |  |
| es-PR | Español (Puerto Rico) | Español (Puerto Rico) | Overlay | es | pluralRule |  |
| es-PY | Español (Paraguay) | Español (Paraguay) | Overlay | es | pluralRule |  |
| es-SV | Español (El Salvador) | Español (El Salvador) | Overlay | es | pluralRule |  |
| es-UY | Español (Uruguay) | Español (Uruguay) | Overlay | es | pluralRule |  |
| es-VE | Español (Venezuela) | Español (Venezuela) | Overlay | es | pluralRule |  |
| es | Español | Español | Base | — | pluralRule + pluralRules |  |
| fr-BE | Français (Belgique) | Français (Belgique) | Overlay | fr | pluralRule + pluralRules |  |
| fr-CA | Français (Canada) | Français (Canada) | Overlay | fr | pluralRule + pluralRules |  |
| fr-CH | Français (Suisse) | Français (Suisse) | Overlay | fr | pluralRule + pluralRules |  |
| fr-DZ | Français (Algérie) | Français (Algérie) | Overlay | fr | pluralRule + pluralRules |  |
| fr-FR | Français (France) | Français (France) | Overlay | fr | pluralRule + pluralRules |  |
| fr-LU | Français (Luxembourg) | Français (Luxembourg) | Overlay | fr | pluralRule + pluralRules |  |
| fr-MA | Français (Maroc) | Français (Maroc) | Overlay | fr | pluralRule + pluralRules |  |
| fr-MC | Français (Monaco) | Français (Monaco) | Overlay | fr | pluralRule + pluralRules |  |
| fr-SN | Français (Sénégal) | Français (Sénégal) | Overlay | fr | pluralRule + pluralRules |  |
| fr-TN | Français (Tunisie) | Français (Tunisie) | Overlay | fr | pluralRule + pluralRules |  |
| fr | Français | Français | Base | — | pluralRule + pluralRules |  |<!-- LOCALE_TABLE_END -->

All overlay locales inherit missing keys from the base English (`en`). Where both embedded resource and file system versions exist, the file system locale overrides the embedded resource, and overlays extend their parent before final merge with `en` for safety.

The gaseous-server provides an extensible localisation (internationalisation) system that supports base language files, overlay locale variants, server-only strings and pluralisation (including multi-category forms: zero, one, few, many, other).

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
Loading and merging occurs in deterministic stages inside `Localisation.GetLanguageFile`:
1. Ensure base English (`en`) is resident (merged resource + file system, file system wins when both exist).
2. Load requested locale from embedded resources (if present).
3. Load requested locale from file system path `Support/Localisation` (if present).
4. Merge resource + file system version (file system wins) into a working locale.
5. If the working locale is an overlay (`type: Overlay`), its base parent is merged first (resource or file), producing a fully expanded overlay.
6. Finally merge the expanded overlay over English (`en`) so any missing keys fall back automatically.

Result: All lookups safely fall back to English strings while preserving region overrides.

### Caching
Loaded locales are cached in memory (`ConcurrentDictionary<string, LocaleFileModel>`) keyed by locale code. Subsequent calls to `Translate`/`TranslatePlural` do not incur I/O or deserialization cost.

## Locale Code Sanitisation
`Localisation.SanitiseLocale` strips invalid characters and normalises formats:
* Input examples: `EN_us`, `en-us`, `en_US` -> `en-US`
* Language only inputs (`en`) return lowercase language.
* Invalid or empty input => fallback to `DefaultLocale` (`en-AU`).

## Translation API
Primary API surface (server-side):
* `Localisation.Translate(key, args?, useServerStrings?)`
* `Localisation.TranslatePlural(baseKey, count, args?, useServerStrings?)`

Use `Localisation.Translate(key)` to obtain a string for the current locale. With formatting parameters:
```csharp
Localisation.Translate("gaseous_title");
Localisation.Translate("game.count.other", new[]{ count.ToString() });
```

Plural-aware translation (multi-category support enabled):
```csharp
Localisation.TranslatePlural("game.count", count, new[]{ count.ToString() });
```
This method resolves to `game.count.<category>` where `<category>` may be zero, one, few, many, or other depending on configured rules (falling back to legacy one/other when advanced rules are absent).

## Pluralisation Model
Pluralisation can be configured in two ways:

1. Legacy binary form using `pluralRule` (boolean expression over `n`).
  - If expression evaluates to `true` -> use `other` variant.
  - If expression evaluates to `false` -> use `one` variant.
2. Advanced multi-category form using `pluralRules` (a JSON object mapping category to expression).
  - Categories currently supported: `zero`, `one`, `few`, `many`, `other`.
  - Each expression is evaluated; the first category in priority order that returns `true` is selected.
  - Evaluation priority order: zero, one, few, many, other.
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
- `<base>.zero`
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
2. Other category keys in order: zero, one, few, many, other (excluding matched).
3. Base key `<base>` if present.
4. Return the resolved category key string (as a last resort) so caller can see missing translation.

### Adding a New Locale
1. Create `xx.json` for the language (Base) with required metadata and strings.
2. (Optional) Create `xx-YY.json` (Overlay) for region-specific adjustments.
3. For binary pluralisation add `pluralRule` (e.g. `"pluralRule": "n != 1"`).
4. For multi-category pluralisation add `pluralRules` object, e.g.:
```json
"pluralRules": {
  "zero": "n == 0",
  "one": "n == 1",
  "few": "n >= 2 && n <= 4",
  "many": "n >= 5",
  "other": "n == 0" // acts as catch-all in this example
}
```
5. Provide matching key variants in `strings`.
6. Restart the server (locale loaded at startup).

### Server Strings (`serverstrings`)
These are internal-only localisation values (ignored in API responses) intended for logs or diagnostics. They are accessed by passing `useServerStrings=true` to translation methods.

## Implementation Notes
Key internal mechanics:
* Plural evaluation uses a custom safe recursive descent parser (no dynamic compilation) supporting comparisons, logical operators and parentheses.
* Overlay inheritance happens early (resource/file parent merge) then final fallback merge with base English.
* `GetActiveDictionary` switches between `Strings` and `ServerStrings` via `useServerStrings` flag.
* Cache uses `ConcurrentDictionary<string, LocaleFileModel>` for thread-safe read/write.
* Missing plural forms cascade through an ordered fallback chain (`resolved`, zero, one, few, many, other, baseKey) before returning a key echo.
* Advanced plural rules (`pluralRules`) supersede legacy `pluralRule` when provided; otherwise legacy binary logic applies.

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
1. Requested locale missing key -> fallback to its merged English base.
2. Plural variant missing -> walk fallback chain (other categories + base key).
3. All lookup attempts fail -> return final resolved key or baseKey (caller can log missing path).

## Extending Localisation
Plural support can be broadened by adding categories (already supports zero/one/few/many/other). For CLDR alignment you could:
1. Import CLDR rule expressions into `pluralRules`.
2. Adjust evaluation order if CLDR specifies alternative priority.
3. Add corresponding `<base>.<category>` keys.

Additional extensibility ideas:
* Introduce non-English base languages (e.g. `fr`, `es`) — mark one as canonical fallback per language family.
* Add automated locale enumeration endpoint distinct from full file to reduce payload (`LocaleSummaryModel`).
* Provide build-time validation script to verify plural keys completeness.
* Add direction `rtl` example (e.g. Arabic) to ensure UI respects `direction` metadata.

---
Generated & updated documentation for localisation architecture (merged resources, overlay inheritance, plural evaluation, server string usage) and current English variants table.
