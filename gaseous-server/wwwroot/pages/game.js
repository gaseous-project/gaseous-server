var gameId = getQueryString('id', 'int');
var gameData;
var artworks = null;
var artworksPosition = 0;
var artworksTimer = null;
var selectedScreenshot = 0;
var remapCallCounter = 0;
var remapCallCounterMax = 0;

function SetupPage() {
    let mappingScript = document.createElement('script');
    mappingScript.src = '/pages/settings/mapping.js';
    document.head.appendChild(mappingScript);

    ajaxCall('/api/v1.1/Games/' + gameId, 'GET', function (result) {
        console.log(result);
        // populate games page
        gameData = result;

        // get name
        var gameTitleLabel = document.getElementById('gametitle_label');
        gameTitleLabel.innerHTML = result.name;

        // get critic rating
        if (gameData.total_rating) {
            var criticscoreval = document.getElementById('gametitle_criticrating_value');
            criticscoreval.innerHTML = Math.floor(gameData.total_rating) + '%';

            if (gameData.total_rating_count) {
                var criticscorelabel = document.getElementById('gametitle_criticrating_label');
                criticscorelabel.innerHTML = '<img src="/images/IGDB_logo.svg" style="filter: invert(100%); height: 13px; margin-bottom: -5px;" /><span style="font-size: 10px;"> User Rating<br />' + "based on " + gameData.total_rating_count + " votes</span>"
            }
        }

        // get alt name
        var gameTitleAltLabel = document.getElementById('gametitle_alts');
        if (result.alternative_names) {
            ajaxCall('/api/v1.1/Games/' + gameId + '/alternativename', 'GET', function (result) {
                var altNames = '';
                for (var i = 0; i < result.length; i++) {
                    if (altNames.length > 0) {
                        altNames += ', ';
                    }
                    altNames += result[i].name;
                }
                var gameTitleAltLabelText = document.getElementById('gametitle_alts_label');
                gameTitleAltLabelText.innerHTML = altNames;
            });
        } else {
            gameTitleAltLabel.setAttribute('style', 'display: none;');
        }

        // get summary
        var gameSummaryLabel = document.getElementById('gamesummarytext_label');
        if (result.summary || result.storyline) {
            if (result.summary) {
                gameSummaryLabel.innerHTML = result.summary.replaceAll("\n", "<br />");
            } else {
                gameSummaryLabel.innerHTML = result.storyline.replaceAll("\n", "<br />");
            }

            if (gameSummaryLabel.offsetHeight < gameSummaryLabel.scrollHeight ||
                gameSummaryLabel.offsetWidth < gameSummaryLabel.scrollWidth) {
                // your element has overflow and truncated
                // show read more / read less button
                document.querySelector('#gamesummarytext_label_button_expand').setAttribute('style', '');
            } else {
                // your element doesn't overflow (not truncated)
            }
        } else {
            gameSummaryLabel.setAttribute('style', 'display: none;');
        }

        // load cover
        var gameSummaryCover = document.getElementById('gamesummary_cover');
        var gameImage = document.createElement('img');
        gameImage.className = 'game_cover_image';
        if (result.cover) {
            ajaxCall('/api/v1.1/Games/' + gameId + '/cover', 'GET', function (coverResult) {
                if (coverResult) {
                    gameImage.src = '/api/v1.1/Games/' + gameId + '/cover/' + coverResult.id + '/image/cover_big/' + coverResult.imageId + '.jpg';

                    loadArtwork(result, coverResult);
                } else {
                    gameImage.src = '/images/unknowngame.png';
                    gameImage.className = 'game_cover_image unknown';

                    loadArtwork(result);
                }
            });
        } else {
            gameImage.src = '/images/unknowngame.png';
            gameImage.className = 'game_cover_image unknown';

            loadArtwork(result);
        }
        gameSummaryCover.appendChild(gameImage);

        // load companies
        var gameHeaderDeveloperLabel = document.getElementById('gamedeveloper_label');
        var gameDeveloperLabel = document.getElementById('gamesummary_developer');
        var gameDeveloperContent = document.getElementById('gamesummary_developer_content');
        var gamePublisherLabel = document.getElementById('gamesummary_publishers');
        var gamePublisherContent = document.getElementById('gamesummary_publishers_content');
        var gameDeveloperLoaded = false;
        var gamePublisherLoaded = false;
        if (result.involved_companies) {
            ajaxCall('/api/v1.1/Games/' + gameId + '/companies', 'GET', function (result) {
                var lstDevelopers = [];
                var lstPublishers = [];

                for (var i = 0; i < result.length; i++) {
                    var companyLabel = document.createElement('span');
                    companyLabel.className = 'gamegenrelabel';
                    companyLabel.innerHTML = result[i].company.name;

                    if (result[i].involvement.developer == true) {
                        if (!lstDevelopers.includes(result[i].company.name)) {
                            if (gameHeaderDeveloperLabel.innerHTML.length > 0) {
                                gameHeaderDeveloperLabel += ", ";
                            }
                            gameHeaderDeveloperLabel.innerHTML += result[i].company.name;

                            gameDeveloperContent.appendChild(companyLabel);

                            lstDevelopers.push(result[i].company.name);

                            gameDeveloperLoaded = true;
                        }
                    } else {
                        if (result[i].involvement.publisher == true) {
                            if (!lstPublishers.includes(result[i].company.name)) {
                                lstPublishers.push(result[i].company.name);
                                gamePublisherContent.appendChild(companyLabel);
                                gamePublisherLoaded = true;
                            }
                        }
                    }
                }

                if (gameDeveloperLoaded == false) {
                    gameHeaderDeveloperLabel.setAttribute('style', 'display: none;');
                    gameDeveloperLabel.setAttribute('style', 'display: none;');
                }
                if (gamePublisherLoaded == false) {
                    gamePublisherLabel.setAttribute('style', 'display: none;');
                }
            });
        } else {
            gameHeaderDeveloperLabel.setAttribute('style', 'display: none;');
            gameDeveloperLabel.setAttribute('style', 'display: none;');
            gamePublisherLabel.setAttribute('style', 'display: none;');
        }

        // load statistics
        ajaxCall('/api/v1.1/Statistics/Games/' + gameId, 'GET', function (result) {
            var gameStat_lastPlayed = document.getElementById('gamestatistics_lastplayed_value');
            var gameStat_timePlayed = document.getElementById('gamestatistics_timeplayed_value');
            if (result) {
                // gameStat_lastPlayed.innerHTML = moment(result.sessionEnd).format("YYYY-MM-DD h:mm:ss a");
                const dateOptions = {
                    //weekday: 'long',
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric'
                };
                gameStat_lastPlayed.innerHTML = new Date(result.sessionEnd).toLocaleDateString(undefined, dateOptions);
                if (result.sessionLength >= 60) {
                    gameStat_timePlayed.innerHTML = Number(result.sessionLength / 60).toFixed(2) + " hours";
                } else {
                    gameStat_timePlayed.innerHTML = Number(result.sessionLength) + " minutes";
                }
            } else {
                gameStat_lastPlayed.innerHTML = '-';
                gameStat_timePlayed.innerHTML = '-';
            }
        });

        // load favourites
        ajaxCall('/api/v1.1/Games/' + gameId + '/favourite', 'GET', function (result) {
            var gameFavButton = document.getElementById('gamestatistics_favourite_button');
            var gameFavIcon = document.createElement('img');
            gameFavIcon.id = "gamestatistics_favourite";
            gameFavIcon.className = "favouriteicon";
            gameFavIcon.title = "Favourite";
            gameFavIcon.alt = "Favourite";

            if (result == true) {
                gameFavIcon.setAttribute("src", '/images/favourite-filled.svg');
                gameFavIcon.setAttribute('onclick', "SetGameFavourite(false);");
            } else {
                gameFavIcon.setAttribute("src", '/images/favourite-empty.svg');
                gameFavIcon.setAttribute('onclick', "SetGameFavourite(true);");
            }

            gameFavButton.innerHTML = '';
            gameFavButton.appendChild(gameFavIcon);
        });

        // load release date
        var gameSummaryRelease = document.getElementById('gamesummary_firstrelease');
        var gameSummaryReleaseContent = document.getElementById('gamesummary_firstrelease_content');
        if (result.first_release_date) {
            var firstRelease = document.createElement('span');
            firstRelease.innerHTML = moment(result.first_release_date).format('LL') + ' (' + moment(result.first_release_date).fromNow() + ')';
            gameSummaryReleaseContent.appendChild(firstRelease);
        } else {
            gameSummaryRelease.setAttribute('style', 'display: none;');
        }

        // load ratings
        let gameSummaryRatings = document.getElementById('gamesummary_ratings');
        let gameSummaryRatingsContent = document.getElementById('gamesummary_ratings_content');
        if (result.age_ratings) {
            ajaxCall('/api/v1.1/Games/' + gameId + '/agerating', 'GET', function (result) {
                let classTable = document.createElement('table');

                let SpotlightClassifications = GetRatingsBoards();

                let ratingSelected = false;
                for (let r = 0; r < SpotlightClassifications.length; r++) {
                    for (let i = 0; i < result.length; i++) {
                        if (result[i].ratingBoard == SpotlightClassifications[r]) {
                            let ratingImage = document.createElement('img');
                            ratingImage.src = '/images/Ratings/' + result[i].ratingBoard + '/' + result[i].ratingTitle + '.svg';
                            let ratingString = ClassificationBoards[result[i].ratingBoard] + "\nRating: " + ClassificationRatings[result[i].ratingTitle];
                            if (result[i].descriptions.length > 0) {
                                ratingString += '\nContains: ' + result[i].descriptions.join(', ');
                            }
                            ratingImage.title = ratingString;

                            ratingImage.className = 'rating_image';

                            let classTableRow = document.createElement('tr');
                            let classTableLogo = document.createElement('td');
                            classTableLogo.className = 'rating_image_logo_table';
                            classTableLogo.appendChild(ratingImage);
                            classTableRow.appendChild(classTableLogo);
                            let classTableDescription = document.createElement('td');
                            if (result[i].descriptions.length > 0) {
                                classTableDescription.innerHTML = result[i].descriptions.join('<br />');
                            } else {
                                classTableDescription.innerHTML = ClassificationRatings[result[i].ratingTitle];
                            }
                            classTableRow.appendChild(classTableDescription);
                            classTable.appendChild(classTableRow);

                            gameSummaryRatingsContent.appendChild(classTable);
                            ratingSelected = true;
                            break;
                        }
                    }
                    if (ratingSelected == true) { break; }
                }

                if (ratingSelected == false) {
                    gameSummaryRatings.setAttribute('style', 'display: none;');
                }
            });
        } else {
            gameSummaryRatings.setAttribute('style', 'display: none;');
        }

        // load genres
        var gameSummaryGenres = document.getElementById('gamesumarry_genres');
        var gameSummaryGenresContent = document.getElementById('gamesumarry_genres_content');
        if (result.genres) {
            ajaxCall('/api/v1.1/Games/' + gameId + '/genre', 'GET', function (result) {
                for (var i = 0; i < result.length; i++) {
                    var genreLabel = document.createElement('span');
                    genreLabel.className = 'gamegenrelabel';
                    genreLabel.innerHTML = result[i].name;

                    gameSummaryGenresContent.appendChild(genreLabel);
                }
            });
        } else {
            gameSummaryGenres.setAttribute('style', 'display: none;');
        }

        // get platforms
        LoadGamePlatforms();

        // load screenshots
        var gameScreenshots = document.getElementById('gamescreenshots');
        if (result.screenshots || result.videos) {
            var gameScreenshots_Main = document.getElementById('gamescreenshots_main');

            let gameScreenshots_Portal = document.getElementById('gamescreenshots');
            gameScreenshots_Portal.addEventListener('mouseenter', function () {
                $(".gamescreenshots_arrows")
                    .stop(true, true)
                    .animate({
                        opacity: 1
                    }, 500);
            });
            gameScreenshots_Portal.addEventListener('mouseleave', function () {
                $(".gamescreenshots_arrows")
                    .stop(true, true)
                    .animate({
                        opacity: 0
                    }, 500);
            });

            // load static screenshots
            var gameScreenshots_Gallery = document.getElementById('gamescreenshots_gallery_panel');
            var imageIndex = 0;
            if (result.videos) {
                imageIndex = result.videos.length;
            }
            if (result.screenshots) {
                ajaxCall('/api/v1.1/Games/' + gameId + '/screenshots', 'GET', function (screenshotsItem) {
                    for (var i = 0; i < screenshotsItem.length; i++) {
                        var screenshotItem = document.createElement('li');
                        screenshotItem.id = 'gamescreenshots_gallery_' + imageIndex;
                        screenshotItem.setAttribute('name', 'gamescreenshots_gallery_item');
                        screenshotItem.setAttribute('style', 'background-image: url("/api/v1.1/Games/' + gameId + '/screenshots/' + screenshotsItem[i].id + '/image/screenshot_thumb/' + screenshotsItem[i].imageId + '.jpg"); background-position: center; background-repeat: no-repeat; background-size: contain;)');
                        screenshotItem.setAttribute('data-url', '/api/v1.1/Games/' + gameId + '/screenshots/' + screenshotsItem[i].id + '/image/screenshot_thumb/' + screenshotsItem[i].imageId + '.jpg');
                        screenshotItem.setAttribute('imageid', imageIndex);
                        screenshotItem.setAttribute('imagetype', 0);
                        screenshotItem.className = 'gamescreenshots_gallery_item';
                        screenshotItem.setAttribute('onclick', 'selectScreenshot(' + imageIndex + ');');
                        gameScreenshots_Gallery.appendChild(screenshotItem);
                        imageIndex += 1;
                    }

                    selectScreenshot(0);
                });
            }

            // load videos
            if (result.videos) {
                ajaxCall('/api/v1.1/Games/' + gameId + '/videos', 'GET', function (result) {
                    var gameScreenshots_vGallery = document.getElementById('gamescreenshots_gallery_panel');
                    for (var i = 0; i < result.length; i++) {
                        var vScreenshotItem = document.createElement('li');
                        vScreenshotItem.id = 'gamescreenshots_gallery_' + i;
                        vScreenshotItem.setAttribute('name', 'gamescreenshots_gallery_item');
                        vScreenshotItem.setAttribute('style', 'background-image: url("https://i.ytimg.com/vi/' + result[i].video_id + '/hqdefault.jpg"); background-position: center; background-repeat: no-repeat; background-size: contain;)');
                        vScreenshotItem.setAttribute('imageid', i);
                        vScreenshotItem.setAttribute('imagetype', 1);
                        vScreenshotItem.setAttribute('imageref', result[i].video_id);
                        vScreenshotItem.className = 'gamescreenshots_gallery_item';
                        vScreenshotItem.setAttribute('onclick', 'selectScreenshot(' + i + ');');

                        var youtubeIcon = document.createElement('img');
                        youtubeIcon.src = '/images/YouTube.svg';
                        youtubeIcon.className = 'gamescreenshosts_gallery_item_youtube';
                        vScreenshotItem.appendChild(youtubeIcon);

                        gameScreenshots_vGallery.insertBefore(vScreenshotItem, gameScreenshots_vGallery.firstChild);
                    }

                    // sort items
                    var items = gameScreenshots_vGallery.childNodes;
                    var itemsArr = [];
                    for (var i in items) {
                        if (items[i].nodeType == 1) { // get rid of the whitespace text nodes
                            itemsArr.push(items[i]);
                        }
                    }

                    itemsArr.sort(function (a, b) {
                        return Number(a.getAttribute('imageid')) == Number(b.getAttribute('imageid'))
                            ? 0
                            : (Number(a.getAttribute('imageid')) > Number(b.getAttribute('imageid')) ? 1 : -1);
                    });

                    for (i = 0; i < itemsArr.length; ++i) {
                        gameScreenshots_vGallery.appendChild(itemsArr[i]);
                    }

                    selectScreenshot(0);
                }, function (error) {
                    selectScreenshot(0);
                });
            } else {
                //selectScreenshot(0);
            }
        } else {
            gamescreenshots.setAttribute('style', 'display: none;');
        }

        // load similar
        var gameSummarySimilar = document.getElementById('gamesummarysimilar');
        ajaxCall('/api/v1.1/Games/' + gameId + '/Related', 'GET', function (result) {
            if (result.games.length > 0) {
                gameSummarySimilar.removeAttribute('style');

                var gameSummarySimilarContent = document.getElementById('gamesummarysimilarcontent');
                for (var i = 0; i < result.games.length; i++) {
                    var similarObject = renderGameIcon(result.games[i], true, true, true, GetRatingsBoards(), false, true, false);
                    gameSummarySimilarContent.appendChild(similarObject);
                }

                $('.lazy').Lazy({
                    scrollDirection: 'vertical',
                    effect: 'fadeIn',
                    visibleOnly: true
                });
            } else {
                gameSummarySimilar.setAttribute('style', 'display: none;');
            }
        });
    });
};

