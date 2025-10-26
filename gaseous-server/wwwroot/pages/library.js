var loadedPages = [];

async function SetupPage() {
    // setup view controls
    document.getElementById('games_filter_button_column').addEventListener('click', function () {
        FilterDisplayToggle();
    });
    document.getElementById('games_filter_button_column_filter').addEventListener('click', function (e) {
        FilterDisplayToggle();
    });

    let displayFilter = GetPreference("Library.ShowFilter");
    FilterDisplayToggle(displayFilter, true);

    // setup filter panel
    let scrollerElement = document.getElementById('games_filter_scroller');
    if (filter) {
        filter.FilterCallbacks.push(async (result) => {
            filter.LoadFilterSettings();

            scrollerElement.innerHTML = '';
            scrollerElement.appendChild(filter.BuildFilterTable(result));

            let gamesElement = document.getElementById('games_library');
            gamesElement.innerHTML = '';

            let freshLoad = true;

            filter.applyCallback = async () => {
                freshLoad = true;
                gamesElement.innerHTML = '';

                coverURLList = [];

                loadedPages = [];

                window.scrollTo(0, 0);
            }

            filter.executeCallback = async (games) => {
                let showTitle = GetPreference("Library.ShowGameTitle");
                let showRatings = GetPreference("Library.ShowGameRating");
                let showClassification = GetPreference("Library.ShowGameClassification");
                let classificationDisplayOrder = GetRatingsBoards();

                if (freshLoad === true) {
                    // render game chrome objects
                    let gameCountElement = document.getElementById('games_library_recordcount');
                    if (filter.GameCount == 1) {
                        gameCountElement.innerText = '1 game';
                    } else {
                        gameCountElement.innerText = filter.GameCount + ' games';
                    }

                    // build alphabet pager
                    let alphaPager = document.getElementById('games_library_alpha_pager');
                    alphaPager.innerHTML = '';
                    for (const [key, value] of Object.entries(filter.AlphaList)) {
                        let alphaSpan = document.createElement('span');
                        alphaSpan.innerText = key;
                        alphaSpan.classList.add('games_library_alpha_pager_letter');
                        alphaSpan.setAttribute('data-letter', key);
                        alphaPager.appendChild(alphaSpan);

                        alphaSpan.addEventListener('click', () => {
                            // get the tile width from the class game_tile
                            let tileSize = CalculateTileSize();
                            // get the width of the game tile
                            let gameTileWidth = tileSize.width;
                            // get the height of the game tile
                            let gameTileHeight = tileSize.height;

                            // calculate the vertical position of the game tile with the index defined in value.index
                            let tilesPerRow = Math.floor(gamesElement.clientWidth / gameTileWidth);
                            // which row is the game tile in
                            let gameTileRow = Math.floor(value.index / tilesPerRow);
                            // scroll to the game tile
                            let gameTileY = gameTileRow * gameTileHeight;
                            // scroll to the game tile
                            let gameTileTop = gameTileY - (window.innerHeight / 2) + (gameTileHeight / 2);
                            // scroll to the game tile
                            window.scrollTo({
                                top: gameTileTop,
                                behavior: 'smooth'
                            });
                        });
                    }
                }

                // render game tiles
                for (const game of games) {
                    // make sure tile with index doesn't exist
                    let existingTile = document.querySelector('div[data-index="' + game.index + '"]');
                    if (existingTile) {
                        continue;
                    }

                    let tileContainer = document.createElement('div');
                    tileContainer.classList.add('game_tile_wrapper_icon');

                    tileContainer.setAttribute('data-index', game.index);

                    let gameObj = new GameIcon(game);
                    let gameTile = await gameObj.Render(showTitle, showRatings, showClassification, classificationDisplayOrder);
                    tileContainer.appendChild(gameTile);

                    if (game.cover) {
                        let coverUrl = '/api/v1.1/Games/' + game.metadataMapId + '/' + game.metadataSource + '/cover/' + game.cover + '/image/original/' + game.cover + '.jpg';
                        if (!coverURLList.includes(coverUrl)) {
                            coverURLList.push(coverUrl);
                        }
                    }

                    gamesElement.appendChild(tileContainer);
                }

                // sort all game tiles by their data-index attribute
                let gameTiles = document.querySelectorAll('.game_tile_wrapper_icon');
                let gameTilesArray = Array.from(gameTiles);
                gameTilesArray.sort((a, b) => {
                    let aIndex = parseInt(a.getAttribute('data-index'));
                    let bIndex = parseInt(b.getAttribute('data-index'));
                    return aIndex - bIndex;
                });
                for (const gameTile of gameTilesArray) {
                    gamesElement.appendChild(gameTile);
                }

                // resize the game library element
                await ResizeLibraryPanel();

                if (freshLoad === true) {
                    backgroundImageHandler = new BackgroundImageRotator(coverURLList, null, true, false);
                    freshLoad = false;

                    // restore the scroll position
                    let scrollPosition = localStorage.getItem('Library.ScrollPosition');
                    if (scrollPosition) {
                        console.log('restoring scroll position: ' + scrollPosition);
                        window.scrollTo(0, scrollPosition);
                    }
                }
            };

            filter.executeBeginCallback = async () => {
                if (freshLoad === true) {
                    let loadingElement = document.createElement('div');
                    loadingElement.id = 'games_library_loading';
                    loadingElement.classList.add('loadingElement');
                    let charCount = 0;
                    setInterval(() => {
                        charCount++;
                        if (charCount > 3) {
                            charCount = 0;
                        }
                        loadingElement.innerHTML = 'Loading' + '.'.repeat(charCount) + '&nbsp;'.repeat(3 - charCount);
                    }, 1000);
                    gamesElement.appendChild(loadingElement);
                }
            }

            filter.executeCompleteCallback = async () => {
                let loadingElement = document.getElementById('games_library_loading');
                if (loadingElement) {
                    loadingElement.remove();
                }
            }

            filter.OrderBySelector(document.getElementById('games_library_orderby_select'));
            filter.OrderDirectionSelector(document.getElementById('games_library_orderby_direction_select'));
            filter.PageSizeSelector(document.getElementById('games_library_pagesize_select'));

            let pageSizeSelect = $('#games_library_pagesize_select');
            pageSizeSelect.select2();
            if (filter.filterSelections['pageSize']) {
                pageSizeSelect.val(filter.filterSelections['pageSize']).trigger('change');
            }
            pageSizeSelect.on('change', function (e) {
                filter.SetPageSize(pageSizeSelect.val());
            });

            let orderBySelect = $('#games_library_orderby_select');
            orderBySelect.select2();
            if (filter.filterSelections['orderBy']) {
                orderBySelect.val(filter.filterSelections['orderBy']).trigger('change');
            }
            orderBySelect.on('change', function (e) {
                filter.SetOrderBy(orderBySelect.val());
            });

            let orderDirectionSelect = $('#games_library_orderby_direction_select');
            orderDirectionSelect.select2();
            if (filter.filterSelections['orderDirection']) {
                orderDirectionSelect.val(filter.filterSelections['orderDirection']).trigger('change');
            }
            orderDirectionSelect.on('change', function (e) {
                filter.SetOrderDirection(orderDirectionSelect.val());
            });

            await filter.ApplyFilter();
        });

        await filter.GetGamesFilter();
    }

    // setup scroll position
    window.addEventListener('scroll', async (pos) => {
        // clear the scroll timer
        if (scrollTimer) {
            clearTimeout(scrollTimer);
        }

        // set the scroll timer to perform the scroll action after 1 second
        scrollTimer = setTimeout(async () => {
            // save the scroll position to localStorage
            localStorage.setItem('Library.ScrollPosition', window.scrollY);

            let gameLibraryElement = document.getElementById('games_library');

            // get the scroll position
            let scrollTop = window.scrollY;

            // get the window height
            let windowHeight = window.innerHeight;

            // get the number of pages
            let pageSize = parseInt(filter.filterSelections['pageSize']);
            let pageCount = Math.ceil(filter.GameCount / pageSize);

            // get the game_tile class
            let tileSize = CalculateTileSize();
            // get the width of the game tile
            let gameTileWidth = tileSize.width;
            // get the height of the game tile
            let gameTileHeight = tileSize.height;

            // each element is 232px wide and 283px high, and there are pageSize elements per page - get the height of the page based on the width of gameLibraryElement
            let tilesPerRow = Math.floor(gameLibraryElement.clientWidth / gameTileWidth);
            // how many rows per page - incomplete rows are not counted
            let rowsPerPage = Math.floor(pageSize / tilesPerRow);
            // calculate the height of the page based on the number of rows per page
            // and the height of the game tile
            // the height of the game tile is 283px, and there is a margin of 0px between tiles
            // the height of the game library element is the number of rows per page * the height of the game tile
            // plus the margin between tiles
            let pageHeight = Math.floor(rowsPerPage * gameTileHeight) + (rowsPerPage - 1);

            // make a list of all the pages and top and bottom coordinates of each page
            let pages = [];
            for (let i = 0; i < pageCount; i++) {
                let pageTop = i * pageHeight;
                let pageBottom = (i + 1) * pageHeight;
                pages.push({ page: i + 1, top: pageTop, bottom: pageBottom });
            }

            // find the pages that are visible in the window
            let visiblePages = [];
            for (const page of pages) {
                if (page.top < scrollTop + windowHeight && page.bottom > scrollTop) {
                    visiblePages.push(page.page);
                }
            }

            // load the visible pages
            for (const page of visiblePages) {
                if (!loadedPages.includes(page)) {
                    loadedPages.push(page);
                    await filter.ExecuteFilter(page, pageSize);

                    // anticipate the next and previous pages
                    if (page + 1 <= pageCount && !loadedPages.includes(page + 1)) {
                        loadedPages.push(page + 1);
                        await filter.ExecuteFilter(page + 1, pageSize);
                    }
                    if (page - 1 > 0 && !loadedPages.includes(page - 1)) {
                        loadedPages.push(page - 1);
                        await filter.ExecuteFilter(page - 1, pageSize);
                    }
                }
            }
        }, 250);
    });
}

