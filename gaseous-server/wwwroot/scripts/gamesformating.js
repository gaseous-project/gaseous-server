var ClassificationBoards = {
    "ESRB":      "Entertainment Software Rating Board (ESRB)",
    "PEGI":      "Pan European Game Information (PEGI)",
    "CERO":      "Computer Entertainment Rating Organisation (CERO)",
    "USK":       "Unterhaltungssoftware Selbstkontrolle (USK)",
    "GRAC":      "Game Rating and Administration Committee (GRAC)",
    "CLASS_IND": "Brazilian advisory rating system",
    "ACB":       "Australian Classification Board (ACB)"
};

var ClassificationRatings = {
    "E":         "Everyone",
    "E10":       "Everyone 10+",
    "T":         "Teen",
    "M":         "Mature 17+",
    "AO":        "Adults Only 18+",
    "RP":        "Rating Pending",

    "Three":     "PEGI 3",
    "Seven":     "PEGI 7",
    "Twelve":    "PEGI 12",
    "Sixteen":   "PEGI 16",
    "Eighteen":  "PEGI 18",
    
    "CERO_A":    "All Ages",
    "CERO_B":    "Ages 12 and up",
    "CERO_C":    "Ages 15 and up",
    "CERO_D":    "Ages 17 and up",
    "CERO_Z":    "Ages 18 and up only",

    "USK_0":     "Approved without age restriction",
    "USK_6":     "Approved for children aged 6 and above",
    "USK_12":    "Approved for children aged 12 and above",
    "USK_16":    "Approved for children aged 16 and above",
    "USK_18":    "Not approved for young persons",

    "GRAC_All":  "All",
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

    "ACB_G":     "General",
    "ACB_PG":    "Parental Guidance",
    "ACB_M":     "Mature",
    "ACB_MA15":  "Mature Accompanied",
    "ACB_R18":   "Restricted",
    "ACB_RC":    "Refused Classification"
};

var pageReloadInterval;
var firstLoad = true;

function formatGamesPanel(targetElement, result, pageNumber, pageSize, forceScrollTop) {
    var pageMode = GetPreference('LibraryPagination', 'paged');

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
            var gamePlaceholders = document.getElementsByName('GamePlaceholder');

            let currentPage = 1;
            let totalPages = Math.ceil(result.count / pageSize);
            let startIndex = 0;
            let endIndex = pageSize;
            for (let p = currentPage; p < totalPages + 1; p++) {
                //console.log("Page: " + p + " - StartIndex: " + startIndex + " - EndIndex: " + endIndex);

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
                    var placeHolderpresent = false;
                    for (var x = 0; x < gamePlaceholders.length; x++) {
                        if (gamePlaceholders[x].getAttribute('data-index') == i) {
                            placeHolderpresent = true;
                        }
                    }
                    if (placeHolderpresent == false) {
                        var gamePlaceholder = document.createElement('div');
                        gamePlaceholder.setAttribute('name', 'GamePlaceholder');
                        gamePlaceholder.id = 'GamePlaceholder' + i;
                        gamePlaceholder.setAttribute('data-index', i);
                        gamePlaceholder.className = 'game_tile';
                        newPageAnchor.appendChild(gamePlaceholder);
                    }
                }

                startIndex = endIndex;
                endIndex = startIndex + pageSize;

                if (startIndex > result.count) {
                    break;
                }
            }

            break;
    }

    document.getElementById('games_library_recordcount').innerHTML = result.count + ' games';

    var existingLoadPageButton = document.getElementById('games_library_loadmore');
    if (existingLoadPageButton) {
        existingLoadPageButton.parentNode.removeChild(existingLoadPageButton);
    }

    // setup preferences
    var showTitle = GetPreference("LibraryShowGameTitle", true);
    var showRatings = GetPreference("LibraryShowGameRating", true);
    var showClassification = GetPreference("LibraryShowGameClassification", true);
    var classificationDisplayOrderString = GetPreference("LibraryGameClassificationDisplayOrder", JSON.stringify([ "ESRB" ]));
    var classificationDisplayOrder = JSON.parse(classificationDisplayOrderString);
    if (showTitle == "true") { showTitle = true; } else { showTitle = false; }
    if (showRatings == "true") { showRatings = true; } else { showRatings = false; }
    if (showClassification == "true") { showClassification = true; } else { showClassification = false; }

    for (var i = 0; i < result.games.length; i++) {
        var game = renderGameIcon(result.games[i], showTitle, showRatings, showClassification, classificationDisplayOrder, false);
        switch (pageMode) {
            case "paged":
                targetElement.appendChild(game);
                break;

            case "infinite":
                var placeholderElement = document.getElementById('GamePlaceholder' + result.games[i].index);
                if (placeholderElement.className != 'game_tile_wrapper') {
                    placeholderElement.className = 'game_tile_wrapper';
                    placeholderElement.innerHTML = '';
                    placeholderElement.appendChild(game);
                }
                break;

        }

        $(game).fadeIn(500);
    }

    var pager = document.getElementById('games_pager');
    pager.style.display = 'none';

    var alphaPager = document.getElementById('games_library_alpha_pager');
    alphaPager.innerHTML = '';

    switch(pageMode) {
        case 'infinite':
            for (const [key, value] of Object.entries(result.alphaList)) {
                var letterPager = document.createElement('span');
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
                var letterPager = document.createElement('span');
                letterPager.className = 'games_library_alpha_pager_letter';
                letterPager.setAttribute('onclick', 'executeFilter1_1(' + value + ');');
                letterPager.innerHTML = key;
                alphaPager.appendChild(letterPager);
            }

            if (result.count > pageSize) {
                var pageCount = Math.ceil(result.count / pageSize);

                // add first page button
                var firstPage = document.createElement('span');
                firstPage.innerHTML = '&#124;&lt;';
                if (pageNumber == 1) {
                    firstPage.className = 'games_pager_number_disabled';
                } else {
                    firstPage.className = 'games_pager_number';
                    firstPage.setAttribute('onclick', 'executeFilter1_1(1);');
                }

                // add previous page button
                var prevPage = document.createElement('span');
                prevPage.innerHTML = '&lt;';
                if (pageNumber == 1) {
                    prevPage.className = 'games_pager_number_disabled';
                } else {
                    prevPage.className = 'games_pager_number';
                    prevPage.setAttribute('onclick', 'executeFilter1_1(' + (pageNumber - 1) + ');');
                }

                // add page numbers
                var pageEitherSide = 4;
                // var currentPage = Number(pagerCheck.innerHTML);
                var currentPage = pageNumber;
                var pageNumbers = document.createElement('span');
                for (var i = 1; i <= pageCount; i++) {
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
                        var pageNum = document.createElement('span');
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
                var nextPage = document.createElement('span');
                nextPage.innerHTML = '&gt;';
                if (pageNumber == pageCount) {
                    nextPage.className = 'games_pager_number_disabled';
                } else {
                    nextPage.className = 'games_pager_number';
                    nextPage.setAttribute('onclick', 'executeFilter1_1(' + (pageNumber + 1) + ');');
                }

                // add last page button
                var lastPage = document.createElement('span');
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
        afterLoad: function(element) {
            //console.log(element[0].getAttribute('data-id'));
        }
    });
}

