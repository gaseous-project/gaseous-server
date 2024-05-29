var existingSearchModel;

function formatFilterPanel(containerElement, result) {
    containerElement.innerHTML = '';

    var targetElement = document.createElement('div');
    targetElement.id = 'games_filter';

    var panel = document.createElement('div');
    panel.id = 'filter_panel_box';

    panel.appendChild(buildFilterPanelHeader('filter', 'Filter'));

    // free text search
    var containerPanelSearch = document.createElement('div');
    containerPanelSearch.className = 'filter_panel_box';
    var containerPanelSearchField = document.createElement('input');
    var searchCookie = getCookie('filter_panel_search');
    if (searchCookie) {
        containerPanelSearchField.value = searchCookie;
    }
    containerPanelSearchField.id = 'filter_panel_search';
    containerPanelSearchField.type = 'text';
    containerPanelSearchField.placeholder = 'Search';
    containerPanelSearchField.addEventListener("keypress", function (event) {
        if (event.key === "Enter") {
            // Cancel the default action, if needed
            event.preventDefault();
            // Trigger the button element with a click
            applyFilters();
        }
    });
    containerPanelSearch.appendChild(containerPanelSearchField);

    panel.appendChild(containerPanelSearch);

    // user rating
    panel.appendChild(buildFilterPanelHeader('userrating', 'User Rating', true, false));
    var containerPanelUserRating = buildFilterRange('userrating', 0, 100);
    panel.appendChild(containerPanelUserRating);

    // user vote count
    panel.appendChild(buildFilterPanelHeader('uservotes', 'User Votes', true, false));
    var containerPanelUserVotes = buildFilterRange('uservotes', 0, 1000000);
    panel.appendChild(containerPanelUserVotes);

    // release year
    panel.appendChild(buildFilterPanelHeader('releaseyear', 'Release Year', true, false));
    var containerPanelReleaseYear = buildFilterRange('releaseyear', 1960, (new Date()).getFullYear());
    panel.appendChild(containerPanelReleaseYear);

    // settings
    buildFilterPanel(panel, 'settings', 'Settings', [
        {
            "id": "savestatesavailable",
            "name": "Game has save states avaialble",
            "gameCount": 0
        },
        {
            "id": "favourite",
            "name": "Favourite",
            "gameCount": 0
        }
    ], true, true);

    // server provided filters
    if (result.platforms) {
        buildFilterPanel(panel, 'platform', 'Platforms', result.platforms, true, true);
    }

    if (result.genres) {
        buildFilterPanel(panel, 'genre', 'Genres', result.genres, true, false);
    }

    if (result.gamemodes) {
        buildFilterPanel(panel, 'gamemode', 'Players', result.gamemodes, true, false);
    }

    if (result.playerperspectives) {
        buildFilterPanel(panel, 'playerperspective', 'Player Perspectives', result.playerperspectives, true, false);
    }

    if (result.themes) {
        buildFilterPanel(panel, 'theme', 'Themes', result.themes, true, false);
    }

    if (result.agegroupings) {
        if (result.agegroupings.length > 1) {
            buildFilterPanel(panel, 'agegroupings', 'Age Groups', result.agegroupings, true, false);
        }
    }

    targetElement.appendChild(panel);

    // filter controls
    var buttonsDiv = document.createElement('div');
    buttonsDiv.id = 'games_library_searchbuttons'

    // add filter button
    var searchButton = document.createElement('div');
    searchButton.id = 'games_library_searchbutton';
    searchButton.setAttribute('onclick', 'applyFilters();');
    searchButton.innerHTML = 'Apply';

    buttonsDiv.appendChild(searchButton);

    // add reset button
    var resetButton = document.createElement('div');
    resetButton.id = 'games_library_resetbutton';
    resetButton.setAttribute('onclick', 'resetFilters();');
    resetButton.innerHTML = 'Reset';

    buttonsDiv.appendChild(resetButton);

    // set page size value
    var pageSizeCookie = GetPreference('LibraryPageSize', '20');
    if (pageSizeCookie) {
        var pageSizeSelector = document.getElementById('games_library_pagesize_select');
        $(pageSizeSelector).select2('destroy');
        $(pageSizeSelector).val(pageSizeCookie).select2();
    }

    // set order by values
    var orderByCookie = GetPreference('LibraryOrderBy', 'NameThe');
    if (orderByCookie) {
        var orderBySelector = document.getElementById('games_library_orderby_select');
        $(orderBySelector).select2('destroy');
        $(orderBySelector).val(orderByCookie).select2();
    }
    var orderByDirectionCookie = GetPreference('LibraryOrderByDirection', 'Ascending');
    if (orderByDirectionCookie) {
        var orderByDirectionSelector = document.getElementById('games_library_orderby_direction_select');
        $(orderByDirectionSelector).select2('destroy');
        $(orderByDirectionSelector).val(orderByDirectionCookie).select2();
    }

    containerElement.appendChild(targetElement);

    containerElement.appendChild(buttonsDiv);

    console.log('Filter generated - execute filter');
    var pageNumber = undefined;
    if (getCookie('games_library_last_page') == "") {
        pageNumber = undefined;
    } else {
        pageNumber = Number(getCookie('games_library_last_page'));
    }

    executeFilter1_1(pageNumber);
}

