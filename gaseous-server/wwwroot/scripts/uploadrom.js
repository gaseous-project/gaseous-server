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
        let dropZone = this.dialog.modalElement.querySelector('#upload_target');
        let uploadedNoRomsLabel = this.dialog.modalElement.querySelector('#uploaded_roms_emptylabel');
        dropZone.classList.add('dragtarget');
        dropZone.addEventListener('click', async (e) => {
            e.preventDefault();
            let fileInput = document.createElement('input');
            fileInput.type = 'file';
            fileInput.multiple = true;
            fileInput.style.display = 'none';

            fileInput.addEventListener('change', async (e) => {
                this.#ProcessFiles(fileInput.files)
            });

            fileInput.click();
        });
        dropZone.addEventListener('drop', async (e) => {
            e.preventDefault();
            dropZone.classList.remove('dragover');
            let files = e.dataTransfer.files;

            uploadedNoRomsLabel.style.display = 'none';

            this.#ProcessFiles(files)
        });
        dropZone.addEventListener('dragover', (e) => {
            e.preventDefault();
            dropZone.classList.add('dragover');
        });
        dropZone.addEventListener('dragleave', (e) => {
            dropZone.classList.remove('dragover');
        });

        // show the dialog
        await this.dialog.open();
    }

    async #ProcessFiles(files) {
        let uploadList = this.dialog.modalElement.querySelector('#uploaded_roms');

        // load queue
        let uploadedItems = [];
        for (let i = 0; i < files.length; i++) {
            let file = files[i];

            let uploadedItem = new UploadItem(file.name, i);
            uploadedItems.push(uploadedItem);
            uploadList.prepend(uploadedItem.Item);
        }

        // process the queue
        for (let i = 0; i < files.length; i++) {
            // handle the file
            let file = files[i];
            let formData = new FormData();
            formData.append('file', file);

            let uploadedItem = uploadedItems[i];

            let xhr = new XMLHttpRequest();
            xhr.open('POST', '/api/v1.1/Roms');
            xhr.upload.addEventListener('progress', (e) => {
                if (e.lengthComputable) {
                    let percentCompleted = Math.round((e.loaded * 100) / e.total);
                    if (percentCompleted < 100) {
                        uploadedItem.SetStatus('unknown', 1, percentCompleted, percentCompleted + '%');
                    } else {
                        uploadedItem.SetStatus('unknown', 4, null);
                    }
                }
            });

            // handle max uploads
            this.UploadCount++;

            await new Promise(resolve => {
                if (this.UploadCount > this.MaxUploads) {
                    console.log('Max uploads reached, waiting for uploads to complete');
                    const interval = setInterval(() => {
                        if (this.UploadCount <= this.MaxUploads) {
                            clearInterval(interval);
                            resolve();
                        }
                    }, 1000);
                } else {
                    resolve();
                }
            });

            // begin the upload
            xhr.addEventListener('load', () => {
                // upload completed
                this.UploadCount--;

                if (xhr.status === 200) {
                    // process the results
                    let response = JSON.parse(xhr.responseText);
                    switch (response.type) {
                        case 'rom':
                            switch (response.status) {
                                case 'duplicate':
                                    uploadedItem.SetStatus(response.type, 5, null, 'Duplicate ROM');
                                    break;

                                default:
                                    uploadedItem.platformId = 0;
                                    uploadedItem.platformName = 'Unknown Platform';
                                    uploadedItem.gameId = 0;
                                    uploadedItem.gameName = 'Unknown Game';
                                    uploadedItem.romId = response.romid;

                                    if (response.game) {
                                        uploadedItem.gameId = response.game.id;
                                        uploadedItem.gameName = response.game.name;
                                        if (response.game.cover != null) {
                                            if (response.game.cover.id != null) {
                                                uploadedItem.coverId = response.game.cover.id;
                                            }
                                        }
                                    }

                                    if (response.platform) {
                                        uploadedItem.platformId = response.platform.id;
                                        uploadedItem.platformName = response.platform.name;
                                    }

                                    uploadedItem.SetStatus(response.type, 2, null);
                                    break;
                            }
                            break;

                        case 'bios':
                            switch (response.status) {
                                case 'duplicate':
                                    uploadedItem.SetStatus(response.type, 5, null, 'Duplicate BIOS');
                                    break;

                                default:
                                    uploadedItem.platformId = 0;
                                    uploadedItem.platformName = 'Unknown Platform';

                                    if (response.platform) {
                                        uploadedItem.platformId = response.platform.id;
                                        uploadedItem.platformName = response.platform.name;
                                    }

                                    uploadedItem.gameName = response.name;

                                    uploadedItem.SetStatus(response.type, 2, null);
                                    break;
                            }

                            break;
                    }
                }
            });
            xhr.send(formData);
        }
    }

    MaxUploads = 2;
    UploadCount = 0;
}

