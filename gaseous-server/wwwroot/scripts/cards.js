/**
 * A class for creating a card
 * @class Card
 * @param {string} cardType The type of card to create
 * @example
 * let card = new Card('game');
 * card.BuildCard();
 * card.Open();
 */
class Card {
    constructor(cardType) {
        this.cardType = cardType;
    }

    async BuildCard() {
        // create the background
        this.modalBackground = document.createElement("div");
        this.modalBackground.classList.add("modal-background");
        this.modalBackground.addEventListener("click", (event) => {
            if (event.target === this.modalBackground) {
                this.Close();
            }
        });
        this.modalBackground.style.display = "none";

        // create the card
        this.card = document.createElement("div");
        this.card.classList.add("card-window");

        // create a card scroller
        this.cardScroller = document.createElement("div");
        this.cardScroller.classList.add("card-scroller");
        this.card.appendChild(this.cardScroller);

        // add the card content
        this.cardContent = document.createElement("div");
        this.cardContent.classList.add("card-content");
        this.cardScroller.appendChild(this.cardContent);

        // add the background image container
        this.cardBackgroundContainer = document.createElement("div");
        this.cardBackgroundContainer.classList.add("card-background-container");
        this.cardContent.appendChild(this.cardBackgroundContainer);

        // set up fancy scrolling, and store the scroll position in session storage
        this.cardScroller.addEventListener("scroll", () => {
            let currentScrollPosition = this.cardScroller.scrollTop;
            let computedScrollPosition = Math.floor(Number((currentScrollPosition / 4) * -1)) + "px";

            this.cardBackgroundContainer.style.top = computedScrollPosition;

            // store the scroll position in session storage
            sessionStorage.setItem("Card." + this.cardType + ".scrollPosition", currentScrollPosition);
        });

        // add the background image placeholder
        this.cardBackground = document.createElement("img");
        this.cardBackground.classList.add("card-background");
        this.cardBackgroundContainer.appendChild(this.cardBackground);

        // add the background image gradient
        this.cardGradient = document.createElement("div");
        this.cardGradient.classList.add("card-gradient");
        this.cardBackgroundContainer.appendChild(this.cardGradient);

        // Load the content from the HTML file
        const response = await fetch("/pages/cards/" + this.cardType + ".html");
        const content = await response.text();

        // add the card body
        this.cardBody = document.createElement("div");
        this.cardBody.classList.add("card-body");
        this.cardBody.innerHTML = content;
        this.cardContent.appendChild(this.cardBody);

        // add the card header
        this.cardHeader = document.createElement("div");
        this.cardHeader.classList.add("card-header");
        this.card.appendChild(this.cardHeader);

        // add the close button
        this.closeButton = document.createElement("div");
        this.closeButton.classList.add("card-close-button");
        this.closeButton.innerHTML = "&times;";
        this.closeButton.addEventListener("click", () => {
            this.Close();
        });
        this.card.appendChild(this.closeButton);

        // add the card to the background
        this.modalBackground.appendChild(this.card);

        // append the modal to the body
        document.body.appendChild(this.modalBackground);

        // add event listener for escape key
        document.addEventListener("keydown", (event) => {
            if (event.key === "Escape") {
                this.Close();
            }
        });
    }

    PostOpenCallbacks = [
        () => {
            // restore the scroll position from session storage
            let scrollPosition = sessionStorage.getItem("Card." + this.cardType + ".scrollPosition");
            if (scrollPosition) {
                this.cardScroller.scrollTop = scrollPosition;
            }

            // set the focus to the card scroller
            this.cardScroller.focus();
        }
    ];

    async Open() {
        // hide the scroll bar for the page
        document.body.style.overflow = "hidden";

        // show the modal
        $(this.modalBackground).fadeIn(200);

        // run any post open callbacks
        this.PostOpenCallbacks.forEach(callback => {
            callback();
        });
    }

    SetHeader(Title, AlwaysVisible) {
        this.cardHeader.innerHTML = Title;

        if (AlwaysVisible) {
            this.cardHeader.style.display = "block";
        } else {
            // create a scroll event for the card that will set the opacity of the header based on the scroll position
            this.cardHeader.style.opacity = 0;
            this.cardHeader.style.display = "block";
            this.cardScroller.addEventListener("scroll", () => {
                if (AlwaysVisible === false) {
                    let opacityValue = 0;
                    if (this.cardScroller.scrollTop > 200 && this.cardScroller.scrollTop < 400) {
                        // begin to fade in the header
                        let scrollPosition = this.cardScroller.scrollTop - 200;
                        let scrollPercentage = scrollPosition / 200;
                        opacityValue = scrollPercentage;
                    } else if (this.cardScroller.scrollTop < 200) {
                        // hide the header
                        opacityValue = 0;
                    } else {
                        // show the header
                        opacityValue = 1;
                    }

                    this.cardHeader.style.opacity = opacityValue;
                } else {
                    this.cardHeader.style.opacity = 1;
                }
            });
        }
    }

    SetBackgroundImage(Url, blur, callback) {
        this.cardBackground.src = Url;
        this.cardBackground.onload = () => {
            // get the average colour of the image
            let rgbAverage = getAverageRGB(this.cardBackground);
            this.card.style.backgroundColor = 'rgb(' + rgbAverage.r + ', ' + rgbAverage.g + ', ' + rgbAverage.b + ')';
            this.cardGradient.style.background = 'linear-gradient(180deg, rgba(0, 0, 0, 0) 0%, rgba(' + rgbAverage.r + ', ' + rgbAverage.g + ', ' + rgbAverage.b + ', 1) 100%)';
            if (blur === true) {
                this.cardBackgroundContainer.classList.add('card-background-blurred');
            }

            // set the font colour to a contrasting colour
            let contrastColour = contrastingColor(rgbToHex(rgbAverage.r, rgbAverage.g, rgbAverage.b).replace("#", ""));
            this.card.style.color = '#' + contrastColour;
            this.contrastColour = contrastColour;

            if (callback) {
                callback();
            }
        }
    }

    Close() {
        // hide the modal
        $(this.modalBackground).fadeOut(200, () => {
            // Show the scroll bar for the page
            if (document.getElementsByClassName('modal-background').length === 1) {
                if (document.getElementsByClassName('modal-window-body').length === 0) {
                    document.body.style.overflow = 'auto';
                }
            }

            // Remove the modal element from the document body
            if (this.modalBackground) {
                this.modalBackground.remove();
                this.modalBackground = null;
            }

            // Remove all keys from session storage that start with "Card." + this.cardType
            let keys = Object.keys(sessionStorage);
            keys.forEach(key => {
                if (key.startsWith("Card." + this.cardType)) {
                    sessionStorage.removeItem(key);
                }
            });
        });
    }
}

/**
 * A class for creating a game card
 * @class GameCard
 * @param {number} gameId The ID of the game to create a card for
 * @example
 * let card = new GameCard(1);
 * card.ShowCard();
 */
class GameCard {
    constructor(gameId) {
        this.gameId = gameId;
    }

