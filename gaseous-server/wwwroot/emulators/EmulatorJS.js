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
        console.log('Core data:', CoreData);

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
                            imgId = gameData.clearLogo[provider][0];
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
    exitEmulation: {
        visible: true,
        callback: async () => {
            // trigger a ram save before exiting
            await SaveRamCapture().then(() => {
                if (history.length > 1) {
                    history.back();
                } else if (document.referrer) {
                    window.location.href = document.referrer;
                } else {
                    window.location.href = '/';
                }
            });
        }
    },
    screenshotBtn: {
        visible: true,
        icon: '<svg version="1.1" viewBox="0 0 512 512" xmlns="http://www.w3.org/2000/svg" xmlns:svg="http://www.w3.org/2000/svg"><g id="g3"><path d="m 256,233.109 c -28.5,0 -51.594,23.297 -51.594,52.047 0,28.766 23.094,52.047 51.594,52.047 28.5,0 51.594,-23.281 51.594,-52.047 0,-28.75 -23.094,-52.047 -51.594,-52.047 z" id="path1"/><path d="m 497.375,140.297 c -8.984,-9.094 -21.641,-14.813 -35.453,-14.813 h -54.203 c -4.359,0.016 -8.438,-2.594 -10.313,-6.813 L 381.172,82.327 C 373.141,64.311 355.281,52.593 335.469,52.593 H 176.531 c -19.813,0 -37.672,11.719 -45.719,29.719 v 0.016 l -16.219,36.344 c -1.875,4.219 -5.953,6.828 -10.313,6.813 H 50.078 c -13.813,0 -26.484,5.719 -35.484,14.813 C 5.594,149.359 0,162.031 0,175.828 v 233.25 c 0,13.797 5.594,26.469 14.594,35.531 9,9.094 21.672,14.813 35.484,14.797 h 225.781 186.063 c 13.813,0.016 26.469,-5.703 35.453,-14.797 C 506.406,435.546 512,422.875 512,409.078 v -233.25 c 0,-13.797 -5.594,-26.484 -14.625,-35.531 z m -24.094,268.781 c 0,3.313 -1.281,6.125 -3.375,8.281 -2.156,2.109 -4.844,3.328 -7.984,3.344 H 275.859 50.078 c -3.156,-0.016 -5.859,-1.234 -7.984,-3.344 -2.094,-2.156 -3.375,-4.969 -3.375,-8.281 v -233.25 c 0,-3.313 1.281,-6.125 3.375,-8.281 2.125,-2.125 4.828,-3.328 7.984,-3.344 h 54.203 c 19.781,0 37.656,-11.734 45.688,-29.766 l 16.188,-36.328 c 1.906,-4.203 5.969,-6.813 10.375,-6.813 H 335.47 c 4.406,0 8.469,2.609 10.359,6.797 l 16.219,36.359 c 8.016,18.016 25.891,29.75 45.672,29.75 h 54.203 c 3.141,0.016 5.828,1.219 7.984,3.344 2.094,2.156 3.375,4.984 3.375,8.281 v 233.251 z" id="path2"/><path d="m 256,170.938 c -31.313,-0.016 -59.75,12.844 -80.203,33.5 -20.484,20.656 -33.172,49.266 -33.156,80.719 -0.016,31.453 12.672,60.094 33.156,80.719 20.453,20.672 48.891,33.516 80.203,33.516 31.297,0 59.75,-12.844 80.203,-33.516 20.484,-20.625 33.172,-49.266 33.156,-80.719 0.016,-31.453 -12.672,-60.063 -33.156,-80.719 C 315.75,183.781 287.297,170.922 256,170.938 Z m 59.031,173.953 c -15.172,15.297 -35.953,24.688 -59.031,24.688 -23.094,0 -43.859,-9.391 -59.047,-24.688 -15.141,-15.297 -24.5,-36.328 -24.516,-59.734 0.016,-23.391 9.375,-44.422 24.516,-59.734 15.188,-15.297 35.953,-24.672 59.047,-24.688 23.078,0.016 43.859,9.391 59.031,24.688 15.156,15.313 24.516,36.344 24.531,59.734 -0.015,23.406 -9.374,44.437 -24.531,59.734 z" id="path3"/><rect x="392.18799" y="197.65601" width="34.405998" height="34.405998" id="rect3"/></g></svg>',
        displayName: 'Screenshot',
        callback: async () => {
            let screenshotData = await EJS_emulator.takeScreenshot("canvas", "png", 1);
            let screenshot = {
                "ByteArrayBase64": btoa(Uint8ToString(screenshotData.screenshot)),
                "ContentType": "Screenshot",
                "Filename": `screenshot_${new Date().toISOString().replace(/[:.]/g, '-')}.png`
            }

            let url = `/api/v1.1/ContentManager/fileupload/bytearray?metadataid=${gameId}`;

            fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(screenshot)
            })
                .then(response => response.json())
                .then(result => {
                    console.log("Upload complete");
                    console.log(result);
                    // show a notification
                    const notification = new Notification(
                        'Screenshot Saved',
                        'Screenshot has been saved to the media library.',
                        `/api/v1.1/ContentManager/attachment/${result.attachmentId}/data`
                    );
                    notification.Show();
                })
                .catch(error => {
                    console.log("An error occurred");
                    console.log(error);
                });
        }
    }
}

EJS_onSaveState = function (e) {
    console.log(e);
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

// if core is arcade, mame, fbneo, fbalpha2012_cps1, fbalpha2012_cps2, then load audio samples if available
if (['arcade', 'mame', 'fbneo', 'fbalpha2012_cps1', 'fbalpha2012_cps2'].includes(getQueryString('core', 'string'))) {
    let pathName = undefined;
    switch (getQueryString('core', 'string')) {
        case 'arcade':
        case 'fbneo':
            pathName = '/fbneo/samples/';
            break;
        case 'mame':
            pathName = '/mame2003-plus/samples/';
            break;
        case 'fbalpha2012_cps1':
            pathName = '/fbalpha2012_cps1/samples/';
            break;
        case 'fbalpha2012_cps2':
            pathName = '/fbalpha2012_cps2/samples/';
            break;
    }

    if (pathName !== undefined) {
        let samplesArray = {};
        fetch(`/api/v1.1/ContentManager/?metadataids=${gameId}&contentTypes=AudioSample`, {
            method: 'GET'
        })
            .then(response => response.json())
            .then(async data => {
                console.log('Samples:', data);
                let sampleIds = '';
                for (let i = 0; i < data.items.length; i++) {
                    if (sampleIds !== '') {
                        sampleIds += ',';
                    }
                    sampleIds += data.items[i].attachmentId;
                }
                samplesArray[pathName] = `/api/v1.1/ContentManager/attachment/zipStream?attachmentIds=${sampleIds}`;

                console.log('Samples Array:', samplesArray);
                EJS_externalFiles = samplesArray;
            });
    }
}

// capture save RAM every minute
let saveRam = setInterval(async () => {
    await SaveRamCapture();
}, 30000);

async function SaveRamCapture() {
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
    await fetch(`/api/v1.1/SaveFile/${getQueryString('core', 'string')}/${IsMediaGroup}/${romId}`, {
        method: 'POST',
        body: JSON.stringify({ SaveByteArrayBase64: SaveByteArrayBase64 }),
        headers: {
            'Content-Type': 'application/json'
        }
    });
}