function SetupPage() {
    // set up new user button
    let newUserButton = document.getElementById('settings_users_new');
    newUserButton.addEventListener('click', function () {
        let userNew = new UserNew();
        userNew.open();
    });
}

function GetUsers() {
    let targetDiv = document.getElementById('settings_users_table_container');
    targetDiv.innerHTML = '';

    ajaxCall(
        '/api/v1.1/Account/Users',
        'GET',
        function (result) {
            let newTable = document.createElement('table');
            newTable.className = 'romtable';
            newTable.style.width = '100%';
            newTable.cellSpacing = 0;

            newTable.appendChild(
                createTableRow(
                    true,
                    [
                        '',
                        'Email',
                        'Role',
                        'Age Restriction',
                        ''
                    ],
                    '',
                    ''
                )
            );

            for (let i = 0; i < result.length; i++) {
                let userAvatar = new Avatar(result[i].profileId, 32, 32, true);
                userAvatar.classList.add("user_list_icon");

                let roleDiv = document.createElement('div');

                let roleItem = CreateBadge(result[i].highestRole);
                roleDiv.appendChild(roleItem);

                let ageRestrictionPolicyDescription = document.createElement('div');
                if (result[i].securityProfile != null) {
                    if (result[i].securityProfile.ageRestrictionPolicy != null) {
                        let IncludeUnratedText = '';
                        if (result[i].securityProfile.ageRestrictionPolicy.includeUnrated == true) {
                            IncludeUnratedText = " &#43; Unclassified titles";
                        }

                        let restrictionText = result[i].securityProfile.ageRestrictionPolicy.maximumAgeRestriction + IncludeUnratedText;

                        ageRestrictionPolicyDescription = CreateBadge(restrictionText);
                    }
                }

                let editButton = '';

                let deleteButton = '';

                if (userProfile.userId != result[i].id) {
                    editButton = '<a href="#" onclick="showDialog(\'settingsuseredit\', \'' + result[i].id + '\');" class="romlink"><img src="/images/edit.svg" class="banner_button_image" alt="Edit" title="Edit" /></a>';

                    deleteButton = '<a href="#" onclick="showSubDialog(\'settingsuserdelete\', \'' + result[i].id + '\');" class="romlink"><img src="/images/delete.svg" class="banner_button_image" alt="Delete" title="Delete" /></a>';
                }

                newTable.appendChild(
                    createTableRow(
                        false,
                        [
                            userAvatar,
                            result[i].emailAddress,
                            roleDiv,
                            ageRestrictionPolicyDescription,
                            '<div style="text-align: right;">' + editButton + deleteButton + '</div>'
                        ],
                        'romrow',
                        'romcell'
                    )
                );
            }

            targetDiv.appendChild(newTable);
        }
    );
}

class UserNew {
    constructor() {

    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("usernew");
        await this.dialog.BuildModal();

        // setup the dialog
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "New User";
        this.dialog.modalElement.style = 'width: 550px; height: 360px; min-width: unset; min-height: unset; max-width: unset; max-height: unset;';

        // setup email check
        this.email = this.dialog.modalElement.querySelector('#email-address');
        this.email_error = this.dialog.modalElement.querySelector('#email-error');
        this.EmailCheck = new EmailCheck(this.email, this.email_error);

        // setup password check
        this.password_new = this.dialog.modalElement.querySelector('#new-password');
        this.password_confirm = this.dialog.modalElement.querySelector('#confirm-new-password');
        this.password_error = this.dialog.modalElement.querySelector('#password-error');
        this.PasswordCheck = new PasswordCheck(this.password_new, this.password_confirm, this.password_error);

        // add the ok button
        let okButton = new ModalButton("OK", 1, this, async function (callingObject) {
            if (!await EmailCheck.CheckEmail(callingObject.EmailCheck, callingObject.email)) {
                // display an error
                let warningDialog = new MessageBox("New User Error", "Invalid email address. Please correct the errors before continuing.");
                warningDialog.open();
                return;
            }

            if (!PasswordCheck.CheckPasswords(callingObject.PasswordCheck, callingObject.password_new, callingObject.password_confirm)) {
                // display an error
                let warningDialog = new MessageBox("New User Error", "The password doesn't meet the requirements. Please correct the errors before continuing.");
                warningDialog.open();
                return;
            }

            // create the new user
            var model = {
                "userName": callingObject.email.value,
                "email": callingObject.email.value,
                "password": callingObject.password_new.value,
                "confirmPassword": callingObject.password_confirm.value
            }

            fetch("/api/v1.1/Account/Users", {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(model)
            }).then(async response => {
                if (response.ok) {
                    let result = await response.json();
                    GetUsers();
                    callingObject.dialog.close();
                } else {
                    let result = await response.json();
                    let warningDialog = new MessageBox("New User Error", "An error occurred while creating the user. Check that the email address is valid and the password meets the requirements.");
                    warningDialog.open();
                }
            });
        });
        this.dialog.addButton(okButton);

        // add the cancel button
        let cancelButton = new ModalButton("Cancel", 0, this, async function (callingObject) {
            callingObject.dialog.close();
        });
        this.dialog.addButton(cancelButton);

        // show the dialog
        await this.dialog.open();
    }
}