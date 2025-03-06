class Filtering {
    constructor(applyCallback, orderBySelector, orderDirectionSelector, pageSizeSelector, executeCallback, executeErrorCallback, executeBeginCallback, executeCompleteCallback) {
        this.applyCallback = applyCallback;
        this.executeCallback = executeCallback;
        this.executeErrorCallback = executeErrorCallback;
        this.executeBeginCallback = executeBeginCallback;
        this.executeCompleteCallback = executeCompleteCallback;
        this.pageSizeSelector = pageSizeSelector;
        this.orderBySelector = orderBySelector;
        this.orderDirectionSelector = orderDirectionSelector;

        if (this.orderBySelector) {
            this.OrderBySelector(this.orderBySelector);
        }

        if (this.orderDirectionSelector) {
            this.OrderDirectionSelector(this.orderDirectionSelector);
        }

        if (this.pageSizeSelector) {
            this.PageSizeSelector(this.pageSizeSelector);
        }
    }

    OrderBySelector(selector) {
        this.orderBySelector = selector;
        this.orderBySelector.addEventListener('change', () => {
            this.filterSelections['orderBy'] = this.orderBySelector.value;
            this.ApplyFilter();
        });
    }

    SetOrderBy(value) {
        this.filterSelections['orderBy'] = value;
        this.ApplyFilter();
    }

    OrderDirectionSelector(selector) {
        this.orderDirectionSelector = selector;
        this.orderDirectionSelector.addEventListener('change', () => {
            this.filterSelections['orderDirection'] = this.orderDirectionSelector.value;
            this.ApplyFilter();
        });
    }

    SetOrderDirection(value) {
        this.filterSelections['orderDirection'] = value;
        this.ApplyFilter();
    }

    PageSizeSelector(selector) {
        this.pageSizeSelector = selector;

        this.pageSizeSelector.innerHTML = '';
        for (let i = 1; i <= 10; i++) {
            let option = document.createElement('option');
            option.value = i * 10;
            option.innerText = i * 10;
            if (i === 2) {
                option.selected = true
            }
            this.pageSizeSelector.appendChild(option);
        }

        this.pageSizeSelector.addEventListener('change', () => {
            this.filterSelections['pageSize'] = this.pageSizeSelector.value;
            this.ApplyFilter();
        });
    }

    SetPageSize(value) {
        this.filterSelections['pageSize'] = value;
        this.ApplyFilter();
    }

    filterSelections = {

    }

    #computedFilterModel = {

    }

    filterCollapsed = {

    }

    async LoadFilterSettings() {
        this.#LoadFilterSettings();
        this.#LoadFilterCollapsedStatus();
    }

    async ApplyFilter(filterOverride) {
        let filter = this.filterSelections;
        if (filterOverride) {
            filter = filterOverride;
        }

        console.log('Filter start time: ' + new Date().toLocaleTimeString());

        // delete entries from filterSelections that are false or empty
        for (let key in filter) {
            if (typeof filter[key] === 'object') {
                for (let subKey in filter[key]) {
                    if (filter[key][subKey] === false) {
                        delete filter[key][subKey];
                    }
                }
            } else {
                if (filter[key] === false || filter[key] === '') {
                    delete filter[key];
                }
            }
        }

        // delete keys from filterSelections that are empty
        for (let key in filter) {
            if (typeof filter[key] === 'object') {
                if (Object.keys(filter[key]).length === 0) {
                    delete filter[key];
                }
            }
        }

        if (!filterOverride) {
            // store the filter selections in local storage
            SetPreference('Library.Filter', filter);
        }

        // build the filter model
        let filterModel = {
            "name": "",
            "platform": [
            ],
            "genre": [
            ],
            "gameMode": [
            ],
            "playerPerspective": [
            ],
            "theme": [
            ],
            "minimumReleaseYear": -1,
            "maximumReleaseYear": -1,
            "gameRating": {
                "minimumRating": -1,
                "minimumRatingCount": -1,
                "maximumRating": -1,
                "maximumRatingCount": -1,
                "includeUnrated": true
            },
            "gameAgeRating": {
                "ageGroupings": [
                ],
                "includeUnrated": false
            },
            "sorting": {
                "sortBy": "Name",
                "sortAscending": true
            },
            "HasSavedGame": false,
            "IsFavourite": false
        }

        if (filter['search']) {
            filterModel.name = filter['search'];
        }
        if (filter['Platforms']) {
            for (let key in filter['Platforms']) {
                filterModel.platform.push(key);
            }
        }
        if (filter['Genres']) {
            for (let key in filter['Genres']) {
                filterModel.genre.push(key);
            }
        }
        if (filter['Players']) {
            for (let key in filter['Players']) {
                filterModel.gameMode.push(key);
            }
        }
        if (filter['perspectives']) {
            for (let key in filter['perspectives']) {
                filterModel.playerPerspective.push(key);
            }
        }
        if (filter['Themes']) {
            for (let key in filter['Themes']) {
                filterModel.theme.push(key);
            }
        }
        if (filter['releaseyear']) {
            if (filter['releaseyear'].min) {
                filterModel.minimumReleaseYear = filter['releaseyear'].min;
            }
            if (filter['releaseyear'].max) {
                filterModel.maximumReleaseYear = filter['releaseyear'].max;
            }
        }
        if (filter['playTime']) {
            if (filter['playTime'].min) {
                filterModel.minPlayTime = filter['playTime'].min;
            }
            if (filter['playTime'].max) {
                filterModel.maxPlayTime = filter['playTime'].max;
            }
        }
        if (filter['userrating']) {
            if (filter['userrating'].min) {
                filterModel.gameRating.minimumRating = filter['userrating'].min;
                filterModel.gameRating.includeUnrated = false;
            }
            if (filter['userrating'].max) {
                filterModel.gameRating.maximumRating = filter['userrating'].max;
                filterModel.gameRating.includeUnrated = false;
            }
        }
        if (filter['uservotecount']) {
            if (filter['uservotecount'].min) {
                filterModel.gameRating.minimumRatingCount = filter['uservotecount'].min;
                filterModel.gameRating.includeUnrated = false;
            }
            if (filter['uservotecount'].max) {
                filterModel.gameRating.maximumRatingCount = filter['uservotecount'].max;
                filterModel.gameRating.includeUnrated = false;
            }
        }
        if (filter['ageGroups']) {
            if (Object.keys(filter['ageGroups']).length > 0) {
                filterModel.gameAgeRating.includeUnrated = false;
                for (let key in filter['ageGroups']) {
                    if (key === 'Unclassified') {
                        filterModel.gameAgeRating.includeUnrated = filter['ageGroups'][key];
                    } else {
                        filterModel.gameAgeRating.ageGroupings.push(key);
                    }
                }
            } else {
                filterModel.gameAgeRating.includeUnrated = true;
            }
        }
        if (filter['orderBy']) {
            filterModel.sorting.sortBy = filter['orderBy'];
        }
        if (filter['orderDirection']) {
            if (filter['orderDirection'] === 'Ascending') {
                filterModel.sorting.sortAscending = true;
            } else {
                filterModel.sorting.sortAscending = false;
            }
        }
        let pageSize = 20;
        if (filter['pageSize']) {
            pageSize = filter['pageSize'];
        }
        if (filter["settings"]) {
            if (filter["settings"]['hasSavedGame']) {
                filterModel.HasSavedGame = filter["settings"]['hasSavedGame'];
            }
            if (filter["settings"]['isFavourite']) {
                filterModel.IsFavourite = filter["settings"]['isFavourite'];
            }
        }

        // store the filter model in memory
        this.#computedFilterModel = filterModel;

        // request the games from the server
        let pageNumber = 1;

        if (filter['limit']) {
            pageNumber = 1;
            pageSize = filter['limit'];
        }

        this.GameCount = 0;
        this.AlphaList = [];

        if (this.applyCallback) {
            this.applyCallback();
        }

        this.ExecuteFilter(pageNumber, pageSize);
    }

    GameCount = 0;
    AlphaList = [];

    async ExecuteFilter(pageNumber, pageSize) {
        if (!pageNumber) {
            pageNumber = 1;
        }
        if (!pageSize) {
            pageSize = this.filterSelections['pageSize'];
        }

        let returnSummary = "false";
        if (this.AlphaList.length === 0) {
            returnSummary = "true";
        }

        if (this.executeBeginCallback) {
            this.executeBeginCallback();
        }

        await fetch('/api/v1.1/Games?pageNumber=' + pageNumber + '&pageSize=' + pageSize + '&returnSummary=' + returnSummary + '&returnGames=true', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(this.#computedFilterModel)
        }).then(response => {
            if (response.ok) {
                return response.json();
            }
            throw new Error('Failed to load games');
        }).then(data => {
            if (data.count) {
                this.GameCount = data.count;
            }
            if (data.alphaList) {
                this.AlphaList = data.alphaList;
            }

            if (this.executeCompleteCallback) {
                this.executeCompleteCallback();
            }

            if (this.executeCallback) {
                this.executeCallback(data.games);
            }
        }).catch(error => {
            console.error(error);

            if (this.executeCompleteCallback) {
                this.executeCompleteCallback();
            }

            if (this.executeErrorCallback) {
                this.executeErrorCallback(error);
            }
        });
    }

    async GetGamesFilter() {
        await fetch('/api/v1.1/Filter', {
            method: 'GET'
        }).then(response => {
            if (response.ok) {
                return response.json();
            }
            throw new Error('Failed to load filter content');
        }).then(data => {
            if (this.FilterCallbacks.length > 0) {
                for (const callback of this.FilterCallbacks) {
                    callback(data);
                }
            }
        }).catch(error => {
            console.error(error);
        });
    }

    FilterCallbacks = [];

    async #LoadFilterSettings() {
        let data = GetPreference('Library.Filter');

        if (data) {
            this.filterSelections = data;
        }
    }

    async #LoadFilterCollapsedStatus() {
        let data = GetPreference('Library.FilterCollapsed');

        if (data) {
            this.filterCollapsed = data;
        }
    }

    async #StoreFilterCollapsedStatus(section, collapsed) {
        this.filterCollapsed[section] = collapsed;

        SetPreference('Library.FilterCollapsed', this.filterCollapsed);
    }

    BuildFilterTable(filter) {
        let targetElement = document.createElement('div');
        targetElement.id = 'games_filter';

        let panel = document.createElement('div');
        panel.id = 'games_filter_box';

        // free text search
        let searchCollapsed = true;
        if (this.filterCollapsed['Title Search'] !== undefined) {
            searchCollapsed = this.filterCollapsed['Title Search'];
        }
        panel.appendChild(this.#BuildBasicPanel('Title Search', true, searchCollapsed, this.#BuildStringPanel('search', 'Title Search'), null));

        // settings filter
        let settingsCollapsed = true;
        if (this.filterCollapsed['Settings'] !== undefined) {
            settingsCollapsed = this.filterCollapsed['Settings'];
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
        if (this.filterCollapsed['Platforms'] !== undefined) {
            platformsCollapsed = this.filterCollapsed['Platforms'];
        }
        panel.appendChild(this.#BuildBasicPanel('Platforms', true, platformsCollapsed, this.#BuildCheckList("Platforms", filter["platforms"], true), null));

        // genres filter
        let genresCollapsed = true;
        if (this.filterCollapsed['Genres'] !== undefined) {
            genresCollapsed = this.filterCollapsed['Genres'];
        }
        panel.appendChild(this.#BuildBasicPanel('Genres', true, genresCollapsed, this.#BuildCheckList("Genres", filter["genres"], true), null));

        // themes filter
        let themesCollapsed = true;
        if (this.filterCollapsed['Themes'] !== undefined) {
            themesCollapsed = this.filterCollapsed['Themes'];
        }
        panel.appendChild(this.#BuildBasicPanel('Themes', true, themesCollapsed, this.#BuildCheckList("Themes", filter["themes"], true), null));

        // release year filter
        let releaseYearCollapsed = true;
        if (this.filterCollapsed['Release Year'] !== undefined) {
            releaseYearCollapsed = this.filterCollapsed['Release Year'];
        }
        panel.appendChild(this.#BuildBasicPanel('Release Year', true, releaseYearCollapsed, this.#BuildRangePanel('releaseyear', 'Release Year', 1960, new Date().getFullYear()), null));

        // players filter
        let playersCollapsed = true;
        if (this.filterCollapsed['Players'] !== undefined) {
            playersCollapsed = this.filterCollapsed['Players'];
        }
        panel.appendChild(this.#BuildBasicPanel('Players', true, playersCollapsed, this.#BuildCheckList("Players", filter["gamemodes"], true), null));

        // player perspectives filter
        let perspectivesCollapsed = true;
        if (this.filterCollapsed['Player Perspectives'] !== undefined) {
            perspectivesCollapsed = this.filterCollapsed['Player Perspectives'];
        }
        panel.appendChild(this.#BuildBasicPanel('Player Perspectives', true, perspectivesCollapsed, this.#BuildCheckList("perspectives", filter["playerperspectives"], true), null));

        // age groups filter
        let ageGroupsCollapsed = true;
        if (this.filterCollapsed['Age Groups'] !== undefined) {
            ageGroupsCollapsed = this.filterCollapsed['Age Groups'];
        }
        panel.appendChild(this.#BuildBasicPanel('Age Groups', true, ageGroupsCollapsed, this.#BuildCheckList("ageGroups", filter["agegroupings"], true), null));

        // user rating filter
        let userRatingCollapsed = true;
        if (this.filterCollapsed['User Rating'] !== undefined) {
            userRatingCollapsed = this.filterCollapsed['User Rating'];
        }
        panel.appendChild(this.#BuildBasicPanel('User Rating', true, userRatingCollapsed, this.#BuildRangePanel('userrating', 'User Rating', 0, 100), null));

        // user vote count
        let userVoteCountCollapsed = true;
        if (this.filterCollapsed['User Votes'] !== undefined) {
            userVoteCountCollapsed = this.filterCollapsed['User Votes'];
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
        if (this.filterSelections[fieldName] !== undefined) {
            input.value = this.filterSelections[fieldName];
        }
        // input.addEventListener('keyup', (event) => {
        input.addEventListener('keypress', (event) => {
            if (event.key === 'Enter') {
                this.filterSelections[fieldName] = input.value;
                this.ApplyFilter();
            }
        });

        content.appendChild(input);

        return content;
    }

    #BuildCheckList(fieldName, checkList, showTags) {
        let content = document.createElement('div');

        for (let checkItem of checkList) {

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

            if (this.filterSelections[fieldName] !== undefined) {
                if (this.filterSelections[fieldName][id] !== undefined) {
                    input.checked = this.filterSelections[fieldName];
                }
            }
            input.addEventListener('change', () => {
                if (!this.filterSelections[fieldName]) {
                    this.filterSelections[fieldName] = {};
                }
                this.filterSelections[fieldName][id] = input.checked;
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
        if (this.filterSelections[fieldName] !== undefined) {
            if (this.filterSelections[fieldName].min !== undefined) {
                selectCheckbox.checked = true;
                minInput.value = this.filterSelections[fieldName].min;
            }
        }

        let maxInput = document.createElement('input');
        maxInput.type = 'number';
        maxInput.id = fieldName + '_max';
        maxInput.name = 'filter_panel_range_max';
        maxInput.min = min;
        maxInput.max = max;
        maxInput.placeholder = max;
        if (this.filterSelections[fieldName] !== undefined) {
            if (this.filterSelections[fieldName].max !== undefined) {
                selectCheckbox.checked = true;
                maxInput.value = this.filterSelections[fieldName].max;
            }
        }

        selectCheckbox.addEventListener('change', () => {
            if (selectCheckbox.checked === false) {
                minInput.value = '';
                maxInput.value = '';

                if (this.filterSelections[fieldName]) {
                    delete this.filterSelections[fieldName];
                }
            } else {
                if (!this.filterSelections[fieldName]) {
                    this.filterSelections[fieldName] = {
                        min: null,
                        max: null
                    };
                }

                if (!this.filterSelections[fieldName].min) {
                    this.filterSelections[fieldName].min = Number(minInput.value);
                }
                if (!this.filterSelections[fieldName].max) {
                    this.filterSelections[fieldName].max = Number(maxInput.value);
                }
            }

            this.ApplyFilter();
        });

        minInput.addEventListener('change', () => {
            if (!this.filterSelections[fieldName]) {
                this.filterSelections[fieldName] = {
                    min: null,
                    max: null
                };
            }

            this.filterSelections[fieldName].min = Number(minInput.value);
            selectCheckbox.checked = true;

            this.ApplyFilter();
        });

        maxInput.addEventListener('change', () => {
            if (!this.filterSelections[fieldName]) {
                this.filterSelections[fieldName] = {
                    min: null,
                    max: null
                };
            }

            this.filterSelections[fieldName].max = Number(maxInput.value);
            selectCheckbox.checked = true;

            this.ApplyFilter();
        });

        content.appendChild(selectCheckbox);
        content.appendChild(minInput);
        content.appendChild(maxInput);

        return content;
    }
}