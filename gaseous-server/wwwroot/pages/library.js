const DEFAULT_TILE_SIZE = {
    width: 232,
    height: 283
};

let coverURLList = [];
let coverURLSet = new Set();
let scrollTimer = null;
let notificationRefreshTimer = null;
let backgroundImageHandler = globalThis.backgroundImageHandler;

const filter = new Filtering();
filter.GetSummary = false;

const libraryState = {
    activeControllers: new Map(),
    currentPageSize: 20,
    fetchOverscanPages: 2,
    hasLoadedAtLeastOnce: false,
    layout: {
        columns: 1,
        height: DEFAULT_TILE_SIZE.height,
        tileHeight: DEFAULT_TILE_SIZE.height,
        tileWidth: DEFAULT_TILE_SIZE.width,
        width: DEFAULT_TILE_SIZE.width
    },
    loadedPages: new Set(),
    loadingInterval: null,
    notificationCallback: null,
    pageTileIndexes: new Map(),
    pendingDomReset: false,
    pendingPages: new Map(),
    queryGeneration: 0,
    renderedTiles: new Map(),
    resizeHandler: null,
    retainOverscanPages: 5,
    scrollDebounceMs: 220,
    scrollHandler: null
};

/**
 * Returns cached DOM references used by the library page.
 * @returns {{alphaPager: HTMLElement|null, filterPanel: HTMLElement|null, gameCount: HTMLElement|null, gamesElement: HTMLElement|null, gamesHome: HTMLElement|null, libraryControls: HTMLElement|null, scrollerElement: HTMLElement|null}}
 */
function getLibraryElements() {
    return {
        alphaPager: document.getElementById('games_library_alpha_pager'),
        filterPanel: document.getElementById('games_filter_panel'),
        gameCount: document.getElementById('games_library_recordcount'),
        gamesElement: document.getElementById('games_library'),
        gamesHome: document.getElementById('games_home'),
        libraryControls: document.getElementById('games_library_controls'),
        scrollerElement: document.getElementById('games_filter_scroller')
    };
}

/**
 * Reads the active page size from filter selections.
 * @returns {number}
 */
function getCurrentPageSize() {
    const rawValue = filter.filterSelections.pageSize || '20';
    const pageSize = Number.parseInt(rawValue, 10);
    return Number.isNaN(pageSize) ? 20 : pageSize;
}

/**
 * Resolves display flags used to render game tiles.
 * @returns {{classificationDisplayOrder: string[], showClassification: boolean, showRatings: boolean, showTitle: boolean}}
 */
function getDisplayPreferences() {
    return {
        classificationDisplayOrder: GetRatingsBoards(),
        showClassification: GetPreference('Library.ShowGameClassification'),
        showRatings: GetPreference('Library.ShowGameRating'),
        showTitle: GetPreference('Library.ShowGameTitle')
    };
}

function getTileSize() {
    return CalculateTileSize() || DEFAULT_TILE_SIZE;
}

/**
 * Calculates grid metrics for the current viewport and filter panel state.
 * @returns {{columns: number, height: number, tileHeight: number, tileWidth: number, width: number}}
 */
function calculateLayout() {
    const { alphaPager, filterPanel } = getLibraryElements();
    const tileSize = getTileSize();

    let width = Math.floor(globalThis.innerWidth - (alphaPager ? alphaPager.clientWidth : 0));
    if (filterPanel?.style.display === 'block') {
        width -= filterPanel.clientWidth;
    }

    width = Math.max(tileSize.width, width);

    const columns = Math.max(1, Math.floor(width / tileSize.width));
    const totalRows = Math.max(1, Math.ceil(Math.max(filter.GameCount || 0, 1) / columns));

    return {
        columns,
        height: totalRows * tileSize.height,
        tileHeight: tileSize.height,
        tileWidth: tileSize.width,
        width
    };
}

/**
 * Positions a tile using its absolute index in the filtered result set.
 * @param {HTMLElement} tileElement
 * @param {number} index
 */
