class Modal {
    constructor(contentUrl, buttons) {
        this.contentUrl = contentUrl;
        this.buttons = buttons;
        this.modalBackground = null;
        this.buttons = [];
    }

    async BuildModal(closeIsHide = false) {
        this.closeIsHide = closeIsHide;

        // Create the modal background
        this.modalBackground = document.createElement('div');
        this.modalBackground.classList.add('modal-background');
        this.modalBackground.style.display = 'none';

        // Create the modal element
        this.modalElement = document.createElement('div');
        this.modalElement.classList.add('modal-window');

        // Load the modal template
        const templateResponse = await fetch('/pages/modals/modal.html');
        const templateContent = await templateResponse.text();

        // Load the content from the HTML file
        const response = await fetch("/pages/modals/" + this.contentUrl + ".html");
        const content = await response.text();

        // Set the content of the modal
        this.modalElement.innerHTML = templateContent;
        this.modalElement.querySelector('#modal-window-content').innerHTML = content;

        // Generate tabs
        const tabcontainer = this.modalElement.querySelector('#modal-tabs');
        const tabs = this.modalElement.querySelectorAll('[name="modalTab"]');
        const popup = this.modalElement.querySelector('#modal-popup');
        if (tabs.length > 0) {
            let firstTab = true;
            tabs.forEach((tab) => {
                let newTab = document.createElement('div');
                newTab.id = 'tab-' + tab.id;
                newTab.classList.add('modal-tab-button');
                newTab.setAttribute('data-tabid', tab.id);
                newTab.textContent = tab.getAttribute('data-tabname');
                newTab.addEventListener('click', () => {
                    tabs.forEach((tab) => {
                        if (tab.getAttribute('id') !== newTab.getAttribute('data-tabid')) {
                            tab.style.display = 'none';
                            tabcontainer.querySelector('[data-tabid="' + tab.id + '"]').classList.remove('model-tab-button-selected');
                        } else {
                            tab.style.display = 'block';
                            tabcontainer.querySelector('[data-tabid="' + tab.id + '"]').classList.add('model-tab-button-selected');
                        }
                    });
                });
                if (firstTab) {
                    newTab.classList.add('model-tab-button-selected');
                    tab.style.display = 'block';
                } else {
                    tab.style.display = 'none';
                }

                let newPopupOption = document.createElement('option');
                newPopupOption.value = tab.id;
                newPopupOption.textContent = tab.getAttribute('data-tabname');
                popup.appendChild(newPopupOption);

                if (firstTab) {
                    newPopupOption.selected = true;
                }

                firstTab = false;
                tabcontainer.appendChild(newTab);
            });
        } else {
            tabcontainer.style.display = 'none';
            popup.style.display = 'none';
        }

        // add the window to the modal background
        this.modalBackground.appendChild(this.modalElement);

        // Append the modal element to the document body
        document.body.appendChild(this.modalBackground);

        // Add event listener to close the modal when the close button is clicked
        this.modalElement.querySelector('#modal-close-button').addEventListener('click', () => {
            this.close();
        });

        // Add event listener to close the modal when clicked outside
        this.modalBackground.addEventListener('click', (event) => {
            if (event.target === this.modalBackground) {
                this.close();
            }
        });
    }

    #exists = false;

