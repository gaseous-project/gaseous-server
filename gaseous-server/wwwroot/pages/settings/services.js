function getBackgroundTaskTimers() {
    ajaxCall(
        '/api/v1/System/Settings/BackgroundTasks/Configuration',
        'GET',
        function (result) {
            let targetTable = document.getElementById('settings_tasktimers');
            targetTable.innerHTML = '';

            for (const [key, value] of Object.entries(result)) {
                let enabledString = "";
                if (value.enabled == true) {
                    enabledString = 'checked="checked"';
                }

                // create section
                let serviceSection = document.createElement('div');
                serviceSection.className = 'section';
                serviceSection.id = 'settings_tasktimers_' + value.task;
                targetTable.appendChild(serviceSection);

                // add heading
                let serviceHeader = document.createElement('div');
                serviceHeader.className = 'section-header';
                serviceHeader.innerHTML = GetTaskFriendlyName(value.task);
                serviceSection.appendChild(serviceHeader);

                // create table for each service
                let serviceTable = document.createElement('table');
                serviceTable.style.width = '100%';
                serviceTable.classList.add('section-body');

                // add enabled
                let newEnabledRow = document.createElement('tr');

                let newEnabledTitle = document.createElement('td');
                newEnabledTitle.className = 'romcell romcell-headercell';
                newEnabledTitle.innerHTML = "Enabled:";
                newEnabledRow.appendChild(newEnabledTitle);

                let newEnabledContent = document.createElement('td');
                newEnabledContent.className = 'romcell';
                let newEnabledCheckbox = document.createElement('input');
                newEnabledCheckbox.id = 'settings_enabled_' + value.task;
                newEnabledCheckbox.name = 'settings_tasktimers_enabled';
                newEnabledCheckbox.type = 'checkbox';
                newEnabledCheckbox.checked = value.enabled;
                newEnabledContent.appendChild(newEnabledCheckbox);
                newEnabledRow.appendChild(newEnabledContent);

                serviceTable.appendChild(newEnabledRow);

                // add interval
                let newIntervalRow = document.createElement('tr');

                let newIntervalTitle = document.createElement('td');
                newIntervalTitle.className = 'romcell romcell-headercell';
                newIntervalTitle.innerHTML = "Minimum Interval (Minutes):";
                newIntervalRow.appendChild(newIntervalTitle);

                let newIntervalContent = document.createElement('td');
                newIntervalContent.className = 'romcell';
                let newIntervalInput = document.createElement('input');
                newIntervalInput.id = 'settings_tasktimers_' + value.task;
                newIntervalInput.name = 'settings_tasktimers_values';
                newIntervalInput.setAttribute('data-name', value.task);
                newIntervalInput.setAttribute('data-default', value.defaultInterval);
                newIntervalInput.type = 'number';
                newIntervalInput.placeholder = value.defaultInterval;
                newIntervalInput.min = value.minimumAllowedInterval;
                newIntervalInput.value = value.interval;
                newIntervalContent.appendChild(newIntervalInput);
                newIntervalRow.appendChild(newIntervalContent);

                serviceTable.appendChild(newIntervalRow);

                // allowed time periods row
                let newTableRowTime = document.createElement('tr');

                let rowTimeContentTitle = document.createElement('td');
                rowTimeContentTitle.className = 'romcell romcell-headercell';
                rowTimeContentTitle.innerHTML = "Allowed Days:";
                newTableRowTime.appendChild(rowTimeContentTitle);

                let rowTimeContent = document.createElement('td');
                // rowTimeContent.setAttribute('colspan', 2);
                rowTimeContent.className = 'romcell';
                let daySelector = document.createElement('select');
                daySelector.id = 'settings_alloweddays_' + value.task;
                daySelector.name = 'settings_alloweddays';
                daySelector.multiple = 'multiple';
                daySelector.setAttribute('data-default', value.defaultAllowedDays.join(","));
                daySelector.style.width = '95%';
                let days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
                for (let d = 0; d < days.length; d++) {
                    let dayOpt = document.createElement('option');
                    dayOpt.value = days[d];
                    dayOpt.innerHTML = days[d];
                    if (value.allowedDays.includes(days[d])) {
                        dayOpt.selected = 'selected';
                    }
                    daySelector.appendChild(dayOpt);
                }
                rowTimeContent.appendChild(daySelector);
                $(daySelector).select2({
                    tags: false
                });
                newTableRowTime.appendChild(rowTimeContent);

                serviceTable.appendChild(newTableRowTime);

                // add start and end times
                let newTableRowClock = document.createElement('tr');

                let rowClockContentTitle = document.createElement('td');
                rowClockContentTitle.className = 'romcell romcell-headercell';
                rowClockContentTitle.innerHTML = "Time Range:";
                newTableRowClock.appendChild(rowClockContentTitle);

                let rowClockContent = document.createElement('td');
                rowClockContent.className = 'romcell';
                // rowClockContent.setAttribute('colspan', 2);

                rowClockContent.appendChild(generateTimeDropDowns(value.task, 'Start', value.defaultAllowedStartHours, value.defaultAllowedStartMinutes, value.allowedStartHours, value.allowedStartMinutes));

                rowClockContentSeparator = document.createElement('span');
                rowClockContentSeparator.innerHTML = '&nbsp;-&nbsp;';
                rowClockContent.appendChild(rowClockContentSeparator);

                rowClockContent.appendChild(generateTimeDropDowns(value.task, 'End', value.defaultAllowedEndHours, value.defaultAllowedEndMinutes, value.allowedEndHours, value.allowedEndMinutes));

                newTableRowClock.appendChild(rowClockContent);

                serviceTable.appendChild(newTableRowClock);

                // blocks tasks
                let newTableRowBlocks = document.createElement('tr');

                let rowBlocksContentTitle = document.createElement('td');
                rowBlocksContentTitle.className = 'romcell romcell-headercell';
                rowBlocksContentTitle.innerHTML = "Blocks:";
                newTableRowBlocks.appendChild(rowBlocksContentTitle);

                let rowBlocksContent = document.createElement('td');
                rowBlocksContent.className = 'romcell';
                // rowBlocksContent.setAttribute('colspan', 2);
                let blocksString = "";
                for (let i = 0; i < value.blocks.length; i++) {
                    if (blocksString.length > 0) { blocksString += ", "; }
                    blocksString += GetTaskFriendlyName(value.blocks[i]);
                }
                if (blocksString.length == 0) { blocksString = 'None'; }
                rowBlocksContent.innerHTML = blocksString;
                newTableRowBlocks.appendChild(rowBlocksContent);

                serviceTable.appendChild(newTableRowBlocks);

                // blocked by tasks
                let newTableRowBlockedBy = document.createElement('tr');

                let rowBlockedByContentTitle = document.createElement('td');
                rowBlockedByContentTitle.className = 'romcell romcell-headercell';
                rowBlockedByContentTitle.innerHTML = "Blocked By:";
                newTableRowBlockedBy.appendChild(rowBlockedByContentTitle);

                let rowBlockedByContent = document.createElement('td');
                rowBlockedByContent.className = 'romcell';
                // rowBlockedByContent.setAttribute('colspan', 2);
                let BlockedByString = "";
                for (let i = 0; i < value.blockedBy.length; i++) {
                    if (BlockedByString.length > 0) { BlockedByString += ", "; }
                    BlockedByString += GetTaskFriendlyName(value.blockedBy[i]);
                }
                if (BlockedByString.length == 0) { BlockedByString = 'None'; }
                rowBlockedByContent.innerHTML = BlockedByString;
                newTableRowBlockedBy.appendChild(rowBlockedByContent);

                serviceTable.appendChild(newTableRowBlockedBy);

                // complete row
                serviceSection.appendChild(serviceTable);
            }
        }
    );
}

