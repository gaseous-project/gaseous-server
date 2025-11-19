let notifications = {};
let notificationLoadStartCallbacks = [];
let notificationLoadEndCallbacks = [];
let notificationLoadErrorCallbacks = [];

// fetch the latest notifications from the server every 5 seconds
function startNotificationFetch() {
    console.log(window.lang ? window.lang.translate('notifications.fetching_every_five_seconds') : 'Fetching notifications every 5 seconds');
    setInterval(async () => {
        if (notificationLoadStartCallbacks) {
            for (const callback of notificationLoadStartCallbacks) {
                callback();
            }
        }
        await fetch('/api/v1.1/Notification').then(response => {
            if (response.ok) {
                return response.json();
            } else {
                console.error((window.lang ? window.lang.translate('notifications.error.failed_fetch_prefix', [response.statusText]) : 'Failed to fetch notifications: ' + response.statusText));
                if (notificationLoadEndCallbacks) {
                    for (const callback of notificationLoadErrorCallbacks) {
                        callback(response);
                    }
                }
            }
        }).then(data => {
            if (data) {
                notifications = data;
                if (notificationLoadEndCallbacks) {
                    for (const callback of notificationLoadEndCallbacks) {
                        callback(data);
                    }
                }
            }
        }).catch(error => {
            console.error((window.lang ? window.lang.translate('notifications.error.fetch_exception_prefix', [error]) : 'Error fetching notifications: ' + error));
            if (notificationLoadEndCallbacks) {
                for (const callback of notificationLoadErrorCallbacks) {
                    callback(error);
                }
            }
        });
    }, 5000);
}

// function to add a notification
class Notification {
    // constructor
    // params:
    // heading: string - the heading of the notification
    // message: string - the message of the notification
    // image: string - the image of the notification
    // callback: function - the function to call when the notification is clicked
    // timeout: int - the timeout of the notification in milliseconds
    // if timeout is not set, it will default to 5000
    // if image is not set, it will default to null
    // if heading is not set, it will default to null
    // if message is not set, it will default to null
    // if callback is not set, it will default to null
    // if timeout is set to 0, the notification will not close automatically
    constructor(heading, message, image, callback, timeout, noteid = undefined) {
        this.heading = heading;
        this.message = message;
        this.image = image;
        if (timeout === undefined) {
            this.timeout = 5000;
        } else {
            this.timeout = timeout;
        }
        this.callback = callback;
        this.noteId = noteid;
    }

    Show() {
        if (this.noteId === undefined) {
            this.noteId = Math.random().toString(36).slice(2, 11).replaceAll('.', '');
        }

        // if the notification id already exists, remove all existing notifications with the same ID
        let existingNotifications = document.querySelectorAll(`.${this.noteId}`);
        if (existingNotifications.length > 0) {
            existingNotifications.forEach(notification => {
                notification.remove();
            });
        }

        // create the notification box
        this.noteBox = document.createElement('div');
        this.noteBox.id = this.noteId;
        this.noteBox.className = 'notification';
        this.noteBox.style.display = 'none';

        this.noteBox.addEventListener('click', () => {
            if (this.callback) { this.callback(); }
            this.#Close();
        });

        if (this.image) {
            const noteImageBox = document.createElement('div');
            noteImageBox.className = 'notification_imagebox';

            const noteImage = document.createElement('img');
            noteImage.className = 'notification_image';
            noteImage.src = this.image;
            noteImageBox.appendChild(noteImage);

            this.noteBox.appendChild(noteImageBox);
        }

        const noteMessageBox = document.createElement('div');
        noteMessageBox.className = 'notification_messagebox';

        if (this.heading) {
            const noteMessageHeading = document.createElement('div');
            noteMessageHeading.className = 'notification_title';
            noteMessageHeading.innerHTML = this.heading;
            noteMessageBox.appendChild(noteMessageHeading);
        }

        const noteMessageBody = document.createElement('div');
        noteMessageBody.className = 'notification_message';
        noteMessageBody.innerHTML = this.message;
        noteMessageBox.appendChild(noteMessageBody);

        this.noteBox.appendChild(noteMessageBox);

        document.body.appendChild(this.noteBox);

        $(this.noteBox).hide().fadeIn(500);

        if (this.timeout > 0) {
            setTimeout(() => {
                this.#Close();
            }, 5000);
        }
    }

    #Close() {
        if (this.noteBox) {
            $(this.noteBox).fadeOut(1000, () => {
                this.noteBox.remove();
                this.noteBox = null;
            });
        }
    }
}

