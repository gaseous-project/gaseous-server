var gameId = getQueryString('gameid', 'int');
var romId = getQueryString('romid', 'int');
var platformId = getQueryString('platformid', 'int');
var IsMediaGroupInt = getQueryString('mediagroup', 'int');
var IsMediaGroup = false;

var StateUrl = undefined;

var gameData;
var contentSource;
var artworks = null;
var artworksPosition = 0;

var emuGameTitle = '';
var emuBios = '';
var emuBackground = '';

// statistics
var SessionId = undefined;

async function SetupPage() {
    if (IsMediaGroupInt == 1) { IsMediaGroup = true; }
    if (getQueryString('stateid', 'int')) {
        StateUrl = '/api/v1.1/StateManager/' + romId + '/' + getQueryString('stateid', 'int') + '/State/savestate.state?StateOnly=true&IsMediaGroup=' + IsMediaGroup;
    }

    console.log(window.lang.translate('console.loading_rom_url', [decodeURIComponent(getQueryString('rompath', 'string'))]));

    try {
    const res = await fetch('/api/v1.1/Games/' + gameId, { method: 'GET' });
    if (!res.ok) throw new Error(window.lang.translate('console.failed_load_game', [res.status, res.statusText]));
        const result = await res.json();

        gameData = result;
        contentSource = gameData.metadataSource;

        if (result.cover) {
            emuBackground = '/api/v1.1/Games/' + gameId + '/' + contentSource + '/cover/' + result.cover + '/image/original/' + result.cover + '.jpg';
        }

        emuGameTitle = gameData.name;
    } catch (err) {
        console.error(window.lang.translate('console.error_fetching_game_data'), err);
    }

    try {
    const res = await fetch('/api/v1.1/Bios/' + platformId, { method: 'GET' });
    if (!res.ok) throw new Error(window.lang.translate('console.failed_load_bios', [res.status, res.statusText]));
        const result = await res.json();

        if (Array.isArray(result) && result.length === 0) {
            emuBios = '';
        } else {
            emuBios = '/api/v1.1/Bios/zip/' + platformId + '/' + gameId + '?filtered=true';
            console.log(window.lang.translate('console.using_bios_link', [emuBios]));
        }

        switch (getQueryString('engine', 'string')) {
            case 'EmulatorJS':
                console.log(window.lang.translate('console.emulator_engine', [getQueryString('engine', 'string')]));
                console.log(window.lang.translate('console.emulator_core', [getQueryString('core', 'string')]));
                $('#emulator').load('/emulators/EmulatorJS.html?v=' + AppVersion);
                break;
        }
    } catch (e) {
        console.error(window.lang.translate('console.error_fetching_bios'), e);
        emuBios = '';
    }

    setInterval(SaveStatistics, 60000);
}

function rotateBackground() {
    if (artworks) {
        artworksPosition += 1;
        if (artworks[artworksPosition] == null) {
            artworksPosition = 0;
        }
        var bg = document.getElementById('bgImage');
        bg.setAttribute('style', 'background-image: url("/api/v1.1/Games/' + gameId + '/' + contentSource + '/artwork/' + artworks[artworksPosition] + '/image/original/' + artworks[artworksPosition] + '.jpg"); background-position: center; background-repeat: no-repeat; background-size: cover; filter: blur(10px); -webkit-filter: blur(10px);');
    }
}

function SaveStatistics() {
    var model;
    if (SessionId == undefined) {
        (async () => {
            try {
                const res = await fetch('/api/v1.1/Statistics/Games/' + gameId + '/' + platformId + '/' + romId + '?IsMediaGroup=' + IsMediaGroup, {
                    method: 'POST'
                });
                if (!res.ok) throw new Error(window.lang.translate('console.failed_create_session', [res.status, res.statusText]));
                const data = await res.json();
                SessionId = data.sessionId;
            } catch (err) {
                console.error(window.lang.translate('console.error_creating_statistics_session'), err);
            }
        })();
    } else {
        (async () => {
            try {
                const res = await fetch(
                    '/api/v1.1/Statistics/Games/' + gameId + '/' + platformId + '/' + romId + '/' + SessionId + '?IsMediaGroup=' + IsMediaGroup,
                    { method: 'PUT' }
                );
                if (!res.ok) throw new Error(window.lang.translate('console.failed_update_statistics', [res.status, res.statusText]));
            } catch (err) {
                console.error(window.lang.translate('console.error_updating_statistics_session'), err);
            }
        })();
    }
}

SetupPage();