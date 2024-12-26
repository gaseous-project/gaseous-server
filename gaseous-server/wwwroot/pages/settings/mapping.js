function loadPlatformMapping(Overwrite) {
    let queryString = '';
    if (Overwrite == true) {
        console.log('Overwriting PlatformMap.json');
        queryString = '?ResetToDefault=true';
    }

    ajaxCall(
        '/api/v1.1/PlatformMaps' + queryString,
        'GET',
        function (result) {
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

            for (let i = 0; i < result.length; i++) {
                let logo = document.createElement('img');
                logo.src = '/api/v1.1/Platforms/' + result[i].igdbId + '/platformlogo/original/logo.png';
                logo.alt = result[i].igdbName;
                logo.title = result[i].igdbName;
                logo.classList.add('platform_image');

                let hasWebEmulator = '';
                if (result[i].webEmulator.type.length > 0) {
                    hasWebEmulator = 'Yes';
                }

                let platformEditButton = null;
                if (userProfile.roles.includes("Admin")) {
                    platformEditButton = document.createElement('div');
                    platformEditButton.classList.add('romlink');
                    platformEditButton.onclick = function () {
                        let mappingModal = new Mapping(result[i].igdbId, loadPlatformMapping);
                        mappingModal.open();
                    };
                    let editButtonImage = document.createElement('img');
                    editButtonImage.src = '/images/edit.svg';
                    editButtonImage.alt = 'Edit';
                    editButtonImage.title = 'Edit';
                    editButtonImage.classList.add('banner_button_image');
                    platformEditButton.appendChild(editButtonImage);
                }

                let newRow = [
                    logo,
                    result[i].igdbName,
                    result[i].extensions.supportedFileExtensions.join(', '),
                    result[i].extensions.uniqueFileExtensions.join(', '),
                    hasWebEmulator,
                    platformEditButton
                ];

                newTable.appendChild(createTableRow(false, newRow, 'romrow', 'romcell logs_table_cell'));
            }
        }
    );
}

function DownloadJSON() {
    window.location = '/api/v1.1/PlatformMaps/PlatformMap.json';
}

