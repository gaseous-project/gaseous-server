class Modal {
    constructor(contentUrl, value) {
        this.contentUrl = contentUrl;
        this.value = value;
        this.modalBackground = null;
    }

    async open(value) {
        if (value) {
            this.value = value;
        }

        document.body.style.overflow = 'hidden';

        // Create the modal background
        this.modalBackground = document.createElement('div');
        this.modalBackground.classList.add('modal-background');

        // Create the modal element
        this.modalElement = document.createElement('div');
        this.modalElement.classList.add('modal-window');

        // Load the modal template
        const templateResponse = await fetch('/pages/modals/modal.html');
        const templateContent = await templateResponse.text();

        // Load the content from the HTML file
        const response = await fetch("/pages/modals/" + this.contentUrl + ".html");
        const content = await response.text();

        // Get the modal javascript
        fetch("/pages/modals/" + this.contentUrl + ".js", { method: "HEAD" }
        ).then((res) => {
            if (res.ok) {
                // file is present at URL
                this.modalScript = document.createElement('script');
                this.modalScript.src = "/pages/modals/" + this.contentUrl + ".js";
                this.modalBackground.appendChild(this.modalScript);
            } else {
                // file is not present at URL
            }
        });

        // Set the content of the modal
        this.modalElement.innerHTML = templateContent;
        this.modalElement.querySelector('#modal-window-content').innerHTML = content;

        // Generate tabs
        const tabcontainer = this.modalElement.querySelector('#modal-tabs');
        const tabs = this.modalElement.querySelectorAll('[name="modalTab"]');
        let firstTab = true;
        tabs.forEach((tab) => {
            let newTab = document.createElement('div');
            newTab.id = 'tab-' + tab.id;
            newTab.classList.add('modal-tab-button');
            newTab.setAttribute('data-tabid', tab.id);
            newTab.innerHTML = tab.getAttribute('data-tabname');
            newTab.addEventListener('click', () => {
                tabs.forEach((tab) => {
                    if (tab.getAttribute('id') !== newTab.getAttribute('data-tabid')) {
                        tab.style.display = 'none';
                        tabcontainer.querySelector('[data-tabid="' + tab.id + '"]').classList.remove('model-tab-button-selected');
                    } else {
                        tab.style.display = 'block';
                        tabcontainer.querySelector('[data-tabid="' + tab.id + '"]').classList.add('model-tab-button-selected');
                    }
                });
            });
            if (firstTab) {
                newTab.classList.add('model-tab-button-selected');
                tab.style.display = 'block';
                firstTab = false;
            } else {
                tab.style.display = 'none';
            }
            tabcontainer.appendChild(newTab);
        });

        // add the window to the modal background
        this.modalBackground.appendChild(this.modalElement);

        // Append the modal element to the document body
        document.body.appendChild(this.modalBackground);

        // Add event listener to close the modal when the close button is clicked
        this.modalElement.querySelector('#modal-close-button').addEventListener('click', () => {
            this.close();
        });

        // Add event listener to close the modal when clicked outside
        this.modalBackground.addEventListener('click', (event) => {
            if (event.target === this.modalBackground) {
                this.close();
            }
        });
    }

    close() {
        // Remove the modal element from the document body
        if (this.modalBackground) {
            this.modalBackground.remove();
            this.modalBackground = null;
        }

        if (document.getElementsByClassName('modal-window-body').length === 0) {
            document.body.style.overflow = 'auto';
        }
    }
}