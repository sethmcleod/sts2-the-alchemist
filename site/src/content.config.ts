import { defineCollection } from 'astro:content';
import { glob } from 'astro/loaders';

// Design notes shown on a card, relic, potion or power page: src/content/commentary/cards/rolling-boil.md
// shows on /cards/rolling-boil
const commentary = defineCollection({
  loader: glob({ pattern: '{cards,relics,potions,powers}/*.md', base: './src/content/commentary' }),
});

export const collections = { commentary };
