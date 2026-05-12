const CACHE_NAME = "gaseous-offline-v2";
const FILE_LIST_URL = "/pwa-file-list.json";
const CORE_ASSETS = ["/", "/index.html", "/site.webmanifest", "/service-worker.js"];
const DEFERRED_PREFIX = "/emulators/EmulatorJS/";
const DEFERRED_CACHE_MARKER = "/__deferred_emulator_cache_complete__";

let deferredCachePromise = null;

self.addEventListener("install", (event) => {
    event.waitUntil(precacheAllAssets());
    globalThis.skipWaiting();
});

self.addEventListener("activate", (event) => {
    event.waitUntil(cleanupOldCaches());
    globalThis.clients.claim();
});

self.addEventListener("fetch", (event) => {
    if (event.request.method !== "GET") {
        return;
    }

    const requestUrl = new URL(event.request.url);
    if (requestUrl.origin !== globalThis.location.origin) {
        return;
    }

    // API responses are dynamic and can be large (e.g., game images); avoid SW cache overhead.
    if (requestUrl.pathname.startsWith("/api/")) {
        event.respondWith(fetch(event.request));
        return;
    }

    if (event.request.mode === "navigate") {
        event.respondWith(networkFirstForNavigation(event.request));
        return;
    }

    event.respondWith(cacheFirst(event.request));
});

self.addEventListener("message", (event) => {
    if (event.origin && event.origin !== globalThis.location.origin) {
        return;
    }

    if (event.data?.type === "CACHE_DEFERRED_EMULATOR_ASSETS") {
        event.waitUntil(startDeferredCachingOnce());
    }
});

function startDeferredCachingOnce() {
    if (deferredCachePromise) {
        return deferredCachePromise;
    }

    deferredCachePromise = cacheDeferredAssets().finally(() => {
        deferredCachePromise = null;
    });

    return deferredCachePromise;
}

async function precacheAllAssets() {
    const cache = await caches.open(CACHE_NAME);
    const allAssets = await getAllAssetUrls();

    for (const assetUrl of allAssets) {
        try {
            const request = new Request(assetUrl, { cache: "no-store" });
            const response = await fetch(request);
            if (response.ok || response.type === "opaque") {
                await cache.put(assetUrl, response.clone());
            }
        } catch {
            // Continue precaching remaining assets even when one request fails.
        }
    }
}

async function getAllAssetUrls() {
    const assets = new Set(CORE_ASSETS);

    try {
        const response = await fetch(FILE_LIST_URL, { cache: "no-store" });
        if (response.ok) {
            const fileList = await response.json();
            if (Array.isArray(fileList)) {
                for (const filePath of fileList) {
                    if (typeof filePath === "string" && filePath.startsWith("/")) {
                        assets.add(filePath);
                    }
                }
            }
        }
    } catch {
        // Fall back to core assets when file list endpoint is unavailable.
    }

    return [...assets].filter((path) => !path.startsWith(DEFERRED_PREFIX));
}

async function cacheDeferredAssets() {
    const cache = await caches.open(CACHE_NAME);

    const alreadyCached = await cache.match(DEFERRED_CACHE_MARKER);
    if (alreadyCached) {
        return;
    }

    try {
        const response = await fetch(FILE_LIST_URL, { cache: "no-store" });
        if (!response.ok) {
            return;
        }

        const fileList = await response.json();
        if (!Array.isArray(fileList)) {
            return;
        }

        for (const filePath of fileList) {
            if (typeof filePath !== "string" || !filePath.startsWith(DEFERRED_PREFIX)) {
                continue;
            }

            try {
                const request = new Request(filePath, { cache: "no-store" });
                const assetResponse = await fetch(request);
                if (assetResponse.ok || assetResponse.type === "opaque") {
                    await cache.put(filePath, assetResponse.clone());
                }
            } catch {
                // Keep processing deferred assets even if one file fails.
            }
        }

        await cache.put(DEFERRED_CACHE_MARKER, new Response("ok"));
    } catch {
        // Defer phase is best effort and can be retried on next app start.
    }
}

async function cleanupOldCaches() {
    const cacheNames = await caches.keys();
    await Promise.all(
        cacheNames
            .filter((name) => name !== CACHE_NAME)
            .map((name) => caches.delete(name))
    );
}

async function networkFirstForNavigation(request) {
    const cache = await caches.open(CACHE_NAME);

    try {
        const response = await fetch(request);
        if (response.ok) {
            cache.put(request, response.clone()).catch(() => {
                // Best effort background cache write.
            });
        }
        return response;
    } catch {
        const cachedNavigation = await cache.match(request);
        if (cachedNavigation) {
            return cachedNavigation;
        }

        const cachedIndex = await cache.match("/index.html");
        if (cachedIndex) {
            return cachedIndex;
        }

        return new Response("Offline and no cached content available.", {
            status: 503,
            statusText: "Service Unavailable",
            headers: { "Content-Type": "text/plain; charset=utf-8" }
        });
    }
}

async function cacheFirst(request) {
    const cache = await caches.open(CACHE_NAME);
    const cachedResponse = await cache.match(request);

    if (cachedResponse) {
        return cachedResponse;
    }

    const response = await fetch(request);
    if (response.ok || response.type === "opaque") {
        cache.put(request, response.clone()).catch(() => {
            // Best effort background cache write.
        });
    }

    return response;
}
