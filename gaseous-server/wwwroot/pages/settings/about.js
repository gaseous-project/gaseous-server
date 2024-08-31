function populatePage() {
    let appVersionBox = document.getElementById('settings_appversion');
    if (AppVersion == "1.5.0.0") {
        appVersionBox.innerHTML = "Built from source";
    } else {
        appVersionBox.innerHTML = AppVersion;
    }

    let dbVersionBox = document.getElementById('settings_dbversion');
    dbVersionBox.innerHTML = DBSchemaVersion;
}