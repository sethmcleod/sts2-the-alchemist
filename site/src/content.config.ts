import { glob } from 'astro/loaders';
import { defineCollection } from 'astro:content';

// Design notes shown on a card, relic, potion or power page: src/content/commentary/cards/rolling-boil.md
// shows on /cards/rolling-boil
const commentary = defineCollection({
  loader: glob({ base: './src/content/commentary', pattern: '{cards,relics,potions,powers}/*.md' }),
});

export const collections = { commentary };