    async ShowCard() {
        this.card = new Card('game', this.gameId);
        this.card.BuildCard();

        // store the card object in the session storage
        sessionStorage.setItem("Card." + this.card.cardType + ".Id", this.gameId);

        // fetch the game data
        const response = await fetch("/api/v1.1/Games/" + this.gameId, {
            method: "GET",
            headers: {
                "Content-Type": "application/json"
            }
        });
        const gameData = await response.json();

        // dump the game data to the console for debugging
        console.log(gameData);

        // set the header
        this.card.SetHeader(gameData.name, false);

        // set the background image
        if (gameData.artworks) {
            // // randomly select an artwork to display
            // let randomIndex = Math.floor(Math.random() * gameData.artworks.length);
            // let artwork = gameData.artworks[randomIndex];
            let artwork = gameData.artworks[0];
            let artworkUrl = `/api/v1.1/Games/${this.gameId}/${gameData.metadataSource}/artwork/${artwork}/image/original/${artwork}.jpg`;
            this.card.SetBackgroundImage(artworkUrl, true, () => {
                if (this.card.contrastColour !== 'fff') {
                    let ratingIgdbLogo = this.card.cardBody.querySelector('#card-userrating-igdb-logo');
                    ratingIgdbLogo.classList.add('card-info-rating-icon-black');
                }
            });
        } else if (gameData.cover) {
            let coverUrl = `/api/v1.1/Games/${this.gameId}/${gameData.metadataSource}/cover/${gameData.cover}/image/original/${gameData.cover}.jpg`;
            this.card.SetBackgroundImage(coverUrl, true, () => {
                if (this.card.contrastColour !== 'fff') {
                    let ratingIgdbLogo = this.card.cardBody.querySelector('#card-userrating-igdb-logo');
                    ratingIgdbLogo.classList.add('card-info-rating-icon-black');
                }
            });
        } else {
            this.card.SetBackgroundImage('/images/SettingsWallpaper.jpg', true, () => {
                if (this.card.contrastColour !== 'fff') {
                    let ratingIgdbLogo = this.card.cardBody.querySelector('#card-userrating-igdb-logo');
                    ratingIgdbLogo.classList.add('card-info-rating-icon-black');
                }
            });
        }

        // set the card title info container classes
        let cardTitleInfo = this.card.cardBody.querySelector('#card-title-info');
        cardTitleInfo.classList.add('card-title-info');

        // set the card attribution
        let cardAttribution = this.card.cardBody.querySelector('#card-metadataattribution');
        switch (gameData.metadataSource) {
            case "IGDB":
                cardAttribution.innerHTML = `Data provided by ${gameData.metadataSource}. <a href="https://www.igdb.com/games/${gameData.slug}" class="romlink" target="_blank" rel="noopener noreferrer">Source</a>`;
                cardAttribution.style.display = '';
                break;

            case "TheGamesDb":
                cardAttribution.innerHTML = `Data provided by ${gameData.metadataSource}. <a href="https://thegamesdb.net/game.php?id=${gameData.id}" class="romlink" target="_blank" rel="noopener noreferrer">Source</a>`;
                cardAttribution.style.display = '';
                break;
        }

        // set the cover art
        let logoProviders = ["ScreenScraper", "TheGamesDb"];
        let clearLogoValid = false;
        if (gameData.clearLogo) {
            for (const provider of logoProviders) {
                if (gameData.clearLogo[provider] !== undefined) {
                    clearLogoValid = true;
                    break;
                }
            }
        }
        let usingClearLogo = false;
        if (clearLogoValid && GetPreference('Library.ShowClearLogo') === true) {
            let clearLogoImg = this.card.cardBody.querySelector('#card-clearlogo');
            if (clearLogoImg) {
                for (const provider of logoProviders) {
                    if (gameData.clearLogo[provider] !== undefined) {
                        let providerIds = gameData.clearLogo[provider];
                        let providerId = null;
                        // check if providerIds is an array
                        if (Array.isArray(providerIds)) {
                            providerId = providerIds[0];
                        } else {
                            providerId = providerIds;
                        }

                        clearLogoImg.src = `/api/v1.1/Games/${this.gameId}/${provider}/clearlogo/${providerId}/image/original/${providerId}.png`;
                        clearLogoImg.alt = gameData.name;
                        clearLogoImg.title = gameData.name;
                        clearLogoImg.style.display = '';
                        usingClearLogo = true;

                        cardTitleInfo.classList.add('card-title-info-clearlogo');

                        if (provider !== gameData.metadataSource) {
                            let logoAttribution = this.card.cardBody.querySelector('#card-logoattribution');
                            logoAttribution.innerHTML = `Logo provided by ${provider}`;
                            logoAttribution.style.display = '';
                        }
                        break;
                    }
                }
            }
        } else {
            let coverImg = this.card.cardBody.querySelector('#card-cover');
            if (coverImg) {
                if (gameData.cover) {
                    coverImg.src = `/api/v1.1/Games/${this.gameId}/${gameData.metadataSource}/cover/${gameData.cover}/image/cover_big/${gameData.cover}.jpg`;
                } else {
                    coverImg.src = '/images/unknowngame.png';
                }
                coverImg.alt = gameData.name;
                coverImg.title = gameData.name;
                coverImg.style.display = '';
            }
        }

        // set the game name
        if (!usingClearLogo) {
            let gameName = this.card.cardBody.querySelector('#card-title');
            if (gameName) {
                gameName.innerHTML = gameData.name;
                gameName.style.display = '';
            }
        }

        // set the game rating
        let ageRating = this.card.cardBody.querySelector('#card-rating');
        if (gameData.age_ratings) {
            fetch(`/api/v1.1/Games/${this.gameId}/${gameData.metadataSource}/agerating`, {
                method: "GET",
                headers: {
                    "Content-Type": "application/json"
                }
            }).then(response => response.json()).then(data => {
                if (data) {
                    let userRatingOrder = GetPreference('Library.GameClassificationDisplayOrder');
                    let abortLoop = false;
                    userRatingOrder.forEach(ratingElement => {
                        if (abortLoop === false) {
                            data.forEach(dataElement => {
                                if (ratingElement.toLowerCase() === dataElement.ratingBoard.toLowerCase()) {
                                    let rating = document.createElement('div');
                                    rating.classList.add('card-rating');

                                    let ratingIcon = document.createElement('img');
                                    ratingIcon.src = `/images/Ratings/${dataElement.ratingBoard.toUpperCase()}/${dataElement.ratingTitle}.svg`;
                                    ratingIcon.alt = dataElement.ratingTitle;
                                    ratingIcon.title = dataElement.ratingTitle;
                                    ratingIcon.classList.add('card-rating-icon');

                                    let description = ClassificationBoards[dataElement.ratingBoard] + '\nRating: ' + ClassificationRatings[dataElement.ratingTitle];
                                    if (dataElement.descriptions && dataElement.descriptions.length > 0) {
                                        description += '\n\nDescription:';
                                        dataElement.descriptions.forEach(element => {
                                            description += '\n' + element;
                                        });
                                    }

                                    ratingIcon.alt = description;
                                    ratingIcon.title = description;

                                    rating.appendChild(ratingIcon);

                                    ageRating.appendChild(rating);
                                    ageRating.style.display = '';

                                    abortLoop = true;
                                }
                            });
                        } else {
                            return;
                        }
                    });
                }
            });
        }

        // set the release date
        if (gameData.first_release_date) {
            let relDate = new Date(gameData.first_release_date);
            let year = new Intl.DateTimeFormat('en', { year: 'numeric' }).format(relDate);
            let releaseDate = this.card.cardBody.querySelector('#card-releasedate');
            releaseDate.innerHTML = year;
            releaseDate.alt = relDate;
            releaseDate.title = relDate;
            releaseDate.style.display = '';
        }

        // set the developers
        if (gameData.involved_companies) {
            fetch(`/api/v1.1/Games/${this.gameId}/${gameData.metadataSource}/companies`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            }).then(response => response.json()).then(data => {
                if (data) {
                    let developers = [];
                    data.forEach(element => {
                        if (element.involvement.developer === true) {
                            if (!developers.includes(element.company.name)) {
                                developers.push(element.company.name);
                            }
                        }
                    });

                    if (developers.length > 0) {
                        let developersLabel = this.card.cardBody.querySelector('#card-developers');
                        developersLabel.innerHTML = developers.join(', ');
                        developersLabel.style.display = '';
                    }
                }
            });
        }

