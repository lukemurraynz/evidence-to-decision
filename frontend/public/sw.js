const CACHE_NAME = 'opportunity-workshop-shell-v1'
const SHELL_PATHS = ['/', '/index.html']

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then(async (cache) => {
      await cache.addAll(SHELL_PATHS)
      const response = await fetch('/index.html', { cache: 'no-store' })
      const html = await response.text()
      const assetPaths = [
        ...new Set(
          [...html.matchAll(/(?:src|href)="(\/assets\/[^"]+)"/g)].map(
            (match) => match[1],
          ),
        ),
      ]
      await cache.addAll(['/favicon.svg', ...assetPaths])
    }),
  )
  self.skipWaiting()
})

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) =>
        Promise.all(
          keys
            .filter((key) => key !== CACHE_NAME)
            .map((key) => caches.delete(key)),
        ),
      ),
  )
  self.clients.claim()
})

self.addEventListener('fetch', (event) => {
  const requestUrl = new URL(event.request.url)
  if (
    event.request.method !== 'GET' ||
    requestUrl.origin !== self.location.origin ||
    requestUrl.pathname.startsWith('/api/') ||
    requestUrl.pathname === '/config.json'
  ) {
    return
  }

  event.respondWith(
    fetch(event.request)
      .then((response) => {
        if (response.ok) {
          const copy = response.clone()
          void caches.open(CACHE_NAME).then((cache) => cache.put(event.request, copy))
        }
        return response
      })
      .catch(async () => {
        const cached = await caches.match(event.request)
        return cached ?? caches.match('/index.html')
      }),
  )
})
