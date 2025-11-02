class LogViewer {
    constructor(information = null, warning = null, critical = null, startDateTime = null, endDateTime = null, searchText = null, correlationId = null) {
        this.logPreconfig = {
            "Information": information,
            "Warning": warning,
            "Critical": critical,
            "StartDateTime": startDateTime,
            "EndDateTime": endDateTime,
            "SearchText": searchText,
            "CorrelationId": correlationId
        }

        this.model.StartIndex = undefined;
        this.model.PageNumber = 1;
        this.model.PageSize = 100;
    }

    Render() {
        // Create main container
        const container = document.createElement('div');

        // Create filter table
        const filterTable = document.createElement('table');
        filterTable.style.width = '100%';
        filterTable.cellSpacing = '0';

        // Row 1: Log type checkboxes
        const row1 = document.createElement('tr');
        const cell1 = document.createElement('td');

        this.infoCheckbox = document.createElement('input');
        this.infoCheckbox.type = 'checkbox';
        this.infoCheckbox.id = 'logs_type_info';
        this.infoCheckbox.checked = this.logPreconfig.Information || true;

        const infoLabel = document.createElement('label');
        infoLabel.htmlFor = 'logs_type_info';
        infoLabel.textContent = 'ℹ️ Information';

        this.warningCheckbox = document.createElement('input');
        this.warningCheckbox.type = 'checkbox';
        this.warningCheckbox.id = 'logs_type_warning';
        this.warningCheckbox.checked = this.logPreconfig.Warning || true;

        const warningLabel = document.createElement('label');
        warningLabel.htmlFor = 'logs_type_warning';
        warningLabel.textContent = '⚠️ Warning';

        this.criticalCheckbox = document.createElement('input');
        this.criticalCheckbox.type = 'checkbox';
        this.criticalCheckbox.id = 'logs_type_critical';
        this.criticalCheckbox.checked = this.logPreconfig.Critical || true;

        const criticalLabel = document.createElement('label');
        criticalLabel.htmlFor = 'logs_type_critical';
        criticalLabel.textContent = '🚫 Critical';

        // Wrap each checkbox + label pair in its own paragraph for clearer vertical spacing
        const infoPara = document.createElement('p');
        infoPara.appendChild(this.infoCheckbox);
        infoPara.appendChild(infoLabel);
        const warningPara = document.createElement('p');
        warningPara.appendChild(this.warningCheckbox);
        warningPara.appendChild(warningLabel);
        const criticalPara = document.createElement('p');
        criticalPara.appendChild(this.criticalCheckbox);
        criticalPara.appendChild(criticalLabel);

        cell1.appendChild(infoPara);
        cell1.appendChild(warningPara);
        cell1.appendChild(criticalPara);
        row1.appendChild(cell1);
        filterTable.appendChild(row1);

        // Row 2: Date inputs
        const row2 = document.createElement('tr');
        const cell2 = document.createElement('td');

        this.startDateInput = document.createElement('input');
        this.startDateInput.type = 'datetime-local';
        this.startDateInput.id = 'logs_startdate';
        this.startDateInput.placeholder = 'Start Date';
        this.startDateInput.style.width = '45%';
        this.startDateInput.value = this.logPreconfig.StartDateTime || '';

        this.endDateInput = document.createElement('input');
        this.endDateInput.type = 'datetime-local';
        this.endDateInput.id = 'logs_enddate';
        this.endDateInput.placeholder = 'End Date';
        this.endDateInput.style.width = '45%';
        this.endDateInput.value = this.logPreconfig.EndDateTime || '';

        cell2.appendChild(this.startDateInput);
        cell2.appendChild(document.createTextNode(' '));
        cell2.appendChild(this.endDateInput);
        row2.appendChild(cell2);
        filterTable.appendChild(row2);

        // Row 3: Search text input
        const row3 = document.createElement('tr');
        const cell3 = document.createElement('td');
        cell3.colSpan = 1;

        this.searchTextInput = document.createElement('input');
        this.searchTextInput.type = 'text';
        this.searchTextInput.id = 'logs_textsearch';
        this.searchTextInput.placeholder = 'Search';
        this.searchTextInput.style.width = '95%';
        this.searchTextInput.value = this.logPreconfig.SearchText || '';

        cell3.appendChild(this.searchTextInput);
        row3.appendChild(cell3);
        filterTable.appendChild(row3);

        // Row 4: Correlation ID input
        const row4 = document.createElement('tr');
        const cell4 = document.createElement('td');
        cell4.colSpan = 3;

        this.correlationIdInput = document.createElement('input');
        this.correlationIdInput.type = 'text';
        this.correlationIdInput.id = 'logs_correlationid';
        this.correlationIdInput.placeholder = 'Correlation Id';
        this.correlationIdInput.style.width = '95%';
        this.correlationIdInput.value = this.logPreconfig.CorrelationId || '';

        cell4.appendChild(this.correlationIdInput);
        row4.appendChild(cell4);
        filterTable.appendChild(row4);

        // Row 5: Buttons
        const row5 = document.createElement('tr');
        const cell5 = document.createElement('td');
        cell5.colSpan = 4;
        cell5.style.textAlign = 'right';

        this.searchButton = document.createElement('button');
        this.searchButton.textContent = 'Search';
        this.searchButton.addEventListener('click', () => {
            this.InitialSearch();
        });

        this.resetButton = document.createElement('button');
        this.resetButton.textContent = 'Reset';
        this.resetButton.addEventListener('click', () => {
            this.infoCheckbox.checked = this.logPreconfig.Information || false;
            this.warningCheckbox.checked = this.logPreconfig.Warning || false;
            this.criticalCheckbox.checked = this.logPreconfig.Critical || false;
            this.startDateInput.value = this.logPreconfig.StartDateTime || '';
            this.endDateInput.value = this.logPreconfig.EndDateTime || '';
            this.searchTextInput.value = this.logPreconfig.SearchText || '';
            this.correlationIdInput.value = this.logPreconfig.CorrelationId || '';
            this.loadMoreButton.style.display = 'none';

            // delete all existing rows in the events table except the thead
            (() => {
                // Remove all existing tbody sections (data rows) leaving the thead intact
                this.eventsTable.querySelectorAll('tbody').forEach(tb => tb.remove());
                // Force reflow to ensure DOM updates are flushed before continuing
                void this.eventsTable.offsetHeight;
            })();
        });

        cell5.appendChild(this.searchButton);
        cell5.appendChild(this.resetButton);
        row5.appendChild(cell5);
        filterTable.appendChild(row5);

        container.appendChild(filterTable);

        // Create events table
        this.eventsTable = document.createElement('table');
        this.eventsTable.id = 'settings_events_table';
        this.eventsTable.classList.add('section');
        this.eventsTable.style.width = '100%';
        this.eventsTable.cellSpacing = '0';

        // Create the header row
        const headerRow = document.createElement('thead');
        const headerCells = [['', '18px'], ['', '30px'], ['Event Time', '190px'], ['Process', '150px'], 'Message'];
        const headerTr = document.createElement('tr');

        for (const headerText of headerCells) {
            const th = document.createElement('th');
            if (Array.isArray(headerText)) {
                th.textContent = headerText[0];
                th.style.width = headerText[1];
            } else {
                th.textContent = headerText;
            }
            th.classList.add('romcell');
            headerTr.appendChild(th);
        }

        headerRow.appendChild(headerTr);
        this.eventsTable.appendChild(headerRow);

        container.appendChild(this.eventsTable);

        // Create load more container
        this.loadMoreContainer = document.createElement('div');
        this.loadMoreContainer.style.width = '100%';
        this.loadMoreContainer.style.textAlign = 'center';
        this.loadMoreContainer.style.display = 'none';

        this.loadMoreButton = document.createElement('button');
        this.loadMoreButton.value = 'Load More';
        this.loadMoreButton.textContent = 'Load More';

        this.loadMoreContainer.appendChild(this.loadMoreButton);
        container.appendChild(this.loadMoreContainer);

        // Add event listener to load more button
        this.loadMoreButton.addEventListener('click', () => {
            this.model.PageNumber += 1;
            this.ExecuteSearch();
        });

        // Initial search
        this.InitialSearch();

        return container;
    }

    InitialSearch() {
        this.model.StartIndex = undefined;
        this.model.PageNumber = 1;

        this.model.Status = [];
        if (this.infoCheckbox.checked) {
            this.model.Status.push('Information');
        }
        if (this.warningCheckbox.checked) {
            this.model.Status.push('Warning');
        }
        if (this.criticalCheckbox.checked) {
            this.model.Status.push('Critical');
        }
        this.model.StartDateTime = null;
        if (this.startDateInput.value != null) { this.model.StartDateTime = new Date(this.startDateInput.value); }
        this.model.EndDateTime = null;
        if (this.endDateInput.value != null) { this.model.EndDateTime = new Date(this.endDateInput.value); }
        this.model.SearchText = null;
        if (this.searchTextInput.value != null) { this.model.SearchText = this.searchTextInput.value; }
        this.model.CorrelationId = null;
        if (this.correlationIdInput.value != null) { this.model.CorrelationId = this.correlationIdInput.value; }

        // delete all existing rows in the events table except the thead
        (() => {
            // Remove all existing tbody sections (data rows) leaving the thead intact
            this.eventsTable.querySelectorAll('tbody').forEach(tb => tb.remove());
            // Force reflow to ensure DOM updates are flushed before continuing
            void this.eventsTable.offsetHeight;
        })();

        this.ExecuteSearch();
    }

    model = {
        "StartIndex": undefined,
        "PageNumber": 1,
        "PageSize": 100,
        "Status": ['Information', 'Warning', 'Critical'],
        "StartDateTime": undefined,
        "EndDateTime": undefined,
        "SearchText": undefined,
        "CorrelationId": undefined
    }

    ExecuteSearch() {
        fetch('/api/v1.1/Logs', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(this.model)
        }).then(response => response.json())
            .then(data => {
                for (const logEntry of data) {
                    if (this.model.StartIndex === undefined || this.model.PageNumber === 1) {
                        this.model.StartIndex = logEntry.id;
                    }

                    // check if a tbody exists with data-log-id === logEntry.id
                    let existingTbody = this.eventsTable.querySelector(`tbody[data-log-id='${logEntry.id}']`);
                    if (existingTbody) {
                        continue;
                    }

                    // Id's are unique and descending, so we can insert new entries at the top after the thead - but as we page in, we need to append at the bottom
                    const tbody = document.createElement('tbody');
                    tbody.setAttribute('data-log-id', logEntry.id);
                    // tbody.classList.add('romrow');
                    tbody.classList.add(`logs_table_row_${logEntry.eventType}`);

                    const row = document.createElement('tr');

                    const expandCell = document.createElement('td');
                    expandCell.textContent = '+';
                    expandCell.classList.add('romcell');
                    row.appendChild(expandCell);

                    const severityCell = document.createElement('td');
                    switch (logEntry.eventType) {
                        case 'Information':
                            severityCell.textContent = 'ℹ️';
                            break;
                        case 'Warning':
                            severityCell.textContent = '⚠️';
                            break;
                        case 'Critical':
                            severityCell.textContent = '🚫';
                            break;
                        default:
                            severityCell.textContent = logEntry.eventType;
                    }
                    severityCell.classList.add('romcell');
                    row.appendChild(severityCell);

                    const eventTimeCell = document.createElement('td');
                    eventTimeCell.textContent = new Date(logEntry.eventTime).toLocaleString();
                    eventTimeCell.classList.add('romcell');
                    row.appendChild(eventTimeCell);

                    const processCell = document.createElement('td');
                    processCell.textContent = logEntry.process;
                    processCell.classList.add('romcell');
                    row.appendChild(processCell);

                    const messageCell = document.createElement('td');
                    messageCell.textContent = logEntry.message;
                    messageCell.classList.add('romcell');
                    row.appendChild(messageCell);

                    // detail row
                    const detailRow = document.createElement('tr');
                    detailRow.style.display = 'none';

                    const detailRowSpacerCell = document.createElement('td');
                    detailRowSpacerCell.classList.add('romcell');
                    detailRowSpacerCell.colSpan = 1;
                    detailRow.appendChild(detailRowSpacerCell);

                    const detailRowDataCell = document.createElement('td');
                    detailRowDataCell.classList.add('romcell');
                    detailRowDataCell.colSpan = 4;
                    detailRowDataCell.style.position = 'relative';

                    // exception
                    if (logEntry.exceptionValue) {
                        const exceptionLabelDiv = document.createElement('strong');
                        exceptionLabelDiv.innerHTML = `Exception:`;
                        detailRowDataCell.appendChild(exceptionLabelDiv);

                        const preDiv = document.createElement('div');
                        preDiv.classList.add('logs_table_exception');
                        preDiv.style.overflow = 'scroll';

                        const exceptionPre = document.createElement('pre');
                        exceptionPre.innerHTML = syntaxHighlight(JSON.stringify(logEntry.exceptionValue, null, 2)).replace(/\\n/g, "<br />    ");

                        // Constrain horizontal growth
                        preDiv.style.width = '100%';
                        preDiv.style.maxWidth = '100%';
                        preDiv.style.overflowX = 'auto';      // allow scroll instead of expanding
                        preDiv.style.boxSizing = 'border-box';

                        exceptionPre.style.margin = '0';
                        exceptionPre.style.whiteSpace = 'pre-wrap'; // wrap long lines
                        exceptionPre.style.wordBreak = 'break-word'; // break long tokens
                        exceptionPre.style.maxWidth = '100%';

                        preDiv.appendChild(exceptionPre);

                        detailRowDataCell.appendChild(preDiv);
                    }

                    // calling process and user
                    const callingProcessDiv = document.createElement('p');
                    const callingProcessLabelDiv = document.createElement('strong');
                    callingProcessLabelDiv.innerHTML = `Calling process:`;
                    callingProcessDiv.appendChild(callingProcessLabelDiv);
                    const callingProcessSpan = document.createElement('span');
                    callingProcessSpan.innerHTML = ` ${logEntry.callingProcess}`;
                    callingProcessDiv.appendChild(callingProcessSpan);
                    detailRowDataCell.appendChild(callingProcessDiv);

                    const userDiv = document.createElement('p');
                    const userLabelDiv = document.createElement('strong');
                    userLabelDiv.innerHTML = `Initiated by user:`;
                    userDiv.appendChild(userLabelDiv);
                    const userSpan = document.createElement('span');
                    userSpan.innerHTML = ` ${logEntry.callingUser || 'N/A'}`;
                    userDiv.appendChild(userSpan);
                    detailRowDataCell.appendChild(userDiv);

                    // add the correlation id below the exception
                    const correlationIdDiv = document.createElement('p');
                    const correlationIdLabelDiv = document.createElement('strong');
                    correlationIdLabelDiv.innerHTML = `Correlation Id: `;
                    correlationIdDiv.appendChild(correlationIdLabelDiv);
                    const correlationIdSpan = document.createElement('span');
                    correlationIdSpan.classList.add('romlink');
                    correlationIdSpan.innerHTML = `${logEntry.correlationId}`;
                    correlationIdSpan.addEventListener('click', () => {
                        // navigate to logs page with correlation id filter
                        this.correlationIdInput.value = logEntry.correlationId;
                        this.InitialSearch();
                    });
                    correlationIdDiv.appendChild(correlationIdSpan);
                    detailRowDataCell.appendChild(correlationIdDiv);

                    // add the additional data if it exists
                    if (logEntry.additionalData) {
                        for (const [key, value] of Object.entries(logEntry.additionalData)) {
                            const additionalDataDiv = document.createElement('p');
                            const additionalDataLabelDiv = document.createElement('strong');
                            additionalDataLabelDiv.innerHTML = `${key}: `;
                            additionalDataDiv.appendChild(additionalDataLabelDiv);
                            const additionalDataSpan = document.createElement('span');
                            additionalDataSpan.innerHTML = `${value}`;
                            if (key.toLowerCase() === 'subtaskcorrelationid') {
                                additionalDataSpan.classList.add('romlink');
                                additionalDataSpan.addEventListener('click', () => {
                                    // navigate to logs page with correlation id filter
                                    this.correlationIdInput.value = value;
                                    this.InitialSearch();
                                });
                            }
                            additionalDataDiv.appendChild(additionalDataSpan);
                            detailRowDataCell.appendChild(additionalDataDiv);
                        }
                    }

                    detailRow.appendChild(detailRowDataCell);

                    tbody.appendChild(row);
                    tbody.appendChild(detailRow);

                    // add click event to row to toggle detailRow
                    row.addEventListener('click', () => {
                        if (detailRow.style.display === 'none') {
                            detailRow.style.display = '';
                            expandCell.textContent = '-';
                        } else {
                            detailRow.style.display = 'none';
                            expandCell.textContent = '+';
                        }
                    });

                    // Append the new tbody at the end of the table
                    this.eventsTable.appendChild(tbody);
                }

                // Show or hide the load more button
                if (data.length === this.model.PageSize) {
                    this.loadMoreContainer.style.display = 'block';
                } else {
                    this.loadMoreContainer.style.display = 'none';
                }
            });
    }
}