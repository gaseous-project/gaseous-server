class NewLibrary {
    constructor(parent) {
        this.parent = parent;
    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("newlibrary");
        await this.dialog.BuildModal();

        // override the dialog size
        this.dialog.modalElement.style = 'width: 600px; height: unset; min-width: unset; min-height: 215px; max-width: unset; max-height: unset;';

        // setup the dialog
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "New Library";

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
        let pathSelector = this.dialog.modalElement.querySelector('#librarynew_pathSelect');
        pathSelector.addEventListener('click', async () => {
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
            placeholder: 'Any',
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

        // add ok button
        let okButton = new ModalButton("OK", 1, this, async (callingObject) => {
            if (await this.validate()) {
                // create the library
                let defaultPlatform = 0;
                let defaultPlatformSelector = $(this.defaultPlatformSelector).select2('data');
                if (defaultPlatformSelector.length > 0) {
                    defaultPlatform = defaultPlatformSelector[0].id;
                }

                // make the ajax call to create the library
                fetch('/api/v1.1/Library?Name=' + encodeURIComponent(this.LibraryName.value) + '&DefaultPlatformId=' + defaultPlatform + '&Path=' + encodeURIComponent(this.LibraryPath.value),
                    {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        }
                    })
                    .then(response => {
                        if (!response.ok) {
                            throw new Error('Network response was not ok: ' + response.statusText);
                        }
                        if (this.parent) {
                            // call the page drawLibrary function
                            this.parent.drawLibrary();
                        }

                        // close the dialog
                        this.dialog.close();
                    })
                    .catch(error => {
                        alert('An error occurred while creating the library:\n\n' + error.message);
                    });
            }
        });
        this.dialog.addButton(okButton);

        // add cancel button
        let cancelButton = new ModalButton("Cancel", 0, this, async function (callingObject) {
            callingObject.dialog.close();
        });
        this.dialog.addButton(cancelButton);

        // show the dialog
        this.dialog.open();

        // disable the ok button
        this.dialog.disableButton("OK");
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
            this.dialog.enableButton("OK");
        } else {
            this.dialog.disableButton("OK");
        }

        return valid;
    }
}