function positionTile(tileElement, index) {
    const { columns, tileHeight, tileWidth } = libraryState.layout;
    const row = Math.floor(index / columns);
    const column = index % columns;

    tileElement.dataset.index = String(index);
    tileElement.style.left = `${column * tileWidth}px`;
    tileElement.style.position = 'absolute';
    tileElement.style.top = `${row * tileHeight}px`;
}

/**
 * Recomputes container dimensions and repositions any rendered tiles.
 * @returns {Promise<void>}
 */
async function ResizeLibraryPanel() {
    const { gamesElement } = getLibraryElements();
    if (!gamesElement) {
        return;
    }

    libraryState.layout = calculateLayout();
    gamesElement.style.height = `${libraryState.layout.height}px`;
    gamesElement.style.position = 'relative';
    gamesElement.style.width = `${libraryState.layout.width}px`;

    for (const [index, tileElement] of libraryState.renderedTiles.entries()) {
        positionTile(tileElement, index);
    }
}

function updateGameCountDisplay() {
    const { gameCount } = getLibraryElements();
    if (!gameCount) {
        return;
    }

    if (filter.GameCount === 1) {
        gameCount.innerText = globalThis.lang.translate('library.game_count.one', [filter.GameCount]);
        return;
    }

    gameCount.innerText = globalThis.lang.translate('library.game_count.other', [filter.GameCount]);
}

/**
 * Rebuilds the alpha pager and wires click handlers for indexed jumps.
 */
function buildAlphaPager() {
    const { alphaPager } = getLibraryElements();
    if (!alphaPager) {
        return;
    }

    alphaPager.innerHTML = '';
    for (const [key, value] of Object.entries(filter.AlphaList || {})) {
        const alphaSpan = document.createElement('span');
        alphaSpan.classList.add('games_library_alpha_pager_letter');
        alphaSpan.dataset.letter = key;
        alphaSpan.innerText = key;
        alphaSpan.addEventListener('click', async () => {
            await jumpToAlphaIndex(value.index);
        });
        alphaPager.appendChild(alphaSpan);
    }
}

function showLoadingIndicator() {
    if (libraryState.renderedTiles.size > 0) {
        return;
    }

    const { gamesElement } = getLibraryElements();
    if (!gamesElement) {
        return;
    }

    let loadingElement = document.getElementById('games_library_loading');
    if (!loadingElement) {
        loadingElement = document.createElement('div');
        loadingElement.id = 'games_library_loading';
        loadingElement.classList.add('loadingElement');
        gamesElement.appendChild(loadingElement);
    }

    if (libraryState.loadingInterval) {
        return;
    }

    let charCount = 0;
    libraryState.loadingInterval = globalThis.setInterval(() => {
        charCount = (charCount + 1) % 4;
        loadingElement.innerHTML = globalThis.lang.translate('generic.loading') + '.'.repeat(charCount) + '&nbsp;'.repeat(3 - charCount);
    }, 1000);
}

function hideLoadingIndicator() {
    if (libraryState.loadingInterval) {
        globalThis.clearInterval(libraryState.loadingInterval);
        libraryState.loadingInterval = null;
    }

    const loadingElement = document.getElementById('games_library_loading');
    if (loadingElement) {
        loadingElement.remove();
    }
}

function resetCoverList() {
    coverURLList = [];
    coverURLSet = new Set();
}

function addCoverUrl(game) {
    if (!game.cover) {
        return;
    }

    const coverUrl = '/api/v1.1/Games/' + game.metadataMapId + '/' + game.metadataSource + '/cover/' + game.cover + '/image/original/' + game.cover + '.jpg';
    if (coverURLSet.has(coverUrl)) {
        return;
    }

    coverURLSet.add(coverUrl);
    coverURLList.push(coverUrl);

    if (backgroundImageHandler?.URLList && !backgroundImageHandler.URLList.includes(coverUrl)) {
        backgroundImageHandler.URLList.push(coverUrl);
    }
}

