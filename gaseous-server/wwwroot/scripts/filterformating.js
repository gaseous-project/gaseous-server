function formatFilterPanel(targetElement, result) {
    var panel = document.createElement('div');
    panel.id = 'filter_panel_box';

    panel.appendChild(buildFilterPanelHeader('filter', 'Filter'));

    var containerPanelSearch = document.createElement('div');
    containerPanelSearch.className = 'filter_panel_box';
    var containerPanelSearchField = document.createElement('input');
    containerPanelSearchField.type = 'text';
    containerPanelSearch.appendChild(containerPanelSearchField);

    panel.appendChild(containerPanelSearch);

    if (result.platforms) {
        panel.appendChild(buildFilterPanelHeader('platforms', 'Platforms'));

        var containerPanelPlatform = document.createElement('div');
        containerPanelPlatform.className = 'filter_panel_box';
        for (var i = 0; i < result.platforms.length; i++) {
            containerPanelPlatform.appendChild(buildFilterPanelItem(result.platforms[i].id, result.platforms[i].name));
        }
        panel.appendChild(containerPanelPlatform);

        targetElement.appendChild(panel);
    }

    if (result.genres) {
        panel.appendChild(buildFilterPanelHeader('genres', 'Genres'));

        var containerPanelGenres = document.createElement('div');
        containerPanelGenres.className = 'filter_panel_box';
        for (var i = 0; i < result.genres.length; i++) {
            containerPanelGenres.appendChild(buildFilterPanelItem(result.genres[i].id, result.genres[i].name));
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

function buildFilterPanelItem(itemString, friendlyItemString) {
    var filterPanelItem = document.createElement('div');
    filterPanelItem.id = 'filter_panel_item_' + itemString;
    filterPanelItem.className = 'filter_panel_item';

    var filterPanelItemCheckBox = document.createElement('div');

    var filterPanelItemCheckBoxItem = document.createElement('input');
    filterPanelItemCheckBoxItem.id = 'filter_panel_item_checkbox_' + itemString;
    filterPanelItemCheckBoxItem.type = 'checkbox';
    filterPanelItemCheckBoxItem.className = 'filter_panel_item_checkbox';
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