function SetupButtons() {
    if (userProfile.roles.includes("Admin")) {
        document.getElementById('settings_mapping_import').style.display = '';

        // Setup the JSON import button
        document.getElementById('uploadjson').addEventListener('change', function () {
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

        document.getElementById('importjson').addEventListener('click', openDialog);

        // Setup the JSON export button
        document.getElementById('exportjson').addEventListener('click', DownloadJSON);

        // Setup the reset to defaults button
        document.getElementById('resetmapping').addEventListener('click', function () {
            let warningDialog = new MessageBox("Platform Mapping Reset", "This will reset the platform mappings to the default values. Are you sure you want to continue?");
            warningDialog.addButton(new ModalButton("OK", 2, warningDialog, async (callingObject) => {
                loadPlatformMapping(true);
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

function openDialog() {
    document.getElementById('uploadjson').click();
}

class Mapping {
    constructor(PlatformId, OKCallback, CancelCallback) {
        this.PlatformId = PlatformId;
        this.OKCallback = OKCallback;
        this.CancelCallback = CancelCallback;
    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("mappings");
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
        let okButton = new ModalButton("OK", 1, this, async function (callingObject) {
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

        this.dialog.open();
    }

    #AddTokensFromList(selectObj, tagList) {
        for (let i = 0; i < tagList.length; i++) {
            let data = {
                id: tagList[i],
                text: tagList[i]
            }

            let newOption = new Option(data.text, data.id, true, true);
            $(selectObj).append(newOption).trigger('change');
        }
    }
}

class WebEmulatorConfiguration {
    constructor(PlatformMap) {
        this.PlatformMap = PlatformMap;

        if (this.PlatformMap.bios.length > 0) {
            this.PlatformMap.bios.sort((a, b) => (a.description > b.description) ? 1 : ((b.description > a.description) ? -1 : 0))
        }
    }

    async open() {
        // create the panel
        this.panel = document.createElement('div');
        const templateResponse = await fetch('/pages/modals/webemulator.html');
        const templateContent = await templateResponse.text();
        this.panel.innerHTML = templateContent;

        // load the emulator list
        this.#LoadEmulatorList();

        // select the emulator and core
        if (this.PlatformMap.webEmulator.type.length === 0) {
            this.PlatformMap.webEmulator.type = 'none';
        }
        let emulatorSelect = this.panel.querySelector('#webemulator_select_' + this.PlatformMap.webEmulator.type);
        if (emulatorSelect != null) {
            emulatorSelect.checked = true;
            this.#LoadCoreList(this.PlatformMap.webEmulator.type);
            let coreSelect = this.panel.querySelector('#webemulator_core_select_' + this.PlatformMap.webEmulator.core);
            if (coreSelect != null) {
                coreSelect.checked = true;
            }
        }
        this.#EmulatorInfoPanels();

        // load the bios list
        this.#LoadBiosList();
    }

    #LoadEmulatorList() {
        let emulatorSelect = this.panel.querySelector('#webemulator_select');

        let emulatorSelectTable = document.createElement('table');

        // create radio button list of available web emulators
        let emulatorSelectTableRow = document.createElement('tr');
        let emulatorSelectTableCell = document.createElement('td');

        let newOption = document.createElement('input');
        newOption.id = 'webemulator_select_none';
        newOption.name = 'webemulator_select';
        newOption.type = 'radio';
        newOption.value = '';
        newOption.addEventListener('change', () => {
            this.#LoadCoreList('');
            this.PlatformMap.webEmulator.type = '';

            this.#EmulatorInfoPanels();
        });
        emulatorSelectTableCell.appendChild(newOption);

        let newLabel = document.createElement('label');
        newLabel.htmlFor = 'webemulator_select_none';
        newLabel.innerHTML = '&nbsp;None';
        emulatorSelectTableCell.appendChild(newLabel);

        emulatorSelectTableRow.appendChild(emulatorSelectTableCell);
        emulatorSelectTable.appendChild(emulatorSelectTableRow);

        if (this.PlatformMap.webEmulator.availableWebEmulators) {
            if (this.PlatformMap.webEmulator.availableWebEmulators.length > 0) {
                for (let i = 0; i < this.PlatformMap.webEmulator.availableWebEmulators.length; i++) {
                    let emulatorSelectTableRow = document.createElement('tr');
                    let emulatorSelectTableCell = document.createElement('td');

                    let newOption = document.createElement('input');
                    newOption.id = 'webemulator_select_' + this.PlatformMap.webEmulator.availableWebEmulators[i].emulatorType;
                    newOption.name = 'webemulator_select';
                    newOption.type = 'radio';
                    newOption.value = this.PlatformMap.webEmulator.availableWebEmulators[i].emulatorType;
                    newOption.addEventListener('change', () => {
                        this.#LoadCoreList(this.PlatformMap.webEmulator.availableWebEmulators[i].emulatorType);
                        this.PlatformMap.webEmulator.type = this.PlatformMap.webEmulator.availableWebEmulators[i].emulatorType;

                        this.#EmulatorInfoPanels();
                    });
                    emulatorSelectTableCell.appendChild(newOption);

                    let newLabel = document.createElement('label');
                    newLabel.htmlFor = 'webemulator_select_' + this.PlatformMap.webEmulator.availableWebEmulators[i].emulatorType;
                    newLabel.innerHTML = "&nbsp;" + this.PlatformMap.webEmulator.availableWebEmulators[i].emulatorType;
                    emulatorSelectTableCell.appendChild(newLabel);

                    emulatorSelectTableRow.appendChild(emulatorSelectTableCell);
                    emulatorSelectTable.appendChild(emulatorSelectTableRow);
                }
            }
        }

        emulatorSelect.appendChild(emulatorSelectTable);
    }

    #EmulatorInfoPanels() {
        // show appropriate emulator info panel
        switch (this.PlatformMap.webEmulator.type) {
            case 'EmulatorJS':
                this.panel.querySelector('#webemulator_info_emulatorjs').style.display = '';
                break;

            default:
                this.panel.querySelector('#webemulator_info_emulatorjs').style.display = 'none';
                break;
        }
    }

    #LoadCoreList(EmulatorType) {
        let coreSelectSection = this.panel.querySelector('#webemulator_core_select-section');
        coreSelectSection.style.display = 'none';

        // show core help (if available)
        switch (EmulatorType) {
            case 'EmulatorJS':
                this.panel.querySelector('#webemulator_core_info_emulatorjs').style.display = '';
                break;
        }

        let coreSelect = this.panel.querySelector('#webemulator_core_select');
        coreSelect.innerHTML = '';

        let coreSelectTable = document.createElement('table');

        if (this.PlatformMap.webEmulator.availableWebEmulators) {
            if (this.PlatformMap.webEmulator.availableWebEmulators.length > 0) {
                for (let i = 0; i < this.PlatformMap.webEmulator.availableWebEmulators.length; i++) {
                    if (this.PlatformMap.webEmulator.availableWebEmulators[i].emulatorType == EmulatorType) {
                        for (let j = 0; j < this.PlatformMap.webEmulator.availableWebEmulators[i].availableWebEmulatorCores.length; j++) {
                            coreSelectSection.style.display = '';

                            let coreSelectTableRow = document.createElement('tr');
                            let coreSelectTableCell = document.createElement('td');

                            let newOption = document.createElement('input');
                            newOption.id = 'webemulator_core_select_' + this.PlatformMap.webEmulator.availableWebEmulators[i].availableWebEmulatorCores[j].core;
                            newOption.name = 'webemulator_core_select';
                            newOption.type = 'radio';
                            newOption.value = this.PlatformMap.webEmulator.availableWebEmulators[i].availableWebEmulatorCores[j].core;
                            newOption.text = this.PlatformMap.webEmulator.availableWebEmulators[i].availableWebEmulatorCores[j].core;
                            if (this.PlatformMap.webEmulator.availableWebEmulators[i].availableWebEmulatorCores[j].default == true) {
                                newOption.checked = true;
                            }
                            newOption.addEventListener('change', () => {
                                this.PlatformMap.webEmulator.core = this.PlatformMap.webEmulator.availableWebEmulators[i].availableWebEmulatorCores[j].core;
                            });
                            coreSelectTableCell.appendChild(newOption);

                            let newLabel = document.createElement('label');
                            newLabel.htmlFor = 'webemulator_core_select_' + this.PlatformMap.webEmulator.availableWebEmulators[i].availableWebEmulatorCores[j].core;
                            let labelText = "";
                            if (this.PlatformMap.webEmulator.availableWebEmulators[i].availableWebEmulatorCores[j].alternateCoreName.length > 0) {
                                labelText = "&nbsp;" + this.PlatformMap.webEmulator.availableWebEmulators[i].availableWebEmulatorCores[j].core + " (maps to core: " + this.PlatformMap.webEmulator.availableWebEmulators[i].availableWebEmulatorCores[j].alternateCoreName + ")";
                            } else {
                                labelText = "&nbsp;" + this.PlatformMap.webEmulator.availableWebEmulators[i].availableWebEmulatorCores[j].core;
                            }
                            if (this.PlatformMap.webEmulator.availableWebEmulators[i].availableWebEmulatorCores[j].default == true) {
                                labelText += " (Default)";
                            }
                            newLabel.innerHTML = labelText;
                            coreSelectTableCell.appendChild(newLabel);

                            coreSelectTableRow.appendChild(coreSelectTableCell);
                            coreSelectTable.appendChild(coreSelectTableRow);
                        }
                    }
                }
            }
        }

        coreSelect.appendChild(coreSelectTable);
    }

    #LoadBiosList() {
        if (this.PlatformMap.bios.length > 0) {
            let biosSelect = this.panel.querySelector('#webemulator_bios_select');
            biosSelect.innerHTML = '';

            let biosSelectTable = document.createElement('table');
            biosSelectTable.style.width = '100%';
            for (let i = 0; i < this.PlatformMap.bios.length; i++) {
                let biosSelectTableRow = document.createElement('tr');
                biosSelectTableRow.classList.add('romrow');
                let biosSelectTableCell = document.createElement('td');
                biosSelectTableCell.classList.add('romcell');

                let newOption = document.createElement('input');
                newOption.id = 'webemulator_bios_select_' + this.PlatformMap.bios[i].hash;
                newOption.name = 'webemulator_bios_select';
                newOption.type = 'checkbox';
                newOption.value = this.PlatformMap.bios[i].hash;
                if (this.PlatformMap.enabledBIOSHashes.includes(this.PlatformMap.bios[i].hash)) {
                    newOption.checked = true;
                }
                newOption.addEventListener('change', () => {
                    if (newOption.checked) {
                        this.PlatformMap.enabledBIOSHashes.push(this.PlatformMap.bios[i].hash);
                    } else {
                        let index = this.PlatformMap.enabledBIOSHashes.indexOf(this.PlatformMap.bios[i].hash);
                        if (index > -1) {
                            this.PlatformMap.enabledBIOSHashes.splice(index, 1);
                        }
                    }
                });
                biosSelectTableCell.appendChild(newOption);

                let newLabel = document.createElement('label');
                newLabel.htmlFor = 'webemulator_bios_select_' + this.PlatformMap.bios[i].hash;
                let labelText = "";
                if (this.PlatformMap.bios[i].description.length > 0) {
                    labelText = this.PlatformMap.bios[i].description + " (" + this.PlatformMap.bios[i].filename + ")";
                } else {
                    labelText = this.PlatformMap.bios[i].filename;
                }
                newLabel.innerHTML = "&nbsp;" + labelText;
                biosSelectTableCell.appendChild(newLabel);

                biosSelectTableRow.appendChild(biosSelectTableCell);
                biosSelectTable.appendChild(biosSelectTableRow);
            }
            biosSelect.appendChild(biosSelectTable);
        } else {
            this.panel.querySelector('#webemulator_bios_select-section').style.display = 'none';
        }
    }
}