function displayNotification(heading, message, image, link) {
    var noteId = Math.random().toString(36).substr(2, 9);
    
    var noteBox = document.createElement('div');
    noteBox.id = noteId;
    noteBox.className = 'notification';
    noteBox.style.display = 'none;'

    if (link) {
        noteBox.setAttribute('onclick', 'window.location.href = "' + link + '"');
    } else {
        noteBox.setAttribute('onclick', 'closeNotification("' + noteId + '");');
    }

    if (image) {
        var noteImageBox = document.createElement('div');
        noteImageBox.className = 'notification_imagebox';

        var noteImage = document.createElement('img');
        noteImage.className = 'notification_image';
        noteImage.src = image;
        noteImageBox.appendChild(noteImage);

        noteBox.appendChild(noteImageBox);
    }

    var noteMessageBox = document.createElement('div');
    noteMessageBox.className = 'notification_messagebox';

    if (heading) {
        var noteMessageHeading = document.createElement('div');
        noteMessageHeading.className = 'notification_title';
        noteMessageHeading.innerHTML = heading;
        noteMessageBox.appendChild(noteMessageHeading);
    }

    var noteMessageBody = document.createElement('div');
    noteMessageBody.className = 'notification_message';
    noteMessageBody.innerHTML = message;
    noteMessageBox.appendChild(noteMessageBody);

    noteBox.appendChild(noteMessageBox);

    document.getElementById('notifications_target').appendChild(noteBox);

    $(noteBox).hide().fadeIn(500);

    setTimeout(function() {
        closeNotification(noteId);
    }, 5000);
}

function closeNotification(id) {
    var notificationObj = document.getElementById(id);
    $(notificationObj).fadeOut(1000, function() {
        notificationObj.parentElement.removeChild(notificationObj);
    });
}