function LoadGamePlatforms() {
    // get platforms
    ajaxCall('/api/v1.1/Games/' + gameId + '/platforms', 'GET', async function (result) {
        let platformContainer = document.getElementById('gamesummaryplatformscontent');
        platformContainer.innerHTML = '';
        for (let i = 0; i < result.length; i++) {
            let logoUrl = '/api/v1.1/Platforms/' + result[i].id + '/platformlogo/original/logo.png';

            // create platform container
            let platformItem = document.createElement('div');
            platformItem.className = 'platform_item';
            platformItem.setAttribute('isFavourite', false);
            platformItem.setAttribute('isLastUsed', false);
            platformItem.setAttribute('isEditButton', false);

            let platformData = result[i];

            let showSaveState = false;
            let romId = null;
            let isMediaGroup = null;

            // the platform button should:
            // 1. if FavouriteRomId is not null, load the rom, otherwise
            // 2. if LastPlayedRomId is null, load the rom, otherwise
            // 3. load the rom management dialog
            if (result[i].emulatorConfiguration.emulatorType.length > 0 && result[i].emulatorConfiguration.core.length > 0 && result[i].favouriteRomId) {
                showSaveState = true;
                romId = result[i].favouriteRomId;
                isMediaGroup = result[i].favouriteRomIsMediagroup;

                platformItem.setAttribute('isFavourite', true);
                platformItem.classList.add('platform_item_green');

                let launchLink = await BuildLaunchLink(platformData.emulatorConfiguration.emulatorType, platformData.emulatorConfiguration.core, platformData.id, Number(gameId), platformData.favouriteRomId, platformData.favouriteRomIsMediagroup, platformData.favouriteRomName);

                platformItem.addEventListener('click', () => {
                    window.location.href = launchLink;
                });
            } else if (result[i].emulatorConfiguration.emulatorType.length > 0 && result[i].emulatorConfiguration.core.length > 0 && result[i].lastPlayedRomId) {
                showSaveState = true;
                romId = result[i].lastPlayedRomId;
                isMediaGroup = result[i].lastPlayedRomIsMediagroup;

                platformItem.setAttribute('isLastUsed', true);
                platformItem.classList.add('platform_item_green');

                let launchLink = await BuildLaunchLink(platformData.emulatorConfiguration.emulatorType, platformData.emulatorConfiguration.core, platformData.id, Number(gameId), platformData.lastPlayedRomId, platformData.lastPlayedRomIsMediagroup, platformData.lastPlayedRomName);

                platformItem.addEventListener('click', () => {
                    window.location.href = launchLink;
                });
            } else {
                platformItem.setAttribute('isEditButton', true);
                platformItem.addEventListener('click', () => {
                    let romMgt = new RomManagement(result[i], LoadGamePlatforms);
                    romMgt.open();
                });
            }

            // create platform image container
            let platformImageContainer = document.createElement('div');
            platformImageContainer.className = 'platform_image_container';

            // create platform image
            let platformImage = document.createElement('img');
            platformImage.src = logoUrl;
            platformImage.className = 'platform_image';

            // create platform name container
            let platformNameContainer = document.createElement('div');
            platformNameContainer.className = 'platform_name_container';

            // create platform name
            let platformName = document.createElement('div');
            platformName.className = 'platform_name';
            platformName.innerHTML = result[i].name;

            // create platform edit button container
            let platformEditButtonContainer = document.createElement('div');
            platformEditButtonContainer.className = 'platform_edit_button_container';

            // create platform state manager button
            let platformStateManagerButton = document.createElement('div');
            if (showSaveState === true) {
                platformStateManagerButton.className = 'platform_edit_button platform_statemanager_button';
                platformStateManagerButton.innerHTML = '<img src="/images/SaveStates.png" class="savedstatemanagericon" />';
                platformStateManagerButton.addEventListener('click', (e) => {
                    e.stopPropagation();
                    console.log('RomID: ' + romId + ' isMediaGroup: ' + isMediaGroup);
                    let stateManager = new EmulatorStateManager(romId, isMediaGroup, platformData.emulatorConfiguration.emulatorType, platformData.emulatorConfiguration.core, platformData.id, gameId, platformData.lastPlayedRomName);
                    stateManager.open();
                });
            }

            // create platform edit button
            let platformEditButton = document.createElement('div');
            platformEditButton.className = 'platform_edit_button';
            platformEditButton.innerHTML = '<img src="/images/edit.svg" class="banner_button_image" />';
            platformEditButton.addEventListener('click', (e) => {
                e.stopPropagation();
                let romMgt = new RomManagement(result[i], LoadGamePlatforms);
                romMgt.open();
            });

            // append elements
            platformImageContainer.appendChild(platformImage);
            platformItem.appendChild(platformImageContainer);
            platformNameContainer.appendChild(platformName);
            platformItem.appendChild(platformNameContainer);
            platformItem.appendChild(platformEditButtonContainer);
            platformItem.appendChild(platformEditButton);
            if (showSaveState === true) {
                platformItem.appendChild(platformStateManagerButton);
            }
            platformContainer.appendChild(platformItem);
        }
    });
}

