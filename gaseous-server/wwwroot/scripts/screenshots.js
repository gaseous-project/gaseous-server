/*
    ScreenshotItem class
    Represents a screenshot or other media item associated with metadata.
    Used in an array to hold structured data for screenshots, photos, YouTube links, and videos.
*/
class ScreenshotItem {
    /*
    type: The type of content (e.g., 'screenshot', 'photo', 'youtube', 'video').
    url: The URL to access the content.
    title: The title of the content item.
    uploadDate: The date the content was uploaded.
    description: A brief description of the content item.
    metadataId: The ID of the associated metadata.
    profile: The user profile object associated with the content item.
    */
    constructor(id, source, type, url, title, uploadDate, description, metadataId, profile) {
        this.id = id;
        this.source = source;
        this.type = type;
        this.url = url;
        this.title = title;
        this.uploadDate = uploadDate;
        this.description = description;
        this.metadataId = metadataId;
        this.profile = profile;
    }

    /*
    createPreviewElement: Creates and returns a DOM element representing the preview of the screenshot or media item.
    */
    createPreviewElement() {
        return this.#createScreenshotElement('card-screenshot-item');
    }

    /*
    createFullElement: Creates and returns a DOM element representing the full view of the screenshot or media item.
    */
    createFullElement() {
        return this.#createScreenshotElement('card-screenshot-big', true);
    }

    /*
    #createScreenshotElement: Internal method to create a DOM element based on the type of content.
    baseClass: The base CSS class to apply to the created element.
    Returns the created DOM element.
    */
    #createScreenshotElement(baseClass, isFull = false) {
        let container = document.createElement('div');
        container.classList.add(baseClass);
        container.setAttribute('data-id', this.id);
        container.setAttribute('data-type', this.type);
        if (this.source) {
            container.setAttribute('data-source', this.source);
        }

        switch (this.type) {
            case 'screenshot':
                if (isFull === false) {
                    container.style.backgroundImage = `url(${this.url})`;
                } else {
                    let img = document.createElement('img');
                    img.src = this.url;
                    img.alt = this.title || 'Screenshot';
                    img.style.maxWidth = "100%";
                    img.style.maxHeight = "100%";
                    container.appendChild(img);
                }
                // add screenshot icon overlay
                let screenshotIcon = document.createElement('div');
                screenshotIcon.classList.add('card-screenshot-icon');
                screenshotIcon.classList.add('card-screenshot-screenshot-icon');
                container.appendChild(screenshotIcon);
                break;

            case 'photo':
                if (isFull === false) {
                    container.style.backgroundImage = `url(${this.url})`;
                } else {
                    let img = document.createElement('img');
                    img.src = this.url;
                    img.alt = this.title || 'Photo';
                    img.style.maxWidth = "100%";
                    img.style.maxHeight = "100%";
                    container.appendChild(img);
                }

                // add photo icon overlay
                let photoIcon = document.createElement('div');
                photoIcon.classList.add('card-screenshot-icon');
                photoIcon.classList.add('card-screenshot-photo-icon');
                container.appendChild(photoIcon);
                break;

            case 'youtube':
                let videoId = this.extractYouTubeId(this.url);
                if (videoId) {
                    if (isFull === false) {
                        container.style.backgroundImage = `url(https://i.ytimg.com/vi/${videoId}/hqdefault.jpg)`;
                    } else {
                        let iframe = document.createElement('iframe');
                        iframe.src = `https://www.youtube.com/embed/${videoId}`;
                        // iframe.setAttribute("width", "100%");
                        // iframe.style.aspectRatio = "16 / 9";
                        // iframe.setAttribute("height", "100%");
                        iframe.setAttribute("frameborder", "0");
                        iframe.setAttribute("allow", "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share");
                        container.appendChild(iframe);
                    }
                } else {
                    container.alt = 'Invalid YouTube URL';
                }

                // add youtube icon overlay
                let ytIcon = document.createElement('div');
                ytIcon.classList.add('card-screenshot-icon');
                ytIcon.classList.add('card-screenshot-youtube-icon');
                container.appendChild(ytIcon);
                break;

            case 'video':
                let vidObj = document.createElement('video');
                let vidObjSrc = document.createElement('source');
                vidObjSrc.src = this.url;
                vidObjSrc.type = 'video/mp4';
                vidObj.appendChild(vidObjSrc);
                if (isFull === false) {
                    vidObj.controls = false;
                    vidObj.muted = true;
                    // vidObj.autoplay = true;
                    // vidObj.loop = true;
                    vidObj.preload = "metadata";
                } else {
                    vidObj.controls = true;
                    vidObj.muted = false;
                    vidObj.autoplay = false;
                    vidObj.loop = false;
                }
                // vidObj.setAttribute("width", "auto");
                // vidObj.setAttribute("height", "100%");
                container.appendChild(vidObj);

                // add video icon overlay
                let vidIcon = document.createElement('div');
                vidIcon.classList.add('card-screenshot-icon');
                vidIcon.classList.add('card-screenshot-video-icon');
                container.appendChild(vidIcon);
                break;

            default:
                let placeholder = document.createElement('div');
                placeholder.classList.add('screenshot-placeholder');
                placeholder.innerText = 'No Preview Available';
                container.appendChild(placeholder);
                break;
        }

