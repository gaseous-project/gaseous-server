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
                let hasWebEmulator = '';
                if (result[i].webEmulator.type.length > 0) {
                    hasWebEmulator = 'Yes';
                }

                let platformLink = '';
                let platformEditButton = null;
                if (userProfile.roles.includes("Admin")) {
                    platformLink = '<a href="#/" onclick="ShowPlatformMappingDialog(' + result[i].igdbId + ');" class="romlink">' + result[i].igdbName + '</a>';
                    platformEditButton = document.createElement('a');
                    platformEditButton.href = '#';
                    platformEditButton.onclick = function () {
                        let mappingModal = new Mapping(result[i].igdbId);
                        mappingModal.open();
                    };
                    let editButtonImage = document.createElement('img');
                    editButtonImage.src = '/images/edit.svg';
                    editButtonImage.alt = 'Edit';
                    editButtonImage.title = 'Edit';
                    editButtonImage.classList.add('banner_button_image');
                    platformEditButton.appendChild(editButtonImage);
                } else {
                    platformLink = result[i].igdbName;
                }

                let newRow = [
                    platformLink,
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
    }
}

function openDialog() {
    document.getElementById('uploadjson').click();
}

class Mapping {
    constructor(PlatformId) {
        this.PlatformId = PlatformId;
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
                let warningDialog = new Dialog("Error", "Failed to load platform data", "OK");
                warningDialog.open();
            }
        });

        console.log(this.PlatformData);

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
        this.webEmulatorConfiguration = new WebEmulatorConfiguration(this.PlatformData.webEmulator.availableWebEmulators, this.PlatformData.webEmulator.type, this.PlatformData.webEmulator.core);
        await this.webEmulatorConfiguration.open();
        this.dialog.modalElement.querySelector('#mapping_edit_webemulator').appendChild(this.webEmulatorConfiguration.panel);

        // setup the buttons
        let okButton = new ModalButton("OK", 1, this, async function (callingObject) {
            let model = {
                IGDBId: callingObject.PlatformId,
                IGDBName: callingObject.PlatformData.igdbName,
                IGDBSlug: callingObject.dialog.modalElement.querySelector('#mapping_edit_igdbslug').value,
                AlternateNames: $(callingObject.alternateNames).val(),
                Extensions: {
                    SupportedFileExtensions: $(callingObject.supportedFileExtensions).val(),
                    UniqueFileExtensions: []
                },
                RetroPieDirectoryName: callingObject.dialog.modalElement.querySelector('#mapping_edit_retropie').value,
                WebEmulator: {
                    Type: callingObject.webEmulatorConfiguration.SelectedWebEmulator,
                    Core: callingObject.webEmulatorConfiguration.SelectedWebEmulatorCore
                }
            };
            console.log(model);

            //callingObject.dialog.close();
        });
        this.dialog.addButton(okButton);

        // create the cancel button
        let cancelButton = new ModalButton("Cancel", 0, this, function (callingObject) {
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
    constructor(AvailableWebEmulators, SelectedWebEmulator, SelectedWebEmulatorCore) {
        this.AvailableWebEmulators = AvailableWebEmulators;
        this.SelectedWebEmulator = SelectedWebEmulator;
        this.SelectedWebEmulatorCore = SelectedWebEmulatorCore;
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
        if (this.SelectedWebEmulator.length === 0) {
            this.SelectedWebEmulator = 'none';
        }
        let emulatorSelect = this.panel.querySelector('#webemulator_select_' + this.SelectedWebEmulator);
        if (emulatorSelect != null) {
            emulatorSelect.checked = true;
            this.#LoadCoreList(this.SelectedWebEmulator);
            let coreSelect = this.panel.querySelector('#webemulator_core_select_' + this.SelectedWebEmulatorCore);
            if (coreSelect != null) {
                coreSelect.checked = true;
            }
        }
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
            this.SelectedWebEmulator = '';
        });
        emulatorSelectTableCell.appendChild(newOption);

        let newLabel = document.createElement('label');
        newLabel.htmlFor = 'webemulator_select_none';
        newLabel.innerHTML = '&nbsp;None';
        emulatorSelectTableCell.appendChild(newLabel);

        emulatorSelectTableRow.appendChild(emulatorSelectTableCell);
        emulatorSelectTable.appendChild(emulatorSelectTableRow);

        if (this.AvailableWebEmulators) {
            if (this.AvailableWebEmulators.length > 0) {
                for (let i = 0; i < this.AvailableWebEmulators.length; i++) {
                    let emulatorSelectTableRow = document.createElement('tr');
                    let emulatorSelectTableCell = document.createElement('td');

                    let newOption = document.createElement('input');
                    newOption.id = 'webemulator_select_' + this.AvailableWebEmulators[i].emulatorType;
                    newOption.name = 'webemulator_select';
                    newOption.type = 'radio';
                    newOption.value = this.AvailableWebEmulators[i].emulatorType;
                    newOption.addEventListener('change', () => {
                        this.#LoadCoreList(this.AvailableWebEmulators[i].emulatorType);
                        this.SelectedWebEmulator = this.AvailableWebEmulators[i].emulatorType;
                    });
                    emulatorSelectTableCell.appendChild(newOption);

                    let newLabel = document.createElement('label');
                    newLabel.htmlFor = 'webemulator_select_' + this.AvailableWebEmulators[i].emulatorType;
                    newLabel.innerHTML = "&nbsp;" + this.AvailableWebEmulators[i].emulatorType;
                    emulatorSelectTableCell.appendChild(newLabel);

                    emulatorSelectTableRow.appendChild(emulatorSelectTableCell);
                    emulatorSelectTable.appendChild(emulatorSelectTableRow);
                }
            }
        }

        emulatorSelect.appendChild(emulatorSelectTable);
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

        if (this.AvailableWebEmulators) {
            if (this.AvailableWebEmulators.length > 0) {
                for (let i = 0; i < this.AvailableWebEmulators.length; i++) {
                    if (this.AvailableWebEmulators[i].emulatorType == EmulatorType) {
                        for (let j = 0; j < this.AvailableWebEmulators[i].availableWebEmulatorCores.length; j++) {
                            coreSelectSection.style.display = '';

                            let coreSelectTableRow = document.createElement('tr');
                            let coreSelectTableCell = document.createElement('td');

                            let newOption = document.createElement('input');
                            newOption.id = 'webemulator_core_select_' + this.AvailableWebEmulators[i].availableWebEmulatorCores[j].core;
                            newOption.name = 'webemulator_core_select';
                            newOption.type = 'radio';
                            newOption.value = this.AvailableWebEmulators[i].availableWebEmulatorCores[j].core;
                            newOption.text = this.AvailableWebEmulators[i].availableWebEmulatorCores[j].core;
                            if (this.AvailableWebEmulators[i].availableWebEmulatorCores[j].default == true) {
                                newOption.checked = true;
                            }
                            newOption.addEventListener('change', () => {
                                this.SelectedWebEmulatorCore = this.AvailableWebEmulators[i].availableWebEmulatorCores[j].core;
                            });
                            coreSelectTableCell.appendChild(newOption);

                            let newLabel = document.createElement('label');
                            newLabel.htmlFor = 'webemulator_core_select_' + this.AvailableWebEmulators[i].availableWebEmulatorCores[j].core;
                            let labelText = "";
                            if (this.AvailableWebEmulators[i].availableWebEmulatorCores[j].alternateCoreName.length > 0) {
                                labelText = "&nbsp;" + this.AvailableWebEmulators[i].availableWebEmulatorCores[j].core + " (maps to core: " + this.AvailableWebEmulators[i].availableWebEmulatorCores[j].alternateCoreName + ")";
                            } else {
                                labelText = "&nbsp;" + this.AvailableWebEmulators[i].availableWebEmulatorCores[j].core;
                            }
                            if (this.AvailableWebEmulators[i].availableWebEmulatorCores[j].default == true) {
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
}