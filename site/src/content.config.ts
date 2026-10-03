import { glob } from 'astro/loaders';
import { defineCollection } from 'astro:content';

const commentary = defineCollection({
  loader: glob({ base: './src/content/commentary', pattern: '{cards,relics,potions,powers}/*.md' }),
});

export const collections = { commentary };
