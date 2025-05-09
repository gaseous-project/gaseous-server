EJS_player = '#game';

// Can also be fceumm or nestopia
EJS_core = getQueryString('core', 'string');

// Lightgun
EJS_lightgun = false; // can be true or false

// URL to BIOS file
EJS_biosUrl = emuBios;

// URL to Game rom
EJS_gameUrl = decodeURIComponent(getQueryString('rompath', 'string'));

// load state if defined
if (StateUrl) {
    console.log('Loading saved state from: ' + StateUrl);
    EJS_loadStateURL = StateUrl;
}

// start the emulator automatically when loaded
EJS_startOnLoaded = true;

// Path to the data directory
EJS_pathtodata = '/emulators/EmulatorJS/data/';

EJS_DEBUG_XX = emulatorDebugMode;
console.log("Debug enabled: " + EJS_DEBUG_XX);

EJS_backgroundImage = emuBackground;
EJS_backgroundBlur = true;

EJS_fullscreenOnLoaded = false;

EJS_gameName = emuGameTitle;

if (typeof SharedArrayBuffer !== 'undefined') {
    if (getQueryString('core', 'string') === "ppsspp") {
        EJS_threads = true;
    }
}

EJS_Buttons = {
    exitEmulation: false
}

EJS_onSaveState = function (e) {
    var returnValue = {
        "ScreenshotByteArrayBase64": btoa(Uint8ToString(e.screenshot)),
        "StateByteArrayBase64": btoa(Uint8ToString(e.state))
    };

    var url = '/api/v1.1/StateManager/' + romId + '?IsMediaGroup=' + IsMediaGroup;

    ajaxCall(
        url,
        'POST',
        function (result) {
            console.log("Upload complete");
            console.log(result);

            const notification = new Notification('State Saved', 'Game state has been saved.', '/api/v1.1/StateManager/' + romId + '/' + result.value.id + '/Screenshot/image.png?IsMediaGroup=' + IsMediaGroup);
            notification.Show();
        },
        function (error) {
            console.log("An error occurred");
            console.log(error);
        },
        JSON.stringify(returnValue)
    );

    returnValue = undefined;
}

EJS_onLoadState = function (e) {
    let rompath = decodeURIComponent(getQueryString('rompath', 'string'));
    rompath = rompath.substring(rompath.lastIndexOf('/') + 1);
    console.log(rompath);
    let stateManager = new EmulatorStateManager(romId, IsMediaGroup, getQueryString('engine', 'string'), getQueryString('core', 'string'), platformId, gameId, rompath);
    stateManager.open();
}

EJS_onGameStart = async function (e) {
    // check if a save file is available
    let format = 'base64';
    let url = `/api/v1.1/SaveFile/${getQueryString('core', 'string')}/${IsMediaGroup}/${romId}/latest/data?format=${format}`;

    // fetch the save file
    let response = await fetch(url, {
        method: 'GET'
    });

    if (!response.ok) {
        console.log("No save file found");
        return;
    }

    // process the response
    let binData;

    switch (format) {
        case 'raw':
            let arrayBuffer = await response.arrayBuffer();
            binData = new Int8Array(arrayBuffer);
            break;

        case 'base64':
            let base64String = await response.text();
            let binaryString = atob(base64String);
            binData = new Int8Array(binaryString.length);
            for (let i = 0; i < binaryString.length; i++) {
                binData[i] = binaryString.charCodeAt(i);
            }
            break;

        default:
            console.log("Unknown format: " + format);
            return;
    }

    // if (binData !== undefined) {
    console.log("Save file found");
    console.log(binData);

    // set up the save file location
    let FS = EJS_emulator.gameManager.FS;
    let path = EJS_emulator.gameManager.getSaveFilePath();
    let paths = path.split("/");
    let savePath = "";
    for (let i = 0; i < paths.length - 1; i++) {
        if (paths[i] === "") continue;
        savePath += "/" + paths[i];
        if (!FS.analyzePath(savePath).exists) FS.mkdir(savePath);
    }

    // check if the save file exists, and remove it if it does
    if (FS.analyzePath(path).exists) FS.unlink(path);

    // write the save file
    FS.writeFile(path, binData);

    // load the save file
    EJS_emulator.gameManager.loadSaveFiles();
    // }
}

// capture save RAM every minute
let saveRam = setInterval(() => {
    // check if the emulator is running
    if (EJS_emulator.gameManager === undefined) {
        return;
    }

    // saves the save file to indexedDB
    EJS_emulator.gameManager.saveSaveFiles();

    // get the save file
    let saveFile = EJS_emulator.gameManager.getSaveFile();
    if (saveFile === null) {
        // no save file
        return;
    }
    let SaveByteArrayBase64 = btoa(String.fromCharCode(...saveFile));

    // upload the save to the server
    fetch(`/api/v1.1/SaveFile/${getQueryString('core', 'string')}/${IsMediaGroup}/${romId}`, {
        method: 'POST',
        body: JSON.stringify({ SaveByteArrayBase64: SaveByteArrayBase64 }),
        headers: {
            'Content-Type': 'application/json'
        }
    });
}, 60000);