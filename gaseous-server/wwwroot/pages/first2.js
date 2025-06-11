var wizardPages = {

}

var settings;
var settings_advanced;

async function SetupPage() {
    await fetch('/api/v1.1/System/Settings/System', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    }).then(response => {
        if (response.ok) {
            response.json().then(data => {
                settings = data;

                let pages = document.getElementsByName('wizard-page');
                for (let page of pages) {
                    page.style.display = 'none';

                    let pageNumber = Number(page.getAttribute('data-page'));
                    wizardPages[pageNumber] = false;

                    switch (pageNumber) {
                        case 0:
                            for (let radio of document.getElementsByName('login_signatures')) {
                                radio.addEventListener('change', function () {
                                    wizardPages[pageNumber] = true;
                                    EnableButtons(pageNumber);
                                });
                            }

                            break;

                        case 1:
                            for (let radio of document.getElementsByName('login_metadata')) {
                                radio.addEventListener('change', function () {
                                    wizardPages[pageNumber] = true;
                                    EnableButtons(pageNumber);
                                });
                            }

                            break;
                    }
                }
                pages[0].style.display = 'block';

                EnableButtons(0);

                let nextButton = document.getElementById('wizard-next');
                let prevButton = document.getElementById('wizard-prev');

                nextButton.addEventListener('click', function () {
                    NextPage();
                });

                prevButton.addEventListener('click', function () {
                    PrevPage();
                });

                let igdbAdvancedButton = document.getElementById('login_metadata_igdb_advanced');
                igdbAdvancedButton.addEventListener('click', async function () {
                    // show the advanced options modal

                    // Create the modal
                    this.dialog = await new Modal("first2-igdb-advanced");
                    await this.dialog.BuildModal();

                    // override the dialog size
                    this.dialog.modalElement.style = 'width: 90%; height: 250px; min-width: 350px; min-height: 215px; max-width: 440px; max-height: unset;';

                    // setup the dialog
                    this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "Advanced";

                    // populate the dialog
                    let useIGDBAPI = this.dialog.modalElement.querySelector('#igdb-advanced-useigdb');
                    let igdbAPIKey = this.dialog.modalElement.querySelector('#igdb-advanced-igdbclientid');
                    let igdbAPISecret = this.dialog.modalElement.querySelector('#igdb-advanced-igdbclientsecret');

                    if (settings.metadataSources != null) {
                        // get element with the IGDB source
                        let igdbSource = settings.metadataSources.find(source => source.source == 'IGDB');
                        if (igdbSource != null && igdbSource.clientId != null && igdbSource.clientId != '') {
                            useIGDBAPI.checked = true;
                            igdbAPIKey.value = igdbSource.clientId;
                            igdbAPISecret.value = igdbSource.secret;
                        }
                    }

                    // create the ok button
                    let okButton = new ModalButton("OK", 1, this, () => {
                        // get igdb source from settings
                        let igdbSource = settings.metadataSources.find(source => source.source == 'IGDB');
                        if (useIGDBAPI.checked === true) {
                            igdbSource.useHasheousProxy = false;
                        } else {
                            igdbSource.useHasheousProxy = true;
                        }
                        igdbSource.clientId = igdbAPIKey.value;
                        igdbSource.secret = igdbAPISecret.value;

                        // remove the source from the settings
                        settings.metadataSources = settings.metadataSources.filter(source => source.source != 'IGDB');

                        // add the source back to the settings
                        settings.metadataSources.push(igdbSource);
                        settings_advanced = igdbSource;

                        console.log(settings);
                        console.log(settings_advanced);
                        this.dialog.close();
                    });
                    this.dialog.addButton(okButton);

                    // create the cancel button
                    let cancelButton = new ModalButton("Cancel", 0, this, () => {
                        this.dialog.close();
                    });
                    this.dialog.addButton(cancelButton);

                    // show the dialog
                    await this.dialog.open();
                });
            });
        }
    });
}

function EnableButtons(pageNumber) {
    let pages = document.getElementsByName('wizard-page');
    let nextButton = document.getElementById('wizard-next');
    nextButton.style.width = '90px';
    let prevButton = document.getElementById('wizard-prev');
    prevButton.style.width = '90px';

    if (pageNumber == 0) {
        prevButton.disabled = true;
        nextButton.disabled = !wizardPages[pageNumber];

        nextButton.innerHTML = 'Next';
        nextButton.classList.remove('bluebutton');
    }

    if (pageNumber != 0) {
        prevButton.disabled = false;
        nextButton.disabled = !wizardPages[pageNumber];

        if (pageNumber == pages.length - 1) {
            nextButton.innerHTML = 'Finish';
            nextButton.classList.add('bluebutton');
        } else {
            nextButton.innerHTML = 'Next';
            nextButton.classList.remove('bluebutton');
        }

        prevButton.disabled = false;
        nextButton.disabled = !wizardPages[pageNumber];
    }
}

function NextPage() {
    let pages = document.getElementsByName('wizard-page');
    let currentPage = 0;
    for (let page of pages) {
        if (page.style.display == 'block') {
            currentPage = parseInt(page.getAttribute('data-page'));
            if (currentPage == pages.length - 1) {
                break;
            }
            page.style.display = 'none';
            break;
        }
    }

    if (currentPage + 1 < pages.length) {
        pages[currentPage + 1].style.display = 'block';
        EnableButtons(currentPage + 1);
    } else {
        // finish the wizard
        // signature source
        let sig_hasheous = document.getElementById('login_signatures_hasheous');
        let sig_none = document.getElementById('login_signatures_localonly');
        settings.signatureSource.source = sig_hasheous.checked ? 'Hasheous' : 'LocalOnly';

        // metadata source
        let meta_igdb = document.getElementById('login_metadata_igdb');
        let meta_thegamedb = document.getElementById('login_metadata_thegamesdb');
        let meta_none = document.getElementById('login_metadata_none');

        // set all sources to disabled
        settings.metadataSources.forEach(source => {
            source.default = false;
        });

        if (meta_igdb.checked) {
            settings.metadataSources.find(source => source.source == 'IGDB').default = true;
            if (settings_advanced) {
                settings.metadataSources.find(source => source.source == 'IGDB').useHasheousProxy = settings_advanced.useHasheousProxy;
                settings.metadataSources.find(source => source.source == 'IGDB').clientId = settings_advanced.clientId;
                settings.metadataSources.find(source => source.source == 'IGDB').secret = settings_advanced.secret;
            } else {
                settings.metadataSources.find(source => source.source == 'IGDB').useHasheousProxy = true;
            }
        } else if (meta_thegamedb.checked) {
            settings.metadataSources.find(source => source.source == 'TheGamesDB').default = true;
        } else if (meta_none.checked) {
            settings.metadataSources.find(source => source.source == 'None').default = true;
        }

        // apply the settings
        fetch('/api/v1.1/FirstSetup/1', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(settings)
        }).then(response => {
            if (response.ok) {
                window.location.replace('/index.html');
            } else {
                response.json().then(data => {
                    console.log(data);
                });
            }
        });
    }
}

function PrevPage() {
    let pages = document.getElementsByName('wizard-page');
    let currentPage = 0;
    for (let page of pages) {
        if (page.style.display == 'block') {
            currentPage = parseInt(page.getAttribute('data-page'));
            page.style.display = 'none';
            break;
        }
    }

    pages[currentPage - 1].style.display = 'block';
    EnableButtons(currentPage - 1);
}

SetupPage();