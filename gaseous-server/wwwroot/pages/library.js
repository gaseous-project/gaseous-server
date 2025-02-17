async function SetupPage() {
    // setup view controls
    document.getElementById('games_filter_button_column').addEventListener('click', function () {
        FilterDisplayToggle();
    });

    FilterDisplayToggle(GetPreference("LibraryShowFilter", true), false);

    let showTitle = GetPreference("LibraryShowGameTitle", true);
    let showRatings = GetPreference("LibraryShowGameRating", true);
    let showClassification = GetPreference("LibraryShowGameClassification", true);
    let classificationDisplayOrder = GetRatingsBoards();
    if (showTitle == "true") { showTitle = true; } else { showTitle = false; }
    if (showRatings == "true") { showRatings = true; } else { showRatings = false; }
    if (showClassification == "true") { showClassification = true; } else { showClassification = false; }

    // setup filter panel
    let scrollerElement = document.getElementById('games_filter_scroller');
    if (db) {
        db.FilterCallbacks.push(async function (result) {
            await filter.LoadFilterSettings();

            scrollerElement.innerHTML = '';
            scrollerElement.appendChild(filter.BuildFilterTable(result));

            // setup filter panel events
            filter.applyCallback = async function (games) {
                // render games
                let gameCountElement = document.getElementById('games_library_recordcount');
                if (games.length == 1) {
                    gameCountElement.innerText = '1 game';
                } else {
                    gameCountElement.innerText = games.length + ' games';
                }

                // clear game tiles not in the dom element
                let gameTiles = document.getElementsByClassName('game_tile');
                for (let x = 0; x < 2; x++) {
                    for (let i = 0; i < gameTiles.length; i++) {
                        if (games.find(x => x.metadataMapId == gameTiles[i].getAttribute('data-id')) == null) {
                            gameTiles[i].remove();
                            i = 0;
                        }
                    }
                }


                // render new games
                let gamesElement = document.getElementById('games_library');

                coverURLList = [];
                for (const game of games) {
                    // if the game tile already exists, skip it
                    let existingGameTile = document.getElementById('game_tile_' + game.metadataMapId);
                    if (existingGameTile) {
                        existingGameTile.setAttribute('data-index', game.resultIndex);
                        continue;
                    }

                    // insert the game tile in the same order as the games array
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

                backgroundImageHandler = new BackgroundImageRotator(coverURLList, null, true);

                // get all elemens in the node gamesElement and sort by the data-index attribute
                gameTiles = Array.from(gamesElement.children);
                gameTiles.sort((a, b) => {
                    return a.getAttribute('data-index') - b.getAttribute('data-index');
                });

                // remove all children from the gamesElement
                gamesElement.innerHTML = '';

                // add the sorted children back to the gamesElement, and update the alpha pager
                let alphaPager = document.getElementById('games_library_alpha_pager');
                alphaPager.innerHTML = '';
                let alphaAdded = [];
                for (const gameTile of gameTiles) {
                    gamesElement.appendChild(gameTile);

                    if (gameTile.getAttribute('data-alpha') != null) {
                        let alpha = gameTile.getAttribute('data-alpha');
                        if (alphaAdded.includes(alpha)) {
                            continue;
                        }
                        let alphaButton = document.createElement('span');
                        alphaButton.classList.add('games_library_alpha_pager_letter');
                        alphaButton.innerText = alpha;
                        alphaButton.addEventListener('click', function () {
                            // scroll to the first game with the alpha
                            let gameTiles = Array.from(document.getElementsByClassName('game_tile'));
                            let gameTile = gameTiles.find(x => x.getAttribute('data-alpha') == alpha);
                            if (gameTile) {
                                // gameTile.scrollIntoView();
                                // scroll to the game tile with the alpha - 100px
                                window.scrollTo(0, gameTile.offsetTop - 100);
                            }
                        });
                        alphaPager.appendChild(alphaButton);
                        alphaAdded.push(alpha);
                    }
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

        await db.GetGamesFilter();
    }
}

function FilterDisplayToggle(display, storePreference = true) {
    let filterPanel = document.getElementById('games_filter_panel');
    let libraryControls = document.getElementById('games_library_controls');
    let gamesHome = document.getElementById('games_home');

    if (filterPanel.style.display == 'none' || display === "true") {
        filterPanel.style.display = 'block';
        libraryControls.classList.remove('games_library_controls_expanded');
        gamesHome.classList.remove('games_home_expanded');
        if (storePreference === true) { SetPreference("LibraryShowFilter", true); }
    } else {
        filterPanel.style.display = 'none';
        libraryControls.classList.add('games_library_controls_expanded');
        gamesHome.classList.add('games_home_expanded');
        if (storePreference === true) { SetPreference("LibraryShowFilter", false); }
    }
}

let coverURLList = [];

SetupPage();