function isScrolledIntoView(elem) {
    if (elem) {
        var docViewTop = $(window).scrollTop();
        var docViewBottom = docViewTop + $(window).height();

        var elemTop = $(elem).offset().top;
        var elemBottom = elemTop + $(elem).height();

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
    var pageMode = GetPreference('LibraryPagination', 'paged');
    switch (pageMode) {
        case "paged":
            var loadElement = document.getElementById('games_library_loadmore');
            if (loadElement) {
                //if (isScrolledIntoView(loadElement)) {
                if (elementIsVisibleInViewport(loadElement, true)) {
                    var pageNumber = Number(document.getElementById('games_library_loadmore').getAttribute('data-pagenumber'));
                    var pageSize = document.getElementById('games_library_loadmore').getAttribute('data-pagesize');
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
                        console.log("Loading page: " + anchors[i].getAttribute('data-page'));
                        document.getElementById(anchors[i].id).setAttribute('data-loaded', "1");
                        executeFilter1_1(Number(anchors[i].getAttribute('data-page')));
                    }
                }
            }
            break;
    }
}

$(window).scroll(IsInView);

function renderGameIcon(gameObject, showTitle, showRatings, showClassification, classificationDisplayOrder, useSmallCover, listView) {
    if (listView == undefined) {
        listView = false;
    }

    var classes = getViewModeClasses(listView);

    var gameBox = document.createElement('div');
    gameBox.id = "game_tile_" + gameObject.id;
    if (useSmallCover == true) {
        gameBox.classList.add(...classes['game_tile game_tile_small']);
    } else {
        gameBox.classList.add(...classes['game_tile']);
    }
    gameBox.setAttribute('onclick', 'window.location.href = "/index.html?page=game&id=' + gameObject.id + '";');
    gameBox.style.display = 'none';

    var gameImageBox = document.createElement('div');
    gameImageBox.classList.add(...classes['game_tile_box']);

    var gameImage = document.createElement('img');
    gameImage.id = 'game_tile_cover_' + gameObject.id;
    gameImage.setAttribute('data-id', gameObject.id);
    if (useSmallCover == true) {
        gameImage.classList.add(...classes['game_tile_image game_tile_image_small lazy']);
    } else {
        gameImage.classList.add(...classes['game_tile_image lazy']);
    }
    // gameImage.src = '/images/unknowngame.png';
    if (gameObject.cover) {
        gameImage.setAttribute('data-src', '/api/v1.1/Games/' + gameObject.id + '/cover/image/cover_big/' + gameObject.cover.imageId + '.jpg');
    } else {
        gameImage.classList.add(...classes['game_tile_image unknown']);
        gameImage.setAttribute('data-src', '/images/unknowngame.png');
    }
    gameImageBox.appendChild(gameImage);

    var classificationPath = '';
    var displayClassification = false;
    var shownClassificationBoard = '';
    if (showClassification == true) {
        for (var b = 0; b < classificationDisplayOrder.length; b++) {
            if (shownClassificationBoard == '') {
                for (var c = 0; c < gameObject.ageRatings.length; c++) {
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
        var gameSaveIcon = document.createElement('img');
        gameSaveIcon.src = '/images/SaveStates.png';
        gameSaveIcon.classList.add(...classes['game_tile_box_savedgame savedstateicon']);
        gameImageBox.appendChild(gameSaveIcon);
    }

    // add favourite game icon
    if (gameObject.isFavourite == true) {
        var gameFavIcon = document.createElement('img');
        gameFavIcon.src = '/images/favourite-filled.svg';
        gameFavIcon.classList.add(...classes['game_tile_box_favouritegame favouriteicon']);
        gameImageBox.appendChild(gameFavIcon);
    }

    if (gameObject.totalRating || displayClassification == true) {
        var gameImageRatingBanner = document.createElement('div');
        gameImageRatingBanner.classList.add(...classes['game_tile_box_ratingbanner']);

        if (showRatings == true || displayClassification == true) {
            if (showRatings == true) {
                if (gameObject.totalRating) {
                    var gameImageRatingBannerLogo = document.createElement('img');
                    gameImageRatingBannerLogo.src = '/images/IGDB_logo.svg';
                    gameImageRatingBannerLogo.setAttribute('style', 'filter: invert(100%); height: 10px; margin-right: 5px; padding-top: 4px;');
                    gameImageRatingBanner.appendChild(gameImageRatingBannerLogo);
    
                    var gameImageRatingBannerValue = document.createElement('span');
                    gameImageRatingBannerValue.innerHTML = Math.floor(gameObject.totalRating) + '% / ' + gameObject.totalRatingCount;
                    gameImageRatingBanner.appendChild(gameImageRatingBannerValue);
                }
            }

            gameImageBox.appendChild(gameImageRatingBanner);
            
            if (displayClassification == true) {
                var gameImageClassificationLogo = document.createElement('img');
                gameImageClassificationLogo.src = classificationPath;
                gameImageClassificationLogo.classList.add(...classes['rating_image_overlay']);
                gameImageBox.appendChild(gameImageClassificationLogo);
            }
        }
    }
    gameBox.appendChild(gameImageBox);

    if (showTitle == true) {
        var gameBoxTitle = document.createElement('div');
        gameBoxTitle.classList.add(...classes['game_tile_label']);
        gameBoxTitle.innerHTML = gameObject.name;
        gameBox.appendChild(gameBoxTitle);
    }

    return gameBox;
}

function getViewModeClasses(listView) {
    if (listView == false) {
        return {
            "game_tile game_tile_small": [ "game_tile", "game_tile_small" ],
            "game_tile": [ "game_tile" ],
            "game_tile_box": [ "game_tile_box" ],
            "game_tile_image game_tile_image_small lazy": [ "game_tile_image", "game_tile_image_small", "lazy" ],
            "game_tile_image lazy": [ "game_tile_image", "lazy" ],
            "game_tile_image unknown": [ "game_tile_image", "unknown" ],
            "game_tile_box_savedgame savedstateicon": [ "game_tile_box_savedgame", "savedstateicon" ],
            "game_tile_box_favouritegame favouriteicon": [ "game_tile_box_favouritegame", "favouriteicon" ],
            "game_tile_box_ratingbanner": [ "game_tile_box_ratingbanner" ],
            "rating_image_overlay": [ "rating_image_overlay" ],
            "game_tile_label": [ "game_tile_label" ]
        };
    } else {
        return {
            "game_tile game_tile_small": [ "game_tile_row", "game_tile_small" ],
            "game_tile": [ "game_tile_row" ],
            "game_tile_box": [ "game_tile_box_row" ],
            "game_tile_image game_tile_image_small lazy": [ "game_tile_image_row", "game_tile_image_small", "lazy" ],
            "game_tile_image lazy": [ "game_tile_image_row", "lazy" ],
            "game_tile_image unknown": [ "game_tile_image_row", "unknown" ],
            "game_tile_box_savedgame savedstateicon": [ "game_tile_box_savedgame_row", "savedstateicon" ],
            "game_tile_box_favouritegame favouriteicon": [ "game_tile_box_favouritegame_row", "favouriteicon" ],
            "game_tile_box_ratingbanner": [ "game_tile_box_ratingbanner_row" ],
            "rating_image_overlay": [ "rating_image_overlay_row" ],
            "game_tile_label": [ "game_tile_label_row" ]
        };
    }
}