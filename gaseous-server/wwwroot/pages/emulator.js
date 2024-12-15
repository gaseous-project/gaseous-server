var gameId = getQueryString('gameid', 'int');
var romId = getQueryString('romid', 'int');
var platformId = getQueryString('platformid', 'int');
var IsMediaGroupInt = getQueryString('mediagroup', 'int');
var IsMediaGroup = false;

var StateUrl = undefined;

var gameData;
var artworks = null;
var artworksPosition = 0;

var emuGameTitle = '';
var emuBios = '';
var emuBackground = '';

// statistics
var SessionId = undefined;

function SetupPage() {
    if (IsMediaGroupInt == 1) { IsMediaGroup = true; }
    if (getQueryString('stateid', 'int')) {
        StateUrl = '/api/v1.1/StateManager/' + romId + '/' + getQueryString('stateid', 'int') + '/State/savestate.state?StateOnly=true&IsMediaGroup=' + IsMediaGroup;
    }

    console.log("Loading rom url: " + decodeURIComponent(getQueryString('rompath', 'string')));

    ajaxCall('/api/v1.1/Games/' + gameId, 'GET', function (result) {
        gameData = result;

        if (result.cover) {
            emuBackground = '/api/v1.1/Games/' + gameId + '/cover/' + result.cover + '/image/original/' + result.cover + '.jpg';
        }

        emuGameTitle = gameData.name;
    });

    ajaxCall('/api/v1.1/Bios/' + platformId, 'GET', function (result) {
        if (result.length == 0) {
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
    });

    setInterval(SaveStatistics, 60000);
}

function rotateBackground() {
    if (artworks) {
        artworksPosition += 1;
        if (artworks[artworksPosition] == null) {
            artworksPosition = 0;
        }
        var bg = document.getElementById('bgImage');
        bg.setAttribute('style', 'background-image: url("/api/v1.1/Games/' + gameId + '/artwork/' + artworks[artworksPosition] + '/image/original/' + artworks[artworksPosition] + '.jpg"); background-position: center; background-repeat: no-repeat; background-size: cover; filter: blur(10px); -webkit-filter: blur(10px);');
    }
}

function SaveStatistics() {
    var model;
    if (SessionId == undefined) {
        ajaxCall(
            '/api/v1.1/Statistics/Games/' + gameId + '/' + platformId + '/' + romId + '?IsMediaGroup=' + IsMediaGroup,
            'POST',
            function (success) {
                SessionId = success.sessionId;
            }
        );
    } else {
        ajaxCall(
            '/api/v1.1/Statistics/Games/' + gameId + '/' + platformId + '/' + romId + '/' + SessionId + '?IsMediaGroup=' + IsMediaGroup,
            'PUT',
            function (success) {

            }
        );
    }
}

SetupPage();