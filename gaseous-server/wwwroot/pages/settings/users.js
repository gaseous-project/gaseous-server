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

                let controls = document.createElement('div');
                controls.style.textAlign = 'right';

                let editButton = '';
                let deleteButton = '';

                if (userProfile.userId != result[i].id) {
                    editButton = document.createElement('a');
                    editButton.href = '#';
                    editButton.addEventListener('click', () => {
                        // showDialog('settingsuseredit', result[i].id);
                        let userEdit = new UserEdit(result[i].id);
                        userEdit.open();
                    });
                    editButton.classList.add('romlink');

                    let editButtonImage = document.createElement('img');
                    editButtonImage.src = '/images/edit.svg';
                    editButtonImage.classList.add('banner_button_image');
                    editButtonImage.alt = 'Edit';
                    editButtonImage.title = 'Edit';
                    editButton.appendChild(editButtonImage);

                    controls.appendChild(editButton);

                    deleteButton = document.createElement('a');
                    deleteButton.href = '#';
                    deleteButton.addEventListener('click', () => {
                        let warningDialog = new MessageBox("Delete User", "Are you sure you want to delete this user?<br /><br /><strong>Warning</strong>: This cannot be undone!");
                        warningDialog.addButton(new ModalButton("OK", 2, warningDialog, async (callingObject) => {
                            fetch("/api/v1.1/Account/Users/" + result[i].id, {
                                method: 'DELETE'
                            }).then(async response => {
                                if (response.ok) {
                                    GetUsers();
                                    callingObject.msgDialog.close();
                                } else {
                                    let result = await response.json();
                                    let warningDialog = new MessageBox("Delete User Error", "An error occurred while deleting the user.");
                                    warningDialog.open();
                                }
                            });
                        }));
                        warningDialog.addButton(new ModalButton("Cancel", 0, warningDialog, async (callingObject) => {
                            callingObject.msgDialog.close();
                        }));
                        warningDialog.open();
                    });
                    deleteButton.classList.add('romlink');

                    let deleteButtonImage = document.createElement('img');
                    deleteButtonImage.src = '/images/delete.svg';
                    deleteButtonImage.classList.add('banner_button_image');
                    deleteButtonImage.alt = 'Delete';
                    deleteButtonImage.title = 'Delete';
                    deleteButton.appendChild(deleteButtonImage);

                    controls.appendChild(deleteButton);
                }

                newTable.appendChild(
                    createTableRow(
                        false,
                        [
                            userAvatar,
                            result[i].emailAddress,
                            roleDiv,
                            ageRestrictionPolicyDescription,
                            controls
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
        this.dialog.modalElement.style = 'width: 550px; height: 380px; min-width: unset; min-height: unset; max-width: unset; max-height: unset;';

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

class UserEdit {
    constructor(UserId) {
        this.userId = UserId;
    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("useredit");
        await this.dialog.BuildModal();

        await fetch("/api/v1.1/Account/Users/" + this.userId, {
            method: 'GET'
        }).then(async response => {
            if (response.ok) {
                let result = await response.json();
                this.user = result;
            } else {
                let result = await response.json();
                let warningDialog = new MessageBox("Edit User Error", "An error occurred while retrieving the user.");
                warningDialog.open();
            }
        });

        // setup the dialog
        if (this.user.profileId == "00000000-0000-0000-0000-000000000000") {
            this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = this.user.emailAddress;
        } else {
            await fetch("/api/v1.1/UserProfile/" + this.user.profileId, {
                method: 'GET'
            }).then(async response => {
                if (response.ok) {
                    let result = await response.json();
                    this.profile = result;
                    this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = this.profile.displayName + ' (' + this.user.emailAddress + ')';
                } else {
                    this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = this.user.emailAddress;
                }
            });
        }

        // set user role
        this.role_Player = this.dialog.modalElement.querySelector('#settings_user_role_player');
        this.role_Player.addEventListener('change', () => {
            this.UpdateRolePermissionsDisplay();
        });
        this.role_Gamer = this.dialog.modalElement.querySelector('#settings_user_role_gamer');
        this.role_Gamer.addEventListener('change', () => {
            this.UpdateRolePermissionsDisplay();
        });
        this.role_Admin = this.dialog.modalElement.querySelector('#settings_user_role_admin');
        this.role_Admin.addEventListener('change', () => {
            this.UpdateRolePermissionsDisplay();
        });
        this.dialog.modalElement.querySelector('#settings_user_role_' + this.user.highestRole.toLowerCase()).checked = true;
        this.UpdateRolePermissionsDisplay();

        // set up the password change form
        this.password_new = this.dialog.modalElement.querySelector('#new-password');
        this.password_confirm = this.dialog.modalElement.querySelector('#confirm-new-password');
        this.password_error = this.dialog.modalElement.querySelector('#password-error');
        this.PasswordCheck = new PasswordCheck(this.password_new, this.password_confirm, this.password_error);

        // create the ok button
        let okButton = new ModalButton("OK", 1, this, async function (callingObject) {
            // check if a new password has been entered
            if (callingObject.password_new.value.length > 0 && callingObject.password_confirm.value.length > 0) {
                // check if the new password meets the rules
                if (!PasswordCheck.CheckPasswords(callingObject.PasswordCheck, callingObject.password_new, callingObject.password_confirm)) {
                    // display an error
                    let warningDialog = new MessageBox("Password Reset Error", "The new password does not meet the requirements.");
                    warningDialog.open();
                    return;
                }

                // requirements met, reset the password
                let model = {
                    newPassword: callingObject.password_new.value,
                    confirmPassword: callingObject.password_confirm.value
                };
                let changeSuccessfull = false;
                await fetch("/api/v1.1/Account/Users/" + callingObject.userId + "/Password", {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(model)
                }).then(async response => {
                    if (!response.ok) {
                        // handle the error
                        console.error("Error updating password:");
                        console.error(response);
                        let warningDialog = new MessageBox("Password Reset Error", "The password reset failed. Check the current password and try again.");
                        warningDialog.open();
                        changeSuccessfull = false;
                        return;
                    } else {
                        // clear the password fields
                        callingObject.password_new.value = "";
                        callingObject.password_confirm.value = "";
                        callingObject.password_error.innerHTML = "";
                        changeSuccessfull = true;
                    }
                });
                if (changeSuccessfull == false) {
                    return;
                }
            }

            callingObject.dialog.close();
        });
        this.dialog.addButton(okButton);

        // create the cancel button
        let cancelButton = new ModalButton("Cancel", 0, this, function (callingObject) {
            callingObject.dialog.close();
        });
        this.dialog.addButton(cancelButton);

        // show the dialog
        this.dialog.open();
    }

    UpdateRolePermissionsDisplay() {
        // get selected role
        let selectedRole = this.dialog.modalElement.querySelector('input[name="settings_user_role"]:checked').value.toLowerCase();

        // get the player role icons
        let playerPermssions = this.dialog.modalElement.querySelectorAll('td[name="role-player"]');
        playerPermssions.forEach(element => {
            if (selectedRole == 'player') {
                element.style.display = '';
            } else {
                element.style.display = 'none';
            }
        });

        // get the gamer role icons
        let gamerPermssions = this.dialog.modalElement.querySelectorAll('td[name="role-gamer"]');
        gamerPermssions.forEach(element => {
            if (selectedRole == 'gamer') {
                element.style.display = '';
            } else {
                element.style.display = 'none';
            }
        });

        // get the admin role icons
        let adminPermssions = this.dialog.modalElement.querySelectorAll('td[name="role-admin"]');
        adminPermssions.forEach(element => {
            if (selectedRole == 'admin') {
                element.style.display = '';
            } else {
                element.style.display = 'none';
            }
        });
    }
}