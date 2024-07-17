let biosDict = {};

function loadBios() {
    ajaxCall('/api/v1.1/Bios', 'GET', function (result) {
        result.sort((a, b) => a.platformname.charCodeAt(0) - b.platformname.charCodeAt(0));

        // sort into a dictionary
        for (let i = 0; i < result.length; i++) {
            let tempArray = [];
            if (biosDict.hasOwnProperty(result[i].platformname)) {
                tempArray = biosDict[result[i].platformname];
                tempArray.push(result[i]);
            } else {
                tempArray.push(result[i]);
                biosDict[result[i].platformname] = tempArray;
            }

            biosDict[result[i].platformname] = tempArray;
        }

        displayFirmwareList();
    });
}

function displayFirmwareList() {
    let lastPlatform = '';

    let newTable = document.getElementById('table_firmware');
    newTable.innerHTML = '';
    newTable.appendChild(createTableRow(true, ['Description', 'File name', 'MD5 Hash', 'Available']));

    let totalAvailable = 0;
    let totalCount = 0;

    for (const [key, value] of Object.entries(biosDict)) {
        // new platform - show a header
        let platformRow = document.createElement('tr');
        let platformHeader = document.createElement('th');
        platformHeader.setAttribute('colspan', 4);

        let platformHeaderValue = document.createElement('span');
        platformHeaderValue.innerHTML = key;
        platformHeader.appendChild(platformHeaderValue);

        let platformHeaderCounter = document.createElement('span');
        platformHeaderCounter.style.float = 'right';
        platformHeader.appendChild(platformHeaderCounter);

        platformRow.appendChild(platformHeader);
        newTable.appendChild(platformRow);

        let totalPlatformAvailable = 0;

        let showAvailable = document.getElementById('firmware_showavailable').checked;
        let showUnavailable = document.getElementById('firmware_showunavailable').checked;

        for (let i = 0; i < value.length; i++) {
            // update counters
            if (value[i].available == true) {
                totalAvailable += 1;
                totalPlatformAvailable += 1;
            }

            if (
                (value[i].available == true && showAvailable == true) ||
                (value[i].available == false && showUnavailable == true)
            ) {
                let biosFilename = document.createElement('a');
                biosFilename.href = '/api/v1.1/Bios/' + value[i].platformid + '/' + value[i].filename;
                biosFilename.innerHTML = value[i].filename;
                biosFilename.className = 'romlink';

                let availableText = document.createElement('span');
                if (value[i].available == true) {
                    availableText.innerHTML = 'Available';
                    availableText.className = 'greentext';

                    biosFilename = document.createElement('a');
                    biosFilename.href = '/api/v1.1/Bios/' + value[i].platformid + '/' + value[i].filename;
                    biosFilename.innerHTML = value[i].filename;
                    biosFilename.className = 'romlink';
                } else {
                    availableText.innerHTML = 'Unavailable';
                    availableText.className = 'redtext';

                    biosFilename = document.createElement('span');
                    biosFilename.innerHTML = value[i].filename;
                }

                let newRow = [
                    value[i].description,
                    biosFilename,
                    value[i].hash,
                    availableText
                ];
                newTable.appendChild(createTableRow(false, newRow, 'romrow', 'romcell'));
            }
            totalCount += 1;
        }

        platformHeaderCounter.innerHTML = totalPlatformAvailable + ' / ' + value.length + ' available';
    }

    document.getElementById('firmware_totalcount').innerHTML = totalAvailable + ' / ' + totalCount + ' available';
}