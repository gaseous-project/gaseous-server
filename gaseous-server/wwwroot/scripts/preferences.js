class PreferencesWindow {
    constructor() {

    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("preferences");
        await this.dialog.BuildModal();

        // setup the dialog
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "Preferences";

        // populate the classification board listbox
        this.populateClassificationList();



        // add a button to move an element up
        let moveUpButton = this.dialog.modalElement.querySelector('#classificationBoardSelector-Up');
        moveUpButton.innerHTML = 'Move Up';
        moveUpButton.addEventListener('click', () => {
            // get the selected classification
            let selectedClassification = classificationSelector.querySelector('.listboxselector-item-selected');
            if (selectedClassification) {
                // get the index of the selected classification
                let selectedIndex = Array.from(classificationSelector.children).indexOf(selectedClassification);
                // check if the selected classification is not the first element
                if (selectedIndex > 0) {
                    // swap the selected classification with the one above it
                    let temp = this.userClassifications[selectedIndex];
                    this.userClassifications[selectedIndex] = this.userClassifications[selectedIndex - 1];
                    this.userClassifications[selectedIndex - 1] = temp;
                    // rerun the populateClassificationList function
                    this.populateClassificationList();
                }
            }
        });





        // show the dialog
        await this.dialog.open();
    }

    populateClassificationList() {
        let classifications = [];
        if (!this.userClassifications) {
            this.userClassifications = GetRatingsBoards();
        }
        for (let i = 0; i < this.userClassifications.length; i++) {
            classifications[this.userClassifications[i]] = ClassificationBoards[this.userClassifications[i]];
        }
        let classificationSelector = this.dialog.modalElement.querySelector('#classificationBoardSelector');
        classificationSelector.innerHTML = "";
        for (const [key, value] of Object.entries(classifications)) {
            var classificationItemBox = document.createElement('div');
            classificationItemBox.classList.add("listboxselector-item");
            classificationItemBox.innerHTML = value;
            classificationItemBox.setAttribute("data-classification", key);
            classificationItemBox.setAttribute("data-selected", "false");
            classificationItemBox.name = "classificationBoardSelectorItem";
            classificationItemBox.addEventListener('click', function (callingObject) {
                for (let i = 0; i < classificationSelector.children.length; i++) {
                    classificationSelector.children[i].setAttribute("data-selected", "false");
                    classificationSelector.children[i].classList.remove("listboxselector-item-selected");
                }

                callingObject.target.setAttribute("data-selected", "true");
                callingObject.target.classList.add("listboxselector-item-selected");
            });
            classificationSelector.appendChild(classificationItemBox);
        }
    }
}