        return container;
    }

    /*
    extractYouTubeId: Extracts the YouTube video ID from a given URL.
    url: The YouTube URL.
    Returns the extracted video ID or null if not found.
    */
    extractYouTubeId(url) {
        let regex = /(?:https?:\/\/)?(?:www\.)?youtu(?:be\.com\/watch\?v=|\.be\/)([^\s&]+)/;
        let match = url.match(regex);
        return match ? match[1] : null;
    }
}

class ScreenshotViewer {
    /*
    screenshots: An array of ScreenshotItem objects to be displayed.
    currentIndex: The index of the currently displayed screenshot in the screenshots array.
    startIndex: The index to start viewing from (default is 0).
    totalImageCount: The total number of images available (for pagination).
    loadMoreCallback: A callback function to load more screenshots when needed.
    closeCallback: A callback function to be called when the viewer is closed.
    deleteCallback: A callback function to handle deletions (if applicable).
    */
    constructor(screenshots, startIndex = 0, totalImageCount, loadMoreCallback, closeCallback, deleteCallback) {
        this.screenshots = screenshots;
        this.currentIndex = 0;
        this.startIndex = startIndex;
        this.totalImageCount = totalImageCount;
        this.loadMoreCallback = loadMoreCallback;
        this.closeCallback = closeCallback;
        this.deleteCallback = deleteCallback;
        this.currentPage = 1;

        this.#initViewer();
    }

    /*
    Keeps track of pages already loaded to avoid duplicate loads
    */
    #loadedPages = [];

    /*
    #initViewer: Initializes the screenshot viewer by creating necessary DOM elements and setting up event listeners.
    */
    #initViewer() {
        // create the background
        this.modalBackground = document.createElement("div");
        this.modalBackground.classList.add("modal-background");
        this.modalBackground.addEventListener("click", (event) => {
            if (event.target === this.modalBackground) {
                this.Close();
            }
        });
        this.modalBackground.style.display = "none";

        // keyboard navigation handler (bound once)
        this._handleKeyDown = (e) => {
            // Only act if viewer is visible
            if (this.modalBackground.style.display !== 'block') return;
            // Ignore if focused element is an input/control the user might be typing in
            const tag = (e.target && e.target.tagName) ? e.target.tagName.toUpperCase() : '';
            if (["INPUT", "TEXTAREA", "SELECT"].includes(tag) || e.target.isContentEditable) return;
            if (e.key === 'ArrowRight') {
                e.preventDefault();
                this.Next();
            } else if (e.key === 'ArrowLeft') {
                e.preventDefault();
                this.Previous();
            }
        };
        this._keyListenerAttached = false;

        /*
        Visual design is:
        - the screenshot centered over the modal background, with only a shadowed border
        - left and right arrows on the sides to navigate
        - a close button in the top-right corner
        - semi transparent panel at the bottom with title, description, profile info and edit/delete buttons
        - a camera roll of thumbnails at the bottom to jump to a specific screenshot
        - the entire viewer is responsive and works on mobile devices
        */

        this.modalContent = document.createElement("div");
        this.modalContent.classList.add("screenshot-content");
        this.modalBackground.appendChild(this.modalContent);

        // create the close button
        this.closeButton = document.createElement("div");
        this.closeButton.classList.add("card-close-button");
        this.closeButton.innerHTML = "&times;";
        this.closeButton.addEventListener("click", () => this.Close());
        this.modalContent.appendChild(this.closeButton);

