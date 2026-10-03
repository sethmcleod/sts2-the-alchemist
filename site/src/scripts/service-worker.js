// Offline support. Every page and file a visitor opens is kept, so the site still opens without a
// connection: pages and data come from the network first and fall back to the copy, and the build's
// hashed files, which never change, come from the copy first. Each build serves a new worker
// (src/pages/sw.js.ts stamps it), which fetches the start pages again and drops the build files
// that no kept page names any more.

const PAGES = 'pages';
const FILES = 'files';
const OFFLINE = '/offline';
const START = ['/', '/relics', '/powers', '/notes', '/stats', OFFLINE];

// A page in another language sits under its code: /de/relics
const languageOf = (url) => {
  const first = new URL(url, self.location.href).pathname.split('/')[1];
  return /^[a-z]{2}(?:-[a-z0-9]+)?$/.test(first) ? `/${first}` : '';
};
const inLanguage = (language, page) => (language && page === '/' ? language : `${language}${page}`);

// A page is the same page whatever its query, which only holds filters
const pageKey = (url) => {
  const { origin, pathname } = new URL(url, self.location.href);
  return `${origin}${pathname}`;
};

// Every build file a page names, and those it needs before it can show: its styles, scripts and
// fonts (its images wait until they are shown)
const buildFiles = (html) => [...new Set(html.match(/\/_astro\/[\w.-]+\.\w+/g) ?? [])];
const firstFiles = (html) => buildFiles(html).filter((file) => /\.(?:css|js|woff2)$/.test(file));

async function keepPage(url) {
  const response = await fetch(url);
  if (!response.ok) return;
  const html = await response.clone().text();
  await (await caches.open(PAGES)).put(pageKey(url), response);
  const cache = await caches.open(FILES);
  await Promise.allSettled(firstFiles(html).map((file) => cache.add(file)));
}

async function dropUnusedFiles() {
  const [pages, files] = await Promise.all([caches.open(PAGES), caches.open(FILES)]);
  const named = new Set();
  const name = async (cache, request) => {
    for (const file of buildFiles(await (await cache.match(request)).text())) {
      named.add(new URL(file, self.location.href).href);
    }
  };
  for (const request of await pages.keys()) await name(pages, request);
  // A stylesheet names the fonts, which no page does
  const kept = (await files.keys()).filter((request) => named.has(request.url));
  for (const request of kept.filter((request) => request.url.endsWith('.css'))) await name(files, request);
  for (const request of await files.keys()) {
    if (new URL(request.url).pathname.startsWith('/_astro/') && !named.has(request.url)) await files.delete(request);
  }
}

self.addEventListener('install', (event) => {
  event.waitUntil(
    (async () => {
      // The start pages in the languages of the open tabs
      const open = await self.clients.matchAll({ includeUncontrolled: true, type: 'window' });
      const languages = new Set(open.map((client) => languageOf(client.url)));
      if (!languages.size) languages.add('');
      // The English offline page is the last fallback for a page in any language
      const pages = new Set([
        OFFLINE,
        ...[...languages].flatMap((language) => START.map((page) => inLanguage(language, page))),
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
      // The page that installed the worker loaded before it, so it is kept now
      const open = await self.clients.matchAll({ type: 'window' });
      await Promise.allSettled(open.map((client) => keepPage(client.url)));
      await dropUnusedFiles();
    })(),
  );
});

async function networkFirst(request, name, key) {
  const cache = await caches.open(name);
  try {
    const response = await fetch(request);
    if (response.ok) cache.put(key, response.clone());
    return response;
  } catch (error) {
    const copy = await cache.match(key);
    if (copy) return copy;
    throw error;
  }
}

async function cacheFirst(request) {
  const cache = await caches.open(FILES);
  const copy = await cache.match(request);
  if (copy) return copy;
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
    // Pages, including the ones the card dialog fetches. Anything else, such as Vercel's analytics
    // scripts, goes to the network untouched
    event.respondWith(
      networkFirst(request, PAGES, pageKey(request.url)).catch(async () =>
        request.mode === 'navigate'
          ? ((await caches.match(pageKey(inLanguage(languageOf(request.url), OFFLINE)))) ??
            (await caches.match(pageKey(OFFLINE))) ??
            Response.error())
          : Response.error(),
      ),
    );
  }
});