class NotificationPanel {
    constructor() {
        this.panel = document.createElement('div');
        this.panel.id = 'notificationPanel';
        this.panel.classList.add('dropdown-content');
        this.panel.classList.add('notification_panel');

        document.body.appendChild(this.panel);

        // create notification subpanels
        // database upgrade panel
        this.databaseUpgradePanel = document.createElement('div');
        this.databaseUpgradePanel.classList.add('section');
        this.databaseUpgradePanel.style.display = 'none';

        const databaseUpgradeTitle = document.createElement('div');
        databaseUpgradeTitle.classList.add('section-header');
        databaseUpgradeTitle.innerHTML = window.lang ? window.lang.translate('notifications.section.database_upgrade.title') : 'Database Upgrade';
        this.databaseUpgradePanel.appendChild(databaseUpgradeTitle);

        this.databaseUpgradeBody = document.createElement('div');
        this.databaseUpgradeBody.classList.add('section-body');
        this.databaseUpgradeBody.classList.add('notification_body');
        this.databaseUpgradePanel.appendChild(this.databaseUpgradeBody);

        // import processing panel
        this.processingPanel = document.createElement('div');
        this.processingPanel.classList.add('section');
        this.processingPanel.style.display = 'none';

        const processingTitle = document.createElement('div');
        processingTitle.classList.add('section-header');
        processingTitle.innerHTML = window.lang ? window.lang.translate('notifications.section.import_status.title') : 'Import Status';
        this.processingPanel.appendChild(processingTitle);

        this.processingBody = document.createElement('div');
        this.processingBody.classList.add('section-body');
        this.processingBody.classList.add('notification_body');
        this.processingPanel.appendChild(this.processingBody);

        this.pendingBody = document.createElement('div');
        this.pendingBody.classList.add('section-body');
        this.pendingBody.classList.add('notification_body');
        this.processingPanel.appendChild(this.pendingBody);

        // completed panel
        this.completedPanel = document.createElement('div');
        this.completedPanel.classList.add('section');
        this.completedPanel.style.display = 'none';

        const completedTitle = document.createElement('div');
        completedTitle.classList.add('section-header');
        completedTitle.innerHTML = window.lang ? window.lang.translate('notifications.section.recently_imported.title') : 'Recently Imported';
        this.completedPanel.appendChild(completedTitle);

        this.completedBody = document.createElement('div');
        this.completedBody.classList.add('section-body');
        this.completedBody.classList.add('notification_body');
        this.completedPanel.appendChild(this.completedBody);

        // no notifications
        this.noNotifications = document.createElement('div');
        this.noNotifications.classList.add('section');

        const noNotificationsBody = document.createElement('div');
        noNotificationsBody.classList.add('section-body');
        noNotificationsBody.innerHTML = window.lang ? window.lang.translate('notifications.none_available') : 'No notifications available';
        this.noNotifications.appendChild(noNotificationsBody);

        // append the panels to the main panel
        this.panel.appendChild(this.databaseUpgradePanel);
        this.panel.appendChild(this.processingPanel);
        this.panel.appendChild(this.completedPanel);
        this.panel.appendChild(this.noNotifications);

        // schedule the update
        notificationLoadEndCallbacks.push((data) => {
            this.Update(data);
        });
    }

    Show() {
        if (!this.panel.classList.contains('show')) {
            this.panel.classList.add('show');
        }
    }

    Hide() {
        if (this.panel.classList.contains('show')) {
            this.panel.classList.remove('show');
        }
    }

