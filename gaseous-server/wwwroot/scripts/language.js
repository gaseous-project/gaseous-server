class Language {
    constructor() {
        // attach unload flush of missing keys; using globalThis for broader environment support
        globalThis.addEventListener?.('beforeunload', () => this.#flushMissingKeys());
    }

    #languageData = {};            // flattened translation map
    #languageSettings = {};        // metadata for current locale
    #missingKeys = new Set();      // aggregate missing keys
    #isReady = false;              // initialization flag
    locale = 'en';                 // current effective locale
    #pluralRules = null;           // Intl.PluralRules instance
    #advancedPluralRules = null;   // map of plural category -> expression string
    #binaryPluralRule = null;      // simple plural rule expression (e.g. "n != 1")

    async Init(forceLoad = false) {
        // If already initialized and not forced, skip.
        if (this.#isReady && !forceLoad) return;
        this.#prepareState();
        if (!forceLoad) this.#deriveInitialLocale();
        this.locale = this.#normalizeLocale(this.locale);

        // Try cache first (unless forcing reload).
        if (!this.#loadFromCache(forceLoad)) {
            await this.#loadSingleLocale();
            this.#persistCache();
        }

        this.applyDirection();
        this.#pluralRules = new Intl.PluralRules(this.locale);
        this.#isReady = true;
    }

    async setLocale(newLocale, options = {}) {
        const { forceReload = false } = options;
        if (!newLocale) return;
        if (newLocale === this.locale && !forceReload) return;
        localStorage.setItem('Language.Selected', newLocale);
        this.locale = newLocale;
        this.#missingKeys.clear();
        await this.Init(true);
        this.translateAllElements();
    }

    // Translate with optional interpolation params or positional args (array)
    // Supports two styles to mirror server behaviour:
    //  - Positional: value contains {0} {1} ... and caller passes an array.
    //  - Named: value contains {{name}} tokens and caller passes an object.
    translate(key, args = null) {
        const direct = this.#languageData[key];
        if (direct == null) {
            this.#recordMissingKey(key);
            return key;
        }
        return this.#formatValue(direct, args);
    }

    // Pluralisation aligned with server logic in Localisation.TranslatePlural
    // Order: evaluate advanced pluralRules (zero, one, few, many, other) first; else binary pluralRule (default n != 1)
    // Fallback chain: resolvedKey -> all category variants -> baseKey
    plural(baseKey, count, args = null) {
        let resolvedKey = null;
        const rules = this.#advancedPluralRules;
        if (rules && typeof rules === 'object' && Object.keys(rules).length > 0) {
            const order = ['zero', 'one', 'few', 'many', 'other'];
            for (const cat of order) {
                if (Object.prototype.hasOwnProperty.call(rules, cat)) {
                    if (this.#evaluatePluralRule(rules[cat], count)) {
                        resolvedKey = `${baseKey}.${cat}`;
                        break;
                    }
                }
            }
        }
        if (!resolvedKey) {
            const rule = this.#binaryPluralRule || 'n != 1';
            const isPlural = this.#evaluatePluralRule(rule, count);
            resolvedKey = isPlural ? `${baseKey}.other` : `${baseKey}.one`;
        }

        const fallbackKeys = [];
        const seen = new Set();
        const push = k => { if (!seen.has(k)) { seen.add(k); fallbackKeys.push(k); } };
        push(resolvedKey);
        for (const cat of ['zero', 'one', 'few', 'many', 'other']) push(`${baseKey}.${cat}`);
        push(baseKey);

        let value = null; let usedKey = null;
        for (const k of fallbackKeys) {
            if (this.#languageData[k] != null) { value = this.#languageData[k]; usedKey = k; break; }
        }
        if (value == null) {
            // record only base key as missing to avoid noise
            this.#recordMissingKey(baseKey);
            return baseKey;
        }
        // merge count into named arguments if object form used
        if (args && !Array.isArray(args) && typeof args === 'object' && !Object.prototype.hasOwnProperty.call(args, 'count')) {
            args = { ...args, count };
        } else if (!args) {
            // allow positional usage where {0} is count
            args = [count];
        }
        return this.#formatValue(value, args);
    }

    // Translate all DOM elements with data-i18n
    translateAllElements() {
        const all = document.querySelectorAll('[data-i18n]');
        for (const elem of all) {
            this.translateElement(elem);
        }
    }

    // Translate a single element.
    // Supports:
    //  1) data-i18n for inner text/HTML (optionally data-i18n-html)
    //  2) data-i18n-attr="attr:key,attr2:key2" OR existing format "attr:key" (single) even without data-i18n
    //     allowing attribute-only translation on elements without visible text.
    translateElement(elem) {
        const hasTextKey = elem.dataset.i18n != null;
        const params = this.#extractParams(elem);

        // Handle text / html content translation if key present
        if (hasTextKey) {
            const key = elem.dataset.i18n;
            const translated = this.translate(key, params);
            const wantsHtml = elem.dataset.i18nHtml !== undefined;
            if (wantsHtml) {
                elem.innerHTML = translated; // trusted / controlled translations only
            } else {
                elem.textContent = translated;
            }
        }

        // Attribute-only translation path.
        // data-i18n-attr expected format: "attr:key" pairs separated by commas.
        // Backwards compatibility: if element also had data-i18n (old behaviour), we derive attr keys as key.attrName.
        const attrSpec = elem.dataset.i18nAttr || elem.dataset.i18nAttrs; // support legacy i18nAttrs
        if (attrSpec) {
            // Normalise list: allow either key.attr style (legacy) or attr:key style (new).
            // Detect style: if first segment contains ':' use attr:key pairs.
            const parts = attrSpec.split(',').map(p => p.trim()).filter(Boolean);
            const usesExplicitPairs = parts.some(p => p.includes(':'));
            if (usesExplicitPairs) {
                for (const pair of parts) {
                    const [attrName, attrKey] = pair.split(':').map(s => s.trim());
                    if (!attrName || !attrKey) continue;
                    const attrValue = this.translate(attrKey, params);
                    if (attrValue !== attrKey) {
                        elem.setAttribute(attrName, attrValue);
                    } else {
                        // attribute translation missing
                        this.#recordMissingKey(attrKey);
                    }
                }
            } else if (hasTextKey) {
                // Legacy behaviour: attrs list like "placeholder,value" -> attr keys derived from text key
                const textKey = elem.dataset.i18n;
                for (const attrName of parts) {
                    const derivedKey = `${textKey}.${attrName}`;
                    const attrValue = this.translate(derivedKey, params);
                    if (attrValue !== derivedKey) {
                        elem.setAttribute(attrName, attrValue);
                    } else {
                        this.#recordMissingKey(derivedKey);
                    }
                }
            }
        }
    }

    // Observe dynamic content insertion
    observeDynamicContent() {
        const observer = new MutationObserver(mutations => {
            for (const mutation of mutations) {
                for (const node of mutation.addedNodes) {
                    if (node.nodeType !== 1) continue;
                    if (node.dataset?.i18n !== undefined) this.translateElement(node);
                    const nested = node.querySelectorAll?.('[data-i18n],[data-i18n-attr]');
                    if (nested) {
                        for (const el of nested) this.translateElement(el);
                    }
                }
            }
        });
        observer.observe(document.body, { childList: true, subtree: true });
        return observer;
    }

    applyDirection() {
        if (this.#languageSettings?.direction) {
            document.documentElement.setAttribute('dir', this.#languageSettings.direction);
        }
    }

    isReady() { return this.#isReady; }

    getDiagnostics() {
        const missingObj = this.#getMissingKeysObject();
        return {
            locale: this.locale,
            keysLoaded: Object.keys(this.#languageData).length,
            missingCount: Object.keys(missingObj).length,
            missingKeys: Object.keys(missingObj)
        };
    }

    // PRIVATE HELPERS
    #recordMissingKey(key) {
        if (!this.#missingKeys.has(key)) {
            console.warn(`Missing translation key: ${key}`);
            this.#missingKeys.add(key);
            if (this.#missingKeys.size % 25 === 0) this.#flushMissingKeys();
        }
    }

    #flushMissingKeys() {
        if (this.#missingKeys.size === 0) return;
        const existing = this.#getMissingKeysObject();
        for (const k of this.#missingKeys) existing[k] = true;
        localStorage.setItem(`Language.MissingTranslations.${this.locale}`, JSON.stringify(existing));
    }

    #getMissingKeysObject() {
        try {
            return JSON.parse(localStorage.getItem(`Language.MissingTranslations.${this.locale}`)) || {};
        } catch { return {}; }
    }

    // Server returns fully merged locale file; no fallback chain logic required.

    async #fetchLanguageFile(langCode) {
        const version = localStorage.getItem('System.AppVersion') || '1.0.0';
        const url = `/api/v1.1/Localisation?locale=${encodeURIComponent(langCode)}&v=${encodeURIComponent(version)}`;
        const etagKey = `Language.${langCode}.ETag`;
        const headers = {};
        const existingEtag = localStorage.getItem(etagKey);
        if (existingEtag) headers['If-None-Match'] = existingEtag;
        const response = await fetch(url, { cache: 'no-cache', headers });
        if (response.status === 304) {
            // Use cached language data (caller ensured loadFromCache already tried for current locale, but on fallback attempts we may still rely on it)
            const cached = localStorage.getItem(`Language.${langCode}`);
            if (cached) {
                console.log(`Language ${langCode} not modified (304). Using cache.`);
                return JSON.parse(cached);
            }
            // If 304 but no cached value, treat as failure to force fallback
            throw new Error(`Language file 304 but no cache present: ${langCode}`);
        }
        if (!response.ok) throw new Error(`Language file not found: ${langCode}`);
        if (response.status === 204) throw new Error(`Language file empty: ${langCode}`);
        const etag = response.headers.get('ETag');
        if (etag) localStorage.setItem(etagKey, etag);
        const data = await response.json();
        console.log(`Fetched merged language file: ${langCode}` + (etag ? ` (etag ${etag})` : ''));
        return data;
    }

    // Replace merge with simple assign because server returns a fully merged structure
    #assignLanguageData(data) {
        const strings = data.strings || {};
        this.#languageData = { ...strings }; // direct copy
        this.#languageSettings = {
            name: data.name,
            nativeName: data.nativeName,
            code: data.code || this.locale,
            pluralRule: data.pluralRule,
            direction: data.direction || 'ltr'
        };
        this.#advancedPluralRules = data.pluralRules || null; // dictionary of category -> expression
        this.#binaryPluralRule = data.pluralRule || null;
    }

    #normalizeLocale(input) {
        if (!input) return 'en';
        const norm = input.replace('_', '-').trim();
        const parts = norm.split('-').filter(Boolean);
        if (parts.length === 0) return 'en';
        const primary = parts[0].toLowerCase();
        const rest = [];
        for (let i = 1; i < parts.length; i++) {
            const seg = parts[i];
            if (/^[a-z]{2}$/i.test(seg)) {
                rest.push(seg.toUpperCase());
            } else {
                rest.push(seg); // script or other segment
            }
        }
        return [primary, ...rest].join('-');
    }

    // State preparation helpers extracted to reduce complexity
    #prepareState() {
        this.#isReady = false;
        this.#languageData = {};
        this.#languageSettings = {};
        this.#pluralRules = null;
        this.#advancedPluralRules = null;
        this.#binaryPluralRule = null;
    }

    #deriveInitialLocale() {
        const stored = localStorage.getItem('Language.Selected');
        if (stored) {
            this.locale = stored;
            return;
        }
        const nav = globalThis.navigator || {};
        this.locale = nav.userLanguage || nav.language || 'en';
    }

    #loadFromCache(forceLoad) {
        if (forceLoad) return false;
        const raw = localStorage.getItem(`Language.${this.locale}`);
        if (!raw) return false;
        try {
            const stored = JSON.parse(raw);
            const storedVersion = stored.Version;
            const appVersion = localStorage.getItem('System.AppVersion') || '1.0.0';
            if (storedVersion === appVersion) {
                this.#languageData = stored;
                this.#languageSettings = JSON.parse(localStorage.getItem(`Language.${this.locale}.Settings`)) || {};
                console.log(`Loaded language data for ${this.locale} from local storage.`);
                return true;
            }
        } catch { /* ignore */ }
        return false;
    }

    async #loadSingleLocale() {
        // Attempt selected locale, then server language, finally hardcoded 'en-AU'.
        try {
            const data = await this.#fetchLanguageFile(this.locale);
            this.#assignLanguageData(data);
            document.documentElement.removeAttribute('data-fallback');
            return;
        } catch (primaryError) {
            console.warn(`Primary locale '${this.locale}' failed: ${primaryError.message}`);
        }

        // Server language fallback
        let serverLang = null;
        try { serverLang = await this.#fetchServerLanguage(); } catch { /* ignore */ }
        if (serverLang && serverLang !== this.locale) {
            try {
                const data2 = await this.#fetchLanguageFile(serverLang);
                this.#assignLanguageData(data2);
                this.locale = serverLang;
                localStorage.setItem('Language.Selected', serverLang);
                document.documentElement.setAttribute('data-fallback', 'true');
                console.warn(`Fell back to server language '${serverLang}'.`);
                return;
            } catch (serverErr) {
                console.error(`Server language '${serverLang}' failed: ${serverErr.message}`);
            }
        }

        // Final guaranteed fallback to en-AU
        const finalFallback = 'en-AU';
        if (finalFallback !== this.locale && finalFallback !== serverLang) {
            try {
                const data3 = await this.#fetchLanguageFile(finalFallback);
                this.#assignLanguageData(data3);
                this.locale = finalFallback;
                localStorage.setItem('Language.Selected', finalFallback);
                document.documentElement.setAttribute('data-fallback', 'true');
                console.warn(`Fell back to final locale '${finalFallback}'.`);
                return;
            } catch (finalErr) {
                console.error(`Final fallback '${finalFallback}' failed: ${finalErr.message}`);
            }
        }

        throw new Error('All locale loading attempts failed (selected, server language, en-AU).');
    }

    async #fetchServerLanguage() {
        try {
            const res = await fetch('/api/v1.1/Localisation/server-language', { cache: 'no-cache' });
            if (!res.ok) return null;
            const json = await res.json();
            return this.#normalizeLocale(json.serverLanguage || 'en');
        } catch { return null; }
    }

    #persistCache() {
        const appVersion = localStorage.getItem('System.AppVersion') || '1.0.0';
        // store merged language data with version marker
        const toStore = { ...this.#languageData, Version: appVersion };
        localStorage.setItem(`Language.${this.locale}`, JSON.stringify(toStore));
        localStorage.setItem(`Language.${this.locale}.Settings`, JSON.stringify(this.#languageSettings));
        console.log('Cached merged locale: ' + this.locale);
    }

    // Fetch list of available locales (for building a selection UI)
    async fetchAvailableLocales() {
        try {
            const res = await fetch('/api/v1.1/Localisation/available', { cache: 'no-cache' });
            if (!res.ok) throw new Error('Failed to load available locales');
            return await res.json(); // array of { code, name, nativeName }
        } catch (e) {
            console.warn(e.message);
            return [];
        }
    }

    // Removed #splitLocale since no fallback chain logic is needed.

    #extractParams(elem) {
        // data-i18n-params="key1:value1;key2:value2" simple format
        const raw = elem.dataset.i18nParams;
        if (!raw) return null;
        return raw.split(';').reduce((acc, pair) => {
            const [k, v] = pair.split(':');
            if (k) acc[k.trim()] = (v || '').trim();
            return acc;
        }, {});
    }

    // Formatting helper: supports positional {0} style and named {{name}} style concurrently.
    #formatValue(value, args) {
        if (args == null) return value;
        let out = value;
        if (Array.isArray(args)) {
            // positional replacement; naive but safe
            for (let i = 0; i < args.length; i++) {
                const token = new RegExp('\\{' + i + '\\}', 'g');
                out = out.replace(token, String(args[i]));
            }
        } else if (typeof args === 'object') {
            for (const k of Object.keys(args)) {
                const token = `{{${k}}}`;
                if (out.includes(token)) out = out.split(token).join(String(args[k]));
            }
        } else {
            // single value replacement for {0}
            const token = new RegExp('\\{0\\}', 'g');
            out = out.replace(token, String(args));
        }
        return out;
    }

    // Plural rule expression evaluator (supports n, integers, (), ==, !=, <, <=, >, >=, &&, ||)
    #evaluatePluralRule(expression, n) {
        try {
            const tokens = this.#tokenizeRule(expression);
            let index = 0;
            return this.#parseOr(tokens, () => index, v => { index = v; }, n);
        } catch {
            return n !== 1; // default
        }
    }
    #tokenizeRule(expr) {
        const tokens = [];
        for (let i = 0; i < expr.length;) {
            const c = expr[i];
            if (/\s/.test(c)) { i++; continue; }
            if (/[0-9]/.test(c)) {
                let start = i; while (i < expr.length && /[0-9]/.test(expr[i])) i++; tokens.push(expr.slice(start, i)); continue;
            }
            if (/[A-Za-z]/.test(c)) {
                let start = i; while (i < expr.length && /[A-Za-z]/.test(expr[i])) i++; tokens.push(expr.slice(start, i)); continue;
            }
            const two = expr.slice(i, i + 2);
            if (["==", "!=", "<=", ">=", "&&", "||"].includes(two)) { tokens.push(two); i += 2; continue; }
            tokens.push(c); i++;
        }
        return tokens;
    }
    #parseOr(tokens, getIndex, setIndex, n) {
        let left = this.#parseAnd(tokens, getIndex, setIndex, n);
        while (getIndex() < tokens.length && tokens[getIndex()] === '||') { setIndex(getIndex() + 1); const right = this.#parseAnd(tokens, getIndex, setIndex, n); left = left || right; }
        return left;
    }
    #parseAnd(tokens, getIndex, setIndex, n) {
        let left = this.#parseComparison(tokens, getIndex, setIndex, n);
        while (getIndex() < tokens.length && tokens[getIndex()] === '&&') { setIndex(getIndex() + 1); const right = this.#parseComparison(tokens, getIndex, setIndex, n); left = left && right; }
        return left;
    }
    #parseComparison(tokens, getIndex, setIndex, n) {
        let leftVal = this.#parseValue(tokens, getIndex, setIndex, n);
        if (getIndex() >= tokens.length) return leftVal !== 1;
        const op = tokens[getIndex()];
        if (!['==', '!=', '<', '<=', '>', '>='].includes(op)) return leftVal !== 1;
        setIndex(getIndex() + 1);
        const rightVal = this.#parseValue(tokens, getIndex, setIndex, n);
        switch (op) {
            case '==': return leftVal === rightVal;
            case '!=': return leftVal !== rightVal;
            case '<': return leftVal < rightVal;
            case '<=': return leftVal <= rightVal;
            case '>': return leftVal > rightVal;
            case '>=': return leftVal >= rightVal;
        }
        return false;
    }
    #parseValue(tokens, getIndex, setIndex, n) {
        if (getIndex() >= tokens.length) return 0;
        const t = tokens[getIndex()];
        if (t === '(') {
            setIndex(getIndex() + 1);
            const inner = this.#parseOr(tokens, getIndex, setIndex, n);
            if (getIndex() < tokens.length && tokens[getIndex()] === ')') setIndex(getIndex() + 1);
            return inner ? 1 : 0;
        }
        if (/^n$/i.test(t)) { setIndex(getIndex() + 1); return n; }
        if (/^[0-9]+$/.test(t)) { setIndex(getIndex() + 1); return parseInt(t, 10); }
        setIndex(getIndex() + 1); return 0;
    }
}

// Export the class as the default export
export default Language;
