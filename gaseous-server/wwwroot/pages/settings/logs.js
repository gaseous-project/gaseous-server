let lastStartIndex = 0;
let logsCurrentPage = 1;
let searchModel = {};

function initLogs() {
    let correlationIdParam = getQueryString('correlationid', 'string');
    if (correlationIdParam) {
        if (correlationIdParam.length > 0) {
            document.getElementById('logs_correlationid').value = correlationIdParam;
        }
    }
}

function resetFilters() {
    document.getElementById('logs_startdate').value = '';
    document.getElementById('logs_enddate').value = '';
    document.getElementById('logs_type_info').checked = false;
    document.getElementById('logs_type_warning').checked = false;
    document.getElementById('logs_type_critical').checked = false;
    document.getElementById('logs_textsearch').value = '';
    document.getElementById('logs_correlationid').value = '';

    loadLogs();
}

function loadLogs(StartIndex, PageNumber) {
    let model = {}

    if (StartIndex && PageNumber) {
        logsCurrentPage += 1;

        // get saved search model
        model = searchModel;
        model.StartIndex = StartIndex;
        model.PageNumber = PageNumber;
    } else {
        logsCurrentPage = 1;

        // create search model
        let statusList = [];
        if (document.getElementById('logs_type_info').checked == true) { statusList.push(0); }
        if (document.getElementById('logs_type_warning').checked == true) { statusList.push(2); }
        if (document.getElementById('logs_type_critical').checked == true) { statusList.push(3); }
        let startDate = null;
        let startDateObj = document.getElementById('logs_startdate');
        if (startDateObj.value != null) { startDate = new Date(startDateObj.value); }
        let endDate = null;
        let endDateObj = document.getElementById('logs_enddate');
        if (endDateObj.value != null) { endDate = new Date(endDateObj.value); }
        let searchText = null;
        let searchTextObj = document.getElementById('logs_textsearch');
        if (searchTextObj.value != null) { searchText = searchTextObj.value; }
        let correlationId = null;
        let correlationIdTextObj = document.getElementById('logs_correlationid');
        if (correlationIdTextObj.value != null) { correlationId = correlationIdTextObj.value; }

        model = {
            "StartIndex": StartIndex,
            "PageNumber": PageNumber,
            "PageSize": 100,
            "Status": statusList,
            "StartDateTime": startDate,
            "EndDateTime": endDate,
            "SearchText": searchText,
            "CorrelationId": correlationId
        }
        console.log(model);
        searchModel = model;
    }

    ajaxCall(
        '/api/v1.1/Logs',
        'POST',
        function (result) {
            let newTable = document.getElementById('settings_events_table');
            if (logsCurrentPage == 1) {
                newTable.innerHTML = '';

                newTable.appendChild(
                    createTableRow(
                        true,
                        [
                            //'Id',
                            ['Event Time', 'logs_table_cell_150px'],
                            ['Severity', 'logs_table_cell_150px'],
                            'Process',
                            'Message'
                        ],
                        '',
                        ''
                    )
                );
            }

            for (let i = 0; i < result.length; i++) {
                lastStartIndex = result[i].id;

                let surroundingRow = document.createElement('tbody');
                surroundingRow.setAttribute('colspan', 4);
                surroundingRow.className = 'logs_table_row_' + result[i].eventType;

                let newRow = [
                    moment(result[i].eventTime).format("YYYY-MM-DD h:mm:ss a"),
                    result[i].eventType,
                    result[i].process,
                    result[i].message.replaceAll("\n", "<br />")
                ];

                surroundingRow.appendChild(createTableRow(false, newRow, '', 'romcell logs_table_cell'));

                // exception
                let exceptionString = '';
                if (result[i].exceptionValue) {
                    exceptionString = "<strong>Exception</strong><pre class='logs_table_exception' style='width: 795px; word-wrap: break-word; overflow-wrap: break-word; overflow-y: scroll;'>" + syntaxHighlight(JSON.stringify(result[i].exceptionValue, null, 2)).replace(/\\n/g, "<br />    ") + "</pre>";
                    let exRow = document.createElement('tr');
                    let leadCell = document.createElement('td');
                    exRow.appendChild(leadCell);
                    let exCell = document.createElement('td');
                    exCell.colSpan = '3';
                    exCell.innerHTML = exceptionString;
                    exRow.appendChild(exCell);
                    surroundingRow.appendChild(exRow);
                }

                // calling process
                let infoRow = document.createElement('tr');

                let infoRowEmptyCell = document.createElement('td');
                infoRowEmptyCell.className = 'romcell';

                let infoRowDataCell = document.createElement('td');
                infoRowDataCell.className = 'romcell';
                infoRowDataCell.setAttribute('colspan', 3);
                infoRowDataCell.innerHTML = '<strong>Calling process:</strong> ' + result[i].callingProcess;

                infoRow.appendChild(infoRowEmptyCell);
                infoRow.appendChild(infoRowDataCell);
                surroundingRow.appendChild(infoRow);

                // initiated by user
                if (result[i].callingUser) {
                    if (result[i].callingUser.length > 0) {
                        let infoRow3 = document.createElement('tr');

                        let infoRowEmptyCell3 = document.createElement('td');
                        infoRowEmptyCell3.className = 'romcell';

                        let infoRowDataCell3 = document.createElement('td');
                        infoRowDataCell3.className = 'romcell';
                        infoRowDataCell3.setAttribute('colspan', 3);
                        infoRowDataCell3.innerHTML = '<strong>User:</strong> ' + result[i].callingUser + "</a>";

                        infoRow3.appendChild(infoRowEmptyCell3);
                        infoRow3.appendChild(infoRowDataCell3);
                        surroundingRow.appendChild(infoRow3);
                    }
                }

                // correlation id
                let infoRow2 = document.createElement('tr');

                let infoRowEmptyCell2 = document.createElement('td');
                infoRowEmptyCell2.className = 'romcell';

                let infoRowDataCell2 = document.createElement('td');
                infoRowDataCell2.className = 'romcell';
                infoRowDataCell2.setAttribute('colspan', 3);
                infoRowDataCell2.innerHTML = '<strong>Correlation Id:</strong> <a class="romlink" href="/index.html?page=settings&sub=logs&correlationid=' + result[i].correlationId + '">' + result[i].correlationId + "</a>";

                infoRow2.appendChild(infoRowEmptyCell2);
                infoRow2.appendChild(infoRowDataCell2);
                surroundingRow.appendChild(infoRow2);

                newTable.appendChild(surroundingRow);
            }
        },
        function (error) {

        },
        JSON.stringify(model)
    );
}