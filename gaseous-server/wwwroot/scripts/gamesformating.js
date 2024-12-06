var ClassificationBoards = {
    "ESRB": "Entertainment Software Rating Board (ESRB)",
    "PEGI": "Pan European Game Information (PEGI)",
    "CERO": "Computer Entertainment Rating Organisation (CERO)",
    "USK": "Unterhaltungssoftware Selbstkontrolle (USK)",
    "GRAC": "Game Rating and Administration Committee (GRAC)",
    "CLASS_IND": "Brazilian advisory rating system",
    "ACB": "Australian Classification Board (ACB)"
};

var ClassificationRatings = {
    "EC": "Early Childhood",
    "E": "Everyone",
    "E10": "Everyone 10+",
    "T": "Teen",
    "M": "Mature 17+",
    "AO": "Adults Only 18+",
    "RP": "Rating Pending",

    "Three": "PEGI 3",
    "Seven": "PEGI 7",
    "Twelve": "PEGI 12",
    "Sixteen": "PEGI 16",
    "Eighteen": "PEGI 18",

    "CERO_A": "All Ages",
    "CERO_B": "Ages 12 and up",
    "CERO_C": "Ages 15 and up",
    "CERO_D": "Ages 17 and up",
    "CERO_Z": "Ages 18 and up only",

    "USK_0": "Approved without age restriction",
    "USK_6": "Approved for children aged 6 and above",
    "USK_12": "Approved for children aged 12 and above",
    "USK_16": "Approved for children aged 16 and above",
    "USK_18": "Not approved for young persons",

    "GRAC_All": "All",
    "GRAC_Twelve": "12+",
    "GRAC_Fifteen": "15+",
    "GRAC_Eighteen": "18+",
    "GRAC_Testing": "Testing",

    "CLASS_IND_L": "General Audiences",
    "CLASS_IND_Ten": "Not recommended for minors under ten",
    "CLASS_IND_Twelve": "Not recommended for minors under twelve",
    "CLASS_IND_Fourteen": "Not recommended for minors under fourteen",
    "CLASS_IND_Sixteen": "Not recommended for minors under sixteen",
    "CLASS_IND_Eighteen": "Not recommended for minors under eighteen",

    "ACB_G": "General",
    "ACB_PG": "Parental Guidance",
    "ACB_M": "Mature",
    "ACB_MA15": "Mature Accompanied",
    "ACB_R18": "Restricted",
    "ACB_RC": "Refused Classification"
};

var pageReloadInterval;
var firstLoad = true;

