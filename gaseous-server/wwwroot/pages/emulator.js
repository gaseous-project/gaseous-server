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

    console.log("Loading rom url: " + decodeURIComponent(getQueryString('rompath', 'string')));

    try {
        const res = await fetch('/api/v1.1/Games/' + gameId, { method: 'GET' });
        if (!res.ok) throw new Error('Failed to load game: ' + res.status + ' ' + res.statusText);
        const result = await res.json();

        gameData = result;
        contentSource = gameData.metadataSource;

        if (result.cover) {
            emuBackground = '/api/v1.1/Games/' + gameId + '/' + contentSource + '/cover/' + result.cover + '/image/original/' + result.cover + '.jpg';
        }

        emuGameTitle = gameData.name;
    } catch (err) {
        console.error('Error fetching game data', err);
    }

    try {
        const res = await fetch('/api/v1.1/Bios/' + platformId, { method: 'GET' });
        if (!res.ok) throw new Error('Failed to load BIOS: ' + res.status + ' ' + res.statusText);
        const result = await res.json();

        if (Array.isArray(result) && result.length === 0) {
            emuBios = '';
        } else {
            emuBios = '/api/v1.1/Bios/zip/' + platformId + '/' + gameId + '?filtered=true';
            console.log("Using BIOS link: " + emuBios);
        }

        switch (getQueryString('engine', 'string')) {
            case 'EmulatorJS':
                console.log("Emulator: " + getQueryString('engine', 'string'));
                console.log("Core: " + getQueryString('core', 'string'));
                $('#emulator').load('/emulators/EmulatorJS.html?v=' + AppVersion);
                break;
        }
    } catch (e) {
        console.error('Error fetching BIOS', e);
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
                if (!res.ok) throw new Error('Failed to create session: ' + res.status + ' ' + res.statusText);
                const data = await res.json();
                SessionId = data.sessionId;
            } catch (err) {
                console.error('Error creating statistics session', err);
            }
        })();
    } else {
        (async () => {
            try {
                const res = await fetch(
                    '/api/v1.1/Statistics/Games/' + gameId + '/' + platformId + '/' + romId + '/' + SessionId + '?IsMediaGroup=' + IsMediaGroup,
                    { method: 'PUT' }
                );
                if (!res.ok) throw new Error('Failed to update statistics: ' + res.status + ' ' + res.statusText);
            } catch (err) {
                console.error('Error updating statistics session', err);
            }
        })();
    }
}

SetupPage();