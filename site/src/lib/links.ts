import { slug } from './mod';

export const cardHref = (id: string) => `/cards/${slug(id)}`;
export const relicHref = (id: string) => `/relics/${slug(id)}`;
export const potionHref = (id: string) => `/potions/${slug(id)}`;
export const powerHref = (id: string) => `/powers/${slug(id)}`;
export const notesHref = (version: string) => `/notes#${version}`;

/** A version's GitHub release, the beta release when there is one */
export function releaseHref(repo: string | null, releases: Record<string, string>, version: string) {
  const tag = releases[version];
  return repo && tag ? `https://github.com/${repo}/releases/tag/${tag}` : null;
}

/** A card, relic, potion or power page, which a link opens in the dialog */
export const opensInSheet = (href: string) => /^\/(cards|relics|potions|powers)\//.test(href);

/** The page's path as the site links to it: /cards/jab rather than /cards/jab.html, and / for home */
export const pagePath = (url: URL) => url.pathname.replace(/(\/index)?\.html$/, '').replace(/(.)\/$/, '$1') || '/';