function formatGamesPanel(targetElement, result, pageNumber, pageSize, forceScrollTop) {
    // set page mode buttons
    let pageViewButton = document.getElementById('games_library_button_pagedview');
    let infiniteViewButton = document.getElementById('games_library_button_infiniteview');
    let pageMode = GetPreference('LibraryPagination', 'infinite');
    switch (pageMode) {
        case 'paged':
            pageViewButton.classList.add('games_library_button_selected');
            infiniteViewButton.classList.remove('games_library_button_selected');
            break;

        case 'infinite':
            pageViewButton.classList.remove('games_library_button_selected');
            infiniteViewButton.classList.add('games_library_button_selected');
            break;
    }

    // set view mode buttons
    let listViewButton = document.getElementById('games_library_button_listview');
    let iconViewButton = document.getElementById('games_library_button_iconview');
    let listViewRaw = GetPreference('LibraryListView', 'false');
    let listView = false;
    if (listViewRaw == 'true') {
        listView = true;
        listViewButton.classList.add('games_library_button_selected');
        iconViewButton.classList.remove('games_library_button_selected');
    } else {
        listViewButton.classList.remove('games_library_button_selected');
        iconViewButton.classList.add('games_library_button_selected');
    }

    if (pageNumber == 1) {
        localStorage.setItem("gaseous-library-scrollpos", 0);
    }

    if (pageMode == 'paged') {
        targetElement.innerHTML = '';
    }

    switch (pageMode) {
        case 'paged':
            if (forceScrollTop == true) {
                window.scrollTo(0, 0);
            }
            break;
        case 'infinite':
            let gamePlaceholders = document.getElementsByName('GamePlaceholder');

            let currentPage = 1;
            let totalPages = Math.ceil(result.count / Number(pageSize));
            let startIndex = 0;
            let endIndex = 0 + Number(pageSize);
            for (let p = currentPage; p < totalPages + 1; p++) {
                // console.log("Page: " + p + " - StartIndex: " + startIndex + " - EndIndex: " + endIndex);

                let newPageAnchor = document.getElementById('pageAnchor' + p);
                if (!newPageAnchor) {
                    newPageAnchor = document.createElement('span');
                    newPageAnchor.id = 'pageAnchor' + p;
                    newPageAnchor.setAttribute('name', 'pageAnchor' + p);
                    newPageAnchor.className = 'pageAnchor';
                    newPageAnchor.setAttribute('data-page', p);
                    newPageAnchor.setAttribute('data-loaded', "0");
                    targetElement.appendChild(newPageAnchor);
                }

                if (endIndex > result.count) {
                    endIndex = result.count;
                }

                for (let i = startIndex; i < endIndex; i++) {
                    let placeHolderpresent = false;
                    for (let x = 0; x < gamePlaceholders.length; x++) {
                        if (gamePlaceholders[x].getAttribute('data-index') == i) {
                            placeHolderpresent = true;
                        }
                    }
                    if (placeHolderpresent == false) {
                        let gamePlaceholder = document.createElement('div');
                        gamePlaceholder.setAttribute('name', 'GamePlaceholder');
                        gamePlaceholder.id = 'GamePlaceholder' + i;
                        gamePlaceholder.setAttribute('data-index', i);
                        gamePlaceholder.className = 'game_tile';
                        newPageAnchor.appendChild(gamePlaceholder);
                    }
                }

                startIndex = endIndex;
                endIndex = startIndex + Number(pageSize);

                if (startIndex > result.count) {
                    break;
                }
            }

            break;
    }

    document.getElementById('games_library_recordcount').innerHTML = result.count + ' games';

    let existingLoadPageButton = document.getElementById('games_library_loadmore');
    if (existingLoadPageButton) {
        existingLoadPageButton.parentNode.removeChild(existingLoadPageButton);
    }

    // setup preferences
    let showTitle = GetPreference("LibraryShowGameTitle", true);
    let showRatings = GetPreference("LibraryShowGameRating", true);
    let showClassification = GetPreference("LibraryShowGameClassification", true);
    let classificationDisplayOrder = GetRatingsBoards();
    if (showTitle == "true") { showTitle = true; } else { showTitle = false; }
    if (showRatings == "true") { showRatings = true; } else { showRatings = false; }
    if (showClassification == "true") { showClassification = true; } else { showClassification = false; }

    let tileWrapperClass = '';
    if (listView === true) {
        tileWrapperClass = 'game_tile_wrapper_list';
    } else {
        tileWrapperClass = 'game_tile_wrapper_icon';
    }

    for (let i = 0; i < result.games.length; i++) {
        let game = renderGameIcon(result.games[i], showTitle, showRatings, showClassification, classificationDisplayOrder, false, listView, true);
        switch (pageMode) {
            case "paged":
                targetElement.appendChild(game);
                break;

            case "infinite":
                let placeholderElement = document.getElementById('GamePlaceholder' + result.games[i].index);
                if (placeholderElement.className != tileWrapperClass) {
                    placeholderElement.className = tileWrapperClass;
                    placeholderElement.innerHTML = '';
                    placeholderElement.appendChild(game);
                }
                break;

        }

        $(game).fadeIn(500);
    }

    let pager = document.getElementById('games_pager');
    pager.style.display = 'none';

    let alphaPager = document.getElementById('games_library_alpha_pager');
    alphaPager.innerHTML = '';

    switch (pageMode) {
        case 'infinite':
            for (const [key, value] of Object.entries(result.alphaList)) {
                let letterPager = document.createElement('span');
                letterPager.className = 'games_library_alpha_pager_letter';
                letterPager.setAttribute('onclick', 'document.location.hash = "#pageAnchor' + (value) + '"; executeFilter1_1(' + (value) + ');');
                letterPager.innerHTML = key;
                alphaPager.appendChild(letterPager);
            }

            if (firstLoad == true) {
                if (localStorage.getItem("gaseous-library-scrollpos") != null) {
                    $(window).scrollTop(localStorage.getItem("gaseous-library-scrollpos"));
                }
                firstLoad = false;
            }

            IsInView();

            break;

        case 'paged':
            for (const [key, value] of Object.entries(result.alphaList)) {
                let letterPager = document.createElement('span');
                letterPager.className = 'games_library_alpha_pager_letter';
                letterPager.setAttribute('onclick', 'executeFilter1_1(' + value + ');');
                letterPager.innerHTML = key;
                alphaPager.appendChild(letterPager);
            }

            if (result.count > pageSize) {
                let pageCount = Math.ceil(result.count / pageSize);

                // add first page button
                let firstPage = document.createElement('span');
                firstPage.innerHTML = '&#124;&lt;';
                if (pageNumber == 1) {
                    firstPage.className = 'games_pager_number_disabled';
                } else {
                    firstPage.className = 'games_pager_number';
                    firstPage.setAttribute('onclick', 'executeFilter1_1(1);');
                }

                // add previous page button
                let prevPage = document.createElement('span');
                prevPage.innerHTML = '&lt;';
                if (pageNumber == 1) {
                    prevPage.className = 'games_pager_number_disabled';
                } else {
                    prevPage.className = 'games_pager_number';
                    prevPage.setAttribute('onclick', 'executeFilter1_1(' + (pageNumber - 1) + ');');
                }

                // add page numbers
                let pageEitherSide = 4;
                // let currentPage = Number(pagerCheck.innerHTML);
                let currentPage = pageNumber;
                let pageNumbers = document.createElement('span');
                for (let i = 1; i <= pageCount; i++) {
                    if (
                        (
                            (i >= currentPage - pageEitherSide) &&
                            (i <= currentPage + pageEitherSide)
                        ) ||
                        (
                            (
                                i <= (pageEitherSide * 2 + 1) &&
                                currentPage <= (pageEitherSide)
                            ) ||
                            (
                                i >= (pageCount - (pageEitherSide * 2)) &&
                                currentPage >= (pageCount - (pageEitherSide))
                            )
                        )
                    ) {
                        let pageNum = document.createElement('span');
                        if (pageNumber == i) {
                            pageNum.className = 'games_pager_number_disabled';
                        } else {
                            pageNum.className = 'games_pager_number';
                            pageNum.setAttribute('onclick', 'executeFilter1_1(' + i + ');');
                        }
                        pageNum.innerHTML = i;
                        pageNumbers.appendChild(pageNum);
                    }
                }

                // add next page button
                let nextPage = document.createElement('span');
                nextPage.innerHTML = '&gt;';
                if (pageNumber == pageCount) {
                    nextPage.className = 'games_pager_number_disabled';
                } else {
                    nextPage.className = 'games_pager_number';
                    nextPage.setAttribute('onclick', 'executeFilter1_1(' + (pageNumber + 1) + ');');
                }

                // add last page button
                let lastPage = document.createElement('span');
                lastPage.innerHTML = '&gt;&#124;';
                if (pageNumber == pageCount) {
                    lastPage.className = 'games_pager_number_disabled';
                } else {
                    lastPage.className = 'games_pager_number';
                    lastPage.setAttribute('onclick', 'executeFilter1_1(' + pageCount + ');');
                }

                pager.innerHTML = '';
                pager.appendChild(firstPage);
                pager.appendChild(prevPage);
                pager.appendChild(pageNumbers);
                pager.appendChild(nextPage);
                pager.appendChild(lastPage);

                pager.style.display = '';
            }
            break;
    }

    $('.lazy').Lazy({
        effect: 'show',
        effectTime: 500,
        visibleOnly: true,
        defaultImage: '/images/unknowngame.png',
        delay: 250,
        enableThrottle: true,
        throttle: 250,
        afterLoad: function (element) {
            //console.log(element[0].getAttribute('data-id'));
        }
    });
}

