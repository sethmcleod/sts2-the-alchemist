// Writes sitemap.xml once the build is done, from the pages it made: every page in every language,
// less the offline and not-found pages. robots.txt names it (pages/robots.txt.ts)

import fs from 'node:fs';

const LEFT_OUT = /(?:^|\/)(?:404|offline)$/;

export function sitemap() {
  let site;
  return {
    hooks: {
      'astro:build:done': ({ dir, pages }) => {
        const paths = pages
          .map(({ pathname }) => pathname.replace(/\/$/, ''))
          .filter((path) => !path.includes('.') && !LEFT_OUT.test(path))
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
