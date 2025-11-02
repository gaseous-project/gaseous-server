class language {
    constructor() {

    }

    #languageData = {};
    #languageSettings = {};
    locale = "en";

    async Init(forceLoad = false) {
        // shortcut if already initialized
        if (Object.keys(this.#languageData).length > 0) {
            return;
        }

        // load base language
        if (localStorage.getItem("Language.Selected")) {
            this.locale = localStorage.getItem("Language.Selected");
        } else {
            this.locale = window.navigator.userLanguage || window.navigator.language;
        }
        // sanitize locale code
        // this.locale = this.locale.replace(/[^a-z0-9]/g, "");
        console.log("Browser locale: " + this.locale);

        // load language file
        // 1. load the base language first (eg. en for en-AU)
        // 2. load the specific language file (eg. en-AU) if available
        // this allows us to only override specific strings in the specific language file
        // and fall back to the base language for other strings
        // also allows us to add new languages that only override a few strings
        // without needing to duplicate the entire language file
        // if the base language file is not found, fall back to en
        let baseLang;
        let langVariant;
        let localeParts = this.locale.split("-");
        baseLang = localeParts[0].toLowerCase();
        if (localeParts.length > 1) {
            langVariant = localeParts[1].toUpperCase();
            this.locale = `${baseLang}-${langVariant}`;
        } else {
            this.locale = baseLang;
        }

        let loadLanguageFiles = true;
        if (localStorage.getItem(`Language.${this.locale}`) && forceLoad === false) {
            // check the stored version
            let storedVersion = JSON.parse(localStorage.getItem(`Language.${this.locale}`)).Version;
            let appVersion = localStorage.getItem('System.AppVersion') || '1.0.0';
            if (storedVersion === appVersion) {
                // load from local storage
                this.#languageData = JSON.parse(localStorage.getItem(`Language.${this.locale}`));
                this.#languageSettings = JSON.parse(localStorage.getItem(`Language.${this.locale}.Settings`));
                console.log(`Loaded language data for ${this.locale} from local storage.`);
                loadLanguageFiles = false;
            }
        }

        if (loadLanguageFiles === true || forceLoad === true) {
            console.log("Loading language files for: " + this.locale);

            // load english as fallback, then base language (if not english), then specific language
            await this.#loadLanguageFile('en');
            if (baseLang !== 'en') {
                await this.#loadLanguageFile(baseLang);
            }
            if (this.locale !== baseLang) {
                await this.#loadLanguageFile(this.locale);
            }

            // store in local storage
            let appVersion = localStorage.getItem('System.AppVersion') || '1.0.0';
            this.#languageData.Version = appVersion;
            localStorage.setItem(`Language.${this.locale}`, JSON.stringify(this.#languageData));
            localStorage.setItem(`Language.${this.locale}.Settings`, JSON.stringify(this.#languageSettings));

            console.log("Final locale: " + this.locale);
            console.log(this.#languageData);
        }
    }

    async #loadLanguageFile(langCode) {
        try {
            let response = await fetch(`/localisation/${langCode}.json?v=${localStorage.getItem('System.AppVersion') || '1.0.0'}`);
            if (!response.ok) {
                throw new Error(`Language file not found: ${langCode}`);
            }
            let data = await response.json();
            // merge strings
            this.#languageData = {
                ...this.#languageData,
                ...data.strings
            };
            this.#languageSettings = {
                name: data.name,
                nativeName: data.nativeName,
                code: data.code,
                pluralRule: data.pluralRule,
                direction: data.direction
            };

            console.log(`Loaded language file: ${langCode}`);
        } catch (error) {
            console.warn(error.message);
        }
    }

    translate(key) {
        if (this.#languageData[key]) {
            return this.#languageData[key];
        } else {
            this.#recordMissingKey(key);

            return key;
        }
    }

    translateAllElements() {
        document.querySelectorAll("[data-i18n]").forEach(elem => {
            let key = elem.getAttribute("data-i18n");
            elem.innerHTML = this.translate(key);
        });
    }

    translateElement(elem) {
        let key = elem.getAttribute("data-i18n");
        elem.innerHTML = this.translate(key);
    }

    #recordMissingKey(key) {
        // key not found - record it for future translation
        console.warn(`Missing translation key: ${key}`);
        localStorage.setItem(`Language.MissingTranslation.${this.locale}.${key}`, key);
    }
}