    async open() {
        if (this.#exists) {
            // already exists, just open it
            $(this.modalBackground).fadeIn(200);
            return;
        }
        this.#exists = true;

        // hide the scroll bar for the page
        document.body.style.overflow = 'hidden';

        // buttons
        const buttonContainer = this.modalElement.querySelector('#modal-footer');
        if (this.buttons.length > 0) {
            this.buttons.forEach((button) => {
                buttonContainer.appendChild(button.render());
            });
        } else {
            const closeButton = document.createElement('button');
            closeButton.classList.add('modal-button');
            closeButton.classList.add('bluebutton');
            closeButton.innerHTML = 'OK';
            closeButton.addEventListener('click', () => {
                this.close();
            });
            buttonContainer.appendChild(closeButton);
        }

        // show the modal
        $(this.modalBackground).fadeIn(200);
        // this.modalBackground.style.display = 'block';

        // make the popup a select2 element
        const tabcontainer = this.modalElement.querySelector('#modal-tabs');
        const tabs = this.modalElement.querySelectorAll('[name="modalTab"]');
        const popup = this.modalElement.querySelector('#modal-popup');
        // if popup has children, then apply select2
        if (popup.children.length > 0) {
            $(popup).select2();
            // add a change event to the popup
            $(popup).on('select2:select', (e) => {
                const popupValue = e.target.value;
                tabs.forEach((tab) => {
                    if (tab.getAttribute('id') !== popupValue) {
                        tab.style.display = 'none';
                        tabcontainer.querySelector('[data-tabid="' + tab.id + '"]').classList.remove('model-tab-button-selected');
                    } else {
                        tab.style.display = 'block';
                        tabcontainer.querySelector('[data-tabid="' + tab.id + '"]').classList.add('model-tab-button-selected');
                    }
                });
            });
        }

        return;
    }

    close() {
        // Hide the modal
        $(this.modalBackground).fadeOut(200, () => {
            // Show the scroll bar for the page
            if (document.getElementsByClassName('modal-background').length === 1) {
                document.body.style.overflow = 'auto';
            }

            // Remove the modal element from the document body
            if (this.closeIsHide === false) {
                if (this.modalBackground) {
                    this.modalBackground.remove();
                    this.modalBackground = null;
                }
            }
        });
    }

    addButton(button) {
        this.buttons.push(button);
    }

    disableButtons() {
        this.buttons.forEach((button) => {
            button.button.disabled = true;
        });
    }

    enableButtons() {
        this.buttons.forEach((button) => {
            button.button.disabled = false;
        });
    }

    disableButton(buttonId) {
        this.buttons.forEach((button) => {
            if (button.text === buttonId) {
                button.button.disabled = true;
            }
        });
    }

    enableButton(buttonId) {
        this.buttons.forEach((button) => {
            if (button.text === buttonId) {
                button.button.disabled = false;
            }
        });
    }

    removeTab(tabId) {
        const tab = this.modalElement.querySelector('#tab-' + tabId);
        if (tab) {
            tab.style.display = 'none';
        }
    }
}

// type: 0 or null = normal, 1 = blue, 2 = red
class ModalButton {
    constructor(text, type, callingObject, callback) {
        this.text = text;
        this.type = type;
        this.callingObject = callingObject;
        this.callback = callback;

        return;
    }

    button = null;

    render() {
        this.button = document.createElement('button');
        this.button.id = this.text;
        this.button.classList.add('modal-button');
        if (this.type) {
            switch (this.type) {
                case 1:
                    this.button.classList.add('bluebutton');
                    break;
                case 2:
                    this.button.classList.add('redbutton');
                    break;
            }
        }
        this.button.innerHTML = this.text;
        let callback = this.callback;
        let callingObject = this.callingObject;
        this.button.addEventListener('click', function () {
            callback(callingObject);
        });
        return this.button;
    }
}

class MessageBox {
    constructor(title, message) {
        this.title = title;
        this.message = message;
        this.buttons = [];

        return;
    }

    async open() {
        // create the dialog
        this.msgDialog = await new Modal('messagebox');
        await this.msgDialog.BuildModal();

        // override the dialog size
        this.msgDialog.modalElement.style = 'width: 400px; height: unset; min-width: unset; min-height: 200px; max-width: unset; max-height: unset;';

        // set the title
        this.msgDialog.modalElement.querySelector('#modal-header-text').innerHTML = this.title;

        // set the message
        this.msgDialog.modalElement.querySelector('#messageText').innerHTML = this.message;

        // add buttons
        if (this.buttons) {
            for (let i = 0; i < this.buttons.length; i++) {
                this.msgDialog.addButton(this.buttons[i]);
            }
        }

        await this.msgDialog.open();
    }

    addButton(button) {
        this.buttons.push(button);
    }
}