function generateTimeDropDowns(taskName, rangeName, defaultHour, defaultMinute, valueHour, valueMinute) {
    let container = document.createElement('div');
    container.style.display = 'inline';

    let elementName = 'settings_tasktimers_time';

    let hourSelector = document.createElement('input');
    hourSelector.id = 'settings_tasktimers_' + taskName + '_' + rangeName + '_Hour';
    hourSelector.name = elementName;
    hourSelector.setAttribute('data-name', taskName);
    hourSelector.setAttribute('type', 'number');
    hourSelector.setAttribute('min', '0');
    hourSelector.setAttribute('max', '23');
    hourSelector.setAttribute('placeholder', defaultHour);
    hourSelector.value = valueHour;
    container.appendChild(hourSelector);

    let separator = document.createElement('span');
    separator.innerHTML = " : ";
    container.appendChild(separator);

    let minSelector = document.createElement('input');
    minSelector.id = 'settings_tasktimers_' + taskName + '_' + rangeName + '_Minute';
    minSelector.name = elementName;
    minSelector.setAttribute('type', 'number');
    minSelector.setAttribute('min', '0');
    minSelector.setAttribute('max', '59');
    minSelector.setAttribute('placeholder', defaultMinute);
    minSelector.value = valueMinute;
    container.appendChild(minSelector);

    return container;
}

