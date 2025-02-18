function SystemLoadStatus() {
    ajaxCall('/api/v1.1/BackgroundTasks', 'GET', function (result) {
        let newTable = document.createElement('table');
        newTable.className = 'romtable';
        newTable.setAttribute('cellspacing', 0);
        newTable.appendChild(createTableRow(true, ['Task', 'Status', '', '', 'Interval<br/>(minutes)', 'Last Run Duration<br />(hh:mm:ss)', 'Last Run Start', 'Next Run Start', '']));

        if (result) {
            for (let i = 0; i < result.length; i++) {
                if (result[i].itemState != "Disabled") {
                    let itemTypeName = GetTaskFriendlyName(result[i].itemType, result[i].options);

                    let itemStateName;
                    let itemLastStart;

                    let hasError = "";
                    if (result[i].hasErrors) {
                        if (result[i].hasErrors.errorType != null) {
                            // hasError = " (" + result[i].hasErrors.errorType + ")";
                            hasError = "<img src='/images/" + result[i].hasErrors.errorType + ".svg' class='banner_button_image' style='padding-top: 5px;' title='" + result[i].hasErrors.errorType + "'>";
                        }
                    }

                    if (result[i].isBlocked == false) {
                        if (result[i].force == true && result[i].itemState != "Running") {
                            itemStateName = "<div>Pending </div><div><progress></progress></div>";
                            itemLastStart = '-';
                        } else {
                            switch (result[i].itemState) {
                                case 'NeverStarted':
                                    itemStateName = "Never started";
                                    itemLastStart = moment(result[i].lastRunTime).format("YYYY-MM-DD h:mm:ss a");
                                    break;
                                case 'Stopped':
                                    itemStateName = "Stopped";
                                    itemLastStart = moment(result[i].lastRunTime).format("YYYY-MM-DD h:mm:ss a");
                                    break;
                                case 'Running':
                                    itemStateName = "Running";
                                    if (result[i].currentStateProgress) {
                                        if (result[i].currentStateProgress.includes(" of ")) {
                                            let progressParts = result[i].currentStateProgress.split(" of ");
                                            itemStateName = "<div>Running " + result[i].currentStateProgress + "</div><div><progress value=\"" + progressParts[0] + "\" max=\"" + progressParts[1] + "\">" + result[i].currentStateProgress + "</progress></div>";
                                        } else {
                                            itemStateName = "Running (" + result[i].currentStateProgress + ")";
                                        }
                                    }
                                    itemLastStart = moment(result[i].lastRunTime).format("YYYY-MM-DD h:mm:ss a");
                                    break;
                                default:
                                    itemStateName = "Unknown status";
                                    itemLastStart = moment(result[i].lastRunTime).format("YYYY-MM-DD h:mm:ss a");
                                    break;
                            }
                        }
                    } else {
                        itemStateName = "Blocked";
                        itemLastStart = moment(result[i].lastRunTime).format("YYYY-MM-DD h:mm:ss a");
                    }

                    let itemInterval = result[i].interval;
                    let nextRunTime = moment(result[i].nextRunTime).format("YYYY-MM-DD h:mm:ss a");
                    let startButton = '';
                    if (userProfile.roles.includes("Admin")) {
                        if (result[i].force == false) {
                            if (result[i].allowManualStart == true && !["Running"].includes(result[i].itemState) && result[i].isBlocked == false) {
                                startButton = "<img id='startProcess' class='taskstart' src='/images/start-task.svg' onclick='StartProcess(\"" + result[i].itemType + "\");' title='Start'>";
                            }
                        }
                    }

                    if (result[i].allowManualStart == false && result[i].removeWhenStopped == true) {
                        itemInterval = '';
                        nextRunTime = '';
                    }

                    let logLink = '';
                    if (result[i].correlationId) {
                        logLink = '<a href="/index.html?page=settings&sub=logs&correlationid=' + result[i].correlationId + '" class="romlink">Logs</a>';
                    }

                    let newRow = [
                        itemTypeName,
                        itemStateName,
                        hasError,
                        logLink,
                        itemInterval,
                        new Date(result[i].lastRunDuration * 1000).toISOString().slice(11, 19),
                        itemLastStart,
                        nextRunTime,
                        startButton
                    ];
                    newTable.appendChild(createTableRow(false, newRow, 'romrow taskrow', 'romcell'));
                }
            }
        }

        let targetDiv = document.getElementById('system_tasks');
        targetDiv.innerHTML = '';
        targetDiv.appendChild(newTable);
    });
}