class FileOpen {
    constructor(okCallback, cancelCallback, ShowFiles = false) {
        this.okCallback = okCallback;
        this.cancelCallback = cancelCallback;
        if (ShowFiles === null || ShowFiles === undefined) {
            this.ShowFiles = false;
        } else {
            this.ShowFiles = ShowFiles;
        }
        this.SelectedPath = '/';
    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("filepicker");
        await this.dialog.BuildModal();

        // override the dialog size
        this.dialog.modalElement.style = 'width: 600px; height: 350px; min-width: unset; min-height: unset; max-width: unset; max-height: unset;';

        // setup the dialog
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "Select Path";
        this.dialog.modalElement.querySelector('#modal-body').setAttribute('style', 'overflow-x: auto; overflow-y: hidden; padding: 0px;');

        // load the first path
        this.filePickerBox = this.dialog.modalElement.querySelector('#fileSelector');
        let fileOpenItem = new FileOpenFolderItem(this, "/", this.ShowFiles);
        await fileOpenItem.open();
        this.filePickerBox.append(fileOpenItem.Item);

        // setup the path text display
        this.pathBox = this.dialog.modalElement.querySelector('#selectedPath');

        // add ok button
        let okButton = new ModalButton("OK", 1, this, async function (callingObject) {
            if (callingObject.okCallback) {
                callingObject.okCallback();
            }
            callingObject.dialog.close();
        });
        this.dialog.addButton(okButton);

        // add cancel button
        let cancelButton = new ModalButton("Cancel", 0, this, async function (callingObject) {
            if (callingObject.cancelButton) {
                callingObject.cancelCallback();
            }
            callingObject.dialog.close();
        });
        this.dialog.addButton(cancelButton);

        // show the dialog
        this.dialog.open();
    }

    async close() {
        this.dialog.close();
    }
}

class FileOpenFolderItem {
    constructor(ParentObject, Path, ShowFiles) {
        this.ParentObject = ParentObject;
        this.Path = Path;
        this.ShowFiles = ShowFiles;
        this.Item = null;
    }

    async open() {
        const response = await fetch('/api/v1.1/FileSystem?path=' + encodeURIComponent(this.Path) + '&showFiles=' + this.ShowFiles).then(async response => {
            if (!response.ok) {
                // handle the error
                console.error("Error fetching profile");
            } else {
                const pathList = await response.json();

                // create the item
                let item = document.createElement('li');
                item.classList.add('filepicker-item');

                // set the item
                this.Item = item;

                // add the paths to the item
                pathList['directories'].forEach((path) => {
                    let pathItem = document.createElement('div');
                    pathItem.classList.add('filepicker-path');
                    pathItem.innerHTML = path.name;
                    pathItem.addEventListener('click', async () => {
                        this.Item.querySelectorAll('.filepicker-path').forEach((path) => {
                            path.classList.remove('filepicker-path-selected');
                        });
                        pathItem.classList.add('filepicker-path-selected');
                        let fileOpenItem = new FileOpenFolderItem(this.ParentObject, path.path, this.ShowFiles);
                        await fileOpenItem.open();

                        // remove all items after this one
                        while (this.ParentObject.filePickerBox.lastChild !== this.Item) {
                            this.ParentObject.filePickerBox.removeChild(this.ParentObject.filePickerBox.lastChild);
                        }

                        this.ParentObject.filePickerBox.append(fileOpenItem.Item);
                        fileOpenItem.Item.scrollIntoView();

                        this.ParentObject.pathBox.innerHTML = path.path;
                        this.ParentObject.SelectedPath = path.path;
                    });
                    item.appendChild(pathItem);
                });
            }
        });
    }
}