function isScrolledIntoView(elem) {
    if (elem) {
        let docViewTop = $(window).scrollTop();
        let docViewBottom = docViewTop + $(window).height();

        let elemTop = $(elem).offset().top;
        let elemBottom = elemTop + $(elem).height();

        return ((elemBottom <= docViewBottom) && (elemTop >= docViewTop));
    }
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

function IsInView() {
    let pageMode = GetPreference('LibraryPagination', 'infinite');
    switch (pageMode) {
        case "paged":
            let loadElement = document.getElementById('games_library_loadmore');
            if (loadElement) {
                //if (isScrolledIntoView(loadElement)) {
                if (elementIsVisibleInViewport(loadElement, true)) {
                    let pageNumber = Number(document.getElementById('games_library_loadmore').getAttribute('data-pagenumber'));
                    let pageSize = document.getElementById('games_library_loadmore').getAttribute('data-pagesize');
                    executeFilter1_1(pageNumber);
                }
            }
            break;

        case "infinite":
            // store scroll location
            localStorage.setItem("gaseous-library-scrollpos", $(window).scrollTop());

            // load page
            let anchors = document.getElementsByClassName('pageAnchor');
            for (let i = 0; i < anchors.length; i++) {
                //if (isScrolledIntoView(anchors[i])) {
                if (elementIsVisibleInViewport(anchors[i], true)) {
                    if (anchors[i].getAttribute('data-loaded') == "0") {
                        document.getElementById(anchors[i].id).setAttribute('data-loaded', "1");
                        executeFilter1_1(Number(anchors[i].getAttribute('data-page')));
                    }
                }
            }
            break;
    }
}

function renderGameIcon(gameObject, showTitle, showRatings, showClassification, classificationDisplayOrder, useSmallCover, listView, showFavourite) {
    if (listView == undefined) {
        listView = false;
    }

    if (showFavourite == undefined) {
        showFavourite = true;
    }

    console.log(gameObject);

    let classes = getViewModeClasses(listView);

    let gameBox = document.createElement('div');
    gameBox.metadataMapId = "game_tile_" + gameObject.metadataMapId;
    if (useSmallCover == true) {
        gameBox.classList.add(...classes['game_tile game_tile_small']);
    } else {
        gameBox.classList.add(...classes['game_tile']);
    }
    // gameBox.style.display = 'none';

    let gameImageBox = document.createElement('div');
    gameImageBox.classList.add(...classes['game_tile_box']);
    if (listView == true) {
        gameBox.setAttribute('onclick', 'window.location.href = "/index.html?page=game&id=' + gameObject.metadataMapId + '";');
    } else {
        gameImageBox.setAttribute('onclick', 'window.location.href = "/index.html?page=game&id=' + gameObject.metadataMapId + '";');
    }

    let gameImage = document.createElement('img');
    gameImage.id = 'game_tile_cover_' + gameObject.metadataMapId;
    gameImage.setAttribute('data-id', gameObject.metadataMapId);
    if (useSmallCover == true) {
        gameImage.classList.add(...classes['game_tile_image game_tile_image_small lazy']);
    } else {
        gameImage.classList.add(...classes['game_tile_image lazy']);
    }
    // gameImage.src = '/images/unknowngame.png';
    if (gameObject.cover) {
        gameImage.setAttribute('data-src', '/api/v1.1/Games/' + gameObject.metadataMapId + '/cover/' + gameObject.cover + '/image/cover_big/' + gameObject.cover + '.jpg');
    } else {
        gameImage.classList.add(...classes['game_tile_image unknown']);
        gameImage.setAttribute('data-src', '/images/unknowngame.png');
    }
    gameImageBox.appendChild(gameImage);

    let classificationPath = '';
    let displayClassification = false;
    let shownClassificationBoard = '';
    if (showClassification == true) {
        for (let b = 0; b < classificationDisplayOrder.length; b++) {
            if (shownClassificationBoard == '') {
                for (let c = 0; c < gameObject.ageRatings.length; c++) {
                    if (gameObject.ageRatings[c].category == classificationDisplayOrder[b]) {
                        shownClassificationBoard = classificationDisplayOrder[b];
                        displayClassification = true;
                        classificationPath = '/images/Ratings/' + classificationDisplayOrder[b] + '/' + gameObject.ageRatings[c].rating + '.svg';
                    }
                }
            } else {
                break;
            }
        }
    }

    // add save game icon
    if (gameObject.hasSavedGame == true) {
        let gameSaveIcon = document.createElement('img');
        gameSaveIcon.src = '/images/SaveStates.png';
        gameSaveIcon.classList.add(...classes['game_tile_box_savedgame savedstateicon']);
        gameImageBox.appendChild(gameSaveIcon);
    }

    // add ratings banner
    if (gameObject.totalRating || displayClassification == true) {
        let gameImageRatingBanner = document.createElement('div');
        gameImageRatingBanner.classList.add(...classes['game_tile_box_ratingbanner']);

        if (showRatings == true || displayClassification == true) {
            if (showRatings == true) {
                if (gameObject.totalRating) {
                    let gameImageRatingBannerLogo = document.createElement('img');
                    gameImageRatingBannerLogo.src = '/images/IGDB_logo.svg';
                    gameImageRatingBannerLogo.setAttribute('style', 'filter: invert(100%); height: 10px; margin-right: 5px; padding-top: 4px;');
                    gameImageRatingBanner.appendChild(gameImageRatingBannerLogo);

                    let gameImageRatingBannerValue = document.createElement('span');
                    gameImageRatingBannerValue.innerHTML = Math.floor(gameObject.totalRating) + '% / ' + gameObject.totalRatingCount;
                    gameImageRatingBanner.appendChild(gameImageRatingBannerValue);
                }
            }

            gameImageBox.appendChild(gameImageRatingBanner);

            if (displayClassification == true) {
                let gameImageClassificationLogo = document.createElement('img');
                gameImageClassificationLogo.src = classificationPath;
                gameImageClassificationLogo.classList.add(...classes['rating_image_overlay']);
                gameImageBox.appendChild(gameImageClassificationLogo);
            }
        }
    }
    gameBox.appendChild(gameImageBox);

    // add favourite game icon
    if (showFavourite == true) {
        let gameFavIconBox = document.createElement('div');
        gameFavIconBox.classList.add(...classes['game_tile_box_favouritegame']);

        let gameFavIcon = document.createElement('img');
        gameFavIcon.classList.add(...classes['favouriteicon']);
        if (gameObject.isFavourite == true) {
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
                gameObject.isFavourite = false;
            } else {
                gameFavIcon.src = '/images/favourite-filled.svg';
                gameFavIconBox.classList.remove('favourite-empty');
                gameFavIconBox.classList.add('favourite-filled');
                gameObject.isFavourite = true;
            }

            fetch('/api/v1.1/Games/' + gameObject.metaDataMapId + '/favourite?favourite=' + gameObject.isFavourite, {
                method: 'POST'
            }).then(response => {
                if (response.ok) {
                    // console.log('Favourite status updated');
                } else {
                    // console.log('Failed to update favourite status');
                }
            });
        });

        gameImageBox.appendChild(gameFavIconBox);
    }

    if (showTitle == true) {
        let gameBoxTitle = document.createElement('div');
        gameBoxTitle.classList.add(...classes['game_tile_label']);
        gameBoxTitle.innerHTML = gameObject.name;
        gameBox.appendChild(gameBoxTitle);

        if (listView == true) {
            if (gameObject.summary) {
                let gameBoxSummary = document.createElement('div');
                gameBoxSummary.classList.add(...classes['game_tile_summary']);
                gameBoxSummary.innerHTML = gameObject.summary;
                gameBox.appendChild(gameBoxSummary);
            }
        }
    }

    return gameBox;
}

