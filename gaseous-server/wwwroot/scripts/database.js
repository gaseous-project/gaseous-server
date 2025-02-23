// this class pulls the games library from the server and stores it in the browser's local storage
// this class also provides methods to filter the games library
// this class also provides methods to sort the games library
// this class also provides methods to paginate the games library
// this class also provides methods to search the games library
class Database {
    // database object
    db;

    constructor() {

    }

    // initialize the database
    async Initialize() {
        return new Promise((resolve, reject) => {
            let openRequest = indexedDB.open('gaseous', 1);

            openRequest.addEventListener('error', () => {
                console.error('Error opening database');

                reject();
            });

            openRequest.addEventListener('success', () => {
                this.db = openRequest.result;

                resolve();
            });

            openRequest.addEventListener('upgradeneeded', () => {
                this.db = openRequest.result;

                let platformStore = this.db.createObjectStore('platforms', { keyPath: 'igdbId' });

                let gamesStore = this.db.createObjectStore('games', { keyPath: 'metadataMapId' });
                gamesStore.createIndex('name', 'name', { unique: false });
                gamesStore.createIndex('nameThe', 'nameThe', { unique: false });
                gamesStore.createIndex('platformIds', 'platformIds', { unique: false, multiEntry: true });
                gamesStore.createIndex('hasSavedGame', 'hasSavedGame', { unique: false });
                gamesStore.createIndex('isFavourite', 'isFavourite', { unique: false });
                gamesStore.createIndex('genres', 'genres', { unique: false, multiEntry: true });
                gamesStore.createIndex('themes', 'themes', { unique: false, multiEntry: true });
                gamesStore.createIndex('players', 'players', { unique: false, multiEntry: true });
                gamesStore.createIndex('perspectives', 'perspectives', { unique: false, multiEntry: true });
                gamesStore.createIndex('firstReleaseDate', 'firstReleaseDate', { unique: false });
                gamesStore.createIndex('ageGroups', 'ageGroup', { unique: false });
                gamesStore.createIndex('totalRating', 'totalRating', { unique: false });
                gamesStore.createIndex('totalRatingCount', 'totalRatingCount', { unique: false });

                let genresStore = this.db.createObjectStore('genres', { keyPath: 'name' });

                let themesStore = this.db.createObjectStore('themes', { keyPath: 'name' });

                let playersStore = this.db.createObjectStore('players', { keyPath: 'name' });

                let perspectivesStore = this.db.createObjectStore('perspectives', { keyPath: 'name' });

                let ageGroupsStore = this.db.createObjectStore('ageGroups', { keyPath: 'name' });

                let settingsStore = this.db.createObjectStore('settings', { keyPath: 'name' });
            });
        });
    }

    databaseTerminated = false;
    async DeleteDatabase(resolve, reject) {
        return new Promise((resolve, reject) => {
            // delete the database
            this.db.close();
            indexedDB.deleteDatabase('gaseous');

            // delete ejs databases
            indexedDB.deleteDatabase('EmulatorJS-roms');
            indexedDB.deleteDatabase('EmulatorJS-bios');
            indexedDB.deleteDatabase('EmulatorJS-core');
            indexedDB.deleteDatabase('/data/saves');

            this.databaseTerminated = true;

            clearInterval(this.TimerRefreshDatabase);

            // sleep for 2 seconds
            setTimeout(() => {
                console.log('Database deleted');
                resolve();
            }, 3000);
        });
    }

    // this is the database refresh timer, it's responsible for refreshing the database every 1 minute and 10 seconds
    TimerRefreshDatabase =
        setInterval(async () => {
            if (this.databaseTerminated === true) {
                return;
            }
            console.log('Refreshing database');
            await this.SyncContent();
        }, 70000);

    syncStartCallbacks = [];
    syncFinishCallbacks = [];

    async SyncContent(force, error, onupdatenotrequired) {
        let startSync = force;

        if (force === true) {
            console.log('Forcing database refresh');
            startSync = true;
        } else {
            // get the last attempted fetch - if last fetch is null or more than 5 minutes ago, fetch the games library
            let lastFetch = await this.GetData('settings', 'lastFetch', null);
            if (lastFetch === undefined || lastFetch.value === null || (new Date() - new Date(lastFetch.value)) >= 60000) {
                console.log('Last fetch was more than a minutes ago. Update forced.');
                startSync = true;
            } else {
                console.log('Last fetch was less than a minute ago. Update not required.');

                if (onupdatenotrequired) {
                    onupdatenotrequired();
                }
            }
        }

        if (startSync === true) {
            if (this.syncStartCallbacks.length > 0) {
                for (let callback of this.syncStartCallbacks) {
                    callback();
                }
            }

            this.SetData('settings', 'lastFetch', new Date());
            await this.GetPlatforms();
            await this.GetGames();
            await this.GetGamesFilter();

            if (this.syncFinishCallbacks.length > 0) {
                for (let callback of this.syncFinishCallbacks) {
                    callback();
                }
            }
        }
    }

