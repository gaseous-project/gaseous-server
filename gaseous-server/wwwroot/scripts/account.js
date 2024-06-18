class AccountWindow {
    constructor() {

    }

    async open() {
        // Create the modal
        this.dialog = await new Modal("account");
        await this.dialog.BuildModal();

        // setup the dialog
        this.dialog.modalElement.querySelector('#modal-header-text').innerHTML = "Profile and Account";

        // configure the file upload buttons
        this.profile_avatarUpload = this.dialog.modalElement.querySelector('#avatar-upload');
        this.profile_avatarUpload.addEventListener('change', function (event) {
            const file = event.target.files[0];
            const reader = new FileReader();
            reader.onload = function (e) {
                const imagePreview = document.querySelector('#avatar-preview');
                imagePreview.style.backgroundImage = `url(${e.target.result})`;
            };
            reader.readAsDataURL(file);
        });

        this.profile_backgroundUpload = this.dialog.modalElement.querySelector('#background-upload');
        this.profile_backgroundUpload.addEventListener('change', function (event) {
            const file = event.target.files[0];
            const reader = new FileReader();
            reader.onload = function (e) {
                const imagePreview = document.querySelector('#background-preview');
                imagePreview.style.backgroundImage = `url(${e.target.result})`;
            };
            reader.readAsDataURL(file);
        });

        // populate the previews with the existing profile images
        this.AvatarPreview = this.dialog.modalElement.querySelector('#avatar-preview');
        this.BackgroundPreview = this.dialog.modalElement.querySelector('#background-preview');
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
                    this.AvatarPreview.style.backgroundColor = avatarBackgroundColor;
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
                }
            }
        });


        // create the ok button
        let okButton = new ModalButton("OK", 1, this, function (callingObject) {
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