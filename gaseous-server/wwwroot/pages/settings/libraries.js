function setupButtons() {
    document.getElementById('settings_newlibrary').addEventListener('click', function () {
        let newLibrary = new NewLibrary();
        newLibrary.open();
    });
}

function drawLibrary() {
    ajaxCall(
        '/api/v1.1/Library?GetStorageInfo=true',
        'GET',
        function (result) {
            let newTable = document.getElementById('settings_libraries');
            newTable.innerHTML = '';

            for (let library of result) {
                let container = document.createElement('div');
                container.classList.add('section');

                let header = document.createElement('div');
                header.classList.add('section-header');

                let headerText = document.createElement('span');
                headerText.innerHTML = library.name;
                header.appendChild(headerText);

                let body = document.createElement('div');
                body.classList.add('section-body');

                let libraryTable = document.createElement('table');
                libraryTable.style.width = '100%';
                libraryTable.style.borderCollapse = 'collapse';

                let pathRow = document.createElement('tr');
                let pathLabel = document.createElement('td');
                pathLabel.style.width = '20%';
                pathLabel.innerHTML = 'Path';
                let pathValue = document.createElement('td');

                let controlsCell = document.createElement('td');
                controlsCell.style.width = '20%';
                controlsCell.style.textAlign = 'right';
                controlsCell.rowSpan = 3;

                if (!library.isDefaultLibrary) {
                    let deleteButton = document.createElement('a');
                    deleteButton.href = '#';
                    deleteButton.addEventListener('click', () => {
                        let deleteLibrary = new MessageBox('Delete Library', 'Are you sure you want to delete this library?<br /><br /><strong>Warning</strong>: This cannot be undone!');
                        deleteLibrary.addButton(new ModalButton('OK', 2, deleteLibrary, function (callingObject) {
                            ajaxCall(
                                '/api/v1.1/Library/' + library.id,
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
                    controlsCell.appendChild(deleteButton);
                }

                let scanButton = document.createElement('img');
                scanButton.classList.add('taskstart');
                scanButton.src = '/images/start-task.svg';
                scanButton.title = 'Start Scan';
                scanButton.alt = 'Start Scan';
                scanButton.addEventListener('click', function () {
                    let scanLibrary = new MessageBox('Scan Library', 'Are you sure you want to scan this library?');
                    scanLibrary.addButton(new ModalButton('OK', 2, scanLibrary, function (callingObject) {
                        ajaxCall(
                            '/api/v1.1/Library/' + library.id + '/Scan',
                            'POST',
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

                    scanLibrary.addButton(new ModalButton('Cancel', 0, scanLibrary, function (callingObject) {
                        callingObject.msgDialog.close();
                    }));

                    scanLibrary.open();
                });
                controlsCell.appendChild(scanButton);

                let pathValueText = document.createElement('span');
                pathValueText.innerHTML = library.path;
                pathValue.appendChild(pathValueText);
                pathRow.appendChild(pathLabel);
                pathRow.appendChild(pathValue);
                pathRow.appendChild(controlsCell);

                let platformRow = document.createElement('tr');
                let platformLabel = document.createElement('td');
                platformLabel.innerHTML = 'Default Platform';
                let platformValue = document.createElement('td');
                platformValue.innerHTML = library.defaultPlatformName || 'n/a';
                platformRow.appendChild(platformLabel);
                platformRow.appendChild(platformValue);

                let libraryRow = document.createElement('tr');
                let libraryLabel = document.createElement('td');
                libraryLabel.innerHTML = 'Default Library';
                let libraryValue = document.createElement('td');
                libraryValue.innerHTML = library.isDefaultLibrary ? 'Yes' : 'No';
                libraryRow.appendChild(libraryLabel);
                libraryRow.appendChild(libraryValue);

                libraryTable.appendChild(pathRow);
                libraryTable.appendChild(platformRow);
                libraryTable.appendChild(libraryRow);

                if (library.pathInfo) {
                    let storageRow = document.createElement('tr');
                    let storageLabel = document.createElement('td');
                    storageLabel.colSpan = 3;
                    storageLabel.style.paddingTop = '10px';

                    let spaceUsedByLibrary = library.pathInfo.spaceUsed;
                    let spaceUsedByOthers = library.pathInfo.totalSpace - library.pathInfo.spaceAvailable;
                    storageLabel.appendChild(BuildSpaceBar(spaceUsedByLibrary, spaceUsedByOthers, library.pathInfo.totalSpace));
                    storageRow.appendChild(storageLabel);

                    libraryTable.appendChild(storageRow);

                    // let storageRow2 = document.createElement('tr');
                    // let storageLabel2 = document.createElement('td');
                    // storageLabel2.innerHTML = 'Space Used by Library: ' + formatBytes(spaceUsedByLibrary);

                    // let storageValue2 = document.createElement('td');
                    // storageValue2.innerHTML = 'Space Used by Others: ' + formatBytes(spaceUsedByOthers);

                    // let storageValue3 = document.createElement('td');
                    // storageValue3.innerHTML = 'Total Space: ' + formatBytes(library.pathInfo.totalSpace);

                    // storageRow2.appendChild(storageLabel2);
                    // storageRow2.appendChild(storageValue2);
                    // storageRow2.appendChild(storageValue3);

                    // libraryTable.appendChild(storageRow2);
                }

                body.appendChild(libraryTable);

                container.appendChild(header);
                container.appendChild(body);

                newTable.appendChild(container);
            }
        }
    );
}