async function ResizeLibraryPanel() {
    // resize the game library element to contain the number of tiles in filter.GameCount - tiles are 232px wide and 283px high
    // the game library element should not be wider than the window width minus the alphabet pager width
    let gameLibraryElement = document.getElementById('games_library');
    let filterPanel = document.getElementById('games_filter_panel');
    let alphaPager = document.getElementById('games_library_alpha_pager');
    let gameLibraryWidth = Math.floor(window.innerWidth - alphaPager.clientWidth);
    if (filterPanel.style.display == 'block') {
        gameLibraryWidth = Math.floor(window.innerWidth - alphaPager.clientWidth - filterPanel.clientWidth);
    }
    // get the number of pages
    let pageSize = parseInt(filter.filterSelections['pageSize']);
    let pageCount = Math.ceil(filter.GameCount / pageSize);

    // get the game_tile class
    let tileSize = CalculateTileSize();
    if (tileSize !== null) {
        // get the width of the game tile
        let gameTileWidth = tileSize.width;
        // get the height of the game tile
        let gameTileHeight = tileSize.height;

        // each element is 232px wide and 283px high, and there are pageSize elements per page - get the height of the page based on the width of gameLibraryElement
        let tilesPerRow = Math.floor(gameLibraryElement.clientWidth / gameTileWidth);
        // how many rows per page - incomplete rows are not counted
        let rowsPerPage = Math.floor(pageSize / tilesPerRow);
        // calculate the height of the page based on the number of rows per page
        // and the height of the game tile
        // the height of the game tile is 283px, and there is a margin of 0px between tiles
        // the height of the game library element is the number of rows per page * the height of the game tile
        // plus the margin between tiles
        let gameLibraryHeight = Math.floor((rowsPerPage * pageCount) * gameTileHeight);
        gameLibraryElement.setAttribute('style', 'width: ' + gameLibraryWidth + 'px; height: ' + gameLibraryHeight + 'px; position: relative;');

        // rearrange the game tiles to fit the new width
        let gameTiles = document.querySelectorAll('.game_tile_wrapper_icon');
        let gameTileCount = Math.floor(gameLibraryWidth / gameTileWidth);
        for (const gameTile of gameTiles) {
            // get the game tile index
            // get the game tile index based on the number of tiles per row
            // the game tile index is the index of the tile in the list of tiles
            let gameTileIndex = gameTile.getAttribute('data-index');
            // determine the row and column of the game tile based on the index
            let gameTileRow = Math.floor(gameTileIndex / gameTileCount);
            let gameTileColumn = gameTileIndex % gameTileCount;
            // calculate the x and y position of the game tile based on the row and column
            // the x position is the column * the width of the game tile + the margin between tiles
            let gameTileX = gameTileColumn * gameTileWidth;
            // the y position is the row * the height of the game tile + the margin between tiles
            let gameTileY = gameTileRow * gameTileHeight;
            // set the position of the game tile
            gameTile.setAttribute('style', 'left: ' + gameTileX + 'px; top: ' + gameTileY + 'px; position: absolute;');
            gameTileIndex++;
        }
    }
}

