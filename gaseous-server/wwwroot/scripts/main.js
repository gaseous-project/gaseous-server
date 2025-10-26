var locale = window.navigator.userLanguage || window.navigator.language;
console.log(locale);
moment.locale(locale);

// clear logo provider preference order
var logoProviders = ["ScreenScraper", "TheGamesDb"];

function ajaxCall(endpoint, method, successFunction, errorFunction, body) {
    $.ajax({

        // Our sample url to make request
        url:
            endpoint,

        // Type of Request
        type: method,

        // data to send to the server
        data: body,

        dataType: 'json',
        contentType: 'application/json',

        // Function to call when to
        // request is ok
        success: function (data) {
            //var x = JSON.stringify(data);
            //console.log(x);
            successFunction(data);
        },

        // Error handling
        error: function (error) {
            console.log('Error reaching URL: ' + endpoint);
            console.log(`Error ${JSON.stringify(error)}`);

            if (errorFunction) {
                errorFunction(error);
            }
        }
    });
}

function getQueryString(stringName, type) {
    const urlParams = new URLSearchParams(window.location.search);
    var myParam = urlParams.get(stringName);

    switch (type) {
        case "int":
            if (typeof (Number(myParam)) == 'number') {
                return Number(myParam);
            } else {
                return null;
            }
            break;
        case "string":
            if (typeof (myParam) == 'string') {
                return encodeURIComponent(myParam);
            } else {
                return null;
            }
        default:
            return null;
            break;
    }
}

function setCookie(cname, cvalue, exdays) {
    const d = new Date();
    d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
    if (exdays) {
        let expires = "expires=" + d.toUTCString();
        document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
    } else {
        document.cookie = cname + "=" + cvalue + ";path=/";
    }
}

function getCookie(cname) {
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}

