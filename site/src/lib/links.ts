import { slug } from './mod';

export const cardHref = (id: string) => `/cards/${slug(id)}`;
export const relicHref = (id: string) => `/relics/${slug(id)}`;
export const potionHref = (id: string) => `/potions/${slug(id)}`;
export const powerHref = (id: string) => `/powers/${slug(id)}`;
export const notesHref = (version: string) => `/notes#${version}`;

export function releaseHref(repo: null | string, releases: Record<string, string>, version: string) {
  const tag = releases[version];
  return repo && tag ? `https://github.com/${repo}/releases/tag/${tag}` : null;
}

export const opensInSheet = (href: string) => /^\/(cards|relics|potions|powers)\//.test(href);

export const pagePath = (url: URL) => url.pathname.replace(/(\/index)?\.html$/, '').replace(/(.)\/$/, '$1') || '/';