class RomManagement {
    constructor(Platform, okCallback) {
        this.Platform = Platform;
        this.MediaGroupCount = 0;
        this.RomCount = 0;

        this.OkCallback = okCallback;
    }

    async open() {
        this.romsModal = await new Modal('gameroms');
        await this.romsModal.BuildModal();

        // set the title
        this.romsModal.modalElement.querySelector('#modal-header-text').innerHTML = this.Platform.name + ' ROMs';

        // set the content - media groups
        this.MediaGroups = this.romsModal.modalElement.querySelector('#gamesummarymediagroups');
        this.MediaGroupsContent = this.romsModal.modalElement.querySelector('#gamesummarymediagroupscontent');
        this.#loadMediaGroups();

        // set the content - roms
        this.Roms = this.romsModal.modalElement.querySelector('#gamesummaryroms');
        this.RomsContent = this.romsModal.modalElement.querySelector('#gamesummaryromscontent');
        this.RomsNone = this.romsModal.modalElement.querySelector('#rom_no_roms');
        this.RomsNameSearch = this.romsModal.modalElement.querySelector('#name_filter');
        this.RomsEditButton = this.romsModal.modalElement.querySelector('#rom_edit');
        this.RomsEditButton.addEventListener('click', () => {
            this.#DisplayROMCheckboxes();
        });
        this.RomsEditPanel = this.romsModal.modalElement.querySelector('#rom_edit_panel');
        this.RomsDeleteButton = this.romsModal.modalElement.querySelector('#rom_edit_delete');
        this.RomsDeleteButton.addEventListener('click', () => {
            this.#deleteGameRoms();
        });
        this.RomsEditUpdateButton = this.romsModal.modalElement.querySelector('#rom_edit_update');
        this.RomsEditUpdateButton.addEventListener('click', () => {
            this.#remapTitles();
        });
        this.RomsCreateMGGroupButton = this.romsModal.modalElement.querySelector('#rom_edit_creategroup');
        this.RomsCreateMGGroupButton.addEventListener('click', () => {
            this.#createMgGroup();
        });
        this.RomsSearchButton = this.romsModal.modalElement.querySelector('#games_library_search');
        this.RomsSearchButton.addEventListener('click', () => {
            this.#loadRoms();
        });
        this.RomsFixPlatformDropdown = this.romsModal.modalElement.querySelector('#rom_edit_fixplatform');
        this.RomsFixGameDropdown = this.romsModal.modalElement.querySelector('#rom_edit_fixgame');
        this.#SetupFixPlatformDropDown();

        // add buttons
        let platformEditButton = new ModalButton('Edit Platform', 0, this, async function (callingObject) {
            let mappingModal = await new Modal('messagebox');
            await mappingModal.BuildModal();

            // override the dialog size
            mappingModal.modalElement.style = 'width: 600px; height: 80%; min-width: unset; min-height: 400px; max-width: unset; max-height: 80%;';

            // get the platform map
            let platformMap = await fetch('/api/v1.1/PlatformMaps/' + callingObject.Platform.id, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            }).then(response => response.json());
            let defaultPlatformMap = platformMap;

            // get the user emulation configuration
            let userEmuConfig = await fetch('/api/v1.1/Games/' + gameId + '/emulatorconfiguration/' + callingObject.Platform.id, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            }).then(response => response.json());
            if (userEmuConfig) {
                if (userEmuConfig.emulatorType || userEmuConfig.core) {
                    platformMap.webEmulator.type = userEmuConfig.emulatorType;
                    platformMap.webEmulator.core = userEmuConfig.core;
                }
                if (userEmuConfig.enableBIOSFiles) {
                    platformMap.enabledBIOSHashes = userEmuConfig.enableBIOSFiles;
                }
            }

            // set the title
            mappingModal.modalElement.querySelector('#modal-header-text').innerHTML = callingObject.Platform.name + ' Emulation Settings';

            // set the content
            let mappingContent = mappingModal.modalElement.querySelector('#modal-body');
            mappingContent.innerHTML = '';
            let emuConfig = await new WebEmulatorConfiguration(platformMap)
            emuConfig.open();
            mappingContent.appendChild(emuConfig.panel);

            // setup the buttons
            let resetButton = new ModalButton('Reset to Default', 0, callingObject, async function (callingObject) {
                await fetch('/api/v1.1/Games/' + gameId + '/emulatorconfiguration/' + callingObject.Platform.id, {
                    method: 'DELETE'
                });
                callingObject.Platform.emulatorConfiguration.emulatorType = defaultPlatformMap.webEmulator.type;
                callingObject.Platform.emulatorConfiguration.core = defaultPlatformMap.webEmulator.core;
                callingObject.Platform.emulatorConfiguration.enabledBIOSHashes = defaultPlatformMap.enabledBIOSHashes;
                callingObject.#loadRoms();
                callingObject.OkCallback();
                mappingModal.close();
            });
            mappingModal.addButton(resetButton);

            let okButton = new ModalButton('OK', 1, callingObject, async function (callingObject) {
                let model = {
                    EmulatorType: emuConfig.PlatformMap.webEmulator.type,
                    Core: emuConfig.PlatformMap.webEmulator.core,
                    EnableBIOSFiles: emuConfig.PlatformMap.enabledBIOSHashes
                }

                await fetch('/api/v1.1/Games/' + gameId + '/emulatorconfiguration/' + callingObject.Platform.id, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(model)
                });
                callingObject.Platform.emulatorConfiguration.emulatorType = emuConfig.PlatformMap.webEmulator.type;
                callingObject.Platform.emulatorConfiguration.core = emuConfig.PlatformMap.webEmulator.core;
                callingObject.Platform.emulatorConfiguration.enabledBIOSHashes = emuConfig.PlatformMap.enabledBIOSHashes;

                callingObject.#loadRoms();
                callingObject.OkCallback();
                mappingModal.close();
            });
            mappingModal.addButton(okButton);

