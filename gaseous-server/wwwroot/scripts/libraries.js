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

        // setup the path selector button
        let pathSelector = this.dialog.modalElement.querySelector('#librarynew_pathSelect');
        pathSelector.addEventListener('click', async () => {
            let fileOpen = new FileOpen(true);
            await fileOpen.open();
        });

        // setup the default platform drop down
        this.defaultPlatformSelector = this.dialog.modalElement.querySelector('#librarynew_defaultPlatformId');
        this.defaultPlatformSelector.innerHTML = '<option value="0">Any</option>';

        $(this.defaultPlatformSelector).select2({
            minimumInputLength: 3,
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

                    arr.push({
                        id: 0,
                        text: 'Any'
                    });

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

        // show the dialog
        this.dialog.open();
    }
}