class NewLibrary {
    constructor(parent, editLibraryId = 0) {
        this.parent = parent;
        this.editLibraryId = editLibraryId;
        this.editMode = editLibraryId > 0; // if editLibraryId is set, we are in edit mode
    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("newlibrary");
        await this.dialog.BuildModal();

        // override the dialog size
        this.dialog.modalElement.style = 'width: 600px; height: unset; min-width: unset; min-height: 215px; max-width: unset; max-height: unset;';

        // setup the dialog
        this.DialogName = this.dialog.modalElement.querySelector('#modal-header-text');
        this.DialogName.innerHTML = window.lang.translate('newlibrarymodal.dialog_title');

        // set up the library name field
        this.LibraryName = this.dialog.modalElement.querySelector('#librarynew_name');
        this.LibraryName.addEventListener('input', async () => {
            await this.validate();
        });

        // set up the library path field
        this.LibraryPath = this.dialog.modalElement.querySelector('#librarynew_path');
        this.LibraryPath.addEventListener('input', async () => {
            await this.validate();
        });

        // setup the path selector button
        this.pathSelector = this.dialog.modalElement.querySelector('#librarynew_pathSelect');
        this.pathSelector.addEventListener('click', async () => {
            let fileOpen = new FileOpen(
                async () => {
                    this.LibraryPath.value = fileOpen.SelectedPath;
                    await this.validate();
                },
                undefined,
                false
            );
            await fileOpen.open();
        });

        // setup the default platform drop down
        this.defaultPlatformSelector = this.dialog.modalElement.querySelector('#librarynew_defaultPlatformId');

        $(this.defaultPlatformSelector).select2({
            minimumInputLength: 3,
            placeholder: window.lang.translate('newlibrarymodal.default_platform_any_option'),
            allowClear: true,
            ajax: {
                url: '/api/v1.1/Search/Platform',
                data: function (params) {
                    var query = {
                        SearchString: params.term
                    }

                    // Query parameters will be ?SearchString=[term]
                    return query;
                },
                processResults: function (data) {
                    var arr = [];

                    for (var i = 0; i < data.length; i++) {
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

        $(this.defaultPlatformSelector).on("select2:select", async (e) => {
            // when a platform is selected, validate the dialog
            await this.validate();
        });

        // add ok button
        let okButton = new ModalButton(window.lang.translate('generic.ok'), 1, this, async (callingObject) => {
            if (await this.validate()) {
                // create the library
                let defaultPlatform = 0;
                let defaultPlatformSelector = $(this.defaultPlatformSelector).select2('data');
                if (defaultPlatformSelector.length > 0) {
                    defaultPlatform = defaultPlatformSelector[0].id;
                }

                let url = `/api/v1.1/Library?Name=${encodeURIComponent(this.LibraryName.value)}&DefaultPlatformId=${defaultPlatform}&Path=${encodeURIComponent(this.LibraryPath.value)}`;
                let urlMethod = 'POST';
                if (this.editMode) {
                    // if in edit mode, we need to update the library
                    url = `/api/v1.1/Library/${this.editLibraryId}?Name=${encodeURIComponent(this.LibraryName.value)}&DefaultPlatformId=${defaultPlatform}`;
                    urlMethod = 'PATCH';
                }

                // make the ajax call to create the library
                fetch(url,
                    {
                        method: urlMethod,
                        headers: {
                            'Content-Type': 'application/json'
                        }
                    })
                    .then(response => {
                        if (!response.ok) {
                            throw new Error(window.lang.translate('newlibrarymodal.error.network_response_prefix') + ' ' + response.statusText);
                        }
                        if (this.parent) {
                            // call the page drawLibrary function
                            this.parent.drawLibrary();
                        }

                        // close the dialog
                        this.dialog.close();
                    })
                    .catch(error => {
                        alert(window.lang.translate('newlibrarymodal.error.creation_prefix') + '\n\n' + error.message);
                    });
            }
        });
        this.dialog.addButton(okButton);

        // add cancel button
        let cancelButton = new ModalButton(window.lang.translate('generic.cancel'), 0, this, async function (callingObject) {
            callingObject.dialog.close();
        });
        this.dialog.addButton(cancelButton);

        // show the dialog
        this.dialog.open();

        // disable the ok button
        this.dialog.disableButton(window.lang.translate('generic.ok'));
    }

    async validate() {
        let valid = true;

        // check name
        if (this.LibraryName.value === "") {
            valid = false;
        }

        // check path
        if (this.LibraryPath.value === "") {
            valid = false;
        }

        if (valid) {
            this.dialog.enableButton(window.lang.translate('generic.ok'));
        } else {
            this.dialog.disableButton(window.lang.translate('generic.ok'));
        }

        return valid;
    }
}