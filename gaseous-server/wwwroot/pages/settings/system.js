function SystemLoadStatus() {
    fetch('/api/v1.1/BackgroundTasks')
        .then(response => response.json())
        .then(result => {
            let newTable = document.createElement('table');
            newTable.className = 'romtable';
            newTable.setAttribute('cellspacing', 0);
            newTable.appendChild(createTableRow(true, ['Task', 'Status', '', '', 'Interval<br/>(minutes)', 'Last Run Duration<br />(hh:mm:ss)', 'Last Run Start', 'Next Run Start', '']));

            if (result) {
                for (const task of result) {
                    if (task.itemState != "Disabled") {
                        if (task.itemType === 'ImportQueueProcessor' && (task.childTasks === undefined || task.childTasks.length === 0)) {
                            // Skip ImportQueueProcessor if no child tasks
                            continue;
                        }

                        let itemTypeName = GetTaskFriendlyName(task.itemType, task.options);

                        let itemStateName;
                        let itemLastStart;

                        let hasError = "";
                        if (task.hasErrors) {
                            if (task.hasErrors.errorType != null) {
                                hasError = `<img src='/images/${task.hasErrors.errorType}.svg' class='banner_button_image' style='padding-top: 5px;' title='${task.hasErrors.errorType}'>`;
                            }
                        }

                        let states = {
                            "NeverStarted": "Never started",
                            "Stopped": "Stopped",
                            "Running": "Running",
                            "Blocked": "Blocked"
                        };

                        if (!task.isBlocked) {
                            if (task.force && task.itemState != "Running") {
                                itemStateName = "<div>Pending </div><div><progress></progress></div>";
                                itemLastStart = '-';
                            } else {
                                switch (task.itemState) {
                                    case 'NeverStarted':
                                        itemStateName = states.NeverStarted;
                                        itemLastStart = moment(task.lastRunTime).format("YYYY-MM-DD h:mm:ss a");
                                        break;
                                    case 'Stopped':
                                        itemStateName = states.Stopped;
                                        itemLastStart = moment(task.lastRunTime).format("YYYY-MM-DD h:mm:ss a");
                                        break;
                                    case 'Running':
                                        itemStateName = states.Running;
                                        if (task.currentStateProgress) {
                                            if (task.currentStateProgress.includes(" of ")) {
                                                let progressParts = task.currentStateProgress.split(" of ");
                                                itemStateName = `<div>Running ${task.currentStateProgress}</div><div><progress value="${progressParts[0]}" max="${progressParts[1]}">${task.currentStateProgress}</progress></div>`;
                                            } else {
                                                itemStateName = `Running (${task.currentStateProgress})`;
                                            }
                                        }
                                        itemLastStart = moment(task.lastRunTime).format("YYYY-MM-DD h:mm:ss a");
                                        break;
                                    default:
                                        itemStateName = "Unknown status";
                                        itemLastStart = moment(task.lastRunTime).format("YYYY-MM-DD h:mm:ss a");
                                        break;
                                }
                            }
                        } else {
                            itemStateName = states.Blocked;
                            itemLastStart = moment(task.lastRunTime).format("YYYY-MM-DD h:mm:ss a");
                        }

                        let itemInterval = task.interval;
                        let nextRunTime = moment(task.nextRunTime).format("YYYY-MM-DD h:mm:ss a");
                        let startButton = '';
                        if (userProfile.roles.includes("Admin")) {
                            if (!task.force) {
                                if (task.allowManualStart && !["Running"].includes(task.itemState) && !task.isBlocked) {
                                    startButton = `<img id='startProcess' class='taskstart' src='/images/start-task.svg' onclick='StartProcess("${task.itemType}");' title='Start'>`;
                                }
                            }
                        }

                        if (!task.allowManualStart && task.removeWhenStopped) {
                            itemInterval = '';
                            nextRunTime = '';
                        }

                        let logLink = '';
                        if (task.correlationId) {
                            logLink = `<a href="/index.html?page=settings&sub=logs&correlationid=${task.correlationId}" class="romlink">Logs</a>`;
                        }

                        let newRow = [
                            itemTypeName,
                            itemStateName,
                            hasError,
                            logLink,
                            itemInterval,
                            new Date(task.lastRunDuration * 1000).toISOString().slice(11, 19),
                            itemLastStart,
                            nextRunTime,
                            startButton
                        ];
                        let newRowBody = document.createElement('tbody');
                        newRowBody.className = 'romrow taskrow';
                        newRowBody.appendChild(createTableRow(false, newRow, '', 'romcell'));

                        // add sub-row for sub tasks
                        if (task.childTasks && task.childTasks.length > 0) {
                            let subRow = document.createElement('tr');
                            let subRowCell = document.createElement('td');
                            subRowCell.style.padding = '10px';
                            subRowCell.colSpan = 9;

                            // create sub table
                            let subTable = document.createElement('table');
                            subTable.className = 'romtable';
                            subTable.setAttribute('cellspacing', 0);
                            // subTable.appendChild(createTableRow(true, ['Sub Task', 'Status']));
                            for (const subTask of task.childTasks) {
                                let subTaskName = subTask.taskName;
                                let subTaskState = states[subTask.state] || subTask.state;
                                let subTaskCounter = '';
                                let subTaskProgress = '<progress value="0" max="100"></progress>';

                                if (subTask.currentStateProgress) {
                                    if (subTask.currentStateProgress.includes(" of ")) {
                                        let progressParts = subTask.currentStateProgress.split(" of ");
                                        subTaskState = `${states[subTask.state] || subTask.state}`;
                                        subTaskCounter = `${subTask.currentStateProgress}`;
                                        subTaskProgress = `<progress value="${progressParts[0]}" max="${progressParts[1]}">${subTask.currentStateProgress}</progress>`;
                                    } else {
                                        subTaskState = `${states[subTask.state] || subTask.state}`;
                                        subTaskCounter = `${subTask.currentStateProgress}`;
                                    }
                                }

                                let subRow = [
                                    subTaskName,
                                    subTaskState,
                                    subTaskCounter,
                                    subTaskProgress
                                ];
                                let subRowBody = document.createElement('tbody');
                                subRowBody.className = 'romrow taskrow';
                                subRowBody.style.padding = '10px';
                                subRowBody.appendChild(createTableRow(false, subRow, '', 'romcell'));
                                subTable.appendChild(subRowBody);
                            }

                            subRowCell.appendChild(subTable);
                            subRow.appendChild(subRowCell);
                            newRowBody.appendChild(subRow);
                        }

                        newTable.appendChild(newRowBody);
                    }
                }
            }

            let targetDiv = document.getElementById('system_tasks');
            targetDiv.innerHTML = '';
            targetDiv.appendChild(newTable);
        })
        .catch(error => console.error('Error fetching background tasks:', error));
}