            let cancelButton = new ModalButton('Cancel', 0, mappingModal, async function (callingObject) {
                mappingModal.close();
            });
            mappingModal.addButton(cancelButton);

            // show the dialog
            await mappingModal.open();
        });
        this.romsModal.addButton(platformEditButton);

        let closeButton = new ModalButton('Close', 0, this, function (callingObject) {
            callingObject.romsModal.close();
        });
        this.romsModal.addButton(closeButton);

        await this.#loadRoms(false);
        this.#DisplayROMCheckboxes(false);

        this.romsModal.open();
    }

    async #loadMediaGroups() {
        this.MediaGroupCount = 0;

        fetch('/api/v1.1/Games/' + gameId + '/romgroup?platformid=' + this.Platform.id, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(response => response.json()).then(result => {
            // display media groups
            if (result.length == 0) {
                this.MediaGroups.style.display = 'none';
            } else {
                this.MediaGroupCount = result.length;

                this.MediaGroups.style.display = '';
                this.MediaGroupsContent.innerHTML = '';
                let mgTable = document.createElement('table');
                mgTable.id = 'mediagrouptable';
                mgTable.className = 'romtable';
                mgTable.setAttribute('cellspacing', 0);
                mgTable.appendChild(createTableRow(true, ['', '', 'Status', 'Images', 'Size', '', '', '']));

                for (let i = 0; i < result.length; i++) {
                    let mediaGroup = result[i];

                    // get rom details including emulator and friendly platform name
                    let launchButton = '';
                    let saveStatesButton = '';
                    if (this.Platform.emulatorConfiguration) {
                        if ((this.Platform.emulatorConfiguration.emulatorType.length > 0) && (this.Platform.emulatorConfiguration.core.length > 0)) {
                            let romPath = encodeURIComponent('/api/v1.1/Games/' + gameId + '/romgroup/' + mediaGroup.id + '/' + gameData.name + '.zip');

                            if (mediaGroup.hasSaveStates == true) {
                                let modalVariables = {
                                    "romId": mediaGroup.id,
                                    "IsMediaGroup": true,
                                    "engine": this.Platform.emulatorConfiguration.emulatorType,
                                    "core": this.Platform.emulatorConfiguration.core,
                                    "platformid": mediaGroup.platformId,
                                    "gameid": gameId,
                                    "mediagroup": 1,
                                    "rompath": romPath
                                };
                                saveStatesButton = document.createElement('div');
                                saveStatesButton.addEventListener('click', () => {
                                    let stateManager = new EmulatorStateManager(mediaGroup.id, true, this.Platform.emulatorConfiguration.emulatorType, this.Platform.emulatorConfiguration.core, mediaGroup.platformId, gameId, romPath);
                                    stateManager.open();
                                });
                                saveStatesButton.innerHTML = '<img src="/images/SaveStates.png" class="savedstateicon" />';
                            }

                            launchButton = '<a href="/index.html?page=emulator&engine=' + this.Platform.emulatorConfiguration.emulatorType + '&core=' + this.Platform.emulatorConfiguration.core + '&platformid=' + mediaGroup.platformId + '&gameid=' + gameId + '&romid=' + mediaGroup.id + '&mediagroup=1&rompath=' + romPath + '" class="romstart">Launch</a>';
                        }
                    }

                    let favouriteRom = document.createElement('img');
                    favouriteRom.className = 'banner_button_image';
                    favouriteRom.title = 'Favourite';
                    favouriteRom.alt = 'Favourite';
                    favouriteRom.setAttribute('name', 'favourite_rom_button');
                    favouriteRom.style.cursor = 'pointer';
                    if (mediaGroup.romUserFavourite == true) {
                        favouriteRom.src = '/images/favourite-filled.svg';
                    } else {
                        favouriteRom.src = '/images/favourite-empty.svg';
                    }
                    favouriteRom.addEventListener('click', async () => {
                        await fetch('/api/v1.1/Games/' + gameId + '/roms/' + mediaGroup.id + '/' + mediaGroup.platformId + '/favourite?IsMediaGroup=true&favourite=true', {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json'
                            }
                        }).then(async response => {
                            if (response.ok) {
                                this.romsModal.modalElement.querySelectorAll('[name="favourite_rom_button"]').forEach((element) => {
                                    element.src = '/images/favourite-empty.svg';
                                });
                                favouriteRom.src = '/images/favourite-filled.svg';
                            }
                        });
                    });

                    let recentlyRun = document.createElement('img');
                    recentlyRun.src = '/images/recent.svg';
                    recentlyRun.className = 'banner_button_image';
                    recentlyRun.title = 'Recently Played';
                    recentlyRun.alt = 'Recently Played';
                    if (mediaGroup.romUserLastUsed == true) {
                        recentlyRun.style.display = '';
                    } else {
                        recentlyRun.style.display = 'none';
                    }

                    let statusText = mediaGroup.status;
                    let downloadLink = '';
                    let packageSize = '-';
                    let launchButtonContent = '';
                    let inProgress = false;
                    switch (mediaGroup.status) {
                        case 'NoStatus':
                            statusText = '-';
                            break;
                        case "WaitingForBuild":
                            statusText = 'Build pending';
                            inProgress = true;
                            break;
                        case "Building":
                            statusText = 'Building';
                            inProgress = true;
                            break;
                        case "Completed":
                            statusText = 'Available';
                            downloadLink = '<a href="/api/v1.1/Games/' + gameId + '/romgroup/' + mediaGroup.id + '/' + gameData.name + '.zip" class="romlink"><img src="/images/download.svg" class="banner_button_image" alt="Download" title="Download" /></a>';
                            packageSize = formatBytes(mediaGroup.size);
                            launchButtonContent = launchButton;
                            break;
                        case "Failed":
                            statusText = 'Build error';
                            break;
                        default:
                            statusText = result[i].buildStatus;
                            break;
                    }

                    let thisObject = this;

                    if (inProgress == true) {
                        setTimeout(this.#loadMediaGroups.bind(this), 10000);
                    }

                    let controls = document.createElement('div');
                    controls.style.textAlign = 'right';
                    controls.innerHTML = downloadLink;

                    let deleteButton = document.createElement('a');
                    deleteButton.href = '#';
                    deleteButton.addEventListener('click', function () {
                        // showSubDialog('mediagroupdelete', mediaGroup.id);
                        const deleteWindow = new MessageBox("Delete Selected Media Group", "Are you sure you want to delete this media group and all associated saved states?");

                        let deleteButton = new ModalButton("Delete", 2, deleteWindow, function (callingObject) {
                            ajaxCall(
                                '/api/v1.1/Games/' + gameData.id + '/romgroup/' + mediaGroup.id,
                                'DELETE',
                                function (result) {
                                    thisObject.#loadRoms();
                                    thisObject.#loadMediaGroups();
                                },
                                function (error) {
                                    thisObject.#loadRoms();
                                    thisObject.#loadMediaGroups();
                                }
                            );
                            callingObject.msgDialog.close();
                        });
                        deleteWindow.addButton(deleteButton);

                        let cancelButton = new ModalButton("Cancel", 0, deleteWindow, function (callingObject) {
                            callingObject.msgDialog.close();
                        });
                        deleteWindow.addButton(cancelButton);

                        deleteWindow.open();
                    });
                    deleteButton.className = 'romlink';
                    deleteButton.innerHTML = '<img src="/images/delete.svg" class="banner_button_image" alt="Delete" title="Delete" />';
                    controls.appendChild(deleteButton);

                    let newRow = [
                        favouriteRom,
                        recentlyRun,
                        statusText,
                        mediaGroup.romIds.length,
                        packageSize,
                        saveStatesButton,
                        launchButtonContent,
                        controls
                    ]

                    let mgRowBody = document.createElement('tbody');
                    mgRowBody.className = 'romrow';

                    mgRowBody.appendChild(createTableRow(false, newRow, '', 'romcell'));

                    let mgRomRow = document.createElement('tr');
                    let mgRomCell = document.createElement('td');
                    mgRomCell.setAttribute('colspan', 8);
                    mgRomCell.className = 'romGroupTitles';

                    // iterate the group members
                    let groupMembers = [];
                    for (let r = 0; r < mediaGroup.roms.length; r++) {
                        groupMembers.push(mediaGroup.roms[r]);
                    }

                    groupMembers.sort((a, b) => (a.name > b.name) ? 1 : ((b.name > a.name) ? -1 : 0));
                    let groupMemberNames = [];
                    for (let r = 0; r < groupMembers.length; r++) {
                        groupMemberNames.push(groupMembers[r].name);
                    }
                    mgRomCell.innerHTML = groupMemberNames.join("<br />");
                    mgRomRow.appendChild(mgRomCell);
                    mgRowBody.appendChild(mgRomRow);

                    mgTable.appendChild(mgRowBody);
                }

                this.MediaGroupsContent.appendChild(mgTable);
            }
        });
    }

    async #loadRoms(displayCheckboxes, pageNumber) {
        if (!pageNumber) {
            pageNumber = 1;
        }

        let selectedPlatform = this.Platform.id;

        let nameSearchQuery = '';
        let nameSearch = this.RomsNameSearch.value;
        if (nameSearch != undefined && nameSearch != "") {
            nameSearchQuery = '&NameSearch=' + encodeURIComponent(nameSearch);
        }

        let existingTable = this.romsModal.modalElement.querySelector('#romtable');
        if (existingTable) {
            existingTable.remove();
        }

        let romPager = this.romsModal.modalElement.querySelector('#romPaginator');
        if (romPager) {
            romPager.remove();
        }

        if (displayCheckboxes == undefined) {
            if (this.romsModal.modalElement.querySelector('#rom_edit_panel').style.display == 'none') {
                displayCheckboxes = false;
            } else {
                displayCheckboxes = true;
            }
        }

        let gameRomsSection = this.Roms;
        let gameRoms = this.RomsContent;
        let pageSize = 200;
        await fetch('/api/v1.1/Games/' + gameId + '/roms?pageNumber=' + pageNumber + '&pageSize=' + pageSize + '&platformId=' + selectedPlatform + nameSearchQuery, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(async response => {
            if (response.ok) {
                let result = await response.json();
                let romCount = this.romsModal.modalElement.querySelector('#games_roms_count');
                this.RomCount = result.count;
                if (result.count != 1) {
                    romCount.innerHTML = result.count + ' ROMs';
                } else {
                    romCount.innerHTML = result.count + ' ROM';
                }

                if (result.gameRomItems) {
                    let gameRomItems = result.gameRomItems;

                    // display roms
                    let romMasterCheck = document.createElement('input');
                    romMasterCheck.id = 'rom_mastercheck';
                    romMasterCheck.type = 'checkbox';
                    romMasterCheck.onclick = () => {
                        this.#selectAllChecks();
                        this.#handleChecks();
                    };

                    let newTable = document.createElement('table');
                    newTable.id = 'romtable';
                    newTable.className = 'romtable';
                    newTable.setAttribute('cellspacing', 0);
                    newTable.appendChild(
                        createTableRow(
                            true,
                            [
                                [
                                    romMasterCheck,
                                    'rom_checkbox_box_hidden',
                                    'rom_edit_checkbox'
                                ],
                                '',
                                '',
                                'Name',
                                'Size',
                                'Media',
                                '',
                                '',
                                '',
                                ''
                            ]
                        )
                    );

                    let thisDialog = this;

                    for (let i = 0; i < gameRomItems.length; i++) {
                        let romItem = gameRomItems[i];

                        let saveStatesButton = '';
                        let launchButton = '';
                        if (this.Platform.emulatorConfiguration) {
                            if (this.Platform.emulatorConfiguration.emulatorType) {
                                if (this.Platform.emulatorConfiguration.emulatorType.length > 0) {
                                    let romPath = encodeURIComponent('/api/v1.1/Games/' + gameId + '/roms/' + gameRomItems[i].id + '/' + gameRomItems[i].name);
                                    if (gameRomItems[i].hasSaveStates == true) {
                                        let modalVariables = {
                                            "romId": gameRomItems[i].id,
                                            "IsMediaGroup": false,
                                            "engine": this.Platform.emulatorConfiguration.emulatorType,
                                            "core": this.Platform.emulatorConfiguration.core,
                                            "platformid": gameRomItems[i].platformId,
                                            "gameid": gameId,
                                            "mediagroup": 0,
                                            "rompath": romPath
                                        };
                                        saveStatesButton = document.createElement('div');
                                        saveStatesButton.addEventListener('click', () => {
                                            let stateManager = new EmulatorStateManager(gameRomItems[i].id, false, this.Platform.emulatorConfiguration.emulatorType, this.Platform.emulatorConfiguration.core, gameRomItems[i].platformId, gameId, gameRomItems[i].name);
                                            stateManager.open();
                                        });
                                        saveStatesButton.innerHTML = '<img src="/images/SaveStates.png" class="savedstateicon" />';
                                    }
                                    launchButton = '<a href="/index.html?page=emulator&engine=' + this.Platform.emulatorConfiguration.emulatorType + '&core=' + this.Platform.emulatorConfiguration.core + '&platformid=' + gameRomItems[i].platformId + '&gameid=' + gameId + '&romid=' + gameRomItems[i].id + '&mediagroup=0&rompath=' + romPath + '" class="romstart">Launch</a>';
                                }
                            }
                        }

                        let romInfoButton = document.createElement('div');
                        romInfoButton.className = 'properties_button';
                        //romInfoButton.setAttribute('onclick', 'showDialog(\'rominfo\', ' + gameRomItems[i].id + ');');
                        romInfoButton.setAttribute('data-romid', gameRomItems[i].id);
                        romInfoButton.addEventListener('click', function () {
                            const romInfoDialog = new rominfodialog(gameId, this.getAttribute('data-romid'));
                            romInfoDialog.open();
                        });
                        romInfoButton.innerHTML = 'i';

                        let romCheckbox = document.createElement('input');
                        romCheckbox.type = 'checkbox';
                        romCheckbox.name = 'rom_checkbox';
                        romCheckbox.setAttribute('data-gameid', gameData.id);
                        romCheckbox.setAttribute('data-platformid', gameRomItems[i].platformId);
                        romCheckbox.setAttribute('data-romid', gameRomItems[i].id);
                        romCheckbox.addEventListener('click', () => {
                            this.#handleChecks();
                        });

                        let favouriteRom = document.createElement('img');
                        favouriteRom.className = 'banner_button_image';
                        favouriteRom.title = 'Favourite';
                        favouriteRom.alt = 'Favourite';
                        favouriteRom.setAttribute('name', 'favourite_rom_button');
                        favouriteRom.style.cursor = 'pointer';
                        if (romItem.romUserFavourite == true) {
                            favouriteRom.src = '/images/favourite-filled.svg';
                        } else {
                            favouriteRom.src = '/images/favourite-empty.svg';
                        }
                        favouriteRom.addEventListener('click', async () => {
                            await fetch('/api/v1.1/Games/' + gameId + '/roms/' + gameRomItems[i].id + '/' + gameRomItems[i].platformId + '/favourite?IsMediaGroup=false&favourite=true', {
                                method: 'POST',
                                headers: {
                                    'Content-Type': 'application/json'
                                }
                            }).then(async response => {
                                if (response.ok) {
                                    this.romsModal.modalElement.querySelectorAll('[name="favourite_rom_button"]').forEach((element) => {
                                        element.src = '/images/favourite-empty.svg';
                                    });
                                    favouriteRom.src = '/images/favourite-filled.svg';
                                }
                            });
                        });

                        let recentlyRun = document.createElement('img');
                        recentlyRun.src = '/images/recent.svg';
                        recentlyRun.className = 'banner_button_image';
                        recentlyRun.title = 'Recently Played';
                        recentlyRun.alt = 'Recently Played';
                        if (romItem.romUserLastUsed == true) {
                            recentlyRun.style.display = '';
                        } else {
                            recentlyRun.style.display = 'none';
                        }

                        let romLink = document.createElement('a');
                        romLink.href = '/api/v1.1/Games/' + gameId + '/roms/' + gameRomItems[i].id + '/' + encodeURIComponent(gameRomItems[i].name);
                        romLink.className = 'romlink';
                        romLink.innerHTML = gameRomItems[i].name;

                        let newRow = [
                            [
                                romCheckbox,
                                'rom_checkbox_box_hidden',
                                'rom_edit_checkbox'
                            ],
                            favouriteRom,
                            recentlyRun,
                            romLink,
                            formatBytes(gameRomItems[i].size, 2),
                            gameRomItems[i].romTypeMedia,
                            gameRomItems[i].mediaLabel,
                            saveStatesButton,
                            launchButton,
                            romInfoButton
                        ];
                        newTable.appendChild(createTableRow(false, newRow, 'romrow romrowgamepage', 'romcell'));
                    }

                    gameRoms.appendChild(newTable);

                    if (displayCheckboxes == true) {
                        this.#DisplayROMCheckboxes(true);
                    }

                    if (result.count > pageSize) {
                        // draw pagination
                        let numOfPages = Math.ceil(result.count / pageSize);

                        let romPaginator = document.createElement('div');
                        romPaginator.id = 'romPaginator';
                        romPaginator.className = 'rom_pager';

                        // draw previous page button
                        let prevPage = document.createElement('span');
                        prevPage.className = 'rom_pager_number_disabled';
                        prevPage.innerHTML = '&lt;';
                        if (pageNumber != 1) {
                            prevPage.setAttribute('onclick', 'loadRoms(' + undefined + ', ' + (pageNumber - 1) + ', ' + selectedPlatform + ');');
                            prevPage.className = 'rom_pager_number';
                        }
                        romPaginator.appendChild(prevPage);

                        // draw page numbers
                        for (let i = 0; i < numOfPages; i++) {
                            let romPaginatorPage = document.createElement('span');
                            romPaginatorPage.className = 'rom_pager_number_disabled';
                            romPaginatorPage.innerHTML = (i + 1);
                            if ((i + 1) != pageNumber) {
                                romPaginatorPage.setAttribute('onclick', 'loadRoms(' + undefined + ', ' + (i + 1) + ', ' + selectedPlatform + ');');
                                romPaginatorPage.className = 'rom_pager_number';
                            }

                            romPaginator.appendChild(romPaginatorPage);
                        }

                        // draw next page button
                        let nextPage = document.createElement('span');
                        nextPage.className = 'rom_pager_number_disabled';
                        nextPage.innerHTML = '&gt;';
                        if (pageNumber != numOfPages) {
                            nextPage.setAttribute('onclick', 'loadRoms(' + undefined + ', ' + (pageNumber + 1) + ', ' + selectedPlatform + ');');
                            nextPage.className = 'rom_pager_number';
                        }
                        romPaginator.appendChild(nextPage);

                        gameRoms.appendChild(romPaginator);

                        gameRomsSection.appendChild(gameRoms);
                    }
                } else {
                    gameRomsSection.setAttribute('style', 'display: none;');
                }
            } else if (response.status === 404) {
                console.log('no roms found');
                this.RomCount = 0;
            } else {
                return Promise.reject('some other error: ' + response.status)
            }
        });

        if (this.RomCount == 0) {
            console.log('no roms found');
            this.Roms.style.display = 'none';
            this.RomsNone.style.display = '';
        }
    }

    #DisplayROMCheckboxes(visible) {
        if (visible) {
            this.RomsEditChecksVisible = visible;
        }

        let checkbox_boxes = this.romsModal.modalElement.querySelectorAll('[name="rom_edit_checkbox"]');

        for (let i = 0; i < checkbox_boxes.length; i++) {
            if (this.RomsEditChecksVisible == true) {
                checkbox_boxes[i].className = 'rom_checkbox_box';
            } else {
                checkbox_boxes[i].className = 'rom_checkbox_box_hidden';
            }
        }

        if (this.RomsEditChecksVisible == true) {
            this.RomsEditButton.innerHTML = 'Cancel';
            this.RomsEditPanel.style.display = '';
        } else {
            this.RomsEditButton.innerHTML = 'Edit';
            this.romsModal.modalElement.querySelector('#rom_mastercheck').checked = false;
            this.RomsEditPanel.style.display = 'none';
            this.#selectAllChecks(false);
        }
        this.RomsEditChecksVisible = !this.RomsEditChecksVisible;
    }

    #selectAllChecks(value) {
        let mastercheckbox = this.romsModal.modalElement.querySelector('#rom_mastercheck');
        let checkboxes = this.romsModal.modalElement.querySelectorAll('[name="rom_checkbox"]');
        for (let i = 0; i < checkboxes.length; i++) {
            if (value) {
                checkboxes[i].checked = value;
            } else {
                checkboxes[i].checked = mastercheckbox.checked;
            }
        }
    }

    #handleChecks() {
        let masterCheck = this.romsModal.modalElement.querySelector('#rom_mastercheck');

        let checkboxes = this.romsModal.modalElement.querySelectorAll('[name="rom_checkbox"]');

        let firstPlatformId = undefined;
        let includesDifferentPlatforms = false;
        let checkCount = 0;
        for (let i = 0; i < checkboxes.length; i++) {
            if (checkboxes[i].checked == true) {
                checkCount += 1;
                if (firstPlatformId == undefined) {
                    // set our comparison platform
                    firstPlatformId = checkboxes[i].getAttribute('data-platformid');
                } else if (firstPlatformId != checkboxes[i].getAttribute('data-platformid')) {
                    includesDifferentPlatforms = true;
                }
            }
        }

        if (checkCount == checkboxes.length) {
            masterCheck.checked = true;
        } else {
            masterCheck.checked = false;
        }

        if (firstPlatformId == undefined) {
            includesDifferentPlatforms = true;
        }

        if (checkCount < 2) {
            includesDifferentPlatforms = true;
        }

        let creategroupButton = this.romsModal.modalElement.querySelector('#rom_edit_creategroup');
        if (includesDifferentPlatforms == false) {
            creategroupButton.removeAttribute('disabled');
        } else {
            creategroupButton.setAttribute('disabled', 'disabled');
        }
    }

    #deleteGameRoms() {
        let rom_checks = this.romsModal.modalElement.querySelectorAll('[name="rom_checkbox"]');
        let itemsChecked = false;
        for (let i = 0; i < rom_checks.length; i++) {
            if (rom_checks[i].checked == true) {
                itemsChecked = true;
                break;
            }
        }
        if (itemsChecked == true) {
            const deleteWindow = new MessageBox("Delete Selected ROMs", "Are you sure you want to delete the selected ROMs and any associated save states?");
            let parentObject = this;

            let deleteButton = new ModalButton("Delete", 2, deleteWindow, function (callingObject) {
                parentObject.#deleteGameRomsCallback();
                callingObject.msgDialog.close();
            });
            deleteWindow.addButton(deleteButton);

            let cancelButton = new ModalButton("Cancel", 0, deleteWindow, function (callingObject) {
                callingObject.msgDialog.close();
            });
            deleteWindow.addButton(cancelButton);

            deleteWindow.open();
        }
    }

    #deleteGameRomsCallback() {
        let rom_checks = this.romsModal.modalElement.querySelectorAll('[name="rom_checkbox"]');
        for (let i = 0; i < rom_checks.length; i++) {
            if (rom_checks[i].checked == true) {
                let romId = rom_checks[i].getAttribute('data-romid');
                remapCallCounter += 1;
                let deletePath = '/api/v1.1/Games/' + gameId + '/roms/' + romId;
                let parentObject = this;
                ajaxCall(deletePath, 'DELETE', function (result) {
                    parentObject.#remapTitlesCallback();
                });
            }
        }
    }

    #remapTitles() {
        let fixplatform = $('#rom_edit_fixplatform').select2('data');
        let fixgame = $('#rom_edit_fixgame').select2('data');

        if (fixplatform[0] && fixgame[0]) {
            let rom_checks = document.getElementsByName('rom_checkbox');

            for (let i = 0; i < rom_checks.length; i++) {
                if (rom_checks[i].checked == true) {
                    remapCallCounterMax += 1;
                }
            }

            if (remapCallCounterMax > 0) {
                this.#showProgress();
                let thisObject = this;
                for (let i = 0; i < rom_checks.length; i++) {
                    if (rom_checks[i].checked == true) {
                        let romId = rom_checks[i].getAttribute('data-romid');
                        remapCallCounter += 1;
                        ajaxCall('/api/v1.1/Games/' + gameId + '/roms/' + romId + '?NewPlatformId=' + fixplatform[0].id + '&NewGameId=' + fixgame[0].id, 'PATCH', function (result) {
                            thisObject.#remapTitlesCallback();
                        }, function (result) {
                            thisObject.#remapTitlesCallback();
                        });
                    }
                }
            }
        }
    }

    #remapTitlesCallback() {
        remapCallCounter = remapCallCounter - 1;

        if (remapCallCounter <= 0) {
            this.#closeProgress();
            this.#loadRoms(true);
            remapCallCounter = 0;
            remapCallCounterMax = 0;
        }
    }

    #showProgress() {
        // Get the modal
        let submodal = document.getElementById("myModalProgress");

        // When the user clicks on the button, open the modal 
        submodal.style.display = "block";
    }

    #closeProgress() {
        // Get the modal
        let submodal = document.getElementById("myModalProgress");

        submodal.style.display = "none";
    }

    #createMgGroup() {
        let checkboxes = this.romsModal.modalElement.querySelectorAll('[name="rom_checkbox"]');

        let platformId = undefined;
        let romIds = [];
        for (let i = 0; i < checkboxes.length; i++) {
            if (checkboxes[i].checked == true) {
                if (platformId == undefined) {
                    platformId = checkboxes[i].getAttribute('data-platformid');
                }
                romIds.push(checkboxes[i].getAttribute('data-romid'));
            }
        }

        let currentObject = this;

        ajaxCall(
            '/api/v1.1/Games/' + gameId + '/romgroup?PlatformId=' + platformId,
            'POST',
            function (result) {
                currentObject.#DisplayROMCheckboxes(false);
                currentObject.#loadRoms();
                currentObject.#loadMediaGroups();
            },
            function (error) {
                currentObject.#DisplayROMCheckboxes(false);
                currentObject.#loadRoms();
                currentObject.#loadMediaGroups();
            },
            JSON.stringify(romIds)
        );
    }

    #SetupFixPlatformDropDown() {
        $(this.RomsFixPlatformDropdown).select2({
            minimumInputLength: 3,
            placeholder: "Platform",
            ajax: {
                url: '/api/v1.1/Search/Platform',
                data: function (params) {
                    let query = {
                        SearchString: params.term
                    }

                    // Query parameters will be ?SearchString=[term]
                    return query;
                },
                processResults: function (data) {
                    let arr = [];

                    for (let i = 0; i < data.length; i++) {
                        arr.push({
                            id: data[i].id,
                            text: data[i].name
                        });
                    }

                    return {
                        results: arr
                    };

                }
            }
        });

        $(this.RomsFixPlatformDropdown).on('select2:select', function (e) {
            let platformData = e.params.data;

            let gameValue = $(this.RomsFixGameDropdown).select2('data');
            if (gameValue) {
                this.#setRomFixGameDropDown();
            }
        });

        this.#setRomFixGameDropDown();
    }

    #setRomFixGameDropDown() {
        let thisObject = this;
        $(this.RomsFixGameDropdown).empty().select2({
            minimumInputLength: 3,
            templateResult: DropDownRenderGameOption,
            placeholder: "Game",
            ajax: {
                url: '/api/v1.1/Search/Game',
                data: function (params) {
                    let fixplatform = $(thisObject.RomsFixPlatformDropdown).select2('data');

                    let query = {
                        PlatformId: fixplatform[0].id,
                        SearchString: params.term
                    }

                    // Query parameters will be ?SearchString=[term]
                    return query;
                },
                processResults: function (data) {
                    let arr = [];

                    for (let i = 0; i < data.length; i++) {
                        arr.push({
                            id: data[i].id,
                            text: data[i].name,
                            cover: data[i].cover,
                            releaseDate: data[i].first_release_date
                        });
                    }

                    return {
                        results: arr
                    };
                }
            }
        });
    }
}