// execute ResizeLibraryPanel() on window resize
window.addEventListener('resize', async () => {
    await ResizeLibraryPanel();
});

async function FilterDisplayToggle(display, storePreference = true) {
    let filterPanel = document.getElementById('games_filter_panel');
    let libraryControls = document.getElementById('games_library_controls');
    let gamesHome = document.getElementById('games_home');

    if (filterPanel.style.display == 'none' || display === true) {
        filterPanel.style.display = 'block';
        libraryControls.classList.remove('games_library_controls_expanded');
        gamesHome.classList.remove('games_home_expanded');
        libraryControls.classList.add('games_library_controls_collapsed');
        gamesHome.classList.add('games_home_collapsed');
        if (storePreference === true) { SetPreference("Library.ShowFilter", true); }
    } else {
        filterPanel.style.display = 'none';
        libraryControls.classList.add('games_library_controls_expanded');
        gamesHome.classList.add('games_home_expanded');
        libraryControls.classList.remove('games_library_controls_collapsed');
        gamesHome.classList.remove('games_home_collapsed');
        if (storePreference === true) { SetPreference("Library.ShowFilter", false); }
    }

    await ResizeLibraryPanel();
}

function CalculateTileSize() {
    // get the game_tile class
    let cssClass = document.querySelector('.game_tile');
    if (cssClass !== null) {
        let cssClassStyle = getComputedStyle(cssClass);
        if (cssClassStyle !== null) {
            // get the width of the game tile
            let gameTileWidth = Number(cssClassStyle.marginLeft.replace('px', '')) + Number(cssClassStyle.width.replace('px', '')) + Number(cssClassStyle.marginRight.replace('px', ''));
            // get the height of the game tile
            let gameTileHeight = Number(cssClassStyle.marginTop.replace('px', '')) + Number(cssClassStyle.height.replace('px', '')) + Number(cssClassStyle.marginBottom.replace('px', ''));
            // add the height of game_tile_label_box to the height of the game tile
            let gameTileLabelBox = document.querySelector('.game_tile_label_box');
            if (gameTileLabelBox !== null) {
                let gameTileLabelBoxStyle = getComputedStyle(gameTileLabelBox);
                if (gameTileLabelBoxStyle !== null) {
                    gameTileHeight += Number(gameTileLabelBoxStyle.height.replace('px', ''));
                }
            }
            return { width: gameTileWidth, height: gameTileHeight };
        }
    }
    return null;
}

