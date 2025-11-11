class PreferencesWindow {
    constructor() {

    }

    OkCallbacks = [];
    CancelCallbacks = [];
    ErrorCallbacks = [];

    async open() {
        // Create the modal
        this.dialog = await new Modal("preferences");
        await this.dialog.BuildModal();

        // load language list
        this.LanguageList = await fetch('/api/v1.1/Localisation/available-languages')
            .then(async response => {
                if (response.ok) {
                    return await response.json();
                } else {
                    throw new Error(window.lang ? window.lang.translate('preferences.error.failed_load_language_list') : 'Failed to load language list');
                }
            })
            .catch(error => {
                console.error(error);
                return [];
            });

        // load age rating mappings
        this.AgeRatingMappings = await fetch('/images/Ratings/AgeGroupMap.json')
            .then(async response => {
                if (response.ok) {
                    return await response.json();
                } else {
                    throw new Error(window.lang ? window.lang.translate('preferences.error.failed_load_age_rating_mappings') : 'Failed to load age rating mappings');
                }
            })
            .catch(error => {
                console.error(error);
                return {};
            });

        // setup the dialog
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = window.lang ? window.lang.translate('preferences.modal.title') : 'Preferences';

        // set initial preference states
        let preferences = GetPreferences();
        for (const [key, value] of Object.entries(preferences)) {
            switch (key) {
                default:
                    let settingElementSelector = '[data-pref="' + key + '"]';
                    let settingElement = this.dialog.modalElement.querySelector(settingElementSelector);
                    if (settingElement) {
                        switch (settingElement.tagName) {
                            case "INPUT":
                                switch (settingElement.type) {
                                    case "checkbox":
                                        if (value === "true" || value === true) {
                                            settingElement.checked = true;
                                        } else {
                                            settingElement.checked = false;
                                        }
                                        break;
                                }
                                break;
                            case "SELECT":
                                settingElement.value = value;
                                $(settingElement).select2();
                                break;
                        }
                    }
                    break;
            }
        }

        // populate the language listbox
        this.#populateLanguageList();

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
        let okButton = new ModalButton(window.lang ? window.lang.translate('generic.ok') : 'OK', 1, this, function (callingObject) {
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
                        pref.value = JSON.stringify(preference.value.toString());
                        break;
                }
                if (preference.getAttribute('data-uselocalstore') === "1") {
                    // apply directly to local storage
                    localStorage.setItem(pref.setting, pref.value);
                    if (pref.setting === "Language.Selected") {
                        // update the language immediately
                        window.lang.setLocale(JSON.parse(pref.value));
                    }
                } else {
                    preferences.push(pref);
                }
            });

            // get the classification order
            let classificationOrder = [];
            for (const child of callingObject.classificationSelector.children) {
                classificationOrder.push(child.getAttribute("data-classification"));
            }
            preferences.push({ setting: "Library.GameClassificationDisplayOrder", value: JSON.stringify(classificationOrder) });

            SetPreference_Batch(preferences, callingObject, function (callingObject) {
                console.log(callingObject);
                if (callingObject.OkCallbacks) {
                    for (const callback of callingObject.OkCallbacks) {
                        callback();
                    }
                }

                callingObject.dialog.close();
            }, () => {
                if (callingObject.ErrorCallbacks) {
                    for (const callback of callingObject.ErrorCallbacks) {
                        callback();
                    }
                }

                callingObject.dialog.close();
            });
        });
        this.dialog.addButton(okButton);

        // create the cancel button
        let cancelButton = new ModalButton(window.lang ? window.lang.translate('generic.cancel') : 'Cancel', 0, this, function (callingObject) {
            if (callingObject.CancelCallbacks) {
                for (const callback of callingObject.CancelCallbacks) {
                    callback();
                }
            }

            callingObject.dialog.close();
        });
        this.dialog.addButton(cancelButton);

        // show the dialog
        await this.dialog.open();
    }

    #populateLanguageList() {
        // populate language selector
        let languageSelector = this.dialog.modalElement.querySelector('[data-pref="Language.Selected"]');
        for (const [key, value] of Object.entries(this.LanguageList)) {
            let option = document.createElement('option');
            option.value = key;
            option.text = value;
            if (key == window.lang?.locale) {
                option.selected = true;
            }
            languageSelector.appendChild(option);
        }
        $(languageSelector).select2();
    }

    #populateClassificationList(preSelectedClassification) {
        let classifications = [];
        if (!this.userClassifications) {
            this.userClassifications = GetRatingsBoards();
        }

        for (const classification of this.userClassifications) {
            classifications[classification] = this.AgeRatingMappings.RatingBoards[classification];
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
                for (const row of rows) {
                    row.setAttribute("data-selected", "false");
                    row.classList.remove("listboxselector-item-selected");
                }

                if (e.target.getAttribute("name") == "classificationBoardSelectorItem") {
                    e.target.setAttribute("data-selected", "true");
                    e.target.classList.add("listboxselector-item-selected");
                }
            });

            let classificationName = document.createElement('div');
            classificationName.classList.add("listboxselector-item-name");
            classificationName.innerHTML = value.Name;
            classificationItemBox.appendChild(classificationName);

            let classificationIcons = document.createElement('div');
            classificationIcons.classList.add("listboxselector-item-icons");
            // loop the age rating groups
            for (const ratingGroup of [
                "Child",
                "Teen",
                "Mature",
                "Adult"
            ]) {
                let ageGroupValue = this.AgeRatingMappings.AgeGroups[ratingGroup];
                let ageGroupRatingGroup = ageGroupValue.Ratings[key];

                let ratingBoard = this.AgeRatingMappings.RatingBoards[key];

                for (const groupRating of ageGroupRatingGroup) {
                    if (ratingBoard.Ratings[groupRating]) {
                        // generate icon
                        let icon = document.createElement('img');
                        icon.src = "/images/Ratings/" + key + "/" + ratingBoard.Ratings[groupRating].IconName + ".svg";
                        icon.title = ratingBoard.Ratings[groupRating].Name;
                        icon.alt = ratingBoard.Ratings[groupRating].Name;
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