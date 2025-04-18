class GameIcon {
    constructor(data) {
        this.data = data;
    }

    async Render(showTitle, showRatings, showClassification, classificationDisplayOrder, showSubtitle = true, useSmallCover = false) {
        let data = this.data;

        if (data === undefined) {
            data = {
                metadataMapId: -1,
                name: 'Unknown Game',
                cover: null,
                totalRating: null,
                totalRatingCount: null,
                ageRatings: [],
                firstReleaseDate: null,
                isFavourite: false,
                hasSavedGame: false,
                alpha: 0,
                index: 0
            }
        }

        let gameTile = document.createElement('div');
        gameTile.id = 'game_tile_' + data.metadataMapId;
        gameTile.classList.add('game_tile');
        if (useSmallCover === true) {
            gameTile.classList.add('game_tile_small');
        }
        gameTile.setAttribute('data-id', data.metadataMapId);
        gameTile.name = 'game_tile';
        if (data.resultIndex) {
            gameTile.setAttribute('data-index', data.resultIndex);
        }
        gameTile.setAttribute('data-alpha', data.alpha);
        gameTile.setAttribute('data-index', data.index);

        let gameTileOuterBox = document.createElement('div');
        gameTileOuterBox.classList.add('game_tile_outer_box');

        let gameTileBox = document.createElement('div');
        gameTileBox.classList.add('game_tile_box');
        if (useSmallCover === true) {
            gameTileBox.classList.add('game_tile_box_small');
        }
        if (data.metadataMapId !== -1) {
            ['click'].forEach(event => {
                gameTileBox.addEventListener(event, (e) => {
                    let gameCard = new GameCard(data.metadataMapId);
                    gameCard.ShowCard();

                    e.stopPropagation();
                    e.preventDefault();
                });
            });
        }
        gameTileOuterBox.appendChild(gameTileBox);

        // cover art
        let gameTileImage = document.createElement('img');
        let gameTileImageSize = 'cover_big';
        gameTileImage.classList.add('game_tile_image');
        if (useSmallCover == true) {
            gameTileImage.classList.add('game_tile_image_small');
            gameTileImageSize = 'cover_small';
        }
        gameTileImage.setAttribute('loading', 'lazy');
        if (data.cover) {
            gameTileImage.setAttribute('src', '/api/v1.1/Games/' + data.metadataMapId + '/' + data.metadataSource + '/cover/' + data.cover + '/image/' + gameTileImageSize + '/' + data.cover + '.jpg');
        } else {
            gameTileImage.setAttribute('src', '/images/unknowngame.png');
        }
        gameTileBox.appendChild(gameTileImage);

        // classification badge
        let classificationPath = '';
        let displayClassification = false;
        let shownClassificationBoard = '';
        if (showClassification === true) {
            for (const board of classificationDisplayOrder) {
                if (shownClassificationBoard == '') {
                    for (const rating of data.ageRatings) {
                        if (rating.category == board) {
                            shownClassificationBoard = board;
                            displayClassification = true;
                            classificationPath = '/images/Ratings/' + board + '/' + rating.rating + '.svg';
                        }
                    }
                } else {
                    break;
                }
            }
        }

        if (data.metadataMapId !== -1) {
            // add save game icon
            if (data.hasSavedGame === true) {
                let gameSaveIcon = document.createElement('img');
                gameSaveIcon.src = '/images/SaveStates.png';
                gameSaveIcon.classList.add('game_tile_box_savedgame');
                gameSaveIcon.classList.add('savedstateicon');
                gameTileBox.appendChild(gameSaveIcon);
            }

            // add favourite game icon
            let gameFavIconBox = document.createElement('div');
            gameFavIconBox.classList.add('game_tile_box_favouritegame');

            let gameFavIcon = document.createElement('img');
            gameFavIcon.classList.add('favouriteicon');
            if (data.isFavourite === true) {
                gameFavIcon.src = '/images/favourite-filled.svg';
                gameFavIconBox.classList.add('favourite-filled');
            } else {
                gameFavIcon.src = '/images/favourite-empty.svg';
                gameFavIconBox.classList.add('favourite-empty');
            }
            gameFavIconBox.appendChild(gameFavIcon);

            gameFavIconBox.addEventListener('click', (e) => {
                e.stopPropagation();

                if (gameFavIconBox.classList.contains('favourite-filled')) {
                    gameFavIcon.src = '/images/favourite-empty.svg';
                    gameFavIconBox.classList.remove('favourite-filled');
                    gameFavIconBox.classList.add('favourite-empty');
                    data.isFavourite = false;
                } else {
                    gameFavIcon.src = '/images/favourite-filled.svg';
                    gameFavIconBox.classList.remove('favourite-empty');
                    gameFavIconBox.classList.add('favourite-filled');
                    data.isFavourite = true;
                }

                fetch('/api/v1.1/Games/' + data.metadataMapId + '/favourite?favourite=' + data.isFavourite, {
                    method: 'POST'
                }).then(response => {
                    if (response.ok) {
                        // console.log('Favourite status updated');
                    } else {
                        // console.log('Failed to update favourite status');
                    }
                });
            });
            gameTileBox.appendChild(gameFavIconBox);
        }

        // add ratings banner
        if (data.totalRating || displayClassification === true) {
            let gameImageRatingBanner = document.createElement('div');
            gameImageRatingBanner.classList.add('game_tile_box_ratingbanner');

            if (showRatings === true || displayClassification === true) {
                if (showRatings === true) {
                    if (data.totalRating) {
                        let gameImageRatingBannerLogo = document.createElement('img');
                        gameImageRatingBannerLogo.src = '/images/IGDB_logo.svg';
                        gameImageRatingBannerLogo.setAttribute('style', 'filter: invert(100%); height: 10px; margin-right: 5px; padding-top: 4px;');
                        gameImageRatingBanner.appendChild(gameImageRatingBannerLogo);

                        let gameImageRatingBannerValue = document.createElement('span');
                        gameImageRatingBannerValue.innerHTML = Math.floor(data.totalRating) + '% / ' + data.totalRatingCount;
                        gameImageRatingBanner.appendChild(gameImageRatingBannerValue);
                    }
                }

                gameTileBox.appendChild(gameImageRatingBanner);

                if (displayClassification === true) {
                    let gameImageClassificationLogo = document.createElement('img');
                    gameImageClassificationLogo.src = classificationPath;
                    gameImageClassificationLogo.classList.add('rating_image_overlay');
                    gameTileBox.appendChild(gameImageClassificationLogo);
                }
            }
        }

        // add game tile title
        if (showTitle === true) {
            let gameBoxTitleBox = document.createElement('div');
            gameBoxTitleBox.classList.add('game_tile_label_box');

            let gameBoxTitle = document.createElement('div');
            gameBoxTitle.classList.add('game_tile_label');
            if (useSmallCover === true) {
                gameBoxTitleBox.classList.add('game_tile_label_box_small');
                gameBoxTitle.classList.add('game_tile_label_small');
            }
            gameBoxTitle.innerHTML = data.name;
            gameBoxTitleBox.appendChild(gameBoxTitle);

            // add game tile subtitle
            if (showSubtitle === true) {
                if (data.firstReleaseDate !== undefined && data.firstReleaseDate !== null) {
                    let gameBoxSubtitle = document.createElement('div');
                    gameBoxSubtitle.classList.add('game_tile_label');
                    gameBoxSubtitle.classList.add('game_tile_subtitle');
                    gameBoxSubtitle.innerHTML = new Date(data.firstReleaseDate).getFullYear();
                    gameBoxTitleBox.appendChild(gameBoxSubtitle);
                }
            }
            gameTileOuterBox.appendChild(gameBoxTitleBox);
        }

        gameTile.appendChild(gameTileOuterBox);

        return gameTile;
    }
}