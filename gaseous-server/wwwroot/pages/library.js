async function SetupPage() {
    // setup view controls
    document.getElementById('games_filter_button_column').addEventListener('click', function () {
        FilterDisplayToggle();
    });

    let displayFilter = GetPreference("Library.ShowFilter", true);
    FilterDisplayToggle(displayFilter, true);

    let showTitle = GetPreference("Library.ShowGameTitle", true);
    let showRatings = GetPreference("Library.ShowGameRating", true);
    let showClassification = GetPreference("Library.ShowGameClassification", true);
    let classificationDisplayOrder = GetRatingsBoards();

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
            }

            filter.executeCallback = async (games) => {
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

                        alphaSpan.addEventListener('click', function () {
                            document.querySelector('div[data-index="' + value.index + '"]').scrollIntoView({ block: "start", behavior: 'smooth' });
                        });
                    }

                    // add placeholder game tiles
                    let maxPages = Math.ceil(filter.GameCount / filter.filterSelections["pageSize"]);
                    // generate page spans
                    for (let i = 1; i <= maxPages; i++) {
                        let pageSpan = document.createElement('span');
                        pageSpan.classList.add('pageAnchor');
                        pageSpan.setAttribute('data-page', i);
                        pageSpan.setAttribute('data-loaded', '0');
                        gamesElement.appendChild(pageSpan);
                    }
                    // generate placeholder game tiles
                    let pageNumber = 1;
                    let tilesPerPage = 0;
                    for (let i = 0; i < filter.GameCount; i++) {
                        tilesPerPage++;
                        if (tilesPerPage > filter.filterSelections["pageSize"]) {
                            pageNumber++;
                            tilesPerPage = 1;
                        }
                        let targetElement = document.querySelector('span[data-page="' + pageNumber + '"]');
                        if (targetElement) {
                            let gameTile = document.createElement('div');
                            gameTile.classList.add('game_tile_placeholder');
                            gameTile.setAttribute('name', 'GamePlaceholder');
                            gameTile.setAttribute('data-index', i);
                            gameTile.setAttribute('data-page', pageNumber);
                            targetElement.appendChild(gameTile);
                        }
                    }
                }

                // render game tiles
                for (const game of games) {
                    let tileContainer = document.querySelector('div[data-index="' + game.index + '"]');

                    if (tileContainer) {
                        // tileContainer.classList.remove('game_tile_placeholder');
                        tileContainer.classList.add('game_tile_wrapper_icon');

                        // set data-loaded=1 on the pageAnchor span to prevent re-rendering
                        let pageAnchor = document.querySelector('span[data-page="' + tileContainer.getAttribute('data-page') + '"]');
                        if (pageAnchor) {
                            pageAnchor.setAttribute('data-loaded', '1');
                        }

                        let gameObj = new GameIcon(game);
                        let gameTile = await gameObj.Render(showTitle, showRatings, showClassification, classificationDisplayOrder);
                        tileContainer.appendChild(gameTile);

                        if (game.cover) {
                            let coverUrl = '/api/v1.1/Games/' + game.metadataMapId + '/cover/' + game.cover + '/image/original/' + game.cover + '.jpg?sourceType=' + game.metadataSource;
                            if (!coverURLList.includes(coverUrl)) {
                                coverURLList.push(coverUrl);
                            }
                        }
                    }
                }

                if (freshLoad === true) {
                    backgroundImageHandler = new BackgroundImageRotator(coverURLList, null, true, false);
                    freshLoad = false;
                }

                // restore the scroll position
                let scrollPosition = localStorage.getItem('Library.ScrollPosition');
                if (scrollPosition) {
                    console.log('restoring scroll position: ' + scrollPosition);
                    window.scrollTo(0, scrollPosition);
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

            filter.ApplyFilter();
        });

        await filter.GetGamesFilter();
    }

    // setup scroll position
    window.addEventListener('scroll', (pos) => {
        // save the scroll position to localStorage
        localStorage.setItem('Library.ScrollPosition', window.scrollY);

        let anchors = document.getElementsByClassName('pageAnchor');
        for (const anchor of anchors) {
            if (elementIsVisibleInViewport(anchor, true)) {
                if (anchor.getAttribute('data-loaded') === "0") {
                    anchor.setAttribute('data-loaded', "1");
                    let pageToLoad = Number(anchor.getAttribute('data-page'));
                    console.log('Loading page: ' + pageToLoad);
                    filter.ExecuteFilter(pageToLoad);
                }
            }
        }
    });
}

const elementIsVisibleInViewport = (el, partiallyVisible = false) => {
    const { top, left, bottom, right } = el.getBoundingClientRect();
    const { innerHeight, innerWidth } = window;
    return partiallyVisible
        ? ((top > 0 && top < innerHeight) ||
            (bottom > 0 && bottom < innerHeight)) &&
        ((left > 0 && left < innerWidth) || (right > 0 && right < innerWidth))
        : top >= 0 && left >= 0 && bottom <= innerHeight && right <= innerWidth;
};

function FilterDisplayToggle(display, storePreference = true) {
    let filterPanel = document.getElementById('games_filter_panel');
    let libraryControls = document.getElementById('games_library_controls');
    let gamesHome = document.getElementById('games_home');

    if (filterPanel.style.display == 'none' || display === true) {
        filterPanel.style.display = 'block';
        libraryControls.classList.remove('games_library_controls_expanded');
        gamesHome.classList.remove('games_home_expanded');
        if (storePreference === true) { SetPreference("Library.ShowFilter", true); }
    } else {
        filterPanel.style.display = 'none';
        libraryControls.classList.add('games_library_controls_expanded');
        gamesHome.classList.add('games_home_expanded');
        if (storePreference === true) { SetPreference("Library.ShowFilter", false); }
    }
}

let filter = new Filtering();

let coverURLList = [];

SetupPage();