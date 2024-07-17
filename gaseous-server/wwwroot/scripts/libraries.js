class NewLibrary {
    constructor() {

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
        let okButton = new ModalButton("OK", 1, this, async function (callingObject) {
            if (await callingObject.validate()) {
                // create the library
                let defaultPlatform = 0;
                let defaultPlatformSelector = $(callingObject.defaultPlatformSelector).select2('data');
                if (defaultPlatformSelector.length > 0) {
                    defaultPlatform = defaultPlatformSelector[0].id;
                }

                ajaxCall(
                    '/api/v1.1/Library?Name=' + encodeURIComponent(callingObject.LibraryName.value) + '&DefaultPlatformId=' + defaultPlatform + '&Path=' + encodeURIComponent(callingObject.LibraryPath.value),
                    'POST',
                    function (result) {
                        // call the page drawLibrary function
                        drawLibrary();

                        // close the dialog
                        callingObject.dialog.close();
                    },
                    function (error) {
                        alert('An error occurred while creating the library:\n\n' + JSON.stringify(error.responseText));
                    }
                );
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