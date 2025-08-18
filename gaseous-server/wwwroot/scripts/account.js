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

        // load the enabled social login buttons
        fetch('/api/v1.1/Account/social-login', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        })
            .then(response => response.json())
            .then(data => {
                if (data.includes('Password')) {
                    this.dialog.modalElement.querySelector('#tab-tab2').style.display = '';
                    this.dialog.modalElement.querySelector('#tab-tab3').style.display = '';
                } else {
                    this.dialog.modalElement.querySelector('#tab-tab2').style.display = 'none';
                    this.dialog.modalElement.querySelector('#tab-tab3').style.display = 'none';
                }
            })
            .catch(error => {
                console.error('Error fetching social login options:', error);
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
                        this.AvatarPreview.style.backgroundImage = "url('/api/v1.1/UserProfile/" + userProfile.profileId + "/Avatar/" + profile.avatar.fileName + profile.avatar.extension + "')";
                    } else {
                        this.AvatarPreview.innerHTML = profile.displayName[0].toUpperCase();
                    }

                    // background preview
                    this.BackgroundPreview.innerHTML = "";
                    this.BackgroundPreview.style.backgroundImage = "";
                    if (profile.profileBackground) {
                        this.BackgroundPreview.style.backgroundImage = "url('/api/v1.1/UserProfile/" + userProfile.profileId + "/Background/" + profile.profileBackground.fileName + profile.profileBackground.extension + "')";
                    }

                    // display name preview
                    this.DisplayNamePreview.value = profile.displayName;

                    // quip preview
                    this.QuipPreview.value = profile.quip;
                }
            }
        });

        // set up the user name form
        this.username_current = this.dialog.modalElement.querySelector('#current-username');
        this.username_current.value = userProfile.userName;
        this.username_new = this.dialog.modalElement.querySelector('#new-username');
        this.UsernameCheck = new UsernameCheck(this.username_new, this.dialog.modalElement.querySelector('#username-error'));

        // set up the password change form
        this.password_current = this.dialog.modalElement.querySelector('#current-password');
        this.password_new = this.dialog.modalElement.querySelector('#new-password');
        this.password_confirm = this.dialog.modalElement.querySelector('#confirm-new-password');
        this.password_error = this.dialog.modalElement.querySelector('#password-error');
        this.PasswordCheck = new PasswordCheck(this.password_new, this.password_confirm, this.password_error);

        // set up the 2fa setup form
        this.twoFactorButton_enable = this.dialog.modalElement.querySelector('#enable-2fa');
        this.twoFactorButton_disable = this.dialog.modalElement.querySelector('#disable-2fa');
        this.twoFactorRecoveryCodes = this.dialog.modalElement.querySelector('#recovery-codes');
        this.twoFactorStatus = await fetch("/api/v1.1/TwoFactor/status").then(async response => {
            if (!response.ok) {
                // handle the error
                console.error("Error fetching 2FA status:");
                console.error(response);
                return "Error";
            } else {
                const status = await response.json();
                if (status.enabled) {
                    this.twoFactorButton_enable.style.display = 'none';
                    this.twoFactorButton_disable.style.display = '';

                    // show the recovery code count
                    this.twoFactorRecoveryCodes.style.display = '';
                    this.twoFactorRecoveryCodes.innerHTML = `<strong>Recovery Codes:</strong> ${status.recoveryCodesLeft} available`;
                    return "Enabled";
                } else {
                    this.twoFactorButton_enable.style.display = '';
                    this.twoFactorButton_disable.style.display = 'none';
                    this.twoFactorRecoveryCodes.style.display = 'none';
                    return "Disabled";
                }
            }
        });

        this.twoFactorConfirmationButton = this.dialog.modalElement.querySelector('#confirm-2fa');
        this.twoFactorConfirmationButton.addEventListener('click', async function () {
            // confirm the user has set up their authenticator app
            const confirmationCode = document.getElementById('2fa-code').value.trim();
            if (confirmationCode.length === 0) {
                let warningDialog = new MessageBox("2FA Confirmation Error", "Please enter the confirmation code from your authenticator app.");
                warningDialog.open();
                return;
            }
            // send the confirmation code to the server
            const response = await fetch("/api/v1.1/TwoFactor/authenticator/confirm", {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ code: confirmationCode })
            }).then(async response => {
                if (!response.ok) {
                    // handle the error
                    console.error("Error confirming 2FA setup:");
                    console.error(response);
                    let warningDialog = new MessageBox("2FA Confirmation Error", "The confirmation code is invalid or expired. Please try again.");
                    warningDialog.open();
                    return;
                } else {
                    // update the 2FA status
                    AccountWindow.#ReloadProfile();
                    let successDialog = new MessageBox("2FA Enabled", "2FA has been successfully enabled for your account.");
                    successDialog.open();
                    document.getElementById('enable-2fa').style.display = 'none';
                    document.getElementById('disable-2fa').style.display = '';

                    // display the recovery codes
                    const recoveryCodesResponse = await fetch("/api/v1.1/TwoFactor/recovery/generate", {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({ count: 10 })
                    });
                    if (!recoveryCodesResponse.ok) {
                        console.error("Error fetching recovery codes:");
                        console.error(recoveryCodesResponse);
                        let warningDialog = new MessageBox("Recovery Codes Error", "Couldn't fetch your recovery codes. Please try again later.");
                        warningDialog.open();
                        return;
                    }
                    const recoveryCodes = await recoveryCodesResponse.json();
                    if (recoveryCodes && recoveryCodes.length > 0) {
                        let recoveryCodesContainer = document.getElementById('recovery-codes');
                        recoveryCodesContainer.innerHTML = "";

                        let recoveryCodesSummary = document.createElement('p');
                        recoveryCodesSummary.innerHTML = "Store these codes in a safe place. They can be used to access your account if you lose access to your authenticator app. They are one-time use only, and never shown again.";
                        recoveryCodesContainer.appendChild(recoveryCodesSummary);

                        recoveryCodes.forEach(code => {
                            let codeElement = document.createElement('div');
                            codeElement.classList.add('recovery-code');
                            codeElement.textContent = code;
                            recoveryCodesContainer.appendChild(codeElement);
                        });
                        recoveryCodesContainer.style.display = 'block';
                    } else {
                        let warningDialog = new MessageBox("Recovery Codes Error", "No recovery codes were returned. Please try again later.");
                        warningDialog.open();
                    }

                    // close the confirmation input
                    const confirmationContainer = document.getElementById('2fa-confirmation');
                    if (confirmationContainer) {
                        confirmationContainer.style.display = 'none';
                    }
                    // hide the QR code and secret key
                    const qrCodeContainer = document.getElementById('2fa-qrcode');
                    if (qrCodeContainer) {
                        qrCodeContainer.innerHTML = "";
                        qrCodeContainer.style.display = 'none';
                    }
                    const secretKeyContainer = document.getElementById('2fa-secret-key');
                    if (secretKeyContainer) {
                        secretKeyContainer.innerHTML = "";
                        secretKeyContainer.style.display = 'none';
                    }
                }
            });
        });

        this.twoFactorButton_enable.addEventListener('click', async function () {
            // Reset/generate a new authenticator key, then render QR code and show the secret
            try {
                const resetResponse = await fetch("/api/v1.1/TwoFactor/authenticator/reset", {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });
                if (!resetResponse.ok) {
                    console.error("Error resetting/generating authenticator key:", resetResponse);
                    let warningDialog = new MessageBox("2FA Setup Error", "Couldn't generate a 2FA key. Please try again later.");
                    warningDialog.open();
                    return;
                }

                // API returns the secret key as plain text (string)
                const contentType = resetResponse.headers.get('content-type') || '';
                let secretKey;
                if (contentType.includes('application/json')) {
                    // Fallback if server ever changes to JSON
                    const parsed = await resetResponse.json();
                    secretKey = typeof parsed === 'string' ? parsed : (parsed?.secret || String(parsed));
                } else {
                    secretKey = await resetResponse.text();
                }

                // Normalize secret to Base32 (remove spaces/non-base32 chars, uppercase) for maximum compatibility
                let normalizedSecret = (secretKey || "").toString().trim();
                normalizedSecret = normalizedSecret.replace(/[^A-Z2-7=]/gi, '').toUpperCase();

                // Build otpauth URL for TOTP
                const issuerRaw = (document.title && document.title.trim().length > 0) ? document.title.trim() : window.location.host;
                const issuerEnc = encodeURIComponent(issuerRaw);
                let accountRaw = (userProfile && (userProfile.email || userProfile.userName)) || "";
                if (!accountRaw || accountRaw.trim().length === 0) {
                    accountRaw = "user";
                }
                const accountEnc = encodeURIComponent(accountRaw);
                const label = `${issuerEnc}:${accountEnc}`;
                // Use only required params for broad client compatibility (Authy, GA, etc.)
                const otpAuthUrl = `otpauth://totp/${label}?secret=${normalizedSecret}&issuer=${issuerEnc}&digits=6&period=30`;

                // Render QR code
                const qrCodeContainer = document.getElementById('2fa-qrcode');
                if (qrCodeContainer) {
                    qrCodeContainer.innerHTML = "";
                    new QRCode(qrCodeContainer, {
                        text: otpAuthUrl,
                        width: 128,
                        height: 128,
                        colorDark: "#000000",
                        colorLight: "#ffffff",
                        correctLevel: QRCode.CorrectLevel.H
                    });
                    qrCodeContainer.style.display = 'block';
                }

                // Show the raw secret key
                const secretKeyContainer = document.getElementById('2fa-secret-key');
                if (secretKeyContainer) {
                    secretKeyContainer.innerHTML = `<strong>Secret Key:</strong> ${normalizedSecret}`;
                    secretKeyContainer.style.display = 'block';
                }

                // Show confirmation input
                const confirmationContainer = document.getElementById('2fa-confirmation');
                const confirmationInput = document.getElementById('2fa-code');
                if (confirmationContainer) {
                    confirmationContainer.style.display = 'block';
                    confirmationInput.value = ""; // Clear previous input
                }
            } catch (err) {
                console.error("Unexpected error during 2FA setup:", err);
                let warningDialog = new MessageBox("2FA Setup Error", "An unexpected error occurred. Please try again later.");
                warningDialog.open();
            }
        });

        this.twoFactorButton_disable.addEventListener('click', async function () {
            // confirm the user wants to disable 2FA
            let confirmation = confirm("Are you sure you want to disable 2FA? This will remove the extra security layer from your account.");
            if (confirmation) {
                // disable 2FA
                const response = await fetch("/api/v1.1/TwoFactor/enable/false", {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                }).then(async response => {
                    if (!response.ok) {
                        // handle the error
                        console.error("Error disabling 2FA:");
                        console.error(response);
                        let warningDialog = new MessageBox("2FA Disable Error", "The 2FA disable failed. Please try again later.");
                        warningDialog.open();
                        return;
                    } else {
                        // update the 2FA status
                        AccountWindow.#ReloadProfile();
                        let successDialog = new MessageBox("2FA Disabled", "2FA has been successfully disabled for your account.");
                        successDialog.open();
                        document.getElementById('enable-2fa').style.display = '';
                        document.getElementById('disable-2fa').style.display = 'none';
                    }
                });
            }
        });


        // create the ok button
        let okButton = new ModalButton("OK", 1, this, async function (callingObject) {
            // check if a new username has been entered
            if (callingObject.username_new.value.length > 0 && callingObject.username_new.value !== userProfile.userName) {
                // assume user wants to change their username
                // check if the new username meets the rules
                if (!UsernameCheck.CheckUsername(callingObject.UsernameCheck, callingObject.username_new)) {
                    // display an error
                    let warningDialog = new MessageBox("Username Change Error", "The new username does not meet the requirements.");
                    warningDialog.open();
                    return;
                }

                // requirements met, change the username
                let model = {
                    newUserName: callingObject.username_new.value
                };
                let changeSuccessfull = false;
                await fetch("/api/v1.1/Account/ChangeUsername", {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(model)
                }).then(async response => {
                    if (!response.ok) {
                        // handle the error
                        console.error("Error updating username:");
                        console.error(response);
                        let warningDialog = new MessageBox("Username Change Error", "The username change failed. Try a different username.");
                        warningDialog.open();
                        changeSuccessfull = false;
                        return;
                    } else {
                        // clear the username field
                        callingObject.username_new.value = "";
                        callingObject.username_current.value = model.newUserName;
                        userProfile.userName = model.newUserName;
                        changeSuccessfull = true;
                    }
                });
                if (changeSuccessfull == false) {
                    return;
                }
            }

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
                profileCard.style.position = "fixed";
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
                        newAvatarImg.style = "background-image: url('/api/v1.1/UserProfile/" + callingObject.ProfileId + "/Avatar/" + profile.avatar.fileName + profile.avatar.extension + "'); background-size: cover; background-position: center; border-radius: 50%; pointer-events: none; height: " + callingObject.ElementHeight + "px; width: " + callingObject.ElementWidth + "px;";
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
        this.BackgroundImageGradient = document.createElement('div');
        this.BackgroundImageGradient.classList.add('profile-card-background-image-gradient');
        this.DisplayName = document.createElement('div');
        this.DisplayName.classList.add('profile-card-display-name');
        this.Quip = document.createElement('div');
        this.Quip.classList.add('profile-card-quip');
        this.ProfileBody = document.createElement('div');
        this.ProfileBody.classList.add('profile-card-body');
        this.ProfileNowPlaying = document.createElement('div');
        this.ProfileNowPlaying.classList.add('profile-card-now-playing-body');
        this.ProfileNowPlayingBg = document.createElement('div');
        this.ProfileNowPlayingBg.classList.add('profile-card-now-playing-body-bg');
        this.ProfileNowPlayingLabel = document.createElement('div');
        this.ProfileNowPlayingLabel.classList.add('profile-card-now-playing-label');
        this.ProfileNowPlayingCover = document.createElement('div');
        this.ProfileNowPlayingCover.classList.add('profile-card-now-playing-cover');
        this.ProfileNowPlayingTitle = document.createElement('div');
        this.ProfileNowPlayingTitle.classList.add('profile-card-now-playing-title');
        this.ProfileNowPlayingPlatform = document.createElement('div');
        this.ProfileNowPlayingPlatform.classList.add('profile-card-now-playing-platform');
        this.ProfileNowPlayingDuration = document.createElement('div');
        this.ProfileNowPlayingDuration.classList.add('profile-card-now-playing-duration');
        this.Avatar = document.createElement('div');
        this.Avatar.classList.add('profile-card-avatar');

        // top half of card

        // bottom half of card
        this.ProfileBody.appendChild(this.DisplayName);
        this.ProfileBody.appendChild(this.Quip);

        // now playing
        this.ProfileNowPlayingBg.appendChild(this.ProfileNowPlaying);
        this.ProfileNowPlaying.appendChild(this.ProfileNowPlayingLabel);
        this.ProfileNowPlaying.appendChild(this.ProfileNowPlayingCover);
        this.ProfileNowPlaying.appendChild(this.ProfileNowPlayingTitle);
        this.ProfileNowPlaying.appendChild(this.ProfileNowPlayingPlatform);
        this.ProfileNowPlaying.appendChild(this.ProfileNowPlayingDuration);

        // assemble card
        this.BackgroundImage.appendChild(this.BackgroundImageGradient);
        this.Card.appendChild(this.BackgroundImage);
        this.Card.appendChild(this.ProfileBody);
        this.Card.appendChild(this.ProfileNowPlayingBg);
        this.Card.appendChild(this.Avatar);

        this.ProfileData = null;
        const response = this.#FetchProfile(this);

        // set timeout to refresh the profile card every 30 seconds
        let callingObject = this;
        setInterval(function () {
            callingObject.#FetchProfile(callingObject);
        }, 15000);

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
                    let stillUpdateAnyway = false;
                    if (callingObject.ProfileData === null) {
                        callingObject.ProfileData = profile;
                        stillUpdateAnyway = true;
                    }

                    // update avatar if different
                    if (callingObject.ProfileData.avatar !== profile.avatar || stillUpdateAnyway === true) {
                        callingObject.Avatar.innerHTML = "";
                        callingObject.Avatar.appendChild(new Avatar(callingObject.ProfileId, 60, 60));
                    }

                    // update profile background if different
                    if (callingObject.ProfileData.profileBackground !== profile.profileBackground || stillUpdateAnyway === true) {
                        if (profile.profileBackground) {
                            callingObject.BackgroundImage.style = "background-image: url('/api/v1.1/UserProfile/" + callingObject.ProfileId + "/Background/" + profile.profileBackground.fileName + profile.profileBackground.extension + "');";
                        } else {
                            // callingObject.BackgroundImage.style = "";
                            // set a random background image
                            let backgroundsList = [
                                '/images/CollectionsWallpaper.jpg'
                            ]
                            let randomBackground = Math.floor(Math.random() * backgroundsList.length);
                            callingObject.BackgroundImage.style = "background-image: url('" + backgroundsList[randomBackground] + "');";
                        }
                    }

                    // update display name if different
                    if (callingObject.ProfileData.displayName !== profile.displayName || stillUpdateAnyway === true) {
                        callingObject.DisplayName.innerHTML = profile.displayName;
                    }

                    // update quip if different
                    if (callingObject.ProfileData.quip !== profile.quip || stillUpdateAnyway === true) {
                        callingObject.Quip.innerHTML = profile.quip;
                    }

                    if (profile.nowPlaying) {
                        callingObject.ProfileNowPlayingLabel.innerHTML = "Now Playing";
                        let cardImage = '';
                        if (profile.nowPlaying.game.cover) {
                            cardImage = "/api/v1.1/Games/" + profile.nowPlaying.game.metadataMapId + '/' + profile.nowPlaying.game.metadataSource + "/cover/" + profile.nowPlaying.game.cover + "/image/cover_small/" + profile.nowPlaying.game.cover + ".jpg";
                        } else {
                            cardImage = "/images/unknowngame.png";
                        }
                        callingObject.ProfileNowPlayingCover.style = "background-image: url(\"" + cardImage + "\");";
                        callingObject.ProfileNowPlayingBg.style = "background-image: url(\"" + cardImage + "\");";
                        callingObject.ProfileNowPlayingTitle.innerHTML = profile.nowPlaying.game.name;
                        callingObject.ProfileNowPlayingPlatform.innerHTML = profile.nowPlaying.platform.name;
                        if (profile.nowPlaying.duration === 1) {
                            callingObject.ProfileNowPlayingDuration.innerHTML = profile.nowPlaying.duration + " minute";
                        } else {
                            callingObject.ProfileNowPlayingDuration.innerHTML = profile.nowPlaying.duration + " minutes";
                        }
                        callingObject.ProfileNowPlayingBg.style.display = "";
                    } else {
                        callingObject.ProfileNowPlayingBg.style.display = "none";
                    }

                    callingObject.ProfileData = profile;
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

class UsernameCheck {
    constructor(UsernameElement, ErrorElement) {
        this.UsernameElement = UsernameElement;
        this.ErrorElement = ErrorElement;

        let CallingObject = this;

        this.UsernameElement.addEventListener('input', function (event) {
            UsernameCheck.CheckUsername(CallingObject, UsernameElement);
        });

        this.DisplayRules(ErrorElement);
    }

    DisplayRules(ErrorElement) {
        this.errorList = document.createElement('ul');
        this.errorList.className = 'password-rules';

        this.listItemLength = document.createElement('li');
        this.listItemLength.innerHTML = "Between 3 and 30 characters";
        this.listItemLength.classList.add('listitem');
        this.errorList.appendChild(this.listItemLength);

        this.listItemCharacters = document.createElement('li');
        this.listItemCharacters.innerHTML = "Only letters, numbers, underscores, dashes, periods, and at signs";
        this.listItemCharacters.classList.add('listitem');
        this.errorList.appendChild(this.listItemCharacters);

        this.listItemUnique = document.createElement('li');
        this.listItemUnique.innerHTML = "Username is unique";
        this.listItemUnique.classList.add('listitem');
        this.errorList.appendChild(this.listItemUnique);

        ErrorElement.innerHTML = "";
        ErrorElement.appendChild(this.errorList);

        UsernameCheck.CheckUsername(this, this.UsernameElement);
    }

    static async CheckUsername(CallingObject, UsernameElement) {
        let usernameMeetsRules = true;

        // check username length
        if (UsernameElement.value.length >= 3 && UsernameElement.value.length <= 30) {
            CallingObject.listItemLength.classList.add('listitem-green');
            CallingObject.listItemLength.classList.remove('listitem-red');
        } else {
            CallingObject.listItemLength.classList.add('listitem-red');
            CallingObject.listItemLength.classList.remove('listitem-green');
            usernameMeetsRules = false;
        }

        // check if username contains only valid characters
        if (UsernameElement.value.match(/^[a-zA-Z0-9_.@-]+$/)) {
            CallingObject.listItemCharacters.classList.add('listitem-green');
            CallingObject.listItemCharacters.classList.remove('listitem-red');
        } else {
            CallingObject.listItemCharacters.classList.add('listitem-red');
            CallingObject.listItemCharacters.classList.remove('listitem-green');
            usernameMeetsRules = false;
        }

        // check if username is unique - skip if empty
        if (UsernameElement.value.length > 0) {
            await fetch("/api/v1.1/Account/Users/Test?Email=" + UsernameElement.value, {
                method: 'GET'
            }).then(async response => {
                if (!response.ok) {
                    // handle the error
                    console.error("Error checking username uniqueness:");
                    console.error(response);
                    CallingObject.listItemUnique.classList.add('listitem-red');
                    CallingObject.listItemUnique.classList.remove('listitem-green');
                    usernameMeetsRules = false;
                } else {
                    let responseJson = await response.json();
                    if (responseJson === false) {
                        CallingObject.listItemUnique.classList.add('listitem-green');
                        CallingObject.listItemUnique.classList.remove('listitem-red');
                    } else {
                        CallingObject.listItemUnique.classList.add('listitem-red');
                        CallingObject.listItemUnique.classList.remove('listitem-green');
                        usernameMeetsRules = false;
                    }
                }
            });
        } else {
            CallingObject.listItemUnique.classList.add('listitem-red');
            CallingObject.listItemUnique.classList.remove('listitem-green');
            usernameMeetsRules = false;
        }

        return usernameMeetsRules;
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