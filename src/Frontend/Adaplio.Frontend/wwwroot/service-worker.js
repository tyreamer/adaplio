// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', () => { });

// Handle cache operations gracefully to prevent NotFoundError
self.addEventListener('install', (event) => {
    console.log('Service worker installing...');
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    console.log('Service worker activating...');
    event.waitUntil(
        caches.keys().then((cacheNames) => {
            return Promise.all(
                cacheNames.map((cacheName) => {
                    // Clear any existing caches during development
                    return caches.delete(cacheName);
                })
            );
        }).catch((error) => {
            console.log('Cache cleanup error:', error);
        })
    );
    return self.clients.claim();
});