class UploadItem {
    constructor(Filename, Index) {
        this.Filename = Filename;
        this.Status = 0;
        this.Type = 'unknown';
        this.Progress = null;

        // create the item
        this.Item = document.createElement('div');
        this.Item.id = 'uploaditem_' + Index;
        this.Item.classList.add('uploaditem');
        this.Item.classList.add('romrow');

        // rom cover art
        this.coverArt = document.createElement('img');
        this.coverArt.classList.add('uploadItem-CoverArt');
        this.coverArt.src = '';

        // file name label
        this.filenameLabel = document.createElement('div');
        this.filenameLabel.classList.add('uploadItem-Label');
        this.filenameLabel.innerHTML = this.Filename;

        // status label
        this.statusLabel = document.createElement('div');
        this.statusLabel.classList.add('uploadItem-Status');
        this.statusLabel.innerHTML = UploadItem.StatusValues[this.Status];

        // game name label
        this.gameNameLabel = document.createElement('div');
        this.gameNameLabel.classList.add('uploadItem-Status');
        this.gameNameLabel.innerHTML = '';
        this.gameNameLabel.style.display = 'none';

        // game platform label
        this.gamePlatformLabel = document.createElement('div');
        this.gamePlatformLabel.classList.add('uploadItem-Status');
        this.gamePlatformLabel.innerHTML = '';
        this.gamePlatformLabel.style.display = 'none';

        // rom info button
        this.infoButton = document.createElement('div');
        this.infoButton.classList.add('properties_button');
        this.infoButton.style.float = 'right';
        this.infoButton.style.display = 'none';
        this.infoButton.innerHTML = 'i';
        this.infoButton.addEventListener('click', () => {
            const romInfoDialog = new rominfodialog(this.gameId, this.romId);
            romInfoDialog.CallbackOk = (rom) => {
                this.platformId = rom.platformId;
                this.platformName = rom.platform;
                this.gameId = rom.gameId;
                this.gameName = rom.game;
                this.#RenderStatus();
            };
            romInfoDialog.CallbackDelete = () => {
                this.Item.remove();
                this.Item = null;
            };
            romInfoDialog.open();
        });

        // flex box
        let flexBox = document.createElement('div');
        flexBox.classList.add('uploadItem-FlexBox');
        this.Item.appendChild(flexBox);

        // add the elements to the item
        let leftColumn = document.createElement('div');
        leftColumn.classList.add('uploadItem-LeftColumn');
        leftColumn.appendChild(this.coverArt);
        flexBox.appendChild(leftColumn);

        let rightColumn = document.createElement('div');
        rightColumn.classList.add('uploadItem-RightColumn');
        rightColumn.appendChild(this.infoButton);
        rightColumn.appendChild(this.filenameLabel);
        rightColumn.appendChild(this.statusLabel);
        rightColumn.appendChild(this.gameNameLabel);
        rightColumn.appendChild(this.gamePlatformLabel);
        flexBox.appendChild(rightColumn);

        // progress bar
        this.progressBar = document.createElement('progress');
        this.progressBar.classList.add('uploadItem-Progress');
        this.Item.appendChild(this.progressBar);

        // render the item
        this.#RenderStatus();

        return this;
    }