function buildFilterPanel(targetElement, headerString, friendlyHeaderString, valueList, showToggle, initialDisplay) {
    if (showToggle == false) { initialDisplay = true; }
    var displayCookie = getCookie('filter_panel_box_' + headerString);
    if (displayCookie) {
        initialDisplay = (displayCookie === 'true');
    }
    targetElement.appendChild(buildFilterPanelHeader(headerString, friendlyHeaderString, showToggle, initialDisplay));

    var containerPanel = document.createElement('div');
    containerPanel.className = 'filter_panel_box';
    containerPanel.id = 'filter_panel_box_' + headerString;
    if (initialDisplay == false) {
        containerPanel.setAttribute('style', 'display: none;');
    }
    for (var i = 0; i < valueList.length; i++) {
        var tags;

        if (valueList[i].gameCount) {
            tags = [
                {
                    'label': valueList[i].gameCount
                }
            ];
        }
        containerPanel.appendChild(buildFilterPanelItem(headerString, valueList[i].id, valueList[i].name, tags));
    }
    targetElement.appendChild(containerPanel);
}

function buildFilterPanelHeader(headerString, friendlyHeaderString, showVisibleToggle, toggleInitialValue) {
    var headerToggle = document.createElement('div');
    headerToggle.setAttribute('style', 'float: right;');
    headerToggle.id = 'filter_panel_header_toggle_' + headerString;
    if (toggleInitialValue == true) {
        headerToggle.innerHTML = '-';
    } else {
        headerToggle.innerHTML = '+';
    }

    var headerLabel = document.createElement('span');
    headerLabel.innerHTML = friendlyHeaderString;

    var header = document.createElement('div');
    header.id = 'filter_panel_header_' + headerString;
    header.className = 'filter_header';

    if (showVisibleToggle == true) {
        header.appendChild(headerToggle);
        header.setAttribute('onclick', 'toggleFilterPanel("' + headerString + '");');
        header.style.cursor = 'pointer';
        header.classList.add('filter_header_toggleable');
    }

    header.appendChild(headerLabel);

    return header;
}

function toggleFilterPanel(panelName) {
    var filterPanel = document.getElementById('filter_panel_box_' + panelName);
    var filterPanelToggle = document.getElementById('filter_panel_header_toggle_' + panelName);

    var cookieVal = '';
    if (filterPanel.style.display == 'none') {
        filterPanelToggle.innerHTML = '-';
        filterPanel.style.display = '';
        cookieVal = "true";
    } else {
        filterPanelToggle.innerHTML = '+';
        filterPanel.style.display = 'none';
        cookieVal = "false";
    }

    setCookie("filter_panel_box_" + panelName, cookieVal);
}

function buildFilterPanelItem(filterType, itemString, friendlyItemString, tags) {
    var checkCookie = getCookie('filter_panel_item_' + filterType + '_checkbox_' + itemString);
    var checkState = false;
    if (checkCookie) {
        checkState = (checkCookie === 'true');
    }

    var filterPanelItem = document.createElement('div');
    filterPanelItem.id = 'filter_panel_item_' + itemString;
    filterPanelItem.className = 'filter_panel_item';

    var filterPanelItemCheckBox = document.createElement('div');

    var filterPanelItemCheckBoxItem = document.createElement('input');
    filterPanelItemCheckBoxItem.id = 'filter_panel_item_' + filterType + '_checkbox_' + itemString;
    filterPanelItemCheckBoxItem.type = 'checkbox';
    filterPanelItemCheckBoxItem.className = 'filter_panel_item_checkbox';
    filterPanelItemCheckBoxItem.name = 'filter_' + filterType;
    filterPanelItemCheckBoxItem.setAttribute('filter_id', itemString);
    if (checkState == true) {
        filterPanelItemCheckBoxItem.checked = true;
    }
    filterPanelItemCheckBox.appendChild(filterPanelItemCheckBoxItem);

    var filterPanelItemLabel = document.createElement('label');
    filterPanelItemLabel.id = 'filter_panel_item_label_' + itemString;
    filterPanelItemLabel.className = 'filter_panel_item_label';
    filterPanelItemLabel.setAttribute('for', filterPanelItemCheckBoxItem.id);
    filterPanelItemLabel.innerHTML = friendlyItemString;

    if (tags) {
        filterPanelItem.appendChild(buildFilterTag(tags));
    }
    filterPanelItem.appendChild(filterPanelItemCheckBox);
    filterPanelItem.appendChild(filterPanelItemLabel);

    return filterPanelItem;
}