function ensureBackgroundRotator() {
    if (!coverURLList.length) {
        return;
    }

    if (!backgroundImageHandler) {
        backgroundImageHandler = new BackgroundImageRotator(coverURLList, null, true, false);
        globalThis.backgroundImageHandler = backgroundImageHandler;
        return;
    }

    if (backgroundImageHandler.URLList) {
        backgroundImageHandler.URLList = coverURLList.slice();
    }
}

async function buildTile(game) {
    const displayPreferences = getDisplayPreferences();
    const tileElement = document.createElement('div');
    tileElement.classList.add('game_tile_wrapper_icon');
    tileElement.dataset.index = String(game.index);

    const gameObj = new GameIcon(game);
    const gameTile = await gameObj.Render(
        displayPreferences.showTitle,
        displayPreferences.showRatings,
        displayPreferences.showClassification,
        displayPreferences.classificationDisplayOrder
    );

    tileElement.appendChild(gameTile);
    addCoverUrl(game);
    return tileElement;
}

function clearRenderedTiles() {
    const { gamesElement } = getLibraryElements();
    if (gamesElement) {
        gamesElement.innerHTML = '';
    }

    libraryState.loadedPages.clear();
    libraryState.pageTileIndexes.clear();
    libraryState.renderedTiles.clear();
}

function removePage(pageNumber) {
    const tileIndexes = libraryState.pageTileIndexes.get(pageNumber);
    if (!tileIndexes) {
        return;
    }

    for (const tileIndex of tileIndexes) {
        const tileElement = libraryState.renderedTiles.get(tileIndex);
        if (tileElement) {
            tileElement.remove();
            libraryState.renderedTiles.delete(tileIndex);
        }
    }

    libraryState.loadedPages.delete(pageNumber);
    libraryState.pageTileIndexes.delete(pageNumber);
}

function abortActiveRequests() {
    for (const controller of libraryState.activeControllers.values()) {
        controller.abort();
    }

    libraryState.activeControllers.clear();
    libraryState.pendingPages.clear();
}

function registerRequestController(requestKey, controller) {
    libraryState.activeControllers.set(requestKey, controller);
}

function unregisterRequestController(requestKey) {
    libraryState.activeControllers.delete(requestKey);
}

function getVisiblePageRange(pageSize) {
    const scrollTop = globalThis.scrollY;
    const viewportBottom = scrollTop + globalThis.innerHeight;
    const rowHeight = Math.max(libraryState.layout.tileHeight, 1);
    const columns = Math.max(libraryState.layout.columns, 1);
    const totalCount = Math.max(filter.GameCount || 0, 1);
    const maxIndex = totalCount - 1;

    const firstVisibleRow = Math.max(0, Math.floor(scrollTop / rowHeight));
    const lastVisibleRow = Math.max(firstVisibleRow, Math.floor(viewportBottom / rowHeight));

    const firstVisibleIndex = Math.min(maxIndex, firstVisibleRow * columns);
    const lastVisibleIndex = Math.min(maxIndex, ((lastVisibleRow + 1) * columns) - 1);

    return {
        firstPage: Math.max(1, Math.floor(firstVisibleIndex / pageSize) + 1),
        lastPage: Math.max(1, Math.floor(lastVisibleIndex / pageSize) + 1)
    };
}

async function renderPage(pageNumber, games, replaceExistingDom = false) {
    const { gamesElement } = getLibraryElements();
    if (!gamesElement) {
        return;
    }

    const fragment = document.createDocumentFragment();
    const pageTileIndexes = [];
    const renderedTiles = [];

    for (const game of games) {
        const tileElement = await buildTile(game);
        renderedTiles.push([game.index, tileElement]);
        pageTileIndexes.push(game.index);
    }

    if (replaceExistingDom) {
        clearRenderedTiles();
    } else {
        removePage(pageNumber);
    }

    for (const [index, tileElement] of renderedTiles) {
        positionTile(tileElement, index);
        fragment.appendChild(tileElement);
        libraryState.renderedTiles.set(index, tileElement);
    }

    gamesElement.appendChild(fragment);
    libraryState.loadedPages.add(pageNumber);
    libraryState.pageTileIndexes.set(pageNumber, pageTileIndexes);

    hideLoadingIndicator();
    ensureBackgroundRotator();
}

