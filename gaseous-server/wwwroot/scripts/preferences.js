class PreferencesWindow {
    constructor() {

    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("preferences");
        await this.dialog.BuildModal();

        // setup the dialog
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "Preferences";

        // set initial preference states
        let preferences = userProfile.userPreferences;
        for (const [key, value] of Object.entries(preferences)) {
            switch (value.setting) {
                default:
                    let settingElementSelector = '[data-pref="' + value.setting + '"]';
                    let settingElement = this.dialog.modalElement.querySelector(settingElementSelector);
                    if (settingElement) {
                        switch (settingElement.tagName) {
                            case "INPUT":
                                switch (settingElement.type) {
                                    case "checkbox":
                                        if (value.value == "true") {
                                            settingElement.checked = true;
                                        } else {
                                            settingElement.checked = false;
                                        }
                                        break;
                                }
                                break;
                            case "SELECT":
                                settingElement.value = value.value;
                                $(settingElement).select2();
                                break;
                        }
                    }
                    break;
            }
        }

        // populate the classification board listbox
        this.#populateClassificationList();

        // add a button to move an element up
        let moveUpButton = this.dialog.modalElement.querySelector('#classificationBoardSelector-Up');
        moveUpButton.addEventListener('click', () => {
            // get the selected classification
            let selectedClassification = this.classificationSelector.querySelector('.listboxselector-item-selected');
            if (selectedClassification) {
                // get the index of the selected classification
                let selectedIndex = Array.from(this.classificationSelector.children).indexOf(selectedClassification);
                // check if the selected classification is not the first element
                if (selectedIndex > 0) {
                    // swap the selected classification with the one above it
                    let temp = this.userClassifications[selectedIndex];
                    this.userClassifications[selectedIndex] = this.userClassifications[selectedIndex - 1];
                    this.userClassifications[selectedIndex - 1] = temp;
                    // rerun the populateClassificationList function
                    this.#populateClassificationList(selectedClassification.getAttribute("data-classification"));
                }
            }
        });

        // add a button to move an element down
        let moveDownButton = this.dialog.modalElement.querySelector('#classificationBoardSelector-Down');
        moveDownButton.addEventListener('click', () => {
            // get the selected classification
            let selectedClassification = this.classificationSelector.querySelector('.listboxselector-item-selected');
            if (selectedClassification) {
                // get the index of the selected classification
                let selectedIndex = Array.from(this.classificationSelector.children).indexOf(selectedClassification);
                // check if the selected classification is not the last element
                if (selectedIndex < this.userClassifications.length - 1) {
                    // swap the selected classification with the one below it
                    let temp = this.userClassifications[selectedIndex];
                    this.userClassifications[selectedIndex] = this.userClassifications[selectedIndex + 1];
                    this.userClassifications[selectedIndex + 1] = temp;
                    // rerun the populateClassificationList function
                    this.#populateClassificationList(selectedClassification.getAttribute("data-classification"));
                }
            }
        });

        // create the ok button
        let okButton = new ModalButton("OK", 1, this, function (callingObject) {
            // get the preferences
            let selectedPreferences = callingObject.dialog.modalElement.querySelectorAll('[data-pref]');
            let preferences = [];
            selectedPreferences.forEach((preference) => {
                let pref = {};
                pref.setting = preference.getAttribute('data-pref');
                switch (preference.tagName) {
                    case "INPUT":
                        switch (preference.type) {
                            case "checkbox":
                                pref.value = preference.checked.toString();
                                break;
                        }
                        break;
                    case "SELECT":
                        pref.value = preference.value.toString();
                        break;
                }
                preferences.push(pref);
            });

            // get the classification order
            let classificationOrder = [];
            for (let i = 0; i < callingObject.classificationSelector.children.length; i++) {
                classificationOrder.push(callingObject.classificationSelector.children[i].getAttribute("data-classification"));
            }
            preferences.push({ setting: "LibraryGameClassificationDisplayOrder", value: JSON.stringify(classificationOrder) });

            SetPreference_Batch(preferences, function () { window.location.reload(); }, function () { window.location.reload(); });
        });
        this.dialog.addButton(okButton);

        // create the cancel button
        let cancelButton = new ModalButton("Cancel", 0, this, function (callingObject) {
            callingObject.dialog.close();
        });
        this.dialog.addButton(cancelButton);

        // show the dialog
        await this.dialog.open();
    }

    #populateClassificationList(preSelectedClassification) {
        let classifications = [];
        if (!this.userClassifications) {
            this.userClassifications = GetRatingsBoards();
        }
        for (let i = 0; i < this.userClassifications.length; i++) {
            classifications[this.userClassifications[i]] = ClassificationBoards[this.userClassifications[i]];
        }
        this.classificationSelector = this.dialog.modalElement.querySelector('#classificationBoardSelector');
        this.classificationSelector.innerHTML = "";
        for (const [key, value] of Object.entries(classifications)) {
            let classificationItemBox = document.createElement('div');
            classificationItemBox.classList.add("listboxselector-item");
            classificationItemBox.setAttribute("data-classification", key);
            if (preSelectedClassification) {
                if (preSelectedClassification == key) {
                    classificationItemBox.setAttribute("data-selected", "true");
                    classificationItemBox.classList.add("listboxselector-item-selected");
                } else {
                    classificationItemBox.setAttribute("data-selected", "false");
                }
            } else {
                classificationItemBox.setAttribute("data-selected", "false");
            }
            classificationItemBox.setAttribute("name", "classificationBoardSelectorItem");
            classificationItemBox.addEventListener('click', (e) => {
                let rows = this.classificationSelector.querySelectorAll('[name="classificationBoardSelectorItem"]');
                for (let i = 0; i < rows.length; i++) {
                    rows[i].setAttribute("data-selected", "false");
                    rows[i].classList.remove("listboxselector-item-selected");
                }

                if (e.target.getAttribute("name") == "classificationBoardSelectorItem") {
                    e.target.setAttribute("data-selected", "true");
                    e.target.classList.add("listboxselector-item-selected");
                }
            });

            let classificationName = document.createElement('div');
            classificationName.classList.add("listboxselector-item-name");
            classificationName.innerHTML = value;
            classificationItemBox.appendChild(classificationName);

            let classificationIcons = document.createElement('div');
            classificationIcons.classList.add("listboxselector-item-icons");
            // loop the age rating groups
            let ratingGroupsOrder = [
                "Child",
                "Teen",
                "Mature",
                "Adult"
            ];
            for (let j = 0; j < ratingGroupsOrder.length; j++) {
                let ratingGroup = ratingGroupsOrder[j];
                let ageGroupValue = AgeRatingGroups[ratingGroup];
                let ageGroupValueLower = {};
                for (const [key, value] of Object.entries(ageGroupValue)) {
                    ageGroupValueLower[key.toLowerCase()] = value;
                }

                let iconIdList = ageGroupValueLower[key.toLowerCase()];
                console.log(key.toLowerCase());
                if (key == 'clasS_IND' || key == 'CLASS_IND') {
                    console.log("here");
                }
                // loop the age rating icons
                if (iconIdList) {
                    for (const [i, value] of Object.entries(iconIdList)) {
                        console.log("  " + iconIdList[i]);
                        let icon = document.createElement('img');

                        // get age rating strings
                        let iconId = iconIdList[i];
                        let ageRatingString;
                        for (const [x, y] of Object.entries(AgeRatingStrings)) {
                            if (AgeRatingStrings[x] == iconId) {
                                ageRatingString = AgeRatingStrings[x];
                                break;
                            }
                        }

                        icon.src = "/images/Ratings/" + key + "/" + ageRatingString + ".svg";
                        icon.title = ageRatingString;
                        icon.alt = ageRatingString;
                        icon.classList.add("rating_image_mini");
                        classificationIcons.appendChild(icon);
                    }
                }
            }
            classificationItemBox.appendChild(classificationIcons);

            this.classificationSelector.appendChild(classificationItemBox);
        }
    }
}