function buildFilterRange(name, min, max) {
    var containerPanelUserRating = document.createElement('div');
    containerPanelUserRating.id = 'filter_panel_box_' + name + '';
    containerPanelUserRating.className = 'filter_panel_box';

    var containerPanelUserRatingCheckBox = document.createElement('input');
    containerPanelUserRatingCheckBox.id = 'filter_panel_' + name + '_enabled';
    containerPanelUserRatingCheckBox.name = 'filter_panel_range_enabled_check';
    containerPanelUserRatingCheckBox.setAttribute('data-name', name);
    containerPanelUserRatingCheckBox.type = 'checkbox';
    containerPanelUserRatingCheckBox.className = 'filter_panel_item_checkbox';
    containerPanelUserRatingCheckBox.setAttribute('onclick', 'filter_panel_range_enabled_check("' + name + '");');
    var ratingEnabledCookie = getCookie('filter_panel_' + name + '_enabled');
    if (ratingEnabledCookie) {
        if (ratingEnabledCookie == "true") {
            containerPanelUserRatingCheckBox.checked = true;
        } else {
            containerPanelUserRatingCheckBox.checked = false;
        }
    }
    containerPanelUserRating.appendChild(containerPanelUserRatingCheckBox);

    var containerPanelUserRatingMinField = document.createElement('input');
    var minRatingCookie = getCookie('filter_panel_' + name + '_min');
    if (minRatingCookie) {
        containerPanelUserRatingMinField.value = minRatingCookie;
    }
    containerPanelUserRatingMinField.id = 'filter_panel_' + name + '_min';
    containerPanelUserRatingMinField.name = 'filter_panel_range_min';
    containerPanelUserRatingMinField.type = 'number';
    containerPanelUserRatingMinField.placeholder = min;
    containerPanelUserRatingMinField.setAttribute('min', min);
    containerPanelUserRatingMinField.setAttribute('max', max);
    containerPanelUserRatingMinField.setAttribute('oninput', 'filter_panel_range_value("' + name + '");');
    containerPanelUserRating.appendChild(containerPanelUserRatingMinField);

    var containerPanelUserRatingMaxField = document.createElement('input');
    var maxRatingCookie = getCookie('filter_panel_' + name + '_max');
    if (maxRatingCookie) {
        containerPanelUserRatingMaxField.value = maxRatingCookie;
    }
    containerPanelUserRatingMaxField.id = 'filter_panel_' + name + '_max';
    containerPanelUserRatingMaxField.name = 'filter_panel_range_max';
    containerPanelUserRatingMaxField.type = 'number';
    containerPanelUserRatingMaxField.placeholder = max;
    containerPanelUserRatingMaxField.setAttribute('min', min);
    containerPanelUserRatingMaxField.setAttribute('max', max);
    containerPanelUserRatingMaxField.setAttribute('oninput', 'filter_panel_range_value("' + name + '");');
    containerPanelUserRating.appendChild(containerPanelUserRatingMaxField);

    return containerPanelUserRating;
}

var filterExecutor = null;
function executeFilterDelayed() {
    if (filterExecutor) {
        filterExecutor = null;
    }

    filterExecutor = setTimeout(executeFilter1_1, 1000);
}

function buildFilterTag(tags) {
    // accepts an array of numbers + classes for styling (optional)
    // example [ { label: "G: 13", class: "tag_Green" }, { label: "R: 17", class: "tag_Orange" } ]

    var boundingDiv = document.createElement('div');
    boundingDiv.className = 'tagBox';

    for (var i = 0; i < tags.length; i++) {
        var tagBox = document.createElement('div');
        tagBox.classList.add('tagBoxItem');
        if (tags[i].class) {
            tagBox.classList.add(tags[i].class);
        }
        tagBox.innerHTML = tags[i].label;

        boundingDiv.appendChild(tagBox);
    }

    return boundingDiv;
}