/**
 * Fetches summary metadata (count + alpha map) for the active query.
 * @param {number} queryGeneration
 * @returns {Promise<boolean>}
 */
async function loadSummary(queryGeneration) {
    const requestKey = `summary:${queryGeneration}`;
    const controller = new AbortController();
    registerRequestController(requestKey, controller);

    try {
        const data = await filter.ExecuteFilter(1, libraryState.currentPageSize, {
            returnGames: false,
            returnSummary: true,
            signal: controller.signal,
            suppressBeginCallback: true,
            suppressCompleteCallback: true
        });

        if (!data || queryGeneration !== libraryState.queryGeneration) {
            return false;
        }

        updateGameCountDisplay();
        buildAlphaPager();
        await ResizeLibraryPanel();
        return true;
    } finally {
        unregisterRequestController(requestKey);
    }
}

/**
 * Fetches and renders a specific page for the active query generation.
 * @param {number} pageNumber
 * @param {number} queryGeneration
 * @param {boolean} replaceExistingDom
 * @returns {Promise<any>}
 */
async function requestPage(pageNumber, queryGeneration, replaceExistingDom = false) {
    if (pageNumber < 1) {
        return null;
    }

    if (libraryState.pendingPages.has(pageNumber)) {
        return libraryState.pendingPages.get(pageNumber);
    }

    const requestKey = `page:${queryGeneration}:${pageNumber}`;
    const controller = new AbortController();
    registerRequestController(requestKey, controller);

    const pagePromise = (async () => {
        try {
            const data = await filter.ExecuteFilter(pageNumber, libraryState.currentPageSize, {
                returnGames: true,
                returnSummary: false,
                signal: controller.signal,
                suppressBeginCallback: true,
                suppressCompleteCallback: true
            });

            if (!data || queryGeneration !== libraryState.queryGeneration) {
                return null;
            }

            await renderPage(pageNumber, data.games || [], replaceExistingDom);
            if (replaceExistingDom) {
                libraryState.pendingDomReset = false;
            }
            return data;
        } finally {
            libraryState.pendingPages.delete(pageNumber);
            unregisterRequestController(requestKey);
        }
    })();

    libraryState.pendingPages.set(pageNumber, pagePromise);
    return pagePromise;
}

function pruneFarPages(firstVisiblePage, lastVisiblePage, totalPages) {
    const minPage = Math.max(1, firstVisiblePage - libraryState.retainOverscanPages);
    const maxPage = Math.min(totalPages, lastVisiblePage + libraryState.retainOverscanPages);

    for (const pageNumber of Array.from(libraryState.pageTileIndexes.keys())) {
        if (pageNumber < minPage || pageNumber > maxPage) {
            removePage(pageNumber);
        }
    }
}

