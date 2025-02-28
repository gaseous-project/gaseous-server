function setupBanner() {
    // attach event listeners to the banner elements
    let userMenu = document.getElementById("banner_user");
    if (userMenu) {
        userMenu.addEventListener('click', () => {
            document.getElementById("myDropdown").classList.toggle("show");
        });
    }

    let userMenuLogoff = document.getElementById("banner_user_logoff");
    if (userMenuLogoff) {
        userMenuLogoff.addEventListener('click', async () => {
            await db.DeleteDatabase();
            ajaxCall(
                '/api/v1.1/Account/LogOff',
                'POST',
                function (result) {
                    location.replace("/index.html");
                },
                function (error) {
                    location.replace("/index.html");
                }
            );
        });
    }

    let bannerCog = document.getElementById("banner_cog");
    if (bannerCog) {
        bannerCog.addEventListener('click', () => {
            window.location.href = '/index.html?page=settings';
        });
    }

    // let refreshButton = document.getElementById("banner_refresh");
    // let refreshButtonImage = document.getElementById("banner_refresh_image");
    // if (refreshButton) {
    //     refreshButton.addEventListener('click', async () => {
    //         await db.SyncContent(true);
    //     });

    //     db.syncStartCallbacks.push(async function () {
    //         refreshButtonImage.classList.add('rotating');
    //     });

    //     db.syncFinishCallbacks.push(async function () {
    //         refreshButtonImage.classList.remove('rotating');
    //     });
    // }

    let bannerUpload = document.getElementById("banner_upload");
    if (bannerUpload) {
        bannerUpload.addEventListener('click', () => {
            const uploadDialog = new UploadRom();
            uploadDialog.open();
        });
    }

    let bannerCollection = document.getElementById("banner_collections");
    if (bannerCollection) {
        bannerCollection.addEventListener('click', () => {
            window.location.href = '/index.html?page=collections';
        });
    }

    let bannerLibrary = document.getElementById("banner_library");
    if (bannerLibrary) {
        bannerLibrary.addEventListener('click', () => {
            window.location.href = '/index.html?page=library';
        });
    }

    let bannerHome = document.getElementById("banner_home");
    if (bannerHome) {
        bannerHome.addEventListener('click', () => {
            window.location.href = '/index.html?page=home';
        });
    }

    // set avatar
    let avatarBox = document.getElementById('banner_user_image_box');
    let avatar = new Avatar(userProfile.profileId, 30, 30);
    avatarBox.style = 'pointer-events: none;';
    avatar.setAttribute('style', 'margin-top: 5px; pointer-events: none; width: 30px; height: 30px;');
    avatarBox.appendChild(avatar);

    // set profile card in drop down
    let profileCard = document.getElementById('banner_user_profilecard');
    let profileCardContent = new ProfileCard(userProfile.profileId, true);
    profileCard.appendChild(profileCardContent);

    // hide the upload button if it's not permitted
    let uploadButton = document.getElementById('banner_upload');
    if (!userProfile.roles.includes("Admin") && !userProfile.roles.includes("Gamer")) {
        uploadButton.style.display = 'none';
    }

    // Close the dropdown menu if the user clicks outside of it
    window.onclick = function (event) {
        if (!event.target.matches('.dropbtn')) {
            let dropdowns = document.getElementsByClassName("dropdown-content");
            for (let i = 0; i < dropdowns.length; i++) {
                let openDropdown = dropdowns[i];
                if (openDropdown.classList.contains('show')) {
                    openDropdown.classList.remove('show');
                }
            }
        }
    }
    // event for preferences drop down item
    document.getElementById('dropdown-menu-preferences').addEventListener('click', function () {
        const prefsDialog = new PreferencesWindow();
        prefsDialog.open();
    });
    // event for account drop down item
    document.getElementById('dropdown-menu-account').addEventListener('click', function () {
        const accountDialog = new AccountWindow(); accountDialog.open();
    });
}

setupBanner();