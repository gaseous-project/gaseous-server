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
        this.row.appendChild(this.games);
    }

    async populate() {
        // get preferences
        let showTitle = GetPreference("Library.ShowGameTitle");
        let showRatings = GetPreference("Library.ShowGameRating");
        let showClassification = GetPreference("Library.ShowGameClassification");
        let classificationDisplayOrder = GetRatingsBoards();

        showRatings = false;
        showClassification = false;

        // start loading indicator
        let charCount = 0;
        this.loadingInterval = setInterval(() => {
            charCount++;
            if (charCount > 3) {
                charCount = 0;
            }
            this.games.innerHTML = '<p>Loading' + '.'.repeat(charCount) + '&nbsp;'.repeat(3 - charCount) + '</p>';
        }, 1000);

        let gameFilter = new Filtering();
        gameFilter.GetSummary = false;
        gameFilter.executeCallback = async (games) => {
            clearInterval(this.loadingInterval);
            this.games.innerHTML = "";

            if (games.length === 0) {
                this.games.innerHTML = "<p>No games found.</p>";
            } else {
                this.games.classList.remove("section-body");
                let scroller = document.createElement("ul");
                scroller.classList.add("homegame-scroller");

                for (const game of games) {
                    let gameItem = document.createElement("li");
                    gameItem.classList.add("homegame-item");

                    let gameObj = new GameIcon(game);
                    let gameTile = await gameObj.Render(showTitle, showRatings, showClassification, classificationDisplayOrder, false, true);
                    gameItem.appendChild(gameTile);

                    scroller.appendChild(gameItem);

                    if (game.cover) {
                        let coverUrl = '/api/v1.1/Games/' + game.metadataMapId + '/' + game.metadataSource + '/cover/' + game.cover + '/image/original/' + game.cover + '.jpg';
                        if (backgroundImageHandler === undefined || (backgroundImageHandler && backgroundImageHandler.URLList && backgroundImageHandler.URLList.length === 1)) {
                            let urls = [];
                            urls.push(coverUrl);
                            if (backgroundImageHandler !== undefined && backgroundImageHandler.URLList) {
                                urls.push(backgroundImageHandler.URLList[0]);
                            }
                            console.log("Creating new BackgroundImageRotator");
                            backgroundImageHandler = new BackgroundImageRotator(urls, null, true, true);
                        } else {
                            if (backgroundImageHandler && backgroundImageHandler.URLList && !backgroundImageHandler.URLList.includes(coverUrl)) {
                                backgroundImageHandler.URLList.push(coverUrl);
                            }
                        }
                    }
                }

                this.games.appendChild(scroller);
            }
        }
        gameFilter.ApplyFilter(this.searchModel);
    }
}

let targetDiv = document.getElementById("gamehome");

var gameRows = [];

backgroundImageHandler = undefined;

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

gameRows.push(new HomePageGameRow("Recently Played Games",
    {
        "playTime": { "min": 1, "max": null },
        "orderBy": "LastPlayed",
        "orderDirection": "Descending",
        "limit": 10
    }
));

gameRows.push(new HomePageGameRow("Recently Added Games",
    {
        "orderBy": "DateAdded",
        "orderDirection": "Descending",
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

    targetDiv.innerHTML = "";

    for (let row of gameRows) {
        targetDiv.appendChild(row.row);
        await row.populate();
    }
}

populateRows();

var coverURLList = [];

let profileDiv = document.getElementById("gameprofile");
profileDiv.innerHTML = "";
let profileCardContent = new ProfileCard(userProfile.profileId, false);
profileDiv.appendChild(profileCardContent);

// Register cleanup callback for home page
if (typeof registerPageUnloadCallback === 'function') {
    registerPageUnloadCallback('home', async () => {
        console.log('Cleaning up home page...');

        // Clear any loading intervals
        if (typeof gameRows !== 'undefined' && gameRows) {
            for (let row of gameRows) {
                if (row.loadingInterval) {
                    clearInterval(row.loadingInterval);
                    row.loadingInterval = null;
                }
            }
        }

        // Clear URL list
        if (typeof coverURLList !== 'undefined') {
            coverURLList = [];
        }

        // Clean up background image handler
        if (typeof backgroundImageHandler !== 'undefined' && backgroundImageHandler) {
            if (backgroundImageHandler.RotationTimer) {
                clearInterval(backgroundImageHandler.RotationTimer);
            }
            backgroundImageHandler = undefined;
        }

        console.log('Home page cleanup completed');
    });
}

// setup preferences callbacks
prefsDialog.OkCallbacks.push(async () => {
    await populateRows();
});