function getViewModeClasses(listView) {
    if (listView == false) {
        return {
            "game_tile game_tile_small": ["game_tile", "game_tile_small"],
            "game_tile": ["game_tile"],
            "game_tile_box": ["game_tile_box"],
            "game_tile_image game_tile_image_small lazy": ["game_tile_image", "game_tile_image_small", "lazy"],
            "game_tile_image lazy": ["game_tile_image", "lazy"],
            "game_tile_image unknown": ["game_tile_image", "unknown"],
            "game_tile_box_savedgame savedstateicon": ["game_tile_box_savedgame", "savedstateicon"],
            "game_tile_box_favouritegame favouriteicon": ["game_tile_box_favouritegame", "favouriteicon"],
            "game_tile_box_favouritegame": ["game_tile_box_favouritegame"],
            "favouriteicon": ["favouriteicon"],
            "game_tile_box_ratingbanner": ["game_tile_box_ratingbanner"],
            "rating_image_overlay": ["rating_image_overlay"],
            "game_tile_label": ["game_tile_label"],
            "game_tile_summary": ["game_tile_summary"]
        };
    } else {
        return {
            "game_tile game_tile_small": ["game_tile_row", "game_tile_small"],
            "game_tile": ["game_tile_row"],
            "game_tile_box": ["game_tile_box_row"],
            "game_tile_image game_tile_image_small lazy": ["game_tile_image_row", "game_tile_image_small", "lazy"],
            "game_tile_image lazy": ["game_tile_image_row", "lazy"],
            "game_tile_image unknown": ["game_tile_image_row", "unknown"],
            "game_tile_box_savedgame savedstateicon": ["game_tile_box_savedgame_row", "savedstateicon"],
            "game_tile_box_favouritegame favouriteicon": ["game_tile_box_favouritegame_row", "favouriteicon"],
            "game_tile_box_favouritegame": ["game_tile_box_favouritegame_row"],
            "favouriteicon": ["favouriteicon"],
            "game_tile_box_ratingbanner": ["game_tile_box_ratingbanner_row"],
            "rating_image_overlay": ["rating_image_overlay_row"],
            "game_tile_label": ["game_tile_label_row"],
            "game_tile_summary": ["game_tile_summary_row"]
        };
    }
}