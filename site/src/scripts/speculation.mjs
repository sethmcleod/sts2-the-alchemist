// The rules Base.astro gives the browser for loading a page before its link is followed: fetched
// while the pointer rests on the link, and made ready once it is pressed. A card dialog link loads
// its own page (scripts/sheet.ts), and a language link notes the reader's place only as it is
// followed (scripts/place.ts), so neither is made ready ahead. astro.config.mjs hashes this same text
// into the security policy.

const everyLink = (excluded) => ({ and: [{ href_matches: '/*' }, { not: { selector_matches: excluded } }] });

export const SPECULATION = JSON.stringify({
  prefetch: [{ eagerness: 'moderate', where: everyLink('[data-sheet-link]') }],
  prerender: [{ eagerness: 'conservative', where: everyLink('[data-sheet-link], [data-language]') }],
});
