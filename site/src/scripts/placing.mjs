// The script Base.astro puts first in every page's head. After a language switch it hides the page
// until place.ts has put the reader back in place, so the jump from the top never shows; the timer is
// a backstop. astro.config.mjs hashes this same text into the security policy, which blocks any
// inline script it does not name.

export const PLACE_KEY = 'language-place';

export const PLACING = `try{if(sessionStorage.getItem('${PLACE_KEY}')){document.documentElement.dataset.placing='';setTimeout(()=>delete document.documentElement.dataset.placing,1500)}}catch{}`;
