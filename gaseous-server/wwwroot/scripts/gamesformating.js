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
            var game = renderGameIcon(result.games[i], true, false);
            targetElement.appendChild(game);
        }

        $('.lazy').Lazy({
            scrollDirection: 'vertical',
            effect: 'fadeIn',
            visibleOnly: false
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

function renderGameIcon(gameObject, showTitle, showRatings) {
    var gameBox = document.createElement('div');
    gameBox.id = "game_tile_" + gameObject.id;
    gameBox.className = 'game_tile';
    gameBox.setAttribute('onclick', 'window.location.href = "/index.html?page=game&id=' + gameObject.id + '";');

    var gameImage = document.createElement('img');
    gameImage.className = 'game_tile_image lazy';
    if (gameObject.cover) {
        gameImage.setAttribute('data-src', '/api/v1.1/Games/' + gameObject.id + '/cover/image');
    } else {
        gameImage.src = '/images/unknowngame.png';
        gameImage.className = 'game_tile_image unknown';
    }
    gameBox.appendChild(gameImage);

    if (showTitle == true) {
        var gameBoxTitle = document.createElement('div');
        gameBoxTitle.class = 'game_tile_label';
        gameBoxTitle.innerHTML = gameObject.name;
        gameBox.appendChild(gameBoxTitle);
    }

    if (showRatings == true) {
        if (gameObject.ageRatings) {
            var ratingsSection = document.createElement('div');
            ratingsSection.id = 'ratings_section';
            for (var i = 0; i < gameObject.ageRatings.ids.length; i++) {
                var ratingImage = document.createElement('img');
                ratingImage.src = '/api/v1.1/Games/' + gameObject.id + '/agerating/' + gameObject.ageRatings.ids[i] + '/image';
                ratingImage.className = 'rating_image_mini';
                ratingsSection.appendChild(ratingImage);
            }
            gameBox.appendChild(ratingsSection);
        }
    }

    return gameBox;
}