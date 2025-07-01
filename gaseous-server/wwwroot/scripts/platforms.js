class Mapping {
    constructor(PlatformId, OKCallback, CancelCallback) {
        this.PlatformId = PlatformId;
        this.OKCallback = OKCallback;
        this.CancelCallback = CancelCallback;
    }

    async open() {
        // Create the modal
        this.dialog = new Modal("mappings");
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
            } else {
                let warningDialog = new MessageBox("Error", "Failed to load platform data", "OK");
                warningDialog.open();
            }
        });

        // setup the dialog
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = this.PlatformData.igdbName;

        // setup general page
        this.alternateNames = this.dialog.modalElement.querySelector('#mapping_edit_alternativenames');
        $(this.alternateNames).select2({
            tags: true,
            tokenSeparators: [',']
        });
        this.#AddTokensFromList(this.alternateNames, this.PlatformData.alternateNames);

        this.supportedFileExtensions = this.dialog.modalElement.querySelector('#mapping_edit_supportedfileextensions');
        $(this.supportedFileExtensions).select2({
            tags: true,
            tokenSeparators: [','],
            createTag: function (params) {
                if (params.term.indexOf('.') === -1) {
                    // Return null to disable tag creation
                    return null;
                }

                return {
                    id: params.term.toUpperCase(),
                    text: params.term.toUpperCase()
                }
            }
        });
        this.#AddTokensFromList(this.supportedFileExtensions, this.PlatformData.extensions.supportedFileExtensions);

        this.dialog.modalElement.querySelector('#mapping_edit_igdbslug').value = this.PlatformData.igdbSlug;
        this.dialog.modalElement.querySelector('#mapping_edit_retropie').value = this.PlatformData.retroPieDirectoryName;

        // setup the emulator page
        this.webEmulatorConfiguration = new WebEmulatorConfiguration(this.PlatformData);
        await this.webEmulatorConfiguration.open();
        this.dialog.modalElement.querySelector('#mapping_edit_webemulator').appendChild(this.webEmulatorConfiguration.panel);

        // setup the buttons
        let okButton = new ModalButton("OK", 1, this, async (callingObject) => {
            callingObject.PlatformData.alternateNames = $(callingObject.alternateNames).val();
            callingObject.PlatformData.extensions.supportedFileExtensions = $(callingObject.supportedFileExtensions).val();
            callingObject.PlatformData.retroPieDirectoryName = callingObject.dialog.modalElement.querySelector('#mapping_edit_retropie').value;
            callingObject.PlatformData.webEmulator.type = callingObject.webEmulatorConfiguration.PlatformMap.webEmulator.type;
            callingObject.PlatformData.webEmulator.core = callingObject.webEmulatorConfiguration.PlatformMap.webEmulator.core;
            callingObject.PlatformData.enabledBIOSHashes = callingObject.webEmulatorConfiguration.PlatformMap.enabledBIOSHashes;

            await fetch('/api/v1.1/PlatformMaps/' + callingObject.PlatformId, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(callingObject.PlatformData)
            }).then(async response => {
                if (response.ok) {
                    let result = await response.json();
                } else {
                    let warningDialog = new Dialog("Error", "Failed to save platform data", "OK");
                    warningDialog.open();
                }
            });

            if (this.OKCallback) {
                console.log("Calling OKCallback");
                this.OKCallback();
            }

            callingObject.dialog.close();
            callingObject = null; // Clear the reference to the calling object
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

        this.dialog.open();
    }

    #AddTokensFromList(selectObj, tagList) {
        for (const tag of tagList) {
            let data = {
                id: tag,
                text: tag
            }

            let newOption = new Option(data.text, data.id, true, true);
            $(selectObj).append(newOption).trigger('change');
        }
    }
}

class BiosTable {
    constructor(targetDiv) {
        this.targetDiv = targetDiv;
        this.showAvailableCheckbox = document.getElementById('firmware_showavailable');
        this.showUnavailableCheckbox = document.getElementById('firmware_showunavailable');

        this.showAvailableCheckbox.addEventListener('change', () => {
            this.displayFirmwareList();
        });

        this.showUnavailableCheckbox.addEventListener('change', () => {
            this.displayFirmwareList();
        });
    }

    biosDict = {};

