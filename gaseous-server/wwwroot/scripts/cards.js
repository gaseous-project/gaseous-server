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

    async Open() {
        // hide the scroll bar for the page
        document.body.style.overflow = "hidden";

        // set the header
        // this.SetHeader("Heading", false);

        // set the background image
        // this.SetBackgroundImage('/images/SettingsWallpaper.jpg');

        // show the modal
        $(this.modalBackground).fadeIn(200);
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

    SetBackgroundImage(Url, blur = false, callback) {
        this.cardBackground.src = Url;
        this.cardBackground.onload = () => {
            // get the average colour of the image
            let rgbAverage = getAverageRGB(this.cardBackground);
            this.card.style.backgroundColor = 'rgb(' + rgbAverage.r + ', ' + rgbAverage.g + ', ' + rgbAverage.b + ')';
            this.cardGradient.style.background = 'linear-gradient(180deg, rgba(0, 0, 0, 0) 0%, rgba(' + rgbAverage.r + ', ' + rgbAverage.g + ', ' + rgbAverage.b + ', 1) 100%)';
            if (blur === true) {
                this.cardBackground.classList.add('card-background-blurred');
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
            // Remove the modal element from the document body
            if (this.modalBackground) {
                this.modalBackground.remove();
                this.modalBackground = null;
            }

            // Show the scroll bar for the page
            if (document.getElementsByClassName('modal-window-body').length === 0) {
                document.body.style.overflow = 'auto';
            }
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
            let artworkUrl = `/api/v1.1/Games/${gameData.metadataMapId}/${gameData.metadataSource}/artwork/${artwork}/image/original/${artwork}.jpg`;
            this.card.SetBackgroundImage(artworkUrl, false, () => {
                if (this.card.contrastColour !== 'fff') {
                    let ratingIgdbLogo = this.card.cardBody.querySelector('#card-userrating-igdb-logo');
                    ratingIgdbLogo.classList.add('card-info-rating-icon-black');
                }
            });
        } else {
            if (gameData.cover) {
                let coverUrl = `/api/v1.1/Games/${gameData.metadataMapId}/${gameData.metadataSource}/cover/${gameData.cover}/image/original/${gameData.cover}.jpg`;
                this.card.SetBackgroundImage(coverUrl, false, () => {
                    if (this.card.contrastColour !== 'fff') {
                        let ratingIgdbLogo = this.card.cardBody.querySelector('#card-userrating-igdb-logo');
                        ratingIgdbLogo.classList.add('card-info-rating-icon-black');
                    }
                });
            } else {
                this.card.SetBackgroundImage('/images/SettingsWallpaper.jpg', false, () => {
                    if (this.card.contrastColour !== 'fff') {
                        let ratingIgdbLogo = this.card.cardBody.querySelector('#card-userrating-igdb-logo');
                        ratingIgdbLogo.classList.add('card-info-rating-icon-black');
                    }
                });
            }
        }

        // set the cover art
        let coverImg = this.card.cardBody.querySelector('#card-cover');
        if (coverImg) {
            if (gameData.cover) {
                coverImg.src = `/api/v1.1/Games/${gameData.metadataMapId}/${gameData.metadataSource}/cover/${gameData.cover}/image/cover_big/${gameData.cover}.jpg`;
            } else {
                coverImg.src = '/images/unknowngame.png';
            }
            coverImg.alt = gameData.name;
            coverImg.title = gameData.name;
            coverImg.style.display = '';
        }

        // set the game name
        let gameName = this.card.cardBody.querySelector('#card-title');
        if (gameName) {
            gameName.innerHTML = gameData.name;
            gameName.style.display = '';
        }

        // set the game rating
        let ageRating = this.card.cardBody.querySelector('#card-rating');
        if (gameData.age_ratings) {
            fetch(`/api/v1.1/Games/${gameData.metadataMapId}/${gameData.metadataSource}/agerating`, {
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
                                    console.log(dataElement);
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
            fetch(`/api/v1.1/Games/${gameData.metadataMapId}/${gameData.metadataSource}/companies`, {
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
            fetch(`/api/v1.1/Games/${gameData.metadataMapId}/${gameData.metadataSource}/genre`, {
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
            fetch(`/api/v1.1/Games/${gameData.metadataMapId}/${gameData.metadataSource}/themes`, {
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
        if (screenshots) {
            if (gameData.screenshots) {
                gameData.screenshots.forEach(screenshot => {
                    let screenshotItem = document.createElement('li');
                    screenshotItem.classList.add('card-screenshot-item');

                    let screenshotImg = document.createElement('img');
                    screenshotImg.src = `/api/v1.1/Games/${gameData.metadataMapId}/${gameData.metadataSource}/screenshots/${screenshot}/image/screenshot_med/${screenshot}.jpg`;
                    screenshotImg.alt = gameData.name;
                    screenshotImg.title = gameData.name;
                    screenshotItem.appendChild(screenshotImg);
                    screenshots.appendChild(screenshotItem);
                });
                screenshots.style.display = '';
            }
        }

        // set the game summary
        let gameSummary = this.card.cardBody.querySelector('#card-summary');
        let gameSummarySection = this.card.cardBody.querySelector('#card-summary-section');
        if (gameSummary) {
            gameSummary.innerHTML = gameData.summary;
            gameSummarySection.style.display = '';

            if (gameSummary.offsetHeight < gameSummary.scrollHeight ||
                gameSummary.offsetWidth < gameSummary.scrollWidth) {
                // your element has overflow and truncated
                // show read more / read less button
                document.querySelector('#card-summary-full').setAttribute('style', '');
            } else {
                // your element doesn't overflow (not truncated)
            }
        }



        // show the card
        this.card.Open();
    }
}