class EmulatorStateManager {
    constructor(RomId, IsMediaGroup, engine, core, platformid, gameid, rompath) {
        this.RomId = RomId;
        this.IsMediaGroup = IsMediaGroup;
        this.engine = engine;
        this.core = core;
        this.platformid = platformid;
        this.gameid = gameid;
        this.rompath = rompath;
    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("emulatorstate");
        await this.dialog.BuildModal();

        // setup the dialog
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "Save Manager";

        this.statesBox = this.dialog.modalElement.querySelector('#saved_states');

        this.FileUpload = this.dialog.modalElement.querySelector('#stateFile');
        this.FileUpload.addEventListener('change', () => {
            let file = this.FileUpload.files[0];
            let formData = new FormData();
            formData.append('file', file);

            console.log("Uploading state file");

            let thisObject = this;
            fetch('/api/v1.1/StateManager/Upload?RomId=' + this.RomId + '&IsMediaGroup=' + this.IsMediaGroup, {
                method: 'POST',
                body: formData
            })
                .then(response => response.json())
                .then(async data => {
                    console.log('Success:', data);
                    thisObject.#UploadAlert(data);
                    await thisObject.#LoadStates();
                })
                .catch(async error => {
                    console.error("Error:", error);
                    thisObject.#UploadAlert(error);
                    await thisObject.#LoadStates();
                });
        });

        // add the buttons
        let closeButton = new ModalButton("Close", 0, this, async function (callingObject) {
            callingObject.dialog.close();
        });
        this.dialog.addButton(closeButton);

        await this.#LoadSRM();

        await this.#LoadStates();

        // show the dialog
        this.dialog.open();
    }

