function setupButtons() {
    document.getElementById('settings_newlibrary').addEventListener('click', function () {
        let newLibrary = new NewLibrary();
        newLibrary.open();
    });
}

function drawLibrary() {
    ajaxCall(
        '/api/v1.1/Library',
        'GET',
        function (result) {
            let newTable = document.getElementById('settings_libraries');
            newTable.innerHTML = '';
            newTable.appendChild(createTableRow(true, ['Name', 'Path', 'Default Platform', 'Default Library', '']));

            for (let i = 0; i < result.length; i++) {
                let platformName = '';
                if (result[i].defaultPlatformId == 0) {
                    if (result[i].isDefaultLibrary == true) {
                        platformName = "n/a";
                    } else {
                        platformName = "";
                    }
                } else {
                    platformName = result[i].defaultPlatformName;
                }

                let defaultLibrary = '';
                if (result[i].isDefaultLibrary == true) {
                    defaultLibrary = "Yes";
                } else {
                    defaultLibrary = "";
                }

                let controls = document.createElement('div');
                controls.style.textAlign = 'right';

                let deleteButton = '';
                if (result[i].isDefaultLibrary == false) {
                    deleteButton = document.createElement('a');
                    deleteButton.href = '#';
                    deleteButton.addEventListener('click', () => {
                        let deleteLibrary = new MessageBox('Delete Library', 'Are you sure you want to delete this library?<br /><br /><strong>Warning</strong>: This cannot be undone!');
                        deleteLibrary.addButton(new ModalButton('OK', 2, deleteLibrary, function (callingObject) {
                            ajaxCall(
                                '/api/v1.1/Library/' + result[i].id,
                                'DELETE',
                                function () {
                                    callingObject.msgDialog.close();
                                    drawLibrary();
                                },
                                function () {
                                    callingObject.msgDialog.close();
                                    drawLibrary();
                                }
                            );
                        }));

                        deleteLibrary.addButton(new ModalButton('Cancel', 0, deleteLibrary, function (callingObject) {
                            callingObject.msgDialog.close();
                        }));

                        deleteLibrary.open();
                    });
                    let deleteButtonImage = document.createElement('img');
                    deleteButtonImage.src = '/images/delete.svg';
                    deleteButtonImage.className = 'banner_button_image';
                    deleteButtonImage.alt = 'Delete';
                    deleteButtonImage.title = 'Delete';
                    deleteButton.appendChild(deleteButtonImage);

                    controls.appendChild(deleteButton);
                }

                newTable.appendChild(createTableRow(
                    false,
                    [
                        result[i].name,
                        result[i].path,
                        platformName,
                        defaultLibrary,
                        controls
                    ],
                    'romrow',
                    'romcell'
                ));
            }
        }
    );
}