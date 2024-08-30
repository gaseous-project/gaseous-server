class AccountWindow {
    constructor() {

    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("account");
        await this.dialog.BuildModal();

        // setup the dialog
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "Profile and Account";

        this.AvatarPreview = this.dialog.modalElement.querySelector('#avatar-preview');
        this.AvatarPreviewChanged = false;
        let AvatarPreviewChanged = this.AvatarPreviewChanged;
        this.BackgroundPreview = this.dialog.modalElement.querySelector('#background-preview');
        this.BackgroundPreviewChanged = false;
        let BackgroundPreviewChanged = this.BackgroundPreviewChanged;
        this.DisplayNamePreview = this.dialog.modalElement.querySelector('#display-name');
        let DisplayNamePreview = this.DisplayNamePreview;
        this.QuipPreview = this.dialog.modalElement.querySelector('#quip');
        let QuipPreview = this.QuipPreview;

        // configure the file upload buttons
        this.profile_avatarUpload = this.dialog.modalElement.querySelector('#avatar-upload');
        let profile_avatarUpload = this.profile_avatarUpload;
        this.profile_avatarUpload.addEventListener('change', function (event) {
            const file = event.target.files[0];
            const reader = new FileReader();
            reader.onload = function (e) {
                const imagePreview = document.querySelector('#avatar-preview');
                imagePreview.style.backgroundImage = `url(${e.target.result})`;
                imagePreview.innerHTML = "";
                AvatarPreviewChanged = true;
            };
            reader.readAsDataURL(file);
        });
        this.profile_avatarUploadClear = this.dialog.modalElement.querySelector('#avatar-upload-clear');
        this.profile_avatarUploadClear.addEventListener('click', function (event) {
            let avatarBackgroundColor = intToRGB(hashCode(DisplayNamePreview.value));
            const imagePreview = document.querySelector('#avatar-preview');
            imagePreview.style.backgroundImage = "";
            imagePreview.innerHTML = DisplayNamePreview.value[0].toUpperCase();
            imagePreview.style.backgroundColor = "#" + avatarBackgroundColor;
            profile_avatarUpload.value = "";
            AvatarPreviewChanged = true;
        });

        this.profile_backgroundUpload = this.dialog.modalElement.querySelector('#background-upload');
        let profile_backgroundUpload = this.profile_backgroundUpload;
        this.profile_backgroundUpload.addEventListener('change', function (event) {
            const file = event.target.files[0];
            const reader = new FileReader();
            reader.onload = function (e) {
                const imagePreview = document.querySelector('#background-preview');
                imagePreview.style.backgroundImage = `url(${e.target.result})`;
                BackgroundPreviewChanged = true;
            };
            reader.readAsDataURL(file);
        });
        this.profile_backgroundUploadClear = this.dialog.modalElement.querySelector('#background-upload-clear');
        this.profile_backgroundUploadClear.addEventListener('click', function (event) {
            const imagePreview = document.querySelector('#background-preview');
            imagePreview.style.backgroundImage = "";
            profile_backgroundUpload.value = "";
            BackgroundPreviewChanged = true;
        });

        // add an event to the display name field to update the avatar preview
        this.DisplayNamePreview.addEventListener('input', function (event) {
            let avatarBackgroundColor = intToRGB(hashCode(DisplayNamePreview.value));
            const imagePreview = document.querySelector('#avatar-preview');
            imagePreview.style.backgroundColor = "#" + avatarBackgroundColor;
            if (imagePreview.style.backgroundImage === "") {
                imagePreview.innerHTML = DisplayNamePreview.value[0].toUpperCase();
            }
        });

        // add an event to the quip field to note an update
        this.QuipPreview.addEventListener('input', function (event) {
            let quip = QuipPreview.value;
        });

        // populate the previews with the existing profile images
        const response = await fetch("/api/v1.1/UserProfile/" + userProfile.profileId).then(async response => {
            if (!response.ok) {
                // handle the error
                console.error("Error fetching profile");
            } else {
                const profile = await response.json();
                if (profile) {
                    // avatar preview
                    let avatarBackgroundColor = intToRGB(hashCode(profile.displayName));
                    this.AvatarPreview.innerHTML = "";
                    this.AvatarPreview.style.backgroundImage = "";
                    this.AvatarPreview.style.backgroundColor = "#" + avatarBackgroundColor;
                    if (profile.avatar) {
                        this.AvatarPreview.style.backgroundImage = "url('/api/v1.1/UserProfile/" + userProfile.profileId + "/Avatar')";
                    } else {
                        this.AvatarPreview.innerHTML = profile.displayName[0].toUpperCase();
                    }

                    // background preview
                    this.BackgroundPreview.innerHTML = "";
                    this.BackgroundPreview.style.backgroundImage = "";
                    if (profile.profileBackground) {
                        this.BackgroundPreview.style.backgroundImage = "url('/api/v1.1/UserProfile/" + userProfile.profileId + "/Background')";
                    }

                    // display name preview
                    this.DisplayNamePreview.value = profile.displayName;

                    // quip preview
                    this.QuipPreview.value = profile.quip;
                }
            }
        });

        // set up the password change form
        this.password_current = this.dialog.modalElement.querySelector('#current-password');
        this.password_new = this.dialog.modalElement.querySelector('#new-password');
        this.password_confirm = this.dialog.modalElement.querySelector('#confirm-new-password');
        this.password_error = this.dialog.modalElement.querySelector('#password-error');
        this.PasswordCheck = new PasswordCheck(this.password_new, this.password_confirm, this.password_error);

        // create the ok button
        let okButton = new ModalButton("OK", 1, this, async function (callingObject) {
            // check if a current password has been entered
            if (callingObject.password_current.value.length > 0) {
                // assume user wants to change their password
                // check if the new password meets the rules
                if (!PasswordCheck.CheckPasswords(callingObject.PasswordCheck, callingObject.password_new, callingObject.password_confirm)) {
                    // display an error
                    let warningDialog = new MessageBox("Password Reset Error", "The new password does not meet the requirements.");
                    warningDialog.open();
                    return;
                }

                // requirements met, reset the password
                let model = {
                    oldPassword: callingObject.password_current.value,
                    newPassword: callingObject.password_new.value,
                    confirmPassword: callingObject.password_confirm.value
                };
                let changeSuccessfull = false;
                await fetch("/api/v1.1/Account/ChangePassword", {
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
                        callingObject.password_current.value = "";
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

            // create profile model
            let model = {
                userId: userProfile.profileId,
                displayName: callingObject.DisplayNamePreview.value,
                quip: callingObject.QuipPreview.value,
                data: {}
            };

            // POST the model to the API
            await fetch("/api/v1.1/UserProfile/" + userProfile.profileId, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(model)
            }).then(async response => {
                if (!response.ok) {
                    // handle the error
                    console.error("Error updating profile:");
                    console.error(response);
                } else {
                    // update the avatar
                    if (AvatarPreviewChanged === true) {
                        if (callingObject.profile_avatarUpload.files.length === 0) {
                            await fetch("/api/v1.1/UserProfile/" + userProfile.profileId + "/Avatar", {
                                method: 'DELETE'
                            }).then(async response => {
                                if (!response.ok) {
                                    // handle the error
                                    console.error("Error deleting avatar:");
                                    console.error(response);
                                }
                            });
                        } else {
                            let avatarFormData = new FormData();
                            avatarFormData.append('file', callingObject.profile_avatarUpload.files[0]);
                            await fetch("/api/v1.1/UserProfile/" + userProfile.profileId + "/Avatar", {
                                method: 'PUT',
                                body: avatarFormData
                            }).then(async response => {
                                if (!response.ok) {
                                    // handle the error
                                    console.error("Error updating avatar:");
                                    console.error(response);
                                }
                            });
                        }
                    }

                    // update the background
                    if (BackgroundPreviewChanged === true) {
                        if (callingObject.profile_backgroundUpload.files.length === 0) {
                            await fetch("/api/v1.1/UserProfile/" + userProfile.profileId + "/Background", {
                                method: 'DELETE'
                            }).then(async response => {
                                if (!response.ok) {
                                    // handle the error
                                    console.error("Error deleting background:");
                                    console.error(response);
                                }
                            });
                        } else {
                            let backgroundFormData = new FormData();
                            backgroundFormData.append('file', callingObject.profile_backgroundUpload.files[0]);
                            await fetch("/api/v1.1/UserProfile/" + userProfile.profileId + "/Background", {
                                method: 'PUT',
                                body: backgroundFormData
                            }).then(async response => {
                                if (!response.ok) {
                                    // handle the error
                                    console.error("Error updating background:");
                                    console.error(response);
                                }
                            });
                        }
                    }
                }
            });

            AccountWindow.#ReloadProfile();
            callingObject.dialog.close();
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

    static async #ReloadProfile() {
        // set avatar
        let avatarBox = document.getElementById('banner_user_image_box');
        avatarBox.innerHTML = "";
        let avatar = new Avatar(userProfile.profileId, 30, 30);
        avatarBox.style = 'pointer-events: none;';
        avatar.setAttribute('style', 'margin-top: 5px; pointer-events: none; width: 30px; height: 30px;');
        avatarBox.appendChild(avatar);

        // set profile card in drop down
        let profileCard = document.getElementById('banner_user_profilecard');
        profileCard.innerHTML = "";
        let profileCardContent = new ProfileCard(userProfile.profileId, true);
        profileCard.appendChild(profileCardContent);
    }
}

class Avatar {
    constructor(ProfileId, ElementWidth, ElementHeight, ShowProfileCard = false) {
        this.ProfileId = ProfileId;
        this.ElementWidth = ElementWidth;
        this.ElementHeight = ElementHeight;
        this.ShowProfileCard = ShowProfileCard;
        this.Avatar = document.createElement('div');
        const response = this.#FetchProfile(this);

        if (this.ShowProfileCard === true) {
            this.ProfileCard = new ProfileCard(this.ProfileId);
            this.ProfileCard.style.display = "none";
            this.Avatar.appendChild(this.ProfileCard);

            let profileCard = this.ProfileCard;
            this.Avatar.addEventListener('mouseenter', function () {
                profileCard.style.position = "absolute";
                profileCard.style.marginTop = "-2px";
                profileCard.style.marginLeft = "-2px";
                profileCard.style.display = "block";
                profileCard.style.zIndex = "100";
            });
            this.Avatar.addEventListener('mouseleave', function () {
                profileCard.style.display = "none";
            });
            this.ProfileCard.addEventListener('mouseleave', function () {
                profileCard.style.display = "none";
            });
        }

        return this.Avatar;
    }

    async Update() {
        const response = this.#FetchProfile(this);
    }

    async #FetchProfile(callingObject) {
        const response = await fetch("/api/v1.1/UserProfile/" + callingObject.ProfileId).then(async response => {
            if (!response.ok) {
                // handle the error
                console.error("Error fetching profile");
            } else {
                const profile = await response.json();
                if (profile) {
                    let newAvatarImg;
                    newAvatarImg = document.createElement('div');
                    if (profile.avatar) {
                        newAvatarImg.style = "background-image: url('/api/v1.1/UserProfile/" + callingObject.ProfileId + "/Avatar'); background-size: cover; background-position: center; border-radius: 50%; pointer-events: none; height: " + callingObject.ElementHeight + "px; width: " + callingObject.ElementWidth + "px;";
                    } else {
                        newAvatarImg.innerHTML = profile.displayName[0].toUpperCase();
                        let backgroundColor = intToRGB(hashCode(profile.displayName));
                        newAvatarImg.style = "background-color: #" + backgroundColor + "; font-size: 1vmax; display: flex; justify-content: center; align-items: center; border-radius: 50%; pointer-events: none; height: " + callingObject.ElementHeight + "px; width: " + callingObject.ElementWidth + "px;";
                    }

                    newAvatarImg.classList.add('avatar');

                    callingObject.Avatar.appendChild(newAvatarImg);
                }
            }
        });
    }
}

class ProfileCard {
    constructor(ProfileId, IsMenuCard = false) {
        this.ProfileId = ProfileId;

        // build profile card
        this.Card = document.createElement('div');
        this.Card.classList.add('profile-card');
        if (IsMenuCard === false) {
            this.Card.classList.add('profile-card-standalone');
        }
        this.BackgroundImage = document.createElement('div');
        this.BackgroundImage.classList.add('profile-card-background-image');
        this.DisplayName = document.createElement('div');
        this.DisplayName.classList.add('profile-card-display-name');
        this.Quip = document.createElement('div');
        this.Quip.classList.add('profile-card-quip');
        this.ProfileBody = document.createElement('div');
        this.ProfileBody.classList.add('profile-card-body');
        this.Avatar = document.createElement('div');
        this.Avatar.classList.add('profile-card-avatar');

        // top half of card

        // bottom half of card
        this.ProfileBody.appendChild(this.DisplayName);
        this.ProfileBody.appendChild(this.Quip);

        // assemble card
        this.Card.appendChild(this.BackgroundImage);
        this.Card.appendChild(this.ProfileBody);
        this.Card.appendChild(this.Avatar);

        const response = this.#FetchProfile(this);
        return this.Card;
    }

    async #FetchProfile(callingObject) {
        const response = await fetch("/api/v1.1/UserProfile/" + callingObject.ProfileId).then(async response => {
            if (!response.ok) {
                // handle the error
                console.error("Error fetching profile");
            } else {
                const profile = await response.json();
                if (profile) {
                    this.Avatar.appendChild(new Avatar(callingObject.ProfileId, 50, 50));
                    if (profile.profileBackground) {
                        this.BackgroundImage.style = "background-image: url('/api/v1.1/UserProfile/" + callingObject.ProfileId + "/Background');";
                    }
                    this.DisplayName.innerHTML = profile.displayName;
                    this.Quip.innerHTML = profile.quip;
                }
            }
        });
    }
}

class EmailCheck {
    constructor(EmailElement, ErrorElement, SkipUniqueCheck = false) {
        this.EmailElement = EmailElement;
        this.ErrorElement = ErrorElement;
        this.SkipUniqueCheck = SkipUniqueCheck;

        let CallingObject = this;

        this.EmailElement.addEventListener('input', function (event) {
            EmailCheck.CheckEmail(CallingObject, EmailElement);
        });

        this.DisplayRules(ErrorElement);
    }

    DisplayRules(ErrorElement) {
        this.errorList = document.createElement('ul');
        this.errorList.className = 'password-rules';

        this.listItemInvalidEmail = document.createElement('li');
        this.listItemInvalidEmail.innerHTML = "Email is a valid address";
        this.listItemInvalidEmail.classList.add('listitem');
        this.errorList.appendChild(this.listItemInvalidEmail);

        if (this.SkipUniqueCheck === false) {
            this.listItemUniqueEmail = document.createElement('li');
            this.listItemUniqueEmail.innerHTML = "Email is unique";
            this.listItemUniqueEmail.classList.add('listitem');
            this.errorList.appendChild(this.listItemUniqueEmail);
        }

        ErrorElement.innerHTML = "";
        ErrorElement.appendChild(this.errorList);

        EmailCheck.CheckEmail(this, this.EmailElement);
    }

    static async CheckEmail(CallingObject, EmailElement) {
        let emailMeetsRules = true;

        // check if email is valid
        if (EmailElement.value.match(/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/)) {
            CallingObject.listItemInvalidEmail.classList.add('listitem-green');
            CallingObject.listItemInvalidEmail.classList.remove('listitem-red');
        } else {
            CallingObject.listItemInvalidEmail.classList.add('listitem-red');
            CallingObject.listItemInvalidEmail.classList.remove('listitem-green');
            emailMeetsRules = false;
        }

        // check if email is unique
        if (CallingObject.SkipUniqueCheck === false) {
            await fetch("/api/v1.1/Account/Users/Test?Email=" + EmailElement.value, {
                method: 'GET'
            }).then(async response => {
                if (!await response.ok) {
                    // handle the error
                    console.error("Error checking email uniqueness:");
                    console.error(response);
                    CallingObject.listItemUniqueEmail.classList.add('listitem-red');
                    CallingObject.listItemUniqueEmail.classList.remove('listitem-green');
                    emailMeetsRules = false;
                } else {
                    let responseJson = await response.json();
                    if (responseJson === false) {
                        CallingObject.listItemUniqueEmail.classList.add('listitem-green');
                        CallingObject.listItemUniqueEmail.classList.remove('listitem-red');
                        emailMeetsRules = true;
                    } else {
                        CallingObject.listItemUniqueEmail.classList.add('listitem-red');
                        CallingObject.listItemUniqueEmail.classList.remove('listitem-green');
                        emailMeetsRules = false;
                    }
                }
            });
        }

        return emailMeetsRules;
    }
}

class PasswordCheck {
    constructor(NewPasswordElement, ConfirmPasswordElement, ErrorElement) {
        this.MinimumPasswordLength = 10;
        this.RequireUppercase = true;
        this.RequireLowercase = true;
        this.RequireNumber = true;
        this.RequireSpecial = false;

        this.NewPasswordElement = NewPasswordElement;
        this.ConfirmPasswordElement = ConfirmPasswordElement;
        this.ErrorElement = ErrorElement;

        let CallingObject = this;

        this.NewPasswordElement.addEventListener('input', function (event) {
            PasswordCheck.CheckPasswords(CallingObject, NewPasswordElement, ConfirmPasswordElement);
        });

        this.ConfirmPasswordElement.addEventListener('input', function (event) {
            PasswordCheck.CheckPasswords(CallingObject, NewPasswordElement, ConfirmPasswordElement);
        });

        this.DisplayRules(ErrorElement);
    }

    DisplayRules(ErrorElement) {
        this.errorList = document.createElement('ul');
        this.errorList.className = 'password-rules';

        this.listItemPasswordLength = document.createElement('li');
        this.listItemPasswordLength.innerHTML = "Minimum " + this.MinimumPasswordLength + " characters";
        this.listItemPasswordLength.classList.add('listitem');
        this.errorList.appendChild(this.listItemPasswordLength);

        if (this.RequireUppercase == true) {
            this.listItemUpper = document.createElement('li');
            this.listItemUpper.innerHTML = "At least one uppercase letter";
            this.listItemUpper.classList.add('listitem');
            this.errorList.appendChild(this.listItemUpper);
        }

        if (this.RequireLowercase == true) {
            this.listItemLower = document.createElement('li');
            this.listItemLower.innerHTML = "At least one lowercase letter";
            this.listItemLower.classList.add('listitem');
            this.errorList.appendChild(this.listItemLower);
        }

        if (this.RequireNumber == true) {
            this.listItemNumber = document.createElement('li');
            this.listItemNumber.innerHTML = "At least one number";
            this.listItemNumber.classList.add('listitem');
            this.errorList.appendChild(this.listItemNumber);
        }

        if (this.RequireSpecial == true) {
            this.listItemSpecial = document.createElement('li');
            this.listItemSpecial.innerHTML = "At least one special character.";
            this.listItemSpecial.classList.add('listitem');
            this.errorList.appendChild(this.listItemSpecial);
        }

        this.listItemMatch = document.createElement('li');
        this.listItemMatch.innerHTML = "Passwords must match.";
        this.listItemMatch.classList.add('listitem');
        this.errorList.appendChild(this.listItemMatch);

        ErrorElement.innerHTML = "";
        ErrorElement.appendChild(this.errorList);

        PasswordCheck.CheckPasswords(this, this.NewPasswordElement, this.ConfirmPasswordElement);
    }

    static CheckPasswords(CallingObject, NewPasswordElement, ConfirmPasswordElement) {
        let passwordMeetsRules = true;

        // check password length
        if (NewPasswordElement.value.length >= CallingObject.MinimumPasswordLength) {
            CallingObject.listItemPasswordLength.classList.add('listitem-green');
            CallingObject.listItemPasswordLength.classList.remove('listitem-red');
        } else {
            CallingObject.listItemPasswordLength.classList.add('listitem-red');
            CallingObject.listItemPasswordLength.classList.remove('listitem-green');
            passwordMeetsRules = false;
        }

        if (CallingObject.RequireUppercase == true) {
            // check for uppercase
            if (NewPasswordElement.value.match(/[A-Z]/)) {
                CallingObject.listItemUpper.classList.add('listitem-green');
                CallingObject.listItemUpper.classList.remove('listitem-red');
            } else {
                CallingObject.listItemUpper.classList.add('listitem-red');
                CallingObject.listItemUpper.classList.remove('listitem-green');
                passwordMeetsRules = false;
            }
        }

        if (CallingObject.RequireLowercase == true) {
            // check for lowercase
            if (NewPasswordElement.value.match(/[a-z]/)) {
                CallingObject.listItemLower.classList.add('listitem-green');
                CallingObject.listItemLower.classList.remove('listitem-red');
            } else {
                CallingObject.listItemLower.classList.add('listitem-red');
                CallingObject.listItemLower.classList.remove('listitem-green');
                passwordMeetsRules = false;
            }
        }

        if (CallingObject.RequireNumber == true) {
            // check for number
            if (NewPasswordElement.value.match(/[0-9]/)) {
                CallingObject.listItemNumber.classList.add('listitem-green');
                CallingObject.listItemNumber.classList.remove('listitem-red');
            } else {
                CallingObject.listItemNumber.classList.add('listitem-red');
                CallingObject.listItemNumber.classList.remove('listitem-green');
                passwordMeetsRules = false;
            }
        }

        if (CallingObject.RequireSpecial == true) {
            // check for special character
            if (NewPasswordElement.value.match(/[!@#$%^&*(),.?":{}|<>]/)) {
                CallingObject.listItemSpecial.classList.add('listitem-green');
                CallingObject.listItemSpecial.classList.remove('listitem-red');
            } else {
                CallingObject.listItemSpecial.classList.add('listitem-red');
                CallingObject.listItemSpecial.classList.remove('listitem-green');
                passwordMeetsRules = false;
            }
        }

        // check if passwords match
        if (NewPasswordElement.value === ConfirmPasswordElement.value) {
            CallingObject.listItemMatch.classList.add('listitem-green');
            CallingObject.listItemMatch.classList.remove('listitem-red');
        } else {
            CallingObject.listItemMatch.classList.add('listitem-red');
            CallingObject.listItemMatch.classList.remove('listitem-green');
            passwordMeetsRules = false;
        }

        return passwordMeetsRules;
    }
}