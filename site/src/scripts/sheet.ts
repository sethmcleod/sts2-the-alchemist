const dialog = document.querySelector<HTMLDialogElement>('#sheet')!;
const body = dialog.querySelector<HTMLElement>('[data-sheet-body]')!;
const sheetRequests = new Map<string, Promise<{ sheet: Element; title: string }>>();
const pageTitle = document.title;
let pushedEntries = 0;
let latestRequest = 0;

function loadSheet(url: string) {
  let request = sheetRequests.get(url);
  if (!request) {
    request = fetch(url)
      .then((response) => {
        if (!response.ok) throw new Error(`${url} answered ${response.status}`);
        return response.text();
      })
      .then((html) => {
        const page = new DOMParser().parseFromString(html, 'text/html');
        const sheet = page.querySelector('[data-sheet]');
        if (!sheet) throw new Error(`${url} has no sheet`);
        return { sheet, title: page.title };
      });
    request.catch(() => sheetRequests.delete(url));
    sheetRequests.set(url, request);
  }
  return request;
}

async function openSheet(url: string, pushHistory: boolean) {
  if (pushHistory && dialog.open && history.state?.sheet === url) return;
  const requestId = ++latestRequest;
  const loaded = await loadSheet(url).catch(() => null);
  if (requestId !== latestRequest) return;
  if (!loaded) {
    location.href = url;
    return;
  }
  body.replaceChildren(document.importNode(loaded.sheet, true));
  document.title = loaded.title;
  const toggle = body.querySelector<HTMLInputElement>('.upgrade-toggle');
  if (!pushHistory && toggle) toggle.checked = (location.hash === '#upgraded') !== allUpgraded();
  if (pushHistory) {
    history.pushState(
      { depth: ++pushedEntries, page: history.state?.page ?? location.href, sheet: url },
      '',
      url + (allUpgraded() ? '#upgraded' : ''),
    );
  }
  if (!dialog.open) dialog.showModal();
  dialog.scrollTop = 0;
  body.querySelector<HTMLElement>('#sheet-title')?.focus();
}

function allUpgraded() {
  return document.querySelector<HTMLInputElement>('#all-upgraded')?.checked ?? false;
}

const linkOf = (e: Event) => (e.target as Element).closest?.<HTMLAnchorElement>('a[data-sheet-link], a[href^="#"]');

document.addEventListener('click', (e) => {
  const link = linkOf(e);
  if (!link || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
  const href = link.getAttribute('href')!;
  if (href.startsWith('#')) {
    if (!dialog.contains(link)) return;
    e.preventDefault();
    body.querySelector(`[id="${CSS.escape(decodeURIComponent(href.slice(1)))}"]`)?.scrollIntoView();
    return;
  }
  e.preventDefault();
  openSheet(link.href, true);
});

let hoverTimer = 0;
const saveData = (navigator as Navigator & { connection?: { saveData?: boolean } }).connection?.saveData;
if (!saveData) {
  document.addEventListener('pointerover', (e) => {
    clearTimeout(hoverTimer);
    const link = linkOf(e);
    if (link?.dataset.sheetLink !== undefined) hoverTimer = window.setTimeout(() => loadSheet(link.href), 150);
  });
  document.addEventListener('focusin', (e) => {
    const link = linkOf(e);
    if (link?.dataset.sheetLink !== undefined) loadSheet(link.href);
  });
}

window.addEventListener('popstate', (e) => {
  pushedEntries = e.state?.depth ?? 0;
  if (e.state?.sheet) openSheet(e.state.sheet, false);
  else dialog.close();
});

dialog.addEventListener('close', () => {
  latestRequest++;
  document.title = pageTitle;
  if (pushedEntries > 0) history.go(-pushedEntries);
  pushedEntries = 0;
});

body.addEventListener('change', (e) => {
  const toggle = (e.target as Element).closest<HTMLInputElement>('.upgrade-toggle');
  if (!toggle || !history.state?.sheet) return;
  history.replaceState(history.state, '', history.state.sheet + (toggle.checked !== allUpgraded() ? '#upgraded' : ''));
});

let pressedOutside = false;
dialog.addEventListener('pointerdown', (e) => (pressedOutside = e.target === dialog));
dialog.addEventListener('click', (e) => {
  if (e.target === dialog && pressedOutside) dialog.close();
});
