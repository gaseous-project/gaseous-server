let biosDict = {};

function loadBios() {
    biosDict = {};

    ajaxCall('/api/v1.1/Bios', 'GET', function (result) {
        result.sort((a, b) => a.platformname.charCodeAt(0) - b.platformname.charCodeAt(0));

        // sort into a dictionary
        for (let i = 0; i < result.length; i++) {
            let tempArray = [];
            if (biosDict.hasOwnProperty(result[i].platformname)) {
                tempArray = biosDict[result[i].platformname];
                tempArray.push(result[i]);
            } else {
                tempArray.push(result[i]);
                biosDict[result[i].platformname] = tempArray;
            }

            biosDict[result[i].platformname] = tempArray;
        }

        displayFirmwareList();
    });
}

function displayFirmwareList() {
    let lastPlatform = '';

    let targetDiv = document.getElementById('table_firmware');
    targetDiv.innerHTML = '';

    let totalAvailable = 0;
    let totalCount = 0;

    for (const [key, value] of Object.entries(biosDict)) {
        // new platform - show a header
        let platformRow = document.createElement('div');
        platformRow.classList.add('section')

        let platformHeader = document.createElement('div');
        platformHeader.classList.add('section-header');

        let platformHeaderValue = document.createElement('span');
        platformHeaderValue.innerHTML = key;
        platformHeader.appendChild(platformHeaderValue);

        let platformHeaderEdit = document.createElement('a');
        platformHeaderEdit.href = '#';
        platformHeaderEdit.style.float = 'right';
        platformHeaderEdit.onclick = function () {
            let biosEditor = new BiosEditor(value[0].platformid, loadBios);
            biosEditor.open();
        };
        let platformHeaderEditIcon = document.createElement('img');
        platformHeaderEditIcon.src = '/images/edit.svg';
        platformHeaderEditIcon.classList.add('banner_button_image');
        platformHeaderEdit.appendChild(platformHeaderEditIcon);
        platformHeader.appendChild(platformHeaderEdit);

        let platformHeaderCounter = document.createElement('span');
        platformHeaderCounter.style.float = 'right';
        platformHeaderCounter.style.marginRight = '10px';
        platformHeader.appendChild(platformHeaderCounter);

        platformRow.appendChild(platformHeader);

        let platformBody = document.createElement('div');
        platformBody.classList.add('section-body');

        // create new table
        let newTable = document.createElement('table');
        newTable.classList.add('romtable');
        newTable.setAttribute('cellspacing', 0);

        newTable.appendChild(createTableRow(true, ['Description', 'File name', 'MD5 Hash', 'Available']));

        let totalPlatformAvailable = 0;

        let showAvailable = document.getElementById('firmware_showavailable').checked;
        let showUnavailable = document.getElementById('firmware_showunavailable').checked;

        for (let i = 0; i < value.length; i++) {
            // update counters
            if (value[i].available == true) {
                totalAvailable += 1;
                totalPlatformAvailable += 1;
            }

            if (
                (value[i].available == true && showAvailable == true) ||
                (value[i].available == false && showUnavailable == true)
            ) {
                let biosFilename = document.createElement('a');
                biosFilename.href = '/api/v1.1/Bios/' + value[i].platformid + '/' + value[i].filename;
                biosFilename.innerHTML = value[i].filename;
                biosFilename.className = 'romlink';

                let availableText = document.createElement('span');
                if (value[i].available == true) {
                    availableText.innerHTML = 'Available';
                    availableText.className = 'greentext';

                    biosFilename = document.createElement('a');
                    biosFilename.href = '/api/v1.1/Bios/' + value[i].platformid + '/' + value[i].filename;
                    biosFilename.innerHTML = value[i].filename;
                    biosFilename.className = 'romlink';
                } else {
                    availableText.innerHTML = 'Unavailable';
                    availableText.className = 'redtext';

                    biosFilename = document.createElement('span');
                    biosFilename.innerHTML = value[i].filename;
                }

                let newRow = [
                    value[i].description,
                    biosFilename,
                    value[i].hash,
                    availableText
                ];
                newTable.appendChild(createTableRow(false, newRow, 'romrow', 'romcell bioscell'));
            }
            totalCount += 1;
        }

        platformHeaderCounter.innerHTML = totalPlatformAvailable + ' / ' + value.length + ' available';

        platformBody.append(newTable);
        platformRow.append(platformBody);

        targetDiv.append(platformRow);
    }

    document.getElementById('firmware_totalcount').innerHTML = totalAvailable + ' / ' + totalCount + ' available';
}

class BiosEditor {
    constructor(PlatformId, OKCallback, CancelCallback) {
        this.PlatformId = PlatformId;
        this.OKCallback = OKCallback;
        this.CancelCallback = CancelCallback;
    }

    BiosItems = [];

