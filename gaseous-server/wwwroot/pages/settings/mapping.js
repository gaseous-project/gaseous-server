function loadPlatformMapping(Overwrite) {
    let queryString = '';
    if (Overwrite == true) {
        console.log('Overwriting PlatformMap.json');
        queryString = '?ResetToDefault=true';
    }

    ajaxCall(
        '/api/v1.1/PlatformMaps' + queryString,
        'GET',
        function (result) {
            let newTable = document.getElementById('settings_mapping_table');
            newTable.innerHTML = '';
            newTable.appendChild(
                createTableRow(
                    true,
                    [
                        'Platform',
                        'Supported File Extensions',
                        'Unique File Extensions',
                        'Has Web Emulator'
                    ],
                    '',
                    ''
                )
            );

            for (let i = 0; i < result.length; i++) {
                let hasWebEmulator = '';
                if (result[i].webEmulator.type.length > 0) {
                    hasWebEmulator = 'Yes';
                }

                let platformLink = '';
                if (userProfile.roles.includes("Admin")) {
                    platformLink = '<a href="#/" onclick="ShowPlatformMappingDialog(' + result[i].igdbId + ');" class="romlink">' + result[i].igdbName + '</a>';
                } else {
                    platformLink = result[i].igdbName;
                }

                let newRow = [
                    platformLink,
                    result[i].extensions.supportedFileExtensions.join(', '),
                    result[i].extensions.uniqueFileExtensions.join(', '),
                    hasWebEmulator
                ];

                newTable.appendChild(createTableRow(false, newRow, 'romrow', 'romcell logs_table_cell'));
            }
        }
    );
}

function DownloadJSON() {
    window.location = '/api/v1.1/PlatformMaps/PlatformMap.json';
}

function SetupButtons() {
    if (userProfile.roles.includes("Admin")) {
        document.getElementById('settings_mapping_import').style.display = '';

        document.getElementById('uploadjson').addEventListener('change', function () {
            $(this).simpleUpload("/api/v1.1/PlatformMaps", {
                start: function (file) {
                    //upload started
                    console.log("JSON upload started");
                },
                success: function (data) {
                    //upload successful
                    window.location.reload();
                }
            });
        });

        document.getElementById('importjson').addEventListener('click', openDialog);
    }
}

function openDialog() {
    document.getElementById('uploadjson').click();
}