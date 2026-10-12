// Cache only the installable application shell. API responses are always network-only.
self.importScripts('./service-worker-assets.js');

const cachePrefix = 'prisma-shell-';
const cacheName = `${cachePrefix}${self.assetsManifest.version}`;
const baseUrl = new URL('/', self.location.origin);

function isApiUrl(url) {
    return url.origin === self.location.origin && /(^|\/)api(?:\/|$)/i.test(url.pathname);
}

function isShellAsset(asset) {
    const url = new URL(asset.url, baseUrl);
    return url.origin === self.location.origin && !isApiUrl(url);
}

self.addEventListener('install', event => {
    event.waitUntil((async () => {
        const cache = await caches.open(cacheName);
        const requests = self.assetsManifest.assets
            .filter(isShellAsset)
            .map(asset => new Request(asset.url, {
                integrity: asset.hash,
                cache: 'reload'
            }));
        await cache.addAll(requests);
        await self.skipWaiting();
    })());
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        const keys = await caches.keys();
        await Promise.all(keys
            .filter(key => key.startsWith(cachePrefix) && key !== cacheName)
            .map(key => caches.delete(key)));
        await self.clients.claim();
    })());
});

self.addEventListener('fetch', event => {
    const request = event.request;
    const url = new URL(request.url);

    if (request.method !== 'GET' || url.origin !== self.location.origin || isApiUrl(url)) {
        return;
    }

    event.respondWith((async () => {
        const cache = await caches.open(cacheName);
        const cachedResponse = request.mode === 'navigate'
            ? await cache.match(new URL('index.html', baseUrl).href)
            : await cache.match(request);

        if (cachedResponse) {
            return cachedResponse;
        }

        return fetch(request);
    })());
});