function saveTaskTimers() {
    let timerValues = document.getElementsByName('settings_tasktimers_values');

    let model = [];
    for (let i = 0; i < timerValues.length; i++) {
        let taskName = timerValues[i].getAttribute('data-name');
        let taskEnabled = document.getElementById('settings_enabled_' + taskName).checked;
        let taskIntervalObj = document.getElementById('settings_tasktimers_' + taskName);
        let taskInterval = function () { if (taskIntervalObj.value) { return taskIntervalObj.value; } else { return taskIntervalObj.getAttribute('placeholder'); } };
        let taskDaysRaw = $('#settings_alloweddays_' + taskName).select2('data');
        let taskDays = [];
        if (taskDaysRaw.length > 0) {
            for (let d = 0; d < taskDaysRaw.length; d++) {
                taskDays.push(taskDaysRaw[d].id);
            }
        } else {
            taskDays.push("Monday");
        }
        let taskStartHourObj = document.getElementById('settings_tasktimers_' + taskName + '_Start_Hour');
        let taskStartMinuteObj = document.getElementById('settings_tasktimers_' + taskName + '_Start_Minute');
        let taskEndHourObj = document.getElementById('settings_tasktimers_' + taskName + '_End_Hour');
        let taskEndMinuteObj = document.getElementById('settings_tasktimers_' + taskName + '_End_Minute');

        let taskStartHour = function () { if (taskStartHourObj.value) { return taskStartHourObj.value; } else { return taskStartHourObj.getAttribute('placeholder'); } };
        let taskStartMinute = function () { if (taskStartMinuteObj.value) { return taskStartMinuteObj.value; } else { return taskStartMinuteObj.getAttribute('placeholder'); } };
        let taskEndHour = function () { if (taskEndHourObj.value) { return taskEndHourObj.value; } else { return taskEndHourObj.getAttribute('placeholder'); } };
        let taskEndMinute = function () { if (taskEndMinuteObj.value) { return taskEndMinuteObj.value; } else { return taskEndMinuteObj.getAttribute('placeholder'); } };

        model.push(
            {
                "task": taskName,
                "enabled": taskEnabled,
                "interval": taskInterval(),
                "allowedDays": taskDays,
                "allowedStartHours": taskStartHour(),
                "allowedStartMinutes": taskStartMinute(),
                "allowedEndHours": taskEndHour(),
                "allowedEndMinutes": taskEndMinute()
            }
        );
    }

    ajaxCall(
        '/api/v1/System/Settings/BackgroundTasks/Configuration',
        'POST',
        function (result) {
            getBackgroundTaskTimers();
        },
        function (error) {
            getBackgroundTaskTimers();
        },
        JSON.stringify(model)
    );
}

function defaultTaskTimers() {
    let taskEnabled = document.getElementsByName('settings_tasktimers_enabled');

    for (let i = 0; i < taskEnabled.length; i++) {
        taskEnabled[i].checked = true;
    }

    let taskTimerValues = document.getElementsByName('settings_tasktimers_values');

    for (let i = 0; i < taskTimerValues.length; i++) {
        taskTimerValues[i].value = taskTimerValues[i].getAttribute('data-default');
    }

    let taskAllowedDays = document.getElementsByName('settings_alloweddays');

    for (let i = 0; i < taskAllowedDays.length; i++) {
        let defaultSelections = taskAllowedDays[i].getAttribute('data-default').split(',');
        $(taskAllowedDays[i]).val(defaultSelections);
        $(taskAllowedDays[i]).trigger('change');
    }

    let taskTimes = document.getElementsByName('settings_tasktimers_time');

    for (let i = 0; i < taskTimes.length; i++) {
        taskTimes[i].value = taskTimes[i].getAttribute('placeholder');
    }

    saveTaskTimers();
}