function loadArtwork(game, cover) {
    let URLList = [];

    // default background should be the artworks
    if (game.artworks) {
        for (let i = 0; i < game.artworks.length; i++) {
            URLList.push("/api/v1.1/Games/" + gameId + "/artwork/" + game.artworks[i] + "/image/original/" + game.artworks[i] + ".jpg");
        }
    } else if (game.cover) {
        // backup background is the cover artwork
        URLList.push("/api/v1.1/Games/" + gameId + "/cover/" + cover.id + "/image/original/" + cover.imageId + ".jpg");
    } else {
        // backup background is a random image
        var randomInt = randomIntFromInterval(1, 3);
        URLList.push("/images/gamebg" + randomInt + ".jpg");
    }

    // give the list of URL's to the background image rotator
    backgroundImageHandler = new BackgroundImageRotator(URLList);
}

function selectScreenshot(index) {
    var gameScreenshots_Main = document.getElementById('gamescreenshots_main');
    var gameScreenshots_Selected = document.getElementById('gamescreenshots_gallery_' + index);
    var gameScreenshots_Items = document.getElementsByName('gamescreenshots_gallery_item');

    // set the selction class
    for (var i = 0; i < gameScreenshots_Items.length; i++) {
        if (gameScreenshots_Items[i].id == gameScreenshots_Selected.id) {
            gameScreenshots_Items[i].classList.add('gamescreenshosts_gallery_item_selected');
            gameScreenshots_Selected.scrollIntoView({ behavior: "smooth", block: "end", inline: "nearest" });
        } else {
            gameScreenshots_Items[i].classList.remove('gamescreenshosts_gallery_item_selected');
        }
    }

    // set the screenshot
    gameScreenshots_Main.setAttribute('style', '');
    gameScreenshots_Main.innerHTML = '';
    switch (gameScreenshots_Selected.getAttribute('imagetype')) {
        case "0":
        default:
            // screenshot
            // gameScreenshots_Main.setAttribute('style', gameScreenshots_Selected.getAttribute('style').replace("/image/screenshot_thumb", "/image/original"));

            var imageTag = document.createElement('img');
            imageTag.setAttribute('height', '290');
            imageTag.setAttribute('width', '515');
            imageTag.setAttribute('src', encodeURI(gameScreenshots_Selected.getAttribute('data-url').replace("/image/screenshot_thumb", "/image/original")));

            gameScreenshots_Main.appendChild(imageTag);

            break;
        case "1":
            // video
            gameScreenshots_Main.setAttribute('style', '');

            var videoIFrame = document.createElement('iframe');
            videoIFrame.setAttribute('height', '290');
            videoIFrame.setAttribute('width', '515');
            videoIFrame.setAttribute('frameBorder', '0');
            videoIFrame.setAttribute('src', encodeURI('https://www.youtube.com/embed/' + gameScreenshots_Selected.getAttribute('imageref') + '?autoplay=1&mute=1'));

            gameScreenshots_Main.appendChild(videoIFrame);

            break;
    }

    selectedScreenshot = index;
}

