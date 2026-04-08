let notifications = {};

/**
 * Central manager for notification polling, toast rendering, and notification history.
 */
class NotificationManager {
    constructor() {
        this.maxVisibleNotifications = 2;
        this.duplicateTimeoutMs = 600000;
        this.historySize = 10;
        this.historyKey = 'System.Notifications';
        this.historyExpiryMs = 86400000;

        this.notificationLoadStartCallbacks = [];
        this.notificationLoadEndCallbacks = [];
        this.notificationLoadErrorCallbacks = [];
        this.notificationLibraryUpdateCallbacks = [];

        this.lastNotificationShown = null;
        this.notificationLibraryUpdateLastUpdate = new Date();
        this.visibleNotifications = [];
        this.historyPanels = new Set();
        this.fetchIntervalId = null;
    }

    /**
     * Shows a toast notification with dedupe, stacking, and history updates.
     */
    showNotification(heading, message, image, callback, timeout, noteid) {
        const resolvedTimeout = timeout === undefined ? 5000 : timeout;
        const resolvedNoteId = noteid || this.generateNoteId();

        if (!this.shouldShowNotification(heading, message, image)) {
            return null;
        }

        if (this.visibleNotifications.length >= this.maxVisibleNotifications) {
            const oldestNotification = this.visibleNotifications.pop();
            if (oldestNotification) {
                this.closeToast(oldestNotification);
            }
        }

        const existingNotifications = document.querySelectorAll(`.${resolvedNoteId}`);
        if (existingNotifications.length > 0) {
            existingNotifications.forEach((notification) => notification.remove());
        }

        const noteBox = document.createElement('div');
        noteBox.id = resolvedNoteId;
        noteBox.className = `notification ${resolvedNoteId}`;
        noteBox.style.display = 'block';
        noteBox.style.visibility = 'hidden';
        noteBox.style.position = 'fixed';
        noteBox.style.top = '10px';
        noteBox.style.right = '10px';
        noteBox.style.zIndex = '10000';
        noteBox.style.transition = 'top 250ms ease';

        const toast = {
            noteId: resolvedNoteId,
            heading,
            message,
            image,
            callback,
            timeout: resolvedTimeout,
            noteBox,
            closed: false
        };

        noteBox.addEventListener('click', () => {
            if (typeof callback === 'function') {
                callback();
            }
            this.closeToast(toast);
        });

        if (image) {
            const noteImageBox = document.createElement('div');
            noteImageBox.className = 'notification_imagebox';

            const noteImage = document.createElement('img');
            noteImage.className = 'notification_image';
            noteImage.src = image;
            noteImageBox.appendChild(noteImage);

            noteBox.appendChild(noteImageBox);
        }

        const noteMessageBox = document.createElement('div');
        noteMessageBox.className = 'notification_messagebox';

        if (heading) {
            const noteMessageHeading = document.createElement('div');
            noteMessageHeading.className = 'notification_title';
            noteMessageHeading.innerHTML = heading;
            noteMessageBox.appendChild(noteMessageHeading);
        }

        const noteMessageBody = document.createElement('div');
        noteMessageBody.className = 'notification_message';
        noteMessageBody.innerHTML = message;
        noteMessageBox.appendChild(noteMessageBody);

        noteBox.appendChild(noteMessageBox);
        document.body.appendChild(noteBox);

        this.visibleNotifications.unshift(toast);
        this.repositionNotifications();

        this.lastNotificationShown = {
            heading,
            message,
            image,
            timestamp: Date.now()
        };

        this.addToNotificationHistory(heading, message, image);

        noteBox.style.visibility = 'visible';
        $(noteBox).hide().fadeIn(500);

        if (resolvedTimeout > 0) {
            toast.timeoutId = setTimeout(() => {
                this.closeToast(toast);
            }, resolvedTimeout);
        }

        return toast;
    }

