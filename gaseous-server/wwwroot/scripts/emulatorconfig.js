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