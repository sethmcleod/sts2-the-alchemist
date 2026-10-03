import { PLACE_KEY } from './placing.mjs';

interface Place {
  anchor: null | { heading?: number; place?: string; top: number };
  path: string;
  upgraded: string[];
}

const headings = () => [...document.querySelectorAll<HTMLElement>('main :is(h1, h2, h3)')];

export function keepPlace(link: HTMLAnchorElement) {
  const params = new URLSearchParams(location.search);
  params.delete('q');
  link.search = params.toString();
  const headerBottom = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--header-stuck')) || 0;
  const firstVisible =
    scrollY > 0
      ? [...document.querySelectorAll<HTMLElement>('[data-place], main :is(h1, h2, h3)')].find((el) => {
          const { height, top } = el.getBoundingClientRect();
          return (
            height > 0 && top >= headerBottom && !el.closest('.toolbar') && getComputedStyle(el).position !== 'sticky'
          );
        })
      : undefined;
  const anchor = firstVisible && {
    ...(firstVisible.dataset.place
      ? { place: firstVisible.dataset.place }
      : { heading: headings().indexOf(firstVisible) }),
    top: firstVisible.getBoundingClientRect().top,
  };
  const flippedByAddress = document.querySelector('#upgraded:target') !== null;
  const upgraded = [...document.querySelectorAll<HTMLInputElement>('.upgrade-toggle')]
    .filter((toggle) => toggle.checked !== flippedByAddress)
    .map((toggle) => toggle.closest<HTMLElement>('[data-place]')?.dataset.place)
    .filter((id): id is string => Boolean(id));
  if (!anchor && !upgraded.length) return;
  const place: Place = { anchor: anchor ?? null, path: link.pathname, upgraded };
  try {
    sessionStorage.setItem(PLACE_KEY, JSON.stringify(place));
  } catch {}
}

export function returnToPlace() {
  let place: null | Place = null;
  try {
    place = JSON.parse(sessionStorage.getItem(PLACE_KEY) ?? 'null');
    sessionStorage.removeItem(PLACE_KEY);
  } catch {}
  document.addEventListener('DOMContentLoaded', () => {
    if (place?.path === location.pathname) {
      for (const id of place.upgraded) {
        const toggle = document.querySelector<HTMLInputElement>(`[data-place="${CSS.escape(id)}"] .upgrade-toggle`);
        if (toggle) toggle.checked = true;
      }
      if (document.getElementById('upgraded') && document.querySelector('[data-sheet] .upgrade-toggle:checked')) {
        addEventListener(
          'load',
          () => history.replaceState(history.state, '', `${location.pathname}${location.search}#upgraded`),
          { once: true },
        );
      }
      const { anchor } = place;
      const scrollToAnchor = () => {
        const el = anchor?.place
          ? document.querySelector<HTMLElement>(`[data-place="${CSS.escape(anchor.place)}"]`)
          : headings()[anchor?.heading ?? -1];
        if (el) window.scrollTo({ behavior: 'instant', top: scrollY + el.getBoundingClientRect().top - anchor!.top });
        return scrollY;
      };
      if (anchor) {
        const scrolledTo = scrollToAnchor();
        document.fonts.ready.then(() => scrollY === scrolledTo && scrollToAnchor());
      }
    }
    delete document.documentElement.dataset.placing;
  });
}