    platformId = null;
    platformName = null;
    gameId = null;
    gameName = null;
    coverId = null;
    romId = null;

    SetStatus(Type, Status, Progress, Message = null) {
        this.Type = Type;
        this.Status = Status;
        this.Progress = Progress;
        this.Message = Message;

        this.#RenderStatus();
    }

    #RenderStatus() {
        this.statusLabel.innerHTML = UploadItem.StatusValues[this.Status] + (this.Message ? ' - ' + this.Message : '');
        this.progressBar.classList.remove('uploaditemprogressinprogress');
        this.progressBar.classList.remove('uploaditemprogressincomplete');
        this.progressBar.classList.remove('uploaditemprogresswarning');
        this.progressBar.classList.remove('uploaditemprogressfailed');
        this.coverArt.classList.remove('svginvert');
        switch (this.Status) {
            case 0: // Pending
                this.progressBar.removeAttribute('value');
                this.progressBar.removeAttribute('max');
                this.progressBar.classList.add('uploaditemprogressinprogress');
                this.coverArt.classList.add('svginvert');
                this.coverArt.src = '/images/pending.svg';
                break;
            case 1: // Uploading
                this.progressBar.value = this.Progress;
                this.progressBar.max = 100;
                this.progressBar.classList.add('uploaditemprogressinprogress');
                this.coverArt.classList.add('svginvert');
                this.coverArt.src = '/images/upload.svg';
                break;
            case 2: // Complete
                this.progressBar.value = 100;
                this.progressBar.max = 100;
                this.progressBar.classList.add('uploaditemprogressincomplete');
                this.statusLabel.style.display = 'none';
                this.gameNameLabel.style.display = 'block';
                this.gamePlatformLabel.style.display = 'block';

                switch (this.Type) {
                    case 'rom':
                        this.infoButton.style.display = 'block';

                        if (this.gameId === null || this.gameId === 0) {
                            this.coverArt.src = '/images/unknowngame.png';
                        } else {
                            this.coverArt.src = '/api/v1.1/Games/' + this.gameId + '/cover/' + this.coverId + '/image/cover_big/cover.jpg';
                        }

                        this.gamePlatformLabel.innerHTML = this.platformName;
                        break;

                    case 'bios':
                        this.coverArt.src = '/images/bios.svg';

                        this.gamePlatformLabel.innerHTML = "BIOS for platform: " + this.platformName;
                        break;

                    case 'unknown':
                        this.coverArt.src = '/images/unknowngame.png';
                        break;
                }

                this.gameNameLabel.innerHTML = this.gameName;
                break;
            case 3: // Failed
                this.progressBar.value = 100;
                this.progressBar.max = 100;
                this.progressBar.classList.add('uploaditemprogressfailed');
                this.coverArt.classList.add('svginvert');
                this.coverArt.src = '/images/Critical.svg';
                break;
            case 4: // Processing
                this.progressBar.removeAttribute('value');
                this.progressBar.removeAttribute('max');
                this.progressBar.classList.add('uploaditemprogressinprogress');
                this.coverArt.classList.add('svginvert');
                this.coverArt.src = '/images/processing.svg';
                break;
            case 5: // Error
                this.progressBar.value = 100;
                this.progressBar.max = 100;
                this.progressBar.classList.add('uploaditemprogresswarning');
                this.coverArt.classList.add('svginvert');
                this.coverArt.src = '/images/Warning.svg';
                break;
        }
    }

    static StatusValues = {
        0: "Pending",
        1: "Uploading",
        2: "Complete",
        3: "Failed",
        4: "Processing",
        5: "Error"
    };
}