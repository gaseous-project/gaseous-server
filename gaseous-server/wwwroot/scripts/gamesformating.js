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

function formatGamesPanel(targetElement, result, pageNumber, pageSize, forceScrollTop) {
    console.log("Displaying page: " + pageNumber);
    console.log("Page size: " + pageSize);

    var pageMode = GetPreference('LibraryPagination', 'paged');

    if (pageNumber == 1 || pageMode == 'paged') {
        targetElement.innerHTML = ''; 
    }

    if (pageMode == 'paged') {
        if (forceScrollTop == true) {
            window.scrollTo(0, 0);
        }
    }

    var pagerCheck = document.getElementById('games_library_pagerstore');
    if (pageNumber == 1) {
        pagerCheck.innerHTML = "0";
    }

    if (pageNumber > Number(pagerCheck.innerHTML) || pageMode == 'paged') {
        pagerCheck.innerHTML = pageNumber;

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
            targetElement.appendChild(game);
        }

        $('.lazy').Lazy({
            scrollDirection: 'vertical',
            effect: 'fadeIn',
            visibleOnly: true
        });

        var pager = document.getElementById('games_pager');
        pager.style.display = 'none';

        switch(pageMode) {
            case 'infinite':
                if (result.games.length == pageSize) {
                    var loadPageButton = document.createElement("div");
                    loadPageButton.id = 'games_library_loadmore';
                    loadPageButton.innerHTML = 'Load More';
                    loadPageButton.setAttribute('onclick', 'executeFilter1_1(' + (pageNumber + 1) + ', ' + pageSize + ');');
                    loadPageButton.setAttribute('data-pagenumber', Number(pageNumber + 1));
                    loadPageButton.setAttribute('data-pagesize', pageSize);
                    targetElement.appendChild(loadPageButton);
                }
                break;

            case 'paged':
                if (result.count > pageSize) {
                    // add some padding to the bottom of the games list
                    var loadPageButton = document.createElement("div");
                    loadPageButton.id = 'games_library_padding';
                    targetElement.appendChild(loadPageButton);

                    var pageCount = Math.ceil(result.count / pageSize);

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
                    var currentPage = Number(pagerCheck.innerHTML);
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
                            if (Number(pagerCheck.innerHTML) == i) {
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

                    pager.innerHTML = '';
                    pager.appendChild(prevPage);
                    pager.appendChild(pageNumbers);
                    pager.appendChild(nextPage);

                    pager.style.display = '';
                }
                break;
        }
    }

    // var pageReloadFunction = function() {
    //     formatGamesPanel(targetElement, result, pageNumber, pageSize, false);

    //     ajaxCall('/api/v1.1/Filter', 'GET', function (result) {
    //         var scrollerElement = document.getElementById('games_filter_scroller');
    //         formatFilterPanel(scrollerElement, result);
    //     })
    // };

    // window.clearTimeout(pageReloadInterval);
    // pageReloadInterval = setTimeout(pageReloadFunction, 10000);
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

function IsInView() {
    var loadElement = document.getElementById('games_library_loadmore');
    if (loadElement) {
        if (isScrolledIntoView(loadElement)) {
            var pageNumber = Number(document.getElementById('games_library_loadmore').getAttribute('data-pagenumber'));
            var pageSize = document.getElementById('games_library_loadmore').getAttribute('data-pagesize');
            executeFilter1_1(pageNumber, pageSize);
        }
    }
}

$(window).scroll(IsInView);

function renderGameIcon(gameObject, showTitle, showRatings, showClassification, classificationDisplayOrder, useSmallCover) {
    var gameBox = document.createElement('div');
    gameBox.id = "game_tile_" + gameObject.id;
    if (useSmallCover == true) {
        gameBox.className = 'game_tile game_tile_small';
    } else {
        gameBox.className = 'game_tile';
    }
    gameBox.setAttribute('onclick', 'window.location.href = "/index.html?page=game&id=' + gameObject.id + '";');

    var gameImageBox = document.createElement('div');
    gameImageBox.className = 'game_tile_box';

    var gameImage = document.createElement('img');
    if (useSmallCover == true) {
        gameImage.className = 'game_tile_image game_tile_image_small lazy';
    } else {
        gameImage.className = 'game_tile_image lazy';
    }
    gameImage.src = '/images/unknowngame.png';
    if (gameObject.cover) {
        gameImage.setAttribute('data-src', '/api/v1.1/Games/' + gameObject.id + '/cover/image/cover_big/' + gameObject.cover.imageId + '.jpg');
    } else {
        gameImage.className = 'game_tile_image unknown';
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
                        //classificationPath = '/api/v1.1/Ratings/Images/' + classificationDisplayOrder[b] + '/' + getKeyByValue(AgeRatingStrings, gameObject.ageRatings[c].rating) + '/image.svg';
                        classificationPath = '/images/Ratings/' + classificationDisplayOrder[b] + '/' + gameObject.ageRatings[c].rating + '.svg';
                    }
                }
            } else {
                break;
            }
        }
    }

    if (gameObject.totalRating || displayClassification == true) {
        var gameImageRatingBanner = document.createElement('div');
        gameImageRatingBanner.className = 'game_tile_box_ratingbanner';

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
                gameImageClassificationLogo.className = 'rating_image_overlay';
                gameImageBox.appendChild(gameImageClassificationLogo);
            }
        }
    }
    gameBox.appendChild(gameImageBox);

    if (showTitle == true) {
        var gameBoxTitle = document.createElement('div');
        gameBoxTitle.class = 'game_tile_label';
        gameBoxTitle.innerHTML = gameObject.name;
        gameBox.appendChild(gameBoxTitle);
    }

    return gameBox;
}