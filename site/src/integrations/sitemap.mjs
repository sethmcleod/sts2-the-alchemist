import fs from 'node:fs';

const UNLISTED_PAGES = /(?:^|\/)(?:404|offline)$/;

export function sitemap() {
  let site;
  return {
    hooks: {
      'astro:build:done': ({ dir, pages }) => {
        const paths = pages
          .map(({ pathname }) => pathname.replace(/\/$/, ''))
          .filter((path) => !path.includes('.') && !UNLISTED_PAGES.test(path))
          .sort();
        const urls = paths.map((path) => `  <url><loc>${new URL(`/${path}`, site).href}</loc></url>`);
        const xml = `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls.join('\n')}\n</urlset>\n`;
        fs.writeFileSync(new URL('sitemap.xml', dir), xml);
      },
      'astro:config:done': ({ config }) => {
        site = config.site;
      },
    },
    name: 'sitemap',
  };
}