    async open() {
        // Create the modal
        this.dialog = await new Modal("bios");
        await this.dialog.BuildModal();

        // Get the platform data
        await fetch('/api/v1.1/PlatformMaps/' + this.PlatformId, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(async response => {
            if (response.ok) {
                let result = await response.json();
                this.PlatformData = result;

                // setup the dialog
                this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = this.PlatformData.igdbName;

                // setup the bios editor page
                let biosEditor = this.dialog.modalElement.querySelector('#bios_editor');
                biosEditor.innerHTML = '';

                for (let i = 0; i < this.PlatformData.bios.length; i++) {
                    let biosItem = new MappingBiosItem(this.PlatformData.bios[i].hash, this.PlatformData.bios[i].description, this.PlatformData.bios[i].filename);
                    biosEditor.appendChild(biosItem.Item);
                    this.BiosItems.push(biosItem);
                }

                let newBiosItem = new MappingBiosItem('', '', '');
                biosEditor.appendChild(newBiosItem.Item);
                this.BiosItems.push(newBiosItem);

                let addBiosButton = this.dialog.modalElement.querySelector('#mapping_edit_bios_add');
                addBiosButton.addEventListener('click', () => {
                    let newBiosItem = new MappingBiosItem('', '', '');
                    biosEditor.appendChild(newBiosItem.Item);
                    this.BiosItems.push(newBiosItem);
                });

                // setup the buttons
                let okButton = new ModalButton("OK", 1, this, async function (callingObject) {
                    // build bios items list
                    let biosItems = [];
                    for (let i = 0; i < callingObject.BiosItems.length; i++) {
                        // only add items that are not deleted and have a hash and filename
                        if (callingObject.BiosItems[i].Deleted == false && callingObject.BiosItems[i].HashInput.value != '' && callingObject.BiosItems[i].FilenameInput.value != '') {
                            let biosItem = {
                                hash: callingObject.BiosItems[i].HashInput.value,
                                description: callingObject.BiosItems[i].DescriptionInput.value,
                                filename: callingObject.BiosItems[i].FilenameInput.value
                            };

                            biosItems.push(biosItem);
                        }
                    }
                    callingObject.PlatformData.bios = biosItems;

                    await fetch('/api/v1.1/PlatformMaps/' + callingObject.PlatformId, {
                        method: 'PATCH',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(callingObject.PlatformData)
                    }).then(async response => {
                        if (response.ok) {
                            let result = await response.json();
                            console.log(result);

                            if (callingObject.OKCallback) {
                                callingObject.OKCallback();
                            }

                            callingObject.dialog.close();
                        } else {
                            let warningDialog = new Dialog("Error", "Failed to save platform data", "OK");
                            warningDialog.open();
                        }
                    });
                });
                this.dialog.addButton(okButton);

                // create the cancel button
                let cancelButton = new ModalButton("Cancel", 0, this, function (callingObject) {
                    if (callingObject.CancelCallback) {
                        callingObject.CancelCallback();
                    }

                    callingObject.dialog.close();
                });
                this.dialog.addButton(cancelButton);

                // Show the modal
                this.dialog.open();
            } else {
                let warningDialog = new MessageBox("Error", "Failed to load platform data", "OK");
                warningDialog.open();

                return;
            }
        });
    }
}

class MappingBiosItem {
    constructor(Hash, Description, Filename) {
        this.Hash = Hash;
        this.Description = Description;
        this.Filename = Filename;

        this.Deleted = false;

        this.Item = document.createElement('div');
        this.Item.classList.add('biositem');
        this.Item.classList.add('romrow');

        this.HashInput = document.createElement('input');
        this.HashInput.type = 'text';
        this.HashInput.value = this.Hash;
        this.HashInput.classList.add('biosinput');
        this.HashInput.classList.add('bioshash');
        this.HashInput.placeholder = 'Hash';
        this.Item.appendChild(this.HashInput);

        this.DescriptionInput = document.createElement('input');
        this.DescriptionInput.type = 'text';
        this.DescriptionInput.value = this.Description;
        this.DescriptionInput.classList.add('biosinput');
        this.DescriptionInput.classList.add('biosdescription');
        this.DescriptionInput.placeholder = 'Description';
        this.Item.appendChild(this.DescriptionInput);

        this.FilenameInput = document.createElement('input');
        this.FilenameInput.type = 'text';
        this.FilenameInput.value = this.Filename;
        this.FilenameInput.classList.add('biosinput');
        this.FilenameInput.classList.add('biosfilename');
        this.FilenameInput.placeholder = 'Filename';
        this.Item.appendChild(this.FilenameInput);

        this.DeleteButton = document.createElement('a');
        this.DeleteButton.href = '#';
        this.DeleteButton.classList.add('biositemcontrol');
        this.DeleteButton.classList.add('biosdelete');
        this.DeleteButton.addEventListener('click', () => {
            this.Item.parentElement.removeChild(this.Item);
            this.Deleted = true;
        });
        this.DeleteImage = document.createElement('img');
        this.DeleteImage.src = '/images/delete.svg';
        this.DeleteImage.alt = 'Delete';
        this.DeleteImage.title = 'Delete';
        this.DeleteImage.classList.add('banner_button_image');
        this.DeleteButton.appendChild(this.DeleteImage);
        this.Item.appendChild(this.DeleteButton);
    }
}