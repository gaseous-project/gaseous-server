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

        let gameFilter = new Filtering();
        gameFilter.applyCallback = async (games) => {
            this.games.innerHTML = "";

            if (games.length === 0) {
                this.games.innerHTML = "<p>No games found.</p>";
            } else {
                let scroller = document.createElement("ul");
                scroller.classList.add("homegame-scroller");

                for (const game of games) {
                    let gameItem = document.createElement("li");
                    gameItem.classList.add("homegame-item");

                    let gameObj = new GameIcon(game);
                    let gameTile = await gameObj.Render(true, showRatings, showClassification, classificationDisplayOrder, false, true);
                    gameItem.appendChild(gameTile);

                    scroller.appendChild(gameItem);

                    if (game.cover) {
                        let coverUrl = '/api/v1.1/Games/' + game.metadataMapId + '/cover/' + game.cover + '/image/original/' + game.cover + '.jpg?sourceType=' + game.metadataSource;
                        if (!coverURLList.includes(coverUrl)) {
                            coverURLList.push(coverUrl);
                        }
                    }
                }

                this.games.appendChild(scroller);

                backgroundImageHandler = new BackgroundImageRotator(coverURLList, null, true);
            }
        }
        gameFilter.ApplyFilter(this.searchModel);
    }
}

let targetDiv = document.getElementById("gamehome");

let gameRows = [];

gameRows.push(new HomePageGameRow("Favourites",
    {
        "orderBy": "NameThe",
        "orderDirection": "Ascending",
        "settings": {
            "isFavourite": true
        },
        "limit": 10
    }
));

gameRows.push(new HomePageGameRow("Saved Games",
    {
        "orderBy": "NameThe",
        "orderDirection": "Ascending",
        "settings": {
            "hasSavedGame": true
        },
        "limit": 10
    }
));

gameRows.push(new HomePageGameRow("Top Rated Games",
    {
        "orderBy": "Rating",
        "orderDirection": "Descending",
        "uservotecount": {
            "min": 15,
            "max": null
        },
        "limit": 10
    }
));

async function populateRows() {
    // start populating the rows

    for (let row of gameRows) {
        targetDiv.appendChild(row.row);
        await row.populate();
    }
}

populateRows();

let coverURLList = [];

let profileDiv = document.getElementById("gameprofile");
profileDiv.innerHTML = "";
let profileCardContent = new ProfileCard(userProfile.profileId, false);
profileDiv.appendChild(profileCardContent);