async function ensurePagesForViewport(queryGeneration) {
    if (queryGeneration !== libraryState.queryGeneration || !filter.GameCount) {
        return;
    }

    const totalPages = Math.max(1, Math.ceil(filter.GameCount / libraryState.currentPageSize));
    const visibleRange = getVisiblePageRange(libraryState.currentPageSize);
    const startPage = Math.max(1, visibleRange.firstPage - libraryState.fetchOverscanPages);
    const endPage = Math.min(totalPages, visibleRange.lastPage + libraryState.fetchOverscanPages);
    const pagesToLoad = [];

    for (let pageNumber = startPage; pageNumber <= endPage; pageNumber++) {
        if (!libraryState.loadedPages.has(pageNumber) && !libraryState.pendingPages.has(pageNumber)) {
            pagesToLoad.push(pageNumber);
        }
    }

    if (libraryState.pendingDomReset) {
        const firstPageToLoad = pagesToLoad[0] || 1;
        await requestPage(firstPageToLoad, queryGeneration, true);
    }

    const remainingPages = pagesToLoad.filter((pageNumber) => {
        return !libraryState.loadedPages.has(pageNumber);
    });

    await Promise.all(remainingPages.map((pageNumber) => {
        return requestPage(pageNumber, queryGeneration, false);
    }));

    pruneFarPages(visibleRange.firstPage, visibleRange.lastPage, totalPages);
}

/**
 * Scrolls to the first item for the selected alpha index.
 * @param {number} index
 * @returns {Promise<void>}
 */
async function jumpToAlphaIndex(index) {
    await ResizeLibraryPanel();

    const targetPage = Math.floor(index / libraryState.currentPageSize) + 1;
    if (!libraryState.loadedPages.has(targetPage)) {
        showLoadingIndicator();
        await requestPage(targetPage, libraryState.queryGeneration, false);
    }

    const targetRow = Math.floor(index / libraryState.layout.columns);
    const targetTop = Math.max(0, (targetRow * libraryState.layout.tileHeight) - 16);
    globalThis.scrollTo({
        behavior: 'smooth',
        top: targetTop
    });
    scheduleViewportLoad();
}

/**
 * Debounces viewport-driven page loading while the user is scrolling.
 */
function scheduleViewportLoad() {
    if (scrollTimer) {
        globalThis.clearTimeout(scrollTimer);
    }

    scrollTimer = globalThis.setTimeout(async () => {
        localStorage.setItem('Library.ScrollPosition', String(globalThis.scrollY));
        await ensurePagesForViewport(libraryState.queryGeneration);
    }, libraryState.scrollDebounceMs);
}

/**
 * Starts a new query cycle and refreshes visible pages.
 * @param {{preserveScroll?: boolean, softRefresh?: boolean}} options
 * @returns {Promise<void>}
 */
async function startLibraryQuery({ preserveScroll = false, softRefresh = false } = {}) {
    const storedScrollPosition = Number(localStorage.getItem('Library.ScrollPosition') || '0');
    const queryGeneration = libraryState.queryGeneration + 1;
    libraryState.queryGeneration = queryGeneration;
    libraryState.currentPageSize = getCurrentPageSize();

    abortActiveRequests();

    if (!softRefresh) {
        libraryState.loadedPages.clear();
        libraryState.pageTileIndexes.clear();
        libraryState.pendingDomReset = true;
        libraryState.renderedTiles.clear();
        resetCoverList();

        if (!preserveScroll) {
            globalThis.scrollTo(0, 0);
        }
    }

    showLoadingIndicator();

    const summaryLoaded = await loadSummary(queryGeneration);
    if (!summaryLoaded || queryGeneration !== libraryState.queryGeneration) {
        return;
    }

    if (!filter.GameCount) {
        clearRenderedTiles();
        libraryState.pendingDomReset = false;
        hideLoadingIndicator();
        libraryState.hasLoadedAtLeastOnce = true;
        return;
    }

    if (preserveScroll) {
        globalThis.scrollTo(0, storedScrollPosition);
    }

    if (softRefresh) {
        const visibleRange = getVisiblePageRange(libraryState.currentPageSize);
        const totalPages = Math.max(1, Math.ceil(filter.GameCount / libraryState.currentPageSize));
        const firstPage = Math.max(1, visibleRange.firstPage - 1);
        const lastPage = Math.min(totalPages, visibleRange.lastPage + 1);

        for (let pageNumber = firstPage; pageNumber <= lastPage; pageNumber++) {
            libraryState.loadedPages.delete(pageNumber);
        }
    }

    await ensurePagesForViewport(queryGeneration);
    libraryState.hasLoadedAtLeastOnce = true;
}

