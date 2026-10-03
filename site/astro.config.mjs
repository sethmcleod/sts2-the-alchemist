import { createHash } from 'node:crypto';
import preact from '@astrojs/preact';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'astro/config';
import { catalog } from './src/i18n/catalog.mjs';
import { sitemap } from './src/integrations/sitemap.mjs';
import { PLACING_SCRIPT } from './src/scripts/placing.mjs';
import { SPECULATION_RULES } from './src/scripts/speculation.mjs';
import { VIEW_SCRIPT } from './src/scripts/view.mjs';

const cspHash = (text) => `sha256-${createHash('sha256').update(text).digest('base64')}`;

export default defineConfig({
  build: { format: 'file' },
  devToolbar: { enabled: false },
  integrations: [preact(), catalog(), sitemap()],
  markdown: { syntaxHighlight: false },
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
      scriptDirective: { hashes: [cspHash(PLACING_SCRIPT), cspHash(SPECULATION_RULES), cspHash(VIEW_SCRIPT)] },
      styleDirective: { resources: ["'self'", "'unsafe-inline'"] },
    },
  },
  site: 'https://alchemist.fyi',
  trailingSlash: 'never',
  vite: {
    plugins: [tailwindcss()],
    server: { fs: { allow: ['.', '../Alchemist/images', '../workshop/previews', '../workshop/image.png'] } },
  },
});
