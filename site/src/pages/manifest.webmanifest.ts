import type { APIRoute } from 'astro';
import { getImage } from 'astro:assets';
import icon from '../../../workshop/image.png';

export const GET: APIRoute = async () => {
  const icons = await Promise.all(
    [192, icon.width].map(async (size) => ({
      sizes: `${size}x${size}`,
      src: (await getImage({ format: 'png', src: icon, width: size })).src,
      type: 'image/png',
    })),
  );
  const manifest = {
    background_color: '#1c1822',
    description: 'Every card, relic and potion of The Alchemist, a Slay the Spire 2 character mod, and its run stats.',
    display: 'standalone',
    icons,
    lang: 'en',
    name: 'The Alchemist',
    scope: '/',
    short_name: 'Alchemist',
    start_url: '/',
    theme_color: '#1c1822',
  };
  return new Response(JSON.stringify(manifest), { headers: { 'Content-Type': 'application/manifest+json' } });
};
