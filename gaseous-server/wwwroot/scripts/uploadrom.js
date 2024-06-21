class UploadRom {
    constructor() {

    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("uploadrom");
        await this.dialog.BuildModal();

        // setup the dialog
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "Upload ROM";

        // set up the drop zone
        this.dropzone = new Dropzone("div#upload_target", {
            url: "/api/v1.1/Roms",
            autoProcessQueue: true,
            uploadMultiple: false,
            paramName: "file",
            maxFilesize: 60000,
            createImageThumbnails: false,
            disablePreviews: false
        });

        // show the dialog
        await this.dialog.open();
    }
}