/**
 * Coalesces bursty notification updates into a single soft refresh.
 */
function queueNotificationRefresh() {
    if (notificationRefreshTimer) {
        globalThis.clearTimeout(notificationRefreshTimer);
    }

    notificationRefreshTimer = globalThis.setTimeout(async () => {
        notificationRefreshTimer = null;
        localStorage.setItem('Library.ScrollPosition', String(globalThis.scrollY));
        await startLibraryQuery({ preserveScroll: true, softRefresh: true });
    }, 1000);
}

/**
 * Initializes select2 for a native select while keeping logic in native listeners.
 * @param {string} elementId
 * @param {string|number|undefined} selectedValue
 * @returns {HTMLElement|null}
 */
function initializeSelect2(elementId, selectedValue) {
    const element = document.getElementById(elementId);
    if (!element) {
        return null;
    }

    if (selectedValue !== undefined) {
        element.value = selectedValue;
    }

    const selectElement = globalThis.$(element);
    selectElement.select2();
    if (selectedValue !== undefined) {
        selectElement.val(selectedValue).trigger('change.select2');
    }

    return element;
}

/**
 * Bootstraps the library page controls, callbacks, and listeners.
 * @returns {Promise<void>}
 */
async function SetupPage() {
    document.getElementById('games_filter_button_column').addEventListener('click', () => {
        FilterDisplayToggle();
    });
    document.getElementById('games_filter_button_column_filter').addEventListener('click', () => {
        FilterDisplayToggle();
    });

    const displayFilter = GetPreference('Library.ShowFilter');
    await FilterDisplayToggle(displayFilter, true);

    const { scrollerElement } = getLibraryElements();
    if (!filter || !scrollerElement) {
        return;
    }

    filter.FilterCallbacks.push(async (result, dontExecuteFilter) => {
        filter.LoadFilterSettings();

        scrollerElement.innerHTML = '';
        scrollerElement.appendChild(filter.BuildFilterTable(result));

        filter.OrderBySelector(document.getElementById('games_library_orderby_select'));
        filter.OrderDirectionSelector(document.getElementById('games_library_orderby_direction_select'));
        filter.PageSizeSelector(document.getElementById('games_library_pagesize_select'));

        initializeSelect2('games_library_pagesize_select', filter.filterSelections.pageSize);
        initializeSelect2('games_library_orderby_select', filter.filterSelections.orderBy);
        initializeSelect2('games_library_orderby_direction_select', filter.filterSelections.orderDirection);

        filter.applyCallback = async () => {
            await startLibraryQuery({
                preserveScroll: !libraryState.hasLoadedAtLeastOnce,
                softRefresh: false
            });
            return false;
        };

        if (dontExecuteFilter === false) {
            await filter.ApplyFilter();
        }
    });

    await filter.GetGamesFilter();

    libraryState.notificationCallback = async () => {
        queueNotificationRefresh();
    };
    notificationLibraryUpdateCallbacks.push(libraryState.notificationCallback);

    libraryState.scrollHandler = () => {
        scheduleViewportLoad();
    };
    globalThis.addEventListener('scroll', libraryState.scrollHandler, { passive: true });

    libraryState.resizeHandler = async () => {
        await ResizeLibraryPanel();
        scheduleViewportLoad();
    };
    globalThis.addEventListener('resize', libraryState.resizeHandler);
}

/**
 * Toggles the filter panel and persists the expanded/collapsed preference.
 * @param {boolean|undefined} display
 * @param {boolean} storePreference
 * @returns {Promise<void>}
 */
