// Corrected localized version
class Mapping {
    constructor(PlatformId, OKCallback, CancelCallback) {
        this.PlatformId = PlatformId;
        this.OKCallback = OKCallback;
        this.CancelCallback = CancelCallback;
    }

    async open() {
        this.dialog = new Modal("mappings");
        await this.dialog.BuildModal();

        // Load platform data
        await fetch('/api/v1.1/PlatformMaps/' + this.PlatformId, {
            method: 'GET',
            headers: { 'Content-Type': 'application/json' }
        }).then(async response => {
            if (response.ok) {
                this.PlatformData = await response.json();
            } else {
                new MessageBox(
                    window.lang ? window.lang.translate('generic.error') : 'Error',
                    window.lang ? window.lang.translate('platforms.mapping.failed_load') : 'Failed to load platform data'
                ).open();
            }
        });
        if (!this.PlatformData) { return; }

        // Header
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = this.PlatformData.igdbName;

        // Alternate names
        this.alternateNames = this.dialog.modalElement.querySelector('#mapping_edit_alternativenames');
        $(this.alternateNames).select2({ tags: true, tokenSeparators: [','] });
        this.#AddTokensFromList(this.alternateNames, this.PlatformData.alternateNames);

        // Supported file extensions
        this.supportedFileExtensions = this.dialog.modalElement.querySelector('#mapping_edit_supportedfileextensions');
        $(this.supportedFileExtensions).select2({
            tags: true,
            tokenSeparators: [','],
            createTag: function (params) {
                if (params.term.indexOf('.') === -1) { return null; }
                return { id: params.term.toUpperCase(), text: params.term.toUpperCase() };
            }
        });
        this.#AddTokensFromList(this.supportedFileExtensions, this.PlatformData.extensions.supportedFileExtensions);

        // Slug & retropie directory
        this.dialog.modalElement.querySelector('#mapping_edit_igdbslug').value = this.PlatformData.igdbSlug;
        this.dialog.modalElement.querySelector('#mapping_edit_retropie').value = this.PlatformData.retroPieDirectoryName;

        // Web emulator config
        this.webEmulatorConfiguration = new WebEmulatorConfiguration(this.PlatformData);
        await this.webEmulatorConfiguration.open();
        this.dialog.modalElement.querySelector('#mapping_edit_webemulator').appendChild(this.webEmulatorConfiguration.panel);

        // OK button
        const okButton = new ModalButton(window.lang ? window.lang.translate('generic.ok') : 'OK', 1, this, async (callingObject) => {
            callingObject.PlatformData.alternateNames = $(callingObject.alternateNames).val();
            callingObject.PlatformData.extensions.supportedFileExtensions = $(callingObject.supportedFileExtensions).val();
            callingObject.PlatformData.retroPieDirectoryName = callingObject.dialog.modalElement.querySelector('#mapping_edit_retropie').value;
            callingObject.PlatformData.webEmulator.type = callingObject.webEmulatorConfiguration.PlatformMap.webEmulator.type;
            callingObject.PlatformData.webEmulator.core = callingObject.webEmulatorConfiguration.PlatformMap.webEmulator.core;
            callingObject.PlatformData.enabledBIOSHashes = callingObject.webEmulatorConfiguration.PlatformMap.enabledBIOSHashes;

            await fetch('/api/v1.1/PlatformMaps/' + callingObject.PlatformId, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(callingObject.PlatformData)
            }).then(async response => {
                if (!response.ok) {
                    new Dialog(
                        window.lang ? window.lang.translate('generic.error') : 'Error',
                        window.lang ? window.lang.translate('platforms.mapping.failed_save') : 'Failed to save platform data',
                        window.lang ? window.lang.translate('generic.ok') : 'OK'
                    ).open();
                }
            });

            if (this.OKCallback) { this.OKCallback(); }
            callingObject.dialog.close();
        });
        this.dialog.addButton(okButton);

        // Cancel button
        const cancelButton = new ModalButton(window.lang ? window.lang.translate('generic.cancel') : 'Cancel', 0, this, (callingObject) => {
            if (callingObject.CancelCallback) { callingObject.CancelCallback(); }
            callingObject.dialog.close();
        });
        this.dialog.addButton(cancelButton);