    /**
     * Starts background notification polling.
     */
    startNotificationFetch() {
        if (this.fetchIntervalId) {
            return;
        }

        console.log(globalThis.lang ? globalThis.lang.translate('notifications.fetching_every_five_seconds') : 'Fetching notifications every 5 seconds');
        this.fetchIntervalId = setInterval(async () => {
            for (const callback of this.notificationLoadStartCallbacks) {
                try {
                    callback();
                } catch (error) {
                    console.error((globalThis.lang ? globalThis.lang.translate('notifications.error.fetch_start_callback_exception_prefix', [error]) : 'Error in notification load start callback: ' + error));
                }
            }

            await fetch('/api/v1.1/Notification').then((response) => {
                if (response.ok) {
                    return response.json();
                }

                console.error((globalThis.lang ? globalThis.lang.translate('notifications.error.failed_fetch_prefix', [response.statusText]) : 'Failed to fetch notifications: ' + response.statusText));
                for (const callback of this.notificationLoadErrorCallbacks) {
                    try {
                        callback(response);
                    } catch (error) {
                        console.error((globalThis.lang ? globalThis.lang.translate('notifications.error.fetch_error_callback_exception_prefix', [error]) : 'Error in notification load error callback: ' + error));
                    }
                }
                return null;
            }).then((data) => {
                if (!data) {
                    return;
                }

                notifications = data;
                for (const callback of this.notificationLoadEndCallbacks) {
                    try {
                        callback(data);
                    } catch (error) {
                        console.error((globalThis.lang ? globalThis.lang.translate('notifications.error.fetch_error_callback_exception_prefix', [error]) : 'Error in notification load end callback: ' + error));
                    }
                }

                if (!this.notificationLibraryUpdateCallbacks.length) {
                    return;
                }

                if (!(notifications.LastLibraryChange || notifications.LastContentChange || notifications.LastMetadataChange)) {
                    return;
                }

                let latestChangeTime = 0;
                if (notifications.LastLibraryChange) {
                    latestChangeTime = Math.max(latestChangeTime, new Date(notifications.LastLibraryChange).getTime());
                }
                if (notifications.LastContentChange) {
                    latestChangeTime = Math.max(latestChangeTime, new Date(notifications.LastContentChange).getTime());
                }
                if (notifications.LastMetadataChange) {
                    latestChangeTime = Math.max(latestChangeTime, new Date(notifications.LastMetadataChange).getTime());
                }

                if (latestChangeTime <= this.notificationLibraryUpdateLastUpdate.getTime() + 30000) {
                    return;
                }

                console.log('Executing callbacks due to change at: ' + new Date(latestChangeTime).toLocaleString());
                this.notificationLibraryUpdateLastUpdate = new Date();

                for (const callback of this.notificationLibraryUpdateCallbacks) {
                    try {
                        callback(data);
                    } catch (error) {
                        console.error((globalThis.lang ? globalThis.lang.translate('notifications.error.fetch_error_callback_exception_prefix', [error]) : 'Error in notification library update callback: ' + error));
                    }
                }
            }).catch((error) => {
                console.error((globalThis.lang ? globalThis.lang.translate('notifications.error.fetch_exception_prefix', [error]) : 'Error fetching notifications: ' + error));
                for (const callback of this.notificationLoadErrorCallbacks) {
                    try {
                        callback(error);
                    } catch (callbackError) {
                        console.error((globalThis.lang ? globalThis.lang.translate('notifications.error.fetch_error_callback_exception_prefix', [callbackError]) : 'Error in notification load error callback: ' + callbackError));
                    }
                }
            });
        }, 5000);
    }

    /**
     * Stops background notification polling.
     */
    stopNotificationFetch() {
        if (!this.fetchIntervalId) {
            return;
        }

        clearInterval(this.fetchIntervalId);
        this.fetchIntervalId = null;
    }

    /**
     * Registers a callback to be invoked when library contents change.
     * Allows pages to resubscribe after the manager is recreated.
     * @param {Function} callback
     */
    addNotificationLibraryUpdateCallback(callback) {
        if (typeof callback === 'function' && !this.notificationLibraryUpdateCallbacks.includes(callback)) {
            this.notificationLibraryUpdateCallbacks.push(callback);
        }
    }

    /**
     * Unregisters a callback previously registered with addNotificationLibraryUpdateCallback.
     * @param {Function} callback
     */
    removeNotificationLibraryUpdateCallback(callback) {
        const index = this.notificationLibraryUpdateCallbacks.indexOf(callback);
        if (index !== -1) {
            this.notificationLibraryUpdateCallbacks.splice(index, 1);
        }
    }

