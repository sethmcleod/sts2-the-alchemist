// A card, relic, potion or power link opens that page's [data-sheet] in a dialog, under the page's
// own URL, so Back closes it and a reload shows the page itself. Without a script, or with a
// modifier key held, the link simply opens the page.

const dialog = document.querySelector<HTMLDialogElement>('#sheet')!;
const body = dialog.querySelector<HTMLElement>('[data-sheet-body]')!;
const sheets = new Map<string, Promise<{ title: string; sheet: Element }>>();
const pageTitle = document.title;
// How many dialog entries sit on top of the page in the history
let depth = 0;
// Only the latest request may open the dialog; closing it cancels any request still loading
let latest = 0;

function load(url: string) {
  let entry = sheets.get(url);
  if (!entry) {
    entry = fetch(url)
      .then((response) => {
        if (!response.ok) throw new Error(`${url} answered ${response.status}`);
        return response.text();
      })
      .then((html) => {
        const page = new DOMParser().parseFromString(html, 'text/html');
        const sheet = page.querySelector('[data-sheet]');
        if (!sheet) throw new Error(`${url} has no sheet`);
        return { title: page.title, sheet };
      });
    entry.catch(() => sheets.delete(url));
    sheets.set(url, entry);
  }
  return entry;
}

async function open(url: string, push: boolean) {
  if (push && dialog.open && history.state?.sheet === url) return;
  const ticket = ++latest;
  const loaded = await load(url).catch(() => null);
  if (ticket !== latest) return;
  if (!loaded) {
    location.href = url;
    return;
  }
  body.replaceChildren(document.importNode(loaded.sheet, true));
  document.title = loaded.title;
  if (push) {
    history.pushState({ sheet: url, depth: ++depth, page: history.state?.page ?? location.href }, '', url);
  }
  if (!dialog.open) dialog.showModal();
  dialog.scrollTop = 0;
  // The link that was followed is gone with the old sheet, so focus starts at the new one's title
  body.querySelector<HTMLElement>('#sheet-title')?.focus();
}

const linkOf = (e: Event) => (e.target as Element).closest?.<HTMLAnchorElement>('a[data-sheet-link], a[href^="#"]');

document.addEventListener('click', (e) => {
  const link = linkOf(e);
  if (!link || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
  const hash = link.getAttribute('href')!;
  if (hash.startsWith('#')) {
    // A link to another spot in the open sheet scrolls there and leaves the history alone
    if (!dialog.contains(link)) return;
    e.preventDefault();
    body.querySelector(`[id="${CSS.escape(decodeURIComponent(hash.slice(1)))}"]`)?.scrollIntoView();
    return;
  }
  e.preventDefault();
  open(link.href, true);
});

// A link the pointer rests on, or that has focus, starts loading before it is clicked
let resting = 0;
const saveData = (navigator as Navigator & { connection?: { saveData?: boolean } }).connection?.saveData;
if (!saveData) {
  document.addEventListener('pointerover', (e) => {
    clearTimeout(resting);
    const link = linkOf(e);
    if (link?.dataset.sheetLink !== undefined) resting = window.setTimeout(() => load(link.href), 150);
  });
  document.addEventListener('focusin', (e) => {
    const link = linkOf(e);
    if (link?.dataset.sheetLink !== undefined) load(link.href);
  });
}

window.addEventListener('popstate', (e) => {
  depth = e.state?.depth ?? 0;
  if (e.state?.sheet) open(e.state.sheet, false);
  else dialog.close();
});

dialog.addEventListener('close', () => {
  latest++;
  document.title = pageTitle;
  if (depth > 0) history.go(-depth);
  depth = 0;
});

// A click on the backdrop closes the dialog, but not the end of a drag that started inside it
let pressedOutside = false;
dialog.addEventListener('pointerdown', (e) => (pressedOutside = e.target === dialog));
dialog.addEventListener('click', (e) => {
  if (e.target === dialog && pressedOutside) dialog.close();
});