function formatBytes(bytes, decimals = 2) {
    if (!+bytes) return '0 Bytes'

    const k = 1024
    const dm = decimals < 0 ? 0 : decimals
    const sizes = ['Bytes', 'KiB', 'MiB', 'GiB', 'TiB', 'PiB', 'EiB', 'ZiB', 'YiB']

    const i = Math.floor(Math.log(bytes) / Math.log(k))

    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(dm))} ${sizes[i]}`
}

function showDialog(dialogPage, variables) {
    // Get the modal
    var modal = document.getElementById("myModal");

    // Get the modal content
    var modalContent = document.getElementById("modal-content");

    // Get the button that opens the modal
    var btn = document.getElementById("myBtn");

    // Get the <span> element that closes the modal
    var span = document.getElementsByClassName("close")[0];

    // When the user clicks on the button, open the modal 
    modal.style.display = "block";

    // When the user clicks on <span> (x), close the modal
    span.onclick = function () {
        modal.style.display = "none";
        modalContent.innerHTML = "";
        modalVariables = null;
    }

    // When the user clicks anywhere outside of the modal, close it
    window.onclick = function (event) {
        if (event.target == modal) {
            modal.style.display = "none";
            modalContent.innerHTML = "";
            modalVariables = null;
        }
    }

    modalVariables = variables;

    $('#modal-content').load('/pages/dialogs/' + dialogPage + '.html?v=' + AppVersion);
}

function closeDialog() {
    // Get the modal
    var modal = document.getElementById("myModal");

    // Get the modal content
    var modalContent = document.getElementById("modal-content");

    modal.style.display = "none";
    modalContent.innerHTML = "";
    modalVariables = null;
}

var subModalVariables;

function showSubDialog(dialogPage, variables) {
    // Get the modal
    var submodal = document.getElementById("myModalSub");

    // Get the modal content
    var subModalContent = document.getElementById("modal-content-sub");

    // Get the button that opens the modal
    var subbtn = document.getElementById("romDelete");

    // Get the <span> element that closes the modal
    var subspan = document.getElementById("modal-close-sub");

    // When the user clicks on the button, open the modal 
    submodal.style.display = "block";

    // When the user clicks on <span> (x), close the modal
    subspan.onclick = function () {
        submodal.style.display = "none";
        subModalContent.innerHTML = "";
        subModalVariables = null;
    }

    subModalVariables = variables;

    $('#modal-content-sub').load('/pages/dialogs/' + dialogPage + '.html?v=' + AppVersion);
}

function closeSubDialog() {
    // Get the modal
    var submodal = document.getElementById("myModalSub");

    // Get the modal content
    var subModalContent = document.getElementById("modal-content-sub");

    submodal.style.display = "none";
    subModalContent.innerHTML = "";
    subModalVariables = null;
}

function randomIntFromInterval(min, max) { // min and max included 
    var rand = Math.floor(Math.random() * (max - min + 1) + min);
    return rand;
}

function createTableRow(isHeader, row, rowClass, cellClass) {
    var newRow = document.createElement('tr');
    newRow.className = rowClass;

    for (var i = 0; i < row.length; i++) {
        var cellType = 'td';
        if (isHeader == true) {
            cellType = 'th';
        }

        var newCell = document.createElement(cellType);
        if (typeof (row[i]) != "object") {
            newCell.innerHTML = row[i];
            newCell.className = cellClass;
        } else {
            if (Array.isArray(row[i])) {
                if (typeof (row[i][0]) != "object") {
                    newCell.innerHTML = row[i][0];
                } else {
                    newCell.appendChild(row[i][0]);
                }
                if (row[i][1]) { newCell.className = row[i][1]; }
                if (row[i][2]) { newCell.setAttribute('name', row[i][2]); }
            } else {
                newCell.appendChild(row[i]);
                newCell.className = cellClass;
            }
        }

        newRow.appendChild(newCell);
    }

    return newRow;
}

function hashCode(str) {
    var hash = 0;
    for (var i = 0; i < str.length; i++) {
        hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }

    return hash;
}

function intToRGB(i) {
    var c = (i & 0x00FFFFFF)
        .toString(16)
        .toUpperCase();

    return "00000".substring(0, 6 - c.length) + c;
}

function DropDownRenderGameOption(state) {
    if (state.loading) {
        return state;
    }

    let response;

    let releaseDate;
    if (state.releaseDate) {
        releaseDate = moment(state.releaseDate).format('yyyy');
    } else {
        releaseDate = '';
    }

    if (state.cover) {
        response = $(
            '<table class="dropdown-div"><tr><td class="dropdown-cover"><img src="/api/v1.1/Games/' + state.id + '/cover/' + state.cover.id + '/image/cover_small/' + state.id + '.jpg" class="game_tile_small_search" /></td><td class="dropdown-label"><span class="dropdown-title">' + state.text + '</span><span class="dropdown-releasedate">' + releaseDate + '</span></td></tr></table>'
        );
    } else {
        response = $(
            '<table class="dropdown-div"><tr><td class="dropdown-cover"><img src="/images/unknowngame.png" style="max-width: 90px;" /></td><td class="dropdown-label"><span>' + state.text + '</span><span class="dropdown-releasedate">' + releaseDate + '</span></td></tr></table>'
        );
    }
    return response;
}

function syntaxHighlight(json) {
    json = json.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    return json.replace(/("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?|(\{|\})?|(\[|\])?\b)/g, function (match) {
        var cls = 'number';
        if (/^"/.test(match)) {
            if (/:$/.test(match)) {
                cls = 'key';
            } else {
                cls = 'string';
            }
        } else if (/true|false/.test(match)) {
            cls = 'boolean';
        } else if (/null/.test(match)) {
            cls = 'null';
        } else if (/\{|\}/.test(match)) {
            cls = 'brace';
        } else if (/\[|\]/.test(match)) {
            cls = 'square';
        }
        return '<span class="' + cls + '">' + match + '</span>';
    });
}

function ShowPlatformMappingDialog(platformId) {
    showDialog('platformmapedit', platformId);
}

function CreateEditableTable(TableName, Headers) {
    var eDiv = document.createElement('div');

    var eTable = document.createElement('table');
    eTable.id = 'EditableTable_' + TableName;
    eTable.style.width = '100%';

    var headRow = document.createElement('tr');
    for (var i = 0; i < Headers.length; i++) {
        var headCell = document.createElement('th');
        headCell.id = 'EditableTable_' + TableName + '_' + Headers[i].name;
        headCell.innerHTML = Headers[i].label;
        headRow.appendChild(headCell);
    }
    eTable.appendChild(headRow);

    eDiv.appendChild(eTable);

    // add more button
    var addButton = document.createElement('button');
    addButton.value = 'Add Row';
    addButton.innerHTML = 'Add Row';

    $(addButton).click(function () {
        eTable.appendChild(AddEditableTableRow(Headers));
    });

    eDiv.appendChild(addButton);

    return eDiv;
}

function AddEditableTableRow(Headers) {
    var uniqueId = Math.floor(Math.random() * Date.now());

    var row = document.createElement('tr');
    row.setAttribute('id', uniqueId);
    for (var i = 0; i < Headers.length; i++) {
        var cell = document.createElement('td');

        var input = document.createElement('input');
        input.type = 'text';
        input.setAttribute('data-cell', Headers[i].name);
        input.style.width = '95%';

        cell.appendChild(input);
        row.appendChild(cell);
    }

    // delete button
    var delButtonCell = document.createElement('td');
    delButtonCell.style.textAlign = 'right';
    var delButton = document.createElement('button');
    delButton.value = 'Delete';
    delButton.innerHTML = 'Delete';
    delButton.setAttribute('onclick', 'document.getElementById("' + uniqueId + '").remove();');

    delButtonCell.appendChild(delButton);
    row.appendChild(delButtonCell);

    return row;
}

function LoadEditableTableData(TableName, Headers, Values) {
    var eTable = document.getElementById('EditableTable_' + TableName);

    for (var i = 0; i < Values.length; i++) {
        // get new row
        var row = AddEditableTableRow(Headers);
        for (var v = 0; v < row.childNodes.length; v++) {
            // looking at the cells here
            var cell = row.childNodes[v];
            for (var c = 0; c < cell.childNodes.length; c++) {
                if (cell.childNodes[c].getAttribute('data-cell')) {
                    var nodeName = cell.childNodes[c].getAttribute('data-cell');
                    if (Values[i][nodeName]) {
                        row.childNodes[v].childNodes[c].value = Values[i][nodeName];
                    }
                    break;
                }
            }
        }

        eTable.appendChild(row);
    }
}

function CreateBadge(BadgeText, ColourOverride) {
    var badgeItem = document.createElement('div');
    badgeItem.className = 'dropdownroleitem';
    badgeItem.innerHTML = BadgeText.toUpperCase();
    var colorVal = intToRGB(hashCode(BadgeText));
    if (!ColourOverride) {
        badgeItem.style.backgroundColor = '#' + colorVal;
        badgeItem.style.borderColor = '#' + colorVal;
    } else {
        badgeItem.style.backgroundColor = ColourOverride;
        badgeItem.style.borderColor = ColourOverride;
    }
    return badgeItem;
}

function getAverageRGB(imgEl) {

    var blockSize = 5, // only visit every 5 pixels
        defaultRGB = { r: 0, g: 0, b: 0 }, // for non-supporting envs
        canvas = document.createElement('canvas'),
        context = canvas.getContext && canvas.getContext('2d'),
        data, width, height,
        i = -4,
        length,
        rgb = { r: 0, g: 0, b: 0 },
        count = 0;

    if (!context) {
        return defaultRGB;
    }

    height = canvas.height = imgEl.naturalHeight || imgEl.offsetHeight || imgEl.height;
    width = canvas.width = imgEl.naturalWidth || imgEl.offsetWidth || imgEl.width;

    context.drawImage(imgEl, 0, 0);

    try {
        data = context.getImageData(0, 0, width, height);
    } catch (e) {
        /* security error, img on diff domain */
        return defaultRGB;
    }

    length = data.data.length;

    while ((i += blockSize * 4) < length) {
        ++count;
        rgb.r += data.data[i];
        rgb.g += data.data[i + 1];
        rgb.b += data.data[i + 2];
    }

    // ~~ used to floor values
    rgb.r = ~~(rgb.r / count);
    rgb.g = ~~(rgb.g / count);
    rgb.b = ~~(rgb.b / count);

    return rgb;
}

function contrastingColor(color) {
    return (luma(color) >= 165) ? '000' : 'fff';
}
function luma(color) // color can be a hx string or an array of RGB values 0-255
{
    let rgb = (typeof color === 'string') ? hexToRGBArray(color) : color;
    return (0.2126 * rgb[0]) + (0.7152 * rgb[1]) + (0.0722 * rgb[2]); // SMPTE C, Rec. 709 weightings
}
function hexToRGBArray(color) {
    if (color.length === 3)
        color = color.charAt(0) + color.charAt(0) + color.charAt(1) + color.charAt(1) + color.charAt(2) + color.charAt(2);
    else if (color.length !== 6)
        throw new Error('Invalid hex color: ' + color);
    let rgb = [];
    for (let i = 0; i <= 2; i++)
        rgb[i] = parseInt(color.substr(i * 2, 2), 16);
    return rgb;
}

function componentToHex(c) {
    let hex = c.toString(16);
    return hex.length == 1 ? "0" + hex : hex;
}

function rgbToHex(r, g, b) {
    return "#" + componentToHex(r) + componentToHex(g) + componentToHex(b);
}

function GetTaskFriendlyName(TaskName, options) {
    switch (TaskName) {
        case 'SignatureIngestor':
            return "Signature import";
        case 'TitleIngestor':
            return "Title import";
        case 'ImportQueueProcessor':
            return "Import Queue Processor";
        case 'MetadataRefresh':
            return "Metadata refresh";
        case 'OrganiseLibrary':
            return "Organise library";
        case 'LibraryScan':
            return "Library scan";
        case 'LibraryScanWorker':
            if (options) {
                return "Library scan worker: " + options.name;
            } else {
                return "Library scan worker";
            }
        case 'CollectionCompiler':
            if (options) {
                return "Compress collection id: " + options;
            } else {
                return "Compress collection";
            }
        case 'BackgroundDatabaseUpgrade':
            return "Background database upgrade";
        case 'TempCleanup':
            return "Temporary directory cleanup";
        case 'DailyMaintainer':
            return "Daily maintenance";
        case 'WeeklyMaintainer':
            return "Weekly maintenance";
        default:
            return TaskName;
    }
}

function getKeyByValue(object, value) {
    return Object.keys(object).find(key => object[key] === value);
}

let PreferenceDefaults = {
    "Library.DefaultHomePage": { default: "home", server: true },
    "Library.ShowFilter": { default: true, server: false },
    "Library.ShowGameTitle": { default: true, server: true },
    "Library.ShowGameRating": { default: true, server: true },
    "Library.ShowGameClassification": { default: true, server: true },
    "Library.GameClassificationDisplayOrder": { default: ["ESRB"], server: true },
    "Library.Filter": { default: { "pageSize": "20", "orderBy": "NameThe" }, server: false },
    "Library.FilterCollapsed": { default: { "Title Search": false, "Platforms": false }, server: false },
    "Library.ShowClearLogo": { default: true, server: true },
    "User.Locale": { default: "default", server: true }
}

function GetPreference(Setting) {
    // check local storage first
    let localValue = localStorage.getItem(Setting);
    if (localValue !== undefined && localValue !== null) {
        let localValueParsed = JSON.parse(localValue);
        return localValueParsed;
    }

    if (PreferenceDefaults[Setting]) {
        if (PreferenceDefaults[Setting].server) {
            // check user profile
            if (userProfile.userPreferences) {
                for (let preference of userProfile.userPreferences) {
                    if (preference.setting == Setting) {
                        let remoteValueParsed = JSON.parse(preference.value);
                        return remoteValueParsed;
                    }
                }
            }

            // return the default value
            SetPreference(Setting, PreferenceDefaults[Setting].default);
        }

        // return the default value
        return PreferenceDefaults[Setting].default;
    }

    // no default value found - return null
    return null;
}

function GetPreferences() {
    let preferences = {};
    for (let key in PreferenceDefaults) {
        preferences[key] = GetPreference(key);
    }
    return preferences;
}

async function SetPreference(Setting, Value, callbackSuccess, callbackError) {
    let model = [
        {
            "setting": Setting,
            "value": JSON.stringify(Value)
        }
    ];

    await SetPreference_Batch(model, null, callbackSuccess, callbackError);
}

async function SetPreference_Batch(model, callingObject, callbackSuccess, callbackError) {
    // set local storage, and create a model for the server
    let serverModel = [];
    for (let item of model) {
        localStorage.setItem(item.setting, item.value);
        if (PreferenceDefaults[item.setting].server) {
            serverModel.push(item);
        }
    }

    // send to server
    if (serverModel.length > 0) {
        await fetch('/api/v1.1/Account/Preferences', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(serverModel)
        })
            .then(response => {
                if (response.ok) {
                    if (callbackSuccess) {
                        callbackSuccess(callingObject);
                    }
                } else {
                    console.log("SetPreference_Batch: Error: " + response.statusText);
                    if (callbackError) {
                        callbackError(callingObject);
                    }
                }
            })
            .catch(error => {
                console.log("SetPreference_Batch: Error: " + error);
                if (callbackError) {
                    callbackError(callingObject);
                }
            });
    }
}

function Uint8ToString(u8a) {
    var CHUNK_SZ = 0x8000;
    var c = [];
    for (var i = 0; i < u8a.length; i += CHUNK_SZ) {
        c.push(String.fromCharCode.apply(null, u8a.subarray(i, i + CHUNK_SZ)));
    }
    return c.join("");
}

function loadAvatar(AvatarId) {
    // load user avatar
    var bannerAvatar = document.getElementById('banner_user_image');
    var bannerAvatarButton = document.getElementById('banner_user');

    if (bannerAvatar && bannerAvatarButton) {
        if (AvatarId != "00000000-0000-0000-0000-000000000000") {
            bannerAvatar.setAttribute("src", "/api/v1.1/Account/Avatar/" + AvatarId + ".jpg");
            bannerAvatar.className = "banner_button_avatar";

            bannerAvatarButton.classList.add('banner_button_avatar_image');
            bannerAvatarButton.classList.remove('banner_button');
        } else {
            bannerAvatar.setAttribute("src", "/images/user.svg");
            bannerAvatar.className = "banner_button_image";

            bannerAvatarButton.classList.remove('banner_button_avatar_image');
            bannerAvatarButton.classList.add('banner_button');
        }
    }
}

function GetRatingsBoards() {
    let ratingsBoards = GetPreference("Library.GameClassificationDisplayOrder");

    // add fallback ratings boards
    if (!ratingsBoards.includes("ESRB")) { ratingsBoards.push("ESRB"); }
    if (!ratingsBoards.includes("ACB")) { ratingsBoards.push("ACB"); }
    if (!ratingsBoards.includes("PEGI")) { ratingsBoards.push("PEGI"); }
    if (!ratingsBoards.includes("USK")) { ratingsBoards.push("USK"); }
    if (!ratingsBoards.includes("CERO")) { ratingsBoards.push("CERO"); }
    if (!ratingsBoards.includes("CLASS_IND")) { ratingsBoards.push("CLASS_IND"); }
    if (!ratingsBoards.includes("GRAC")) { ratingsBoards.push("GRAC"); }

    return ratingsBoards;
}

/**
  * @param {Object} object
  * @param {string} key
  * @return {any} value
 */
function getParameterCaseInsensitive(object, key) {
    const asLowercase = key.toLowerCase();
    return object[Object.keys(object)
        .find(k => k.toLowerCase() === asLowercase)
    ];
}

function BuildSpaceBar(LibrarySize, OtherSize, TotalSize) {
    let containerDiv = document.createElement('div');
    containerDiv.setAttribute('style', 'width: 100%;');

    let newTable = document.createElement('table');
    newTable.setAttribute('cellspacing', 0);
    newTable.setAttribute('style', 'width: 100%; height: 10px;');

    let newRow = document.createElement('tr');

    let LibrarySizePercent = Math.floor(Number(LibrarySize) / Number(TotalSize) * 100);
    let OtherSizePercent = Math.floor(Number(OtherSize) / Number(TotalSize) * 100);
    let FreeSizePercent = Math.floor((Number(LibrarySize) + Number(OtherSize)) / Number(TotalSize) * 100);

    let LibraryCell = document.createElement('td');
    LibraryCell.setAttribute('style', 'width: ' + LibrarySizePercent + '%; background-color: green;');

    let OtherCell = document.createElement('td');
    OtherCell.setAttribute('style', 'width: ' + OtherSizePercent + '%; background-color: lightgreen;');

    let FreeCell = document.createElement('td');
    FreeCell.setAttribute('style', 'width: ' + FreeSizePercent + '%; background-color: lightgray;');

    newRow.appendChild(LibraryCell);
    newRow.appendChild(OtherCell);
    newRow.appendChild(FreeCell);

    newTable.appendChild(newRow);

    containerDiv.appendChild(newTable);

    let sizeBox = document.createElement('div');
    sizeBox.setAttribute('style', 'width: 100%; height: 55px; position: relative;');

    let librarySizeSpan = document.createElement('span');
    librarySizeSpan.style.position = 'absolute';
    librarySizeSpan.classList.add('sizelabel');
    librarySizeSpan.classList.add('sizelabel_left');
    librarySizeSpan.style.display = 'none';
    if (LibrarySizePercent > 10) {
        librarySizeSpan.style.left = 'calc(' + LibrarySizePercent + '% - 75px)';
    } else {
        librarySizeSpan.style.left = '0px';
    }
    librarySizeSpan.innerHTML = 'Library: ' + formatBytes(LibrarySize) + ' (' + LibrarySizePercent + '%)';
    sizeBox.appendChild(librarySizeSpan);

    LibraryCell.addEventListener('mouseover', function () {
        librarySizeSpan.style.display = 'block';
    });
    LibraryCell.addEventListener('mouseout', function () {
        librarySizeSpan.style.display = 'none';
    });

    let otherSizeSpan = document.createElement('span');
    otherSizeSpan.style.position = 'absolute';
    otherSizeSpan.style.left = 'calc(' + OtherSizePercent + '% - 75px)';
    otherSizeSpan.classList.add('sizelabel');
    otherSizeSpan.classList.add('sizelabel_center');
    otherSizeSpan.style.display = 'none';
    otherSizeSpan.innerHTML = 'Other: ' + formatBytes(OtherSize) + ' (' + OtherSizePercent + '%)';
    sizeBox.appendChild(otherSizeSpan);

    OtherCell.addEventListener('mouseover', function () {
        otherSizeSpan.style.display = 'block';
    });
    OtherCell.addEventListener('mouseout', function () {
        otherSizeSpan.style.display = 'none';
    });

    let freeSizeSpan = document.createElement('span');
    freeSizeSpan.style.position = 'absolute';
    freeSizeSpan.style.right = '0px';
    freeSizeSpan.classList.add('sizelabel');
    freeSizeSpan.classList.add('sizelabel_right');
    freeSizeSpan.style.display = 'none';
    freeSizeSpan.innerHTML = 'Free: ' + formatBytes(TotalSize - (Number(LibrarySize) + Number(OtherSize))) + ' (' + FreeSizePercent + '%)';
    sizeBox.appendChild(freeSizeSpan);

    FreeCell.addEventListener('mouseover', function () {
        freeSizeSpan.style.display = 'block';
    });
    FreeCell.addEventListener('mouseout', function () {
        freeSizeSpan.style.display = 'none';
    });

    containerDiv.appendChild(sizeBox);

    return containerDiv;
}

class BackgroundImageRotator {
    constructor(URLList, CustomClass, Randomise, Rotate = true) {
        // Defensive programming: ensure URLList is an array
        this.URLList = URLList && Array.isArray(URLList) ? URLList : [];

        if (Randomise == true && this.URLList.length > 0) {
            this.CurrentIndex = randomIntFromInterval(0, this.URLList.length - 1);
        } else {
            this.CurrentIndex = 0;
        }
        this.RotationTimer = undefined;
        if (CustomClass) {
            this.CustomClass = CustomClass;
            this.CustomClassSet = true;
        } else {
            this.CustomClass = '';
            this.CustomClassSet = false;
        }

        this.bgImages = document.getElementById('bgImages');
        this.bgImages.innerHTML = '';

        // apply default background image
        let defaultBgImage = this.#CreateBackgroundImage('DefaultBgImage', '/images/librarybg.jpg');

        if (this.URLList) {
            if (this.URLList.length > 1) {
                // handle multiple supplied images

                // create the first image
                let bgImage = this.#CreateBackgroundImage('bgImage0', this.URLList[this.CurrentIndex]);
                this.bgImages.appendChild(bgImage);

                // start the rotation
                if (Rotate === true) {
                    this.StartRotation();
                }
            } else if (this.URLList.length == 1) {
                // handle only a single supplied image
                this.CurrentIndex = 0;

                // create the image
                let bgImage = this.#CreateBackgroundImage('bgImage0', this.URLList[0]);
                this.bgImages.appendChild(bgImage);
            } else {
                // no supplied images, but URLList is defined
                this.CurrentIndex = 0;

                // apply default background image
                this.bgImages.appendChild(defaultBgImage);
            }
        } else {
            // no supplied images, and URLList is not defined
            this.CurrentIndex = 0;

            // apply default background image
            this.bgImages.appendChild(defaultBgImage);
        }
    }

    #CreateBackgroundImage(Id, URL) {
        let BgImage = document.createElement('div');
        BgImage.id = Id;
        BgImage.classList.add('bgImage');
        if (this.CustomClassSet == true) {
            BgImage.classList.add(this.CustomClass);
        }
        BgImage.style.backgroundImage = "url('" + URL + "')";

        // remove default background colour
        document.getElementsByTagName('html')[0].removeAttribute('style');

        return BgImage;
    }

    // rotate each image in URLList using a JQuery fade in/out effect every 10 seconds
    StartRotation() {
        this.RotationTimer = setInterval(this.RotateImage.bind(this), 10000);
    }

    // stop the rotation
    StopRotation() {
        clearInterval(this.RotationTimer);
    }

    // rotate the image
    RotateImage() {
        // get the current background image
        let currentImage = this.bgImages.querySelector('#bgImage' + this.CurrentIndex);

        // increment the index
        this.CurrentIndex += 1;
        if (this.CurrentIndex >= this.URLList.length) {
            this.CurrentIndex = 0;
        }

        // create a new background image
        let bgImage = this.#CreateBackgroundImage('bgImage' + this.CurrentIndex, this.URLList[this.CurrentIndex]);
        bgImage.style.display = 'none';
        this.bgImages.appendChild(bgImage);

        // fade out the current image
        $(bgImage).fadeIn(1000, function () {
            // remove the old image
            if (currentImage) {
                currentImage.remove();
            }
        });

        // clear the timer
        clearInterval(this.RotationTimer);

        // restart the timer
        this.StartRotation();
    }
}

async function BuildLaunchLink(engine, core, platformId, gameId, romId, isMediaGroup, filename, srmIndex = undefined) {
    let launchLink = '/index.html?page=emulator&engine=<ENGINE>&core=<CORE>&platformid=<PLATFORMID>&gameid=<GAMEID>&romid=<ROMID>&mediagroup=<ISMEDIAGROUP>&rompath=<FILENAME>';

    if (srmIndex) {
        launchLink = launchLink + '&srmindex=' + srmIndex;
    }

    let isValid = true;

    console.log('Validating launch link: ' + engine + ' ' + core + ' ' + platformId + ' ' + gameId + ' ' + romId + ' ' + isMediaGroup + ' ' + filename);

    let returnLink = '/index.html';

    // check if engine is valid
    let validEngines = ['EmulatorJS'];
    if (!validEngines.includes(engine)) {
        isValid = false;
        console.log('Engine is invalid!');
    }

    // fetch valid cores from json file /emulators/EmulatorJS/data/cores.json
    let validCores = [];
    await fetch('/api/v1.1/PlatformMaps', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(response => response.json())
        .then(data => {
            validCores = data;

            for (let i = 0; i < validCores.length; i++) {
                isValid = false;
                if (validCores[i].webEmulator) {
                    if (validCores[i].webEmulator.availableWebEmulators) {
                        for (let y = 0; y < validCores[i].webEmulator.availableWebEmulators.length; y++) {
                            if (validCores[i].webEmulator.availableWebEmulators[y].emulatorType == engine) {
                                for (let x = 0; x < validCores[i].webEmulator.availableWebEmulators[y].availableWebEmulatorCores.length; x++) {
                                    if (validCores[i].webEmulator.availableWebEmulators[y].availableWebEmulatorCores[x].core == core ||
                                        validCores[i].webEmulator.availableWebEmulators[y].availableWebEmulatorCores[x].alternateCoreName == core
                                    ) {
                                        isValid = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (isValid == true) {
                    break;
                }
            }
            if (isValid == false) {
                console.log('Core is invalid!');
            }

            // check if platformId is an int64
            if (!Number(platformId)) {
                isValid = false;
                console.log('PlatformId is invalid!');
            }

            // check if gameId is a an int64
            if (!Number(gameId)) {
                isValid = false;
                console.log('GameId is invalid!');
            }

            // check if romId is a an int64
            if (!Number(romId)) {
                isValid = false;
                console.log('RomId is invalid!');
            }

            // check if isMediaGroup is a boolean in a number format - if not, verify it is a boolean
            if (isMediaGroup == 0 || isMediaGroup == 1) {
                // value is a number, and is valid
            } else {
                if (isMediaGroup == true || isMediaGroup == false) {
                    // value is a boolean, and is valid
                } else {
                    isValid = false;
                    console.log('IsMediaGroup is invalid!');
                }
            }

            // check if filename is a string
            if (typeof (filename) != 'string') {
                isValid = false;
                console.log('Filename is invalid!');
            }

            if (isValid == false) {
                console.log('Link is invalid!');
                returnLink = '/index.html';
                return;
            }

            // generate the launch link
            launchLink = launchLink.replace('<ENGINE>', engine);
            launchLink = launchLink.replace('<CORE>', core);
            launchLink = launchLink.replace('<PLATFORMID>', platformId);
            launchLink = launchLink.replace('<GAMEID>', gameId);
            launchLink = launchLink.replace('<ROMID>', romId);
            if (isMediaGroup == true) {
                launchLink = launchLink.replace('<ISMEDIAGROUP>', 1);
                launchLink = launchLink.replace('<FILENAME>', '/api/v1.1/Games/' + encodeURI(gameId) + '/romgroup/' + encodeURI(romId) + '/' + encodeURI(filename) + '.zip');
            } else {
                launchLink = launchLink.replace('<ISMEDIAGROUP>', 0);
                launchLink = launchLink.replace('<FILENAME>', '/api/v1.1/Games/' + encodeURI(gameId) + '/roms/' + encodeURI(romId) + '/' + encodeURI(filename));
            }

            console.log('Validated link: ' + launchLink);

            returnLink = launchLink;

            return;
        })
        .catch(error => {
            console.error('Error:', error);
            isValid = false;
            console.log('Link is invalid!');
            returnLink = '/index.html';
        });

    return returnLink;
}

let coreData = [];

async function LoadCoreData() {
    if (coreData.length > 0) {
        return coreData; // already loaded
    }

    await fetch('/api/v1.1/PlatformMaps', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(response => response.json())
        .then(data => {
            coreData = data;
        })
        .catch(error => {
            console.error('Error loading core data:', error);
        });

    return coreData;
}

async function BuildGameLaunchLink(gamePlatformObject) {
    let launchLink = '/index.html?page=emulator&engine=<ENGINE>&core=<CORE>&platformid=<PLATFORMID>&gameid=<GAMEID>&romid=<ROMID>&mediagroup=<ISMEDIAGROUP>&rompath=<FILENAME>';

    let isValid = true;

    // set variables
    let engine = gamePlatformObject.emulatorConfiguration.emulatorType;
    let core = gamePlatformObject.emulatorConfiguration.core;
    let platformId = gamePlatformObject.id;
    let gameId = gamePlatformObject.metadataMapId;
    let romId;
    let isMediaGroup;
    let filename;
    if (gamePlatformObject.favouriteRomId) {
        romId = gamePlatformObject.favouriteRomId;
        isMediaGroup = gamePlatformObject.favouriteRomIsMediagroup;
        filename = gamePlatformObject.favouriteRomName;
    } else if (gamePlatformObject.lastPlayedRomId) {
        romId = gamePlatformObject.lastPlayedRomId;
        isMediaGroup = gamePlatformObject.lastPlayedRomIsMediagroup;
        filename = gamePlatformObject.lastPlayedRomName;
    } else {
        romId = gamePlatformObject.romId;
        isMediaGroup = gamePlatformObject.isMediaGroup;
        filename = gamePlatformObject.romName;
    }

    console.log(gamePlatformObject);
    console.log('Validating launch link: Engine: ' + engine + ' Core: ' + core + ' PlatformId: ' + platformId + ' GameId: ' + gameId + ' RomId: ' + romId + ' IsMediaGroup: ' + isMediaGroup + ' Filename: ' + filename);

    let returnLink = null;

    // check if engine is valid
    let validEngines = ['EmulatorJS'];
    if (!validEngines.includes(engine)) {
        isValid = false;
        console.log('Engine is invalid!');
        return returnLink;
    }

    // fetch valid cores from json file /emulators/EmulatorJS/data/cores.json
    let validCores = await LoadCoreData();
    if (validCores === undefined || validCores.length === 0) {
        console.log('No valid cores found!');
        isValid = false;
        return returnLink;
    } else {
        for (const coreDef of validCores) {
            if (!coreDef.webEmulator || !coreDef.webEmulator.availableWebEmulators) continue;

            isValid = coreDef.webEmulator.availableWebEmulators.some(emulator => {
                if (emulator.emulatorType !== engine) return false;

                return emulator.availableWebEmulatorCores.some(emulatorCore =>
                    emulatorCore.core === core || emulatorCore.alternateCoreName === core
                );
            });

            if (isValid) break;
        }
    }

    if (isValid === false) {
        console.log('Core is invalid!');
        return returnLink;
    }

    // check if platformId is an int64
    if (!Number(platformId)) {
        isValid = false;
        console.log('PlatformId is invalid!');
        return returnLink;
    }

    // check if gameId is a an int64
    if (!Number(gameId)) {
        isValid = false;
        console.log('GameId is invalid!');
        return returnLink;
    }

    // check if romId is a an int64
    if (!Number(romId)) {
        isValid = false;
        console.log('RomId is invalid!');
        return returnLink;
    }

    // check if isMediaGroup is a boolean in a number format - if not, verify it is a boolean
    if (isMediaGroup === 0 || isMediaGroup === 1) {
        // value is a number, and is valid
    } else {
        if (isMediaGroup === true || isMediaGroup === false) {
            // value is a boolean, and is valid
        } else {
            isValid = false;
            console.log('IsMediaGroup is invalid!');
            return returnLink;
        }
    }

    // check if filename is a string
    if (typeof (filename) != 'string') {
        isValid = false;
        console.log('Filename is invalid!');
        return returnLink;
    }

    if (isValid === false) {
        console.log('Link is invalid!');
        return returnLink;
    } else {

        // generate the launch link
        launchLink = launchLink.replace('<ENGINE>', engine);
        launchLink = launchLink.replace('<CORE>', core);
        launchLink = launchLink.replace('<PLATFORMID>', platformId);
        launchLink = launchLink.replace('<GAMEID>', gameId);
        launchLink = launchLink.replace('<ROMID>', romId);
        if (isMediaGroup === true) {
            launchLink = launchLink.replace('<ISMEDIAGROUP>', 1);
            launchLink = launchLink.replace('<FILENAME>', '/api/v1.1/Games/' + encodeURI(gameId) + '/romgroup/' + encodeURI(romId) + '/' + encodeURI(filename) + '.zip');
        } else {
            launchLink = launchLink.replace('<ISMEDIAGROUP>', 0);
            launchLink = launchLink.replace('<FILENAME>', '/api/v1.1/Games/' + encodeURI(gameId) + '/roms/' + encodeURI(romId) + '/' + encodeURI(filename));
        }

        // console.log('Validated link: ' + launchLink);

        returnLink = launchLink;
    }

    return returnLink;
}