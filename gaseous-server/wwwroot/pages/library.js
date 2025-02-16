async function SetupPage() {
    // setup view controls
    document.getElementById('games_filter_button_column').addEventListener('click', function () {
        let filterPanel = document.getElementById('games_filter_panel');
        let libraryControls = document.getElementById('games_library_controls');
        let gamesHome = document.getElementById('games_home');

        if (filterPanel.style.display == 'none') {
            filterPanel.style.display = 'block';
            libraryControls.classList.remove('games_library_controls_expanded');
            gamesHome.classList.remove('games_home_expanded');
        } else {
            filterPanel.style.display = 'none';
            libraryControls.classList.add('games_library_controls_expanded');
            gamesHome.classList.add('games_home_expanded');
        }
    });
    $('#games_library_pagesize_select').select2();
    $('#games_library_orderby_select').select2();
    $('#games_library_orderby_direction_select').select2();

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
                }

                // get all elemens in the node gamesElement and sort by the data-index attribute
                gameTiles = Array.from(gamesElement.children);
                gameTiles.sort((a, b) => {
                    return a.getAttribute('data-index') - b.getAttribute('data-index');
                });

                // remove all children from the gamesElement
                gamesElement.innerHTML = '';

                // add the sorted children back to the gamesElement
                for (const gameTile of gameTiles) {
                    gamesElement.appendChild(gameTile);
                }
            };

            filter.ApplyFilter();
        });

        await db.GetGamesFilter();
    }
}

SetupPage();