function filter_panel_range_enabled_check(name) {
    var ratingCheck = document.getElementById('filter_panel_' + name + '_enabled');
    var minRatingValue = document.getElementById('filter_panel_' + name + '_min');
    var maxRatingValue = document.getElementById('filter_panel_' + name + '_max');

    if (ratingCheck.checked == false) {
        minRatingValue.value = '';
        maxRatingValue.value = '';
    } else {
        minRatingValue.value = minRatingValue.min;
        maxRatingValue.value = maxRatingValue.max;
    }
}

function filter_panel_range_value(name) {
    var ratingCheck = document.getElementById('filter_panel_' + name + '_enabled');
    var minRatingValue = document.getElementById('filter_panel_' + name + '_min');
    var maxRatingValue = document.getElementById('filter_panel_' + name + '_max');

    if (minRatingValue.value || maxRatingValue.value) {
        ratingCheck.checked = true;
    } else {
        ratingCheck.checked = false;
    }
}

function applyFilters() {
    document.getElementById('games_library').innerHTML = '';

    executeFilter1_1();
}

function resetFilters() {
    // clear name
    document.getElementById('filter_panel_search').value = '';

    // clear filter check boxes
    var filterChecks = document.getElementsByClassName('filter_panel_item_checkbox');
    for (var i = 0; i < filterChecks.length; i++) {
        filterChecks[i].checked = false;
    }

    // fire checkbox specific scripts
    var rangeCheckboxes = document.getElementsByName('filter_panel_range_enabled_check');
    for (var i = 0; i < rangeCheckboxes.length; i++) {
        filter_panel_range_enabled_check(rangeCheckboxes[i].getAttribute('data-name'));
    }

    document.getElementById('games_library').innerHTML = '';
    executeFilter1_1();
}

