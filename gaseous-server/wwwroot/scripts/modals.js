class Modal {
    constructor(contentUrl, buttons) {
        this.contentUrl = contentUrl;
        this.buttons = buttons;
        this.modalBackground = null;
        this.buttons = [];
    }

    async BuildModal() {
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
        if (tabs.length > 0) {
            let firstTab = true;
            tabs.forEach((tab) => {
                let newTab = document.createElement('div');
                newTab.id = 'tab-' + tab.id;
                newTab.classList.add('modal-tab-button');
                newTab.setAttribute('data-tabid', tab.id);
                newTab.innerHTML = tab.getAttribute('data-tabname');
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
                    firstTab = false;
                } else {
                    tab.style.display = 'none';
                }
                tabcontainer.appendChild(newTab);
            });
        } else {
            tabcontainer.style.display = 'none';
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

        // Add event listener to close the modal when the escape key is pressed
        document.addEventListener('keydown', (event) => {
            if (event.key === 'Escape') {
                this.close();
            }
        });

        return;
    }

    async open() {
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

        return;
    }

    close() {
        // Hide the modal
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
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "Save State Manager";

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

        await this.#LoadStates();

        // show the dialog
        this.dialog.open();
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
                    console.log(thisObject);
                    for (let i = 0; i < result.length; i++) {
                        let stateBox = document.createElement('div');
                        stateBox.id = 'stateBox_' + result[i].id;
                        stateBox.className = 'saved_state_box romrow';

                        // screenshot panel
                        let stateImageBox = document.createElement('div');
                        stateImageBox.id = 'stateImageBox_' + result[i].id;
                        stateImageBox.className = 'saved_state_image_box';

                        // screenshot image
                        let stateImage = null;
                        if (result[i].hasScreenshot == true) {
                            stateImage = document.createElement('img');
                            stateImage.className = 'saved_state_image_image';
                            stateImage.src = '/api/v1.1/StateManager/' + thisObject.RomId + '/' + result[i].id + '/Screenshot/image.png?IsMediaGroup=' + thisObject.IsMediaGroup;
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
                        stateMainPanel.id = 'stateMainPanel_' + result[i].id;
                        stateMainPanel.className = 'saved_state_main_box';

                        let stateName = document.createElement('input');
                        stateName.id = 'stateName_' + result[i].id;
                        stateName.type = 'text';
                        stateName.className = 'saved_state_name';
                        stateName.addEventListener('change', async () => {
                            thisObject.#UpdateStateSave(result[i].id, thisObject.IsMediaGroup);
                        });
                        if (result[i].name) {
                            stateName.value = result[i].name;
                        } else {
                            stateName.setAttribute('placeholder', "Untitled");
                        }
                        stateMainPanel.appendChild(stateName);

                        let stateTime = document.createElement('div');
                        stateTime.id = 'stateTime_' + result[i].id;
                        stateTime.className = 'saved_state_date';
                        stateTime.innerHTML = moment(result[i].saveTime).format("YYYY-MM-DD h:mm:ss a");
                        stateMainPanel.appendChild(stateTime);

                        let stateControls = document.createElement('div');
                        stateControls.id = 'stateControls_' + result[i].id;
                        stateControls.className = 'saved_state_controls';

                        let stateControlsLaunch = document.createElement('span');
                        stateControlsLaunch.id = 'stateControlsLaunch_' + result[i].id;
                        stateControlsLaunch.classList.add('platform_edit_button');
                        stateControlsLaunch.classList.add('platform_item_green');
                        // stateControlsLaunch.classList.add('romstart');
                        let emulatorTarget;
                        let mediagroupint = 0;
                        if (thisObject.IsMediaGroup == true) {
                            mediagroupint = 1;
                        }
                        switch (getQueryString('page', 'string')) {
                            case 'emulator':
                                emulatorTarget = await BuildLaunchLink(getQueryString('engine', 'string'), getQueryString('core', 'string'), getQueryString('platformid', 'string'), getQueryString('gameid', 'string'), getQueryString('romid', 'string'), mediagroupint, thisObject.rompath, result[i].id) + '&stateid=' + result[i].id;
                                stateControlsLaunch.addEventListener('click', () => {
                                    window.location.replace(emulatorTarget);
                                });
                                break;
                            default:
                                emulatorTarget = await BuildLaunchLink(thisObject.engine, thisObject.core, thisObject.platformid, thisObject.gameid, thisObject.RomId, mediagroupint, thisObject.rompath, result[i].id) + '&stateid=' + result[i].id;
                                stateControlsLaunch.addEventListener('click', () => {
                                    window.location.href = emulatorTarget;
                                });
                                break;
                        }

                        stateControlsLaunch.innerHTML = '<img src="/images/play.svg" class="banner_button_image" alt="Play" title="Play" />';
                        stateControlsLaunch.style.float = 'right';
                        stateControls.appendChild(stateControlsLaunch);

                        let stateControlsDownload = document.createElement('a');
                        stateControlsDownload.id = 'stateControlsDownload_' + result[i].id;
                        stateControlsDownload.classList.add('platform_edit_button');
                        stateControlsDownload.classList.add('saved_state_buttonlink');
                        stateControlsDownload.href = '/api/v1.1/StateManager/' + thisObject.RomId + '/' + result[i].id + '/State/savestate.state?IsMediaGroup=' + thisObject.IsMediaGroup;
                        stateControlsDownload.innerHTML = '<img src="/images/download.svg" class="banner_button_image" alt="Download" title="Download" />';
                        stateControls.appendChild(stateControlsDownload);

                        let stateControlsDelete = document.createElement('span');
                        stateControlsDelete.id = 'stateControlsDelete_' + result[i].id;
                        stateControlsDelete.classList.add('platform_edit_button');
                        stateControlsDelete.classList.add('saved_state_buttonlink');
                        stateControlsDelete.addEventListener('click', async () => {
                            await thisObject.#DeleteStateSave(result[i].id, thisObject.IsMediaGroup);
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