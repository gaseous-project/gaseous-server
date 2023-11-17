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
            console.log(`Error ${JSON.stringify(error)}`);

            if (errorFunction) {
                errorFunction(error);
            }
        }
    });
}

function getQueryString(stringName, type) {
    const urlParams = new URLSearchParams(window.location.search);
    var myParam =  urlParams.get(stringName);

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
    d.setTime(d.getTime() + (exdays*24*60*60*1000));
    if (exdays) {
        let expires = "expires="+ d.toUTCString();
        document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
    } else {
        document.cookie = cname + "=" + cvalue + ";path=/";
    }
}

function getCookie(cname) {
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for(let i = 0; i <ca.length; i++) {
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
        if (typeof(row[i]) != "object") {
            newCell.innerHTML = row[i];
            newCell.className = cellClass;
        } else {
            if (Array.isArray(row[i])) {
                newCell.innerHTML = row[i][0];
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
    console.log(JSON.stringify(state));

    if (state.loading) {
        return state;
    }

    var response;

    if (state.cover) {
        response = $(
            '<table class="dropdown-div"><tr><td class="dropdown-cover"><img src="https://images.igdb.com/igdb/image/upload/t_cover_small/' + state.cover.value.imageId + '.jpg" /></td><td class="dropdown-label"><span>' + state.text + '</span></td></tr></table>'
        );
    } else {
        response = $(
            '<table class="dropdown-div"><tr><td class="dropdown-cover"><img src="/images/unknowngame.png" /></td><td class="dropdown-label"><span>' + state.text + '</span></td></tr></table>'
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
    
    $(addButton).click(function() {
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
