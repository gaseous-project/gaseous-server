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
    description: A brief description of the content item.
    metadataId: The ID of the associated metadata.
    profile: The user profile object associated with the content item.
    */
    constructor(type, url, title, description, metadataId, profile) {
        this.type = type;
        this.url = url;
        this.title = title;
        this.description = description;
        this.metadataId = metadataId;
        this.profile = profile;
    }

    /*
    createPreviewElement: Creates and returns a DOM element representing the preview of the screenshot or media item.
    */
    createPreviewElement() {
        let container = document.createElement('div');
        container.classList.add('card-screenshot-item');

        switch (this.type) {
            case 'screenshot':
                container.style.backgroundImage = `url(${this.url})`;

                // add screenshot icon overlay
                let screenshotIcon = document.createElement('div');
                screenshotIcon.classList.add('card-screenshot-icon');
                screenshotIcon.classList.add('card-screenshot-screenshot-icon');
                container.appendChild(screenshotIcon);
                break;

            case 'photo':
                container.style.backgroundImage = `url(${this.url})`;

                // add photo icon overlay
                let photoIcon = document.createElement('div');
                photoIcon.classList.add('card-screenshot-icon');
                photoIcon.classList.add('card-screenshot-photo-icon');
                container.appendChild(photoIcon);
                break;

            case 'youtube':
                let videoId = this.extractYouTubeId(this.url);
                if (videoId) {
                    container.style.backgroundImage = `url(https://i.ytimg.com/vi/${videoId}/hqdefault.jpg)`;
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
                vidObj.src = this.url;
                vidObj.controls = false;
                vidObj.muted = true;
                vidObj.autoplay = true;
                vidObj.loop = true;
                vidObj.setAttribute("width", "100%");
                vidObj.setAttribute("height", "100%");
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