    /**
     * Registers a callback to be invoked when notification fetch starts.
     * @param {Function} callback
     */
    addNotificationLoadStartCallback(callback) {
        if (typeof callback === 'function' && !this.notificationLoadStartCallbacks.includes(callback)) {
            this.notificationLoadStartCallbacks.push(callback);
        }
    }

    /**
     * Registers a callback to be invoked when notification fetch completes.
     * @param {Function} callback
     */
    addNotificationLoadEndCallback(callback) {
        if (typeof callback === 'function' && !this.notificationLoadEndCallbacks.includes(callback)) {
            this.notificationLoadEndCallbacks.push(callback);
        }
    }

    /**
     * Registers a callback to be invoked when notification fetch errors.
     * @param {Function} callback
     */
    addNotificationLoadErrorCallback(callback) {
        if (typeof callback === 'function' && !this.notificationLoadErrorCallbacks.includes(callback)) {
            this.notificationLoadErrorCallbacks.push(callback);
        }
    }

    /**
     * Registers a panel instance for history refresh notifications.
     */
    registerHistoryPanel(panel) {
        this.historyPanels.add(panel);
    }

    /**
     * Returns history after trimming expired entries.
     */
    getNotificationHistory() {
        try {
            const stored = localStorage.getItem(this.historyKey);
            if (!stored) {
                return [];
            }

            let notificationHistory = JSON.parse(stored);
            const now = Date.now();

            notificationHistory = notificationHistory.filter((item) => (now - item.timestamp) < this.historyExpiryMs);

            if (notificationHistory.length > 0) {
                localStorage.setItem(this.historyKey, JSON.stringify(notificationHistory));
            } else {
                localStorage.removeItem(this.historyKey);
            }

            return notificationHistory;
        } catch (error) {
            console.error('Error retrieving notification history:', error);
            return [];
        }
    }

    /**
     * Adds a history record and keeps the configured retention limits.
     */
    addToNotificationHistory(heading, message, image) {
        try {
            let notificationHistory = this.getNotificationHistory();
            const now = Date.now();

            notificationHistory.unshift({ heading, message, image, timestamp: now });

            if (notificationHistory.length > this.historySize) {
                notificationHistory = notificationHistory.slice(0, this.historySize);
            }

            localStorage.setItem(this.historyKey, JSON.stringify(notificationHistory));
            this.notifyHistoryPanels();
        } catch (error) {
            console.error('Error saving notification to history:', error);
        }
    }

    /**
     * Repositions visible toast notifications with animation.
     */
    repositionNotifications() {
        let yPosition = 10;
        for (const notification of this.visibleNotifications) {
            if (notification?.noteBox) {
                notification.noteBox.style.top = yPosition + 'px';
                yPosition += notification.noteBox.offsetHeight + 10;
            }
        }
    }

    /**
     * Closes an existing toast and reflows remaining toasts.
     */
    closeToast(toast) {
        if (!toast || toast.closed || !toast.noteBox) {
            return;
        }

        toast.closed = true;
        if (toast.timeoutId) {
            clearTimeout(toast.timeoutId);
        }

        const index = this.visibleNotifications.indexOf(toast);
        if (index > -1) {
            this.visibleNotifications.splice(index, 1);
        }

        $(toast.noteBox).fadeOut(1000, () => {
            toast.noteBox.remove();
            toast.noteBox = null;
            this.repositionNotifications();
        });
    }

    /**
     * Returns true when a toast should be shown based on dedupe settings.
     */
    shouldShowNotification(heading, message, image) {
        const now = Date.now();
        if (!this.lastNotificationShown) {
            return true;
        }

        const timeSinceLastNotification = now - this.lastNotificationShown.timestamp;
        return !(timeSinceLastNotification < this.duplicateTimeoutMs &&
            this.lastNotificationShown.heading === heading &&
            this.lastNotificationShown.message === message &&
            this.lastNotificationShown.image === image);
    }

    /**
     * Generates a CSS-safe notification id.
     */
    generateNoteId() {
        return 'n' + Math.random().toString(36).slice(2, 11).replaceAll('.', '');
    }

    /**
     * Notifies registered panels that history changed.
     */
    notifyHistoryPanels() {
        for (const panel of this.historyPanels) {
            if (panel && typeof panel.UpdateHistory === 'function') {
                panel.UpdateHistory();
            }
        }
    }
}

