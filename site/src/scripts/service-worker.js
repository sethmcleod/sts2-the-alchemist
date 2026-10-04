const PAGES = 'pages';
const FILES = 'files';
const OFFLINE = '/offline';
const START = ['/', '/compendium', '/notes', '/stats', OFFLINE];

const languagePrefixOf = (url) => {
  const first = new URL(url, self.location.href).pathname.split('/')[1];
  return /^[a-z]{2}(?:-[a-z0-9]+)?$/.test(first) ? `/${first}` : '';
};
const inLanguage = (prefix, page) => (prefix && page === '/' ? prefix : `${prefix}${page}`);

const pageKey = (url) => {
  const { origin, pathname } = new URL(url, self.location.href);
  return `${origin}${pathname}`;
};

const buildFiles = (html) => [...new Set(html.match(/\/_astro\/[\w.-]+\.\w+/g) ?? [])];
const criticalFiles = (html) => buildFiles(html).filter((file) => /\.(?:css|js|woff2)$/.test(file));

async function keepPage(url) {
  const response = await fetch(url);
  if (!response.ok) return;
  const html = await response.clone().text();
  await (await caches.open(PAGES)).put(pageKey(url), response);
  const cache = await caches.open(FILES);
  await Promise.allSettled(criticalFiles(html).map((file) => cache.add(file)));
}

async function dropUnusedFiles() {
  const [pages, files] = await Promise.all([caches.open(PAGES), caches.open(FILES)]);
  const namedFiles = new Set();
  const addFilesNamedBy = async (cache, request) => {
    for (const file of buildFiles(await (await cache.match(request)).text())) {
      namedFiles.add(new URL(file, self.location.href).href);
    }
  };
  for (const request of await pages.keys()) await addFilesNamedBy(pages, request);
  const kept = (await files.keys()).filter((request) => namedFiles.has(request.url));
  for (const request of kept.filter((request) => request.url.endsWith('.css'))) await addFilesNamedBy(files, request);
  for (const request of await files.keys()) {
    if (new URL(request.url).pathname.startsWith('/_astro/') && !namedFiles.has(request.url))
      await files.delete(request);
  }
}

self.addEventListener('install', (event) => {
  event.waitUntil(
    (async () => {
      const openTabs = await self.clients.matchAll({ includeUncontrolled: true, type: 'window' });
      const prefixes = new Set(openTabs.map((client) => languagePrefixOf(client.url)));
      if (!prefixes.size) prefixes.add('');
      const pages = new Set([
        OFFLINE,
        ...[...prefixes].flatMap((prefix) => START.map((page) => inLanguage(prefix, page))),
      ]);
      await Promise.allSettled([...pages].map(keepPage));
      await self.skipWaiting();
    })(),
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    (async () => {
      const keys = await caches.keys();
      await Promise.all(keys.filter((key) => key !== PAGES && key !== FILES).map((key) => caches.delete(key)));
      await self.clients.claim();
      const openTabs = await self.clients.matchAll({ type: 'window' });
      await Promise.allSettled(openTabs.map((client) => keepPage(client.url)));
      await dropUnusedFiles();
    })(),
  );
});

async function networkFirst(request, cacheName, key) {
  const cache = await caches.open(cacheName);
  try {
    const response = await fetch(request);
    if (response.ok) cache.put(key, response.clone());
    return response;
  } catch (error) {
    const cached = await cache.match(key);
    if (cached) return cached;
    throw error;
  }
}

async function cacheFirst(request) {
  const cache = await caches.open(FILES);
  const cached = await cache.match(request);
  if (cached) return cached;
  const response = await fetch(request);
  if (response.ok) cache.put(request, response.clone());
  return response;
}

self.addEventListener('fetch', (event) => {
  const { request } = event;
  const url = new URL(request.url);
  if (request.method !== 'GET' || url.origin !== self.location.origin || url.pathname === '/sw.js') return;

  if (url.pathname.startsWith('/_astro/')) {
    event.respondWith(cacheFirst(request));
  } else if (url.pathname.startsWith('/data/')) {
    event.respondWith(networkFirst(request, FILES, request));
  } else if (request.mode === 'navigate' || request.destination === '') {
    event.respondWith(
      networkFirst(request, PAGES, pageKey(request.url)).catch(async () =>
        request.mode === 'navigate'
          ? ((await caches.match(pageKey(inLanguage(languagePrefixOf(request.url), OFFLINE)))) ??
            (await caches.match(pageKey(OFFLINE))) ??
            Response.error())
          : Response.error(),
      ),
    );
  }
});