        // set the genres
        if (gameData.genres) {
            fetch(`/api/v1.1/Games/${this.gameId}/${gameData.metadataSource}/genre`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            }).then(response => response.json()).then(data => {
                if (data) {
                    let genres = [];
                    data.forEach(element => {
                        if (!genres.includes(element.name)) {
                            genres.push(element.name);
                        }
                    });

                    if (genres.length > 0) {
                        let genresLabel = this.card.cardBody.querySelector('#card-genres');
                        genresLabel.innerHTML = genres.join(', ');
                        genresLabel.style.display = '';
                    }
                }
            });
        }

        // set the themes
        if (gameData.themes) {
            fetch(`/api/v1.1/Games/${this.gameId}/${gameData.metadataSource}/themes`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            }).then(response => response.json()).then(data => {
                if (data) {
                    let themes = [];
                    data.forEach(element => {
                        if (!themes.includes(element.name)) {
                            themes.push(element.name);
                        }
                    });

                    if (themes.length > 0) {
                        let themesLabel = this.card.cardBody.querySelector('#card-themes');
                        themesLabel.innerHTML = themes.join(', ');
                        themesLabel.style.display = '';
                    }
                }
            });
        }

        // set the game rating
        if (gameData.total_rating) {
            let rating = this.card.cardBody.querySelector('#card-userrating-igdb-value');
            rating.innerHTML = Math.floor(gameData.total_rating) + '%';
            rating.style.display = '';

            let ratingIgdb = this.card.cardBody.querySelector('#card-userrating-igdb');
            ratingIgdb.style.display = '';

            let ratingPanel = this.card.cardBody.querySelector('#card-userratings');
            ratingPanel.style.display = '';
        }

        // display the screenshots
        let screenshots = this.card.cardBody.querySelector('#card-screenshots');
        let screenshotsSection = this.card.cardBody.querySelector('#card-screenshots-section');
        if (screenshots) {
            if (gameData.screenshots) {
                gameData.screenshots.forEach(screenshot => {
                    let screenshotItem = document.createElement('li');
                    screenshotItem.classList.add('card-screenshot-item');

                    let screenshotImg = document.createElement('img');
                    screenshotImg.src = `/api/v1.1/Games/${this.gameId}/${gameData.metadataSource}/screenshots/${screenshot}/image/screenshot_med/${screenshot}.jpg`;
                    screenshotImg.alt = gameData.name;
                    screenshotImg.title = gameData.name;
                    screenshotItem.appendChild(screenshotImg);
                    screenshots.appendChild(screenshotItem);
                });
                screenshotsSection.style.display = '';
            }
        }

        // set the game summary
        let gameSummary = this.card.cardBody.querySelector('#card-summary');
        let gameSummarySection = this.card.cardBody.querySelector('#card-summary-section');
        if (gameData.summary || gameData.storyline) {
            if (gameData.summary) {
                gameSummary.innerHTML = gameData.summary.replaceAll("\n", "<br />");
            } else {
                gameSummary.innerHTML = gameData.storyLine.replaceAll("\n", "<br />");
            }
            gameSummarySection.style.display = '';

            this.card.PostOpenCallbacks.push(() => {
                if (gameSummary.offsetHeight < gameSummary.scrollHeight ||
                    gameSummary.offsetWidth < gameSummary.scrollWidth) {
                    // your element has overflow and truncated
                    // show read more / read less button
                    let readMoreLink = this.card.cardBody.querySelector('#card-summary-full');
                    readMoreLink.style.display = 'block';
                    [gameSummary, readMoreLink].forEach(element => {
                        element.addEventListener('click', () => {
                            if (gameSummary.classList.contains('line-clamp-4')) {
                                gameSummary.classList.remove('line-clamp-4');
                                readMoreLink.innerHTML = 'Read less';
                            } else {
                                gameSummary.classList.add('line-clamp-4');
                                readMoreLink.innerHTML = 'Read more';
                            }
                        });
                    });
                } else {
                    // your element doesn't overflow (not truncated)
                    this.card.cardBody.querySelector('#card-summary-full').style.display = 'none';
                }
            });
        }

        // get the game statistics
        fetch(`/api/v1.1/Statistics/Games/${this.gameId}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(response => response.json()).then(data => {
            if (data) {
                let gameStat_lastPlayed = document.getElementById('gamestatistics_lastplayed_value');
                let gameStat_timePlayed = document.getElementById('gamestatistics_timeplayed_value');

                const dateOptions = {
                    //weekday: 'long',
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric'
                };
                gameStat_lastPlayed.innerHTML = new Date(data.sessionEnd).toLocaleDateString(undefined, dateOptions);
                if (data.sessionLength >= 60) {
                    gameStat_timePlayed.innerHTML = Number(data.sessionLength / 60).toFixed(2) + " hours";
                } else {
                    gameStat_timePlayed.innerHTML = Number(data.sessionLength) + " minutes";
                }
            }
        });

        // get the game favourite status
        fetch(`/api/v1.1/Games/${this.gameId}/favourite`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(response => response.json()).then(data => {
            let favouriteButton = this.card.cardBody.querySelector('#gamestatistics_favourite_button');
            let gameFavIcon = document.createElement('img');
            gameFavIcon.id = "gamestatistics_favourite";
            gameFavIcon.className = "favouriteicon";
            gameFavIcon.title = "Favourite";
            gameFavIcon.alt = "Favourite";

            if (data === true) {
                gameFavIcon.setAttribute("src", '/images/favourite-filled.svg');
            } else {
                gameFavIcon.setAttribute("src", '/images/favourite-empty.svg');
            }

            favouriteButton.addEventListener('click', async (e) => {
                e.stopPropagation();
                let favouriteStatus = await fetch(`/api/v1.1/Games/${this.gameId}/favourite?favourite=` + !data, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                }).then(response => response.json());

                if (favouriteStatus === true) {
                    gameFavIcon.setAttribute("src", '/images/favourite-filled.svg');
                } else {
                    gameFavIcon.setAttribute("src", '/images/favourite-empty.svg');
                }
                data = favouriteStatus;
            });

            favouriteButton.innerHTML = '';
            favouriteButton.appendChild(gameFavIcon);
            favouriteButton.style.display = '';
        });

        // get the available game platforms
        await fetch(`/api/v1.1/Games/${this.gameId}/${gameData.metadataSource}/platforms`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(response => response.json()).then(data => {
            if (data) {
                // sort data by name attribute, then by metadataGameName attribute
                data.sort((a, b) => {
                    let nameA = a.name.toUpperCase();
                    let nameB = b.name.toUpperCase();
                    let metadataGameNameA = a.metadataGameName.toUpperCase();
                    let metadataGameNameB = b.metadataGameName.toUpperCase();

                    if (nameA < nameB) {
                        return -1;
                    } else if (nameA > nameB) {
                        return 1;
                    } else {
                        if (metadataGameNameA < metadataGameNameB) {
                            return -1;
                        } else if (metadataGameNameA > metadataGameNameB) {
                            return 1;
                        } else {
                            return 0;
                        }
                    }
                });

                let platforms = {};
                let mostRecentPlatform = null;
                data.forEach(element => {
                    // check if the platform id is already in the platforms object
                    if (platforms[element.id] === undefined) {
                        platforms[element.id] = [element];
                    } else {
                        // add the game object to the platform
                        platforms[element.id].push(element);
                    }

                    // set mostRecentPlatform to element only if element.lastPlayed has a value and is a more recent date than mostRecentPlatform
                    if (element.lastPlayed) {
                        if (mostRecentPlatform === null || new Date(element.lastPlayed) > new Date(mostRecentPlatform.lastPlayed)) {
                            mostRecentPlatform = element;
                        }
                    }
                });

                if (mostRecentPlatform && mostRecentPlatform.lastPlayed) {
                    // set the most recent platform
                    let mostRecentPlatformName = this.card.cardBody.querySelector('#card-launchgame');
                    mostRecentPlatformName.classList.add('platform_edit_button');
                    mostRecentPlatformName.classList.add('platform_item_green');
                    mostRecentPlatformName.style.display = '';

                    // set the button name
                    let mostRecentPlatformButton = this.card.cardBody.querySelector('#card-launchgame-text');
                    mostRecentPlatformButton.innerHTML = mostRecentPlatform.metadataGameName + '<br />' + mostRecentPlatform.name;

                    mostRecentPlatformName.addEventListener('click', async (e) => {
                        e.stopPropagation();
                        let launchLink = await BuildGameLaunchLink(mostRecentPlatform);
                        if (launchLink === null) {
                            console.log('Error: Unable to validate launch link');
                            console.log(mostRecentPlatform);
                        } else {
                            // launch the game
                            window.location.href = launchLink;
                        }
                    });
                }

                // create the platform items and add them to the DOM object named 'card-platforms'
                let cardPlatforms = this.card.cardBody.querySelector('#card-platforms');

                for (const [key, value] of Object.entries(platforms)) {
                    let platformItem = new GameCardPlatformItem(value[0].name, key);
                    value.forEach(element => {
                        platformItem.Add(element);
                    });

                    let platformItemElement = platformItem.BuildItem();
                    cardPlatforms.appendChild(platformItemElement);
                }

                // show the platform section
                let platformSection = this.card.cardBody.querySelector('#card-platforms-section');
                platformSection.style.display = '';
            }
        });

        // show the card
        this.card.Open();
    }
}

class GameCardPlatformItem {
    constructor(platformName, platformId) {
        this.platformName = platformName;
        this.platformId = platformId;
    }

    gameObjects = [];

    Add(gameObject) {
        this.gameObjects.push(gameObject);
    }

    BuildItem() {
        // create the platform item
        // the platform item is a two column div - the left column contains the platform logo, the right column contains the game list
        let platformItem = document.createElement('div');
        platformItem.classList.add('section');
        platformItem.classList.add('card-platform-section');

        // create the platform name
        let platformName = document.createElement('div');
        platformName.classList.add('section-header');
        platformName.classList.add('card-platform-name');
        platformName.innerHTML = this.platformName;
        platformItem.appendChild(platformName);

        // create the platform items container
        let platformItemsContainer = document.createElement('div');
        platformItemsContainer.classList.add('card-platform-item');
        platformItem.appendChild(platformItemsContainer);

        // create the platform logo container
        let platformLogoContainer = document.createElement('div');
        platformLogoContainer.classList.add('card_image_container');
        platformLogoContainer.classList.add('platform_image_container');
        platformItemsContainer.appendChild(platformLogoContainer);

        // create the platform logo
        let platformLogo = document.createElement('img');
        platformLogo.src = `/api/v1.1/Platforms/${this.platformId}/platformlogo/original/logo.png`;
        platformLogo.alt = this.platformName;
        platformLogo.title = this.platformName;
        platformLogo.classList.add('platform_image');
        platformLogoContainer.appendChild(platformLogo);

        // create the game list
        let gameList = document.createElement('div');
        gameList.classList.add('card-platform-gamelist');
        platformItemsContainer.appendChild(gameList);

        // add the game objects to the game list
        let firstGameItem = true;
        this.gameObjects.forEach(element => {
            let romItem = null;

            let outerGameItem = document.createElement('div');
            outerGameItem.classList.add('card-platform-gameitem-container');

            let gameItem = document.createElement('div');
            gameItem.classList.add('card-platform-gameitem');

            // create expand button
            let expandButton = document.createElement('div');
            expandButton.classList.add('platform_edit_button');
            expandButton.classList.add('platform_edit_button_expand');
            let expandButtonImage = document.createElement('img');
            expandButtonImage.classList.add('banner_button_image');
            expandButtonImage.src = '/images/arrow-right.svg';
            expandButtonImage.alt = 'Expand';
            expandButtonImage.title = 'Expand';
            expandButton.appendChild(expandButtonImage);
            expandButton.addEventListener('click', async (e) => {
                e.stopPropagation();
                if (!romItem) {
                    romItem = new GameCardRomList(element);
                    romItem.BuildRomList();
                    outerGameItem.append(romItem.Body);
                    romItem.Body.style.display = 'block';
                    expandButton.classList.add('platform_edit_button_active');
                    expandButtonImage.classList.add('banner_button_image_rotated');
                } else {
                    // toggle the visibility of the rom list
                    if (romItem.Body.style.display === 'none') {
                        romItem.Body.style.display = 'block';
                        expandButton.classList.add('platform_edit_button_active');
                        expandButtonImage.classList.add('banner_button_image_rotated');
                    } else {
                        romItem.Body.style.display = 'none';
                        expandButton.classList.remove('platform_edit_button_active');
                        expandButtonImage.classList.remove('banner_button_image_rotated');
                    }
                }
            });
            gameItem.appendChild(expandButton);

            // create the game item name
            let gameItemName = document.createElement('div');
            gameItemName.classList.add('card-platform-gamename');
            gameItemName.innerHTML = element.metadataGameName;
            gameItem.appendChild(gameItemName);

            // create the game item user manual button
            if (element.userManualLink !== undefined && element.userManualLink !== null) {
                let userManualButton = document.createElement('div');
                userManualButton.className = 'platform_edit_button';
                userManualButton.innerHTML = '<img src="/images/manual.svg" class="banner_button_image" />';
                userManualButton.addEventListener('click', (e) => {
                    e.stopPropagation();
                    let guideUrl = window.open(element.userManualLink, '_blank');
                    guideUrl.opener = null;
                });
                gameItem.appendChild(userManualButton);
            }

            // create platform state manager button
            if (element.lastPlayedRomId !== undefined && element.lastPlayedRomId !== null) {
                let platformStateManagerButton = document.createElement('div');
                platformStateManagerButton.className = 'platform_edit_button platform_statemanager_button';
                platformStateManagerButton.innerHTML = '<img src="/images/SaveStates.png" class="savedstatemanagericon" />';
                platformStateManagerButton.addEventListener('click', (e) => {
                    e.stopPropagation();
                    let isMediaGroup = false;
                    if (element.lastPlayedRomIsMediagroup === true || element.favouriteRomIsMediagroup === true) {
                        isMediaGroup = true;
                    }
                    console.log('RomID: ' + element.lastPlayedRomId + ' isMediaGroup: ' + isMediaGroup);
                    let stateManager = new EmulatorStateManager(element.lastPlayedRomId, isMediaGroup, element.emulatorConfiguration.emulatorType, element.emulatorConfiguration.core, element.id, element.metadataMapId, element.lastPlayedRomName);
                    stateManager.open();
                });
                gameItem.appendChild(platformStateManagerButton);
            }



            outerGameItem.appendChild(gameItem);

            // create the game item play button
            if (
                (element.lastPlayedRomId !== undefined && element.lastPlayedRomId !== null) ||
                (element.favouriteRomId !== undefined && element.favouriteRomId !== null)
            ) {
                let playButton = document.createElement('div');
                playButton.classList.add('platform_edit_button');
                playButton.classList.add('platform_item_green');
                playButton.innerHTML = '<img src="/images/play.svg" class="banner_button_image" />';
                playButton.addEventListener('click', async (e) => {
                    e.stopPropagation();
                    let launchLink = await BuildGameLaunchLink(element);
                    if (launchLink === null) {
                        console.log('Error: Unable to validate launch link');
                        console.log(element);
                    } else {
                        // launch the game
                        window.location.href = launchLink;
                    }
                });
                gameItem.appendChild(playButton);
            } else if (firstGameItem) {
                // expand the edit button by default if there is no global play button visible
                if (document.getElementById('card-launchgame').style.display !== '') {
                    expandButton.click();
                }
            }
            firstGameItem = false;

            gameList.appendChild(outerGameItem);
        });

        return platformItem;
    }
}

class GameCardRomList {
    constructor(gamePlatformObject) {
        this.gamePlatformObject = gamePlatformObject;
    }

    BuildRomList() {
        // create the rom list div
        this.Body = document.createElement('div');
        this.Body.classList.add('card-romlist');

        // create the media group container
        this.mediaGroupContainer = document.createElement('div');
        this.mediaGroupContainer.classList.add('card-romlist-group');
        this.mediaGroupContainer.classList.add('card-romlist-group-header');
        this.Body.appendChild(this.mediaGroupContainer);
        this.Body.style.display = 'none';
        this.LoadMediaGroups();

        // create the rom list container
        this.romListContainer = document.createElement('div');
        this.romListContainer.classList.add('card-romlist-group');
        this.romListContainer.classList.add('card-romlist-group-header');
        this.Body.appendChild(this.romListContainer);
        this.LoadRoms();

        // create the mangement buttons
        this.managementButtons = document.createElement('div');
        this.managementButtons.classList.add('card-romlist-management');
        this.Body.appendChild(this.managementButtons);

        // create the edit button
        if (userProfile.roles.includes("Admin")) {
            this.createMediaGroupButton = document.createElement('button');
            this.createMediaGroupButton.classList.add('modal-button');
            this.createMediaGroupButton.classList.add('card-romlist-management-button');
            this.createMediaGroupButton.innerHTML = 'Create Media Group';
            this.createMediaGroupButton.style.display = 'none';
            this.createMediaGroupButton.disabled = true;
            this.createMediaGroupButton.addEventListener('click', async (e) => {
                e.stopPropagation();
                let checkboxes = this.Body.querySelectorAll('[name="rom_item"][data-metadatamapid="' + this.gamePlatformObject.metadataMapId + '"]');
                let romIds = [];
                checkboxes.forEach(checkbox => {
                    if (checkbox.checked) {
                        if (checkbox.getAttribute('name') === 'rom_item') {
                            romIds.push(checkbox.getAttribute('data-romid'));
                        }
                    }
                });

                // create the media group
                fetch(`/api/v1.1/Games/${this.gamePlatformObject.metadataMapId}/romgroup`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(romIds)
                }).then(response => response.json()).then(data => {
                    if (data) {
                        console.log('Media group created');
                        this.SetEditMode(true);
                        this.Refresh();
                    } else {
                        console.log('Error creating media group');
                    }
                });
            });
            this.managementButtons.appendChild(this.createMediaGroupButton);

            this.deleteButton = document.createElement('button');
            this.deleteButton.classList.add('modal-button');
            this.deleteButton.classList.add('card-romlist-management-button');
            this.deleteButton.innerHTML = 'Delete';
            this.deleteButton.style.display = 'none';
            this.deleteButton.disabled = true;
            this.deleteButton.addEventListener('click', async (e) => {
                e.stopPropagation();

                let checkboxes = this.Body.querySelectorAll('[name="rom_item"][data-metadatamapid="' + this.gamePlatformObject.metadataMapId + '"]');

                // create a delete dialog
                let deleteDialog = new MessageBox('Delete Selected ROMs and Media Groups', 'Are you sure you want to delete the selected ROMs and Media Groups?');
                let deleteDialogDeleteButton = new ModalButton('Delete', 2, this, async (e) => {
                    checkboxes.forEach(checkbox => {
                        if (checkbox.checked) {
                            let deleteUrl = '';
                            if (checkbox.getAttribute('data-ismediagroup') === '0') {
                                deleteUrl = `/api/v1.1/Games/${this.gamePlatformObject.metadataMapId}/roms/${checkbox.getAttribute('data-romid')}`;
                            } else {
                                deleteUrl = `/api/v1.1/Games/${this.gamePlatformObject.metadataMapId}/romgroup/${checkbox.getAttribute('data-romid')}`;
                            }
                            fetch(deleteUrl, {
                                method: 'DELETE',
                                headers: {
                                    'Content-Type': 'application/json'
                                }
                            }).then(response => response.json()).then(data => {
                                if (data) {
                                    console.log('Deleted');

                                    this.SetEditMode(true);
                                    this.Refresh();
                                } else {
                                    console.log('Error deleting');
                                }
                            });
                        }
                    });

                    deleteDialog.msgDialog.close();
                });
                deleteDialog.addButton(deleteDialogDeleteButton);

                let deleteDialogCancelButton = new ModalButton('Cancel', 0, this, async (e) => {
                    deleteDialog.msgDialog.close();
                });
                deleteDialog.addButton(deleteDialogCancelButton);

                deleteDialog.open();
            });
            this.managementButtons.appendChild(this.deleteButton);

            this.editButton = document.createElement('button');
            this.editButton.classList.add('modal-button');
            this.editButton.classList.add('card-romlist-management-button');
            this.editButton.innerHTML = 'Edit';
            this.editButton.addEventListener('click', async (e) => {
                e.stopPropagation();
                this.SetEditMode();
            });
            this.managementButtons.appendChild(this.editButton);
        }

        // create the metadata mapping button
        if (userProfile.roles.includes("Admin")) {
            this.metadataMappingButton = document.createElement('button');
            this.metadataMappingButton.classList.add('modal-button');
            this.metadataMappingButton.classList.add('card-romlist-management-button');
            this.metadataMappingButton.innerHTML = 'Metadata';
            this.metadataMappingButton.addEventListener('click', async (e) => {
                e.stopPropagation();
                this.ShowMetadataMappingModal();
            });
            this.managementButtons.appendChild(this.metadataMappingButton);
        }

        // create the configure emulator button
        this.configureEmulatorButton = document.createElement('button');
        this.configureEmulatorButton.classList.add('modal-button');
        this.configureEmulatorButton.classList.add('card-romlist-management-button');
        this.configureEmulatorButton.innerHTML = 'Emulator';
        this.configureEmulatorButton.addEventListener('click', async (e) => {
            e.stopPropagation();
            this.ShowEmulatorConfigureModal();
        });
        this.managementButtons.appendChild(this.configureEmulatorButton);
    }

    SetEditMode(mode) {
        if (mode !== undefined) {
            this.editMode = mode;
        }

        let favIconDisplay;
        let checkboxDisplay;
        if (this.editMode === false || this.editMode === undefined) {
            this.editMode = true;
            favIconDisplay = 'none';
            checkboxDisplay = 'block';
            this.createMediaGroupButton.style.display = '';
            this.deleteButton.style.display = '';
            this.metadataMappingButton.style.display = 'none';
            this.configureEmulatorButton.style.display = 'none';
            this.editButton.innerHTML = 'Done';
        } else {
            this.editMode = false;
            favIconDisplay = 'block';
            checkboxDisplay = 'none';
            this.createMediaGroupButton.style.display = 'none';
            this.deleteButton.style.display = 'none';
            this.metadataMappingButton.style.display = '';
            this.configureEmulatorButton.style.display = '';
            this.editButton.innerHTML = 'Edit';
        }

        // hide all favourite buttons for this rom list
        let romFavButtons = document.querySelectorAll('[name="rom_favourite"][data-metadatamapid="' + this.gamePlatformObject.metadataMapId + '"]');
        romFavButtons.forEach(button => {
            button.style.display = favIconDisplay;
        });
        // show the selection checkboxes
        let romItemCheckboxes = document.querySelectorAll('[name="rom_item"][data-metadatamapid="' + this.gamePlatformObject.metadataMapId + '"]');
        romItemCheckboxes.forEach(checkbox => {
            checkbox.checked = false;
            checkbox.style.display = checkboxDisplay;
        });
    }

    MediaGroupState = undefined;
    RomListState = undefined;

    LoadMediaGroups() {
        fetch(`/api/v1.1/Games/${this.gamePlatformObject.metadataMapId}/romgroup`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(response => response.json()).then(data => {
            if (data) {
                let forceUpdate = false;
                let refreshNeeded = false;
                if (this.MediaGroupState === undefined) {
                    this.MediaGroupState = data;
                    forceUpdate = true;
                } else {
                    // check each element in data against the MediaGroupState
                    // if any ids are missing then force an update
                    // if there are any extra ids then force an update
                    // if ids match, but the status is different, then force an update
                    let missingIds = [];
                    let extraIds = [];
                    let statusChangedIds = [];
                    data.forEach(element => {
                        let found = false;
                        this.MediaGroupState.forEach(stateElement => {
                            if (element.id === stateElement.id) {
                                found = true;
                                if (element.status !== stateElement.status) {
                                    statusChangedIds.push(element.id);
                                }

                                if (element.status !== "Completed") {
                                    console.log("Update refresh required due to status not being completed");
                                    refreshNeeded = true;
                                }
                            }
                        });
                        if (!found) {
                            missingIds.push(element.id);
                        }
                    });
                    this.MediaGroupState.forEach(stateElement => {
                        let found = false;
                        data.forEach(element => {
                            if (element.id === stateElement.id) {
                                found = true;
                            }
                        });
                        if (!found) {
                            extraIds.push(stateElement.id);
                        }
                    });
                    if (missingIds.length > 0 || extraIds.length > 0 || statusChangedIds.length > 0) {
                        console.log("Update refresh required due to missing or extra ids or status changed");
                        this.MediaGroupState = data;
                        forceUpdate = true;
                    }
                }

                console.log(data);

                if (forceUpdate === true) {
                    // clear the media group container
                    this.mediaGroupContainer.innerHTML = '';

                    data.forEach(element => {
                        let mediaGroupItem = document.createElement('div');
                        mediaGroupItem.classList.add('card-romlist-item');
                        mediaGroupItem.classList.add('card-romlist-item-media');

                        // create the left div
                        let leftDiv = document.createElement('div');
                        leftDiv.classList.add('card-romlist-item-left');
                        mediaGroupItem.appendChild(leftDiv);

                        // create the item selection checkbox
                        let romItemCheckbox = document.createElement('input');
                        romItemCheckbox.type = 'checkbox';
                        romItemCheckbox.id = 'rommg_item_check_' + element.id;
                        romItemCheckbox.classList.add('card-romlist-checkbox');
                        romItemCheckbox.setAttribute('name', 'rom_item');
                        romItemCheckbox.setAttribute('data-metadataMapId', this.gamePlatformObject.metadataMapId);
                        romItemCheckbox.setAttribute('data-platformId', element.platformId);
                        romItemCheckbox.setAttribute('data-romid', element.id);
                        romItemCheckbox.setAttribute('data-ismediagroup', '1');
                        romItemCheckbox.style.display = 'none';
                        romItemCheckbox.addEventListener('click', async (e) => {
                            e.stopPropagation();
                            let checkboxes = this.Body.querySelectorAll('[name="rom_item"][data-metadatamapid="' + this.gamePlatformObject.metadataMapId + '"]');
                            this.deleteButton.disabled = true;
                            this.deleteButton.classList.remove('redbutton');
                            this.createMediaGroupButton.disabled = true;
                            let checkedMediaGroupCount = 0;
                            let checkedRomCount = 0;
                            checkboxes.forEach(checkbox => {
                                if (checkbox.checked === true) {
                                    this.deleteButton.disabled = false;
                                    this.deleteButton.classList.add('redbutton');

                                    if (checkbox.getAttribute('data-ismediagroup') === '1') {
                                        checkedMediaGroupCount++;
                                    } else if (checkbox.getAttribute('data-ismediagroup') === '0') {
                                        checkedRomCount++;
                                    }
                                }
                            });
                            if (checkedMediaGroupCount > 0) {
                                this.createMediaGroupButton.disabled = true;
                            } else if (checkedRomCount >= 2) {
                                this.createMediaGroupButton.disabled = false;
                            } else {
                                this.createMediaGroupButton.disabled = true;
                            }
                        });
                        leftDiv.appendChild(romItemCheckbox);

                        // create the rom favourite/last used button
                        let romFavButton = document.createElement('div');
                        romFavButton.classList.add('platform_edit_button');
                        romFavButton.setAttribute('name', 'rom_favourite');
                        romFavButton.setAttribute('data-metadataMapId', this.gamePlatformObject.metadataMapId);
                        if (element.romUserFavourite === false) {
                            romFavButton.innerHTML = '<img src="/images/favourite-empty.svg" class="banner_button_image" />';
                        } else {
                            romFavButton.innerHTML = '<img src="/images/favourite-filled.svg" class="banner_button_image" />';
                        }
                        romFavButton.addEventListener('click', async (e) => {
                            e.stopPropagation();
                            fetch(`/api/v1.1/Games/${this.gamePlatformObject.metadataMapId}/roms/${element.id}/${element.platformId}/favourite?favourite=true&isMediaGroup=true`, {
                                method: 'POST',
                                headers: {
                                    'Content-Type': 'application/json'
                                }
                            }).then(response => response.json());
                            romFavButton.innerHTML = '<img src="/images/favourite-filled.svg" class="banner_button_image" />';

                            // set all other roms to not favourite
                            let romFavButtons = document.querySelectorAll('[name="' + romFavButton.getAttribute('name') + '"][data-metadataMapId="' + romFavButton.getAttribute('data-metadataMapId') + '"]');
                            romFavButtons.forEach(button => {
                                if (button !== romFavButton) {
                                    button.innerHTML = '<img src="/images/favourite-empty.svg" class="banner_button_image" />';
                                }
                            });
                        });
                        leftDiv.appendChild(romFavButton);

                        // create the label container
                        let romName = document.createElement('label');
                        romName.setAttribute('for', 'rommg_item_check_' + element.id);
                        romName.classList.add('card-romlist-labels');
                        leftDiv.appendChild(romName);

                        // create the label
                        let romLabel = document.createElement('div');
                        romLabel.classList.add('card-romlist-name');
                        romName.appendChild(romLabel);

                        if (element.status != "Completed" && element.status != "Error") {
                            romLabel.innerHTML = element.status;

                            // create a timeout to reload the media group list
                            setTimeout(() => {
                                this.LoadMediaGroups();
                            }, 5000);
                        } else {
                            let labelText = '';
                            element.roms.forEach(rom => {
                                if (labelText.length > 0) {
                                    labelText += '<br />';
                                }
                                labelText += rom.name;
                            });
                            romLabel.innerHTML = labelText;
                        }

                        // create the size label
                        let romSize = document.createElement('div');
                        romSize.classList.add('card-romlist-size');
                        if (element.size !== undefined && element.size !== null) {
                            romSize.innerHTML = formatBytes(element.size);
                            romName.appendChild(romSize);
                        }

                        // create the right div
                        let rightDiv = document.createElement('div');
                        rightDiv.classList.add('card-romlist-item-right');
                        mediaGroupItem.appendChild(rightDiv);

                        if (element.status === "Completed") {
                            // create the download button
                            let downloadButton = document.createElement('div');
                            downloadButton.classList.add('platform_edit_button');
                            downloadButton.innerHTML = '<img src="/images/download.svg" class="banner_button_image" />';
                            downloadButton.addEventListener('click', async (e) => {
                                e.stopPropagation();
                                let downloadLink = `/api/v1.1/Games/${this.gamePlatformObject.metadataMapId}/romgroup/${element.id}/${element.roms[0].game}.zip`;
                                if (downloadLink === null) {
                                    console.log('Error: Unable to validate download link');
                                    console.log(element);
                                } else {
                                    // launch the game
                                }
                                window.location.href = downloadLink;
                            });
                            rightDiv.appendChild(downloadButton);

                            // create the save state manager button
                            let platformStateManagerButton = document.createElement('div');
                            platformStateManagerButton.className = 'platform_edit_button platform_statemanager_button';
                            platformStateManagerButton.innerHTML = '<img src="/images/SaveStates.png" class="savedstatemanagericon" />';
                            platformStateManagerButton.addEventListener('click', (e) => {
                                e.stopPropagation();
                                console.log('RomID: ' + element.id + ' isMediaGroup: ' + true);
                                let stateManager = new EmulatorStateManager(element.id, true, this.gamePlatformObject.emulatorConfiguration.emulatorType, this.gamePlatformObject.emulatorConfiguration.core, element.platformId, element.metadataMapId, element.name);
                                stateManager.open();
                            });
                            rightDiv.appendChild(platformStateManagerButton);

                            // create the play button
                            let playButton = document.createElement('div');
                            playButton.classList.add('platform_edit_button');
                            playButton.classList.add('platform_item_green');
                            playButton.innerHTML = '<img src="/images/play.svg" class="banner_button_image" />';
                            playButton.addEventListener('click', async (e) => {
                                e.stopPropagation();

                                // create launch object
                                let launchObject = {
                                    "emulatorConfiguration": this.gamePlatformObject.emulatorConfiguration,
                                    "id": this.gamePlatformObject.id,
                                    "metadataMapId": this.gamePlatformObject.metadataMapId,
                                    "romId": element.id,
                                    "romName": this.gamePlatformObject.name,
                                    "isMediaGroup": true
                                };

                                let launchLink = await BuildGameLaunchLink(launchObject);
                                if (launchLink === null) {
                                    console.log('Error: Unable to validate launch link');
                                    console.log(element);
                                } else {
                                    // launch the game
                                    window.location.href = launchLink;
                                }
                            });
                            rightDiv.appendChild(playButton);
                        }

                        this.mediaGroupContainer.appendChild(mediaGroupItem);
                    });
                } else if (refreshNeeded === true) {
                    // create a timeout to reload the media group list
                    setTimeout(() => {
                        this.LoadMediaGroups();
                    }, 5000);
                }
            }
        });
    }

    LoadRoms() {
        fetch(`/api/v1.1/Games/${this.gamePlatformObject.metadataMapId}/roms?platformId=${this.gamePlatformObject.id}&pageNumber=0&pageSize=0`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(response => response.json()).then(data => {
            console.log(data);
            if (data.gameRomItems) {
                data.gameRomItems.forEach(element => {
                    let romItem = document.createElement('div');
                    romItem.id = 'rom_item_' + element.id;
                    romItem.classList.add('card-romlist-item');
                    romItem.setAttribute('data-metadataMapId', element.metadataMapId);
                    romItem.setAttribute('data-platformId', element.platformId);
                    romItem.setAttribute('data-romid', element.id);
                    romItem.setAttribute('data-ismediagroup', '0');

                    // create the left div
                    let leftDiv = document.createElement('div');
                    leftDiv.classList.add('card-romlist-item-left');
                    romItem.appendChild(leftDiv);

                    // create the item selection checkbox
                    let romItemCheckbox = document.createElement('input');
                    romItemCheckbox.type = 'checkbox';
                    romItemCheckbox.id = 'rom_item_check_' + element.id;
                    romItemCheckbox.classList.add('card-romlist-checkbox');
                    romItemCheckbox.setAttribute('name', 'rom_item');
                    romItemCheckbox.setAttribute('data-metadataMapId', element.metadataMapId);
                    romItemCheckbox.setAttribute('data-platformId', element.platformId);
                    romItemCheckbox.setAttribute('data-romid', element.id);
                    romItemCheckbox.setAttribute('data-ismediagroup', '0');
                    romItemCheckbox.style.display = 'none';
                    romItemCheckbox.addEventListener('click', async (e) => {
                        e.stopPropagation();
                        let checkboxes = this.Body.querySelectorAll('[name="rom_item"][data-metadatamapid="' + element.metadataMapId + '"]');
                        this.deleteButton.disabled = true;
                        this.deleteButton.classList.remove('redbutton');
                        this.createMediaGroupButton.disabled = true;
                        let checkedMediaGroupCount = 0;
                        let checkedRomCount = 0;
                        checkboxes.forEach(checkbox => {
                            if (checkbox.checked === true) {
                                this.deleteButton.disabled = false;
                                this.deleteButton.classList.add('redbutton');

                                if (checkbox.getAttribute('data-ismediagroup') === '1') {
                                    checkedMediaGroupCount++;
                                } else if (checkbox.getAttribute('data-ismediagroup') === '0') {
                                    checkedRomCount++;
                                }
                            }
                        });
                        if (checkedMediaGroupCount > 0) {
                            this.createMediaGroupButton.disabled = true;
                        } else if (checkedRomCount >= 2) {
                            this.createMediaGroupButton.disabled = false;
                        } else {
                            this.createMediaGroupButton.disabled = true;
                        }
                    });
                    leftDiv.appendChild(romItemCheckbox);

                    // create the rom favourite/last used button
                    let romFavButton = document.createElement('div');
                    romFavButton.classList.add('platform_edit_button');
                    romFavButton.setAttribute('name', 'rom_favourite');
                    romFavButton.setAttribute('data-metadataMapId', element.metadataMapId);
                    if (element.romUserFavourite === false) {
                        romFavButton.innerHTML = '<img src="/images/favourite-empty.svg" class="banner_button_image" />';
                    } else {
                        romFavButton.innerHTML = '<img src="/images/favourite-filled.svg" class="banner_button_image" />';
                    }
                    romFavButton.addEventListener('click', async (e) => {
                        e.stopPropagation();
                        fetch(`/api/v1.1/Games/${element.metadataMapId}/roms/${element.id}/${element.platformId}/favourite?favourite=true&isMediaGroup=false`, {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json'
                            }
                        }).then(response => response.json());
                        romFavButton.innerHTML = '<img src="/images/favourite-filled.svg" class="banner_button_image" />';

                        // set all other roms to not favourite
                        let romFavButtons = document.querySelectorAll('[name="' + romFavButton.getAttribute('name') + '"][data-metadataMapId="' + romFavButton.getAttribute('data-metadataMapId') + '"]');
                        romFavButtons.forEach(button => {
                            if (button !== romFavButton) {
                                button.innerHTML = '<img src="/images/favourite-empty.svg" class="banner_button_image" />';
                            }
                        });
                    });
                    leftDiv.appendChild(romFavButton);

                    // create the label container
                    let romName = document.createElement('label');
                    romName.setAttribute('for', 'rom_item_check_' + element.id);
                    romName.classList.add('card-romlist-labels');
                    leftDiv.appendChild(romName);

                    // create the rom name
                    let romNameLabel = document.createElement('div');
                    romNameLabel.classList.add('card-romlist-name');
                    romNameLabel.innerHTML = element.name;
                    romName.appendChild(romNameLabel);

                    // create the rom size
                    let romSizeLabel = document.createElement('div');
                    romSizeLabel.classList.add('card-romlist-size');
                    romSizeLabel.innerHTML = 'Size: ' + formatBytes(element.size);
                    romName.appendChild(romSizeLabel);

                    // create the rom type
                    if (element.romTypeMedia) {
                        let romTypeLabel = document.createElement('div');
                        romTypeLabel.classList.add('card-romlist-type');
                        romTypeLabel.innerHTML = 'Media: ' + element.romTypeMedia;
                        romName.appendChild(romTypeLabel);
                    }

                    // create last used label
                    if (element.romUserLastUsed) {
                        let lastUsedLabel = document.createElement('div');
                        lastUsedLabel.classList.add('card-romlist-lastused');
                        lastUsedLabel.innerHTML = 'Most recently used ROM';
                        romName.appendChild(lastUsedLabel);
                    }

                    // create the right div
                    let rightDiv = document.createElement('div');
                    rightDiv.classList.add('card-romlist-item-right');
                    romItem.appendChild(rightDiv);

                    // create the info button
                    let infoButton = document.createElement('div');
                    infoButton.classList.add('platform_edit_button');
                    infoButton.innerHTML = '<img src="/images/info.svg" class="banner_button_image" />';
                    infoButton.addEventListener('click', async (e) => {
                        e.stopPropagation();
                        const romInfoDialog = new rominfodialog(element.metadataMapId, element.id);
                        romInfoDialog.open();
                    });
                    rightDiv.appendChild(infoButton);

                    // create the download button
                    let downloadButton = document.createElement('div');
                    downloadButton.classList.add('platform_edit_button');
                    downloadButton.innerHTML = '<img src="/images/download.svg" class="banner_button_image" />';
                    downloadButton.addEventListener('click', async (e) => {
                        e.stopPropagation();
                        let downloadLink = `/api/v1.1/Games/${this.gamePlatformObject.metadataMapId}/roms/${element.id}/${element.name}`;
                        if (downloadLink === null) {
                            console.log('Error: Unable to validate download link');
                            console.log(element);
                        }
                        else {
                            // launch the game
                            window.location.href = downloadLink;
                        }
                    });
                    rightDiv.appendChild(downloadButton);

                    // create the save state manager button
                    let platformStateManagerButton = document.createElement('div');
                    platformStateManagerButton.className = 'platform_edit_button platform_statemanager_button';
                    platformStateManagerButton.innerHTML = '<img src="/images/SaveStates.png" class="savedstatemanagericon" />';
                    platformStateManagerButton.addEventListener('click', (e) => {
                        e.stopPropagation();
                        console.log('RomID: ' + element.id + ' isMediaGroup: ' + false);
                        let stateManager = new EmulatorStateManager(element.id, false, this.gamePlatformObject.emulatorConfiguration.emulatorType, this.gamePlatformObject.emulatorConfiguration.core, element.platformId, element.metadataMapId, element.name);
                        stateManager.open();
                    });
                    rightDiv.appendChild(platformStateManagerButton);

                    // create the play button
                    let playButton = document.createElement('div');
                    playButton.classList.add('platform_edit_button');
                    playButton.classList.add('platform_item_green');
                    playButton.innerHTML = '<img src="/images/play.svg" class="banner_button_image" />';
                    playButton.addEventListener('click', async (e) => {
                        e.stopPropagation();

                        // create launch object
                        let launchObject = {
                            "emulatorConfiguration": this.gamePlatformObject.emulatorConfiguration,
                            "id": this.gamePlatformObject.id,
                            "metadataMapId": this.gamePlatformObject.metadataMapId,
                            "romId": element.id,
                            "romName": element.name,
                            "isMediaGroup": false
                        };

                        let launchLink = await BuildGameLaunchLink(launchObject);
                        if (launchLink === null) {
                            console.log('Error: Unable to validate launch link');
                            console.log(element);
                        } else {
                            // launch the game
                            window.location.href = launchLink;
                        }
                    });
                    rightDiv.appendChild(playButton);

                    this.romListContainer.appendChild(romItem);
                });
            }
        });
    }

    Refresh() {
        this.mediaGroupContainer.innerHTML = '';
        this.romListContainer.innerHTML = '';
        this.LoadMediaGroups();
        this.LoadRoms();
    }

    async ShowMetadataMappingModal() {
        let metadataModal = await new Modal('messagebox');
        await metadataModal.BuildModal();

        // override the dialog size
        // metadataModal.modalElement.style = 'width: 600px; height: 400px; min-width: unset; min-height: 400px; max-width: unset; max-height: 400px;';
        metadataModal.modalElement.classList.add('modal-metadataconfiguration');

        // set the title
        metadataModal.modalElement.querySelector('#modal-header-text').innerHTML = this.gamePlatformObject.name + ' Metadata Mapping';

        // set the content
        let metadataContent = metadataModal.modalElement.querySelector('#modal-body');

        // fetch the metadata map
        let metadataMap = await fetch('/api/v1.1/Games/' + this.gamePlatformObject.metadataMapId + '/metadata', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(response => response.json());

        metadataMap.metadataMapItems.forEach(element => {
            let itemSection = document.createElement('div');
            itemSection.className = 'section';

            // header
            let itemSectionHeader = document.createElement('div');
            itemSectionHeader.className = 'section-header';

            let itemSectionHeaderRadio = document.createElement('input');
            itemSectionHeaderRadio.id = 'platformMappingSource_' + element.sourceType;
            itemSectionHeaderRadio.type = 'radio';
            itemSectionHeaderRadio.name = 'platformMappingSource';
            itemSectionHeaderRadio.value = element.sourceType;
            itemSectionHeaderRadio.style.margin = '0px';
            itemSectionHeaderRadio.style.height = 'unset';
            itemSectionHeaderRadio.addEventListener('change', () => {
                metadataMap.metadataMapItems.forEach(element => {
                    element.preferred = false;
                });

                element.preferred = true;
                console.log('Selected: ' + element.sourceType);
                console.log(metadataMap);
            });
            if (element.preferred == true) {
                itemSectionHeaderRadio.checked = true;
            }
            itemSectionHeader.appendChild(itemSectionHeaderRadio);

            let itemSectionHeaderLabel = document.createElement('label');
            itemSectionHeaderLabel.htmlFor = 'platformMappingSource_' + element.sourceType;
            itemSectionHeaderLabel.style.marginLeft = '10px';
            itemSectionHeaderLabel.innerHTML = element.sourceType;
            itemSectionHeader.appendChild(itemSectionHeaderLabel);

            itemSection.appendChild(itemSectionHeader);

            // content
            let itemSectionContent = document.createElement('div');
            itemSectionContent.className = 'section-body';
            switch (element.sourceType) {
                case 'None':
                    let noneContent = document.createElement('div');
                    noneContent.className = 'section-body-content';

                    let noneContentLabel = document.createElement('label');
                    noneContentLabel.innerHTML = 'No Metadata Source';
                    noneContent.appendChild(noneContentLabel);

                    itemSectionContent.appendChild(noneContent);
                    break;

                default:
                    let contentLabel2 = document.createElement('div');
                    contentLabel2.innerHTML = 'ID: ' + element.sourceId;
                    itemSectionContent.appendChild(contentLabel2);

                    let contentLabel3 = document.createElement('div');
                    contentLabel3.innerHTML = 'Slug: ' + element.sourceSlug;
                    itemSectionContent.appendChild(contentLabel3);

                    if (element.link) {
                        if (element.link.length > 0) {
                            let contentLabel4 = document.createElement('div');
                            contentLabel4.innerHTML = 'Link: <a href="' + element.link + '" target="_blank" rel="noopener noreferrer" class="romlink">' + element.link + '</a>';
                            itemSectionContent.appendChild(contentLabel4);
                        }
                    }
                    break;

            }
            itemSection.appendChild(itemSectionContent);

            metadataContent.appendChild(itemSection);
        });


        // setup the buttons
        let okButton = new ModalButton('OK', 1, this.gamePlatformObject, async function (callingObject) {
            let model = metadataMap.metadataMapItems;

            await fetch('/api/v1.1/Games/' + callingObject.metadataMapId + '/metadata', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(model)
            }).then(response => response.json()).then(result => {
                location.reload(true);
            });
        });
        metadataModal.addButton(okButton);

        let cancelButton = new ModalButton('Cancel', 0, metadataModal, async function (callingObject) {
            metadataModal.close();
        });
        metadataModal.addButton(cancelButton);

        // show the dialog
        await metadataModal.open();
    }

    async ShowEmulatorConfigureModal() {
        let mappingModal = await new Modal('messagebox');
        await mappingModal.BuildModal();

        // override the dialog size
        mappingModal.modalElement.classList.add('modal-emulatorconfiguration');

        // get the platform map
        let platformMap = await fetch('/api/v1.1/PlatformMaps/' + this.gamePlatformObject.id, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(response => response.json());
        let defaultPlatformMap = platformMap;

        // get the user emulation configuration
        let userEmuConfig = await fetch('/api/v1.1/Games/' + this.gamePlatformObject.metadataMapId + '/emulatorconfiguration/' + this.gamePlatformObject.id, {
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
        mappingModal.modalElement.querySelector('#modal-header-text').innerHTML = this.gamePlatformObject.name + ' Emulation Settings';

        // set the content
        let mappingContent = mappingModal.modalElement.querySelector('#modal-body');
        mappingContent.innerHTML = '';
        let emuConfig = await new WebEmulatorConfiguration(platformMap)
        emuConfig.open();
        mappingContent.appendChild(emuConfig.panel);

        // setup the buttons
        let resetButton = new ModalButton('Reset to Default', 0, this, async function (callingObject) {
            await fetch('/api/v1.1/Games/' + callingObject.gamePlatformObject.metadataMapId + '/emulatorconfiguration/' + callingObject.gamePlatformObject.id, {
                method: 'DELETE'
            });
            callingObject.gamePlatformObject.emulatorConfiguration.emulatorType = defaultPlatformMap.webEmulator.type;
            callingObject.gamePlatformObject.emulatorConfiguration.core = defaultPlatformMap.webEmulator.core;
            callingObject.gamePlatformObject.emulatorConfiguration.enabledBIOSHashes = defaultPlatformMap.enabledBIOSHashes;
            callingObject.Refresh();
            mappingModal.close();
        });
        mappingModal.addButton(resetButton);

        let okButton = new ModalButton('OK', 1, this, async function (callingObject) {
            let model = {
                EmulatorType: emuConfig.PlatformMap.webEmulator.type,
                Core: emuConfig.PlatformMap.webEmulator.core,
                EnableBIOSFiles: emuConfig.PlatformMap.enabledBIOSHashes
            }

            await fetch('/api/v1.1/Games/' + callingObject.gamePlatformObject.metadataMapId + '/emulatorconfiguration/' + callingObject.gamePlatformObject.id, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(model)
            });
            callingObject.gamePlatformObject.emulatorConfiguration.emulatorType = emuConfig.PlatformMap.webEmulator.type;
            callingObject.gamePlatformObject.emulatorConfiguration.core = emuConfig.PlatformMap.webEmulator.core;
            callingObject.gamePlatformObject.emulatorConfiguration.enabledBIOSHashes = emuConfig.PlatformMap.enabledBIOSHashes;

            callingObject.Refresh();
            mappingModal.close();
        });
        mappingModal.addButton(okButton);

        let cancelButton = new ModalButton('Cancel', 0, mappingModal, async function (callingObject) {
            mappingModal.close();
        });
        mappingModal.addButton(cancelButton);

        // show the dialog
        await mappingModal.open();
    }
}