/** Shared notification manager instance. */
let notificationManager = null;
let notificationLoadStartCallbacks = [];
let notificationLoadEndCallbacks = [];
let notificationLoadErrorCallbacks = [];
let notificationLibraryUpdateCallbacks = [];

/**
 * Creates a fresh notification manager and optionally keeps app-wide callback wiring.
 * @param {{preserveGlobalCallbacks?: boolean}} options
 * @returns {NotificationManager}
 */
function createNotificationManager(options = {}) {
    const preserveGlobalCallbacks = options.preserveGlobalCallbacks !== false;

    const previousManager = notificationManager;
    const preservedLoadStartCallbacks = preserveGlobalCallbacks && previousManager
        ? [...previousManager.notificationLoadStartCallbacks]
        : [];
    const preservedLoadEndCallbacks = preserveGlobalCallbacks && previousManager
        ? [...previousManager.notificationLoadEndCallbacks]
        : [];
    const preservedLoadErrorCallbacks = preserveGlobalCallbacks && previousManager
        ? [...previousManager.notificationLoadErrorCallbacks]
        : [];
    const preservedHistoryPanels = preserveGlobalCallbacks && previousManager
        ? [...previousManager.historyPanels]
        : [];

    if (previousManager) {
        previousManager.stopNotificationFetch();

        // Fade out existing toasts and clear their timeout handles from the old manager.
        for (const toast of [...previousManager.visibleNotifications]) {
            previousManager.closeToast(toast);
        }
    }

    notificationManager = new NotificationManager();
    globalThis.notificationManager = notificationManager;

    notificationLoadStartCallbacks = notificationManager.notificationLoadStartCallbacks;
    notificationLoadEndCallbacks = notificationManager.notificationLoadEndCallbacks;
    notificationLoadErrorCallbacks = notificationManager.notificationLoadErrorCallbacks;
    notificationLibraryUpdateCallbacks = notificationManager.notificationLibraryUpdateCallbacks;

    if (preservedLoadStartCallbacks.length) {
        notificationLoadStartCallbacks.push(...preservedLoadStartCallbacks);
    }
    if (preservedLoadEndCallbacks.length) {
        notificationLoadEndCallbacks.push(...preservedLoadEndCallbacks);
    }
    if (preservedLoadErrorCallbacks.length) {
        notificationLoadErrorCallbacks.push(...preservedLoadErrorCallbacks);
    }
    if (preservedHistoryPanels.length) {
        for (const panel of preservedHistoryPanels) {
            notificationManager.registerHistoryPanel(panel);
        }
    }

    return notificationManager;
}

/**
 * Recreates notifications for a page transition and restarts polling.
 */
function resetNotificationManagerForPage() {
    createNotificationManager({ preserveGlobalCallbacks: true });
    notificationManager.startNotificationFetch();
}

createNotificationManager();

/**
 * Backward-compatible global entrypoint used by existing pages.
 */
function startNotificationFetch() {
    notificationManager.startNotificationFetch();
}

/**
 * Backward-compatible toast wrapper.
 */
class Notification {
    /**
     * @param {string} heading
     * @param {string} message
     * @param {string|undefined} image
     * @param {Function|undefined} callback
     * @param {number|undefined} timeout
     * @param {string|undefined} noteid
     */
    constructor(heading, message, image, callback, timeout, noteid = undefined) {
        this.heading = heading;
        this.message = message;
        this.image = image;
        this.callback = callback;
        this.timeout = timeout;
        this.noteId = noteid;
    }

    /**
     * Shows this notification using NotificationManager.
     */
    Show() {
        this.instance = notificationManager.showNotification(
            this.heading,
            this.message,
            this.image,
            this.callback,
            this.timeout,
            this.noteId
        );
        return this.instance;
    }
}

/**
 * Notification dropdown panel renderer.
 */
