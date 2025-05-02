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

class Notification {
    constructor(heading, message, image, callback) {
        this.heading = heading;
        this.message = message;
        this.image = image;
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

        setTimeout(() => {
            this.#Close();
        }, 5000);
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