function selectScreenshot_Next() {
    var gameScreenshots_Items = document.getElementsByName('gamescreenshots_gallery_item');

    selectedScreenshot += 1;

    if (selectedScreenshot >= gameScreenshots_Items.length) {
        selectedScreenshot = 0;
    }

    selectScreenshot(selectedScreenshot);
}

function selectScreenshot_Prev() {
    var gameScreenshots_Items = document.getElementsByName('gamescreenshots_gallery_item');

    selectedScreenshot = selectedScreenshot - 1;

    if (selectedScreenshot < 0) {
        selectedScreenshot = gameScreenshots_Items.length - 1;
    }

    selectScreenshot(selectedScreenshot);
}

function ShowCollectionDialog(platformId) {
    modalVariables = platformId;
    showSubDialog("collectionaddgame");
}

function SetGameFavourite(status) {
    ajaxCall(
        '/api/v1.1/Games/' + gameId + '/favourite?favourite=' + status,
        'POST',
        function (result) {
            var gameFavButton = document.getElementById('gamestatistics_favourite_button');
            var gameFavIcon = document.createElement('img');
            gameFavIcon.id = "gamestatistics_favourite";
            gameFavIcon.className = "favouriteicon";
            gameFavIcon.title = "Favourite";
            gameFavIcon.alt = "Favourite";

            if (result == true) {
                gameFavIcon.setAttribute("src", '/images/favourite-filled.svg');
                gameFavIcon.setAttribute('onclick', "SetGameFavourite(false);");
            } else {
                gameFavIcon.setAttribute("src", '/images/favourite-empty.svg');
                gameFavIcon.setAttribute('onclick', "SetGameFavourite(true);");
            }

            gameFavButton.innerHTML = '';
            gameFavButton.appendChild(gameFavIcon);
        }
    );
}

SetupPage();