class NotificationPanel {
    constructor() {
        this.panel = document.createElement('div');
        this.panel.id = 'notificationPanel';
        this.panel.classList.add('dropdown-content', 'notification_panel');

        document.body.appendChild(this.panel);

        this.databaseUpgradePanel = document.createElement('div');
        this.databaseUpgradePanel.classList.add('section');
        this.databaseUpgradePanel.classList.add('importNotification');
        this.databaseUpgradePanel.style.display = 'none';

        const databaseUpgradeTitle = document.createElement('div');
        databaseUpgradeTitle.classList.add('section-header');
        databaseUpgradeTitle.innerHTML = globalThis.lang ? globalThis.lang.translate('notifications.section.database_upgrade.title') : 'Database Upgrade';
        this.databaseUpgradePanel.appendChild(databaseUpgradeTitle);

        this.databaseUpgradeBody = document.createElement('div');
        this.databaseUpgradeBody.classList.add('section-body', 'notification_body');
        this.databaseUpgradePanel.appendChild(this.databaseUpgradeBody);

        this.processingPanel = document.createElement('div');
        this.processingPanel.classList.add('section');
        this.processingPanel.classList.add('importNotification');
        this.processingPanel.style.display = 'none';

        const processingTitle = document.createElement('div');
        processingTitle.classList.add('section-header');
        processingTitle.innerHTML = globalThis.lang ? globalThis.lang.translate('notifications.section.import_status.title') : 'Import Status';
        this.processingPanel.appendChild(processingTitle);

        this.processingBody = document.createElement('div');
        this.processingBody.classList.add('section-body', 'notification_body');
        this.processingPanel.appendChild(this.processingBody);

        this.pendingBody = document.createElement('div');
        this.pendingBody.classList.add('section-body', 'notification_body');
        this.processingPanel.appendChild(this.pendingBody);

        this.completedPanel = document.createElement('div');
        this.completedPanel.classList.add('section');
        this.completedPanel.style.display = 'none';

        const completedTitle = document.createElement('div');
        completedTitle.classList.add('section-header');
        completedTitle.innerHTML = globalThis.lang ? globalThis.lang.translate('notifications.section.recently_imported.title') : 'Recently Imported';
        this.completedPanel.appendChild(completedTitle);

        this.completedBody = document.createElement('div');
        this.completedBody.classList.add('section-body', 'notification_body');
        this.completedPanel.appendChild(this.completedBody);

        this.importedNotifications = [];

        this.noNotifications = document.createElement('div');
        this.noNotifications.classList.add('section');

        const noNotificationsBody = document.createElement('div');
        noNotificationsBody.classList.add('section-body');
        noNotificationsBody.innerHTML = globalThis.lang ? globalThis.lang.translate('notifications.none_available') : 'No notifications available';
        this.noNotifications.appendChild(noNotificationsBody);

        this.panel.appendChild(this.processingPanel);
        this.panel.appendChild(this.databaseUpgradePanel);
        this.panel.appendChild(this.completedPanel);
        this.panel.appendChild(this.noNotifications);

        notificationManager.registerHistoryPanel(this);
        notificationLoadEndCallbacks.push((data) => this.Update(data));
    }

    /**
     * Shows the panel.
     */
    Show() {
        if (!this.panel.classList.contains('show')) {
            this.panel.classList.add('show');
        }
    }

    /**
     * Hides the panel.
     */
    Hide() {
        if (this.panel.classList.contains('show')) {
            this.panel.classList.remove('show');
        }
    }

