function formatGamesPanel(targetElement, result) {
    targetElement.innerHTML = '';
    for (var i = 0; i < result.length; i++) {
        var game = renderGameIcon(result[i], true, false);
        targetElement.appendChild(game);
    }

    $('.lazy').Lazy({
        scrollDirection: 'vertical',
        effect: 'fadeIn',
        visibleOnly: true
    });
}

function renderGameIcon(gameObject, showTitle, showRatings) {
    var gameBox = document.createElement('div');
    gameBox.className = 'game_tile';
    gameBox.setAttribute('onclick', 'window.location.href = "/index.html?page=game&id=' + gameObject.id + '";');

    var gameImage = document.createElement('img');
    gameImage.className = 'game_tile_image lazy';
    if (gameObject.cover) {
        gameImage.setAttribute('data-src', '/api/v1.0/Games/' + gameObject.id + '/cover/image');
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
                ratingImage.src = '/api/v1.0/Games/' + gameObject.id + '/agerating/' + gameObject.ageRatings.ids[i] + '/image';
                ratingImage.className = 'rating_image_mini';
                ratingsSection.appendChild(ratingImage);
            }
            gameBox.appendChild(ratingsSection);
        }
    }

    return gameBox;
}