    async #LoadSRM() {
        // load the srm
        let thisObject = this;
        let url = `/api/v1.1/SaveFile/${thisObject.core}/${thisObject.IsMediaGroup}/${thisObject.RomId}`;
        await fetch(url).then(async response => {
            if (!response.ok) {
                this.dialog.modalElement.querySelector('#loadFile').style.display = 'none';
            } else {
                let result = await response.json();
                if (result.length === 0) {
                    this.dialog.modalElement.querySelector('#loadFile').style.display = 'none';
                } else {
                    let srmSelect = this.dialog.modalElement.querySelector('#srmFileSelect');
                    let srmStart = this.dialog.modalElement.querySelector('#srmStart');
                    let srmPurge = this.dialog.modalElement.querySelector('#srmPurge');

                    srmSelect.innerHTML = '';

                    // create latest option
                    let latestOption = document.createElement('option');
                    latestOption.value = 'latest';
                    latestOption.innerHTML = 'Latest';
                    srmSelect.appendChild(latestOption);

                    // create all the options
                    result.forEach((srm) => {
                        let option = document.createElement('option');
                        option.value = srm.id;
                        option.innerHTML = moment(srm.saveTime).format("YYYY-MM-DD h:mm:ss a");
                        srmSelect.appendChild(option);
                    });

                    // make the select2
                    $(srmSelect).select2({
                        minimumResultsForSearch: Infinity
                    });

                    // add the click event to the start button
                    let selection = srmSelect.value;

                    let emulatorTarget;
                    let mediagroupint = 0;
                    if (thisObject.IsMediaGroup == true) {
                        mediagroupint = 1;
                    }
                    switch (getQueryString('page', 'string')) {
                        case 'emulator':
                            emulatorTarget = await BuildLaunchLink(getQueryString('engine', 'string'), getQueryString('core', 'string'), getQueryString('platformid', 'string'), getQueryString('gameid', 'string'), getQueryString('romid', 'string'), mediagroupint, thisObject.rompath, selection);
                            srmStart.addEventListener('click', () => {
                                window.location.replace(emulatorTarget);
                            });
                            break;
                        default:
                            emulatorTarget = await BuildLaunchLink(thisObject.engine, thisObject.core, thisObject.platformid, thisObject.gameid, thisObject.RomId, mediagroupint, thisObject.rompath, selection);
                            srmStart.addEventListener('click', () => {
                                window.location.href = emulatorTarget;
                            });
                            break;
                    }

                    // add the click event to the purge button
                    srmPurge.addEventListener('click', async () => {
                        await fetch('/api/v1.1/SaveFile/' + thisObject.core + '/' + thisObject.IsMediaGroup + '/' + thisObject.RomId, {
                            method: 'DELETE'
                        }).then(async response => {
                            if (!response.ok) {
                                console.error("Error deleting srm");
                            } else {
                                this.dialog.modalElement.querySelector('#loadFile').style.display = 'none';
                            }
                        });
                    });
                }
            }
        });
    }

    async #LoadStates() {
        // load the states
        let thisObject = this;
        thisObject.statesBox.innerHTML = '';
        let statesUrl = '/api/v1.1/StateManager/' + thisObject.RomId + '?IsMediaGroup=' + thisObject.IsMediaGroup;
        await fetch(statesUrl).then(async response => {
            if (!response.ok) {
                thisObject.statesBox.innerHTML = 'No saved states found.';
            } else {
                let result = await response.json();

                thisObject.dialog.modalElement.querySelector('#stateFile').value = '';

                if (result.length === 0) {
                    thisObject.statesBox.innerHTML = 'No saved states found.';
                } else {
                    console.log(result);
                    for (let i = 0; i < result.length; i++) {
                        let state = result[i];

                        let stateBox = document.createElement('div');
                        stateBox.id = 'stateBox_' + state.id;
                        stateBox.className = 'saved_state_box romrow';

                        // screenshot panel
                        let stateImageBox = document.createElement('div');
                        stateImageBox.id = 'stateImageBox_' + state.id;
                        stateImageBox.className = 'saved_state_image_box';

                        // screenshot image
                        let stateImage = null;
                        if (state.hasScreenshot == true) {
                            stateImage = document.createElement('img');
                            stateImage.className = 'saved_state_image_image';
                            stateImage.src = '/api/v1.1/StateManager/' + thisObject.RomId + '/' + state.id + '/Screenshot/image.png?IsMediaGroup=' + thisObject.IsMediaGroup;
                        } else {
                            stateImage = document.createElement('div');
                            stateImage.className = 'saved_state_image_image';
                            stateImage.style.height = '100px';
                            stateImage.style.backgroundImage = 'url(/images/unknowngame.png)';
                            stateImage.style.backgroundSize = 'cover';
                            stateImage.style.backgroundRepeat = 'no-repeat';
                            stateImage.style.backgroundPosition = 'center center';
                        }
                        stateImageBox.appendChild(stateImage);
                        stateBox.appendChild(stateImageBox);

                        // main panel
                        let stateMainPanel = document.createElement('div');
                        stateMainPanel.id = 'stateMainPanel_' + state.id;
                        stateMainPanel.className = 'saved_state_main_box';

                        let stateName = document.createElement('input');
                        stateName.id = 'stateName_' + state.id;
                        stateName.type = 'text';
                        stateName.className = 'saved_state_name';
                        stateName.addEventListener('change', async () => {
                            thisObject.#UpdateStateSave(state.id, thisObject.IsMediaGroup);
                        });
                        if (state.name) {
                            stateName.value = state.name;
                        } else {
                            stateName.setAttribute('placeholder', "Untitled");
                        }
                        stateMainPanel.appendChild(stateName);

                        let stateTime = document.createElement('div');
                        stateTime.id = 'stateTime_' + state.id;
                        stateTime.className = 'saved_state_date';
                        stateTime.innerHTML = moment(state.saveTime).format("YYYY-MM-DD h:mm:ss a");
                        stateMainPanel.appendChild(stateTime);

                        let stateControls = document.createElement('div');
                        stateControls.id = 'stateControls_' + state.id;
                        stateControls.className = 'saved_state_controls';

                        let stateControlsLaunch = document.createElement('span');
                        stateControlsLaunch.id = 'stateControlsLaunch_' + state.id;
                        stateControlsLaunch.classList.add('platform_edit_button');
                        stateControlsLaunch.classList.add('platform_item_green');
                        let emulatorTarget;
                        let mediagroupint = 0;
                        if (thisObject.IsMediaGroup == true) {
                            mediagroupint = 1;
                        }
                        switch (getQueryString('page', 'string')) {
                            case 'emulator':
                                emulatorTarget = await BuildLaunchLink(getQueryString('engine', 'string'), getQueryString('core', 'string'), getQueryString('platformid', 'string'), getQueryString('gameid', 'string'), getQueryString('romid', 'string'), mediagroupint, thisObject.rompath) + '&stateid=' + state.id;
                                stateControlsLaunch.addEventListener('click', () => {
                                    window.location.replace(emulatorTarget);
                                });
                                break;
                            default:
                                emulatorTarget = await BuildLaunchLink(thisObject.engine, thisObject.core, thisObject.platformid, thisObject.gameid, thisObject.RomId, mediagroupint, thisObject.rompath) + '&stateid=' + state.id;
                                stateControlsLaunch.addEventListener('click', () => {
                                    window.location.href = emulatorTarget;
                                });
                                break;
                        }

                        stateControlsLaunch.innerHTML = '<img src="/images/play.svg" class="banner_button_image" alt="Play" title="Play" />';
                        stateControlsLaunch.style.float = 'right';
                        stateControls.appendChild(stateControlsLaunch);

                        let stateControlsDownload = document.createElement('a');
                        stateControlsDownload.id = 'stateControlsDownload_' + state.id;
                        stateControlsDownload.classList.add('platform_edit_button');
                        stateControlsDownload.classList.add('saved_state_buttonlink');
                        stateControlsDownload.href = '/api/v1.1/StateManager/' + thisObject.RomId + '/' + state.id + '/State/savestate.state?IsMediaGroup=' + thisObject.IsMediaGroup;
                        stateControlsDownload.innerHTML = '<img src="/images/download.svg" class="banner_button_image" alt="Download" title="Download" />';
                        stateControls.appendChild(stateControlsDownload);

                        let stateControlsDelete = document.createElement('span');
                        stateControlsDelete.id = 'stateControlsDelete_' + state.id;
                        stateControlsDelete.classList.add('platform_edit_button');
                        stateControlsDelete.classList.add('saved_state_buttonlink');
                        stateControlsDelete.addEventListener('click', async () => {
                            await thisObject.#DeleteStateSave(state.id, thisObject.IsMediaGroup);
                        });
                        stateControlsDelete.innerHTML = '<img src="/images/delete.svg" class="banner_button_image" alt="Delete" title="Delete" />';
                        stateControls.appendChild(stateControlsDelete);

                        stateMainPanel.appendChild(stateControls);

                        stateBox.appendChild(stateMainPanel);

                        thisObject.statesBox.appendChild(stateBox);
                    }
                }
            }
        });
    }

    async #DeleteStateSave(StateId, IsMediaGroup) {
        await fetch('/api/v1.1/StateManager/' + this.RomId + '/' + StateId + '?IsMediaGroup=' + IsMediaGroup, {
            method: 'DELETE'
        }).then(async response => {
            if (!response.ok) {
                console.error("Error deleting state");
            } else {
                await this.#LoadStates();
            }
        });
    }

    async #UpdateStateSave(StateId, IsMediaGroup) {
        let stateName = this.dialog.modalElement.querySelector('#stateName_' + StateId);

        let model = {
            "name": stateName.value
        };

        await fetch('/api/v1.1/StateManager/' + this.RomId + '/' + StateId + '?IsMediaGroup=' + IsMediaGroup, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(model)
        }).then(async response => {
            if (!response.ok) {
                console.error("Error updating state");
            } else {
                await this.#LoadStates();
            }
        });
    }

    #UploadAlert(data) {
        if (data.Management == "Managed") {
            alert("State uploaded successfully.");
        } else {
            alert("State uploaded successfully, but it might not function correctly for this platform and ROM.");
        }
    }
}