        // create delete button if deleteCallback is provided
        if (this.deleteCallback) {
            this.deleteButton = document.createElement("div");
            this.deleteButton.classList.add("screenshot-delete-button");
            this.deleteButton.innerText = "Delete";
            this.deleteButton.addEventListener("click", async () => {
                if (this.screenshots.length === 0) return;
                let screenshotToDelete = this.screenshots[this.currentIndex];
                let confirmed = confirm("Are you sure you want to delete this screenshot?");
                if (confirmed) {
                    let success = await this.deleteCallback(screenshotToDelete);
                    if (success) {
                        this.screenshots.splice(this.currentIndex, 1);
                        // Adjust currentIndex if needed
                        if (this.currentIndex >= this.screenshots.length) {
                            this.currentIndex = Math.max(0, this.screenshots.length - 1);
                        }
                        this.createCameraRoll();
                        if (this.screenshots.length > 0) {
                            this.GoTo(this.currentIndex);
                        } else {
                            this.Close();
                        }
                    } else {
                        alert("Failed to delete the screenshot.");
                    }
                }
            });
            this.modalContent.appendChild(this.deleteButton);
        }

        // create the left arrow
        this.leftArrow = document.createElement("div");
        this.leftArrow.classList.add("screenshot-left-arrow");
        this.leftArrow.innerHTML = "&#10094;";
        this.leftArrow.addEventListener("click", () => this.Previous());
        this.modalContent.appendChild(this.leftArrow);

        // create the right arrow
        this.rightArrow = document.createElement("div");
        this.rightArrow.classList.add("screenshot-right-arrow");
        this.rightArrow.innerHTML = "&#10095;";
        this.rightArrow.addEventListener("click", () => this.Next());
        this.modalContent.appendChild(this.rightArrow);

        // create the screenshot container
        this.screenshotContainer = document.createElement("div");
        this.screenshotContainer.classList.add("screenshot-screenshot-container");
        this.modalContent.appendChild(this.screenshotContainer);

        // create the info panel
        this.infoPanel = document.createElement("div");
        this.infoPanel.classList.add("screenshot-info-panel");
        this.modalContent.appendChild(this.infoPanel);

        // date time
        this.dateTimeElement = document.createElement("span");
        this.dateTimeElement.classList.add("screenshot-info-datetime");
        this.infoPanel.appendChild(this.dateTimeElement);

        // profile info
        this.profileElement = document.createElement("div");
        this.profileElement.classList.add("screenshot-info-profile");
        this.infoPanel.appendChild(this.profileElement);

        // title
        this.titleElement = document.createElement("h2");
        this.titleElement.classList.add("screenshot-info-title");
        this.infoPanel.appendChild(this.titleElement);

        // description
        this.descriptionElement = document.createElement("span");
        this.descriptionElement.classList.add("screenshot-info-description");
        this.infoPanel.appendChild(this.descriptionElement);

        // camera roll container
        this.cameraRollContainer = document.createElement("div");
        this.cameraRollContainer.classList.add("screenshot-camera-roll-container");
        this.modalContent.appendChild(this.cameraRollContainer);

        // Append the modal element to the document body
        document.body.appendChild(this.modalBackground);

        // create the camera roll thumbnails
        this.createCameraRoll();