    async GetPlatforms(callback, error) {
        // fetch the platforms from the server
        await fetch('/api/v1.1/PlatformMaps',
            {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            })
            .then(async response => {
                if (response.ok) {
                    return await response.json();
                } else {
                    throw new Error('Error fetching platforms');
                }
            })
            .then(data => {
                for (const platform of data) {
                    let transaction = this.db.transaction('platforms', 'readwrite');
                    let platformsStore = transaction.objectStore('platforms');
                    platformsStore.put(platform);
                }

                if (callback) {
                    callback(data);
                }
            })
            .catch(err => {
                if (error) {
                    error(err);
                }
            });
    }

    // this method pulls the games library from the server and stores it in the browser's local storage
    // this method also provides a callback
    // this method also provides a method to handle errors
    async GetGames(callback, error) {
        // let filterModel = { "Name": "", "HasSavedGame": false, "isFavourite": false, "Platform": [], "Genre": [], "GameMode": [], "PlayerPerspective": [], "Theme": [], "MinimumReleaseYear": -1, "MaximumReleaseYear": -1, "GameRating": { "MinimumRating": -1, "MinimumRatingCount": -1, "MaximumRating": -1, "MaximumRatingCount": -1, "IncludeUnrated": false }, "GameAgeRating": { "AgeGroupings": [], "IncludeUnrated": false }, "Sorting": { "SortBy": "NameThe", "SortAscending": true } };

        let filterModel = { "Name": "", "HasSavedGame": false, "isFavourite": false, "MinimumReleaseYear": -1, "MaximumReleaseYear": -1, "Sorting": { "SortBy": "NameThe", "SortAscending": true } };

        let dbLoadComplete = false;
        let pageNumber = 1;
        let pageSize = 1000;
        let maxPages = 1000;

        // clear the games library
        let transaction = this.db.transaction('games', 'readwrite');
        let gamesStore = transaction.objectStore('games');
        gamesStore.clear();

        // fetch the games library from the server
        // keep fetching until the games library is complete
        while (!dbLoadComplete) {
            await fetch('/api/v1.1/Games?pageNumber=' + pageNumber + '&pageSize=' + pageSize + '&returnSummary=false&returnGames=true',
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(filterModel)
                })
                .then(async response => {
                    if (response.ok) {
                        return await response.json();
                    } else {
                        throw new Error('Error fetching games');
                    }
                })
                .then(data => {
                    // store the games library in the browser's local storage
                    if (data.games.length === 0) {
                        dbLoadComplete = true;
                    } else {
                        let transaction = this.db.transaction('games', 'readwrite');
                        let ageGroupTransaction = this.db.transaction('ageGroups', 'readwrite');

                        for (const game of data.games) {
                            let gamesStore = transaction.objectStore('games');
                            gamesStore.put(game);

                            this.#InsertFilters('genres', game.genres);
                            this.#InsertFilters('themes', game.themes);
                            this.#InsertFilters('players', game.players);
                            this.#InsertFilters('perspectives', game.perspectives);

                            let ageGroupsStore = ageGroupTransaction.objectStore('ageGroups');
                            ageGroupsStore.put({ name: game.ageGroup });
                        }

                        transaction.onsuccess = () => {
                            transaction.commit();
                        }

                        ageGroupTransaction.onsuccess = () => {
                            ageGroupTransaction.commit();
                        }
                    }
                })
                .catch(err => {
                    if (error) {
                        error(err);
                    }
                });

            if (maxPages !== -1 && pageNumber >= maxPages) {
                dbLoadComplete = true;
            }
            pageNumber += 1;
        }
    }

    #InsertFilters(tableName, source) {
        let genreTransaction = this.db.transaction(tableName, 'readwrite');
        let itemStore = genreTransaction.objectStore(tableName);
        for (const item of source) {
            itemStore.put({ name: item });
        }

        genreTransaction.onsuccess = () => {
            genreTransaction.commit();
        }
    }

    async GetGamesFilter() {
        this.filter = {
            "platforms": [],
            "genres": [],
            "themes": [],
            "players": [],
            "perspectives": [],
            "ageGroups": []
        };

        await this.#GetPlatformsFilter((items) => {
            this.filter["platforms"] = items;
        });

        await this.#GetGamesFilter('genres', true, (items) => {
            this.filter["genres"] = items;

            for (const item of items) {
                this.#GetGameCount('genres', item.name, (count) => {
                    item.gameCount = count;
                }, (err) => {
                    console.error(err);
                });
            }
        });

        await this.#GetGamesFilter('themes', true, (items) => {
            this.filter["themes"] = items;

            for (const item of items) {
                this.#GetGameCount('themes', item.name, (count) => {
                    item.gameCount = count;
                }, (err) => {
                    console.error(err);
                });
            }
        });

        await this.#GetGamesFilter('players', true, (items) => {
            this.filter["players"] = items;

            for (const item of items) {
                this.#GetGameCount('players', item.name, (count) => {
                    item.gameCount = count;
                }, (err) => {
                    console.error(err);
                });
            }
        });

        await this.#GetGamesFilter('perspectives', true, (items) => {
            this.filter["perspectives"] = items;

            for (const item of items) {
                this.#GetGameCount('perspectives', item.name, (count) => {
                    item.gameCount = count;
                }, (err) => {
                    console.error(err);
                });
            }
        });

        await this.#GetGamesFilter('ageGroups', false, (items) => {
            this.filter["ageGroups"] = items;

            for (const item of items) {
                this.#GetGameCount('ageGroups', item.name, (count) => {
                    item.gameCount = count;
                }, (err) => {
                    console.error(err);
                });
            }
        });

        // execute filter callbacks
        for (let callback of this.FilterCallbacks) {
            callback(this.filter);
        }
    }

    FilterCallbacks = [];

    async #GetGamesFilter(tableName, sort, callback) {
        return new Promise((resolve, reject) => {
            let fItemTransaction = this.db.transaction(tableName, 'readonly');
            let fItemStore = fItemTransaction.objectStore(tableName);

            let items = [];

            fItemTransaction.oncomplete = () => {
                if (sort === true) {
                    items.sort((a, b) => {
                        return a.name.localeCompare(b.name);
                    });
                }

                resolve(callback(items));
            };

            let fItemCursor = fItemStore.openCursor();

            fItemCursor.onerror = () => {
                console.error('Error opening cursor');

                reject('Error opening cursor');
            };

            fItemCursor.onsuccess = async () => {
                let cursor = fItemCursor.result;
                if (cursor) {
                    let Item = cursor.value;
                    let fItem = {
                        "name": Item.name,
                        "gameCount": 0
                    };
                    items.push(fItem);
                    cursor.continue();
                }
            };
        });
    }

    async #GetPlatformsFilter(callback) {
        return new Promise((resolve, reject) => {
            let fPlatformTransaction = this.db.transaction('platforms', 'readonly');
            let fPlatformStore = fPlatformTransaction.objectStore('platforms');

            let items = [];

            fPlatformTransaction.oncomplete = () => {
                items.sort((a, b) => {
                    return a.name.localeCompare(b.name);
                });

                for (const platform of items) {
                    this.#GetGameCount('platformIds', platform.id, (count) => {
                        platform.gameCount = count;
                    }, (err) => {
                        console.error(err);
                    });
                }

                resolve(callback(items));
            };

            let fPlatformCursor = fPlatformStore.openCursor();

            fPlatformCursor.onerror = () => {
                console.error('Error opening cursor');

                reject('Error opening cursor');
            };

            fPlatformCursor.onsuccess = () => {
                let cursor = fPlatformCursor.result;
                if (cursor) {
                    let platform = cursor.value;
                    let fPlatform = {
                        "id": platform.igdbId,
                        "name": platform.igdbName,
                        "gameCount": 0
                    };
                    items.push(fPlatform);
                    cursor.continue();
                }
            };
        });
    }

    async #GetGameCount(filterIndex, filterValue, callback, error) {
        let transaction = this.db.transaction("games", "readonly");
        let objectStore = transaction.objectStore("games");
        try {
            let index = objectStore.index(filterIndex);

            let countRequest = index.count(filterValue);

            countRequest.onsuccess = async () => {
                await callback(countRequest.result);
            };

            countRequest.onerror = () => {
                error(countRequest.error);
            };
        }
        catch (err) {
            console.error(filterIndex + ' index does not exist');
            error(err);
        }
    }

    async GetData(tableName, key, defaultValue) {
        return new Promise((resolve, reject) => {
            let transaction = this.db.transaction(tableName, 'readonly');
            let objectStore = transaction.objectStore(tableName);

            let request = objectStore.get(key);

            request.onsuccess = () => {
                resolve(request.result);
            };

            request.onerror = () => {
                reject(defaultValue);
            };
        });
    }

    async SetData(tableName, key, data) {
        return new Promise((resolve, reject) => {
            let transaction = this.db.transaction(tableName, 'readwrite');
            let objectStore = transaction.objectStore(tableName);

            let storedData = { name: key, value: data };

            let request = objectStore.put(storedData);

            request.onsuccess = () => {
                resolve();
            };

            request.onerror = () => {
                reject();
            };
        });
    }
}