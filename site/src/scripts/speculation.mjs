const everyLinkExcept = (selector) => ({ and: [{ href_matches: '/*' }, { not: { selector_matches: selector } }] });

export const SPECULATION_RULES = JSON.stringify({
  prefetch: [{ eagerness: 'moderate', where: everyLinkExcept('[data-sheet-link]') }],
  prerender: [{ eagerness: 'conservative', where: everyLinkExcept('[data-sheet-link], [data-language]') }],
});