    Update(notifications) {
        if (notifications === undefined || notifications === null || notifications.length === 0) {
            this.processingPanel.style.display = 'none';
            this.completedPanel.style.display = 'none';
            return;
        }

        let showDatabaseUpgrade = false;
        let showPending = false;
        let showProcessing = false;
        let showCompleted = false;

        this.databaseUpgradeBody.innerHTML = '';
        this.processingBody.innerHTML = '';
        this.pendingBody.innerHTML = '';
        this.completedBody.innerHTML = '';

        // check database upgrade notifications
        if (notifications['databaseUpgrade']) {
            const upgradeNotification = document.createElement('div');
            upgradeNotification.classList.add('notification_item');

            let upgradeLabel = document.createElement('span');
            upgradeLabel.innerHTML = window.lang ? window.lang.translate('notifications.database_upgrade.in_progress') : 'Upgrading database...';
            upgradeNotification.appendChild(upgradeLabel);

            let upgradeLabels = {
                "MetadataRefresh_Platform": window.lang ? window.lang.translate('notifications.database_upgrade.task.platform_metadata') : 'Platform Metadata',
                "MetadataRefresh_Signatures": window.lang ? window.lang.translate('notifications.database_upgrade.task.signature_metadata') : 'Signature Metadata',
                "MetadataRefresh_Game": window.lang ? window.lang.translate('notifications.database_upgrade.task.game_metadata') : 'Game Metadata',
                "DatabaseMigration_1031": window.lang ? window.lang.translate('notifications.database_upgrade.task.migrating_user_data') : 'Migrating user data to new database schema'
            }

            if (Object.keys(notifications['databaseUpgrade']).length > 0) {
                let upgradeList = document.createElement('ul');
                upgradeList.classList.add('password-rules');
                for (const key of Object.keys(notifications['databaseUpgrade'])) {
                    let subTask = notifications['databaseUpgrade'][key];

                    let upgradeItem = document.createElement('li');
                    // upgradeItem.classList.add('listitem');
                    upgradeItem.classList.add('listitem-narrow');
                    upgradeItem.classList.add('taskrow');
                    switch (subTask['state']) {
                        case 'NeverStarted':
                            upgradeItem.classList.add('listitem-pending');
                            break;
                        case 'Running':
                            upgradeItem.classList.add('listitem-inprogress');
                            break;
                        case 'Stopped':
                            upgradeItem.classList.add('listitem-green');
                            break;
                    }

                    upgradeItem.innerHTML = `${upgradeLabels[key] || key}`;
                    if (subTask['progress'].split(' of ').length === 2) {
                        const parts = subTask['progress'].split(' of ');
                        upgradeItem.innerHTML += `<br /><progress value="${parts[0]}" max="${parts[1]}" style="width: 100%;"></progress>`;
                    } else {
                        if (subTask['state'] === 'Stopped') {
                            upgradeItem.innerHTML += `<br /><progress value="1" max="1" style="width: 100%;"></progress>`;
                        } else {
                            upgradeItem.innerHTML += `<br /><progress value="0" max="1" style="width: 100%;"></progress>`;
                        }
                    }
                    upgradeList.appendChild(upgradeItem);
                }

                upgradeNotification.appendChild(upgradeList);
            }

            let upgradeText = document.createElement('p');
            upgradeText.innerHTML = window.lang ? window.lang.translate('notifications.database_upgrade.explanation') : 'The system is currently performing a database upgrade. This may take some time depending on the size of your library. Some features may not be available during the upgrade.';
            upgradeNotification.appendChild(upgradeText);
            this.databaseUpgradeBody.appendChild(upgradeNotification);
            showDatabaseUpgrade = true;
        }

        // check import notifications
        if (notifications['importQueue']) {
            if (notifications['importQueue']['Pending'] || notifications['importQueue']['Processing']) {
                if (notifications['importQueue']['Pending']) {
                    showPending = true;
                    this.pendingBody.innerHTML = window.lang ? window.lang.translate('notifications.import_queue.pending_count', [notifications['importQueue']['Pending']]) : (notifications['importQueue']['Pending'] + ' imports pending');
                }

                if (notifications['importQueue']['Processing']) {
                    showProcessing = true;
                    notifications['importQueue']['Processing'].forEach(item => {
                        this.processingBody.appendChild(createNotificationPanelItem(item));
                    });
                }
            }

            if (notifications['importQueue']['imported']) {
                showCompleted = true;

                notifications['importQueue']['imported'].forEach(item => {
                    this.completedBody.appendChild(createNotificationPanelItem(item));
                });
            }
        }

        // update the visibility of the panels
        if (showDatabaseUpgrade) {
            this.databaseUpgradePanel.style.display = 'block';
        } else {
            this.databaseUpgradePanel.style.display = 'none';
        }

        if (showProcessing || showPending) {
            this.processingPanel.style.display = 'block';
            if (showPending) {
                this.pendingBody.style.display = 'block';
            } else {
                this.pendingBody.style.display = 'none';
            }
            if (showProcessing) {
                this.processingBody.style.display = 'block';
            } else {
                this.processingBody.style.display = 'none';
            }
        } else {
            this.processingPanel.style.display = 'none';
        }

        if (showCompleted) {
            this.completedPanel.style.display = 'block';
        } else {
            this.completedPanel.style.display = 'none';
        }

        if (showDatabaseUpgrade === false && showPending === false && showProcessing === false && showCompleted === false) {
            this.noNotifications.style.display = 'block';
        } else {
            this.noNotifications.style.display = 'none';
        }
    }
}

// Function to create a notification panel item
function createNotificationPanelItem(importQueueItem) {
    const itemDiv = document.createElement('div');
    itemDiv.classList.add('notification_item');

    const itemName = document.createElement('div');
    itemName.classList.add('notification_item_name');
    itemName.innerHTML = importQueueItem['filename'];
    itemDiv.appendChild(itemName);

    const itemDate = document.createElement('div');
    itemDate.classList.add('notification_item_date');
    const date = new Date(importQueueItem['lastupdated']);
    itemDate.innerHTML = date.toLocaleString();
    itemDiv.appendChild(itemDate);

    return itemDiv;
}