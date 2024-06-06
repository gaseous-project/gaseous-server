class rominfodialog {
    constructor(gameId, romId) {
        this.gameId = gameId;
        this.romId = romId;
    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("rominfo");

        // Load the rom information
        let isDeleteable = true;
        await this.#fetchData(this, function (callingObject, data) {
            console.log(data);
            // populate the dialog with the rom information
            callingObject.dialog.modalElement.querySelector('#modal-header-text').innerHTML = data.name;
            callingObject.dialog.modalElement.querySelector('#rominfo_library').innerHTML = data.library.name;
            callingObject.dialog.modalElement.querySelector('#rominfo_platform').innerHTML = data.platform;
            callingObject.dialog.modalElement.querySelector('#rominfo_size').innerHTML = formatBytes(data.size, 2);
            callingObject.dialog.modalElement.querySelector('#rominfo_type').innerHTML = rominfodialog.getRomType(data.romType);
            callingObject.dialog.modalElement.querySelector('#rominfo_mediatype').innerHTML = data.romTypeMedia;
            callingObject.dialog.modalElement.querySelector('#rominfo_medialabel').innerHTML = data.mediaLabel;
            callingObject.dialog.modalElement.querySelector('#rominfo_md5').innerHTML = data.md5;
            callingObject.dialog.modalElement.querySelector('#rominfo_sha1').innerHTML = data.sha1;
            callingObject.dialog.modalElement.querySelector('#rominfo_signaturematch').innerHTML = data.signatureSource;
            callingObject.dialog.modalElement.querySelector('#rominfo_signaturetitle').innerHTML = data.signatureSourceGameTitle;

            if (data.platformId == 0) {
                callingObject.dialog.modalElement.querySelector('#rominfo_metadata_match_none').checked = true;
            } else {
                callingObject.dialog.modalElement.querySelector('#rominfo_metadata_match_match').checked = true;
                callingObject.dialog.modalElement.querySelector('#properties_fixplatform').innerHTML = "<option value='" + data.platformId + "' selected='selected'>" + data.platform + "</option>";
                if (data.gameId != 0) {
                    callingObject.dialog.modalElement.querySelector('#properties_fixgame').innerHTML = "<option value='" + data.gameid + "' selected='selected'>" + data.game + "</option>";
                }
            }

            if (data.library.isDefaultLibrary == false) {
                isDeleteable = false;
            }

            // populate the attributes field, and zip tab
            let attributesTable = document.createElement('table');
            let zipTable = document.createElement('table');
            zipTable.setAttribute('cellspacing', 0);
            zipTable.style.width = '100%';
            if (data.attributes) {
                if (!data.attributes.hasOwnProperty("ZipContents")) {
                    callingObject.dialog.removeTab("tab2");
                }
                for (const [key, value] of Object.entries(data.attributes)) {
                    let row = attributesTable.insertRow();
                    switch (key) {
                        case "ZipContents":
                            let zipRow = zipTable.insertRow();
                            zipRow.classList.add("romrow");
                            let zipCell1 = document.createElement('th');
                            zipCell1.classList.add("romcell");
                            zipCell1.innerHTML = "File Name";
                            zipRow.appendChild(zipCell1);
                            let zipCell2 = document.createElement('th');
                            zipCell2.classList.add("romcell");
                            zipCell2.innerHTML = "Size";
                            zipRow.appendChild(zipCell2);

                            let zipContents = JSON.parse(value);
                            for (let i = 0; i < zipContents.length; i++) {
                                let zipBody = zipTable.createTBody();
                                zipBody.classList.add("romrow");

                                // file name and size row
                                let zipRow = zipBody.insertRow();
                                let zipCell1 = zipRow.insertCell();
                                zipCell1.classList.add("romcell");
                                zipCell1.innerHTML = zipContents[i].FilePath + '/' + zipContents[i].FileName;
                                let zipCell2 = zipRow.insertCell();
                                zipCell2.classList.add("romcell");
                                zipCell2.innerHTML = formatBytes(zipContents[i].Size, 2);

                                // hash values
                                let hashRow = zipBody.insertRow();
                                let hashCell1 = hashRow.insertCell();
                                hashCell1.classList.add("romcell");
                                hashCell1.innerHTML = "MD5: " + zipContents[i].MD5 + "<br>SHA1: " + zipContents[i].SHA1;
                                hashCell1.colSpan = 2;
                                hashCell1.setAttribute('style', 'padding-left: 20px;');

                                // signature selector
                                if (zipContents[i].isSignatureSelector == true) {
                                    let selectorRow = zipBody.insertRow();
                                    let selectorCell1 = selectorRow.insertCell();
                                    selectorCell1.classList.add("romcell");
                                    selectorCell1.innerHTML = "This hash was used to match this archive";
                                    selectorCell1.colSpan = 2;
                                    selectorCell1.setAttribute('style', 'padding-left: 20px;');
                                }

                            }
                            break;
                        default:
                            let cell1 = row.insertCell();
                            cell1.innerHTML = rominfodialog.convertTOSECAttributeName(key);
                            let cell2 = row.insertCell();
                            cell2.innerHTML = value;
                            break;
                    }
                }
            }

            callingObject.dialog.modalElement.querySelector('#rominfo_signatureattributes').appendChild(attributesTable);
            callingObject.dialog.modalElement.querySelector('#tab2').appendChild(zipTable);
        });

        // setup the fix match tab
        this.setFixPlatformDropDown(this);
        this.setFixGameDropDown(this);

        // create the delete button
        if (isDeleteable == true) {
            let deleteButton = new ModalButton("Delete", true, this.dialog, function (callingObject) {
                callingObject.close();
            });
            this.dialog.addButton(deleteButton);
        }

        // create the ok button
        let okButton = new ModalButton("OK", false, this.dialog, function (callingObject) {
            callingObject.close();
        });
        this.dialog.addButton(okButton);

        // create the cancel button
        let cancelButton = new ModalButton("Cancel", false, this.dialog, function (callingObject) {
            callingObject.close();
        });
        this.dialog.addButton(cancelButton);

        // show the dialog
        await this.dialog.open();
    }

    async #fetchData(callingObject, callback) {
        const response = await fetch("/api/v1.1/Games/" + this.gameId + "/roms/" + this.romId);
        const data = await response.json();

        callback(callingObject, data);
    }

    static getRomType(typeId) {
        switch (typeId) {
            case 1:
                return "Optical media";
            case 2:
                return "Magnetic media";
            case 3:
                return "Individual files";
            case 4:
                return "Individual pars";
            case 5:
                return "Tape based media";
            case 6:
                return "Side of the media";
            case 0:
            default:
                return "Media type is unknown";
        }
    }

    static convertTOSECAttributeName(attributeName) {
        let tosecAttributeNames = {
            "cr": "Cracked",
            "f": "Fixed",
            "h": "Hacked",
            "m": "Modified",
            "p": "Pirated",
            "t": "Trained",
            "tr": "Translated",
            "o": "Over Dump",
            "u": "Under Dump",
            "v": "Virus",
            "b": "Bad Dump",
            "a": "Alternate",
            "!": "Known Verified Dump"
        };

        if (attributeName in tosecAttributeNames) {
            return tosecAttributeNames[attributeName];
        } else {
            return attributeName;
        }
    }

    setFixPlatformDropDown(callingObject) {
        $('#properties_fixplatform').select2({
            minimumInputLength: 3,
            placeholder: 'Platform',
            allowClear: true,
            ajax: {
                url: '/api/v1.1/Search/Platform',
                data: function (params) {
                    let query = {
                        SearchString: params.term
                    }

                    // Query parameters will be ?SearchString=[term]
                    return query;
                },
                processResults: function (data) {
                    let arr = [];

                    for (let i = 0; i < data.length; i++) {
                        arr.push({
                            id: data[i].id,
                            text: data[i].name
                        });
                    }

                    return {
                        results: arr
                    };

                }
            }
        });

        $('#properties_fixplatform').on('select2:select', function (e) {
            let platformData = e.params.data;

            let gameValue = $('#properties_fixgame').empty().select2('data');
            if (gameValue) {
                callingObject.setFixGameDropDown(callingObject);
            }
        });
    }

    setFixGameDropDown(callingObject) {
        $('#properties_fixgame').select2({
            minimumInputLength: 3,
            placeholder: 'Game',
            allowClear: true,
            templateResult: DropDownRenderGameOption,
            ajax: {
                url: '/api/v1.1/Search/Game',
                data: function (params) {
                    let fixplatform = $('#properties_fixplatform').select2('data');

                    let query = {
                        PlatformId: fixplatform[0].id,
                        SearchString: params.term
                    }

                    // Query parameters will be ?SearchString=[term]
                    return query;
                },
                processResults: function (data) {
                    let arr = [];

                    for (let i = 0; i < data.length; i++) {
                        arr.push({
                            id: data[i].id,
                            text: data[i].name,
                            cover: data[i].cover,
                            releaseDate: data[i].firstReleaseDate
                        });
                    }

                    return {
                        results: arr
                    };
                }
            }
        });
    }
}