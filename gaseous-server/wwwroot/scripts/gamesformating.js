function formatGamesPanel(targetElement, result, pageNumber, pageSize) {
    console.log("Displaying page: " + pageNumber);
    console.log("Page size: " + pageSize);

    if (pageNumber == 1) {
        targetElement.innerHTML = ''; 
    }

    var pagerCheck = document.getElementById('games_library_pagerstore');
    if (pageNumber == 1) {
        pagerCheck.innerHTML = "0";
    }

    if (pageNumber > Number(pagerCheck.innerHTML)) {
        pagerCheck.innerHTML = pageNumber;

        document.getElementById('games_library_recordcount').innerHTML = result.count + ' games';

        var existingLoadPageButton = document.getElementById('games_library_loadmore');
        if (existingLoadPageButton) {
            existingLoadPageButton.parentNode.removeChild(existingLoadPageButton);
        }

        for (var i = 0; i < result.games.length; i++) {
            var game = renderGameIcon(result.games[i], true, true, true);
            targetElement.appendChild(game);
        }

        $('.lazy').Lazy({
            scrollDirection: 'vertical',
            effect: 'fadeIn',
            visibleOnly: true
        });

        if (result.games.length == pageSize) {
            var loadPageButton = document.createElement("div");
            loadPageButton.id = 'games_library_loadmore';
            loadPageButton.innerHTML = 'Load More';
            loadPageButton.setAttribute('onclick', 'executeFilter1_1(' + (pageNumber + 1) + ', ' + pageSize + ');');
            loadPageButton.setAttribute('data-pagenumber', Number(pageNumber + 1));
            loadPageButton.setAttribute('data-pagesize', pageSize);
            targetElement.appendChild(loadPageButton);
        }
    }
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

function renderGameIcon(gameObject, showTitle, showRatings, showClassification, useSmallCover) {
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
        gameImage.setAttribute('data-src', '/api/v1.1/Games/' + gameObject.id + '/cover/image');
    } else {
        gameImage.className = 'game_tile_image unknown';
    }
    gameImageBox.appendChild(gameImage);

    var displayClassificationBoards = [ 'ACB', 'ESRB', 'USK' ];
    var classificationPath = '';
    var displayClassification = false;
    var shownClassificationBoard = '';
    if (showClassification == true) {
        for (var b = 0; b < displayClassificationBoards.length; b++) {
            if (shownClassificationBoard == '') {
                for (var c = 0; c < gameObject.ageRatings.length; c++) {
                    if (gameObject.ageRatings[c].category == displayClassificationBoards[b]) {
                        shownClassificationBoard = displayClassificationBoards[b];
                        displayClassification = true;
                        classificationPath = '/api/v1.1/Ratings/Images/' + displayClassificationBoards[b] + '/' + getKeyByValue(AgeRatingStrings, gameObject.ageRatings[c].rating) + '/image.svg';
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
                gameImageClassificationLogo.className = 'rating_image_mini';
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