function SystemLoadSystemStatus() {
    ajaxCall('/api/v1.1/System', 'GET', function (result) {
        if (result) {
            let totalLibrarySpace = 0;

            // // disks
            // let newTable = document.createElement('table');
            // newTable.className = 'romtable';
            // newTable.setAttribute('cellspacing', 0);
            // newTable.appendChild(createTableRow(true, ['Path', 'Library Size <div id="disk_LibSize" style="width: 10px; height: 10px; background-color: green;"></div>', 'Other <div id="disk_OtherSize" style="width: 10px; height: 10px; background-color: lightgreen;"></div>', 'Total Size <div id="disk_FreeSize" style="width: 10px; height: 10px; background-color: lightgray;"></div>']));

            for (let i = 0; i < result.paths.length; i++) {
                let spaceUsedByLibrary = result.paths[i].spaceUsed;
                totalLibrarySpace += spaceUsedByLibrary;
                //     let spaceUsedByOthers = result.paths[i].totalSpace - result.paths[i].spaceAvailable;

                //     let libraryRow = document.createElement('tbody');
                //     libraryRow.className = 'romrow';

                //     let titleRow = document.createElement('tr');
                //     let titleCell = document.createElement('td');
                //     titleCell.setAttribute('colspan', 4);
                //     titleCell.innerHTML = '<strong>' + result.paths[i].name + '</strong>';
                //     titleCell.className = 'romcell';
                //     titleRow.appendChild(titleCell);
                //     libraryRow.appendChild(titleRow);

                //     let newRow = [
                //         result.paths[i].libraryPath,
                //         formatBytes(spaceUsedByLibrary),
                //         formatBytes(spaceUsedByOthers),
                //         formatBytes(result.paths[i].totalSpace)
                //     ];

                //     libraryRow.appendChild(createTableRow(false, newRow, '', 'romcell'));

                //     let spaceRow = document.createElement('tr');
                //     let spaceCell = document.createElement('td');
                //     spaceCell.setAttribute('colspan', 4);
                //     spaceCell.appendChild(BuildSpaceBar(spaceUsedByLibrary, spaceUsedByOthers, result.paths[i].totalSpace));
                //     spaceRow.appendChild(spaceCell);
                //     libraryRow.appendChild(spaceRow);

                //     newTable.appendChild(libraryRow);
            }

            // let targetDiv = document.getElementById('system_disks');
            // targetDiv.innerHTML = '';
            // targetDiv.appendChild(newTable);

            BuildLibraryStatisticsBar(document.getElementById('system_platforms'), document.getElementById('system_platforms_legend'), result.platformStatistics, totalLibrarySpace);

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

function BuildLibraryStatisticsBar(TargetObject, TargetObjectLegend, LibraryStatistics, LibrarySize) {
    TargetObject.innerHTML = '';
    TargetObjectLegend.innerHTML = '';

    let newTable = document.createElement('div');
    newTable.setAttribute('cellspacing', 0);
    newTable.setAttribute('style', 'width: 100%; height: 10px;');

    let newRow = document.createElement('div');
    newRow.setAttribute('style', 'display: flex; width: 100%;');

    for (let i = 0; i < LibraryStatistics.length; i++) {
        let platformSizePercent = LibraryStatistics[i].totalSize / LibrarySize * 100;
        let platformSizeColour = intToRGB(hashCode(LibraryStatistics[i].platform));
        let newCell = document.createElement('div');
        let segmentId = 'platform_' + LibraryStatistics[i].platform;
        newCell.id = segmentId;
        newCell.setAttribute('style', 'display: inline; height: 10px; min-width: 1px; width: ' + platformSizePercent + '%; background-color: #' + platformSizeColour);
        // newCell.innerHTML = '&nbsp;';
        newRow.appendChild(newCell);

        let legend = document.createElement('div');
        legend.id = 'legend_' + LibraryStatistics[i].platform;
        legend.className = 'legend_box';

        let legendColour = document.createElement('div');
        let colourId = 'colour_' + LibraryStatistics[i].platform;
        legendColour.id = colourId;
        legendColour.className = 'legend_colour';
        legendColour.setAttribute('style', 'background-color: #' + platformSizeColour + ';');

        let legendLabel = document.createElement('div');
        legendLabel.className = 'legend_label';
        legendLabel.innerHTML = '<strong>' + LibraryStatistics[i].platform + '</strong><br />' + formatBytes(LibraryStatistics[i].totalSize) + '<br />ROMs: ' + LibraryStatistics[i].romCount;

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
    ajaxCall('/api/v1.1/Signatures/Status', 'GET', function (result) {
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
    });
}

function StartProcess(itemType) {
    ajaxCall('/api/v1.1/BackgroundTasks/' + itemType + '?ForceRun=true', 'GET', function (result) {
        SystemLoadStatus();
    });
}