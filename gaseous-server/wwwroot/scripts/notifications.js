let notifications = {};
let notificationLoadStartCallbacks = [];
let notificationLoadEndCallbacks = [];
let notificationLoadErrorCallbacks = [];

// fetch the latest notifications from the server every 5 seconds
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
            console.error('Failed to fetch notifications:', response.statusText);
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
        console.error('Error fetching notifications:', error);
        if (notificationLoadEndCallbacks) {
            for (const callback of notificationLoadErrorCallbacks) {
                callback(error);
            }
        }
    });
}, 5000);

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
    constructor(heading, message, image, callback, timeout) {
        this.heading = heading;
        this.message = message;
        this.image = image;
        if (timeout === undefined) {
            this.timeout = 5000;
        } else {
            this.timeout = timeout;
        }
        this.callback = callback;
    }

    Show() {
        this.noteId = Math.random().toString(36).slice(2, 11);

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
        // processing panel
        this.processingPanel = document.createElement('div');
        this.processingPanel.classList.add('section');
        this.processingPanel.style.display = 'none';

        const processingTitle = document.createElement('div');
        processingTitle.classList.add('section-header');
        processingTitle.innerHTML = 'Import Status';
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
        completedTitle.innerHTML = 'Recently Imported';
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
        noNotificationsBody.innerHTML = 'No notifications available';
        this.noNotifications.appendChild(noNotificationsBody);

        // append the panels to the main panel
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

        let showPending = false;
        let showProcessing = false;
        let showCompleted = false;

        this.processingBody.innerHTML = '';
        this.pendingBody.innerHTML = '';
        this.completedBody.innerHTML = '';

        // check import notifications
        if (notifications['importQueue']) {
            if (notifications['importQueue']['Pending'] || notifications['importQueue']['Processing']) {
                if (notifications['importQueue']['Pending']) {
                    showPending = true;
                    this.pendingBody.innerHTML = notifications['importQueue']['Pending'] + ' imports pending';
                }

                if (notifications['importQueue']['Processing']) {
                    showProcessing = true;
                    notifications['importQueue']['Processing'].forEach(item => {
                        this.processingBody.appendChild(createNotificationPanelItem(item));
                    });
                }
            }

            if (notifications['importQueue']['Completed']) {
                showCompleted = true;

                notifications['importQueue']['Completed'].forEach(item => {
                    this.completedBody.appendChild(createNotificationPanelItem(item));
                });
            }
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

        if (showPending === false && showProcessing === false && showCompleted === false) {
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