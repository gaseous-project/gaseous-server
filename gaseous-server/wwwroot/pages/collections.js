function SetupPage() {
    var newCollectionButton = document.getElementById('collection_new');
    if (userProfile.roles.includes("Admin") || userProfile.roles.includes("Gamer")) {
        newCollectionButton.style.display = '';
    } else {
        newCollectionButton.style.display = 'none';
    }

    GetCollections();
}

function GetCollections() {
    ajaxCall('/api/v1.1/Collections', 'GET', function (result) {
        if (result) {
            var targetDiv = document.getElementById('collection_table_location');
            targetDiv.innerHTML = '';

            var newTable = document.createElement('table');
            newTable.id = 'romtable';
            newTable.className = 'romtable';
            newTable.setAttribute('cellspacing', 0);
            newTable.appendChild(createTableRow(true, ['Name', 'Description', 'Download Status', 'Size', '']));

            for (var i = 0; i < result.length; i++) {
                var statusText = result[i].buildStatus;
                var downloadLink = '';
                var packageSize = '-';
                var inProgress = false;
                switch (result[i].buildStatus) {
                    case 'NoStatus':
                        statusText = '-';
                        break;
                    case "WaitingForBuild":
                        statusText = 'Build pending';
                        inProgress = true;
                        break;
                    case "Building":
                        statusText = 'Building';
                        inProgress = true;
                        break;
                    case "Completed":
                        statusText = 'Available';
                        downloadLink = '<a href="/api/v1.1/Collections/' + result[i].id + '/Roms/Zip" class="romlink"><img src="/images/download.svg" class="banner_button_image" alt="Download" title="Download" /></a>';
                        packageSize = formatBytes(result[i].collectionBuiltSizeBytes);
                        break;
                    case "Failed":
                        statusText = 'Build error';
                        break;
                    default:
                        statusText = result[i].buildStatus;
                        break;
                }

                if (inProgress == true) {
                    setTimeout(GetCollections, 10000);
                }

                var editButton = '';
                var deleteButton = '';

                if (userProfile.roles.includes("Admin") || userProfile.roles.includes("Gamer")) {
                    editButton = '<a href="#" onclick="showDialog(\'collectionedit\', ' + result[i].id + ');" class="romlink"><img src="/images/edit.svg" class="banner_button_image" alt="Edit" title="Edit" /></a>';

                    deleteButton = '<a href="#" onclick="showSubDialog(\'collectiondelete\', ' + result[i].id + ');" class="romlink"><img src="/images/delete.svg" class="banner_button_image" alt="Delete" title="Delete" /></a>';
                }

                var newRow = [
                    result[i].name,
                    result[i].description,
                    statusText,
                    packageSize,
                    '<div style="text-align: right;">' + downloadLink + editButton + deleteButton + '</div>'
                ];

                newTable.appendChild(createTableRow(false, newRow, 'romrow', 'romcell'));
            }

            targetDiv.appendChild(newTable);
        }
    });
}