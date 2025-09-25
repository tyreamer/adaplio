// Development service worker - minimal functionality
console.log('Service worker script loaded');

// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', function(event) {
    // Let the browser handle the fetch request normally
    // No caching in development mode
    console.log('Fetch event for:', event.request.url);
});

// Handle cache operations gracefully to prevent NotFoundError
self.addEventListener('install', function(event) {
    console.log('Service worker installing...');
    // Skip waiting to activate immediately
    self.skipWaiting();
});

self.addEventListener('activate', function(event) {
    console.log('Service worker activating...');

    event.waitUntil(
        caches.keys()
            .then(function(cacheNames) {
                return Promise.all(
                    cacheNames.map(function(cacheName) {
                        // Clear any existing caches during development
                        console.log('Deleting cache:', cacheName);
                        return caches.delete(cacheName);
                    })
                );
            })
            .catch(function(error) {
                console.log('Cache cleanup error:', error);
            })
            .then(function() {
                console.log('Service worker activated');
                return self.clients.claim();
            })
    );
});

// Handle any errors
self.addEventListener('error', function(event) {
    console.error('Service worker error:', event.error);
});

self.addEventListener('unhandledrejection', function(event) {
    console.error('Service worker unhandled promise rejection:', event.reason);
});
