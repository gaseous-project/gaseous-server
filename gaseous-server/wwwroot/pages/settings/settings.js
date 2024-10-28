function getSystemSettings() {
    ajaxCall(
        '/api/v1.1/System/Settings/System',
        'GET',
        function (result) {
            console.log(result);
            let optionToSelect = 'settings_logs_write_db';
            if (result.alwaysLogToDisk == true) {
                optionToSelect = 'settings_logs_write_fs';
            }
            document.getElementById(optionToSelect).checked = true;

            document.getElementById('settings_logs_retention').value = result.minimumLogRetentionPeriod;

            document.getElementById('settings_emulator_debug').checked = result.emulatorDebugMode;

            switch (result.signatureSource.source) {
                case "LocalOnly":
                    document.getElementById('settings_signaturesource_local').checked = true;
                    break;

                case "Hasheous":
                    document.getElementById('settings_signaturesource_hasheous').checked = true;
                    break;

            }

            switch (result.metadataSource.source) {
                case "None":
                    document.getElementById('settings_metadatasource_none').checked = true;
                    break;

                case "Hasheous":
                    document.getElementById('settings_metadatasource_hasheous').checked = true;
                    break;

                case "IGDB":
                    document.getElementById('settings_metadatasource_igdb').checked = true;
                    break;

            }

            document.getElementById('settings_igdbclientid').value = result.metadataSource.igdbClientId;
            document.getElementById('settings_igdbclientkey').value = result.metadataSource.igdbClientSecret;

            document.getElementById('settings_signaturesource_hasheoushost').value = result.signatureSource.hasheousHost;

            let hasheousSubmitCheck = document.getElementById('settings_hasheoussubmit');
            if (result.signatureSource.hasheousSubmitFixes == true) {
                hasheousSubmitCheck.checked = true;
            }
            document.getElementById('settings_hasheousapikey').value = result.signatureSource.hasheousAPIKey;
            toggleHasheousAPIKey(hasheousSubmitCheck);
        }
    );
}

function setSystemSettings() {
    let alwaysLogToDisk = false;
    if ($("input[type='radio'][name='settings_logs_write']:checked").val() == "true") {
        alwaysLogToDisk = true;
    }

    let retention = document.getElementById('settings_logs_retention');
    let retentionValue = 0;
    if (retention.value) {
        retentionValue = retention.value;
    } else {
        retentionValue = 7;
    }

    let model = {
        "alwaysLogToDisk": alwaysLogToDisk,
        "minimumLogRetentionPeriod": Number(retentionValue),
        "emulatorDebugMode": document.getElementById('settings_emulator_debug').checked,
        "metadataSource": {
            "Source": $("input[type='radio'][name='settings_metadatasource']:checked").val(),
            "IgdbClientId": document.getElementById('settings_igdbclientid').value,
            "IgdbClientSecret": document.getElementById('settings_igdbclientkey').value
        },
        "signatureSource": {
            "Source": $("input[type='radio'][name='settings_signaturesource']:checked").val(),
            "HasheousHost": document.getElementById('settings_signaturesource_hasheoushost').value,
            "HasheousAPIKey": document.getElementById('settings_hasheousapikey').value,
            "HasheousSubmitFixes": document.getElementById('settings_hasheoussubmit').checked
        }
    };
    console.log(model);

    ajaxCall(
        '/api/v1.1/System/Settings/System',
        'POST',
        function (result) {
            getSystemSettings();
        },
        function (error) {
            getSystemSettings();
        },
        JSON.stringify(model)
    );
}

function toggleHasheousAPIKey(checkbox) {
    let settings_hasheousapikey_row = document.getElementById('settings_hasheousapikey_row');
    if (checkbox.checked == true) {
        settings_hasheousapikey_row.style.display = '';
    } else {
        settings_hasheousapikey_row.style.display = 'none';
    }
}