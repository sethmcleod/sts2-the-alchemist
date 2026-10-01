// What a browser needs to install the site as an app. The service worker (scripts/service-worker.js)
// already keeps it working offline

import type { APIRoute } from 'astro';
import { getImage } from 'astro:assets';
import icon from '../../../workshop/image.png';

export const GET: APIRoute = async () => {
  const icons = await Promise.all(
    [192, icon.width].map(async (size) => ({
      src: (await getImage({ src: icon, width: size, format: 'png' })).src,
      sizes: `${size}x${size}`,
      type: 'image/png',
    })),
  );
  const manifest = {
    name: 'The Alchemist',
    short_name: 'Alchemist',
    description: 'Every card, relic and potion of The Alchemist, a Slay the Spire 2 character mod, and its run stats.',
    lang: 'en',
    start_url: '/',
    scope: '/',
    display: 'standalone',
    background_color: '#1c1822',
    theme_color: '#1c1822',
    icons,
  };
  return new Response(JSON.stringify(manifest), { headers: { 'Content-Type': 'application/manifest+json' } });
};
