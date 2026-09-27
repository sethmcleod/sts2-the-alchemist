// Switching language keeps the reader's place. The new page gets the same filters and sort (a search
// stays behind, since its words are in the old language), the same cards flipped to their upgrade,
// and the same card or heading at the same height on the screen. Text runs longer or shorter in
// another language, so the place is that element rather than a scroll offset.

import { PLACE_KEY as KEY } from './placing.mjs';

interface Place {
  path: string;
  /** A card or item by its id, or a heading by its position on the page, and its height on screen */
  anchor: { place?: string; heading?: number; top: number } | null;
  upgraded: string[];
}

// A heading has the same position on the page in every language
const headings = () => [...document.querySelectorAll<HTMLElement>('main :is(h1, h2, h3)')];

/** Notes where the reader is, just before a language link opens the page in that language */
export function keepPlace(link: HTMLAnchorElement) {
  const params = new URLSearchParams(location.search);
  params.delete('q');
  link.search = params.toString();
  // The first card or heading below the header's bottom edge, leaving out the bars that stick there
  const edge = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--header-stuck')) || 0;
  const first =
    scrollY > 0
      ? [...document.querySelectorAll<HTMLElement>('[data-place], main :is(h1, h2, h3)')].find((el) => {
          const { top, height } = el.getBoundingClientRect();
          return height > 0 && top >= edge && !el.closest('.toolbar') && getComputedStyle(el).position !== 'sticky';
        })
      : undefined;
  const anchor = first && {
    ...(first.dataset.place ? { place: first.dataset.place } : { heading: headings().indexOf(first) }),
    top: first.getBoundingClientRect().top,
  };
  const upgraded = [...document.querySelectorAll<HTMLInputElement>('.upgrade-toggle:checked')]
    .map((toggle) => toggle.closest<HTMLElement>('[data-place]')?.dataset.place)
    .filter((id): id is string => Boolean(id));
  if (!anchor && !upgraded.length) return;
  const place: Place = { path: link.pathname, anchor: anchor ?? null, upgraded };
  try {
    sessionStorage.setItem(KEY, JSON.stringify(place));
  } catch {
    // Without storage the new page opens at its top
  }
}

/** Puts the reader back at the place a language link noted, once the page has laid itself out, and
 *  shows the page, which Base.astro hides meanwhile */
export function returnToPlace() {
  let place: Place | null = null;
  try {
    place = JSON.parse(sessionStorage.getItem(KEY) ?? 'null');
    sessionStorage.removeItem(KEY);
  } catch {
    // No storage, no place
  }
  // After every page script has run, so the library has applied its filters
  document.addEventListener('DOMContentLoaded', () => {
    if (place?.path === location.pathname) {
      for (const id of place.upgraded) {
        const toggle = document.querySelector<HTMLInputElement>(`[data-place="${CSS.escape(id)}"] .upgrade-toggle`);
        if (toggle) toggle.checked = true;
      }
      const { anchor } = place;
      const scroll = () => {
        const el = anchor?.place
          ? document.querySelector<HTMLElement>(`[data-place="${CSS.escape(anchor.place)}"]`)
          : headings()[anchor?.heading ?? -1];
        if (el) window.scrollTo({ top: scrollY + el.getBoundingClientRect().top - anchor!.top, behavior: 'instant' });
        return scrollY;
      };
      if (anchor) {
        const at = scroll();
        // Late fonts can move the text above; follow them unless the reader has scrolled since
        document.fonts.ready.then(() => scrollY === at && scroll());
      }
    }
    delete document.documentElement.dataset.placing;
  });
}