async function FilterDisplayToggle(display, storePreference = true) {
    const { filterPanel, gamesHome, libraryControls } = getLibraryElements();

    if (!filterPanel || !gamesHome || !libraryControls) {
        return;
    }

    if (filterPanel.style.display === 'none' || display === true) {
        filterPanel.style.display = 'block';
        libraryControls.classList.add('games_library_controls_collapsed');
        libraryControls.classList.remove('games_library_controls_expanded');
        gamesHome.classList.add('games_home_collapsed');
        gamesHome.classList.remove('games_home_expanded');
        if (storePreference === true) {
            SetPreference('Library.ShowFilter', true);
        }
    } else {
        filterPanel.style.display = 'none';
        libraryControls.classList.add('games_library_controls_expanded');
        libraryControls.classList.remove('games_library_controls_collapsed');
        gamesHome.classList.add('games_home_expanded');
        gamesHome.classList.remove('games_home_collapsed');
        if (storePreference === true) {
            SetPreference('Library.ShowFilter', false);
        }
    }

    await ResizeLibraryPanel();
    scheduleViewportLoad();
}

/**
 * Measures tile dimensions from computed styles.
 * @returns {{height: number, width: number}|null}
 */
function CalculateTileSize() {
    const cssClass = document.querySelector('.game_tile');
    if (cssClass !== null) {
        const cssClassStyle = getComputedStyle(cssClass);
        if (cssClassStyle !== null) {
            let gameTileWidth = Number(cssClassStyle.marginLeft.replace('px', '')) + Number(cssClassStyle.width.replace('px', '')) + Number(cssClassStyle.marginRight.replace('px', ''));
            let gameTileHeight = Number(cssClassStyle.marginTop.replace('px', '')) + Number(cssClassStyle.height.replace('px', '')) + Number(cssClassStyle.marginBottom.replace('px', ''));
            const gameTileLabelBox = document.querySelector('.game_tile_label_box');
            if (gameTileLabelBox !== null) {
                const gameTileLabelBoxStyle = getComputedStyle(gameTileLabelBox);
                if (gameTileLabelBoxStyle !== null) {
                    gameTileHeight += Number(gameTileLabelBoxStyle.height.replace('px', ''));
                }
            }
            return { height: gameTileHeight, width: gameTileWidth };
        }
    }
    return null;
}

prefsDialog.OkCallbacks.push(async () => {
    await startLibraryQuery({ preserveScroll: true, softRefresh: true });
});

if (typeof registerPageUnloadCallback === 'function') {
    /**
     * Releases timers, listeners, and transient page state on SPA navigation.
     */
    registerPageUnloadCallback('library', async () => {
        console.log('Cleaning up library page...');

        if (scrollTimer) {
            globalThis.clearTimeout(scrollTimer);
            scrollTimer = null;
        }
        if (notificationRefreshTimer) {
            globalThis.clearTimeout(notificationRefreshTimer);
            notificationRefreshTimer = null;
        }

        localStorage.setItem('Library.ScrollPosition', String(globalThis.scrollY));

        abortActiveRequests();
        hideLoadingIndicator();

        if (libraryState.notificationCallback) {
            const callbackIndex = notificationLibraryUpdateCallbacks.indexOf(libraryState.notificationCallback);
            if (callbackIndex !== -1) {
                notificationLibraryUpdateCallbacks.splice(callbackIndex, 1);
            }
            libraryState.notificationCallback = null;
        }

        if (libraryState.scrollHandler) {
            globalThis.removeEventListener('scroll', libraryState.scrollHandler);
            libraryState.scrollHandler = null;
        }
        if (libraryState.resizeHandler) {
            globalThis.removeEventListener('resize', libraryState.resizeHandler);
            libraryState.resizeHandler = null;
        }

        clearRenderedTiles();
        resetCoverList();

        if (backgroundImageHandler?.RotationTimer) {
            globalThis.clearInterval(backgroundImageHandler.RotationTimer);
        }
        backgroundImageHandler = undefined;
        globalThis.backgroundImageHandler = undefined;

        console.log('Library page cleanup completed');
    });
}

(async () => {
    try {
        await SetupPage();
    } catch (error) {
        console.error('Failed to setup library page', error);
    }
})();