class GameIcon {
    constructor(data) {
        this.data = data;
    }

    async Render(showTitle, showRatings, showClassification, classificationDisplayOrder, showSubtitle = true, useSmallCover = false) {
        let data = this.data;

        if (data === undefined) {
            data = {
                metadataMapId: -1,
                name: window.lang ? window.lang.translate('rendergames.unknown_game') : 'Unknown Game',
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
                if (shownClassificationBoard === '') {
                    for (const rating of data.ageRatings) {
                        let organization = null;
                        for (const key of Object.keys(AgeRatingMappings.RatingBoards)) {
                            if (AgeRatingMappings.RatingBoards[key].IGDBId === rating.organization) {
                                organization = AgeRatingMappings.RatingBoards[key];
                                break;
                            }
                        }

                        if (organization !== null) {
                            if (organization.ShortName === board) {
                                shownClassificationBoard = board;
                                displayClassification = true;
                                let ratingItem = Object.keys(organization.Ratings).find(key => organization.Ratings[key].IGDBId === rating.rating_category);
                                let ratingIcon = organization.Ratings[ratingItem].IconName;
                                classificationPath = '/images/Ratings/' + board + '/' + ratingIcon + '.svg';
                            }
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
                        gameImageRatingBannerValue.innerHTML = window.lang ? window.lang.translate('rendergames.rating.value', [Math.floor(data.totalRating), data.totalRatingCount]) : Math.floor(data.totalRating) + '% / ' + data.totalRatingCount;
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

class WideGameIcon {
    constructor(data) {
        this.data = data;
    }

    async Render(showTitle, showRatings, showClassification, classificationDisplayOrder, showSubtitle = true, useSmallCover = false) {
        let data = this.data;

        if (data === undefined) {
            data = {
                metadataMapId: -1,
                name: window.lang ? window.lang.translate('rendergames.unknown_game') : 'Unknown Game',
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
        gameTile.id = 'game_tile_wide_' + data.metadataMapId;
        gameTile.classList.add('game_tile_wide');
        gameTile.setAttribute('data-id', data.metadataMapId);
        gameTile.name = 'game_tile_wide';
        if (data.resultIndex) {
            gameTile.setAttribute('data-index', data.resultIndex);
        }
        gameTile.setAttribute('data-alpha', data.alpha);
        gameTile.setAttribute('data-index', data.index);

        // set the background image to the first screenshot if it exists, then fall back to the first artwork, then fall back to the cover if it exists
        this.backgroundImageUrls = this.GetBackgroundImageURLs('original');

        if (this.backgroundImageUrls.length > 0) {
            // Set initial background
            gameTile.style.backgroundImage = 'url(' + this.backgroundImageUrls[0] + ')';

            if (this.backgroundImageUrls.length > 1) {
                // Configuration: interval range in seconds for background image transitions
                const minIntervalSeconds = 3;
                const maxIntervalSeconds = 3;

                // Create an overlay element for smooth opacity-based fade transitions
                let overlay = document.createElement('div');
                overlay.style.position = 'absolute';
                overlay.style.top = '0';
                overlay.style.left = '0';
                overlay.style.width = '100%';
                overlay.style.height = '100%';
                overlay.style.opacity = '0';
                overlay.style.transition = 'opacity 1s ease-in-out';
                overlay.style.backgroundSize = 'cover';
                overlay.style.backgroundPosition = 'center';
                overlay.style.pointerEvents = 'none';
                gameTile.appendChild(overlay);

                // Ensure gameTile has position relative for overlay positioning and overflow hidden to clip rounded corners
                gameTile.style.position = 'relative';
                gameTile.style.overflow = 'hidden';

                // set up a random length interval to randomly change the background image - only when mouse is over
                let currentImageIndex = 0;
                let imageRotationInterval = null;
                const intervalMs = Math.floor(Math.random() * (maxIntervalSeconds - minIntervalSeconds) * 1000) + (minIntervalSeconds * 1000);

                const startImageRotation = () => {
                    if (imageRotationInterval !== null) return; // Already running

                    imageRotationInterval = setInterval(() => {
                        currentImageIndex++;
                        if (currentImageIndex >= this.backgroundImageUrls.length) {
                            currentImageIndex = 0;
                        }

                        // Set the new image on the overlay
                        overlay.style.backgroundImage = 'url(' + this.backgroundImageUrls[currentImageIndex] + ')';

                        // Fade in the overlay
                        overlay.style.opacity = '1';

                        // After fade completes, swap the images and reset overlay
                        setTimeout(() => {
                            gameTile.style.backgroundImage = 'url(' + this.backgroundImageUrls[currentImageIndex] + ')';
                            overlay.style.opacity = '0';
                        }, 1000); // Match transition duration
                    }, intervalMs);
                };

                const stopImageRotation = () => {
                    if (imageRotationInterval !== null) {
                        clearInterval(imageRotationInterval);
                        imageRotationInterval = null;
                    }

                    // Reset to first image
                    currentImageIndex = 0;
                    overlay.style.backgroundImage = 'url(' + this.backgroundImageUrls[0] + ')';
                    overlay.style.opacity = '1';
                    setTimeout(() => {
                        gameTile.style.backgroundImage = 'url(' + this.backgroundImageUrls[0] + ')';
                        overlay.style.opacity = '0';
                    }, 1000);
                };

                // Add mouse event listeners
                gameTile.addEventListener('mouseenter', startImageRotation);
                gameTile.addEventListener('mouseleave', stopImageRotation);
            }
        }

        // create the overlay
        let titleOverlay = document.createElement('div');
        titleOverlay.classList.add('game_tile_wide_banner');
        if (!data.cover) {
            titleOverlay.classList.add('game_tile_wide_banner_nocover');
        }
        gameTile.appendChild(titleOverlay);

        if (data.name) {
            let titleOverlayText = document.createElement('div');
            titleOverlayText.classList.add('game_tile_wide_title');
            titleOverlayText.innerHTML = data.name;
            titleOverlay.appendChild(titleOverlayText);
        }

        if (data.platformIds && data.platformIds.length > 0) {
            let platformIconsContainer = document.createElement('div');
            platformIconsContainer.classList.add('game_tile_wide_platforms');
            platformIconsContainer.style.display = 'none';

            for (const platformId of data.platformIds) {
                let platformIcon = document.createElement('div');
                platformIcon.classList.add('game_tile_wide_platform_icon');
                let platformIconImg = document.createElement('img');
                platformIconImg.src = `/api/v1.1/Platforms/${platformId}/platformlogo/original/logo.png`;
                platformIcon.appendChild(platformIconImg);
                platformIconsContainer.appendChild(platformIcon);
            }

            titleOverlay.appendChild(platformIconsContainer);

            // Set up hover events for showing/hiding platforms and animating titleOverlay
            gameTile.addEventListener('mouseenter', () => {
                platformIconsContainer.style.display = 'flex';
                titleOverlay.classList.add('expanded');
            });

            gameTile.addEventListener('mouseleave', () => {
                platformIconsContainer.style.display = 'none';
                titleOverlay.classList.remove('expanded');
            });
        }


        if (data.cover) {
            let coverUrl = '/api/v1.1/Games/' + data.metadataMapId + '/' + data.metadataSource + '/cover/' + data.cover + '/image/cover_big/' + data.cover + '.jpg';

            // place the cover image as a foreground element in the bottom left corner to ensure it is always visible and not obscured by the background images, with a fade in effect when it loads
            let coverImage = document.createElement('img');
            coverImage.src = coverUrl;
            coverImage.classList.add('game_tile_wide_cover');
            coverImage.style.opacity = '0';
            coverImage.onload = () => {
                coverImage.style.transition = 'opacity 1s ease-in-out';
                coverImage.style.opacity = '1';
            };
            gameTile.appendChild(coverImage);
        }

        // add save game icon
        if (data.hasSavedGame === true) {
            let gameSaveIcon = document.createElement('img');
            gameSaveIcon.src = '/images/SaveStates.png';
            gameSaveIcon.classList.add('game_tile_box_savedgame');
            gameSaveIcon.classList.add('savedstateicon');
            gameTile.appendChild(gameSaveIcon);
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
        gameTile.appendChild(gameFavIconBox);

        if (data.metadataMapId !== -1) {
            ['click'].forEach(event => {
                gameTile.addEventListener(event, (e) => {
                    let gameCard = new GameCard(data.metadataMapId);
                    gameCard.ShowCard();

                    e.stopPropagation();
                    e.preventDefault();
                });
            });
        }

        return gameTile;
    }

    GetBackgroundImageURLs(imageSize = 'original') {
        let urls = [];

        if (this.data.screenshots && this.data.screenshots.length > 0) {
            urls = this.GenerateBackgroundImageURLs('screenshot', imageSize, this.data.screenshots);
        } else if (this.data.artworks && this.data.artworks.length > 0) {
            urls = this.GenerateBackgroundImageURLs('artwork', imageSize, this.data.artworks);
        } else if (this.data.cover) {
            urls = this.GenerateBackgroundImageURLs('cover', imageSize, [this.data.cover]);
        } else {
            urls.push('/images/unknowngame.png');
        }
        return urls;
    }

    GenerateBackgroundImageURLs(imageType, imageSize, imageIds) {
        let urls = [];
        for (const imageId of imageIds) {
            let url = '/api/v1.1/Games/' + this.data.metadataMapId + '/' + this.data.metadataSource + '/' + imageType + '/' + imageId + '/image/' + imageSize + '/' + imageId + '.jpg';
            urls.push(url);
        }
        return urls;
    }
}