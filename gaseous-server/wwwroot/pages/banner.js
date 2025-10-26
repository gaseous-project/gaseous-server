function setupBanner() {
    // attach event listeners to the banner elements
    let userMenu = document.getElementById("banner_user");
    if (userMenu) {
        userMenu.addEventListener('click', () => {
            let profileMenu = document.getElementById("myDropdown");
            if (!profileMenu.classList.contains('show')) {
                hideDropdowns();
                profileMenu.classList.add('show');
            } else {
                profileMenu.classList.remove('show');
            }
        });
    }

    let userMenuLogoff = document.getElementById("banner_user_logoff");
    if (userMenuLogoff) {
        userMenuLogoff.addEventListener('click', async () => {
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

    let bannerCogold = document.getElementById("banner_cogold");
    if (bannerCogold) {
        bannerCogold.addEventListener('click', () => {
            window.location.href = '/index.html?page=settings';
        });
    }

    let bannerCog = document.getElementById("banner_cog");
    if (bannerCog) {
        bannerCog.addEventListener('click', () => {
            let settingsCard = new SettingsCard();
            settingsCard.ShowCard();
        });
    }

    let bannerUpload = document.getElementById("banner_upload");
    if (bannerUpload) {
        bannerUpload.addEventListener('click', () => {
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
            if (typeof navigateToPage === 'function') {
                navigateToPage('library');
            } else {
                window.location.href = '/index.html?page=library';
            }
        });
    }

    let bannerHome = document.getElementById("banner_home");
    if (bannerHome) {
        bannerHome.addEventListener('click', () => {
            if (typeof navigateToPage === 'function') {
                navigateToPage('home');
            } else {
                window.location.href = '/index.html?page=home';
            }
        });
    }

    // set notifications
    notificationLoadEndCallbacks.push(function (data) {
        let notificationState = 0;

        if (data) {
            if (data['databaseUpgrade']) {
                // this alert should only been shown when the database upgrade notification has changed state
                // store this alert in localStorage, and then only display if it if the notification has changed
                let showDatabaseUpgradeNotification = false;

                notificationState = 1; // set the notification state to 1, as we updates in progress

                // only show the notification if it's been more than 5 minutes since the last notification
                let lastNotification = localStorage.getItem('DatabaseUpgradeNotification');
                if (lastNotification) {
                    lastNotification = JSON.parse(lastNotification);
                    let lastNotificationTime = new Date(lastNotification.timestamp);

                    // is current time greater than lastNotificationTime?
                    let currentTime = new Date();
                    if (currentTime > lastNotificationTime) {
                        showDatabaseUpgradeNotification = true;
                    }
                } else {
                    showDatabaseUpgradeNotification = true; // no previous notification, so show the notification
                }

                if (showDatabaseUpgradeNotification) {
                    // we had a change in the notification, show the notification and then store the new notification in localStorage
                    let notificationMsg = new Notification(
                        'Database Upgrade In Progress',
                        'Performance may be degraded while the database is being upgraded, while favourites and game saves may be missing. Please wait until the upgrade is complete.',
                        undefined,
                        undefined,
                        undefined,
                        'DatabaseUpgrade'
                    );
                    notificationMsg.Show();

                    // get the date and time and add 5 minutes to it
                    let nextUpdateDateTime = new Date();
                    nextUpdateDateTime.setMinutes(nextUpdateDateTime.getMinutes() + 5);

                    // store the database upgrade notification time in localStorage
                    localStorage.setItem('DatabaseUpgradeNotification', JSON.stringify(nextUpdateDateTime));
                }
            } else {
                // remove the database upgrade notification from localStorage if it exists
                localStorage.removeItem('DatabaseUpgradeNotification');
            }
            if (data['importQueue']) {
                if (data['importQueue']['Pending'] || data['importQueue']['Processing']) {
                    notificationState = 1;
                } else if (data['importQueue']['Completed']) {
                    // check localStorage for the notification. If there is a record with the same id, do not retrigger the notification
                    let importQueueNotificationData = data['importQueue']['Completed'];

                    // check if there are any notifications in the importQueue
                    let notificationsTracker = localStorage.getItem('NotificationsTracker');
                    if (!notificationsTracker) {
                        localStorage.setItem('NotificationsTracker', JSON.stringify(importQueueNotificationData));
                        notificationState = 2;
                    } else {
                        notificationsTracker = JSON.parse(notificationsTracker);
                        let found = false;

                        // check if the notification is already in the localStorage
                        // find if notification.sessionid is in the array importQueueNotificationData
                        for (const notification of importQueueNotificationData) {
                            found = false;
                            for (const notificationTracker of notificationsTracker) {
                                if (notification.sessionid === notificationTracker.sessionid) {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found) {
                                // add the notification to the localStorage, and set the notification state to 2 as this is a new notification
                                notification.read = false;
                                notificationsTracker.push(notification);
                                localStorage.setItem('NotificationsTracker', JSON.stringify(notificationsTracker));
                                notificationState = 2;

                                // show the notification
                                let notificationMsg = new Notification(
                                    'Game Imported',
                                    'New games have been imported. Reload the library to see them.',
                                    undefined,
                                    undefined,
                                    undefined,
                                    'GameImported'
                                );
                                notificationMsg.Show();
                            }
                        }

                        // check if there are any unread notifications in the notificationTracker
                        for (const notification of notificationsTracker) {
                            // check if the notification is read
                            if (!notification.read) {
                                notificationState = 2;
                                break;
                            }
                        }
                    }
                }
            }
        }

        setNotificationIconState(notificationState);

        // remove notifications older than 70 minutes from the localStorage
        let notificationsTracker = localStorage.getItem('NotificationsTracker');
        if (notificationsTracker) {
            notificationsTracker = JSON.parse(notificationsTracker);
            let currentTime = new Date();
            let newNotificationsTracker = [];
            for (const notification of notificationsTracker) {
                if (new Date(notification.expiration) > currentTime) {
                    newNotificationsTracker.push(notification);
                }
            }
            localStorage.setItem('NotificationsTracker', JSON.stringify(newNotificationsTracker));
        }
    });
    const notificationCentre = new NotificationPanel();
    document.getElementById('banner_notif').addEventListener('click', (e) => {
        // mark all notifications as read
        let notificationsTracker = localStorage.getItem('NotificationsTracker');
        if (notificationsTracker) {
            notificationsTracker = JSON.parse(notificationsTracker);
            for (const notification of notificationsTracker) {
                // mark the notification as read
                notification.read = true;
            }
            localStorage.setItem('NotificationsTracker', JSON.stringify(notificationsTracker));
        }

        // clear the notification icon only if it's in active state
        let notificationIcon = document.getElementById("banner_notifications_image");
        if (notificationIcon.classList.contains('throbbing')) {
            setNotificationIconState(0);
        }

        // open the notification center
        if (!notificationCentre.panel.classList.contains('show')) {
            hideDropdowns();
            notificationCentre.Show();
        } else {
            notificationCentre.Hide();
        }
    });

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
        hideDropdowns(event);
    }
    // event for preferences drop down item
    document.getElementById('dropdown-menu-preferences').addEventListener('click', function () {
        prefsDialog.open();
    });
    // event for account drop down item
    document.getElementById('dropdown-menu-account').addEventListener('click', function () {
        accountDialog.open();
    });
}

function hideDropdowns(event) {
    if (event === undefined || !event.target.matches('.dropbtn')) {
        let dropdowns = document.getElementsByClassName("dropdown-content");
        for (let openDropdown of dropdowns) {
            if (openDropdown.classList.contains('show')) {
                openDropdown.classList.remove('show');
            }
        }
    }
}

// notificationIconState can have 3 values:
// 0 = no notifications
// 1 = notifications has pending or processing items
// 2 = notifications are active
function setNotificationIconState(state) {
    let notificationIcon = document.getElementById("banner_notifications_image");

    switch (state) {
        case 0:
            notificationIcon.src = '/images/notifications.svg';
            notificationIcon.classList.remove('rotating');
            notificationIcon.classList.remove('throbbing');
            break;
        case 1:
            notificationIcon.src = '/images/refresh2.svg';
            notificationIcon.classList.add('rotating');
            notificationIcon.classList.remove('throbbing');
            break;
        case 2:
            notificationIcon.src = '/images/notifications-active.svg';
            notificationIcon.classList.remove('rotating');
            notificationIcon.classList.add('throbbing');
            break;
    }
}

const accountDialog = new AccountWindow();
const prefsDialog = new PreferencesWindow();
const uploadDialog = new UploadRom();

setupBanner();