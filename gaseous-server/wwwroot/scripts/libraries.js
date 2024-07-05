class NewLibrary {
    constructor() {

    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("newlibrary");
        await this.dialog.BuildModal();

        // setup the dialog
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "New Library";

        // show the dialog
        this.dialog.open();
    }
}