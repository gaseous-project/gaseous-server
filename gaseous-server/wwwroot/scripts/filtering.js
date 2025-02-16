class Filtering {
    constructor(applyCallback) {
        this.applyCallback = applyCallback;
    }

    #filterSelections = {

    }

    #filterCollapsed = {

    }

    async LoadFilterSettings() {
        await this.#LoadFilterSettings();
        await this.#LoadFilterCollapsedStatus();
    }

    ApplyFilter() {
        // delete entries from filterSelections that are false or empty
        for (let key in this.#filterSelections) {
            if (typeof this.#filterSelections[key] === 'object') {
                for (let subKey in this.#filterSelections[key]) {
                    if (this.#filterSelections[key][subKey] === false) {
                        delete this.#filterSelections[key][subKey];
                    }
                }
            } else {
                if (this.#filterSelections[key] === false || this.#filterSelections[key] === '') {
                    delete this.#filterSelections[key];
                }
            }
        }

        // delete keys from filterSelections that are empty
        for (let key in this.#filterSelections) {
            if (typeof this.#filterSelections[key] === 'object') {
                if (Object.keys(this.#filterSelections[key]).length === 0) {
                    delete this.#filterSelections[key];
                }
            }
        }

        // store the filter selections in local storage
        db.SetData('settings', 'libraryFilter', this.#filterSelections);

        let results = [];

        // return a list of ids that match the filter
        let transaction = db.db.transaction('games', 'readonly');
        let store = transaction.objectStore('games');

        let filter = this.#filterSelections;

        let request = store.openCursor();
        request.onsuccess = async () => {
            let cursor = request.result;
            if (cursor) {
                let game = cursor.value;
                let textMatch = true;
                let settingsMatch = true;
                let platformsMatch = true;
                let genresMatch = true;
                let themesMatch = true;
                let releaseYearMatch = true;
                let playersMatch = true;
                let perspectivesMatch = true;
                let userRatingMatch = true;
                let userVoteCountMatch = true;

                // check each filter
                for (let key in filter) {
                    switch (key.toLowerCase()) {
                        case 'search':
                            if (game.name.toLowerCase().includes(filter[key].toLowerCase()) === false) {
                                textMatch = false;
                            }
                            break;

                        case 'settings':
                            for (let setting in filter[key]) {
                                if (game[setting] !== filter[key][setting]) {
                                    settingsMatch = false;
                                }
                            }
                            break;

                        case 'platforms':
                            for (let platform in filter[key]) {
                                if (game.platformIds.includes(Number(platform)) === false) {
                                    platformsMatch = false;
                                }
                            }
                            break;

                        case 'genres':
                            for (let genre in filter[key]) {
                                if (game.genres.includes(genre) === false) {
                                    genresMatch = false;
                                }
                            }
                            break;

                        case 'themes':
                            for (let theme in filter[key]) {
                                if (game.themes.includes(theme) === false) {
                                    themesMatch = false;
                                }
                            }
                            break;

                        case 'releaseyear':
                            // convert release year to a date
                            let releaseDate = new Date(game.firstReleaseDate);

                            // get release year from date
                            let releaseYear = releaseDate.getFullYear();

                            if (filter[key].min) {
                                if (releaseYear < filter[key].min) {
                                    releaseYearMatch = false;
                                }
                            }
                            if (filter[key].max) {
                                if (releaseYear > filter[key].max) {
                                    releaseYearMatch = false;
                                }
                            }
                            break;

                        case 'players':
                            for (let player in filter[key]) {
                                if (game.players.includes(player) === false) {
                                    playersMatch = false;
                                }
                            }
                            break;

                        case 'perspectives':
                            for (let perspective in filter[key]) {
                                if (game.perspectives.includes(perspective) === false) {
                                    perspectivesMatch = false;
                                }
                            }
                            break;

                        case 'userrating':
                            if (game.totalRating) {
                                if (filter[key].min) {
                                    if (game.totalRating < Number(filter[key].min)) {
                                        userRatingMatch = false;
                                    }
                                }
                                if (filter[key].max) {
                                    if (game.totalRating > Number(filter[key].max)) {
                                        userRatingMatch = false;
                                    }
                                }
                            } else {
                                userRatingMatch = false;
                            }
                            break;

                        case 'uservotecount':
                            if (game.totalRatingCount) {
                                if (filter[key].min) {
                                    if (game.totalRatingCount < Number(filter[key].min)) {
                                        userVoteCountMatch = false;
                                    }
                                }
                                if (filter[key].max) {
                                    if (game.totalRatingCount > Number(filter[key].max)) {
                                        userVoteCountMatch = false;
                                    }
                                }
                            } else {
                                userVoteCountMatch = false;
                            }
                            break;

                        default:
                            break;
                    }
                }

                if (
                    textMatch === true &&
                    settingsMatch === true &&
                    platformsMatch === true &&
                    genresMatch === true &&
                    themesMatch === true &&
                    releaseYearMatch === true &&
                    playersMatch === true &&
                    perspectivesMatch === true &&
                    userRatingMatch === true &&
                    userVoteCountMatch === true
                ) {
                    results.push(game);
                }

                cursor.continue();
            }
        };

        transaction.oncomplete = () => {
            // sort results by name
            results.sort((a, b) => {
                return a.name.localeCompare(b.name);
            });

            for (let i = 0; i < results.length; i++) {
                results[i]['resultIndex'] = i;
            }

            if (this.applyCallback) {
                this.applyCallback(results);
            }
        };
    }

    async #LoadFilterSettings() {
        let data = await db.GetData('settings', 'libraryFilter');

        if (data) {
            this.#filterSelections = data.value;
        }
    }

    async #LoadFilterCollapsedStatus() {
        let data = await db.GetData('settings', 'libraryFilterCollapsed');

        if (data) {
            this.#filterCollapsed = data.value;
        }
    }

    async #StoreFilterCollapsedStatus(section, collapsed) {
        this.#filterCollapsed[section] = collapsed;

        db.SetData('settings', 'libraryFilterCollapsed', this.#filterCollapsed);
    }

    BuildFilterTable(filter) {
        let targetElement = document.createElement('div');
        targetElement.id = 'games_filter';

        let panel = document.createElement('div');
        panel.id = 'games_filter_box';

        // free text search
        let searchCollapsed = true;
        if (this.#filterCollapsed['Title Search'] !== undefined) {
            searchCollapsed = this.#filterCollapsed['Title Search'];
        }
        panel.appendChild(this.#BuildBasicPanel('Title Search', true, searchCollapsed, this.#BuildStringPanel('search', 'Title Search'), null));

        // settings filter
        let settingsCollapsed = true;
        if (this.#filterCollapsed['Settings'] !== undefined) {
            settingsCollapsed = this.#filterCollapsed['Settings'];
        }
        panel.appendChild(this.#BuildBasicPanel('Settings', true, settingsCollapsed, this.#BuildCheckList('settings', [
            {
                "id": "hasSavedGame",
                "name": "Game has saves avaialble",
                "gameCount": 0
            },
            {
                "id": "isFavourite",
                "name": "Favourite",
                "gameCount": 0
            }
        ], false), null));

        // platforms filter
        let platformsCollapsed = false;
        if (this.#filterCollapsed['Platforms'] !== undefined) {
            platformsCollapsed = this.#filterCollapsed['Platforms'];
        }
        panel.appendChild(this.#BuildBasicPanel('Platforms', true, platformsCollapsed, this.#BuildCheckList("Platforms", filter["platforms"], true), null));

        // genres filter
        let genresCollapsed = true;
        if (this.#filterCollapsed['Genres'] !== undefined) {
            genresCollapsed = this.#filterCollapsed['Genres'];
        }
        panel.appendChild(this.#BuildBasicPanel('Genres', true, genresCollapsed, this.#BuildCheckList("Genres", filter["genres"], true), null));

        // themes filter
        let themesCollapsed = true;
        if (this.#filterCollapsed['Themes'] !== undefined) {
            themesCollapsed = this.#filterCollapsed['Themes'];
        }
        panel.appendChild(this.#BuildBasicPanel('Themes', true, themesCollapsed, this.#BuildCheckList("Themes", filter["themes"], true), null));

        // release year filter
        let releaseYearCollapsed = true;
        if (this.#filterCollapsed['Release Year'] !== undefined) {
            releaseYearCollapsed = this.#filterCollapsed['Release Year'];
        }
        panel.appendChild(this.#BuildBasicPanel('Release Year', true, releaseYearCollapsed, this.#BuildRangePanel('releaseyear', 'Release Year', 1960, new Date().getFullYear()), null));

        // players filter
        let playersCollapsed = true;
        if (this.#filterCollapsed['Players'] !== undefined) {
            playersCollapsed = this.#filterCollapsed['Players'];
        }
        panel.appendChild(this.#BuildBasicPanel('Players', true, playersCollapsed, this.#BuildCheckList("Players", filter["players"], true), null));

        // player perspectives filter
        let perspectivesCollapsed = true;
        if (this.#filterCollapsed['Player Perspectives'] !== undefined) {
            perspectivesCollapsed = this.#filterCollapsed['Player Perspectives'];
        }
        panel.appendChild(this.#BuildBasicPanel('Player Perspectives', true, perspectivesCollapsed, this.#BuildCheckList("perspectives", filter["perspectives"], true), null));

        // user rating filter
        let userRatingCollapsed = true;
        if (this.#filterCollapsed['User Rating'] !== undefined) {
            userRatingCollapsed = this.#filterCollapsed['User Rating'];
        }
        panel.appendChild(this.#BuildBasicPanel('User Rating', true, userRatingCollapsed, this.#BuildRangePanel('userrating', 'User Rating', 0, 100), null));

        // user vote count
        let userVoteCountCollapsed = true;
        if (this.#filterCollapsed['User Votes'] !== undefined) {
            userVoteCountCollapsed = this.#filterCollapsed['User Votes'];
        }
        panel.appendChild(this.#BuildBasicPanel('User Votes', true, userVoteCountCollapsed, this.#BuildRangePanel('uservotecount', 'User Votes', 0, 1000000), null));

        targetElement.appendChild(panel);
        return targetElement;
    }

    #BuildBasicPanel(headingDisplayName, collapsible, defaultCollapsed, content, onChangeCallback) {
        let panel = document.createElement('div');
        panel.classList.add('section');

        let header = document.createElement('div');
        header.classList.add('section-header');
        header.style.cursor = 'pointer';

        let body = document.createElement('div');
        body.classList.add('section-body');

        if (collapsible === true) {
            let toggle = document.createElement('div');
            toggle.style.float = 'right';
            if (defaultCollapsed === true) {
                toggle.innerText = '+';
                body.style.display = 'none';
            } else {
                toggle.innerText = '-';
                body.style.display = 'block';
            }

            header.addEventListener('click', () => {
                if (toggle.innerText === '+') {
                    toggle.innerText = '-';
                    body.style.display = 'block';
                    this.#StoreFilterCollapsedStatus(headingDisplayName, false);
                } else {
                    toggle.innerText = '+';
                    body.style.display = 'none';
                    this.#StoreFilterCollapsedStatus(headingDisplayName, true);
                }
            });

            header.appendChild(toggle);
        }

        let heading = document.createElement('div');
        heading.innerText = headingDisplayName;
        header.appendChild(heading);

        panel.appendChild(header);

        body.appendChild(content);
        panel.appendChild(body);

        return panel;
    }

    #BuildStringPanel(fieldName, displayName) {
        let content = document.createElement('div');

        let input = document.createElement('input');
        input.type = 'text';
        input.id = fieldName;
        input.placeholder = displayName;
        input.name = fieldName;
        if (this.#filterSelections[fieldName] !== undefined) {
            input.value = this.#filterSelections[fieldName];
        }
        input.addEventListener('keyup', (event) => {
            // input.addEventListener('keypress', (event) => {
            // if (event.key === 'Enter') {
            this.#filterSelections[fieldName] = input.value;
            this.ApplyFilter();
            // }
        });

        content.appendChild(input);

        return content;
    }

    #BuildCheckList(fieldName, checkList, showTags) {
        let content = document.createElement('div');

        for (let i = 0; i < checkList.length; i++) {
            let checkItem = checkList[i];

            let id = checkItem.name;
            if (checkItem.id) {
                id = checkItem.id;
            }
            let name = checkItem.name;
            let gameCount = 0;
            if (checkItem.gameCount) {
                gameCount = checkItem.gameCount;
            }

            if (showTags === true && gameCount === 0) {
                continue;
            }

            let row = document.createElement('div');
            row.classList.add('filter_panel_item');

            if (showTags === true) {
                let tagBox = document.createElement('div');
                tagBox.classList.add('tagBox');

                let tag = document.createElement('div');
                tag.classList.add('tagBoxItem');
                tag.innerText = gameCount;
                tagBox.appendChild(tag);

                row.appendChild(tagBox);
            }

            let input = document.createElement('input');
            input.type = 'checkbox';
            input.classList.add('filter_panel_item_checkbox');
            input.id = fieldName + '_' + id;
            input.name = fieldName;
            // input.value = checkItem;
            if (this.#filterSelections[fieldName] !== undefined) {
                if (this.#filterSelections[fieldName][id] !== undefined) {
                    input.checked = this.#filterSelections[fieldName];
                }
            }
            input.addEventListener('change', () => {
                if (!this.#filterSelections[fieldName]) {
                    this.#filterSelections[fieldName] = {};
                }
                this.#filterSelections[fieldName][id] = input.checked;
                this.ApplyFilter();
            });

            let label = document.createElement('label');
            label.innerText = name;
            label.classList.add('filter_panel_item_label');
            label.setAttribute('for', input.id);

            row.appendChild(input);
            row.appendChild(label);

            content.appendChild(row);
        }

        return content;
    }

    #BuildRangePanel(fieldName, displayName, min, max) {
        let content = document.createElement('div');

        let selectCheckbox = document.createElement('input');
        selectCheckbox.type = 'checkbox';
        selectCheckbox.id = fieldName + '_select';
        selectCheckbox.name = fieldName + '_select';
        selectCheckbox.classList.add('filter_panel_item_checkbox');

        let minInput = document.createElement('input');
        minInput.type = 'number';
        minInput.id = fieldName + '_min';
        minInput.name = 'filter_panel_range_min';
        minInput.min = min;
        minInput.max = max;
        minInput.placeholder = min;
        if (this.#filterSelections[fieldName] !== undefined) {
            if (this.#filterSelections[fieldName].min !== undefined) {
                selectCheckbox.checked = true;
                minInput.value = this.#filterSelections[fieldName].min;
            }
        }

        let maxInput = document.createElement('input');
        maxInput.type = 'number';
        maxInput.id = fieldName + '_max';
        maxInput.name = 'filter_panel_range_max';
        maxInput.min = min;
        maxInput.max = max;
        maxInput.placeholder = max;
        if (this.#filterSelections[fieldName] !== undefined) {
            if (this.#filterSelections[fieldName].max !== undefined) {
                selectCheckbox.checked = true;
                maxInput.value = this.#filterSelections[fieldName].max;
            }
        }

        selectCheckbox.addEventListener('change', () => {
            if (selectCheckbox.checked === false) {
                minInput.value = '';
                maxInput.value = '';

                if (this.#filterSelections[fieldName]) {
                    delete this.#filterSelections[fieldName];
                }
            } else {
                if (!this.#filterSelections[fieldName]) {
                    this.#filterSelections[fieldName] = {
                        min: null,
                        max: null
                    };
                }

                if (!this.#filterSelections[fieldName].min) {
                    this.#filterSelections[fieldName].min = Number(minInput.value);
                }
                if (!this.#filterSelections[fieldName].max) {
                    this.#filterSelections[fieldName].max = Number(maxInput.value);
                }
            }

            this.ApplyFilter();
        });

        minInput.addEventListener('change', () => {
            if (!this.#filterSelections[fieldName]) {
                this.#filterSelections[fieldName] = {
                    min: null,
                    max: null
                };
            }

            this.#filterSelections[fieldName].min = Number(minInput.value);
            selectCheckbox.checked = true;

            this.ApplyFilter();
        });

        maxInput.addEventListener('change', () => {
            if (!this.#filterSelections[fieldName]) {
                this.#filterSelections[fieldName] = {
                    min: null,
                    max: null
                };
            }

            this.#filterSelections[fieldName].max = Number(maxInput.value);
            selectCheckbox.checked = true;

            this.ApplyFilter();
        });

        content.appendChild(selectCheckbox);
        content.appendChild(minInput);
        content.appendChild(maxInput);

        return content;
    }
}