function executeFilter1_1(pageNumber) {
    var freshSearch = false;

    if (!pageNumber) {
        pageNumber = 1;
        freshSearch = true;
        existingSearchModel = undefined;
    }

    // get settings
    let pageSize = Number($('#games_library_pagesize_select').val());
    let orderBy = $('#games_library_orderby_select').val();
    let orderByDirectionSelect = $('#games_library_orderby_direction_select').val();

    var model;

    // get order by
    var orderByDirection = true;
    if (orderByDirectionSelect == "Ascending") {
        orderByDirection = true;
    } else {
        orderByDirection = false;
    }

    if (existingSearchModel == undefined || freshSearch == true) {
        // search name
        setCookie('filter_panel_search', document.getElementById('filter_panel_search').value);

        // user ratings
        var userRatingEnabled = document.getElementById('filter_panel_userrating_enabled');

        var minUserRating = -1;
        var minUserRatingInput = document.getElementById('filter_panel_userrating_min');
        if (minUserRatingInput.value) {
            minUserRating = minUserRatingInput.value;
            userRatingEnabled.checked = true;
        }
        setCookie(minUserRatingInput.id, minUserRatingInput.value);

        var maxUserRating = -1;
        var maxUserRatingInput = document.getElementById('filter_panel_userrating_max');
        if (maxUserRatingInput.value) {
            maxUserRating = maxUserRatingInput.value;
            userRatingEnabled.checked = true;
        }
        setCookie(maxUserRatingInput.id, maxUserRatingInput.value);

        if (minUserRating == -1 && maxUserRating == -1) {
            userRatingEnabled.checked = false;
        }

        if (userRatingEnabled.checked == false) {
            setCookie("filter_panel_userrating_enabled", false);

            minUserRating = -1;
            minUserRatingInput.value = "";
            setCookie(minUserRatingInput.id, minUserRatingInput.value);
            maxUserRating = -1;
            maxUserRatingInput.value = "";
            setCookie(maxUserRatingInput.id, maxUserRatingInput.value);
        } else {
            setCookie("filter_panel_userrating_enabled", true);
        }

        // user votes
        var userVotesEnabled = document.getElementById('filter_panel_uservotes_enabled');

        var minUserVotes = -1;
        var minUserVotesInput = document.getElementById('filter_panel_uservotes_min');
        if (minUserVotesInput.value) {
            minUserVotes = minUserVotesInput.value;
            userVotesEnabled.checked = true;
        }
        setCookie(minUserVotesInput.id, minUserVotesInput.value);

        var maxUserVotes = -1;
        var maxUserVotesInput = document.getElementById('filter_panel_uservotes_max');
        if (maxUserVotesInput.value) {
            maxUserVotes = maxUserVotesInput.value;
            userVotesEnabled.checked = true;
        }
        setCookie(maxUserVotesInput.id, maxUserVotesInput.value);

        if (minUserVotes == -1 && maxUserVotes == -1) {
            userVotesEnabled.checked = false;
        }

        if (userVotesEnabled.checked == false) {
            setCookie("filter_panel_uservotes_enabled", false);

            minUserVotes = -1;
            minUserVotesInput.value = "";
            setCookie(minUserVotesInput.id, minUserVotesInput.value);
            maxUserVotes = -1;
            maxUserVotesInput.value = "";
            setCookie(maxUserVotesInput.id, maxUserVotesInput.value);
        } else {
            setCookie("filter_panel_uservotes_enabled", true);
        }

        // release year
        var releaseYearEnabled = document.getElementById('filter_panel_releaseyear_enabled');

        var minReleaseYear = -1;
        var minReleaseYearInput = document.getElementById('filter_panel_releaseyear_min');
        if (minReleaseYearInput.value) {
            minReleaseYear = minReleaseYearInput.value;
            releaseYearEnabled.checked = true;
        }
        setCookie(minReleaseYearInput.id, minReleaseYearInput.value);

        var maxReleaseYear = -1;
        var maxReleaseYearInput = document.getElementById('filter_panel_releaseyear_max');
        if (maxReleaseYearInput.value) {
            maxReleaseYear = maxReleaseYearInput.value;
            releaseYearEnabled.checked = true;
        }
        setCookie(maxReleaseYearInput.id, maxReleaseYearInput.value);

        if (minReleaseYear == -1 && maxReleaseYear == -1) {
            releaseYearEnabled.checked = false;
        }

        if (releaseYearEnabled.checked == false) {
            setCookie("filter_panel_releaseyear_enabled", false);

            minReleaseYear = -1;
            minReleaseYearInput.value = "";
            setCookie(minReleaseYearInput.id, minReleaseYearInput.value);
            maxReleaseYear = -1;
            maxReleaseYearInput.value = "";
            setCookie(maxReleaseYearInput.id, maxReleaseYearInput.value);
        } else {
            setCookie("filter_panel_releaseyear_enabled", true);
        }

        // save cookies for settings
        GetFilterQuery1_1('settings');

        // build filter model
        var ratingAgeGroups = GetFilterQuery1_1('agegroupings');
        var ratingIncludeUnrated = false;
        if (ratingAgeGroups.includes("0")) {
            ratingIncludeUnrated = true;
        }

        model = {
            "Name": document.getElementById('filter_panel_search').value,
            "HasSavedGame": document.getElementById('filter_panel_item_settings_checkbox_savestatesavailable').checked,
            "isFavourite": document.getElementById('filter_panel_item_settings_checkbox_favourite').checked,
            "Platform": GetFilterQuery1_1('platform'),
            "Genre": GetFilterQuery1_1('genre'),
            "GameMode": GetFilterQuery1_1('gamemode'),
            "PlayerPerspective": GetFilterQuery1_1('playerperspective'),
            "Theme": GetFilterQuery1_1('theme'),
            "MinimumReleaseYear": minReleaseYear,
            "MaximumReleaseYear": maxReleaseYear,
            "GameRating": {
                "MinimumRating": minUserRating,
                "MinimumRatingCount": minUserVotes,
                "MaximumRating": maxUserRating,
                "MaximumRatingCount": maxUserVotes,
                "IncludeUnrated": !userRatingEnabled
            },
            "GameAgeRating": {
                "AgeGroupings": ratingAgeGroups,
                "IncludeUnrated": ratingIncludeUnrated
            },
            "Sorting": {
                "SortBy": orderBy,
                "SortAscending": orderByDirection
            }
        };
        console.log(model);

        existingSearchModel = model;
    } else {
        existingSearchModel.Sorting.SortBy = orderBy;
        existingSearchModel.Sorting.SortAscending = orderByDirection;
        model = existingSearchModel;
    }

    let gamesCallURL = '/api/v1.1/Games?pageNumber=' + pageNumber + '&pageSize=' + pageSize;
    console.log(gamesCallURL);
    ajaxCall(
        gamesCallURL,
        'POST',
        function (result) {
            console.log(result);
            var gameElement = document.getElementById('games_library');
            setCookie('games_library_last_page', pageNumber);
            formatGamesPanel(gameElement, result, pageNumber, pageSize, true);
        },
        function (error) {
            console.log('An error occurred: ' + JSON.stringify(error));
        },
        JSON.stringify(model)
    );
}

function GetFilterQuery1_1(filterName) {
    var Filters = document.getElementsByName('filter_' + filterName);
    var selections = [];

    for (var i = 0; i < Filters.length; i++) {
        if (Filters[i].checked) {
            setCookie(Filters[i].id, true);
            selections.push(Filters[i].getAttribute('filter_id'));
        } else {
            setCookie(Filters[i].id, false);
        }
    }

    return selections;
}