class ScreenshotDisplay {
    constructor(metadataSource, gameid, selectedImage = null) {
        this.gameid = gameid;
        this.metadataSource = metadataSource;
        this.selectedImage = selectedImage;
    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("screenshots");
        await this.dialog.BuildModal();

        // setup the dialog
        this.dialog.modalElement.classList.add('modal-screenshots');
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "Screenshots";
        this.panel = this.dialog.modalElement.querySelector('#modal-body');
        this.panel.setAttribute('style', 'overflow-x: auto; overflow-y: hidden; padding: 0px; position: relative;');
        this.scroller = this.dialog.modalElement.querySelector('#modal-window-content');
        this.scroller.setAttribute('style', 'position: absolute; top: 0px; left: 0px; bottom: 0px; height: 100%; white-space: nowrap;');

        // load the game data
        let gameUrl = `/api/v1.1/Games/${this.gameid}`;
        await fetch(gameUrl).then(async response => {
            if (!response.ok) {
                this.scroller.innerHTML = '<li class=screenshot-item">No screenshots found.</li>';
            } else {
                let gameData = await response.json();

                if (gameData.videos && gameData.videos.length > 0) {
                    let videoGameUrl = `/api/v1.1/Games/${this.gameid}/${this.metadataSource}/videos`;
                    let videoResponse = await fetch(videoGameUrl);
                    if (videoResponse.ok) {
                        let videoData = await videoResponse.json();
                        if (videoData && videoData.length > 0) {

                            videoData.forEach(video => {
                                let videoItem = document.createElement('div');
                                videoItem.id = 'video_' + video.id;
                                videoItem.classList.add('screenshot-item');
                                videoItem.innerHTML = `
                                    <iframe
                                        height="100%"
                                        width="100%"
                                        src="https://www.youtube.com/embed/${video.video_id}?autoplay=0&mute=0&controls=1"
                                        frameborder="0"
                                        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                                        allowfullscreen
                                    </iframe>`
                                this.scroller.appendChild(videoItem);
                            });
                        }
                    }
                }

                if (gameData.screenshots && gameData.screenshots.length > 0) {
                    gameData.screenshots.forEach((screenshot) => {
                        let screenshotItem = document.createElement('div');
                        screenshotItem.id = 'screenshot_' + screenshot;
                        screenshotItem.classList.add('screenshot-item');
                        screenshotItem.style.backgroundImage = `url(/api/v1.1/Games/${this.gameid}/${this.metadataSource}/screenshots/${screenshot}/image/original/${screenshot}.jpg)`;
                        this.scroller.appendChild(screenshotItem);
                    });
                }
            }
        });

        // show the dialog
        this.dialog.open();

        if (this.selectedImage) {
            let selectedImage = this.scroller.querySelector('#' + this.selectedImage);
            if (selectedImage) {
                console.log("Selected image found: " + this.selectedImage);
                // scroll into view smoothly
                selectedImage.scrollIntoView({
                    behavior: 'smooth',
                    block: 'nearest',
                    inline: 'start'
                });
            }
        }
    }
}

