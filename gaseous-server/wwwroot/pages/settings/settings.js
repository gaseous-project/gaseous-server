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

            let metadataSettingsContainer = document.getElementById('settings_metadata');
            metadataSettingsContainer.innerHTML = '';
            result.metadataSources.forEach(element => {
                // section
                let sourceSection = document.createElement('div');
                sourceSection.classList.add('section');
                sourceSection.setAttribute('id', 'settings_metadatasource_' + element.source);

                // section header
                let sourceHeader = document.createElement('div');
                sourceHeader.classList.add('section-header');

                let sourceRadio = document.createElement('input');
                sourceRadio.setAttribute('type', 'radio');
                sourceRadio.setAttribute('name', 'settings_metadatasource');
                sourceRadio.setAttribute('value', element.source);
                sourceRadio.setAttribute('id', 'settings_metadatasource_' + element.source + '_radio');
                sourceRadio.style.margin = '0px';
                sourceRadio.style.height = 'unset';
                if (element.default) {
                    sourceRadio.checked = true;
                }

                let sourceLabel = document.createElement('label');
                sourceLabel.setAttribute('for', 'settings_metadatasource_' + element.source + '_radio');

                let sourceName = document.createElement('span');
                switch (element.source) {
                    case "IGDB":
                        sourceName.innerText = 'Internet Game Database (IGDB)';
                        break;

                    default:
                        sourceName.innerText = element.source;
                        break;
                }
                sourceName.style.marginLeft = '10px';
                sourceLabel.appendChild(sourceName);

                let sourceConfigured = document.createElement('span');
                sourceConfigured.style.float = 'right';
                sourceConfigured.classList.add(element.configured ? 'greentext' : 'redtext');
                sourceConfigured.innerText = element.configured ? 'Configured' : 'Not Configured';

                sourceHeader.appendChild(sourceRadio);
                sourceHeader.appendChild(sourceLabel);
                sourceHeader.appendChild(sourceConfigured);
                sourceSection.appendChild(sourceHeader);

                // section body
                let sourceContent = document.createElement('div');
                sourceContent.classList.add('section-body');
                if (element.usesProxy === false && element.usesClientIdAndSecret === false) {
                    sourceContent.innerText = 'No options to configure';
                } else {
                    // render controls
                    let controlsTable = document.createElement('table');
                    controlsTable.style.width = '100%';

                    // hasheous proxy row
                    if (element.usesProxy === true) {
                        let proxyRow = document.createElement('tr');

                        let proxyLabel = document.createElement('td');
                        if (element.usesClientIdAndSecret === true) {
                            let proxyRadio = document.createElement('input');
                            proxyRadio.id = 'settings_metadatasource_proxy_' + element.source;
                            proxyRadio.setAttribute('type', 'radio');
                            proxyRadio.setAttribute('name', 'settings_metadatasource_proxy_' + element.source);
                            proxyRadio.style.marginRight = '10px';
                            if (element.useHasheousProxy === true) {
                                proxyRadio.checked = true;
                            }
                            proxyLabel.appendChild(proxyRadio);

                            let proxyLabelLabel = document.createElement('label');
                            proxyLabelLabel.setAttribute('for', 'settings_metadatasource_proxy_' + element.source);

                            let proxyLabelSpan = document.createElement('span');
                            proxyLabelSpan.innerText = 'Use Hasheous Proxy';
                            proxyLabelLabel.appendChild(proxyLabelSpan);
                            proxyLabel.appendChild(proxyLabelLabel);

                            proxyRow.appendChild(proxyLabel);
                        } else {
                            proxyLabel.innerHTML = 'Uses Hasheous Proxy';
                            proxyRow.appendChild(proxyLabel);
                        }

                        controlsTable.appendChild(proxyRow);
                    }

                    // client id and secret row
                    if (element.usesClientIdAndSecret === true) {
                        if (element.usesProxy === true) {
                            let clientRadioRow = document.createElement('tr');

                            let clientRadioLabel = document.createElement('td');
                            let clientRadio = document.createElement('input');
                            clientRadio.id = 'settings_metadatasource_client_' + element.source;
                            clientRadio.setAttribute('type', 'radio');
                            clientRadio.setAttribute('name', 'settings_metadatasource_proxy_' + element.source);
                            clientRadio.style.marginRight = '10px';
                            if (element.useHasheousProxy === false) {
                                clientRadio.checked = true;
                            }
                            clientRadioLabel.appendChild(clientRadio);

                            let clientRadioLabelLabel = document.createElement('label');
                            clientRadioLabelLabel.setAttribute('for', 'settings_metadatasource_client_' + element.source);

                            let clientRadioLabelSpan = document.createElement('span');
                            clientRadioLabelSpan.innerText = 'Direct connection';
                            clientRadioLabelLabel.appendChild(clientRadioLabelSpan);
                            clientRadioLabel.appendChild(clientRadioLabelLabel);

                            clientRadioRow.appendChild(clientRadioLabel);

                            controlsTable.appendChild(clientRadioRow);
                        }

                        let clientIdTable = document.createElement('table');
                        clientIdTable.style.width = '100%';
                        if (element.usesProxy === true) {
                            clientIdTable.style.marginLeft = '30px';
                        }

                        let clientIdRow = document.createElement('tr');

                        let clientIdLabel = document.createElement('td');
                        clientIdLabel.style.width = '15%';
                        clientIdLabel.innerText = 'Client ID';
                        clientIdRow.appendChild(clientIdLabel);

                        let clientIdInput = document.createElement('td');
                        let clientIdInputField = document.createElement('input');
                        clientIdInputField.style.width = '90%';
                        clientIdInputField.setAttribute('type', 'text');
                        clientIdInputField.setAttribute('id', 'settings_metadatasource_' + element.source + '_clientid');
                        clientIdInputField.value = element.clientId;
                        clientIdInput.appendChild(clientIdInputField);
                        clientIdRow.appendChild(clientIdInput);

                        clientIdTable.appendChild(clientIdRow);

                        let clientSecretRow = document.createElement('tr');

                        let clientSecretLabel = document.createElement('td');
                        clientSecretLabel.style.width = '15%';
                        clientSecretLabel.innerText = 'Client Secret';
                        clientSecretRow.appendChild(clientSecretLabel);

                        let clientSecretInput = document.createElement('td');
                        let clientSecretInputField = document.createElement('input');
                        clientSecretInputField.style.width = '90%';
                        clientSecretInputField.setAttribute('type', 'text');
                        clientSecretInputField.setAttribute('id', 'settings_metadatasource_' + element.source + '_clientsecret');
                        clientSecretInputField.value = element.secret;
                        clientSecretInput.appendChild(clientSecretInputField);
                        clientSecretRow.appendChild(clientSecretInput);

                        clientIdTable.appendChild(clientSecretRow);

                        controlsTable.appendChild(clientIdTable);
                    }


                    sourceContent.appendChild(controlsTable);
                }
                sourceSection.appendChild(sourceContent);

                metadataSettingsContainer.appendChild(sourceSection);
            });

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

    let metadataSources = [];
    let metadataSourceRadios = $("input[type='radio'][name='settings_metadatasource']");
    metadataSourceRadios.each(function (index, element) {
        let source = $(element).val();
        let useHasheousProxy = false;
        let clientId = '';
        let secret = '';
        if (source == "IGDB") {
            let igdbClientId = document.getElementById('settings_metadatasource_' + source + '_clientid').value;
            let igdbClientSecret = document.getElementById('settings_metadatasource_' + source + '_clientsecret').value;
            if (igdbClientId && igdbClientSecret) {
                clientId = igdbClientId;
                secret = igdbClientSecret;
            }
        }

        let useHasheousProxyRadio = $("input[type='radio'][id='settings_metadatasource_proxy_" + source + "']:checked");
        if (useHasheousProxyRadio.length > 0) {
            useHasheousProxy = true;
        } else {
            useHasheousProxy = false;
        }

        let metadataSource = {
            "Source": source,
            "UseHasheousProxy": useHasheousProxy,
            "ClientId": clientId,
            "Secret": secret,
            "Default": $(element).is(':checked')
        };
        metadataSources.push(metadataSource);
    });

    let model = {
        "alwaysLogToDisk": alwaysLogToDisk,
        "minimumLogRetentionPeriod": Number(retentionValue),
        "emulatorDebugMode": document.getElementById('settings_emulator_debug').checked,
        "metadataSources": metadataSources,
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