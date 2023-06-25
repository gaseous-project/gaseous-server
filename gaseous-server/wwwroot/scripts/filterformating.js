function formatFilterPanel(targetElement, result) {
    var panel = document.createElement('div');
    panel.id = 'filter_panel_box';

    panel.appendChild(buildFilterPanelHeader('filter', 'Filter'));

    var containerPanelSearch = document.createElement('div');
    containerPanelSearch.className = 'filter_panel_box';
    var containerPanelSearchField = document.createElement('input');
    containerPanelSearchField.id = 'filter_panel_search';
    containerPanelSearchField.type = 'text';
    containerPanelSearchField.placeholder = 'Search';
    containerPanelSearchField.setAttribute('onkeydown', 'executeFilterDelayed();');
    containerPanelSearch.appendChild(containerPanelSearchField);

    panel.appendChild(containerPanelSearch);

    if (result.platforms) {
        panel.appendChild(buildFilterPanelHeader('platforms', 'Platforms'));

        var containerPanelPlatform = document.createElement('div');
        containerPanelPlatform.className = 'filter_panel_box';
        for (var i = 0; i < result.platforms.length; i++) {
            containerPanelPlatform.appendChild(buildFilterPanelItem('platforms', result.platforms[i].id, result.platforms[i].name));
        }
        panel.appendChild(containerPanelPlatform);

        targetElement.appendChild(panel);
    }

    if (result.genres) {
        panel.appendChild(buildFilterPanelHeader('genres', 'Genres'));

        var containerPanelGenres = document.createElement('div');
        containerPanelGenres.className = 'filter_panel_box';
        for (var i = 0; i < result.genres.length; i++) {
            containerPanelGenres.appendChild(buildFilterPanelItem('genres', result.genres[i].id, result.genres[i].name));
        }
        panel.appendChild(containerPanelGenres);

        targetElement.appendChild(panel);
    }

    
}

function buildFilterPanelHeader(headerString, friendlyHeaderString) {
    var header = document.createElement('div');
    header.id = 'filter_panel_header_' + headerString;
    header.className = 'filter_header';
    header.innerHTML = friendlyHeaderString;

    return header;
}

function buildFilterPanelItem(filterType, itemString, friendlyItemString) {
    var filterPanelItem = document.createElement('div');
    filterPanelItem.id = 'filter_panel_item_' + itemString;
    filterPanelItem.className = 'filter_panel_item';

    var filterPanelItemCheckBox = document.createElement('div');

    var filterPanelItemCheckBoxItem = document.createElement('input');
    filterPanelItemCheckBoxItem.id = 'filter_panel_item_checkbox_' + itemString;
    filterPanelItemCheckBoxItem.type = 'checkbox';
    filterPanelItemCheckBoxItem.className = 'filter_panel_item_checkbox';
    filterPanelItemCheckBoxItem.name = 'filter_' + filterType;
    filterPanelItemCheckBoxItem.setAttribute('filter_id', itemString);
    filterPanelItemCheckBoxItem.setAttribute('oninput' , 'executeFilter();');
    filterPanelItemCheckBox.appendChild(filterPanelItemCheckBoxItem);

    var filterPanelItemLabel = document.createElement('label');
    filterPanelItemLabel.id = 'filter_panel_item_label_' + itemString;
    filterPanelItemLabel.className = 'filter_panel_item_label';
    filterPanelItemLabel.setAttribute('for', filterPanelItemCheckBoxItem.id);
    filterPanelItemLabel.innerHTML = friendlyItemString;

    filterPanelItem.appendChild(filterPanelItemCheckBox);
    filterPanelItem.appendChild(filterPanelItemLabel);

    return filterPanelItem;
}

var filterExecutor = null;
function executeFilterDelayed() {
    if (filterExecutor) {
        filterExecutor = null;
    }

    filterExecutor = setTimeout(executeFilter, 1000);
}

function executeFilter() {
    // build filter lists
    var platforms = '';
    var genres = '';

    var searchString = document.getElementById('filter_panel_search').value;
    var platformFilters = document.getElementsByName('filter_platforms');
    var genreFilters = document.getElementsByName('filter_genres');

    for (var i = 0; i < platformFilters.length; i++) {
        if (platformFilters[i].checked) {
            if (platforms.length > 0) {
                platforms += ',';
            }
            platforms += platformFilters[i].getAttribute('filter_id');
        }
    }

    for (var i = 0; i < genreFilters.length; i++) {
        if (genreFilters[i].checked) {
            if (genres.length > 0) {
                genres += ',';
            }
            genres += genreFilters[i].getAttribute('filter_id');
        }
    }

    ajaxCall('/api/v1/Games?name=' + searchString + '&platform=' + platforms + '&genre=' + genres, 'GET', function (result) {
        var gameElement = document.getElementById('games_library');
        formatGamesPanel(gameElement, result);
    });
}