    /**
     * Re-renders local history sections at the top of the panel.
     */
    UpdateHistory() {
        const notificationHistory = notificationManager.getNotificationHistory();
        this.panel.querySelectorAll('.notification_history_section, .notification_recent_import_section').forEach((section) => section.remove());

        const mergedItems = [];

        for (const historyItem of notificationHistory) {
            mergedItems.push({
                type: 'history',
                timestamp: historyItem.timestamp,
                payload: historyItem
            });
        }

        for (const importedItem of this.importedNotifications) {
            const importedTimestamp = new Date(importedItem.lastupdated).getTime();
            mergedItems.push({
                type: 'imported',
                timestamp: Number.isNaN(importedTimestamp) ? 0 : importedTimestamp,
                payload: importedItem
            });
        }

        mergedItems.sort((a, b) => b.timestamp - a.timestamp);

        if (mergedItems.length === 0) {
            return 0;
        }

        const historyFragment = document.createDocumentFragment();
        for (const mergedItem of mergedItems) {
            if (mergedItem.type === 'history') {
                const historyItem = mergedItem.payload;
                const historySection = document.createElement('div');
                historySection.classList.add('section', 'notification_history_section');

                const historyHeader = document.createElement('div');
                historyHeader.classList.add('section-header');
                historyHeader.innerHTML = historyItem.heading || (globalThis.lang ? globalThis.lang.translate('notifications.section.history.title') : 'Notification');
                historySection.appendChild(historyHeader);

                const historyBody = document.createElement('div');
                historyBody.classList.add('section-body', 'notification_body');

                if (historyItem.image) {
                    const historyImageBox = document.createElement('div');
                    historyImageBox.classList.add('notification_imagebox');

                    const historyImage = document.createElement('img');
                    historyImage.classList.add('notification_image');
                    historyImage.src = historyItem.image;
                    historyImage.onerror = () => {
                        historyImageBox.style.display = 'none';
                    };
                    historyImageBox.appendChild(historyImage);
                    historyBody.appendChild(historyImageBox);
                }

                const itemMessage = document.createElement('div');
                itemMessage.classList.add('notification_message');
                itemMessage.innerHTML = historyItem.message;
                historyBody.appendChild(itemMessage);

                const itemDate = document.createElement('div');
                itemDate.classList.add('notification_item_date');
                itemDate.innerHTML = new Date(historyItem.timestamp).toLocaleString();
                historyBody.appendChild(itemDate);

                historySection.appendChild(historyBody);
                historyFragment.appendChild(historySection);
                continue;
            }

            const importedSection = document.createElement('div');
            importedSection.classList.add('section', 'notification_recent_import_section');

            const importedHeader = document.createElement('div');
            importedHeader.classList.add('section-header');
            importedHeader.innerHTML = globalThis.lang ? globalThis.lang.translate('notifications.section.recently_imported.title') : 'Recently Imported';
            importedSection.appendChild(importedHeader);

            const importedBody = document.createElement('div');
            importedBody.classList.add('section-body', 'notification_body');
            importedBody.appendChild(createNotificationPanelItem(mergedItem.payload));
            importedSection.appendChild(importedBody);
            historyFragment.appendChild(importedSection);
        }

        this.panel.insertBefore(historyFragment, this.databaseUpgradePanel);
        return notificationHistory.length;
    }