var filter = new Filtering();
filter.GetSummary = true;

var coverURLList = [];

var lastScrollTop = localStorage.getItem('Library.ScrollPosition') || 0;
var scrollTimer = null;

// setup preferences callbacks
prefsDialog.OkCallbacks.push(async () => {
    await filter.ApplyFilter();
});

// Register cleanup callback for library page
if (typeof registerPageUnloadCallback === 'function') {
    registerPageUnloadCallback('library', async () => {
        console.log('Cleaning up library page...');

        // Clear scroll timer
        if (typeof scrollTimer !== 'undefined' && scrollTimer) {
            clearTimeout(scrollTimer);
            scrollTimer = null;
        }

        // Save current scroll position
        localStorage.setItem('Library.ScrollPosition', window.scrollY);

        // Clean up filter
        if (typeof filter !== 'undefined' && filter) {
            // Clear any ongoing filter operations
            if (filter.executeCallback) {
                filter.executeCallback = null;
            }
            if (filter.applyCallback) {
                filter.applyCallback = null;
            }
            if (filter.executeBeginCallback) {
                filter.executeBeginCallback = null;
            }
            if (filter.executeCompleteCallback) {
                filter.executeCompleteCallback = null;
            }
        }

        // Clear URL list
        if (typeof coverURLList !== 'undefined') {
            coverURLList = [];
        }

        // Reset loaded pages array
        if (typeof loadedPages !== 'undefined') {
            loadedPages = [];
        }

        // Clean up background image handler
        if (typeof backgroundImageHandler !== 'undefined' && backgroundImageHandler) {
            if (backgroundImageHandler.RotationTimer) {
                clearInterval(backgroundImageHandler.RotationTimer);
            }
            backgroundImageHandler = undefined;
        }

        // Remove scroll event listener - this will be handled by general cleanup

        console.log('Library page cleanup completed');
    });
}

SetupPage();