import preact from '@astrojs/preact';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'astro/config';
import { createHash } from 'node:crypto';
import { catalog } from './src/i18n/catalog.mjs';
import { PLACING } from './src/scripts/placing.mjs';

export default defineConfig({
  site: 'https://alchemist.fyi',
  // /cards/rolling-boil.html, served as /cards/rolling-boil (vercel.json cleanUrls)
  build: { format: 'file' },
  devToolbar: { enabled: false },
  trailingSlash: 'never',
  // catalog() writes src/i18n/en.json, the English every translation follows
  integrations: [preact(), catalog()],
  // Commentary is prose, and Shiki's inline styles would need a looser policy
  markdown: { syntaxHighlight: false },
  // Each page gets a policy with the hashes of its own scripts. Inline styles stay allowed: card
  // faces and charts set their sizes in style attributes
  security: {
    csp: {
      directives: [
        "default-src 'self'",
        "img-src 'self' data:",
        "connect-src 'self'",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self'",
      ],
      styleDirective: { resources: ["'self'", "'unsafe-inline'"] },
      // Astro hashes the scripts it bundles; the one inline script is hashed here
      scriptDirective: { hashes: [`sha256-${createHash('sha256').update(PLACING).digest('base64')}`] },
    },
  },
  vite: {
    plugins: [tailwindcss()],
    // The dev server may serve the site and the mod's images from the repo, and nothing else: the
    // repo also holds local secrets
    server: { fs: { allow: ['.', '../Alchemist/images', '../workshop/previews'] } },
  },
});
