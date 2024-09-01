class HomePageGameRow {
    constructor(title, searchModel) {
        this.title = title;
        this.searchModel = searchModel;

        // Create the row
        this.row = document.createElement("div");
        this.row.classList.add("section");

        let titleHeader = document.createElement("div");
        titleHeader.classList.add("section-header");
        titleHeader.textContent = this.title;
        this.row.appendChild(titleHeader);

        this.games = document.createElement("div");
        this.games.classList.add("section-body");
        this.games.innerHTML = "<p>Loading...</p>";
        this.row.appendChild(this.games);

        // this.populate();
    }

    async populate() {
        // get preferences
        let showRatings = GetPreference("LibraryShowGameRating", true);
        let showClassification = GetPreference("LibraryShowGameClassification", true);
        let classificationDisplayOrder = GetRatingsBoards();
        if (showRatings == "true") { showRatings = true; } else { showRatings = false; }
        if (showClassification == "true") { showClassification = true; } else { showClassification = false; }

        showRatings = false;
        showClassification = false;

        // get the games
        let gamesCallURL = '/api/v1.1/Games?pageNumber=1&pageSize=20';
        await fetch(gamesCallURL, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(this.searchModel)
        })
            .then(response => response.json())
            .then(data => {
                this.games.innerHTML = "";
                this.gamesResponse = data;

                if (data.games.length == 0) {
                    this.games.innerHTML = "<p>No games found</p>";
                } else {
                    let scroller = document.createElement("ul");
                    scroller.classList.add("homegame-scroller");

                    for (let game of data.games) {
                        let gameItem = document.createElement("li");
                        gameItem.classList.add("homegame-item");

                        let gameIcon = renderGameIcon(game, true, showRatings, showClassification, classificationDisplayOrder, true, false);
                        gameItem.appendChild(gameIcon);
                        scroller.appendChild(gameItem);
                    }
                    this.games.appendChild(scroller);

                    $('.lazy').Lazy({
                        effect: 'fadeIn',
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
            });
    }
}

let targetDiv = document.getElementById("gamehome");

let gameRows = [];

gameRows.push(new HomePageGameRow("Favourites",
    {
        "Name": "",
        "HasSavedGame": false,
        "isFavourite": true,
        "Platform": [],
        "Genre": [],
        "GameMode": [],
        "PlayerPerspective": [],
        "Theme": [],
        "MinimumReleaseYear": -1,
        "MaximumReleaseYear": -1,
        "GameRating": {
            "MinimumRating": -1,
            "MinimumRatingCount": -1,
            "MaximumRating": -1,
            "MaximumRatingCount": -1,
            "IncludeUnrated": false
        },
        "GameAgeRating": {
            "AgeGroupings": [],
            "IncludeUnrated": false
        },
        "Sorting": {
            "SortBy": "NameThe",
            "SortAscending": true
        }
    }
));

gameRows.push(new HomePageGameRow("Saved Games",
    {
        "Name": "",
        "HasSavedGame": true,
        "isFavourite": false,
        "Platform": [],
        "Genre": [],
        "GameMode": [],
        "PlayerPerspective": [],
        "Theme": [],
        "MinimumReleaseYear": -1,
        "MaximumReleaseYear": -1,
        "GameRating": {
            "MinimumRating": -1,
            "MinimumRatingCount": -1,
            "MaximumRating": -1,
            "MaximumRatingCount": -1,
            "IncludeUnrated": false
        },
        "GameAgeRating": {
            "AgeGroupings": [],
            "IncludeUnrated": false
        },
        "Sorting": {
            "SortBy": "NameThe",
            "SortAscending": true
        }
    }
));

gameRows.push(new HomePageGameRow("Top Rated Games",
    {
        "Name": "",
        "HasSavedGame": false,
        "isFavourite": false,
        "Platform": [],
        "Genre": [],
        "GameMode": [],
        "PlayerPerspective": [],
        "Theme": [],
        "MinimumReleaseYear": -1,
        "MaximumReleaseYear": -1,
        "GameRating": {
            "MinimumRating": -1,
            "MinimumRatingCount": 15,
            "MaximumRating": -1,
            "MaximumRatingCount": -1,
            "IncludeUnrated": false
        },
        "GameAgeRating": {
            "AgeGroupings": [],
            "IncludeUnrated": false
        },
        "Sorting": {
            "SortBy": "Rating",
            "SortAscending": false
        }
    }
));

async function populateRows() {
    // start populating the rows
    let coverURLList = [];
    for (let row of gameRows) {
        targetDiv.appendChild(row.row);
        await row.populate();
        console.log(row.gamesResponse);

        // collect the cover URLs
        for (let game of row.gamesResponse.games) {
            if (game.cover) {
                if (game.cover.id) {
                    let coverUrl = '/api/v1.1/Games/' + game.id + '/cover/' + game.cover.id + '/image/cover_big/' + game.cover.id + '.jpg';
                    if (!coverURLList.includes(coverUrl)) {
                        coverURLList.push(coverUrl);
                    }
                }
            }
        }
    }

    backgroundImageHandler = new BackgroundImageRotator(coverURLList, null, true);
}

populateRows();

let profileDiv = document.getElementById("gameprofile");
profileDiv.innerHTML = "";
let profileCardContent = new ProfileCard(userProfile.profileId, false);
profileDiv.appendChild(profileCardContent);