        this.dialog.open();
    }

    #AddTokensFromList(selectObj, tagList) {
        for (const tag of tagList) {
            const data = { id: tag, text: tag };
            const newOption = new Option(data.text, data.id, true, true);
            $(selectObj).append(newOption).trigger('change');
        }
    }
}

class BiosTable {
    constructor(targetDiv) {
        this.targetDiv = targetDiv;
        this.showAvailableCheckbox = document.getElementById('firmware_showavailable');
        this.showUnavailableCheckbox = document.getElementById('firmware_showunavailable');
        this.showAvailableCheckbox.addEventListener('change', () => this.displayFirmwareList());
        this.showUnavailableCheckbox.addEventListener('change', () => this.displayFirmwareList());
        this.biosDict = {};
    }

    async loadBios() {
        this.biosDict = {};
        await fetch('/api/v1.1/Bios', { method: 'GET', headers: { 'Content-Type': 'application/json' } }).then(async response => {
            if (response.ok) {
                const result = await response.json();
                result.sort((a, b) => a.platformname.localeCompare(b.platformname));
                result.forEach(item => {
                    const arr = this.biosDict[item.platformname] || [];
                    arr.push(item);
                    this.biosDict[item.platformname] = arr;
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
            const platformRow = document.createElement('div');
            platformRow.classList.add('section');
            const platformHeader = document.createElement('div');
            platformHeader.classList.add('section-header');
            const platformHeaderValue = document.createElement('span');
            platformHeaderValue.innerHTML = key;
            platformHeader.appendChild(platformHeaderValue);
            const platformHeaderEdit = document.createElement('a');
            platformHeaderEdit.href = '#';
            platformHeaderEdit.style.float = 'right';
            platformHeaderEdit.addEventListener('click', () => {
                const biosEditor = new BiosEditor(value[0].platformid, this.loadBios.bind(this));
                biosEditor.OKCallback = this.loadBios.bind(this);
                biosEditor.open();
            });
            const platformHeaderEditIcon = document.createElement('img');
            platformHeaderEditIcon.src = '/images/edit.svg';
            platformHeaderEditIcon.classList.add('banner_button_image');
            platformHeaderEdit.appendChild(platformHeaderEditIcon);
            platformHeader.appendChild(platformHeaderEdit);
            const platformHeaderCounter = document.createElement('span');
            platformHeaderCounter.style.float = 'right';
            platformHeaderCounter.style.marginRight = '10px';
            platformHeader.appendChild(platformHeaderCounter);
            platformRow.appendChild(platformHeader);
            const platformBody = document.createElement('div');
            platformBody.classList.add('section-body');
            const newTable = document.createElement('table');
            newTable.classList.add('romtable');
            newTable.setAttribute('cellspacing', 0);
            const headerRow = document.createElement('tr');
            headerRow.classList.add('romrow', 'romheader');
            const headerCell1 = document.createElement('th');
            headerCell1.classList.add('romcell', 'card-services-column');
            headerCell1.innerHTML = window.lang ? window.lang.translate('platforms.firmware.table.header.description') : 'Description';
            headerRow.appendChild(headerCell1);
            const headerCell2 = document.createElement('th');
            headerCell2.classList.add('romcell');
            headerCell2.innerHTML = window.lang ? window.lang.translate('platforms.firmware.table.header.file_name') : 'File name';
            headerRow.appendChild(headerCell2);
            const headerCell3 = document.createElement('th');
            headerCell3.classList.add('romcell', 'card-services-column');
            headerCell3.innerHTML = window.lang ? window.lang.translate('platforms.firmware.table.header.md5_hash') : 'MD5 Hash';
            headerRow.appendChild(headerCell3);
            const headerCell4 = document.createElement('th');
            headerCell4.classList.add('romcell');
            headerCell4.innerHTML = window.lang ? window.lang.translate('platforms.firmware.table.header.availability') : 'Availability';
            headerRow.appendChild(headerCell4);
            newTable.appendChild(headerRow);
            let totalPlatformAvailable = 0;
            const showAvailable = this.showAvailableCheckbox.checked;
            const showUnavailable = this.showUnavailableCheckbox.checked;
            value.forEach(item => {
                if (item.available) { totalAvailable++; totalPlatformAvailable++; }
                if ((item.available && showAvailable) || (!item.available && showUnavailable)) {
                    let biosFilename = document.createElement(item.available ? 'a' : 'span');
                    if (item.available) {
                        biosFilename.href = '/api/v1.1/Bios/' + item.platformid + '/' + item.filename;
                        biosFilename.classList.add('romlink');
                    }
                    biosFilename.innerHTML = item.filename;
                    const availableText = document.createElement('span');
                    if (item.available) {
                        availableText.innerHTML = window.lang ? window.lang.translate('firmware.filter.available') : 'Available';
                        availableText.classList.add('greentext');
                    } else {
                        availableText.innerHTML = window.lang ? window.lang.translate('firmware.filter.unavailable') : 'Unavailable';
                        availableText.classList.add('redtext');
                    }
                    const itemRow = document.createElement('tr');
                    itemRow.classList.add('romrow');
                    const descriptionCell = document.createElement('td');
                    descriptionCell.classList.add('romcell', 'bioscell', 'card-services-column');
                    descriptionCell.innerHTML = item.description;
                    itemRow.appendChild(descriptionCell);
                    const filenameCell = document.createElement('td');
                    filenameCell.classList.add('romcell', 'bioscell');
                    filenameCell.appendChild(biosFilename);
                    itemRow.appendChild(filenameCell);
                    const hashCell = document.createElement('td');
                    hashCell.classList.add('romcell', 'bioscell', 'card-services-column');
                    hashCell.innerHTML = item.hash;
                    itemRow.appendChild(hashCell);
                    const availableCell = document.createElement('td');
                    availableCell.classList.add('romcell', 'bioscell');
                    availableCell.appendChild(availableText);
                    itemRow.appendChild(availableCell);
                    newTable.appendChild(itemRow);
                }
                totalCount++;
            });
            platformHeaderCounter.innerHTML = window.lang ? window.lang.translate('platforms.firmware.platform_available_count', [totalPlatformAvailable, value.length]) : (totalPlatformAvailable + ' / ' + value.length + ' available');
            platformBody.append(newTable);
            platformRow.append(platformBody);
            this.targetDiv.append(platformRow);
        }
        document.getElementById('firmware_totalcount').innerHTML = window.lang ? window.lang.translate('platforms.firmware.total_available_count', [totalAvailable, totalCount]) : (totalAvailable + ' / ' + totalCount + ' available');
    }
}

class BiosEditor {
    constructor(PlatformId, OKCallback, CancelCallback) {
        this.PlatformId = PlatformId;
        this.OKCallback = OKCallback;
        this.CancelCallback = CancelCallback;
        this.BiosItems = [];
    }

    async open() {
        this.dialog = new Modal("bios");
        await this.dialog.BuildModal();
        await fetch('/api/v1.1/PlatformMaps/' + this.PlatformId, { method: 'GET', headers: { 'Content-Type': 'application/json' } }).then(async response => {
            if (response.ok) {
                this.PlatformData = await response.json();
            } else {
                new MessageBox(window.lang ? window.lang.translate('generic.error') : 'Error', window.lang ? window.lang.translate('platforms.mapping.failed_load') : 'Failed to load platform data').open();
            }
        });
        if (!this.PlatformData) { return; }
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = this.PlatformData.igdbName;
        const biosEditor = this.dialog.modalElement.querySelector('#bios_editor');
        biosEditor.innerHTML = '';
        this.PlatformData.bios.forEach(bios => {
            const biosItem = new MappingBiosItem(bios.hash, bios.description, bios.filename);
            biosEditor.appendChild(biosItem.Item);
            this.BiosItems.push(biosItem);
        });
        const newBiosItem = new MappingBiosItem('', '', '');
        biosEditor.appendChild(newBiosItem.Item);
        this.BiosItems.push(newBiosItem);
        const addBiosButton = this.dialog.modalElement.querySelector('#mapping_edit_bios_add');
        addBiosButton.addEventListener('click', () => {
            const nb = new MappingBiosItem('', '', '');
            biosEditor.appendChild(nb.Item);
            this.BiosItems.push(nb);
        });
        const okButton = new ModalButton(window.lang ? window.lang.translate('generic.ok') : 'OK', 1, this, async (callingObject) => {
            const biosItems = [];
            callingObject.BiosItems.forEach(item => {
                if (!item.Deleted && item.HashInput.value !== '' && item.FilenameInput.value !== '') {
                    biosItems.push({ hash: item.HashInput.value, description: item.DescriptionInput.value, filename: item.FilenameInput.value });
                }
            });
            callingObject.PlatformData.bios = biosItems;
            await fetch('/api/v1.1/PlatformMaps/' + callingObject.PlatformId, {
                method: 'PATCH', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(callingObject.PlatformData)
            }).then(async response => {
                if (response.ok) {
                    if (this.OKCallback) { this.OKCallback(); }
                    callingObject.dialog.close();
                } else {
                    new Dialog(window.lang ? window.lang.translate('generic.error') : 'Error', window.lang ? window.lang.translate('platforms.mapping.failed_save') : 'Failed to save platform data', window.lang ? window.lang.translate('generic.ok') : 'OK').open();
                }
            });
        });
        this.dialog.addButton(okButton);
        const cancelButton = new ModalButton(window.lang ? window.lang.translate('generic.cancel') : 'Cancel', 0, this, (callingObject) => {
            if (this.CancelCallback) { this.CancelCallback(); }
            callingObject.dialog.close();
        });
        this.dialog.addButton(cancelButton);
        this.dialog.open();
    }
}

class MappingBiosItem {
    constructor(Hash, Description, Filename) {
        this.Hash = Hash;
        this.Description = Description;
        this.Filename = Filename;
        this.Deleted = false;
        this.Item = document.createElement('div');
        this.Item.classList.add('biositem', 'romrow');
        this.HashInput = document.createElement('input');
        this.HashInput.type = 'text';
        this.HashInput.value = this.Hash;
        this.HashInput.classList.add('biosinput', 'bioshash');
        this.HashInput.placeholder = window.lang ? window.lang.translate('platforms.bios.placeholder.hash') : 'Hash';
        this.Item.appendChild(this.HashInput);
        this.DescriptionInput = document.createElement('input');
        this.DescriptionInput.type = 'text';
        this.DescriptionInput.value = this.Description;
        this.DescriptionInput.classList.add('biosinput', 'biosdescription');
        this.DescriptionInput.placeholder = window.lang ? window.lang.translate('platforms.bios.placeholder.description') : 'Description';
        this.Item.appendChild(this.DescriptionInput);
        this.FilenameInput = document.createElement('input');
        this.FilenameInput.type = 'text';
        this.FilenameInput.value = this.Filename;
        this.FilenameInput.classList.add('biosinput', 'biosfilename');
        this.FilenameInput.placeholder = window.lang ? window.lang.translate('platforms.bios.placeholder.filename') : 'Filename';
        this.Item.appendChild(this.FilenameInput);
        this.DeleteButton = document.createElement('a');
        this.DeleteButton.href = '#';
        this.DeleteButton.classList.add('biositemcontrol', 'biosdelete');
        this.DeleteButton.addEventListener('click', () => { this.Item.parentElement.removeChild(this.Item); this.Deleted = true; });
        this.DeleteImage = document.createElement('img');
        this.DeleteImage.src = '/images/delete.svg';
        this.DeleteImage.alt = window.lang ? window.lang.translate('generic.delete') : 'Delete';
        this.DeleteImage.title = window.lang ? window.lang.translate('generic.delete') : 'Delete';
        this.DeleteImage.classList.add('banner_button_image');
        this.DeleteButton.appendChild(this.DeleteImage);
        this.Item.appendChild(this.DeleteButton);
    }
}