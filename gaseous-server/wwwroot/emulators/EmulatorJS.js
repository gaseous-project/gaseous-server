EJS_player = '#game';

// Can also be fceumm or nestopia
EJS_core = getQueryString('core', 'string');

// get core data from cores.json
async function GetCoreData(coreName) {
    let response = await fetch('/emulators/EmulatorJS/data/cores/cores.json');
    if (!response.ok) {
        throw new Error('Network response was not ok');
    }
    let data = await response.json();

    for (let i = 0; i < data.length; i++) {
        if (data[i].name === coreName) {
            return data[i];
        }
    }

    return null; // Return null if core not found
}

let CoreData = null;
// Ensure a safe default before loader.js reads it (we will override before loading loader.js)
EJS_threads = false;

// Inject loader.js only after CoreData is resolved and EJS_threads is finalized
function injectLoaderOnce() {
    if (document.querySelector('script[data-ejs-loader]')) return; // already injected
    const s = document.createElement('script');
    s.src = '/emulators/EmulatorJS/data/loader.js';
    s.async = false; // preserve execution order
    s.setAttribute('data-ejs-loader', 'true');
    document.head.appendChild(s);
}

// Promise-based initialization to fetch cores.json and configure threading
GetCoreData(EJS_core)
    .then(data => {
        CoreData = data;
        console.log(CoreData);

        if (typeof SharedArrayBuffer !== 'undefined') {
            const requireThreads = !!(CoreData && CoreData.options && CoreData.options.requireThreads === true);
            EJS_threads = requireThreads;
            console.log(requireThreads ? 'Threads enabled for this core.' : 'Threads disabled for this core.');
        } else {
            // SharedArrayBuffer is not supported, disable threads
            EJS_threads = false;
            console.log('SharedArrayBuffer unsupported. Threads disabled.');
        }

        // Notify listeners that CoreData is ready (optional)
        try {
            document.dispatchEvent(new CustomEvent('coredata:ready', { detail: CoreData }));
        } catch (_) { }
    })
    .catch(err => {
        console.error('Failed to load core data', err);
        // Keep safe defaults
        EJS_threads = false;
    })
    .finally(() => {
        // After we have decided EJS_threads (even on failure), load the emulator loader
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', injectLoaderOnce, { once: true });
        } else {
            injectLoaderOnce();
        }
    });

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

let srmVersion = getQueryString('srmVersion', 'string');
if (!srmVersion) {
    srmVersion = 'latest';
}

console.log(gameData);

EJS_Buttons = {
    pause: {
        callback: () => {
            let bgMask = document.createElement('div');
            bgMask.id = 'pause-mask';
            bgMask.classList.add('emulator_pausemask');

            // decorate the mask
            if (gameData) {
                let imgType = undefined;
                let imgId = undefined;
                let imgProvider = undefined;
                let imgClass = undefined;
                let showLabel = false;
                // add the clear logo if available - fallback to cover if not
                if (gameData.clearLogo) {
                    for (const provider of logoProviders) {
                        if (gameData.clearLogo[provider]) {
                            imgType = 'clearlogo';
                            imgId = gameData.clearLogo[provider];
                            imgProvider = provider;
                            break;
                        }
                    }
                }

                if (imgId === undefined && gameData.cover) {
                    imgType = 'cover';
                    imgId = gameData.cover;
                    imgProvider = gameData.metadataSource;
                    imgClass = 'emulator_pausemask-coverimage';
                    showLabel = true;
                }

                if (imgType && imgId) {
                    let imgUrl = `/api/v1.1/Games/${gameId}/${imgProvider}/${imgType}/${imgId}/image/original/${imgId}.png`;

                    let gameImage = document.createElement('img');
                    gameImage.src = imgUrl;
                    gameImage.alt = 'Game Image';
                    gameImage.classList.add('emulator_pausemask-image');
                    if (imgClass) {
                        gameImage.classList.add(imgClass);
                    }
                    bgMask.appendChild(gameImage);
                }

                if (showLabel && gameData.name) {
                    let gameLabel = document.createElement('div');
                    gameLabel.id = 'emulator_pausemask-label';
                    gameLabel.classList.add('emulator_pausemask-label');
                    gameLabel.textContent = gameData.name;
                    bgMask.appendChild(gameLabel);
                }

                // paused label
                let pausedLabel = document.createElement('div');
                pausedLabel.id = 'paused-label';
                pausedLabel.classList.add('emulator_pausemask-pausedlabel');
                pausedLabel.classList.add('emulator_pausemask-pausedlabel-flash');
                pausedLabel.textContent = '*** Paused ***';
                bgMask.appendChild(pausedLabel);

                // resume label
                let resumeLabel = document.createElement('div');
                resumeLabel.id = 'resume-label';
                resumeLabel.classList.add('emulator_pausemask-resumelabel');
                resumeLabel.textContent = 'Click to Resume';
                bgMask.appendChild(resumeLabel);
            }

            bgMask.addEventListener('click', () => {
                let pauseMask = document.getElementById('pause-mask');
                if (pauseMask) {
                    pauseMask.remove();
                }

                EJS_emulator.gameManager.EJS.play();
            });

            let emulatorContainer = document.querySelector(EJS_player);
            emulatorContainer.appendChild(bgMask);
        }
    },
    play: {
        callback: () => {
            alert('Play button clicked!'); // Placeholder for play button action
        }
    },
    cacheManager: false,
    exitEmulation: false
}

EJS_onSaveState = function (e) {
    let returnValue = {
        "ScreenshotByteArrayBase64": btoa(Uint8ToString(e.screenshot)),
        "StateByteArrayBase64": btoa(Uint8ToString(e.state))
    };

    let url = '/api/v1.1/StateManager/' + romId + '?IsMediaGroup=' + IsMediaGroup;

    fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(returnValue)
    })
        .then(response => response.json())
        .then(result => {
            console.log("Upload complete");
            console.log(result);

            const notification = new Notification(
                'State Saved',
                'Game state has been saved.',
                '/api/v1.1/StateManager/' + romId + '/' + result.value.id + '/Screenshot/image.png?IsMediaGroup=' + IsMediaGroup
            );
            notification.Show();
        })
        .catch(error => {
            console.log("An error occurred");
            console.log(error);
        });
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
    let url = `/api/v1.1/SaveFile/${getQueryString('core', 'string')}/${IsMediaGroup}/${romId}/${srmVersion}/data?format=${format}`;

    // fetch the save file
    let response = await fetch(url, {
        method: 'GET'
    });

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
}, 30000);