function SystemLoadSystemStatus() {
    ajaxCall('/api/v1.1/System', 'GET', function (result) {
        if (result) {
            BuildLibraryStatisticsBar(document.getElementById('system_platforms'), document.getElementById('system_platforms_legend'), result.platformStatistics);

            // database
            let newDbTable = document.createElement('table');
            newDbTable.className = 'romtable';
            newDbTable.setAttribute('cellspacing', 0);
            newDbTable.appendChild(createTableRow(false, ['Database Size', formatBytes(result.databaseSize)]));

            let targetDbDiv = document.getElementById('system_database');
            targetDbDiv.innerHTML = '';
            targetDbDiv.appendChild(newDbTable);
        }
    });
}

function BuildLibraryStatisticsBar(TargetObject, TargetObjectLegend, LibraryStatistics) {
    TargetObject.innerHTML = '';
    TargetObjectLegend.innerHTML = '';

    let newTable = document.createElement('div');
    newTable.setAttribute('cellspacing', 0);
    newTable.setAttribute('style', 'width: 100%; height: 10px;');

    let newRow = document.createElement('div');
    newRow.setAttribute('style', 'display: flex; width: 100%;');

    let LibrarySize = 0;
    // get LibarySize as sum of all platform sizes
    for (const stat of LibraryStatistics) {
        LibrarySize += stat.totalSize;
    }

    for (const stat of LibraryStatistics) {
        let platformSizePercent = stat.totalSize / LibrarySize * 100;
        let platformSizeColour = intToRGB(hashCode(stat.platform));
        let newCell = document.createElement('div');
        let segmentId = 'platform_' + stat.platform;
        newCell.id = segmentId;
        newCell.setAttribute('style', 'display: inline; height: 10px; min-width: 1px; width: ' + platformSizePercent + '%; background-color: #' + platformSizeColour);
        newRow.appendChild(newCell);

        let legend = document.createElement('div');
        legend.id = 'legend_' + stat.platform;
        legend.className = 'legend_box';

        let legendColour = document.createElement('div');
        let colourId = 'colour_' + stat.platform;
        legendColour.id = colourId;
        legendColour.className = 'legend_colour';
        legendColour.setAttribute('style', 'background-color: #' + platformSizeColour + ';');

        let legendLabel = document.createElement('div');
        legendLabel.className = 'legend_label';
        legendLabel.innerHTML = '<strong>' + stat.platform + '</strong><br />' + formatBytes(stat.totalSize) + '<br />ROMs: ' + stat.romCount;

        // event listeners
        legend.addEventListener('mouseenter', function () {
            let segment = document.getElementById(segmentId);
            segment.style.outline = '2px solid #' + platformSizeColour;
            segment.style.outlineOffset = '0px';
            segment.style.zIndex = '1';
            segment.style.boxShadow = '0px 0px 10px 0px #' + platformSizeColour;

            let legendColour = document.getElementById(colourId);
            legendColour.style.outline = '2px solid #' + platformSizeColour;
            legendColour.style.outlineOffset = '0px';
            legendColour.style.zIndex = '1';
            legendColour.style.boxShadow = '0px 0px 10px 0px #' + platformSizeColour;
        });
        legend.addEventListener('mouseleave', function () {
            let segment = document.getElementById(segmentId);
            segment.style.outline = 'none';
            segment.style.outlineOffset = '0px';
            segment.style.zIndex = '0';
            segment.style.boxShadow = 'none';

            let legendColour = document.getElementById(colourId);
            legendColour.style.outline = 'none';
            legendColour.style.outlineOffset = '0px';
            legendColour.style.zIndex = '0';
            legendColour.style.boxShadow = 'none';
        });

        legend.appendChild(legendColour);
        legend.appendChild(legendLabel);
        TargetObjectLegend.appendChild(legend);
    }

    newTable.appendChild(newRow);
    TargetObject.appendChild(newTable);
}

function SystemSignaturesStatus() {
    fetch('/api/v1.1/Signatures/Status')
        .then(response => response.json())
        .then(result => {
            let newTable = document.createElement('table');
            newTable.className = 'romtable';
            newTable.setAttribute('cellspacing', 0);
            newTable.appendChild(createTableRow(true, ['Sources', 'Platforms', 'Games', 'ROMs']));

            if (result) {
                let newRow = [
                    result.sources,
                    result.platforms,
                    result.games,
                    result.roms
                ];
                newTable.appendChild(createTableRow(false, newRow, 'romrow', 'romcell'));
            }

            let targetDiv = document.getElementById('system_signatures');
            targetDiv.innerHTML = '';
            targetDiv.appendChild(newTable);
        })
        .catch(error => console.error('Error fetching signatures status:', error));
}

function StartProcess(itemType) {
    fetch('/api/v1.1/BackgroundTasks/' + itemType + '?ForceRun=true', { method: 'GET' })
        .then(response => response.json())
        .then(result => {
            SystemLoadStatus();
        })
        .catch(error => console.error('Error starting process:', error));
}