    async loadBios() {
        this.biosDict = {};

        await fetch('/api/v1.1/Bios', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(async response => {
            if (response.ok) {
                let result = await response.json();
                result.sort((a, b) => a.platformname.charCodeAt(0) - b.platformname.charCodeAt(0));

                // sort into a dictionary
                result.forEach(item => {
                    let tempArray = [];
                    if (this.biosDict.hasOwnProperty(item.platformname)) {
                        tempArray = this.biosDict[item.platformname];
                        tempArray.push(item);
                    } else {
                        tempArray.push(item);
                        this.biosDict[item.platformname] = tempArray;
                    }

                    this.biosDict[item.platformname] = tempArray;
                });

                this.displayFirmwareList();
            }
        });
    }

    displayFirmwareList() {
        this.targetDiv.innerHTML = '';

        let totalAvailable = 0;
        let totalCount = 0;

        for (const [key, value] of Object.entries(this.biosDict)) {
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
            platformHeaderEdit.addEventListener('click', () => {
                let biosEditor = new BiosEditor(value[0].platformid, this.loadBios);
                biosEditor.OKCallback = this.loadBios.bind(this);
                biosEditor.open();
            });
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

            // create the header row
            let headerRow = document.createElement('tr');
            headerRow.classList.add('romrow');
            headerRow.classList.add('romheader');

            let headerCell1 = document.createElement('th');
            headerCell1.classList.add('romcell');
            headerCell1.classList.add('card-services-column');
            headerCell1.innerHTML = 'Description';
            headerRow.appendChild(headerCell1);

            let headerCell2 = document.createElement('th');
            headerCell2.classList.add('romcell');
            headerCell2.innerHTML = 'File name';
            headerRow.appendChild(headerCell2);

            let headerCell3 = document.createElement('th');
            headerCell3.classList.add('romcell');
            headerCell3.classList.add('card-services-column');
            headerCell3.innerHTML = 'MD5 Hash';
            headerRow.appendChild(headerCell3);

            let headerCell4 = document.createElement('th');
            headerCell4.classList.add('romcell');
            headerCell4.innerHTML = 'Availability';
            headerRow.appendChild(headerCell4);

            newTable.appendChild(headerRow);

            let totalPlatformAvailable = 0;

            let showAvailable = this.showAvailableCheckbox.checked;
            let showUnavailable = this.showUnavailableCheckbox.checked;

            value.forEach(item => {
                // update counters
                if (item.available == true) {
                    totalAvailable += 1;
                    totalPlatformAvailable += 1;
                }

                if (
                    (item.available == true && showAvailable == true) ||
                    (item.available == false && showUnavailable == true)
                ) {
                    let biosFilename = document.createElement('a');
                    biosFilename.href = '/api/v1.1/Bios/' + item.platformid + '/' + item.filename;
                    biosFilename.innerHTML = item.filename;
                    biosFilename.classList.add('romlink');

                    let availableText = document.createElement('span');
                    if (item.available == true) {
                        availableText.innerHTML = 'Available';
                        availableText.classList.add('greentext');

                        biosFilename = document.createElement('a');
                        biosFilename.href = '/api/v1.1/Bios/' + item.platformid + '/' + item.filename;
                        biosFilename.innerHTML = item.filename;
                        biosFilename.classList.add('romlink');
                    } else {
                        availableText.innerHTML = 'Unavailable';
                        availableText.classList.add('redtext');

                        biosFilename = document.createElement('span');
                        biosFilename.innerHTML = item.filename;
                    }

                    // create a new row
                    let itemRow = document.createElement('tr');
                    itemRow.classList.add('romrow');

                    // create the cells
                    let descriptionCell = document.createElement('td');
                    descriptionCell.classList.add('romcell');
                    descriptionCell.classList.add('bioscell');
                    descriptionCell.classList.add('card-services-column');
                    descriptionCell.innerHTML = item.description;
                    itemRow.appendChild(descriptionCell);

                    let filenameCell = document.createElement('td');
                    filenameCell.classList.add('romcell');
                    filenameCell.classList.add('bioscell');
                    filenameCell.appendChild(biosFilename);
                    itemRow.appendChild(filenameCell);

                    let hashCell = document.createElement('td');
                    hashCell.classList.add('romcell');
                    hashCell.classList.add('bioscell');
                    hashCell.classList.add('card-services-column');
                    hashCell.innerHTML = item.hash;
                    itemRow.appendChild(hashCell);

                    let availableCell = document.createElement('td');
                    availableCell.classList.add('romcell');
                    availableCell.classList.add('bioscell');
                    availableCell.appendChild(availableText);
                    itemRow.appendChild(availableCell);

                    newTable.appendChild(itemRow);
                }
                totalCount += 1;
            });

            platformHeaderCounter.innerHTML = totalPlatformAvailable + ' / ' + value.length + ' available';

            platformBody.append(newTable);
            platformRow.append(platformBody);

            this.targetDiv.append(platformRow);
        }

        document.getElementById('firmware_totalcount').innerHTML = totalAvailable + ' / ' + totalCount + ' available';
    }
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
        this.dialog = new Modal("bios");
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

                this.PlatformData.bios.forEach(bios => {
                    let biosItem = new MappingBiosItem(bios.hash, bios.description, bios.filename);
                    biosEditor.appendChild(biosItem.Item);
                    this.BiosItems.push(biosItem);
                });

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
                let okButton = new ModalButton("OK", 1, this, async (callingObject) => {
                    // build bios items list
                    let biosItems = [];
                    callingObject.BiosItems.forEach(item => {
                        // only add items that are not deleted and have a hash and filename
                        if (item.Deleted == false && item.HashInput.value != '' && item.FilenameInput.value != '') {
                            let biosItem = {
                                hash: item.HashInput.value,
                                description: item.DescriptionInput.value,
                                filename: item.FilenameInput.value
                            };

                            biosItems.push(biosItem);
                        }
                    });
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

                            if (this.OKCallback) {
                                this.OKCallback();
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
                let cancelButton = new ModalButton("Cancel", 0, this, (callingObject) => {
                    if (this.CancelCallback) {
                        this.CancelCallback();
                    }

                    callingObject.dialog.close();
                });
                this.dialog.addButton(cancelButton);

                // Show the modal
                this.dialog.open();
            } else {
                let warningDialog = new MessageBox("Error", "Failed to load platform data", "OK");
                warningDialog.open();
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