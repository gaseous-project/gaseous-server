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

            // setup filter panel events
            filter.applyCallback = async (games) => {
                // render games
                let gameCountElement = document.getElementById('games_library_recordcount');
                if (games.length == 1) {
                    gameCountElement.innerText = '1 game';
                } else {
                    gameCountElement.innerText = games.length + ' games';
                }

                // render new games
                let gamesElement = document.getElementById('games_library');
                gamesElement.innerHTML = '';

                coverURLList = [];
                for (const game of games) {
                    let gameObj = new GameIcon(game);
                    let gameTile = await gameObj.Render(showTitle, showRatings, showClassification, classificationDisplayOrder);
                    gamesElement.appendChild(gameTile);

                    if (game.cover) {
                        let coverUrl = '/api/v1.1/Games/' + game.metadataMapId + '/cover/' + game.cover + '/image/original/' + game.cover + '.jpg?sourceType=' + game.metadataSource;
                        if (!coverURLList.includes(coverUrl)) {
                            coverURLList.push(coverUrl);
                        }
                    }
                }

                // backgroundImageHandler = new BackgroundImageRotator(coverURLList, null, true);

                // restore the scroll position
                let scrollPosition = localStorage.getItem('Library.ScrollPosition');
                if (scrollPosition) {
                    console.log('restoring scroll position: ' + scrollPosition);
                    window.scrollTo(0, scrollPosition);
                }
            };

            filter.OrderBySelector(document.getElementById('games_library_orderby_select'));
            filter.OrderDirectionSelector(document.getElementById('games_library_orderby_direction_select'));

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
    });
}

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