    /**
     * Renders server-backed status sections and empty state.
     */
    Update(currentNotifications) {
        if (currentNotifications === undefined || currentNotifications === null || currentNotifications.length === 0) {
            this.processingPanel.style.display = 'none';
            this.completedPanel.style.display = 'none';
            this.databaseUpgradePanel.style.display = 'none';
            this.noNotifications.style.display = this.UpdateHistory() === 0 ? 'block' : 'none';
            return;
        }

        let showDatabaseUpgrade = false;
        let showPending = false;
        let showProcessing = false;
        let showCompleted = false;

        this.databaseUpgradeBody.innerHTML = '';
        this.processingBody.innerHTML = '';
        this.pendingBody.innerHTML = '';
        this.completedBody.innerHTML = '';

        if (currentNotifications.databaseUpgrade) {
            const upgradeNotification = document.createElement('div');
            upgradeNotification.classList.add('notification_item');

            const upgradeLabel = document.createElement('span');
            upgradeLabel.innerHTML = globalThis.lang ? globalThis.lang.translate('notifications.database_upgrade.in_progress') : 'Upgrading database...';
            upgradeNotification.appendChild(upgradeLabel);

            const upgradeLabels = {
                MetadataRefresh_Platform: globalThis.lang ? globalThis.lang.translate('notifications.database_upgrade.task.platform_metadata') : 'Platform Metadata',
                MetadataRefresh_Signatures: globalThis.lang ? globalThis.lang.translate('notifications.database_upgrade.task.signature_metadata') : 'Signature Metadata',
                MetadataRefresh_Game: globalThis.lang ? globalThis.lang.translate('notifications.database_upgrade.task.game_metadata') : 'Game Metadata',
                DatabaseMigration_1031: globalThis.lang ? globalThis.lang.translate('notifications.database_upgrade.task.migrating_user_data') : 'Migrating user data to new database schema'
            };

            if (Object.keys(currentNotifications.databaseUpgrade).length > 0) {
                const upgradeList = document.createElement('ul');
                upgradeList.classList.add('password-rules');

                for (const key of Object.keys(currentNotifications.databaseUpgrade)) {
                    const subTask = currentNotifications.databaseUpgrade[key];
                    const upgradeItem = document.createElement('li');
                    upgradeItem.classList.add('listitem-narrow', 'taskrow');

                    switch (subTask.state) {
                        case 'NeverStarted':
                            upgradeItem.classList.add('listitem-pending');
                            break;
                        case 'Running':
                            upgradeItem.classList.add('listitem-inprogress');
                            break;
                        case 'Stopped':
                            upgradeItem.classList.add('listitem-green');
                            break;
                    }

                    upgradeItem.innerHTML = `${upgradeLabels[key] || key}`;
                    if (subTask.progress.split(' of ').length === 2) {
                        const parts = subTask.progress.split(' of ');
                        upgradeItem.innerHTML += `<br /><progress value="${parts[0]}" max="${parts[1]}" style="width: 100%;"></progress>`;
                    } else if (subTask.state === 'Stopped') {
                        upgradeItem.innerHTML += '<br /><progress value="1" max="1" style="width: 100%;"></progress>';
                    } else {
                        upgradeItem.innerHTML += '<br /><progress value="0" max="1" style="width: 100%;"></progress>';
                    }

                    upgradeList.appendChild(upgradeItem);
                }

                upgradeNotification.appendChild(upgradeList);
            }

            const upgradeText = document.createElement('p');
            upgradeText.innerHTML = globalThis.lang ? globalThis.lang.translate('notifications.database_upgrade.explanation') : 'The system is currently performing a database upgrade. This may take some time depending on the size of your library. Some features may not be available during the upgrade.';
            upgradeNotification.appendChild(upgradeText);

            this.databaseUpgradeBody.appendChild(upgradeNotification);
            showDatabaseUpgrade = true;
        }

        if (currentNotifications.importQueue) {
            if (currentNotifications.importQueue.Pending || currentNotifications.importQueue.Processing) {
                if (currentNotifications.importQueue.Pending) {
                    showPending = true;
                    this.pendingBody.innerHTML = globalThis.lang ? globalThis.lang.translate('notifications.import_queue.pending_count', [currentNotifications.importQueue.Pending]) : (currentNotifications.importQueue.Pending + ' imports pending');
                }

                if (currentNotifications.importQueue.Processing) {
                    showProcessing = true;
                    currentNotifications.importQueue.Processing.forEach((item) => {
                        this.processingBody.appendChild(createNotificationPanelItem(item));
                    });
                }
            }

            this.importedNotifications = Array.isArray(currentNotifications.importQueue.imported) ? currentNotifications.importQueue.imported : [];
            showCompleted = this.importedNotifications.length > 0;
        } else {
            this.importedNotifications = [];
        }

        this.databaseUpgradePanel.style.display = showDatabaseUpgrade ? 'block' : 'none';

        if (showProcessing || showPending) {
            this.processingPanel.style.display = 'block';
            this.pendingBody.style.display = showPending ? 'block' : 'none';
            this.processingBody.style.display = showProcessing ? 'block' : 'none';
        } else {
            this.processingPanel.style.display = 'none';
        }

        this.completedPanel.style.display = 'none';

        const historyCount = this.UpdateHistory();
        this.noNotifications.style.display = (!showDatabaseUpgrade && !showPending && !showProcessing && !showCompleted && historyCount === 0) ? 'block' : 'none';
    }
}

/**
 * Creates an import queue row for the notification panel.
 */
function createNotificationPanelItem(importQueueItem) {
    const itemDiv = document.createElement('div');
    itemDiv.classList.add('notification_item');

    const itemName = document.createElement('div');
    itemName.classList.add('notification_item_name');
    itemName.innerHTML = importQueueItem.filename;
    itemDiv.appendChild(itemName);

    const itemDate = document.createElement('div');
    itemDate.classList.add('notification_item_date');
    itemDate.innerHTML = new Date(importQueueItem.lastupdated).toLocaleString();
    itemDiv.appendChild(itemDate);

    return itemDiv;
}

/**
 * Console helper to quickly test notification rendering.
 */
function testNotification(heading = 'Test Notification', message = 'This is a test notification from the browser console.') {
    notificationManager.showNotification(heading, message, undefined, () => console.log('Test notification clicked!'), 5000);
    console.log('Test notification displayed. Check the top-right corner.');
}