        // show the starting screenshot
        this.GoTo(this.startIndex);
    }

    /*
    createCameraRoll: Creates the camera roll thumbnails for all screenshots.
    */
    createCameraRoll() {
        this.cameraRollContainer.innerHTML = ""; // clear existing thumbnails

        this.screenshots.forEach((screenshot, index) => {
            let thumb = screenshot.createPreviewElement();
            thumb.classList.add("screenshot-camera-roll-thumbnail");
            thumb.addEventListener("click", () => this.GoTo(index));
            this.cameraRollContainer.appendChild(thumb);
        });
    }

    /*
    Open: Opens the screenshot viewer modal.
    */
    Open() {
        this.modalBackground.style.display = "block";
        // Attach key listener when opened
        if (!this._keyListenerAttached) {
            document.addEventListener('keydown', this._handleKeyDown);
            this._keyListenerAttached = true;
        }
    }

    /*
    Close: Closes the screenshot viewer modal.
    */
    Close() {
        this.modalBackground.style.display = "none";
        // Detach key listener when closed
        if (this._keyListenerAttached) {
            document.removeEventListener('keydown', this._handleKeyDown);
            this._keyListenerAttached = false;
        }

        this.screenshots = [];
        this.currentIndex = 0;
        this.startIndex = 0;
        this.currentPage = 1;
        this.#loadedPages = [];
        this.cameraRollContainer.innerHTML = "";
        this.screenshotContainer.innerHTML = "";
        this.titleElement.innerText = "";
        this.dateTimeElement.innerText = "";
        this.descriptionElement.innerText = "";
        this.profileElement.innerText = "";

        if (this.closeCallback) {
            this.closeCallback();
        }

        if (this.modalBackground) {
            this.modalBackground.remove();
            this.modalBackground = null;
        }
    }

    /*
    Next: Navigates to the next screenshot in the array.
    */
    Next() {
        let newIndex = (this.currentIndex + 1) % this.screenshots.length;
        this.GoTo(newIndex);
    }

    /*
    Previous: Navigates to the previous screenshot in the array.
    */
    Previous() {
        let newIndex = (this.currentIndex - 1 + this.screenshots.length) % this.screenshots.length;
        this.GoTo(newIndex);
    }

    /*
    GoTo: Navigates to a specific screenshot by index.
    index: The index of the screenshot to display.
    */
    async GoTo(index) {
        if (index === undefined) {
            index = this.startIndex;
        }

        if (index < 0 || index >= this.screenshots.length) {
            console.error("Index out of bounds in ScreenshotViewer. GoTo: ", index, this.screenshots);
            return;
        }

        this.currentIndex = index;
        let screenshot = this.screenshots[index];

        // If we've reached the end of the current screenshots, and there's a load more callback, invoke it
        if (this.totalImageCount && this.screenshots.length < this.totalImageCount) {
            if (index === this.screenshots.length - 1 && this.loadMoreCallback) {
                // Avoid loading the same page multiple times
                if (this.#loadedPages.includes(this.currentPage + 1)) {
                    return;
                }

                this.currentPage += 1;
                this.#loadedPages.push(this.currentPage);
                await this.loadMoreCallback(this.currentPage);

                // Rebuild the camera roll with the new screenshots
                this.createCameraRoll();
            }
        }

        // clear existing content
        this.screenshotContainer.innerHTML = "";

        // create and append the image element
        let previewElement = screenshot.createFullElement();
        for (let child of Array.from(previewElement.children)) {
            this.screenshotContainer.appendChild(child);
        }

        // update info panel
        if (screenshot.title) {
            this.titleElement.style.display = "block";
            this.titleElement.innerText = screenshot.title || "Untitled";
        } else {
            this.titleElement.style.display = "none";
        }
        if (screenshot.uploadDate) {
            this.dateTimeElement.style.display = "block";
            this.dateTimeElement.innerText = screenshot.uploadDate ? `${new Date(screenshot.uploadDate).toLocaleString()}` : "Upload date unknown";
        } else {
            this.dateTimeElement.style.display = "none";
        }
        if (screenshot.description) {
            this.descriptionElement.style.display = "block";
            this.descriptionElement.innerText = screenshot.description || "No description available.";
        } else {
            this.descriptionElement.style.display = "none";
        }
        if (screenshot.profile) {
            this.profileElement.style.display = "block";
            this.profileElement.innerText = '';
            let userAvatar = new Avatar(screenshot.profile.userId, 32, 32, true, true);
            userAvatar.classList.add("user_list_icon");
            this.profileElement.appendChild(userAvatar);
        } else {
            this.profileElement.style.display = "none";
        }

        // highlight the current thumbnail in the camera roll
        Array.from(this.cameraRollContainer.children).forEach((thumb, idx) => {
            if (idx === index) {
                thumb.classList.add("screenshot-camera-roll-thumbnail-active");
                thumb.classList.remove("screenshot-camera-roll-thumbnail-inactive");
                thumb.scrollIntoView({ behavior: 'smooth', inline: 'center' });
            } else {
                thumb.classList.remove("screenshot-camera-roll-thumbnail-active");
                thumb.classList.add("screenshot-camera-roll-thumbnail-inactive");
            }
        });

        // open the viewer if not already open
        this.Open();
    }
}