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
    #localeChain = [];             // ordered chain of attempted locale files
    #pluralRules = null;           // Intl.PluralRules instance

    async Init(forceLoad = false) {
        // If already initialized and not forced, skip.
        if (this.#isReady && !forceLoad) return;
        this.#prepareState();
        if (!forceLoad) this.#deriveInitialLocale();
        this.locale = this.#normalizeLocale(this.locale);

        // Try cache first (unless forcing reload).
        if (!this.#loadFromCache(forceLoad)) {
            await this.#loadSingleLocaleWithFallback();
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

    // Translate with optional interpolation params
    translate(key, params = null) {
        let value = this.#languageData[key];
        if (value == null) {
            this.#recordMissingKey(key);
            return key; // fallback displays key
        }
        if (params) {
            // manual token replacement to avoid regex complexity flags
            for (const k of Object.keys(params)) {
                const token = `{{${k}}}`;
                if (value.includes(token)) value = value.split(token).join(String(params[k]));
            }
        }
        return value;
    }

    // Pluralization helper expecting keys like `${base}.one`, `${base}.other` etc.
    plural(baseKey, count, params = {}) {
        if (!this.#pluralRules) this.#pluralRules = new Intl.PluralRules(this.locale);
        const category = this.#pluralRules.select(count); // e.g. one, other
        const key = `${baseKey}.${category}`;
        return this.translate(key, { ...params, count });
    }

    // Translate all DOM elements with data-i18n
    translateAllElements() {
        const all = document.querySelectorAll('[data-i18n]');
        for (const elem of all) {
            this.translateElement(elem);
        }
    }

    // Translate a single element; supports attribute list and html opt-in
    translateElement(elem) {
        const key = elem.dataset.i18n;
        if (!key) return;
        const params = this.#extractParams(elem);
    const translated = this.translate(key, params);
    const wantsHtml = elem.dataset.i18nHtml !== undefined;
        if (wantsHtml) {
            elem.innerHTML = translated; // trusted / controlled translations only
        } else {
            elem.textContent = translated;
        }
        // attribute translations
        if (elem.dataset.i18nAttr) {
            const attrs = elem.dataset.i18nAttr.split(',').map(a => a.trim()).filter(Boolean);
            for (const attrName of attrs) {
                const attrKey = `${key}.${attrName}`;
                const attrValue = this.translate(attrKey, params);
                if (attrValue !== attrKey) elem.setAttribute(attrName, attrValue);
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
                    const nested = node.querySelectorAll?.('[data-i18n]');
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
            localeChain: this.#localeChain.slice(),
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

    // (Removed multi-file chain logic – server now returns fully merged locale file)

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
        this.#localeChain = [];
        this.#pluralRules = null;
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
                this.#localeChain = [this.locale];
                console.log(`Loaded language data for ${this.locale} from local storage.`);
                return true;
            }
        } catch { /* ignore */ }
        return false;
    }

    async #loadSingleLocaleWithFallback() {
        // Attempt desired locale first; on failure try its base (if variant), then fallback to 'en'.
        const desired = this.locale;
        const { baseLang, variant } = this.#splitLocale(desired);
        const attempts = [desired];
        if (variant) attempts.push(baseLang); // base language of the locale
        if (!attempts.includes('en')) attempts.push('en');
        this.#localeChain = [];
        for (const code of attempts) {
            try {
                const data = await this.#fetchLanguageFile(code);
                this.#assignLanguageData(data);
                this.#localeChain.push(code);
                // Stop after first successful fetch because server response is complete.
                return;
            } catch (err) {
                console.warn(err.message || `Failed to load locale ${code}`);
                this.#localeChain.push(code + ':failed');
            }
        }
        throw new Error('Failed to load any locale file (attempts: ' + attempts.join(', ') + ')');
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

    #splitLocale(locale) {
        const parts = locale.split('-');
        return { baseLang: parts[0].toLowerCase(), variant: parts.length > 1 ? parts.slice(1).join('-') : null };
    }

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
}

// Export the class as the default export
export default Language;
