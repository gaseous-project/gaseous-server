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

        // add the card body
        this.cardBody = document.createElement("div");
        this.cardBody.classList.add("card-body");
        this.cardContent.appendChild(this.cardBody);

        // add the card header
        this.cardHeader = document.createElement("div");
        this.cardHeader.classList.add("card-header");
        this.card.appendChild(this.cardHeader);

        // add the back button
        this.backButton = document.createElement("div");
        this.backButton.classList.add("card-back-button");
        this.backButton.innerHTML = "&larr;";
        this.card.appendChild(this.backButton);

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
    }

    async BuildCard() {
        // Load the content from the HTML file
        const response = await fetch("/pages/cards/" + this.cardType + ".html");
        const content = await response.text();

        this.cardBody.innerHTML = content;
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
            let rgbSlightlyBrighter = {
                r: Math.min(rgbAverage.r + 20, 255),
                g: Math.min(rgbAverage.g + 20, 255),
                b: Math.min(rgbAverage.b + 20, 255)
            };
            let rgbMuchBrighter = {
                r: Math.min(rgbAverage.r + 50, 255),
                g: Math.min(rgbAverage.g + 50, 255),
                b: Math.min(rgbAverage.b + 50, 255)
            };
            this.card.style.background = 'rgb(' + rgbAverage.r + ', ' + rgbAverage.g + ', ' + rgbAverage.b + ')';
            this.cardGradient.style.background = 'linear-gradient(180deg, rgba(0, 0, 0, 0) 0%, rgba(' + rgbAverage.r + ', ' + rgbAverage.g + ', ' + rgbAverage.b + ', 1) 100%)';
            if (blur === true) {
                this.cardBackgroundContainer.classList.add('card-background-blurred');
            }

            // set the font colour to a contrasting colour
            let contrastColour = contrastingColor(rgbToHex(rgbAverage.r, rgbAverage.g, rgbAverage.b).replace("#", ""));
            // this.card.style.color = '#' + contrastColour;
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
        await this.card.BuildCard();

        // store the card object in the session storage
        sessionStorage.setItem("Card." + this.card.cardType + ".Id", this.gameId);

        // set up content tabs
        this.contentTabs = this.card.cardBody.querySelector('#card-tabs');
        this.contentContents = this.card.cardBody.querySelector("#card-contents");

        // fetch the game data
        const response = await fetch("/api/v1.1/Games/" + this.gameId, {
            method: "GET",
            headers: {
                "Content-Type": "application/json"
            }
        });
        this.gameData = await response.json();
        this.metadataSource = this.gameData.metadataSource;

        // dump the game data to the console for debugging
        console.log(this.gameData);

        // set the header
        this.card.SetHeader(this.gameData.name, false);

        // set the background image
        if (this.gameData.artworks && this.gameData.artworks.length > 0) {
            // // randomly select an artwork to display
            let randomIndex = Math.floor(Math.random() * this.gameData.artworks.length);
            let artwork = this.gameData.artworks[randomIndex];
            // let artwork = this.gameData.artworks[0];
            let artworkUrl = `/api/v1.1/Games/${this.gameId}/${this.gameData.metadataSource}/artwork/${artwork}/image/original/${artwork}.jpg`;
            this.card.SetBackgroundImage(artworkUrl, true, () => {
                if (this.card.contrastColour !== 'fff') {
                    let ratingIgdbLogo = this.card.cardBody.querySelector('#card-userrating-igdb-logo');
                    ratingIgdbLogo.classList.add('card-info-rating-icon-black');
                }
            });
        } else if (this.gameData.screenshots && this.gameData.screenshots.length > 0) {
            // randomly select a screenshot to display
            let randomIndex = Math.floor(Math.random() * this.gameData.screenshots.length);
            let screenshot = this.gameData.screenshots[randomIndex];
            let screenshotUrl = `/api/v1.1/Games/${this.gameId}/${this.gameData.metadataSource}/screenshots/${screenshot}/image/original/${screenshot}.jpg`;
            this.card.SetBackgroundImage(screenshotUrl, true, () => {
                if (this.card.contrastColour !== 'fff') {
                    let ratingIgdbLogo = this.card.cardBody.querySelector('#card-userrating-igdb-logo');
                    ratingIgdbLogo.classList.add('card-info-rating-icon-black');
                }
            });
        } else if (this.gameData.cover) {
            let coverUrl = `/api/v1.1/Games/${this.gameId}/${this.gameData.metadataSource}/cover/${this.gameData.cover}/image/original/${this.gameData.cover}.jpg`;
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
        switch (this.gameData.metadataSource) {
            case "IGDB":
                cardAttribution.innerHTML = `Data provided by ${this.gameData.metadataSource}. <a href="https://www.igdb.com/games/${this.gameData.slug}" class="romlink" target="_blank" rel="noopener noreferrer">Source</a>`;
                cardAttribution.style.display = '';
                break;

            case "TheGamesDb":
                cardAttribution.innerHTML = `Data provided by ${this.gameData.metadataSource}. <a href="https://thegamesdb.net/game.php?id=${this.gameData.id}" class="romlink" target="_blank" rel="noopener noreferrer">Source</a>`;
                cardAttribution.style.display = '';
                break;
        }

        // set the cover art
        let clearLogoValid = false;
        if (this.gameData.clearLogo) {
            for (const provider of logoProviders) {
                if (this.gameData.clearLogo[provider] !== undefined) {
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
                    if (this.gameData.clearLogo[provider] !== undefined) {
                        let providerIds = this.gameData.clearLogo[provider];
                        let providerId = null;
                        // check if providerIds is an array
                        if (Array.isArray(providerIds)) {
                            providerId = providerIds[0];
                        } else {
                            providerId = providerIds;
                        }

                        clearLogoImg.src = `/api/v1.1/Games/${this.gameId}/${provider}/clearlogo/${providerId}/image/original/${providerId}.png`;
                        clearLogoImg.alt = this.gameData.name;
                        clearLogoImg.title = this.gameData.name;
                        clearLogoImg.style.display = '';
                        usingClearLogo = true;

                        cardTitleInfo.classList.add('card-title-info-clearlogo');

                        if (provider !== this.gameData.metadataSource) {
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
                if (this.gameData.cover) {
                    coverImg.src = `/api/v1.1/Games/${this.gameId}/${this.gameData.metadataSource}/cover/${this.gameData.cover}/image/cover_big/${this.gameData.cover}.jpg`;
                } else {
                    coverImg.src = '/images/unknowngame.png';
                }
                coverImg.alt = this.gameData.name;
                coverImg.title = this.gameData.name;
                coverImg.style.display = '';
            }
        }

        // set the game name
        if (!usingClearLogo) {
            let gameName = this.card.cardBody.querySelector('#card-title');
            if (gameName) {
                gameName.innerHTML = this.gameData.name;
                gameName.style.display = '';
            }
        }

        // set the game rating
        let ageRating = this.card.cardBody.querySelector('#card-rating');
        if (this.gameData.age_ratings) {
            fetch(`/api/v1.1/Games/${this.gameId}/${this.gameData.metadataSource}/agerating`, {
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
                                if (ratingElement.toLowerCase() === dataElement.ratingBoard.name.toLowerCase()) {
                                    // find rating in AgeRatingMappings
                                    let organization = null;
                                    let organizationRatingKey = null;
                                    let organizationRating = null;

                                    for (const key of Object.keys(AgeRatingMappings.RatingBoards)) {
                                        if (AgeRatingMappings.RatingBoards[key].IGDBId === dataElement.ratingBoard.id) {
                                            organization = AgeRatingMappings.RatingBoards[key];
                                            for (const ratingKey of Object.keys(AgeRatingMappings.RatingBoards[key].Ratings)) {
                                                if (AgeRatingMappings.RatingBoards[key].Ratings[ratingKey].IGDBId === dataElement.ratingTitle.id) {
                                                    organizationRatingKey = ratingKey;
                                                    organizationRating = AgeRatingMappings.RatingBoards[key].Ratings[ratingKey];
                                                    break;
                                                }
                                            }
                                            break;
                                        }
                                    }

                                    if (organization !== null || organizationRating !== null) {
                                        let rating = document.createElement('div');
                                        rating.classList.add('card-rating');

                                        let ratingIcon = document.createElement('img');
                                        ratingIcon.src = `/images/Ratings/${dataElement.ratingBoard.name.toUpperCase()}/${organizationRating.IconName}.svg`;
                                        ratingIcon.alt = dataElement.ratingTitle;
                                        ratingIcon.title = dataElement.ratingTitle;
                                        ratingIcon.classList.add('card-rating-icon');

                                        let description = AgeRatingMappings.RatingBoards[dataElement.ratingBoard.name].Name + '\nRating: ' + organizationRatingKey;
                                        if (dataElement.descriptions && dataElement.descriptions.length > 0) {
                                            description += '\n\nDescription:';
                                            dataElement.descriptions.forEach(element => {
                                                description += '\n' + element.description;
                                            });
                                        }

                                        ratingIcon.alt = description;
                                        ratingIcon.title = description;

                                        rating.appendChild(ratingIcon);

                                        ageRating.appendChild(rating);
                                        ageRating.style.display = '';

                                        abortLoop = true;
                                    }
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
        if (this.gameData.first_release_date) {
            let relDate = new Date(this.gameData.first_release_date);
            let year = new Intl.DateTimeFormat('en', { year: 'numeric' }).format(relDate);
            let releaseDate = this.card.cardBody.querySelector('#card-releasedate');
            releaseDate.innerHTML = year;
            releaseDate.alt = relDate;
            releaseDate.title = relDate;
            releaseDate.style.display = '';
        }

        // set the developers
        if (this.gameData.involved_companies) {
            fetch(`/api/v1.1/Games/${this.gameId}/${this.gameData.metadataSource}/companies`, {
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
        if (this.gameData.genres) {
            fetch(`/api/v1.1/Games/${this.gameId}/${this.gameData.metadataSource}/genre`, {
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
        if (this.gameData.themes) {
            fetch(`/api/v1.1/Games/${this.gameId}/${this.gameData.metadataSource}/themes`, {
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
        if (this.gameData.total_rating) {
            let rating = this.card.cardBody.querySelector('#card-userrating-igdb-value');
            rating.innerHTML = Math.floor(this.gameData.total_rating) + '%';
            rating.style.display = '';

            let ratingIgdb = this.card.cardBody.querySelector('#card-userrating-igdb');
            ratingIgdb.style.display = '';

            let ratingPanel = this.card.cardBody.querySelector('#card-userratings');
            ratingPanel.style.display = '';
        }

        // set the game summary
        let gameSummary = this.card.cardBody.querySelector('#card-summary');
        let gameSummarySection = this.card.cardBody.querySelector('#card-summary-section');
        if (this.gameData.summary || this.gameData.storyline) {
            if (this.gameData.summary) {
                gameSummary.innerHTML = this.gameData.summary.replaceAll("\n", "<br />");
            } else {
                gameSummary.innerHTML = this.gameData.storyLine.replaceAll("\n", "<br />");
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
        this.metadataIds = [];
        await fetch(`/api/v1.1/Games/${this.gameId}/${this.gameData.metadataSource}/platforms`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(response => response.json()).then(async data => {
            if (data) {
                // sort data by:
                // 1. name (ascending, case-insensitive)
                // 2. lastPlayed (descending - most recent first) when both have a value
                //    Items with a lastPlayed value come before items without one.
                // 3. metadataGameName (ascending, case-insensitive) only when BOTH lastPlayed values are null/undefined
                data.sort((a, b) => {
                    const nameA = (a.name || '').toUpperCase();
                    const nameB = (b.name || '').toUpperCase();

                    if (nameA < nameB) return -1;
                    if (nameA > nameB) return 1;

                    // Names are equal, move to lastPlayed logic
                    const hasLastPlayedA = !!a.lastPlayed;
                    const hasLastPlayedB = !!b.lastPlayed;

                    if (hasLastPlayedA && hasLastPlayedB) {
                        // Both have lastPlayed -> compare dates (newest first)
                        const dateA = new Date(a.lastPlayed);
                        const dateB = new Date(b.lastPlayed);
                        if (!isNaN(dateA) && !isNaN(dateB)) {
                            if (dateA > dateB) return -1;
                            if (dateA < dateB) return 1;
                        } else if (!isNaN(dateA) && isNaN(dateB)) {
                            return -1;
                        } else if (isNaN(dateA) && !isNaN(dateB)) {
                            return 1;
                        }
                        // fall through if equal/invalid -> no return, continue to metadataGameName
                    } else if (hasLastPlayedA && !hasLastPlayedB) {
                        return -1; // a before b
                    } else if (!hasLastPlayedA && hasLastPlayedB) {
                        return 1; // b before a
                    }

                    // Either both missing lastPlayed or both lastPlayed equal -> compare metadataGameName
                    const metaA = (a.metadataGameName || '').toUpperCase();
                    const metaB = (b.metadataGameName || '').toUpperCase();
                    if (metaA < metaB) return -1;
                    if (metaA > metaB) return 1;
                    return 0;
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

                if (mostRecentPlatform?.lastPlayed) {
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
                    this.metadataIds.push(...value.map(v => v.metadataMapId));
                    let platformItem = new GameCardPlatformItem(value[0].name, key);
                    value.forEach(element => {
                        platformItem.Add(element);
                    });

                    let platformItemElement = await platformItem.BuildItem();
                    cardPlatforms.appendChild(platformItemElement);
                }

                // show the platform section
                let platformSection = this.card.cardBody.querySelector('#card-platforms-section');
                platformSection.style.display = '';
            }
        });

        // display the screenshots
        this.screenshotItems = [];
        this.screenshotItems[this.gameData.metadataSource] = [];
        this.screenshotItemsCount = [];
        this.screenshotItemsCount[this.gameData.metadataSource] = 0;
        if (this.gameData.videos && this.gameData.videos.length > 0) {
            this.screenshotItemsCount[this.gameData.metadataSource] += this.gameData.videos.length;

            await fetch(`/api/v1.1/Games/${this.gameId}/${this.gameData.metadataSource}/videos`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            }).then(response => response.json()).then(videoData => {
                if (!videoData) {
                    console.error(`Error fetching video data for game ${this.gameId}`);
                    return;
                }

                videoData.forEach(element => {
                    // create new screenshot item
                    let screenshotItem = new ScreenshotItem(element.video_id, this.gameData.metadataSource, 'youtube', `https://www.youtube.com/watch?v=${element.video_id}`, element.name, null, null, this.gameId);
                    this.screenshotItems[this.gameData.metadataSource].push(screenshotItem);
                });
            });
        }
        if (this.gameData.screenshots) {
            this.screenshotItemsCount[this.gameData.metadataSource] += this.gameData.screenshots.length;

            this.gameData.screenshots.forEach(screenshot => {
                // create new screenshot item
                let screenshotItem = new ScreenshotItem(screenshot, this.gameData.metadataSource, 'screenshot', `/api/v1.1/Games/${this.gameId}/${this.gameData.metadataSource}/screenshots/${screenshot}/image/original/${screenshot}.jpg`, null, null, null, this.gameId);
                this.screenshotItems[this.gameData.metadataSource].push(screenshotItem);
            });
        }

        // get user graphical content
        await this.#LoadUserContent(1);

        // render the screenshot tab contents
        this.RenderScreenshotTabContents();

        // show the card
        this.card.Open();
    }

    async #ShowScreenshots(metadataSource, gameid, selectedImage) {
        let screenshotsDialog = new ScreenshotDisplay(metadataSource, gameid, selectedImage);
        await screenshotsDialog.open();
    }

    // Use an arrow function so that when passed as a callback (e.g. to ScreenshotViewer) it retains the correct
    // lexical 'this' bound to the GameCard instance.
    #LoadUserContent = async (page) => {
        console.log(`Loading page ${page}`);
        await fetch(`/api/v1.1/ContentManager/?metadataids=${this.metadataIds.join(",")}&contentTypes=Screenshot,Photo,Video&page=${page}&pageSize=50`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(response => response.json()).then(data => {
            if (page === 1) {
                this.screenshotItems["My Content"] = [];
                this.screenshotItemsCount["My Content"] = 0;
            }
            if (data) {
                this.screenshotItemsCount["My Content"] = data.totalCount;
                if (data.items.length > 0) {
                    // load content elements
                    data.items.forEach(element => {
                        // create new screenshot item
                        let screenshotItem = new ScreenshotItem(element.attachmentId, 'My Content', element.contentType.toLowerCase(), `/api/v1.1/ContentManager/attachment/${element.attachmentId}/data`, null, element.uploadedAt, null, this.gameId, element.uploadedBy);
                        this.screenshotItems["My Content"].push(screenshotItem);
                    });
                }
                return this.screenshotItemsCount["My Content"];
            }
        });
    }

    RenderScreenshotTabContents(selectedTab) {
        // build the screenshot section
        let firstTab = true;
        this.contentTabs.innerHTML = '';
        this.contentContents.innerHTML = '';

        // if there is nothing in my content, update selectedTab to the metadata source
        if ((this.screenshotItems["My Content"] === undefined || this.screenshotItemsCount["My Content"] === 0)) {
            selectedTab = this.gameData.metadataSource;
        }

        for (const [key, value] of Object.entries(this.screenshotItems)) {
            let tabName = key.toLowerCase().replaceAll(' ', '');

            // skip if there are no items
            if (this.screenshotItemsCount[key] === 0) {
                continue;
            }

            // create the tab
            let tab = document.createElement('div');
            tab.classList.add('card-tab');
            if ((selectedTab !== undefined && selectedTab !== null && key === selectedTab) ||
                (selectedTab === undefined && firstTab)) {
                tab.classList.add('card-tab-selected');
                selectedTab = key;
            }
            tab.id = `card-tabs-${tabName}`;
            tab.setAttribute('data-section', tabName);
            tab.innerHTML = key;
            this.contentTabs.appendChild(tab);

            // create the section
            let section = document.createElement('div');
            section.id = `card-${tabName}-section`;
            if (selectedTab !== key) {
                section.style.display = 'none';
            }
            section.classList.add('card-info-block');
            section.classList.add('card-screenshots');

            // add the screenshots to the container
            let imgCount = 0;
            for (let i = 0; i < value.length; i++) {
                imgCount++;
                let screenshotItem = value[i];
                let previewElement = screenshotItem.createPreviewElement();
                if (this.screenshotItemsCount[key] === 1) {
                    previewElement.classList.add('card-screenshot-item-single');
                } else if (imgCount <= 2) {
                    previewElement.classList.add('card-screenshot-item-double');
                } else {
                    previewElement.classList.add('card-screenshot-item-small');
                }

                previewElement.addEventListener('click', (e) => {
                    e.stopPropagation();
                    let screenshotViewerContent = [];
                    let screenshotViewerContentCount = 0;
                    let closeCallback = undefined;
                    let deleteCallback = undefined;
                    if (key === "My Content") {
                        // provided by user
                        screenshotViewerContent = this.screenshotItems["My Content"];
                        screenshotViewerContentCount = this.screenshotItemsCount["My Content"];
                        deleteCallback = async (screenshotItem) => {
                            if (!screenshotItem) {
                                return;
                            }

                            let id = screenshotItem.id;

                            // delete the item with the specified id from the server
                            let retVal = false;

                            await fetch(`/api/v1.1/ContentManager/attachment/${id}`, {
                                method: 'DELETE',
                                headers: {
                                    'Content-Type': 'application/json'
                                }
                            }).then(response => {
                                if (response.ok) {
                                    retVal = id;
                                }
                            });

                            // refetch the user content
                            await this.#LoadUserContent(1);

                            // re-render the screenshot tab contents
                            this.RenderScreenshotTabContents("My Content");

                            return retVal;
                        };
                    } else {
                        // provided by metadata provider
                        screenshotViewerContent = this.screenshotItems[key];
                        screenshotViewerContentCount = this.screenshotItemsCount[key];
                    }

                    let screenshotViewer = new ScreenshotViewer(screenshotViewerContent, i, screenshotViewerContentCount, this.#LoadUserContent, closeCallback, deleteCallback);
                    // screenshotViewer.GoTo();
                });
                section.appendChild(previewElement);

                if (imgCount >= 7) {
                    // add a counter to show how many more images there are
                    let moreCount = this.screenshotItemsCount[key] - imgCount;
                    if (moreCount > 0) {
                        let moreElement = document.createElement('div');
                        moreElement.classList.add('card-screenshot-item-counter');
                        moreElement.innerHTML = `+${moreCount}`;
                        previewElement.appendChild(moreElement);
                    }

                    break;
                }
            };

            this.contentContents.appendChild(section);

            // add event listeners to the tabs
            tab.addEventListener('click', () => {
                // hide all content sections
                this.card.cardBody.querySelectorAll('[data-section]').forEach(t => {
                    t.classList.remove('card-tab-selected');
                    const sectionName = t.getAttribute('data-section');
                    const section = this.card.cardBody.querySelector(`#card-${sectionName}-section`);
                    if (section) {
                        section.style.display = 'none';
                    }
                });
                // show the selected content section
                tab.classList.add('card-tab-selected');
                const sectionName = tab.getAttribute('data-section');
                const section = this.card.cardBody.querySelector(`#card-${sectionName}-section`);
                if (section) {
                    section.style.display = '';
                }
            });

            firstTab = false;
        }
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

    async BuildItem() {
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
        for (let i = 0; i < this.gameObjects.length; i++) {
            const element = this.gameObjects[i];
            // this.gameObjects.forEach(element => {
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
                let launchLink = await BuildGameLaunchLink(element);
                if (launchLink !== null) {
                    playButton.addEventListener('click', async (e) => {
                        e.stopPropagation();
                        if (launchLink === null) {
                            console.log('Error: Unable to validate launch link');
                            console.log(element);
                        } else {
                            // launch the game
                            window.location.href = launchLink;
                        }
                    });
                    gameItem.appendChild(playButton);
                }
            } else if (firstGameItem) {
                // expand the edit button by default if there is no global play button visible
                if (document.getElementById('card-launchgame').style.display !== '') {
                    expandButton.click();
                }
            }
            firstGameItem = false;

            gameList.appendChild(outerGameItem);
        };

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

        // create a row of tabs for ROMs and Content
        this.contentTabs = document.createElement('div');
        this.contentTabs.classList.add('card-romlist-tabs');

        // create the ROMs tab
        this.romsTab = document.createElement('div');
        this.romsTab.classList.add('card-tab');
        this.romsTab.classList.add('card-tab-selected');
        this.romsTab.id = 'card-romlist-roms-tab';
        this.romsTab.setAttribute('data-section', 'roms');
        this.romsTab.innerHTML = 'ROMs';
        this.contentTabs.appendChild(this.romsTab);

        // create the Content tab
        this.contentTab = document.createElement('div');
        this.contentTab.classList.add('card-tab');
        this.contentTab.id = 'card-romlist-content-tab';
        this.contentTab.setAttribute('data-section', 'content');
        this.contentTab.innerHTML = 'Content';
        this.contentTabs.appendChild(this.contentTab);

        // add event listeners to the tabs
        this.romListTabs = this.contentTabs.querySelectorAll('[data-section]');
        this.romListTabs.forEach(t => {
            t.addEventListener('click', () => {
                // hide all content sections
                this.romListTabs.forEach(tab => {
                    tab.classList.remove('card-tab-selected');
                    const sectionName = tab.getAttribute('data-section');
                    const sections = this.Body.querySelectorAll(`.card-romlist-${sectionName}-section`);
                    if (sections) {
                        sections.forEach(section => {
                            section.style.display = 'none';
                        });
                    }
                });
                // show the selected content section
                t.classList.add('card-tab-selected');
                const sectionName = t.getAttribute('data-section');
                const sections = this.Body.querySelectorAll(`.card-romlist-${sectionName}-section`);
                if (sections) {
                    sections.forEach(section => {
                        section.style.display = '';
                    });
                }
            });
        });

        this.Body.appendChild(this.contentTabs);

        // create the media group container
        this.mediaGroupContainer = document.createElement('div');
        this.mediaGroupContainer.classList.add('card-romlist-group');
        this.mediaGroupContainer.classList.add('card-romlist-group-header');
        this.mediaGroupContainer.classList.add('card-romlist-roms-section');
        this.Body.appendChild(this.mediaGroupContainer);
        this.Body.style.display = 'none';
        this.LoadMediaGroups();

        // create the rom list container
        this.romListContainer = document.createElement('div');
        this.romListContainer.classList.add('card-romlist-group');
        this.romListContainer.classList.add('card-romlist-group-header');
        this.romListContainer.classList.add('card-romlist-roms-section');
        this.Body.appendChild(this.romListContainer);
        this.LoadRoms();

        // create the mangement buttons
        this.managementButtons = document.createElement('div');
        this.managementButtons.classList.add('card-romlist-management');
        this.managementButtons.classList.add('card-romlist-roms-section');
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

        if (userProfile.roles.includes("Admin")) {
            // create the content container upload buttons
            this.contentUploadButtons = document.createElement('div');
            this.contentUploadButtons.classList.add('card-romlist-group');
            this.contentUploadButtons.classList.add('card-romlist-group-header');
            this.contentUploadButtons.classList.add('card-romlist-content-section');
            this.contentUploadButtons.style.display = 'none';

            // upload audio sample button - only show on arcade platforms
            if (this.gamePlatformObject.id === 52) {
                this.uploadAudioButton = document.createElement('button');
                this.uploadAudioButton.classList.add('modal-button');
                this.uploadAudioButton.classList.add('card-romlist-management-button');
                this.uploadAudioButton.innerHTML = 'Upload Audio Sample';
                this.uploadAudioButton.addEventListener('click', async (e) => {
                    e.stopPropagation();
                    let uploadDialog = new ContentUploadDialog(this.gamePlatformObject.metadataMapId, 'AudioSample', this.Refresh.bind(this));
                    await uploadDialog.open();
                    this.Refresh();
                });
                this.contentUploadButtons.appendChild(this.uploadAudioButton);
            }

            // upload manual button
            this.uploadManualButton = document.createElement('button');
            this.uploadManualButton.classList.add('modal-button');
            this.uploadManualButton.classList.add('card-romlist-management-button');
            this.uploadManualButton.innerHTML = 'Upload Manual';
            this.uploadManualButton.addEventListener('click', async (e) => {
                e.stopPropagation();
                let uploadDialog = new ContentUploadDialog(this.gamePlatformObject.metadataMapId, 'GlobalManual', this.Refresh.bind(this));
                await uploadDialog.open();
                // this.Refresh();
            });
            this.contentUploadButtons.appendChild(this.uploadManualButton);
        }

        // create the upload content button
        this.Body.appendChild(this.contentUploadButtons);

        // create the content container
        this.contentContainer = document.createElement('div');
        this.contentContainer.classList.add('card-romlist-group');
        this.contentContainer.classList.add('card-romlist-group-header');
        this.contentContainer.classList.add('card-romlist-content-section');
        this.contentContainer.style.display = 'none';
        this.Body.appendChild(this.contentContainer);
        this.LoadContent();
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

                if (forceUpdate === true) {
                    // clear the media group container
                    this.mediaGroupContainer.innerHTML = '';

                    data.forEach(async element => {
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
                                let stateManager = new EmulatorStateManager(element.id, true, this.gamePlatformObject.emulatorConfiguration.emulatorType, this.gamePlatformObject.emulatorConfiguration.core, element.platformId, element.metadataMapId, element.name);
                                stateManager.open();
                            });
                            rightDiv.appendChild(platformStateManagerButton);

                            // create the play button
                            let playButton = document.createElement('div');
                            playButton.classList.add('platform_edit_button');
                            playButton.classList.add('platform_item_green');
                            playButton.innerHTML = '<img src="/images/play.svg" class="banner_button_image" />';

                            // create launch object
                            let launchObject = {
                                "emulatorConfiguration": this.gamePlatformObject.emulatorConfiguration,
                                "id": this.gamePlatformObject.id,
                                "metadataMapId": this.gamePlatformObject.metadataMapId,
                                "romId": element.id,
                                "romName": this.gamePlatformObject.metadataGameName,
                                "isMediaGroup": true
                            };
                            let launchLink = await BuildGameLaunchLink(launchObject);
                            if (launchLink !== null) {
                                playButton.addEventListener('click', async (e) => {
                                    e.stopPropagation();
                                    console.log(this.gamePlatformObject);
                                    console.log(launchObject);
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
            if (data.gameRomItems) {
                data.gameRomItems.forEach(async element => {
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
                        let stateManager = new EmulatorStateManager(element.id, false, this.gamePlatformObject.emulatorConfiguration.emulatorType, this.gamePlatformObject.emulatorConfiguration.core, element.platformId, element.metadataMapId, element.name);
                        stateManager.open();
                    });
                    rightDiv.appendChild(platformStateManagerButton);

                    // create the play button
                    let playButton = document.createElement('div');
                    playButton.classList.add('platform_edit_button');
                    playButton.classList.add('platform_item_green');
                    playButton.innerHTML = '<img src="/images/play.svg" class="banner_button_image" />';

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
                    if (launchLink !== null) {
                        playButton.addEventListener('click', async (e) => {
                            e.stopPropagation();
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

                    this.romListContainer.appendChild(romItem);
                });
            }
        });
    }

    LoadContent() {
        // load any additional content
        console.log('Loading additional content for platform ' + this.gamePlatformObject.name);
        this.contentContainer.innerHTML = '';
        fetch(`/api/v1.1/ContentManager/?metadataids=${this.gamePlatformObject.metadataMapId}&contentTypes=AudioSample,GlobalManual&pageSize=50`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(response => response.json()).then(data => {
            console.log(data);
            if (data.totalCount === 0) {
                let noContentLabel = document.createElement('div');
                noContentLabel.classList.add('card-romlist-no-content');
                noContentLabel.innerHTML = 'No additional content available for this ROM.';
                this.contentContainer.appendChild(noContentLabel);
            } else {
                let contentTable = document.createElement('table');
                contentTable.classList.add('section');
                contentTable.classList.add('romtable');
                contentTable.setAttribute('cellspacing', '0');
                this.contentContainer.appendChild(contentTable);

                let contentTableHeader = document.createElement('tr');
                contentTableHeader.classList.add('romrow');
                contentTable.appendChild(contentTableHeader);

                let contentTableHeaderName = document.createElement('th');
                contentTableHeaderName.classList.add('romcell');
                contentTableHeaderName.innerHTML = 'Name';
                contentTableHeader.appendChild(contentTableHeaderName);

                let contentTableHeaderSize = document.createElement('th');
                contentTableHeaderSize.innerHTML = 'Size';
                contentTableHeaderSize.classList.add('romcell');
                contentTableHeader.appendChild(contentTableHeaderSize);

                let contentTableHeaderType = document.createElement('th');
                contentTableHeaderType.innerHTML = 'Type';
                contentTableHeaderType.classList.add('romcell');
                contentTableHeader.appendChild(contentTableHeaderType);

                let contentTableHeaderAction = document.createElement('th');
                contentTableHeaderAction.innerHTML = '';
                contentTableHeaderAction.classList.add('romcell');
                contentTableHeader.appendChild(contentTableHeaderAction);

                data.items.forEach(element => {
                    let contentTableRow = document.createElement('tr');
                    contentTableRow.classList.add('romrow');
                    contentTable.appendChild(contentTableRow);

                    let contentTableName = document.createElement('td');
                    contentTableName.classList.add('romcell');
                    contentTableName.innerHTML = element.fileName + element.fileExtension;
                    contentTableRow.appendChild(contentTableName);

                    let contentTableSize = document.createElement('td');
                    contentTableSize.classList.add('romcell');
                    contentTableSize.innerHTML = formatBytes(element.size);
                    contentTableRow.appendChild(contentTableSize);

                    let contentTableType = document.createElement('td');
                    contentTableType.classList.add('romcell');
                    switch (element.contentType) {
                        case 'AudioSample':
                            contentTableType.innerHTML = 'Audio Sample';
                            break;
                        case 'GlobalManual':
                            contentTableType.innerHTML = 'Manual';
                            break;
                        default:
                            contentTableType.innerHTML = element.contentType;
                            break;
                    }
                    contentTableRow.appendChild(contentTableType);

                    let contentTableAction = document.createElement('td');
                    contentTableAction.classList.add('romcell');

                    let contentTableActionDownload = document.createElement('img');
                    contentTableActionDownload.src = '/images/download.svg';
                    contentTableActionDownload.classList.add('banner_button_image');
                    contentTableActionDownload.style.cursor = 'pointer';
                    contentTableAction.appendChild(contentTableActionDownload);

                    let contentTableActionDelete = document.createElement('img');
                    contentTableActionDelete.src = '/images/delete.svg';
                    contentTableActionDelete.classList.add('banner_button_image');
                    contentTableActionDelete.style.cursor = 'pointer';
                    contentTableActionDelete.style.marginLeft = '10px';
                    contentTableAction.appendChild(contentTableActionDelete);

                    contentTableRow.appendChild(contentTableAction);

                    contentTableActionDownload.addEventListener('click', async (e) => {
                        e.stopPropagation();
                        // download and open the content in a new tab
                        let downloadLink = `/api/v1.1/ContentManager/attachment/${element.attachmentId}/data`;
                        window.open(downloadLink, '_blank');
                    });

                    if (userProfile.roles.includes("Admin")) {
                        contentTableActionDelete.addEventListener('click', async (e) => {
                            e.stopPropagation();
                            // delete the content
                            fetch(`/api/v1.1/ContentManager/attachment/${element.attachmentId}`, {
                                method: 'DELETE',
                                headers: {
                                    'Content-Type': 'application/json'
                                }
                            }).then(response => {
                                if (response.status === 200) {
                                    this.Refresh();
                                } else {
                                    console.log('Error deleting content item: ' + response.statusText);
                                }
                            });
                        });
                    }
                });
            }
        });
    }

    Refresh() {
        this.mediaGroupContainer.innerHTML = '';
        this.romListContainer.innerHTML = '';
        this.contentContainer.innerHTML = '';
        this.LoadMediaGroups();
        this.LoadRoms();
        this.LoadContent();
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

        // add missing supported metadata sources to the map
        supportedMetadataSources.forEach(sourceElement => {
            let element = metadataMap.metadataMapItems.find(item => item.sourceType === sourceElement);
            if (!element) {
                metadataMap.metadataMapItems.push({
                    automaticMetadataSourceId: '',
                    isManual: true,
                    sourceType: sourceElement,
                    sourceId: '',
                    sourceSlug: '',
                    link: '',
                    preferred: false,
                    supportedDataSource: true
                });
            }
        });

        console.log(metadataMap.metadataMapItems);

        metadataMap.metadataMapItems.forEach(element => {
            if (supportedMetadataSources.includes(element.sourceType) === false) {
                return;
            }

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
            });
            if (element.preferred === true) {
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
                    let contentTable = document.createElement('table');
                    contentTable.style.width = '100%';

                    let contentTableIdRow = document.createElement('tr');

                    let contentTableIdCellLabel = document.createElement('td');
                    contentTableIdCellLabel.style.width = '50px';
                    contentTableIdCellLabel.innerHTML = 'Id:';
                    contentTableIdRow.appendChild(contentTableIdCellLabel);

                    let contentTableIdCellInput = document.createElement('td');
                    let contentInput = document.createElement('input');
                    contentInput.type = 'text';
                    contentInput.value = element.sourceId;
                    contentInput.placeholder = element.sourceId;
                    contentInput.style.width = '95%';
                    contentTableIdCellInput.appendChild(contentInput);

                    contentTableIdRow.appendChild(contentTableIdCellInput);

                    let contentTableIdCellManual = document.createElement('td');
                    let contentInputManual = document.createElement('input');
                    contentInputManual.type = 'checkbox';
                    contentInputManual.checked = element.isManual;
                    contentInputManual.title = 'Manual Entry';
                    contentInput.addEventListener('input', (e) => {
                        element.sourceId = e.target.value;
                        element.sourceSlug = e.target.value;

                        if (contentInput.value !== element.automaticMetadataSourceId) {
                            element.isManual = true;
                            contentInputManual.checked = true;
                        } else {
                            element.isManual = false;
                            contentInputManual.checked = false;
                        }
                    });
                    contentInputManual.addEventListener('change', (e) => {
                        element.isManual = e.target.checked;
                        if (element.isManual === false) {
                            contentInput.value = element.automaticMetadataSourceId;
                        } else {
                            contentInput.value = element.sourceId;
                        }
                        element.sourceId = contentInput.value;
                    });
                    contentTableIdCellManual.appendChild(contentInputManual);

                    let contentTableIdCellManualLabel = document.createElement('label');
                    contentTableIdCellManualLabel.htmlFor = contentInputManual.id;
                    contentTableIdCellManualLabel.style.marginLeft = '5px';
                    contentTableIdCellManualLabel.style.marginRight = '20px';
                    contentTableIdCellManualLabel.innerHTML = 'Manual';
                    contentTableIdCellManual.appendChild(contentTableIdCellManualLabel);

                    contentTableIdRow.appendChild(contentTableIdCellManual);

                    contentTable.appendChild(contentTableIdRow);

                    if (element.link) {
                        if (element.link.length > 0) {
                            let contentTableLinkRow = document.createElement('tr');

                            let contentTableLinkCellLabel = document.createElement('td');
                            contentTableLinkCellLabel.innerHTML = 'Link:';
                            contentTableLinkRow.appendChild(contentTableLinkCellLabel);

                            let contentTableLinkCellLink = document.createElement('td');
                            contentTableLinkCellLink.innerHTML = '<a href="' + element.link + '" target="_blank" rel="noopener noreferrer" class="romlink">' + element.link + '</a>';
                            contentTableLinkCellLink.colSpan = 3;
                            contentTableLinkRow.appendChild(contentTableLinkCellLink);

                            contentTable.appendChild(contentTableLinkRow);
                        }
                    }

                    itemSectionContent.appendChild(contentTable);
                    break;

            }
            itemSection.appendChild(itemSectionContent);

            metadataContent.appendChild(itemSection);
        });


        // setup the buttons
        let okButton = new ModalButton('OK', 1, this.gamePlatformObject, async function (callingObject) {
            let model = metadataMap.metadataMapItems;

            // process the model to ensure only one preferred is set
            let preferredSet = false;
            model.forEach(item => {
                if (item.preferred === true) {
                    if (preferredSet === false) {
                        preferredSet = true;
                    } else {
                        item.preferred = false;
                    }
                }
            });

            // process the model to ensure unsupported data sources are not included
            model = model.filter(item => supportedMetadataSources.includes(item.sourceType));

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

class SettingsCard {
    constructor() {

    }

    menuItems = {
        "/home": {
            name: "General"
        },
        "/server": {
            name: "Server Settings",
            roles: [
                "Admin"
            ]
        },
        "/datasources": {
            name: "Data Sources",
            roles: [
                "Admin"
            ]
        },
        "/libraries": {
            name: "Libraries",
            roles: [
                "Admin"
            ]
        },
        "/services": {
            name: "Services"
        },
        "/services/services-configure": {
            name: "Service Configuration",
            roles: [
                "Admin"
            ]
        },
        "/users": {
            name: "Users",
            roles: [
                "Admin"
            ]
        },
        "/users/user-management": {
            name: "User Management",
            roles: [
                "Admin"
            ]
        },
        "/platforms": {
            name: "Platforms",
            roles: [
                "Admin"
            ]
        },
        "/firmware": {
            name: "Firmware",
            roles: [
                "Admin"
            ]
        },
        "/logs": {
            name: "Logs",
            roles: [
                "Admin"
            ]
        },
        "/about": {
            name: "About"
        }
    }

    currentPage = '';

    async ShowCard() {
        this.card = new Card('settings', this.gameId);
        this.card.BuildCard();

        // set the header
        this.card.SetHeader("Settings", true);

        // set the background
        this.card.cardBackgroundContainer.style.display = 'none';
        this.card.cardScroller.classList.add('card-settings-scroller-background');

        // load the card body
        fetch("/pages/cards/settings.html", {
            method: 'GET',
            headers: {
                'Content-Type': 'text/html'
            }
        }).then(async response => {
            if (!response.ok) {
                throw new Error('Network response was not ok: ' + response.statusText);
            }
            const content = await response.text();
            this.card.cardBody.innerHTML = content;

            // set the body
            this.menu = this.card.cardBody.querySelector('#card-settings-menu-box');
            this.body = this.card.cardBody.querySelector('#card-settings-content');
            this.contentHeading = this.card.cardBody.querySelector('#card-settings-content-heading-label');
            this.content = this.card.cardBody.querySelector('#card-settings-content-body');

            // set the back button
            this.card.backButton.addEventListener('click', async () => {
                let pagePathParts = this.currentPagePath.split('/');
                if (pagePathParts.length === 2) {
                    // show the menu
                    this.menu.classList.remove('card-settings-menu-box-smallscreen-invisible');
                    // hide the content box
                    this.body.classList.add('card-settings-content-box-smallscreen-invisible');
                    // hide the back button
                    this.card.backButton.classList.remove('card-back-button-smallscreen-visible');
                } else {
                    // switch to the parent page
                    let parentPagePath = pagePathParts.slice(0, -1).join('/');
                    await this.SwitchPage(parentPagePath);
                }
            });

            // build the menu
            this.BuildMenu();

            // show the card
            this.card.Open();

            // load the home settings page
            await this.SwitchPage('/home', true);
        }).catch(error => {
            console.error('Error fetching card content:', error);
            // handle error, e.g., show an error message
        });
    }

    BuildMenu() {
        let menuContainer = this.card.cardBody.querySelector('#card-settings-menu');
        menuContainer.classList.add('section');
        menuContainer.innerHTML = '';

        for (const item in Object.keys(this.menuItems)) {
            // skip if item has more than one slash
            if (Object.keys(this.menuItems)[item].split('/').length > 2) {
                continue;
            }

            let key = Object.keys(this.menuItems)[item];

            if (this.menuItems[key].roles) {
                this.menuItems[key].roles.forEach(role => {
                    if (!userProfile.roles.includes(role)) {
                        // user does not have the required role, skip this item
                        key = null;
                    }
                });
            }

            if (key === null) {
                continue;
            }

            let menuItem = document.createElement('div');
            menuItem.classList.add('section-body');
            menuItem.classList.add('card-settings-menu-item');
            menuItem.classList.add('section-body-button');
            menuItem.setAttribute('data-page', key);
            menuItem.innerHTML = this.menuItems[key].name;
            menuItem.addEventListener('click', async () => {
                // switch the page
                await this.SwitchPage(key);
            });
            menuContainer.appendChild(menuItem);
        }
    }

    async SwitchPage(pagePath, initialLoad = false, model = null) {
        // split the page path to get the page name - page name to load is the last part of the path
        let pagePathParts = pagePath.split('/');
        let page = pagePathParts[pagePathParts.length - 1];
        let pageRoot = `/${pagePathParts[1]}`;

        await fetch('/pages/cards/settings/' + page + '.html', {
            method: 'GET',
            headers: {
                'Content-Type': 'text/html'
            }
        }).then(async response => {
            if (!response.ok) {
                throw new Error('Network response was not ok: ' + response.statusText);
            }

            if (initialLoad === false) {
                // hide the menu on small screens
                this.menu.classList.add('card-settings-menu-box-smallscreen-invisible');
                // show the content box on small screens
                this.body.classList.remove('card-settings-content-box-smallscreen-invisible');
                // show the back button
                this.card.backButton.classList.add('card-back-button-smallscreen-visible');
            } else {
                // show the menu on initial load on small screens
                this.menu.classList.remove('card-settings-menu-box-smallscreen-invisible');
                // hide the content box on initial load on small screens
                this.body.classList.add('card-settings-content-box-smallscreen-invisible');
                // hide the back button on initial load
                this.card.backButton.classList.remove('card-back-button-smallscreen-visible');
            }

            if (pagePathParts.length > 2) {
                // show the back button since we're on a subpage
                this.card.backButton.classList.add('card-back-button-nested-visible');
            } else {
                // hide the back button since we're on a top-level page
                this.card.backButton.classList.remove('card-back-button-nested-visible');
            }

            // select the appropriate button
            let menuItems = this.card.cardBody.querySelectorAll('.card-settings-menu-item');

            menuItems.forEach(mi => {
                mi.classList.remove('card-settings-menu-item-selected');
                if (mi.getAttribute('data-page').startsWith(pageRoot)) {
                    mi.classList.add('card-settings-menu-item-selected');
                }
            });

            // set the content
            let pageName = this.menuItems[pagePath].name;
            this.contentHeading.innerHTML = pageName;
            this.content.innerHTML = await response.text();

            this.currentPage = page;
            this.currentPagePath = pagePath;
        }).then(async () => {
            // clear the refresher if it exists
            if (this.refresher) {
                clearInterval(this.refresher);
                this.refresher = null;
            }

            // load the content based on the page
            switch (page) {
                case 'home':
                    await this.SystemLoadSystemStatus();
                    await this.SystemSignaturesStatus();
                    break;

                case 'server':
                case 'datasources':
                    await this.LoadServerSettings();
                    break;

                case 'services':
                    await this.SystemLoadStatus();
                    this.refresher = setInterval(() => {
                        this.SystemLoadStatus();
                    }, 5000);
                    this.body.querySelector('#system_tasks_config').addEventListener('click', async () => {
                        this.SwitchPage('/services/services-configure');
                    });

                    if (!userProfile.roles.includes('Admin')) {
                        document.getElementById('system_tasks_config').style.display = 'none';
                    }

                    break;

                case 'services-configure':
                    await this.getBackgroundTaskTimers();
                    this.body.querySelector('#settings_tasktimers_default').addEventListener('click', async () => {
                        this.defaultTaskTimers();
                    });
                    this.body.querySelector('#settings_tasktimers_new').addEventListener('click', async () => {
                        await this.saveTaskTimers();
                        await this.SwitchPage('/services');
                    });
                    break;

                case 'libraries':
                    await this.drawLibrary();
                    this.body.querySelector('#settings_newlibrary').addEventListener('click', async () => {
                        let newLibrary = new NewLibrary(this);
                        newLibrary.open();
                        await this.drawLibrary();
                    });
                    break;

                case 'users':
                    await this.GetUsers();
                    this.body.querySelector('#settings_users_new').addEventListener('click', async () => {
                        let newUser = new UserNew(this);
                        newUser.open();
                    });
                    break;

                case 'platforms':
                    await this.SetupButtons();
                    await this.loadPlatformMapping();
                    break;

                case 'firmware':
                    let bios = new BiosTable(this.body.querySelector('#table_firmware'));
                    await bios.loadBios();
                    break;

                case 'logs':
                    if (model) {
                        this.LoadLogs(
                            model.information,
                            model.warning,
                            model.critical,
                            model.startDateTime,
                            model.endDateTime,
                            model.searchText,
                            model.correlationId
                        );
                    } else {
                        this.LoadLogs();
                    }
                    break;

                case 'about':
                    await this.setupAboutPage();
                    break;
            }
        }).catch(error => {
            console.error('Error fetching page:', error);
            // handle error, e.g., show an error message
        });
    }

    refresher = null;

    async SystemLoadSystemStatus() {
        fetch('/api/v1.1/System')
            .then(response => response.json())
            .then(result => {
                if (result) {
                    this.#BuildLibraryStatisticsBar(
                        this.content.querySelector('#system_platforms'),
                        this.content.querySelector('#system_platforms_legend'),
                        result.platformStatistics
                    );

                    // database
                    let newDbTable = document.createElement('table');
                    newDbTable.className = 'romtable';
                    newDbTable.setAttribute('cellspacing', 0);
                    newDbTable.appendChild(createTableRow(false, ['Database Size', formatBytes(result.databaseSize)]));

                    let targetDbDiv = this.body.querySelector('#system_database');
                    targetDbDiv.innerHTML = '';
                    targetDbDiv.appendChild(newDbTable);
                }
            });
    }

    #BuildLibraryStatisticsBar(TargetObject, TargetObjectLegend, LibraryStatistics) {
        TargetObject.innerHTML = '';
        TargetObjectLegend.innerHTML = '';

        let newTable = document.createElement('div');
        newTable.setAttribute('cellspacing', 0);
        newTable.setAttribute('style', 'width: 100%; height: 10px;');

        let newRow = document.createElement('div');
        newRow.setAttribute('style', 'display: flex; width: 100%;');

        let LibrarySize = 0;
        // get LibarySize as sum of all platform sizes
        for (const stat of LibraryStatistics) {
            LibrarySize += stat.totalSize;
        }

        for (const stat of LibraryStatistics) {
            let platformSizePercent = stat.totalSize / LibrarySize * 100;
            let platformSizeColour = intToRGB(hashCode(stat.platform));
            let newCell = document.createElement('div');
            let segmentId = 'platform_' + stat.platform;
            newCell.id = segmentId;
            newCell.setAttribute('style', 'display: inline; height: 10px; min-width: 1px; width: ' + platformSizePercent + '%; background-color: #' + platformSizeColour);
            newRow.appendChild(newCell);

            let legend = document.createElement('div');
            legend.id = 'legend_' + stat.platform;
            legend.className = 'legend_box';

            let legendColour = document.createElement('div');
            let colourId = 'colour_' + stat.platform;
            legendColour.id = colourId;
            legendColour.className = 'legend_colour';
            legendColour.setAttribute('style', 'background-color: #' + platformSizeColour + ';');

            let legendLabel = document.createElement('div');
            legendLabel.className = 'legend_label';
            legendLabel.innerHTML = '<strong>' + stat.platform + '</strong><br />' + formatBytes(stat.totalSize) + '<br />ROMs: ' + stat.romCount;

            // event listeners
            legend.addEventListener('mouseenter', () => {
                newCell.style.outline = '2px solid #' + platformSizeColour;
                newCell.style.outlineOffset = '0px';
                newCell.style.zIndex = '1';
                newCell.style.boxShadow = '0px 0px 10px 0px #' + platformSizeColour;

                legendColour.style.outline = '2px solid #' + platformSizeColour;
                legendColour.style.outlineOffset = '0px';
                legendColour.style.zIndex = '1';
                legendColour.style.boxShadow = '0px 0px 10px 0px #' + platformSizeColour;
            });
            legend.addEventListener('mouseleave', () => {
                newCell.style.outline = 'none';
                newCell.style.outlineOffset = '0px';
                newCell.style.zIndex = '0';
                newCell.style.boxShadow = 'none';

                legendColour.style.outline = 'none';
                legendColour.style.outlineOffset = '0px';
                legendColour.style.zIndex = '0';
                legendColour.style.boxShadow = 'none';
            });

            legend.appendChild(legendColour);
            legend.appendChild(legendLabel);
            TargetObjectLegend.appendChild(legend);
        }

        newTable.appendChild(newRow);
        TargetObject.appendChild(newTable);
    }

    async SystemSignaturesStatus() {
        fetch('/api/v1.1/Signatures/Status')
            .then(response => response.json())
            .then(result => {
                if (!result || (result.sources === 0 && result.platforms === 0 && result.games === 0 && result.roms === 0)) {
                    let targetDiv = this.body.querySelector('#system_signaturessection');
                    targetDiv.style.display = 'none';
                }

                let newTable = document.createElement('table');
                newTable.className = 'romtable';
                newTable.setAttribute('cellspacing', 0);
                newTable.appendChild(createTableRow(true, ['Sources', 'Platforms', 'Games', 'ROMs']));

                if (result) {
                    let newRow = [
                        result.sources,
                        result.platforms,
                        result.games,
                        result.roms
                    ];
                    newTable.appendChild(createTableRow(false, newRow, 'romrow', 'romcell'));
                }

                let targetDiv = this.body.querySelector('#system_signatures');
                targetDiv.innerHTML = '';
                targetDiv.appendChild(newTable);
            })
            .catch(error => console.error('Error fetching signatures status:', error));
    }

    async LoadServerSettings() {
        // this is used for both the Data Sources and Server Settings pages, so we can use the same function
        await fetch('/api/v1.1/System/Settings/System')
            .then(response => response.json())
            .then(result => {
                switch (this.currentPage) {
                    case 'server':
                        // set the server settings
                        let optionToSelect = '#settings_logs_write_db';
                        if (result.alwaysLogToDisk == true) {
                            optionToSelect = '#settings_logs_write_fs';
                        }
                        this.card.cardBody.querySelector(optionToSelect).checked = true;

                        this.card.cardBody.querySelector('#settings_logs_retention').value = result.minimumLogRetentionPeriod;

                        this.card.cardBody.querySelector('#settings_emulator_debug').checked = result.emulatorDebugMode;
                        break;

                    case 'datasources':
                        // set the data sources settings
                        switch (result.signatureSource.source) {
                            case "LocalOnly":
                                this.card.cardBody.querySelector('#settings_signaturesource_local').checked = true;
                                break;

                            case "Hasheous":
                                this.card.cardBody.querySelector('#settings_signaturesource_hasheous').checked = true;
                                break;

                        }

                        let metadataSettingsContainer = this.card.cardBody.querySelector('#settings_metadata');
                        metadataSettingsContainer.innerHTML = '';
                        result.metadataSources.forEach(element => {
                            // section
                            let sourceSection = document.createElement('div');
                            sourceSection.classList.add('section');
                            sourceSection.setAttribute('id', 'settings_metadatasource_' + element.source);

                            // section header
                            let sourceHeader = document.createElement('div');
                            sourceHeader.classList.add('section-header');

                            let sourceRadio = document.createElement('input');
                            sourceRadio.setAttribute('type', 'radio');
                            sourceRadio.setAttribute('name', 'settings_metadatasource');
                            sourceRadio.setAttribute('value', element.source);
                            sourceRadio.setAttribute('data-settingname', 'metadataconfiguration.defaultmetadatasource');
                            sourceRadio.setAttribute('id', 'settings_metadatasource_' + element.source + '_radio');
                            sourceRadio.style.margin = '0px';
                            sourceRadio.style.height = 'unset';
                            if (element.default) {
                                sourceRadio.checked = true;
                            }

                            let sourceLabel = document.createElement('label');
                            sourceLabel.setAttribute('for', 'settings_metadatasource_' + element.source + '_radio');

                            let sourceName = document.createElement('span');
                            switch (element.source) {
                                case "IGDB":
                                    sourceName.innerText = 'Internet Game Database (IGDB)';
                                    break;

                                default:
                                    sourceName.innerText = element.source;
                                    break;
                            }
                            sourceName.style.marginLeft = '10px';
                            sourceLabel.appendChild(sourceName);

                            let sourceConfigured = document.createElement('span');
                            sourceConfigured.style.float = 'right';
                            sourceConfigured.classList.add(element.configured ? 'greentext' : 'redtext');
                            sourceConfigured.innerText = element.configured ? 'Configured' : 'Not Configured';

                            sourceHeader.appendChild(sourceRadio);
                            sourceHeader.appendChild(sourceLabel);
                            sourceHeader.appendChild(sourceConfigured);
                            sourceSection.appendChild(sourceHeader);

                            // section body
                            let sourceContent = document.createElement('div');
                            sourceContent.classList.add('section-body');
                            if (element.usesProxy === false && element.usesClientIdAndSecret === false) {
                                sourceContent.innerText = 'No options to configure';
                            } else {
                                // render controls
                                let controlsTable = document.createElement('table');
                                controlsTable.style.width = '100%';

                                // hasheous proxy row
                                if (element.usesProxy === true) {
                                    let proxyRow = document.createElement('tr');

                                    let proxyLabel = document.createElement('td');
                                    if (element.usesClientIdAndSecret === true) {
                                        let proxyRadio = document.createElement('input');
                                        proxyRadio.id = 'settings_metadatasource_proxy_' + element.source;
                                        proxyRadio.setAttribute('type', 'radio');
                                        proxyRadio.setAttribute('name', 'settings_metadatasource_proxy_' + element.source);
                                        proxyRadio.setAttribute('data-settingname', 'igdb.usehasheousproxy');
                                        proxyRadio.setAttribute('value', 'true');
                                        proxyRadio.style.marginRight = '10px';
                                        if (element.useHasheousProxy === true) {
                                            proxyRadio.checked = true;
                                        }
                                        proxyLabel.appendChild(proxyRadio);

                                        let proxyLabelLabel = document.createElement('label');
                                        proxyLabelLabel.setAttribute('for', 'settings_metadatasource_proxy_' + element.source);

                                        let proxyLabelSpan = document.createElement('span');
                                        proxyLabelSpan.innerText = 'Use Hasheous Proxy';
                                        proxyLabelLabel.appendChild(proxyLabelSpan);
                                        proxyLabel.appendChild(proxyLabelLabel);

                                        proxyRow.appendChild(proxyLabel);
                                    } else {
                                        proxyLabel.innerHTML = 'Uses Hasheous Proxy';
                                        proxyRow.appendChild(proxyLabel);
                                    }

                                    controlsTable.appendChild(proxyRow);
                                }

                                // client id and secret row
                                if (element.usesClientIdAndSecret === true) {
                                    if (element.usesProxy === true) {
                                        let clientRadioRow = document.createElement('tr');

                                        let clientRadioLabel = document.createElement('td');
                                        let clientRadio = document.createElement('input');
                                        clientRadio.id = 'settings_metadatasource_client_' + element.source;
                                        clientRadio.setAttribute('type', 'radio');
                                        clientRadio.setAttribute('name', 'settings_metadatasource_proxy_' + element.source);
                                        clientRadio.setAttribute('data-settingname', 'igdb.usehasheousproxy');
                                        clientRadio.setAttribute('value', 'false');
                                        clientRadio.style.marginRight = '10px';
                                        if (element.useHasheousProxy === false) {
                                            clientRadio.checked = true;
                                        }
                                        clientRadioLabel.appendChild(clientRadio);

                                        let clientRadioLabelLabel = document.createElement('label');
                                        clientRadioLabelLabel.setAttribute('for', 'settings_metadatasource_client_' + element.source);

                                        let clientRadioLabelSpan = document.createElement('span');
                                        clientRadioLabelSpan.innerText = 'Direct connection';
                                        clientRadioLabelLabel.appendChild(clientRadioLabelSpan);
                                        clientRadioLabel.appendChild(clientRadioLabelLabel);

                                        clientRadioRow.appendChild(clientRadioLabel);

                                        controlsTable.appendChild(clientRadioRow);
                                    }

                                    let clientIdTable = document.createElement('table');
                                    clientIdTable.style.width = '100%';
                                    if (element.usesProxy === true) {
                                        clientIdTable.style.marginLeft = '30px';
                                    }

                                    let clientIdRow = document.createElement('tr');

                                    let clientIdLabel = document.createElement('td');
                                    clientIdLabel.style.width = '15%';
                                    clientIdLabel.innerText = 'Client ID';
                                    clientIdRow.appendChild(clientIdLabel);

                                    let clientIdInput = document.createElement('td');
                                    let clientIdInputField = document.createElement('input');
                                    clientIdInputField.style.width = '90%';
                                    clientIdInputField.setAttribute('type', 'text');
                                    clientIdInputField.setAttribute('id', 'settings_metadatasource_' + element.source + '_clientid');
                                    clientIdInputField.setAttribute('data-settingname', element.source.toLowerCase() + '.clientid');
                                    clientIdInputField.value = element.clientId;
                                    clientIdInput.appendChild(clientIdInputField);
                                    clientIdRow.appendChild(clientIdInput);

                                    clientIdTable.appendChild(clientIdRow);

                                    let clientSecretRow = document.createElement('tr');

                                    let clientSecretLabel = document.createElement('td');
                                    clientSecretLabel.style.width = '15%';
                                    clientSecretLabel.innerText = 'Client Secret';
                                    clientSecretRow.appendChild(clientSecretLabel);

                                    let clientSecretInput = document.createElement('td');
                                    let clientSecretInputField = document.createElement('input');
                                    clientSecretInputField.style.width = '90%';
                                    clientSecretInputField.setAttribute('type', 'text');
                                    clientSecretInputField.setAttribute('id', 'settings_metadatasource_' + element.source + '_clientsecret');
                                    clientSecretInputField.setAttribute('data-settingname', element.source.toLowerCase() + '.secret');
                                    clientSecretInputField.value = element.secret;
                                    clientSecretInput.appendChild(clientSecretInputField);
                                    clientSecretRow.appendChild(clientSecretInput);

                                    clientIdTable.appendChild(clientSecretRow);

                                    controlsTable.appendChild(clientIdTable);
                                }


                                sourceContent.appendChild(controlsTable);
                            }
                            sourceSection.appendChild(sourceContent);

                            metadataSettingsContainer.appendChild(sourceSection);
                        });

                        this.card.cardBody.querySelector('#settings_signaturesource_hasheoushost').value = result.signatureSource.hasheousHost;

                        let hasheousSubmitCheck = this.card.cardBody.querySelector('#settings_hasheoussubmit');
                        if (result.signatureSource.hasheousSubmitFixes === true) {
                            hasheousSubmitCheck.checked = true;
                        }
                        hasheousSubmitCheck.addEventListener('change', () => {
                            this.#toggleHasheousAPIKey(hasheousSubmitCheck);
                        });
                        this.card.cardBody.querySelector('#settings_hasheousapikey').value = result.signatureSource.hasheousAPIKey;
                        this.#toggleHasheousAPIKey(hasheousSubmitCheck);

                        break;
                }
            })
            .catch(error => {
                console.error('Error fetching server settings:', error);
                // handle error, e.g., show an error message
            });

        // set the event listeners for the settings
        let settingInputs = this.card.cardBody.querySelectorAll('[data-settingname]');
        settingInputs.forEach(input => {
            let eventTypes = ['change'];
            if (input.type === 'text' || input.type === 'password' || input.type === 'email' || input.type === 'url' || input.type === 'number') {
                eventTypes = ['change', 'input'];
            }

            eventTypes.forEach(eventType => {
                input.addEventListener(eventType, async (event) => {
                    let settingName = event.target.getAttribute('data-settingname');
                    let settingValue = event.target.value;

                    // handle checkbox inputs
                    if (event.target.type === 'checkbox') {
                        settingValue = event.target.checked;
                    }

                    // format the setting
                    let settingValueDict = {};
                    settingValueDict[settingName] = settingValue;

                    console.log('Setting changed:', settingValue);
                    console.log(settingValueDict);

                    // send the setting change to the server
                    await fetch('/api/v1.1/System/Settings/System', {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(settingValueDict)
                    }).then(response => response.json()).then(result => {
                        console.log('Setting updated:', result);
                    }).catch(error => {
                        console.error('Error updating setting:', error);
                    });
                });
            });
        });
    }

    #toggleHasheousAPIKey(checkbox) {
        let settings_hasheousapikey_row = document.getElementById('settings_hasheousapikey_row');
        if (checkbox.checked === true) {
            settings_hasheousapikey_row.style.display = '';
        } else {
            settings_hasheousapikey_row.style.display = 'none';
        }
    }

    async SystemLoadStatus() {
        await fetch('/api/v1.1/BackgroundTasks')
            .then(response => response.json())
            .then(result => {
                // table header
                const columnHeaders = {
                    status: {
                        text: '',
                        classList: ['romcell'],
                        styleOverrideList: ['width: 20px;', 'height: 20px;'],
                        tooltip: 'Status',
                        jsonName: 'itemState'
                    },
                    type: {
                        text: 'Task Name',
                        classList: ['romcell'],
                        styleOverrideList: [],
                        tooltip: 'Task Type',
                        jsonName: 'itemType'
                    },
                    interval: {
                        text: 'Interval',
                        classList: ['romcell', 'card-services-column'],
                        styleOverrideList: [],
                        tooltip: 'Interval',
                        jsonName: 'interval'
                    },
                    lastRunDuration: {
                        text: 'Last Run Duration',
                        classList: ['romcell', 'card-services-column'],
                        styleOverrideList: [],
                        tooltip: 'Last Run Duration',
                        jsonName: 'lastRunDuration'
                    },
                    lastRunTime: {
                        text: 'Last Run Time',
                        classList: ['romcell', 'card-services-column'],
                        styleOverrideList: [],
                        tooltip: 'Last Run Time',
                        jsonName: 'lastRunTime'
                    },
                    nextRunTime: {
                        text: 'Next Run Time',
                        classList: ['romcell', 'card-services-column'],
                        styleOverrideList: [],
                        tooltip: 'Next Run Time',
                        jsonName: 'nextRunTime'
                    },
                    logLink: {
                        text: '',
                        classList: ['romcell'],
                        styleOverrideList: ['width: 20px;'],
                        tooltip: 'Logs',
                        jsonName: 'correlationId'
                    },
                    startButton: {
                        text: '',
                        classList: ['romcell'],
                        styleOverrideList: ['width: 20px;'],
                        tooltip: 'Start Task',
                        jsonName: 'force'
                    }
                }

                const states = {
                    NeverStarted: {
                        icon: "",
                        text: "",
                        hoverText: "Never started"
                    },
                    Stopped: {
                        icon: "",
                        text: "",
                        hoverText: "Stopped"
                    },
                    Running: {
                        icon: "play-icon.svg",
                        text: "Running",
                        hoverText: "Running"
                    },
                    Blocked: {
                        icon: "blocked.svg",
                        text: "Blocked",
                        hoverText: "Blocked"
                    },
                    Unknown: {
                        icon: "",
                        text: "Unknown",
                        hoverText: "Unknown status"
                    }
                }

                if (result === null || result === undefined || result.length === 0) {
                    let errorReport = document.createElement('div');
                    errorReport.className = 'error-report';
                    errorReport.innerHTML = '<p>No background tasks found.</p>';
                    this.card.cardBody.querySelector('#system_tasks').appendChild(errorReport);
                    return;
                }

                // filter out disabled tasks and ImportQueueProcessor without child tasks
                result = result.filter(task => task.itemState !== "Disabled" && !(task.itemType === 'ImportQueueProcessor' && (task.childTasks === undefined || task.childTasks.length === 0)));

                // sort tasks by itemType
                result.sort((a, b) => a.itemType.localeCompare(b.itemType));

                // generate a table for the tasks
                let newTable = document.createElement('table');
                newTable.className = 'romtable';
                newTable.setAttribute('cellspacing', 0);

                // create the header row
                let headerRow = document.createElement('tr');
                headerRow.className = 'romrow taskrow';
                for (const key in columnHeaders) {
                    let header = columnHeaders[key];
                    let headerCell = document.createElement('th');
                    headerCell.className = header.classList.join(' ');
                    headerCell.style = header.styleOverrideList.join(' ');
                    headerCell.title = header.tooltip;
                    headerCell.innerHTML = header.text;

                    headerRow.appendChild(headerCell);
                }
                newTable.appendChild(headerRow);

                // iterate over the tasks and create a row for each
                for (const task of result) {
                    if (task.itemState === "Disabled") {
                        continue; // skip disabled tasks
                    }

                    if (task.itemType === 'ImportQueueProcessor' && (task.childTasks === undefined || task.childTasks.length === 0)) {
                        continue; // skip ImportQueueProcessor if no child tasks
                    }

                    // create a new row for the task
                    let taskRow = document.createElement('tbody');
                    taskRow.className = 'romrow taskrow';

                    let newRow = document.createElement('tr');

                    // create cells for each column
                    for (const key in columnHeaders) {
                        let header = columnHeaders[key];
                        let cell = document.createElement('td');
                        cell.className = header.classList.join(' ');
                        cell.style = header.styleOverrideList.join(' ');

                        // handle the specific data for each column
                        switch (header.jsonName) {
                            case 'itemState':
                                let state = task.itemState;
                                if (states[state]) {
                                    let stateIcon = states[state].icon ? `<img src='/images/${states[state].icon}' class='banner_button_image' style='padding-top: 5px;' title='${states[state].hoverText}'>` : '';
                                    cell.innerHTML = stateIcon;
                                } else {
                                    cell.innerHTML = `<img src='/images/Critical.svg' class='banner_button_image' style='padding-top: 5px;' title='Unknown status'>`;
                                }
                                break;

                            case 'itemType':
                                let itemTypeName = GetTaskFriendlyName(task.itemType, task.options);
                                cell.innerHTML = itemTypeName;
                                break;

                            case 'interval':
                                let itemInterval = task.interval;
                                if (!task.allowManualStart && task.removeWhenStopped) {
                                    itemInterval = '';
                                }
                                cell.innerHTML = itemInterval;
                                break;

                            case 'lastRunDuration':
                                cell.innerHTML = new Date(task.lastRunDuration * 1000).toISOString().slice(11, 19);
                                break;

                            case 'lastRunTime':
                                cell.innerHTML = moment(task.lastRunTime).format("YYYY-MM-DD h:mm:ss a");
                                break;

                            case 'nextRunTime':
                                cell.innerHTML = moment(task.nextRunTime).format("YYYY-MM-DD h:mm:ss a");
                                break;

                            case 'correlationId':
                                if (userProfile.roles && userProfile.roles.includes("Admin") && task.correlationId) {
                                    cell.innerHTML = '';
                                    const logLinkImg = document.createElement('img');
                                    logLinkImg.id = 'logLink';
                                    logLinkImg.className = 'banner_button_image';
                                    logLinkImg.src = '/images/log.svg';
                                    logLinkImg.title = 'Logs';
                                    logLinkImg.alt = 'Logs';
                                    logLinkImg.style.cursor = 'pointer';
                                    logLinkImg.addEventListener('click', () => {
                                        // window.location.href = `/index.html?page=settings&sub=logs&correlationid=${task.correlationId}`;
                                        this.SwitchPage('/logs', null, { correlationId: task.correlationId });
                                    });
                                    cell.appendChild(logLinkImg);
                                } else {
                                    cell.innerHTML = '';
                                }
                                break;

                            case 'force':
                                if (userProfile.roles.includes("Admin")) {
                                    if (!task.force) {
                                        if (task.allowManualStart && !["Running"].includes(task.itemState) && !task.isBlocked) {
                                            let startButton = document.createElement('img');
                                            startButton.id = 'startProcess';
                                            startButton.className = 'taskstart';
                                            startButton.src = '/images/start-task.svg';
                                            startButton.title = 'Start';
                                            startButton.alt = 'Start';
                                            startButton.addEventListener('click', () => {
                                                fetch('/api/v1.1/BackgroundTasks/' + task.itemType + '?ForceRun=true', { method: 'GET' })
                                                    .then(response => response.json())
                                                    .then(result => {
                                                        this.SystemLoadStatus();
                                                    })
                                                    .catch(error => console.error('Error starting process:', error));
                                            });
                                            cell.appendChild(startButton);
                                        }
                                    }
                                }
                                break;

                            default:
                                cell.innerHTML = ''; // default case, should not happen
                                break;
                        }

                        newRow.appendChild(cell);
                    }

                    taskRow.appendChild(newRow);

                    // add a more detailed row for the task
                    if (task.force === true && task.itemState !== "Running") {
                        // add a pending state row
                        let pendingRow = document.createElement('tr');
                        pendingRow.className = 'taskrow';
                        let pendingCell = document.createElement('td');
                        pendingCell.colSpan = Object.keys(columnHeaders).length;

                        pendingCell.innerHTML = `<table style="width: 100%;"><tr><td style="width: 25%;padding-left: 10px; padding-right: 10px;">Pending</td><td style="width: 75%; padding-left: 10px; padding-right: 10px;"><progress style="width: 100%;"></progress></td></tr></table>`;
                        pendingRow.appendChild(pendingCell);
                        taskRow.appendChild(pendingRow);
                    } else if (task.itemState === "Running" && task.currentStateProgress) {
                        // add a running state row with progress
                        let runningRow = document.createElement('tr');
                        runningRow.className = 'taskrow';
                        let runningCell = document.createElement('td');
                        runningCell.colSpan = Object.keys(columnHeaders).length;

                        if (task.currentStateProgress.includes(" of ")) {
                            let progressParts = task.currentStateProgress.split(" of ");
                            runningCell.innerHTML = `<table style="width: 100%;"><tr><td style="width: 35%;padding-left: 10px; padding-right: 10px;">Running ${task.currentStateProgress}</td><td style="width: 65%; padding-left: 10px; padding-right: 10px;"><progress value="${progressParts[0]}" max="${progressParts[1]}" style="width: 100%;">${task.currentStateProgress}</progress></td></tr></table>`;
                        } else {
                            runningCell.innerHTML = `<table style="width: 100%;"><tr><td style="padding-left: 10px; padding-right: 10px;">Running (${task.currentStateProgress})</td></tr></table>`;
                        }
                        runningRow.appendChild(runningCell);
                        taskRow.appendChild(runningRow);
                    }

                    // add sub-row for sub tasks if they exist
                    if (task.childTasks && task.childTasks.length > 0) {
                        let subRow = document.createElement('tr');
                        let subRowCell = document.createElement('td');
                        subRowCell.style.padding = '10px';
                        subRowCell.colSpan = Object.keys(columnHeaders).length; // span all columns

                        // create sub table
                        let subTable = document.createElement('table');
                        subTable.className = 'romtable';
                        subTable.setAttribute('cellspacing', 0);

                        // create header row for sub table
                        let subHeaderRow = document.createElement('tr');
                        subHeaderRow.className = 'romrow taskrow';
                        subHeaderRow.innerHTML = `
                            <th class="romcell" style="width: 20px;"></th>
                            <th class="romcell" style="width: 20%;">Task Name</th>
                            <th class="romcell" style="width: 25%;">Progress</th>
                            <th class="romcell"></th>
                            <th class="romcell" style="width: 20px;"></th>
                        `;
                        subTable.appendChild(subHeaderRow);

                        // iterate over child tasks and create a row for each
                        for (const subTask of task.childTasks) {
                            let subRow = document.createElement('tr');
                            subRow.className = 'romrow taskrow';

                            let subState = states[subTask.state] || { text: subTask.state, icon: '' };
                            let subStateIcon = subState.icon ? `<img src='/images/${subState.icon}' class='banner_button_image' style='padding-top: 5px;' title='${subState.text}'>` : '';

                            let subTaskLogLink = '';
                            if (userProfile.roles && userProfile.roles.includes("Admin") && subTask.correlationId) {
                                subTaskLogLink = subTask.correlationId ? `<img id="logLink" class="banner_button_image" src="/images/log.svg" onclick="window.location.href='/index.html?page=settings&sub=logs&correlationid=${subTask.correlationId}'" title="Logs" style="cursor: pointer;">` : '';
                            }

                            let subRowData = [
                                subStateIcon,
                                subTask.taskName,
                                subTask.currentStateProgress || '',
                                subTask.currentStateProgress ? `<progress value="${subTask.currentStateProgress.split(" of ")[0]}" max="${subTask.currentStateProgress.split(" of ")[1]}">${subTask.currentStateProgress}</progress>` : '<progress value="0" max="100"></progress>',
                                subTaskLogLink
                            ];

                            let subRowBody = document.createElement('tbody');
                            subRowBody.className = 'romrow taskrow';
                            subRowBody.appendChild(createTableRow(false, subRowData, '', 'romcell'));
                            subTable.appendChild(subRowBody);
                        }

                        subRowCell.appendChild(subTable);
                        subRow.appendChild(subRowCell);
                        taskRow.appendChild(subRow);
                    }

                    newTable.appendChild(taskRow);
                }

                // clear the previous content and append the new table
                let targetDiv = this.card.cardBody.querySelector('#system_tasks');
                targetDiv.innerHTML = '';
                targetDiv.appendChild(newTable);
            })
            .catch(error => console.error('Error fetching background tasks:', error));
    }

    async getBackgroundTaskTimers() {
        fetch('/api/v1.1/System/Settings/BackgroundTasks/Configuration', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        })
            .then(response => response.json())
            .then((result) => {
                let targetTable = this.card.cardBody.querySelector('#settings_tasktimers');
                targetTable.innerHTML = '';

                for (const [key, value] of Object.entries(result)) {
                    let enabledString = "";
                    if (value.enabled == true) {
                        enabledString = 'checked="checked"';
                    }

                    // create section
                    let serviceSection = document.createElement('div');
                    serviceSection.className = 'section';
                    // serviceSection.id = 'settings_tasktimers_' + value.task;
                    targetTable.appendChild(serviceSection);

                    // add heading
                    let serviceHeader = document.createElement('div');
                    serviceHeader.className = 'section-header';
                    serviceHeader.innerHTML = GetTaskFriendlyName(value.task);
                    serviceSection.appendChild(serviceHeader);

                    // create table for each service
                    let serviceTable = document.createElement('table');
                    serviceTable.style.width = '100%';
                    serviceTable.classList.add('section-body');

                    // add enabled
                    let newEnabledRow = document.createElement('tr');

                    let newEnabledTitle = document.createElement('td');
                    newEnabledTitle.className = 'romcell romcell-headercell';
                    newEnabledTitle.innerHTML = "Enabled:";
                    newEnabledRow.appendChild(newEnabledTitle);

                    let newEnabledContent = document.createElement('td');
                    newEnabledContent.className = 'romcell';
                    let newEnabledCheckbox = document.createElement('input');
                    newEnabledCheckbox.id = 'settings_enabled_' + value.task;
                    newEnabledCheckbox.name = 'settings_tasktimers_enabled';
                    newEnabledCheckbox.type = 'checkbox';
                    newEnabledCheckbox.checked = value.enabled;
                    newEnabledContent.appendChild(newEnabledCheckbox);
                    newEnabledRow.appendChild(newEnabledContent);

                    serviceTable.appendChild(newEnabledRow);

                    // add interval
                    let newIntervalRow = document.createElement('tr');

                    let newIntervalTitle = document.createElement('td');
                    newIntervalTitle.className = 'romcell romcell-headercell';
                    newIntervalTitle.innerHTML = "Minimum Interval (Minutes):";
                    newIntervalRow.appendChild(newIntervalTitle);

                    let newIntervalContent = document.createElement('td');
                    newIntervalContent.className = 'romcell';
                    let newIntervalInput = document.createElement('input');
                    newIntervalInput.id = 'settings_tasktimers_' + value.task;
                    newIntervalInput.name = 'settings_tasktimers_values';
                    newIntervalInput.setAttribute('data-name', value.task);
                    newIntervalInput.setAttribute('data-default', value.defaultInterval);
                    newIntervalInput.type = 'number';
                    newIntervalInput.placeholder = value.defaultInterval;
                    newIntervalInput.min = value.minimumAllowedInterval;
                    newIntervalInput.value = value.interval;
                    newIntervalContent.appendChild(newIntervalInput);
                    newIntervalRow.appendChild(newIntervalContent);

                    serviceTable.appendChild(newIntervalRow);

                    // allowed time periods row
                    let newTableRowTime = document.createElement('tr');

                    let rowTimeContentTitle = document.createElement('td');
                    rowTimeContentTitle.className = 'romcell romcell-headercell';
                    rowTimeContentTitle.innerHTML = "Allowed Days:";
                    newTableRowTime.appendChild(rowTimeContentTitle);

                    let rowTimeContent = document.createElement('td');
                    // rowTimeContent.setAttribute('colspan', 2);
                    rowTimeContent.className = 'romcell';
                    let daySelector = document.createElement('select');
                    daySelector.id = 'settings_alloweddays_' + value.task;
                    daySelector.name = 'settings_alloweddays';
                    daySelector.multiple = 'multiple';
                    daySelector.setAttribute('data-default', value.defaultAllowedDays.join(","));
                    daySelector.style.width = '95%';
                    let days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
                    for (let d = 0; d < days.length; d++) {
                        let dayOpt = document.createElement('option');
                        dayOpt.value = days[d];
                        dayOpt.innerHTML = days[d];
                        if (value.allowedDays.includes(days[d])) {
                            dayOpt.selected = 'selected';
                        }
                        daySelector.appendChild(dayOpt);
                    }
                    rowTimeContent.appendChild(daySelector);
                    $(daySelector).select2({
                        tags: false
                    });
                    newTableRowTime.appendChild(rowTimeContent);

                    serviceTable.appendChild(newTableRowTime);

                    // add start and end times
                    let newTableRowClock = document.createElement('tr');

                    let rowClockContentTitle = document.createElement('td');
                    rowClockContentTitle.className = 'romcell romcell-headercell';
                    rowClockContentTitle.innerHTML = "Time Range:";
                    newTableRowClock.appendChild(rowClockContentTitle);

                    let rowClockContent = document.createElement('td');
                    rowClockContent.className = 'romcell';
                    // rowClockContent.setAttribute('colspan', 2);

                    rowClockContent.appendChild(this.generateTimeDropDowns(value.task, 'Start', value.defaultAllowedStartHours, value.defaultAllowedStartMinutes, value.allowedStartHours, value.allowedStartMinutes));

                    let rowClockContentSeparator = document.createElement('span');
                    rowClockContentSeparator.innerHTML = '&nbsp;-&nbsp;';
                    rowClockContent.appendChild(rowClockContentSeparator);

                    rowClockContent.appendChild(this.generateTimeDropDowns(value.task, 'End', value.defaultAllowedEndHours, value.defaultAllowedEndMinutes, value.allowedEndHours, value.allowedEndMinutes));

                    newTableRowClock.appendChild(rowClockContent);

                    serviceTable.appendChild(newTableRowClock);

                    // blocks tasks
                    let newTableRowBlocks = document.createElement('tr');

                    let rowBlocksContentTitle = document.createElement('td');
                    rowBlocksContentTitle.className = 'romcell romcell-headercell';
                    rowBlocksContentTitle.innerHTML = "Blocks:";
                    newTableRowBlocks.appendChild(rowBlocksContentTitle);

                    let rowBlocksContent = document.createElement('td');
                    rowBlocksContent.className = 'romcell';
                    // rowBlocksContent.setAttribute('colspan', 2);
                    let blocksString = "";
                    for (let i = 0; i < value.blocks.length; i++) {
                        if (blocksString.length > 0) { blocksString += ", "; }
                        blocksString += GetTaskFriendlyName(value.blocks[i]);
                    }
                    if (blocksString.length == 0) { blocksString = 'None'; }
                    rowBlocksContent.innerHTML = blocksString;
                    newTableRowBlocks.appendChild(rowBlocksContent);

                    serviceTable.appendChild(newTableRowBlocks);

                    // blocked by tasks
                    let newTableRowBlockedBy = document.createElement('tr');

                    let rowBlockedByContentTitle = document.createElement('td');
                    rowBlockedByContentTitle.className = 'romcell romcell-headercell';
                    rowBlockedByContentTitle.innerHTML = "Blocked By:";
                    newTableRowBlockedBy.appendChild(rowBlockedByContentTitle);

                    let rowBlockedByContent = document.createElement('td');
                    rowBlockedByContent.className = 'romcell';
                    // rowBlockedByContent.setAttribute('colspan', 2);
                    let BlockedByString = "";
                    for (let i = 0; i < value.blockedBy.length; i++) {
                        if (BlockedByString.length > 0) { BlockedByString += ", "; }
                        BlockedByString += GetTaskFriendlyName(value.blockedBy[i]);
                    }
                    if (BlockedByString.length == 0) { BlockedByString = 'None'; }
                    rowBlockedByContent.innerHTML = BlockedByString;
                    newTableRowBlockedBy.appendChild(rowBlockedByContent);

                    serviceTable.appendChild(newTableRowBlockedBy);

                    // complete row
                    serviceSection.appendChild(serviceTable);
                }
            }
            );
    }

    generateTimeDropDowns(taskName, rangeName, defaultHour, defaultMinute, valueHour, valueMinute) {
        let container = document.createElement('div');
        container.style.display = 'inline';

        let elementName = 'settings_tasktimers_time';

        let hourSelector = document.createElement('input');
        hourSelector.id = 'settings_tasktimers_' + taskName + '_' + rangeName + '_Hour';
        hourSelector.name = elementName;
        hourSelector.setAttribute('data-name', taskName);
        hourSelector.setAttribute('type', 'number');
        hourSelector.setAttribute('min', '0');
        hourSelector.setAttribute('max', '23');
        hourSelector.setAttribute('placeholder', defaultHour);
        hourSelector.value = valueHour;
        container.appendChild(hourSelector);

        let separator = document.createElement('span');
        separator.innerHTML = " : ";
        container.appendChild(separator);

        let minSelector = document.createElement('input');
        minSelector.id = 'settings_tasktimers_' + taskName + '_' + rangeName + '_Minute';
        minSelector.name = elementName;
        minSelector.setAttribute('type', 'number');
        minSelector.setAttribute('min', '0');
        minSelector.setAttribute('max', '59');
        minSelector.setAttribute('placeholder', defaultMinute);
        minSelector.value = valueMinute;
        container.appendChild(minSelector);

        return container;
    }

    async saveTaskTimers() {
        let timerValues = this.card.cardBody.querySelectorAll('[name="settings_tasktimers_values"]');

        let model = [];
        for (let i = 0; i < timerValues.length; i++) {
            let taskName = timerValues[i].getAttribute('data-name');
            let taskEnabled = this.card.cardBody.querySelector('#settings_enabled_' + taskName).checked;
            let taskIntervalObj = this.card.cardBody.querySelector('#settings_tasktimers_' + taskName);
            let taskInterval = function () { if (taskIntervalObj.value) { return taskIntervalObj.value; } else { return taskIntervalObj.getAttribute('placeholder'); } };
            let taskDaysRaw = $('#settings_alloweddays_' + taskName).select2('data');
            let taskDays = [];
            if (taskDaysRaw.length > 0) {
                for (let d = 0; d < taskDaysRaw.length; d++) {
                    taskDays.push(taskDaysRaw[d].id);
                }
            } else {
                taskDays.push("Monday");
            }
            let taskStartHourObj = this.card.cardBody.querySelector('#settings_tasktimers_' + taskName + '_Start_Hour');
            let taskStartMinuteObj = this.card.cardBody.querySelector('#settings_tasktimers_' + taskName + '_Start_Minute');
            let taskEndHourObj = this.card.cardBody.querySelector('#settings_tasktimers_' + taskName + '_End_Hour');
            let taskEndMinuteObj = this.card.cardBody.querySelector('#settings_tasktimers_' + taskName + '_End_Minute');

            let taskStartHour = function () { if (taskStartHourObj.value) { return taskStartHourObj.value; } else { return taskStartHourObj.getAttribute('placeholder'); } };
            let taskStartMinute = function () { if (taskStartMinuteObj.value) { return taskStartMinuteObj.value; } else { return taskStartMinuteObj.getAttribute('placeholder'); } };
            let taskEndHour = function () { if (taskEndHourObj.value) { return taskEndHourObj.value; } else { return taskEndHourObj.getAttribute('placeholder'); } };
            let taskEndMinute = function () { if (taskEndMinuteObj.value) { return taskEndMinuteObj.value; } else { return taskEndMinuteObj.getAttribute('placeholder'); } };

            model.push(
                {
                    "task": taskName,
                    "enabled": taskEnabled,
                    "interval": taskInterval(),
                    "allowedDays": taskDays,
                    "allowedStartHours": taskStartHour(),
                    "allowedStartMinutes": taskStartMinute(),
                    "allowedEndHours": taskEndHour(),
                    "allowedEndMinutes": taskEndMinute()
                }
            );
        }

        await fetch('/api/v1.1/System/Settings/BackgroundTasks/Configuration',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(model)
            }
        ).then((result) => {
            this.getBackgroundTaskTimers();
        }
        ).catch((error) => {
            console.error('Error saving task timers:', error);
        });
    }

    defaultTaskTimers() {
        let taskEnabled = this.card.cardBody.querySelectorAll('[name="settings_tasktimers_enabled"]');

        for (let i = 0; i < taskEnabled.length; i++) {
            taskEnabled[i].checked = true;
        }

        let taskTimerValues = this.card.cardBody.querySelectorAll('[name="settings_tasktimers_values"]');

        for (let i = 0; i < taskTimerValues.length; i++) {
            taskTimerValues[i].value = taskTimerValues[i].getAttribute('data-default');
        }

        let taskAllowedDays = this.card.cardBody.querySelectorAll('[name="settings_alloweddays"]');

        for (let i = 0; i < taskAllowedDays.length; i++) {
            let defaultSelections = taskAllowedDays[i].getAttribute('data-default').split(',');
            $(taskAllowedDays[i]).val(defaultSelections);
            $(taskAllowedDays[i]).trigger('change');
        }

        let taskTimes = this.card.cardBody.querySelectorAll('[name="settings_tasktimers_time"]');

        for (let i = 0; i < taskTimes.length; i++) {
            taskTimes[i].value = taskTimes[i].getAttribute('placeholder');
        }

        this.saveTaskTimers();
    }

    async drawLibrary() {
        await fetch('/api/v1.1/Library?GetStorageInfo=true', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        })
            .then(response => response.json())
            .then((result) => {
                let newTable = this.card.cardBody.querySelector('#settings_libraries');
                newTable.innerHTML = '';
                console.log(result);
                for (let library of result) {
                    let container = document.createElement('div');
                    container.classList.add('section');

                    let header = document.createElement('div');
                    header.classList.add('section-header');

                    let headerText = document.createElement('span');
                    headerText.innerHTML = library.name;
                    header.appendChild(headerText);

                    let body = document.createElement('div');
                    body.classList.add('section-body');

                    let libraryTable = document.createElement('table');
                    libraryTable.style.width = '100%';
                    libraryTable.style.borderCollapse = 'collapse';

                    let pathRow = document.createElement('tr');
                    let pathLabel = document.createElement('td');
                    pathLabel.style.width = '20%';
                    pathLabel.innerHTML = 'Path';
                    let pathValue = document.createElement('td');

                    let controlsCell = document.createElement('td');
                    controlsCell.style.width = '20%';
                    controlsCell.style.textAlign = 'right';
                    controlsCell.rowSpan = 3;

                    if (!library.isDefaultLibrary) {
                        let deleteButton = document.createElement('a');
                        deleteButton.href = '#';
                        deleteButton.style.marginRight = '10px';
                        deleteButton.addEventListener('click', () => {
                            let deleteLibrary = new MessageBox('Delete Library', 'Are you sure you want to delete this library?<br /><br /><strong>Warning</strong>: This cannot be undone!');
                            deleteLibrary.addButton(new ModalButton('OK', 2, deleteLibrary, async (callingObject) => {
                                await fetch('/api/v1.1/Library/' + library.id, {
                                    method: 'DELETE',
                                    headers: {
                                        'Content-Type': 'application/json'
                                    }
                                }).then(response => response.json())
                                    .then(() => {
                                        callingObject.msgDialog.close();
                                        this.drawLibrary();
                                    })
                                    .catch(() => {
                                        callingObject.msgDialog.close();
                                        this.drawLibrary();
                                    }
                                    );
                            }));


                            deleteLibrary.addButton(new ModalButton('Cancel', 0, deleteLibrary, function (callingObject) {
                                callingObject.msgDialog.close();
                            }));

                            deleteLibrary.open();
                        });
                        let deleteButtonImage = document.createElement('img');
                        deleteButtonImage.src = '/images/delete.svg';
                        deleteButtonImage.className = 'banner_button_image';
                        deleteButtonImage.alt = 'Delete';
                        deleteButtonImage.title = 'Delete';
                        deleteButton.appendChild(deleteButtonImage);
                        controlsCell.appendChild(deleteButton);

                        let editButton = document.createElement('a');
                        editButton.href = '#';
                        editButton.addEventListener('click', async () => {
                            let newLibrary = new NewLibrary(this, library.id);
                            await newLibrary.open();
                            newLibrary.DialogName.innerHTML = "Edit Library";
                            newLibrary.LibraryName.value = library.name;
                            newLibrary.LibraryPath.value = library.path;
                            newLibrary.LibraryPath.disabled = true;
                            newLibrary.pathSelector.disabled = true;
                            if (library.defaultPlatformId !== 0 && library.defaultPlatformId !== "0") {
                                var newOption = new Option(library.defaultPlatformName, library.defaultPlatformId, true, true); // text, value, isSelected, isTriggered
                                $(newLibrary.defaultPlatformSelector).append(newOption).trigger('change');
                            }
                            await this.drawLibrary();
                        });
                        let editButtonImage = document.createElement('img');
                        editButtonImage.src = '/images/edit.svg';
                        editButtonImage.className = 'banner_button_image';
                        editButtonImage.alt = 'Edit';
                        editButtonImage.title = 'Edit';
                        editButton.appendChild(editButtonImage);
                        controlsCell.appendChild(editButton);
                    }

                    let scanButton = document.createElement('img');
                    scanButton.classList.add('taskstart');
                    scanButton.src = '/images/start-task.svg';
                    scanButton.title = 'Start Scan';
                    scanButton.alt = 'Start Scan';
                    scanButton.addEventListener('click', function () {
                        let scanLibrary = new MessageBox('Scan Library', 'Are you sure you want to scan this library?');
                        scanLibrary.addButton(new ModalButton('OK', 2, scanLibrary, async (callingObject) => {
                            await fetch('/api/v1.1/Library/' + library.id + '/Scan', {
                                method: 'POST',
                                headers: {
                                    'Content-Type': 'application/json'
                                }
                            }).then(response => response.json())
                                .then(() => {
                                    callingObject.msgDialog.close();
                                    this.drawLibrary();
                                })
                                .catch(() => {
                                    callingObject.msgDialog.close();
                                    this.drawLibrary();
                                }
                                );
                        }));

                        scanLibrary.addButton(new ModalButton('Cancel', 0, scanLibrary, function (callingObject) {
                            callingObject.msgDialog.close();
                        }));

                        scanLibrary.open();
                    });
                    controlsCell.appendChild(scanButton);

                    let pathValueText = document.createElement('span');
                    pathValueText.innerHTML = library.path;
                    pathValue.appendChild(pathValueText);
                    pathRow.appendChild(pathLabel);
                    pathRow.appendChild(pathValue);
                    pathRow.appendChild(controlsCell);

                    let platformRow = document.createElement('tr');
                    let platformLabel = document.createElement('td');
                    platformLabel.innerHTML = 'Default Platform';
                    let platformValue = document.createElement('td');
                    platformValue.innerHTML = library.defaultPlatformName || 'n/a';
                    platformRow.appendChild(platformLabel);
                    platformRow.appendChild(platformValue);

                    let libraryRow = document.createElement('tr');
                    let libraryLabel = document.createElement('td');
                    libraryLabel.innerHTML = 'Default Library';
                    let libraryValue = document.createElement('td');
                    libraryValue.innerHTML = library.isDefaultLibrary ? 'Yes' : 'No';
                    libraryRow.appendChild(libraryLabel);
                    libraryRow.appendChild(libraryValue);

                    libraryTable.appendChild(pathRow);
                    libraryTable.appendChild(platformRow);
                    libraryTable.appendChild(libraryRow);

                    if (library.pathInfo) {
                        let storageRow = document.createElement('tr');
                        let storageLabel = document.createElement('td');
                        storageLabel.colSpan = 3;
                        storageLabel.style.paddingTop = '10px';

                        let spaceUsedByLibrary = library.pathInfo.spaceUsed;
                        let spaceUsedByOthers = library.pathInfo.totalSpace - library.pathInfo.spaceAvailable;
                        storageLabel.appendChild(BuildSpaceBar(spaceUsedByLibrary, spaceUsedByOthers, library.pathInfo.totalSpace));
                        storageRow.appendChild(storageLabel);

                        libraryTable.appendChild(storageRow);
                    }

                    body.appendChild(libraryTable);

                    container.appendChild(header);
                    container.appendChild(body);

                    newTable.appendChild(container);
                }
            }
            );
    }

    async GetUsers() {
        console.log("Loading users...");
        let targetDiv = this.card.cardBody.querySelector('#settings_users_table_container');
        targetDiv.innerHTML = '';

        fetch('/api/v1.1/Account/Users', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(response => response.json())
            .then((result) => {
                let newTable = document.createElement('table');
                newTable.className = 'romtable';
                newTable.style.width = '100%';
                newTable.cellSpacing = 0;

                let headerRow = document.createElement('tr');
                headerRow.className = 'romrow';

                let headerCell1 = document.createElement('th');
                headerCell1.classList.add('romcell');
                headerCell1.style.width = '32px'; // Avatar width
                headerRow.appendChild(headerCell1);

                let headerCell2 = document.createElement('th');
                headerCell2.classList.add('romcell');
                headerCell2.innerHTML = 'User name';
                headerRow.appendChild(headerCell2);

                let headerCell3 = document.createElement('th');
                headerCell3.classList.add('romcell');
                headerCell3.classList.add('card-services-column');
                headerCell3.innerHTML = 'Role';
                headerRow.appendChild(headerCell3);

                let headerCell4 = document.createElement('th');
                headerCell4.classList.add('romcell');
                headerCell4.classList.add('card-services-column');
                headerCell4.innerHTML = 'Age Restriction';
                headerRow.appendChild(headerCell4);

                let headerCell5 = document.createElement('th');
                headerCell5.className = 'romcell';
                headerRow.appendChild(headerCell5);

                let headerCell6 = document.createElement('th');
                headerCell6.className = 'romcell';
                headerRow.appendChild(headerCell6);

                newTable.appendChild(headerRow);

                for (const user of result) {
                    let userAvatar = new Avatar(user.profileId, 32, 32, true);
                    userAvatar.classList.add("user_list_icon");

                    let roleDiv = document.createElement('div');

                    let roleItem = CreateBadge(user.highestRole);
                    roleDiv.appendChild(roleItem);

                    let ageRestrictionPolicyDescription = document.createElement('div');
                    if (user.securityProfile != null) {
                        if (user.securityProfile.ageRestrictionPolicy != null) {
                            let IncludeUnratedText = '';
                            if (user.securityProfile.ageRestrictionPolicy.includeUnrated) {
                                IncludeUnratedText = " &#43; Unclassified titles";
                            }

                            let restrictionText = user.securityProfile.ageRestrictionPolicy.maximumAgeRestriction + IncludeUnratedText;

                            ageRestrictionPolicyDescription = CreateBadge(restrictionText);
                        }
                    }

                    let controls = document.createElement('div');
                    controls.style.textAlign = 'right';

                    let editButton;
                    let deleteButton;

                    if (userProfile.userId != user.id) {
                        editButton = document.createElement('a');
                        editButton.href = '#';
                        let thisObject = this;
                        editButton.addEventListener('click', () => {
                            let userEdit = new UserEdit(user.id, async () => { await thisObject.GetUsers(); });
                            userEdit.open();
                        });
                        editButton.classList.add('romlink');

                        let editButtonImage = document.createElement('img');
                        editButtonImage.src = '/images/edit.svg';
                        editButtonImage.classList.add('banner_button_image');
                        editButtonImage.alt = 'Edit';
                        editButtonImage.title = 'Edit';
                        editButton.appendChild(editButtonImage);

                        deleteButton = document.createElement('a');
                        deleteButton.href = '#';
                        deleteButton.addEventListener('click', () => {
                            let warningDialog = new MessageBox("Delete User", "Are you sure you want to delete this user?<br /><br /><strong>Warning</strong>: This cannot be undone!");
                            const handleDelete = async (callingObject) => {
                                try {
                                    const response = await fetch("/api/v1.1/Account/Users/" + user.id, {
                                        method: 'DELETE'
                                    });
                                    if (response.ok) {
                                        this.GetUsers();
                                        callingObject.msgDialog.close();
                                    } else {
                                        let warningDialogError = new MessageBox("Delete User Error", "An error occurred while deleting the user.");
                                        warningDialogError.open();
                                    }
                                } catch (error) {
                                    let warningDialogError = new MessageBox("Delete User Error", "An error occurred while deleting the user.");
                                    warningDialogError.open();
                                }
                            };
                            warningDialog.addButton(new ModalButton("OK", 2, warningDialog, handleDelete));
                            warningDialog.addButton(new ModalButton("Cancel", 0, warningDialog, function (callingObject) {
                                callingObject.msgDialog.close();
                            }));
                            warningDialog.open();
                        });
                        deleteButton.classList.add('romlink');

                        let deleteButtonImage = document.createElement('img');
                        deleteButtonImage.src = '/images/delete.svg';
                        deleteButtonImage.classList.add('banner_button_image');
                        deleteButtonImage.alt = 'Delete';
                        deleteButtonImage.title = 'Delete';
                        deleteButton.appendChild(deleteButtonImage);
                    }

                    // create the table row for the user
                    let userRow = document.createElement('tr');
                    userRow.classList.add('romrow');

                    // create the table cells for the user
                    let userAvatarCell = document.createElement('td');
                    userAvatarCell.classList.add('romcell');
                    userAvatarCell.style.width = '32px'; // Avatar width
                    userAvatarCell.appendChild(userAvatar);
                    userRow.appendChild(userAvatarCell);

                    let userEmailCell = document.createElement('td');
                    userEmailCell.classList.add('romcell');
                    if (user.userName !== null && user.userName.length > 0 && user.userName !== user.emailAddress) {
                        userEmailCell.innerHTML = user.userName;
                    } else {
                        userEmailCell.innerHTML = user.emailAddress;
                    }
                    userRow.appendChild(userEmailCell);

                    let userRoleCell = document.createElement('td');
                    userRoleCell.classList.add('romcell');
                    userRoleCell.classList.add('card-services-column');
                    userRoleCell.appendChild(roleDiv);
                    userRow.appendChild(userRoleCell);

                    let ageRestrictionCell = document.createElement('td');
                    ageRestrictionCell.classList.add('romcell');
                    ageRestrictionCell.classList.add('card-services-column');
                    ageRestrictionCell.appendChild(ageRestrictionPolicyDescription);
                    userRow.appendChild(ageRestrictionCell);

                    let controlsCell = document.createElement('td');
                    controlsCell.className = 'romcell';
                    if (editButton) {
                        controlsCell.appendChild(editButton);
                    }
                    userRow.appendChild(controlsCell);

                    let controlsCell2 = document.createElement('td');
                    controlsCell2.className = 'romcell';
                    if (deleteButton) {
                        controlsCell2.appendChild(deleteButton);
                    }
                    userRow.appendChild(controlsCell2);

                    // append the user row to the new table
                    newTable.appendChild(userRow);
                }

                targetDiv.appendChild(newTable);
            }
            );
    }

    async setupAboutPage() {
        let appVersionBox = this.card.cardBody.querySelector('#settings_appversion');
        if (AppVersion == "1.5.0.0") {
            appVersionBox.innerHTML = "Built from source";
        } else {
            appVersionBox.innerHTML = AppVersion;
        }

        let dbVersionBox = this.card.cardBody.querySelector('#settings_dbversion');
        dbVersionBox.innerHTML = DBSchemaVersion;
    }

    async loadPlatformMapping(Overwrite) {
        let queryString = '';
        if (Overwrite == true) {
            console.log('Overwriting PlatformMap.json');
            queryString = '?ResetToDefault=true';
        }

        console.log('Loading platform mappings');

        await fetch('/api/v1.1/PlatformMaps' + queryString, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(async response => await response.json())
            .then((result) => {
                let newTable = document.getElementById('settings_mapping_table');

                newTable.innerHTML = '';
                newTable.appendChild(
                    createTableRow(
                        true,
                        [
                            '',
                            'Platform',
                            'Supported File Extensions',
                            'Unique File Extensions',
                            'Has Web Emulator',
                            ''
                        ],
                        '',
                        ''
                    )
                );

                for (const platform of result) {
                    let logoBox = document.createElement('div');
                    logoBox.classList.add('platform_image_container');

                    let logo = document.createElement('img');
                    logo.src = '/api/v1.1/Platforms/' + platform.igdbId + '/platformlogo/original/';
                    logo.alt = platform.igdbName;
                    logo.title = platform.igdbName;
                    logo.classList.add('platform_image');

                    logoBox.appendChild(logo);

                    let hasWebEmulator = '';
                    if (platform.webEmulator.type.length > 0) {
                        hasWebEmulator = 'Yes';
                    }

                    let platformEditButton = null;
                    if (userProfile.roles.includes("Admin")) {
                        platformEditButton = document.createElement('div');
                        platformEditButton.classList.add('romlink');
                        platformEditButton.addEventListener('click', () => {
                            let mappingModal = new Mapping(platform.igdbId, this.loadPlatformMapping);
                            mappingModal.OKCallback = this.loadPlatformMapping.bind(this);
                            console.log(mappingModal);
                            mappingModal.open();
                        });
                        let editButtonImage = document.createElement('img');
                        editButtonImage.src = '/images/edit.svg';
                        editButtonImage.alt = 'Edit';
                        editButtonImage.title = 'Edit';
                        editButtonImage.classList.add('banner_button_image');
                        platformEditButton.appendChild(editButtonImage);
                    }

                    let newRow = [
                        logoBox,
                        platform.igdbName,
                        platform.extensions.supportedFileExtensions.join(', '),
                        platform.extensions.uniqueFileExtensions.join(', '),
                        hasWebEmulator,
                        platformEditButton
                    ];

                    newTable.appendChild(createTableRow(false, newRow, 'romrow', 'romcell logs_table_cell'));
                }
            }
            );
    }

    DownloadJSON() {
        window.location = '/api/v1.1/PlatformMaps/PlatformMap.json';
    }

    async SetupButtons() {
        if (userProfile.roles.includes("Admin")) {
            this.card.cardBody.querySelector('#settings_mapping_import').style.display = '';

            // Setup the JSON import button
            this.card.cardBody.querySelector('#uploadjson').addEventListener('change', function () {
                $(this).simpleUpload("/api/v1.1/PlatformMaps", {
                    start: function (file) {
                        //upload started
                        console.log("JSON upload started");
                    },
                    success: function (data) {
                        //upload successful
                        window.location.reload();
                    }
                });
            });

            this.card.cardBody.querySelector('#importjson').addEventListener('click', () => {
                this.card.cardBody.querySelector('#uploadjson').click();
            });

            // Setup the JSON export button
            this.card.cardBody.querySelector('#exportjson').addEventListener('click', this.DownloadJSON);

            // Setup the reset to defaults button
            this.card.cardBody.querySelector('#resetmapping').addEventListener('click', () => {
                let warningDialog = new MessageBox("Platform Mapping Reset", "This will reset the platform mappings to the default values. Are you sure you want to continue?");
                warningDialog.addButton(new ModalButton("OK", 2, warningDialog, async (callingObject) => {
                    this.loadPlatformMapping(true);
                    callingObject.msgDialog.close();
                    let completedDialog = new MessageBox("Platform Mapping Reset", "All platform mappings have been reset to default values.");
                    completedDialog.open();
                }));
                warningDialog.addButton(new ModalButton("Cancel", 0, warningDialog, async (callingObject) => {
                    callingObject.msgDialog.close();
                }));
                warningDialog.open();
            });
        }
    }

    LoadLogs(information = null, warning = null, critical = null, startDateTime = null, endDateTime = null, searchText = null, correlationId = null) {
        let logViewer = new LogViewer(information, warning, critical, startDateTime, endDateTime, searchText, correlationId);
        let logsBody = this.card.cardBody.querySelector('#logsbody');
        logsBody.innerHTML = '';
        logsBody.appendChild(logViewer.Render());
    }
}