class ContentUploadDialog {
    constructor(metadataId, contentType, closeCallback) {
        this.metadataId = metadataId;
        this.contentType = contentType;
        this.closeCallback = closeCallback;
    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("contentupload");
        await this.dialog.BuildModal();

        // override the dialog size
        this.dialog.modalElement.style = 'width: 350px; height: 150px; min-width: unset; min-height: unset; max-width: unset; max-height: unset;';

        // setup the dialog
        this.dialog.modalElement.classList.add('modal-content-upload');
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "Upload Content";
        this.panel = this.dialog.modalElement.querySelector('#modal-body');
        this.panel.setAttribute('style', 'overflow-x: auto; overflow-y: hidden; padding: 20px; position: relative;');

        // setup the upload box
        this.uploadBox = this.dialog.modalElement.querySelector('#contentUploadBox');
        this.uploadBox.addEventListener('change', async () => {
            let file = this.uploadBox.files[0];
            let formData = new FormData();
            formData.append('file', file);
            formData.append('ContentType', this.contentType);

            console.log("Uploading content file");

            let thisObject = this;
            fetch('/api/v1.1/ContentManager/fileupload/single?metadataid=' + this.metadataId, {
                method: 'POST',
                body: formData
            })
                .then(response => response.json())
                .then(async data => {
                    console.log('Success:', data);
                    alert("Content uploaded successfully.");

                    if (this.closeCallback) {
                        this.closeCallback();
                    }

                    thisObject.dialog.close();
                })
                .catch(async error => {
                    console.error("Error:", error);
                    alert("Error uploading content: " + error);
                });
        });

        // add the buttons
        let closeButton = new ModalButton("Cancel", 0, this, async function (callingObject) {
            callingObject.dialog.close();
        });
        this.dialog.addButton(closeButton);

        // show the dialog
        this.dialog.open();
    }
}